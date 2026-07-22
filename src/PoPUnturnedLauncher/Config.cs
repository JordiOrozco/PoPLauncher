using System;
using System.IO;
using System.Text.Json;

namespace PoPUnturnedLauncher
{
    public class LauncherConfig
    {
        public string ServerName { get; set; } = "PoPUnturned Roleplay";
        public string ServerIp { get; set; } = "102.129.137.140";
        public int ServerPort { get; set; } = 27015;
        public int QueryPort { get; set; } = 27016;
        public int SteamAppId { get; set; } = 304930;
        public string DiscordUrl { get; set; } = "https://discord.popunturned.com";
        public string WebsiteUrl { get; set; } = "https://popunturned.com";
        public string YoutubeUrl { get; set; } = "https://www.youtube.com/@PoPUnturned";
        public string TiktokUrl { get; set; } = "https://www.tiktok.com/@popunturnedrp";
        public string TwitterUrl { get; set; } = "https://x.com/PoPUnturnedRP";
        public string InstagramUrl { get; set; } = "https://www.instagram.com/popunturnedrp/";
        public string CustomUnturnedPath { get; set; } = "";
        public string CustomWorkshopPath { get; set; } = "";
        public string UpdateCheckUrl { get; set; } = "https://raw.githubusercontent.com/JordiOrozco/PoPLauncher/main/version.json";
        public bool EnableMusic { get; set; } = true;
        public double MusicVolume { get; set; } = 50.0;
        public string LauncherVersion { get; set; } = "1.0.0";
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_config.json");
        private static LauncherConfig? _current;

        public static LauncherConfig Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Load();
                }
                return _current;
            }
        }

        public static LauncherConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<LauncherConfig>(json, options) ?? new LauncherConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando configuración: {ex.Message}");
            }
            return new LauncherConfig();
        }

        public static bool Save(LauncherConfig config)
        {
            try
            {
                _current = config;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando configuración: {ex.Message}");
                return false;
            }
        }
    }
}
