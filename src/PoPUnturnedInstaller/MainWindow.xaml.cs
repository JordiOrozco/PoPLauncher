using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PoPUnturnedInstaller
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            // Obtener configuración del UI
            bool createDesktopShortcut = DesktopShortcutCheckbox.IsChecked ?? false;
            bool createStartMenuShortcut = StartMenuShortcutCheckbox.IsChecked ?? false;

            // Cambiar vista de UI a progreso
            ConfigPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            InstallButton.IsEnabled = false;

            try
            {
                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoPUnturnedLauncher");

                await Task.Run(() => PerformInstallation(targetDir, createDesktopShortcut, createStartMenuShortcut));

                // Lanzar el Launcher instalado y cerrar
                string launcherExe = Path.Combine(targetDir, "PoPUnturnedLauncher.exe");
                if (File.Exists(launcherExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = launcherExe,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error crítico durante la instalación:\n{ex.Message}",
                    "Error de Instalación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // Revertir UI
                ConfigPanel.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
                InstallButton.IsEnabled = true;
            }
        }

        private void PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu)
        {
            UpdateStatus("Preparando directorio de instalación...", 5);
            
            // 1. Crear directorio
            if (Directory.Exists(targetDir))
            {
                try
                {
                    // Limpieza previa si existe
                    Directory.Delete(targetDir, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error limpiando directorio previo: {ex.Message}");
                }
            }
            Directory.CreateDirectory(targetDir);

            // 2. Extraer archivos del Launcher desde el Zip incrustado
            UpdateStatus("Extrayendo archivos del Launcher...", 15);
            var assembly = Assembly.GetExecutingAssembly();
            
            // Buscar y extraer recurso
            using (Stream? resourceStream = assembly.GetManifestResourceStream("PoPUnturnedInstaller.LauncherFiles.zip"))
            {
                if (resourceStream == null)
                {
                    throw new FileNotFoundException("No se encontró el recurso del Launcher incrustado (LauncherFiles.zip).");
                }

                using (var archive = new ZipArchive(resourceStream))
                {
                    int totalEntries = archive.Entries.Count;
                    int extractedCount = 0;

                    foreach (var entry in archive.Entries)
                    {
                        string destPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                        
                        // Validar Path Traversal
                        if (!destPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; 
                        }

                        // Asegurar directorio destino
                        string? dirPath = Path.GetDirectoryName(destPath);
                        if (dirPath != null)
                        {
                            Directory.CreateDirectory(dirPath);
                        }

                        if (!entry.FullName.EndsWith("/"))
                        {
                            entry.ExtractToFile(destPath, true);
                        }

                        extractedCount++;
                        if (totalEntries > 0)
                        {
                            double progress = 15 + ((double)extractedCount / totalEntries * 65); // Mapeado de 15% a 80%
                            UpdateStatus($"Copiando: {entry.Name}", (int)progress);
                        }
                    }
                }
            }

            // 3. Copiar el instalador a la carpeta destino para servir como uninstaller
            UpdateStatus("Configurando desinstalador...", 85);
            string installerPath = Environment.ProcessPath ?? "";
            string destInstaller = Path.Combine(targetDir, "PoPUnturnedInstaller.exe");
            try
            {
                File.Copy(installerPath, destInstaller, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copiando desinstalador: {ex.Message}");
            }

            // 4. Crear accesos directos
            UpdateStatus("Creando accesos directos...", 90);
            string targetExe = Path.Combine(targetDir, "PoPUnturnedLauncher.exe");
            string iconPath = Path.Combine(targetDir, "Assets", "icon.ico");

            if (createDesktop)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnkPath = Path.Combine(desktopPath, "PoPUnturned Launcher.lnk");
                CreateShortcut(lnkPath, targetExe, "Launcher oficial de PoPUnturned", iconPath);
            }

            if (createStartMenu)
            {
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string lnkPath = Path.Combine(startMenuPath, "PoPUnturned Launcher.lnk");
                CreateShortcut(lnkPath, targetExe, "Launcher oficial de PoPUnturned", iconPath);
            }

            // 5. Escribir Registro
            UpdateStatus("Registrando aplicación...", 95);
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PoPUnturnedLauncher"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "PoPUnturned Launcher");
                        key.SetValue("DisplayVersion", "1.0.0");
                        key.SetValue("Publisher", "PoPUnturned Community");
                        key.SetValue("DisplayIcon", iconPath);
                        key.SetValue("InstallLocation", targetDir);
                        key.SetValue("UninstallString", $"\"{destInstaller}\" /uninstall");
                        key.SetValue("NoModify", 1);
                        key.SetValue("NoRepair", 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registrando desinstalador: {ex.Message}");
            }

            UpdateStatus("¡Todo listo! Iniciando launcher...", 100);
            System.Threading.Thread.Sleep(500); // Pequeña pausa para efecto visual
        }

        private void UpdateStatus(string message, int percent)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                InstallProgressBar.Value = percent;
            });
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string description, string iconPath)
        {
            try
            {
                Type? t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) return;
                dynamic shell = Activator.CreateInstance(t)!;
                try
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                    shortcut.Description = description;
                    if (File.Exists(iconPath))
                    {
                        shortcut.IconLocation = iconPath + ",0";
                    }
                    shortcut.Save();
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creando acceso directo: {ex.Message}");
            }
        }
    }
}