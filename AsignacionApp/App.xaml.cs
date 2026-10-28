using System.Windows;
using AsignacionApp.Services;
using AsignacionApp.ViewModels;

namespace AsignacionApp
{
    public partial class App : Application
    {
        private readonly ZipService _zipService = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Red de seguridad: si algo falla y nadie lo atrapó, se avisa en lugar de cerrar la app
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show($"Ocurrió un error inesperado:\n{args.Exception.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            // Composición: aquí se decide qué implementación usa cada interfaz
            var limpiador = Limpiador.DesdeArchivo(System.IO.Path.Combine(AppContext.BaseDirectory, "reemplazos.txt"));
            var viewModel = new MainViewModel(new ArchivoService(), new DialogService(), new ConversionService(limpiador), _zipService);

            // Por si la última vez la app se cerró de golpe y quedaron TXT extraídos
            _zipService.BorrarTemporales();

            new MainWindow(viewModel).Show();
        }

        // Al cerrar se borran los TXT que se sacaron de los ZIP
        protected override void OnExit(ExitEventArgs e)
        {
            _zipService.BorrarTemporales();
            base.OnExit(e);
        }
    }
}
