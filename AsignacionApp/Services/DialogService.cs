using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        public string[] ElegirArchivos(string titulo, string filtro)
        {
            var dialogo = new OpenFileDialog { Title = titulo, Filter = filtro, Multiselect = true };
            return dialogo.ShowDialog() == true ? dialogo.FileNames : [];
        }

        public void MostrarError(string mensaje) =>
            MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
