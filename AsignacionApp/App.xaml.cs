using System.Windows;
using AsignacionApp.Services;
using AsignacionApp.ViewModels;

namespace AsignacionApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Composición: aquí se decide qué implementación usa cada interfaz
            var viewModel = new MainViewModel(new ArchivoService(), new DialogService());

            new MainWindow(viewModel).Show();
        }
    }
}
