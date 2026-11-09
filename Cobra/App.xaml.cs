using System.Windows;
using Cobra.UI.Views;

namespace Cobra
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var mainWindow = new MainWindow { WindowState = WindowState.Maximized };
            mainWindow.Show();
        }
    }
}
