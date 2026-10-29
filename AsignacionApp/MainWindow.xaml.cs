using System.Windows;
using System.Windows.Controls;
using AsignacionApp.ViewModels;

namespace AsignacionApp
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = _viewModel = viewModel;   // conecta la pantalla con el ViewModel
            AjustarAPantalla();
        }

        // En laptops con pantalla chica o con escala de Windows al 125% / 150%,
        // la ventana no debe salirse de la pantalla (se perdería el botón Convertir)
        private void AjustarAPantalla()
        {
            var area = SystemParameters.WorkArea;   // pantalla sin la barra de tareas
            MinHeight = Math.Min(MinHeight, area.Height);
            MinWidth = Math.Min(MinWidth, area.Width);
            Height = Math.Min(Height, area.Height);
            Width = Math.Min(Width, area.Width);
        }

        // SelectedItems es siempre la misma lista, así que WPF no sabe que cambió:
        // se le avisa al botón "Quitar seleccionados" para que se active o desactive
        private void ListaArchivos_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            _viewModel.QuitarCommand.NotifyCanExecuteChanged();

        // Arrastrar y soltar: solo pasa las rutas al ViewModel
        private void ListaArchivos_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void ListaArchivos_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] rutas)
                await _viewModel.AgregarRutasAsync(rutas);
        }
    }
}
