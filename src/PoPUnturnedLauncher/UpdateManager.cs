using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PoPUnturnedLauncher
{
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "1.0.0";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public bool Mandatory { get; set; } = false;
    }

    public static class UpdateManager
    {
        public static async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                string checkUrl = ConfigManager.Current.UpdateCheckUrl;
                if (string.IsNullOrWhiteSpace(checkUrl)) return null;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(6);
                    string json = await client.GetStringAsync(checkUrl);
                    var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (info != null && !string.IsNullOrWhiteSpace(info.LatestVersion) && IsNewerVersion(info.LatestVersion))
                    {
                        return info;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Información: No se pudo comprobar actualizaciones ({ex.Message})");
            }

            return null;
        }

        private static bool IsNewerVersion(string remoteVersionStr)
        {
            try
            {
                var local = new Version(ConfigManager.Current.LauncherVersion);
                var remote = new Version(remoteVersionStr);
                return remote > local;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, Action<double> progressCallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl)) return false;

                string tempFile = Path.Combine(Path.GetTempPath(), "PoPUnturnedSetup_Update.exe");
                
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        
                        long? totalBytes = response.Content.Headers.ContentLength;
                        
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int read;
                            
                            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                
                                if (totalBytes.HasValue && totalBytes.Value > 0)
                                {
                                    double progress = (double)totalRead / totalBytes.Value * 100;
                                    progressCallback?.Invoke(progress);
                                }
                            }
                        }
                    }
                }

                // Ejecutar el nuevo instalador en segundo plano para sobreescribir la instalación
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                // Cerrar el launcher actual para permitir el reemplazo de ejecutable
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al descargar actualización: {ex.Message}");
                return false;
            }
        }
    }
}
