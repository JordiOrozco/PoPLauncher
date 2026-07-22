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

        // Lista de 5 Pistas Lo-Fi Chill distintas de alta calidad (libres de derechos / royalty-free)
        private static readonly string[] _playlistUrls = new string[]
        {
            "https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3?filename=lofi-study-112191.mp3",       // Pista 1: Study Chill
            "https://cdn.pixabay.com/download/audio/2022/03/15/audio_c8c8a8161c.mp3?filename=chill-lofi-song-8444.mp3",       // Pista 2: Midnight Lounge
            "https://cdn.pixabay.com/download/audio/2023/04/18/audio_24856f4d2f.mp3?filename=lofi-study-medium-149876.mp3", // Pista 3: Coffee Shop Beats
            "https://cdn.pixabay.com/download/audio/2022/11/06/audio_9b6574f85e.mp3?filename=ambient-lofi-chill-126227.mp3",  // Pista 4: Night Rain Ambient
            "https://cdn.pixabay.com/download/audio/2022/10/14/audio_993f350c30.mp3?filename=cozy-lofi-song-122718.mp3"       // Pista 5: Cozy Sunset
        };

        public static bool IsPlaying { get; private set; } = false;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                _player.MediaEnded += Player_MediaEnded;

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
