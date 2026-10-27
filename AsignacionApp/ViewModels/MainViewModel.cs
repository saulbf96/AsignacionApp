using AsignacionApp.Models;
using AsignacionApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private readonly IArchivoService _archivoService;
        private readonly IDialogService _dialogService;

        // Los servicios llegan por el constructor (inyección de dependencias)
        public MainViewModel(IArchivoService archivoService, IDialogService dialogService)
        {
            _archivoService = archivoService;
            _dialogService = dialogService;

            // Cada vez que cambia la lista: recalcula el resumen y habilita o deshabilita "Limpiar"
            Archivos.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Resumen));
                LimpiarCommand.NotifyCanExecuteChanged();
            };
        }

        // ---------- Estado de la pantalla ----------

        public ObservableCollection<ArchivoTxt> Archivos { get; } = new();

        public string Resumen =>
            $"{Archivos.Count} archivos · " +
            $"{Archivos.Sum(a => a.Bytes) / 1048576.0:N1} MB · " +
            $"{Archivos.Sum(a => a.Registros):N0} registros";

        [ObservableProperty]
        private bool _estaOcupado;   // genera la propiedad EstaOcupado con aviso a la pantalla

        

        [RelayCommand]
        private async Task AgregarCarpetaAsync()
        {
            var carpeta = _dialogService.ElegirCarpeta("Selecciona la carpeta con los TXT");
            if (carpeta is not null)
                await AgregarRutasAsync([carpeta]);
        }

        [RelayCommand]
        private async Task AgregarArchivosAsync()
        {
            var archivos = _dialogService.ElegirArchivos(
                "Selecciona los archivos TXT", "Archivos de texto (*.txt)|*.txt");
            await AgregarRutasAsync(archivos);
        }

        [RelayCommand]
        private void Quitar(IList? seleccionados)
        {
            if (seleccionados is null) return;

            // Copia aparte: no se puede borrar de una lista mientras se recorre
            foreach (var archivo in seleccionados.Cast<ArchivoTxt>().ToList())
                Archivos.Remove(archivo);
        }

        [RelayCommand(CanExecute = nameof(HayArchivos))]
        private void Limpiar() => Archivos.Clear();

        private bool HayArchivos() => Archivos.Count > 0;

        // ---------- Lógica común (botones y arrastrar) ----------

        public async Task AgregarRutasAsync(IEnumerable<string> rutas)
        {
            EstaOcupado = true;
            try
            {
                foreach (var ruta in _archivoService.ObtenerTxt(rutas))
                {
                    bool yaExiste = Archivos.Any(a => a.Ruta.Equals(ruta, StringComparison.OrdinalIgnoreCase));
                    if (yaExiste) continue;

                    Archivos.Add(await _archivoService.LeerInfoAsync(ruta));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogService.MostrarError($"No se pudo leer el archivo:\n{ex.Message}");
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}

