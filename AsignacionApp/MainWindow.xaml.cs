using System.Windows;
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
        }

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
