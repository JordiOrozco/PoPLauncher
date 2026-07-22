using System;
using System.IO;
using System.Windows.Media;

namespace PoPUnturnedLauncher
{
    public static class AudioManager
    {
        private static readonly MediaPlayer _player = new MediaPlayer();
        private static bool _isInitialized = false;
        private static int _currentTrackIndex = -1;

        public static bool IsPlaying { get; private set; } = false;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                _player.MediaEnded += Player_MediaEnded;
                _player.MediaFailed += Player_MediaFailed;
                _player.MediaOpened += Player_MediaOpened;

                // Cargar volumen y preferencia de reproducción
                var config = ConfigManager.Current;
                SetVolume(config.MusicVolume);

                // Seleccionar pista aleatoria para el inicio
                PlayRandomTrack();

                if (!config.EnableMusic)
                {
                    Pause();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando AudioManager: {ex.Message}");
            }
        }

        private static void Player_MediaOpened(object? sender, EventArgs e)
        {
            if (ConfigManager.Current.EnableMusic && IsPlaying)
            {
                _player.Play();
            }
        }

        private static void Player_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Error reproduciendo pista {_currentTrackIndex}: {e.ErrorException?.Message}");
        }

        private static void PlayRandomTrack()
        {
            try
            {
                string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                string[] localMp3Files = Directory.Exists(assetsDir) 
                    ? Directory.GetFiles(assetsDir, "*.mp3") 
                    : Array.Empty<string>();

                if (localMp3Files.Length > 0)
                {
                    int nextIndex;
                    if (localMp3Files.Length > 1)
                    {
                        do
                        {
                            nextIndex = Random.Shared.Next(0, localMp3Files.Length);
                        } while (nextIndex == _currentTrackIndex);
                    }
                    else
                    {
                        nextIndex = 0;
                    }

                    _currentTrackIndex = nextIndex;
                    string trackToPlay = localMp3Files[_currentTrackIndex];
                    _player.Open(new Uri(trackToPlay, UriKind.Absolute));

                    if (ConfigManager.Current.EnableMusic)
                    {
                        Play();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seleccionando pista aleatoria: {ex.Message}");
            }
        }

        private static void Player_MediaEnded(object? sender, EventArgs e)
        {
            PlayRandomTrack();
        }

        public static void Play()
        {
            try
            {
                _player.Play();
                IsPlaying = true;
            }
            catch { }
        }

        public static void Pause()
        {
            try
            {
                _player.Pause();
                IsPlaying = false;
            }
            catch { }
        }

        public static bool ToggleMute()
        {
            if (IsPlaying)
            {
                Pause();
                ConfigManager.Current.EnableMusic = false;
            }
            else
            {
                Play();
                ConfigManager.Current.EnableMusic = true;
            }
            ConfigManager.Save(ConfigManager.Current);
            return IsPlaying;
        }

        public static void SetVolume(double volumePercent)
        {
            try
            {
                if (volumePercent <= 0)
                {
                    _player.Volume = 0.0;
                    return;
                }

                // Mapeo perceptual suave sin cortes de cuantización de Windows WASAPI:
                // Garantiza que cualquier porcentaje > 0% sea audible sin cortes de audio repentinos.
                double normalized = Math.Clamp(volumePercent / 100.0, 0.0, 1.0);
                double realVolume = (normalized * 0.15) + 0.015; // 0.015 umbral mínimo audible continuo
                _player.Volume = Math.Clamp(realVolume, 0.0, 1.0);
            }
            catch { }
        }
    }
}
