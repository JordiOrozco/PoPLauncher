using System;
using System.Windows;

namespace PoPUnturnedInstaller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Comprobar argumentos de la línea de comandos
            if (e.Args.Length > 0 && (e.Args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase) || e.Args[0].Equals("-uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                UninstallHelper.Uninstall();
                Environment.Exit(0);
                return;
            }

            base.OnStartup(e);
        }
    }
}
