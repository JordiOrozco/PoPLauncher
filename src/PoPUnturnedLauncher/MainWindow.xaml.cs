using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoPUnturnedLauncher
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _serverQueryTimer;
        private bool _isQuerying = false;

        private UpdateInfo? _pendingUpdate;

        public MainWindow()
        {
            InitializeComponent();
            
            // Configurar el temporizador para consultar el servidor
            _serverQueryTimer = new DispatcherTimer();
            _serverQueryTimer.Interval = TimeSpan.FromSeconds(15);
            _serverQueryTimer.Tick += ServerQueryTimer_Tick;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Cargar configuración de red inicial e interfaz
            UpdateWorkshopStatus();

            // Inicializar reproductor de música Lo-Fi Chill
            AudioManager.Initialize();
            UpdateMusicButtonUI(AudioManager.IsPlaying);
            
            // Consultar el servidor inmediatamente al cargar
            await RefreshServerStatusAsync();
            _serverQueryTimer.Start();

            // Comprobar actualizaciones en segundo plano
            await CheckUpdatesBackgroundAsync();
        }

        private void MusicToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isPlaying = AudioManager.ToggleMute();
            UpdateMusicButtonUI(isPlaying);
        }

        private void UpdateMusicButtonUI(bool isPlaying)
        {
            if (MusicIconText != null)
            {
                if (isPlaying)
                {
                    MusicIconText.Text = "🎵";
                    MusicIconText.Opacity = 1.0;
                    MusicToggleButton.ToolTip = "Música de ambiente Lo-Fi (Silenciar)";
                }
                else
                {
                    MusicIconText.Text = "🔇";
                    MusicIconText.Opacity = 0.5;
                    MusicToggleButton.ToolTip = "Música de ambiente Lo-Fi (Activar)";
                }
            }
        }

        private async Task CheckUpdatesBackgroundAsync()
        {
            try
            {
                var update = await UpdateManager.CheckForUpdatesAsync();
                if (update != null)
                {
                    _pendingUpdate = update;
                    NewVersionText.Text = $"v{update.LatestVersion}";
                    ReleaseNotesText.Text = string.IsNullOrWhiteSpace(update.ReleaseNotes) 
                        ? "Mejoras de rendimiento y estabilidad." 
                        : update.ReleaseNotes;

                    UpdateModal.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error comprobando actualización: {ex.Message}");
            }
        }

        private void SkipUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateModal.Visibility = Visibility.Collapsed;
        }

        private async void ApplyUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate == null) return;

            try
            {
                UpdateButtonsPanel.Visibility = Visibility.Collapsed;
                UpdateProgressPanel.Visibility = Visibility.Visible;

                bool downloaded = await UpdateManager.DownloadAndApplyUpdateAsync(_pendingUpdate, (progress) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgressBar.Value = progress;
                        UpdatePercentText.Text = $"{Math.Round(progress)}%";
                    });
                });

                if (!downloaded)
                {
                    MessageBox.Show("Ocurrió un error al descargar la actualización. Por favor, intente más tarde.", "Error de Actualización", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateButtonsPanel.Visibility = Visibility.Visible;
                    UpdateProgressPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar actualización: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateButtonsPanel.Visibility = Visibility.Visible;
                UpdateProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void ServerQueryTimer_Tick(object? sender, EventArgs e)
        {
            await RefreshServerStatusAsync();
        }

        private async Task RefreshServerStatusAsync()
        {
            if (_isQuerying) return;
            _isQuerying = true;

            try
            {
                var config = ConfigManager.Current;
                var info = await ServerQuery.QueryServerAsync(config.ServerIp, config.QueryPort);

                if (info.IsOnline)
                {
                    ServerStatusLed.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 102)); // Verde brillante
                    LedGlow.Color = Color.FromRgb(0, 255, 102);
                    ServerStatusText.Text = "ONLINE";
                    ServerPlayersText.Text = $"{info.Players} / {info.MaxPlayers} Jugadores";

                    // Consultar nombres de jugadores activos
                    var players = await ServerQuery.QueryPlayersAsync(config.ServerIp, config.QueryPort);
                    
                    PlayersListBox.ItemsSource = players;

                    if (players.Count > 0)
                    {
                        NoPlayersText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoPlayersText.Text = "No hay jugadores en línea.";
                        NoPlayersText.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    ServerStatusLed.Fill = new SolidColorBrush(Color.FromRgb(255, 85, 85)); // Rojo brillante
                    LedGlow.Color = Color.FromRgb(255, 85, 85);
                    ServerStatusText.Text = "OFFLINE";
                    ServerPlayersText.Text = "0 / 0 Jugadores";

                    // Limpiar lista e indicar offline
                    PlayersListBox.ItemsSource = null;
                    NoPlayersText.Text = "Servidor fuera de servicio.";
                    NoPlayersText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al actualizar interfaz de servidor: {ex.Message}");
            }
            finally
            {
                _isQuerying = false;
            }
        }

        private void UpdateWorkshopStatus()
        {
            try
            {
                string workshopPath = SteamHelper.GetWorkshopDirectory();
                long totalSize = 0;

                if (Directory.Exists(workshopPath))
                {
                    var dirInfo = new DirectoryInfo(workshopPath);
                    totalSize = GetDirectorySize(dirInfo);
                }

                if (totalSize > 0)
                {
                    WorkshopStatusText.Text = $"{SteamHelper.FormatBytes(totalSize)} en caché";
                    WorkshopStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 204, 0)); // Amarillo
                }
                else
                {
                    WorkshopStatusText.Text = "Limpio";
                    WorkshopStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 102)); // Verde
                }
            }
            catch
            {
                WorkshopStatusText.Text = "Desconocido";
                WorkshopStatusText.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            }
        }

        private long GetDirectorySize(DirectoryInfo d)
        {
            long size = 0;
            try
            {
                foreach (FileInfo fi in d.GetFiles())
                {
                    size += fi.Length;
                }
                foreach (DirectoryInfo di in d.GetDirectories())
                {
                    size += GetDirectorySize(di);
                }
            }
            catch { }
            return size;
        }

        // EVENTOS DE LA VENTANA
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // CONTROL DE OPCIONES / MODAL
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            LoadOptionsData();
            OptionsModal.Visibility = Visibility.Visible;
        }

        private void CloseOptions_Click(object sender, RoutedEventArgs e)
        {
            OptionsModal.Visibility = Visibility.Collapsed;
        }

        private void LoadOptionsData()
        {
            string unturnedDir = SteamHelper.GetUnturnedDirectory();
            string workshopDir = SteamHelper.GetWorkshopDirectory();

            UnturnedPathTextBox.Text = unturnedDir;
            WorkshopPathTextBox.Text = workshopDir;

            float currentVolume = PreferencesManager.GetVehicleEngineVolume(unturnedDir);
            VehicleVolumeSlider.Value = Math.Round(currentVolume * 100);
            VehicleVolumeText.Text = $"{VehicleVolumeSlider.Value}%";

            MusicVolumeSlider.Value = ConfigManager.Current.MusicVolume;
            MusicVolumeText.Text = $"{Math.Round(MusicVolumeSlider.Value)}%";
        }

        private void VehicleVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VehicleVolumeText != null)
            {
                VehicleVolumeText.Text = $"{Math.Round(e.NewValue)}%";
            }
        }

        private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MusicVolumeText != null)
            {
                MusicVolumeText.Text = $"{Math.Round(e.NewValue)}%";
                AudioManager.SetVolume(e.NewValue);
            }
        }

        private void BrowseUnturned_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Seleccionar carpeta de Unturned",
                    InitialDirectory = Directory.Exists(UnturnedPathTextBox.Text) ? UnturnedPathTextBox.Text : string.Empty
                };

                if (dialog.ShowDialog() == true)
                {
                    UnturnedPathTextBox.Text = dialog.FolderName;
                    // Actualizar volumen slider según las preferencias de la nueva carpeta
                    float vol = PreferencesManager.GetVehicleEngineVolume(dialog.FolderName);
                    VehicleVolumeSlider.Value = Math.Round(vol * 100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error seleccionando carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BrowseWorkshop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Seleccionar carpeta de Mods de Workshop (304930)",
                    InitialDirectory = Directory.Exists(WorkshopPathTextBox.Text) ? WorkshopPathTextBox.Text : string.Empty
                };

                if (dialog.ShowDialog() == true)
                {
                    WorkshopPathTextBox.Text = dialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error seleccionando carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetOptions_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigManager.Current;
            config.CustomUnturnedPath = "";
            config.CustomWorkshopPath = "";
            config.MusicVolume = 2.0;
            ConfigManager.Save(config);

            AudioManager.SetVolume(2.0);
            LoadOptionsData();
            MessageBox.Show("Se ha restablecido la autodetección de rutas por defecto y el volumen de música.", "Opciones Restablecidas", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ConfigManager.Current;
                config.CustomUnturnedPath = UnturnedPathTextBox.Text.Trim();
                config.CustomWorkshopPath = WorkshopPathTextBox.Text.Trim();
                config.MusicVolume = MusicVolumeSlider.Value;
                ConfigManager.Save(config);

                AudioManager.SetVolume(config.MusicVolume);

                // Guardar volumen de vehículos en Preferences.json de Unturned
                string unturnedFolder = SteamHelper.GetUnturnedDirectory();
                float volumeMult = (float)(VehicleVolumeSlider.Value / 100.0);
                bool prefSaved = PreferencesManager.SetVehicleEngineVolume(unturnedFolder, volumeMult);

                UpdateWorkshopStatus();
                OptionsModal.Visibility = Visibility.Collapsed;

                if (!prefSaved && !Directory.Exists(unturnedFolder))
                {
                    MessageBox.Show(
                        "Las rutas de configuración se guardaron, pero no se encontró el archivo Preferences.json de Unturned en la carpeta especificada.\n" +
                        "Asegúrese de haber seleccionado la carpeta principal de Unturned.",
                        "Advertencia de Ruta",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    MessageBox.Show("¡Configuración y volumen de vehículos guardados correctamente!", "Opciones Guardadas", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando opciones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ACCIONES PRINCIPALES
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ConfigManager.Current;
                
                // Formato de URI de Steam para conectar directamente
                // steam://run/304930//+connect 102.129.137.140:27015
                string steamUri = $"steam://run/{config.SteamAppId}//+connect {config.ServerIp}:{config.ServerPort}";
                
                var psi = new ProcessStartInfo
                {
                    FileName = steamUri,
                    UseShellExecute = true
                };
                
                Process.Start(psi);
                
                // Minimizar el launcher tras abrir el juego
                WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo iniciar Unturned mediante Steam. Verifique que Steam está instalado.\nDetalles: {ex.Message}", 
                    "Error de Lanzamiento", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }

        private void DeleteMods_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = MessageBox.Show(
                "¿Está seguro de que desea eliminar los mods descargados de la Workshop de Unturned?\n\n" +
                "Esto borrará únicamente la caché descargada para el juego con ID 304930. " +
                "Steam volverá a descargar automáticamente los mods requeridos al conectarse al servidor de nuevo.",
                "Confirmar Limpieza de Mods",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirmResult == MessageBoxResult.Yes)
            {
                // Cambiar cursor a espera
                Mouse.OverrideCursor = Cursors.Wait;
                
                try
                {
                    var result = SteamHelper.DeleteUnturnedWorkshopMods();

                    if (result.Success)
                    {
                        string sizeFreed = SteamHelper.FormatBytes(result.BytesFreed);
                        MessageBox.Show(
                            $"¡Limpieza completada con éxito!\n\n" +
                            $"- Espacio liberado: {sizeFreed}\n" +
                            $"- Carpetas de mods eliminadas: {result.FoldersDeleted}\n\n" +
                            "Al volver a entrar al servidor, Steam descargará los mods necesarios limpios de errores.",
                            "Limpieza Completada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Ocurrió un problema al limpiar los mods:\n{result.ErrorMessage}",
                            "Error de Limpieza",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error inesperado durante la limpieza: {ex.Message}",
                        "Error Crítico",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                finally
                {
                    // Restaurar cursor y estado visual
                    Mouse.OverrideCursor = null;
                    UpdateWorkshopStatus();
                }
            }
        }

        // SOCIAL CLICKS
        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.DiscordUrl);
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.WebsiteUrl);
        }

        private void Youtube_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.YoutubeUrl);
        }

        private void Tiktok_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.TiktokUrl);
        }

        private void Twitter_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.TwitterUrl);
        }

        private void Instagram_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ConfigManager.Current.InstagramUrl);
        }

        private void OpenUrl(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error abriendo enlace social: {ex.Message}");
            }
        }
    }
}