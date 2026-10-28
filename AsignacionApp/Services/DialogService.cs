using AsignacionApp.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AsignacionApp.Services
{
    public  class DialogService :IDialogService
    {
        public string? ElegirCarpeta(string titulo)
        {
            var dialogo = new OpenFolderDialog { Title = titulo };
            return dialogo.ShowDialog() == true ? dialogo.FolderName : null;
        }

        // Una sola ventana para elegir archivos o una carpeta completa.
        // Windows no trae una ventana para ambos, así que se usa este truco: el nombre
        // trae un texto fijo; si el usuario da "Abrir" sin elegir archivos, se toma la carpeta donde está.
        private const string TextoCarpeta = "(Usar esta carpeta completa)";

        public string[] ElegirArchivosOCarpeta(string titulo, string filtro)
        {
            var dialogo = new OpenFileDialog
            {
                Title = titulo,
                Filter = filtro,
                Multiselect = true,
                FileName = TextoCarpeta,
                CheckFileExists = false,   // el texto fijo no es un archivo real
                ValidateNames = false,
                AddExtension = false
            };

            if (dialogo.ShowDialog() != true)
                return [];

            if (Path.GetFileName(dialogo.FileName).StartsWith(TextoCarpeta))
                return [Path.GetDirectoryName(dialogo.FileName)!];

            return dialogo.FileNames;
        }



        public void MostrarError(string mensaje) =>
            MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        public void MostrarInfo(string mensaje) =>
            MessageBox.Show(mensaje, "Listo", MessageBoxButton.OK, MessageBoxImage.Information);

        // "No" es la opción por defecto: si da Enter sin leer, no se quita nada
        public bool Confirmar(string titulo, string mensaje) =>
            MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                == MessageBoxResult.Yes;

        // Abre la carpeta en el Explorador de Windows
        public void AbrirCarpeta(string ruta) =>
            System.Diagnostics.Process.Start("explorer.exe", $"\"{ruta}\"");

        public string? PedirContrasena(string archivo)
        {
            var ventana = new VentanaContrasena(archivo) { Owner = Application.Current.MainWindow };
            return ventana.ShowDialog() == true ? ventana.Contrasena : null;

        }


      


        public void MostrarReporte(ResultadoConversion resultado, string bitacora)
        {
            // Mismo tamaño que la ventana principal y encima de ella 

            var principal = Application.Current.MainWindow;
            var ventana = new VentanaReporte(resultado, bitacora)
            {
                Owner = principal,
                Width = principal.ActualWidth,
                Height = principal.ActualHeight
            };


            ventana.ShowDialog();
            
        }
    }


}
