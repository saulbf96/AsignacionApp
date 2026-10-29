using AsignacionApp.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace AsignacionApp
{
    // Ventana con el resultado de la conversión: números grandes, archivos generados y la bitácora completa.
    // Solo muestra información; los datos llegan ya calculados desde el ViewModel.
    public partial class VentanaReporte : Window
    {
        private readonly string? _archivo;   // el primer archivo generado (Excel si se generó)

        public VentanaReporte(ResultadoConversion resultado, string bitacora)
        {
            InitializeComponent();

            bool hayOmitidos = resultado.ArchivosOmitidos > 0;
            TxtTitulo.Text = hayOmitidos
                ? $"⚠ Se unieron {resultado.RegistrosEscritos:N0} registros, pero {resultado.ArchivosOmitidos} archivo(s) NO se unieron (ver bitácora)"
                : $"✔ Se unieron {resultado.RegistrosEscritos:N0} registros";
            TxtTitulo.Foreground = new SolidColorBrush(hayOmitidos
                ? Color.FromRgb(0xC0, 0x6A, 0x00)
                : Color.FromRgb(0x2E, 0x7D, 0x32));

            TxtLeidos.Text = resultado.RegistrosLeidos.ToString("N0");
            TxtEscritos.Text = resultado.RegistrosEscritos.ToString("N0");
            TxtCorregidos.Text = resultado.ValoresCorregidos.ToString("N0");
            TxtAdvertencias.Text = resultado.TotalAdvertencias.ToString("N0");

            TxtGenerados.Text = string.Join("\n", resultado.ArchivosGenerados.Select(a => "Generado: " + a));
            TxtBitacora.Text = bitacora;

            _archivo = resultado.ArchivosGenerados.FirstOrDefault();
            BtnAbrirArchivo.Content = _archivo?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true
                ? "Abrir CSV" : "Abrir Excel";
            BtnAbrirArchivo.IsEnabled = _archivo is not null;
        }

        private void AbrirArchivo_Click(object sender, RoutedEventArgs e)
        {
            if (_archivo is not null)
                Abrir(_archivo);
        }

        private void AbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            if (_archivo is not null)
                Abrir(Path.GetDirectoryName(_archivo)!);
        }

        private void Copiar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtBitacora.Text);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Otro programa tiene ocupado el portapapeles
                MessageBox.Show("No se pudo copiar, el portapapeles está ocupado. Intenta de nuevo.", "Copiar",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Abre el archivo o la carpeta con el programa que tenga Windows (Excel, Explorador)
        private static void Abrir(string ruta)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"No se pudo abrir:\n{ruta}\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
