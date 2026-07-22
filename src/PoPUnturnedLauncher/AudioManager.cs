using System;
using System.IO;
using System.Windows.Media;

namespace PoPUnturnedLauncher
{
    public static class AudioManager
    {
        private static readonly MediaPlayer _player = new MediaPlayer();
        private static bool _isInitialized = false;

        public static bool IsPlaying { get; private set; } = false;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                // Intentar cargar la pista Lo-Fi Chill local de Assets o usar stream CDN royalty-free
                string localAudioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "chill_lofi.mp3");
                
                if (File.Exists(localAudioPath))
                {
                    _player.Open(new Uri(localAudioPath, UriKind.Absolute));
                }
                else
                {
                    // Stream CDN royalty-free de Lo-Fi Chill por defecto
                    _player.Open(new Uri("https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3?filename=lofi-study-112191.mp3", UriKind.Absolute));
                }

                _player.MediaEnded += Player_MediaEnded;

                // Cargar volumen y preferencia de reproducción
                var config = ConfigManager.Current;
                SetVolume(config.MusicVolume);

                if (config.EnableMusic)
                {
                    Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando AudioManager: {ex.Message}");
            }
        }

        private static void Player_MediaEnded(object? sender, EventArgs e)
        {
            // Bucle infinito continuo
            _player.Position = TimeSpan.Zero;
            if (IsPlaying)
            {
                _player.Play();
            }
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
