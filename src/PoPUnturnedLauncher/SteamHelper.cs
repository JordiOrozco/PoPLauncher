using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PoPUnturnedLauncher
{
    public static class SteamHelper
    {
        public static string? GetSteamPath()
        {
            try
            {
                // Intentar leer de HKCU
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    object? val = key?.GetValue("SteamPath");
                    if (val != null)
                    {
                        return val.ToString()?.Replace('/', '\\');
                    }
                }

                // Intentar leer de HKLM
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam"))
                {
                    object? val = key?.GetValue("InstallPath");
                    if (val != null)
                    {
                        return val.ToString()?.Replace('/', '\\');
                    }
                }
                
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Valve\Steam"))
                {
                    object? val = key?.GetValue("InstallPath");
                    if (val != null)
                    {
                        return val.ToString()?.Replace('/', '\\');
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo ruta de Steam: {ex.Message}");
            }

            // Rutas por defecto en Windows
            string defaultPath = @"C:\Program Files (x86)\Steam";
            if (Directory.Exists(defaultPath)) return defaultPath;

            defaultPath = @"C:\Program Files\Steam";
            if (Directory.Exists(defaultPath)) return defaultPath;

            return null;
        }

        public static List<string> GetSteamLibraryFolders()
        {
            var libraries = new List<string>();
            string? steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath)) return libraries;

            // La carpeta principal de Steam siempre es una biblioteca
            libraries.Add(steamPath);

            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    string content = File.ReadAllText(vdfPath);
                    // Buscar patrones como: "path"    "D:\\SteamLibrary"
                    var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string path = match.Groups[1].Value.Replace(@"\\", @"\");
                            if (Directory.Exists(path) && !libraries.Contains(path))
                            {
                                libraries.Add(path);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parseando libraryfolders.vdf: {ex.Message}");
                }
            }

            return libraries;
        }

        public static string GetUnturnedDirectory()
        {
            var config = ConfigManager.Current;
            if (!string.IsNullOrWhiteSpace(config.CustomUnturnedPath) && Directory.Exists(config.CustomUnturnedPath))
            {
                return config.CustomUnturnedPath;
            }

            var libraries = GetSteamLibraryFolders();
            foreach (var library in libraries)
            {
                string unturnedPath = Path.Combine(library, "steamapps", "common", "Unturned");
                if (Directory.Exists(unturnedPath))
                {
                    return unturnedPath;
                }
            }

            return @"C:\Program Files (x86)\Steam\steamapps\common\Unturned";
        }

        public static string GetWorkshopDirectory()
        {
            var config = ConfigManager.Current;
            if (!string.IsNullOrWhiteSpace(config.CustomWorkshopPath) && Directory.Exists(config.CustomWorkshopPath))
            {
                return config.CustomWorkshopPath;
            }

            int appId = config.SteamAppId;
            var libraries = GetSteamLibraryFolders();
            foreach (var library in libraries)
            {
                string workshopPath = Path.Combine(library, "steamapps", "workshop", "content", appId.ToString());
                if (Directory.Exists(workshopPath))
                {
                    return workshopPath;
                }
            }

            return $@"C:\Program Files (x86)\Steam\steamapps\workshop\content\{config.SteamAppId}";
        }

        public struct DeletionResult
        {
            public bool Success { get; set; }
            public long BytesFreed { get; set; }
            public int FoldersDeleted { get; set; }
            public string ErrorMessage { get; set; }
        }

        public static DeletionResult DeleteUnturnedWorkshopMods()
        {
            var result = new DeletionResult();
            int appId = ConfigManager.Current.SteamAppId;

            // Recopilar carpetas de workshop a limpiar (personalizada + bibliotecas detectadas)
            var targetFolders = new List<string>();

            string customWorkshop = ConfigManager.Current.CustomWorkshopPath;
            if (!string.IsNullOrWhiteSpace(customWorkshop) && Directory.Exists(customWorkshop))
            {
                targetFolders.Add(customWorkshop);
            }
            else
            {
                var libraries = GetSteamLibraryFolders();
                foreach (var library in libraries)
                {
                    string workshopPath = Path.Combine(library, "steamapps", "workshop", "content", appId.ToString());
                    if (Directory.Exists(workshopPath) && !targetFolders.Contains(workshopPath))
                    {
                        targetFolders.Add(workshopPath);
                    }
                }
            }

            if (targetFolders.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No se pudo encontrar ninguna carpeta de Mods/Workshop de Unturned.";
                return result;
            }

            bool foundAny = false;
            long totalBytes = 0;
            int totalFolders = 0;

            foreach (var workshopPath in targetFolders)
            {
                if (Directory.Exists(workshopPath))
                {
                    foundAny = true;
                    try
                    {
                        var dirInfo = new DirectoryInfo(workshopPath);
                        long size = GetDirectorySize(dirInfo);
                        
                        foreach (var subDir in dirInfo.GetDirectories())
                        {
                            try
                            {
                                subDir.Delete(true);
                                totalFolders++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error borrando subcarpeta {subDir.Name}: {ex.Message}");
                            }
                        }
                        
                        foreach (var file in dirInfo.GetFiles())
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error borrando archivo {file.Name}: {ex.Message}");
                            }
                        }

                        totalBytes += size;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Error al vaciar la carpeta en {workshopPath}: {ex.Message}";
                        return result;
                    }
                }
            }

            if (!foundAny)
            {
                result.Success = true;
                result.BytesFreed = 0;
                result.FoldersDeleted = 0;
                return result;
            }

            result.Success = true;
            result.BytesFreed = totalBytes;
            result.FoldersDeleted = totalFolders;
            return result;
        }

        private static long GetDirectorySize(DirectoryInfo d)
        {
            long size = 0;
            try
            {
                // Sumar tamaño de archivos
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                // Sumar tamaño de subcarpetas
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += GetDirectorySize(di);
                }
            }
            catch
            {
                // Ignorar carpetas sin permisos
            }
            return size;
        }

        public static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0 B";
            long bytesAbs = Math.Abs(bytes);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytesAbs, 1024)));
            double num = Math.Round(bytesAbs / Math.Pow(1024, place), 1);
            return $"{(Math.Sign(bytes) * num)} {suf[place]}";
        }
    }
}
