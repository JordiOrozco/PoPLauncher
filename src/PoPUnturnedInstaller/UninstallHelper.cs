using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace PoPUnturnedInstaller
{
    public static class UninstallHelper
    {
        public static void Uninstall()
        {
            try
            {
                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoPUnturnedLauncher");
                
                // 1. Eliminar accesos directos
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string desktopLnk = Path.Combine(desktopPath, "PoPUnturned Launcher.lnk");
                if (File.Exists(desktopLnk))
                {
                    File.Delete(desktopLnk);
                }

                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string startMenuLnk = Path.Combine(startMenuPath, "PoPUnturned Launcher.lnk");
                if (File.Exists(startMenuLnk))
                {
                    File.Delete(startMenuLnk);
                }

                // 2. Eliminar clave de registro de agregar o quitar programas
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("PoPUnturnedLauncher", false);
                    }
                }

                // 3. Ejecutar comando en segundo plano para borrar la carpeta del programa después de cerrar
                if (Directory.Exists(targetDir))
                {
                    // Lanza un script de PowerShell oculto para borrar los archivos residuales después de que cerremos
                    string cmd = $"-Command \"Start-Sleep -Seconds 1; if (Test-Path '{targetDir}') {{ Remove-Item -Recurse -Force '{targetDir}' }}\"";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = cmd,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true
                    });
                }

                MessageBox.Show(
                    "PoPUnturned Launcher ha sido desinstalado correctamente de su sistema.",
                    "Desinstalación Completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error al desinstalar el programa:\n{ex.Message}",
                    "Error de Desinstalación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
