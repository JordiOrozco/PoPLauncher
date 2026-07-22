using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PoPUnturnedLauncher
{
    public static class PreferencesManager
    {
        public static string GetPreferencesPath(string unturnedFolder)
        {
            if (string.IsNullOrWhiteSpace(unturnedFolder)) return string.Empty;
            return Path.Combine(unturnedFolder, "Preferences.json");
        }

        public static float GetVehicleEngineVolume(string unturnedFolder)
        {
            string prefPath = GetPreferencesPath(unturnedFolder);
            if (!File.Exists(prefPath))
            {
                return 1.0f; // 100% por defecto
            }

            try
            {
                string jsonContent = File.ReadAllText(prefPath);

                // 1. Probar con Regex primero para ser rápido y flexible
                var match = Regex.Match(jsonContent, @"""Vehicle_Engine_Volume_Multiplier""\s*:\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
                if (match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
                {
                    return Math.Clamp(val, 0.0f, 1.0f);
                }

                // 2. Fallback a JsonNode
                var node = JsonNode.Parse(jsonContent);
                if (node != null)
                {
                    if (node["Vehicle_Engine_Volume_Multiplier"] != null)
                    {
                        return Math.Clamp(node["Vehicle_Engine_Volume_Multiplier"]!.GetValue<float>(), 0.0f, 1.0f);
                    }
                    if (node["Audio"]?["Vehicle_Engine_Volume_Multiplier"] != null)
                    {
                        return Math.Clamp(node["Audio"]!["Vehicle_Engine_Volume_Multiplier"]!.GetValue<float>(), 0.0f, 1.0f);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error leyendo Preferences.json: {ex.Message}");
            }

            return 1.0f;
        }

        public static bool SetVehicleEngineVolume(string unturnedFolder, float volumeMultiplier)
        {
            if (string.IsNullOrWhiteSpace(unturnedFolder) || !Directory.Exists(unturnedFolder))
            {
                return false;
            }

            string prefPath = GetPreferencesPath(unturnedFolder);
            volumeMultiplier = Math.Clamp(volumeMultiplier, 0.0f, 1.0f);
            string formattedVal = volumeMultiplier.ToString("0.00", CultureInfo.InvariantCulture);

            try
            {
                if (File.Exists(prefPath))
                {
                    string jsonContent = File.ReadAllText(prefPath);

                    // Si la propiedad existe en alguna parte del JSON, la reemplazamos manteniendo el formato
                    if (Regex.IsMatch(jsonContent, @"""Vehicle_Engine_Volume_Multiplier""\s*:\s*[0-9]+(?:\.[0-9]+)?", RegexOptions.IgnoreCase))
                    {
                        string updatedJson = Regex.Replace(
                            jsonContent,
                            @"""Vehicle_Engine_Volume_Multiplier""\s*:\s*[0-9]+(?:\.[0-9]+)?",
                            $"\"Vehicle_Engine_Volume_Multiplier\": {formattedVal}",
                            RegexOptions.IgnoreCase
                        );
                        File.WriteAllText(prefPath, updatedJson);
                        return true;
                    }

                    // Si no existe la clave aún en el JSON, parsear con JsonNode e insertarla
                    var node = JsonNode.Parse(jsonContent) as JsonObject;
                    if (node != null)
                    {
                        if (node["Audio"] is JsonObject audioObj)
                        {
                            audioObj["Vehicle_Engine_Volume_Multiplier"] = volumeMultiplier;
                        }
                        else
                        {
                            node["Vehicle_Engine_Volume_Multiplier"] = volumeMultiplier;
                        }

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(prefPath, node.ToJsonString(options));
                        return true;
                    }
                }

                // Si el archivo Preferences.json no existe aún, creamos un JSON limpio
                var newObj = new JsonObject
                {
                    ["Vehicle_Engine_Volume_Multiplier"] = volumeMultiplier
                };
                var writeOpts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(prefPath, newObj.ToJsonString(writeOpts));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando Preferences.json: {ex.Message}");
                return false;
            }
        }
    }
}
