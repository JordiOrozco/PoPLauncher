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

        // Lista de 5 Pistas Lo-Fi Chill distintas de alta calidad (libres de derechos y sin bloqueo CDN)
        private static readonly string[] _playlistUrls = new string[]
        {
            "https://ia801503.us.archive.org/15/items/ChillLofiSong/chill-lofi-song.mp3",
            "https://raw.githubusercontent.com/AnishDe12020/chill-zone/main/music/lofi1.mp3",
            "https://raw.githubusercontent.com/AnishDe12020/chill-zone/main/music/lofi2.mp3",
            "https://raw.githubusercontent.com/AnishDe12020/chill-zone/main/music/lofi3.mp3",
            "https://ia601503.us.archive.org/15/items/ChillLofiSong/lofi-study.mp3"
        };

        public static bool IsPlaying { get; private set; } = false;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                _player.MediaEnded += Player_MediaEnded;
                _player.MediaFailed += Player_MediaFailed;

                // Cargar volumen y preferencia de reproducción
                var config = ConfigManager.Current;
                SetVolume(config.MusicVolume);

                // Seleccionar una pista aleatoria para el inicio
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

        private static void Player_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MediaFailed en la pista {_currentTrackIndex}: {e.ErrorException?.Message}");
            // Si falla la transmisión por red, intentar con la siguiente pista de forma transparente
            PlayRandomTrack();
        }

        private static void PlayRandomTrack()
        {
            try
            {
                // Elegir índice aleatorio diferente al actual
                int nextIndex;
                if (_playlistUrls.Length > 1)
                {
                    do
                    {
                        nextIndex = Random.Shared.Next(0, _playlistUrls.Length);
                    } while (nextIndex == _currentTrackIndex);
                }
                else
                {
                    nextIndex = 0;
                }

                _currentTrackIndex = nextIndex;

                // Comprobar si existe un archivo local en Assets/track_X.mp3 o usar la URL de la lista
                string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", $"track_{_currentTrackIndex + 1}.mp3");
                string defaultLocalFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "chill_lofi.mp3");

                if (File.Exists(localFile))
                {
                    _player.Open(new Uri(localFile, UriKind.Absolute));
                }
                else if (File.Exists(defaultLocalFile))
                {
                    _player.Open(new Uri(defaultLocalFile, UriKind.Absolute));
                }
                else
                {
                    _player.Open(new Uri(_playlistUrls[_currentTrackIndex], UriKind.Absolute));
                }

                if (ConfigManager.Current.EnableMusic)
                {
                    Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reproduciendo pista aleatoria: {ex.Message}");
            }
        }

        private static void Player_MediaEnded(object? sender, EventArgs e)
        {
            // Al terminar la pista actual, cambiar automáticamente a otra pista aleatoria
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
                double vol = Math.Clamp(volumePercent / 100.0, 0.0, 1.0);
                _player.Volume = vol;
            }
            catch { }
        }
    }
}
