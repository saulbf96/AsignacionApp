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
        private readonly IConversionService _conversionService;
        private readonly IZipService _zipService;

        // Los servicios llegan por el constructor (inyección de dependencias)
        public MainViewModel(IArchivoService archivoService, IDialogService dialogService,
            IConversionService conversionService, IZipService zipService)
        {
            _archivoService = archivoService;
            _dialogService = dialogService;
            _conversionService = conversionService;
            _zipService = zipService;

            // Cada vez que cambia la lista: recalcula el resumen y habilita o deshabilita Limpiar y Convertir
            Archivos.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Resumen));
                LimpiarCommand.NotifyCanExecuteChanged();
                ConvertirCommand.NotifyCanExecuteChanged();
            };
        }

        //  Estado de la pantalla 

        public ObservableCollection<ArchivoTxt> Archivos { get; } = new();

        public string Resumen =>
            $"Total: {Archivos.Count} archivos · " +
            $"{Archivos.Sum(a => a.Bytes) / 1048576.0:N1} MB · " +
            $"{Archivos.Sum(a => a.Registros):N0} registros";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertirCommand))]
        [NotifyCanExecuteChangedFor(nameof(AgregarCommand))]
        [NotifyCanExecuteChangedFor(nameof(QuitarCommand))]
        [NotifyCanExecuteChangedFor(nameof(LimpiarCommand))]
        private bool _estaOcupado;   // genera la propiedad EstaOcupado con aviso a la pantalla

        //  Opciones y salida 

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertirCommand))]
        private bool _generarExcel = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertirCommand))]
        private bool _generarCsv;

        [ObservableProperty]
        private bool _agregarColumnaArchivo;

        [ObservableProperty]
        private string _carpetaSalida = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NombreFinal))]
        private string _nombreArchivo = "ASIGNACION INBURSA GOTEO";

        // Al nombre se le agrega la fecha del día al final: "ASIGNACION INBURSA GOTEO 28102026"
        public string NombreFinal => $"{NombreArchivo.Trim()} {DateTime.Today:ddMMyyyy}";

        [ObservableProperty]
        private string _nombreHoja = "ASIGNACION";

        [ObservableProperty]
        private int _progreso;

        [ObservableProperty]
        private string _log = "";   // la bitácora se ve en la ventana de reporte

        // Resultado de la última conversión, para volver a abrir el reporte
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(VerReporteCommand))]
        private ResultadoConversion? _ultimoResultado;

        [RelayCommand(CanExecute = nameof(HayReporte))]
        private void VerReporte() => _dialogService.MostrarReporte(UltimoResultado!, Log);

        private bool HayReporte() => UltimoResultado is not null;

        [RelayCommand]
        private void ElegirSalida()
        {
            var carpeta = _dialogService.ElegirCarpeta("¿Dónde guardo el archivo?");
            if (carpeta is not null)
                CarpetaSalida = carpeta;
        }

        [RelayCommand]
        private void AbrirCarpeta()
        {
            if (Directory.Exists(CarpetaSalida))
                _dialogService.AbrirCarpeta(CarpetaSalida);
        }

        [RelayCommand(CanExecute = nameof(PuedeConvertir))]
        private async Task ConvertirAsync()
        {
            // Revisa los datos de salida antes de empezar
            string? problema = ValidarSalida();
            if (problema is not null)
            {
                _dialogService.MostrarError(problema);
                return;
            }

            EstaOcupado = true;
            Progreso = 0;
            Log = "";
            UltimoResultado = null;

            // Progress<T> regresa los avisos al hilo de la pantalla
            var progreso = new Progress<int>(p => Progreso = p);
            var log = new Progress<string>(Escribir);

            try
            {
                var resultado = await _conversionService.ConvertirAsync(new OpcionesConversion
                {
                    Archivos = Archivos.Select(a => a.Ruta).ToList(),
                    CarpetaSalida = CarpetaSalida,
                    NombreArchivo = NombreFinal,
                    NombreHoja = NombreHoja,
                    GenerarExcel = GenerarExcel,
                    GenerarCsv = GenerarCsv,
                    AgregarColumnaArchivo = AgregarColumnaArchivo
                }, progreso, log);

                Escribir("");
                Escribir($"Registros leídos:   {resultado.RegistrosLeidos:N0}");
                Escribir($"Registros escritos: {resultado.RegistrosEscritos:N0}");
                Escribir($"Valores corregidos: {resultado.ValoresCorregidos:N0}");
                foreach (var archivo in resultado.ArchivosGenerados)
                    Escribir("Generado: " + archivo);

                // Resumen por tipo, de más a menos, con ejemplos para buscarlos en el TXT
                Escribir("");
                Escribir($"Advertencias: {resultado.TotalAdvertencias:N0} (datos que ya vienen así en los TXT; se dejan igual)");
                foreach (var (tipo, grupo) in resultado.Advertencias.OrderByDescending(a => a.Value.Cantidad))
                {
                    Escribir($"  {grupo.Cantidad,7:N0}  {tipo}");
                    foreach (var ejemplo in grupo.Ejemplos)
                        Escribir($"             ej. {ejemplo}");
                }

                if (resultado.ArchivosOmitidos > 0)
                    Escribir($"Archivos omitidos:  {resultado.ArchivosOmitidos} (no tienen el formato de Inbursa, ver arriba)");

                // Una sola ventana con el resultado y la bitácora (reemplaza el mensaje "Se unieron...")
                UltimoResultado = resultado;
                _dialogService.MostrarReporte(resultado, Log);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                _dialogService.MostrarError($"No se encontró un archivo o carpeta:\n{ex.Message}");
            }
            catch (IOException ex)
            {
                // Pasa, por ejemplo, si el Excel de salida está abierto
                _dialogService.MostrarError($"No se pudo escribir el archivo (¿está abierto en Excel?):\n{ex.Message}");
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
            {
                _dialogService.MostrarError(ex.Message);
            }
            catch (Exception ex)
            {
                // Cualquier otro error: se avisa en lugar de cerrar la aplicación
                Escribir("ERROR: " + ex);
                _dialogService.MostrarError($"Ocurrió un error inesperado:\n{ex.Message}");
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private bool PuedeConvertir() => Archivos.Count > 0 && !EstaOcupado && (GenerarExcel || GenerarCsv);

        // Regresa null si la salida está bien, o el mensaje para el usuario
        private string? ValidarSalida()
        {
            if (string.IsNullOrWhiteSpace(CarpetaSalida) || !Path.IsPathFullyQualified(CarpetaSalida))
                return "Elige una carpeta de salida válida (botón \"...\").";

            // La raíz ("C:\") sí lleva ":"; cada carpeta después de ella no puede llevar : * ? " < > |
            string raiz = Path.GetPathRoot(CarpetaSalida)!;
            var carpetas = CarpetaSalida[raiz.Length..].Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (carpetas.Any(c => c.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                return "La carpeta de salida no puede llevar  : * ? \" < > |\nElígela con el botón \"...\".";

            if (!Directory.Exists(raiz))
                return $"No existe la unidad {raiz}";

            if (string.IsNullOrWhiteSpace(NombreArchivo))
                return "Escribe el nombre del archivo.";

            if (NombreArchivo.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return "El nombre del archivo no puede llevar  \\ / : * ? \" < > |";

            // Un TXT pudo moverse o borrarse después de agregarlo a la lista
            var faltante = Archivos.FirstOrDefault(a => !File.Exists(a.Ruta));
            if (faltante is not null)
                return $"El archivo ya no existe o se movió:\n{faltante.Ruta}\nQuítalo de la lista y vuelve a agregarlo.";

            // Si el archivo de salida ya existe y está abierto en Excel, se avisa antes de procesar
            var extensiones = new[] { (GenerarExcel, ".xlsx"), (GenerarCsv, ".csv") };
            foreach (var (generar, extension) in extensiones)
            {
                string ruta = Path.Combine(CarpetaSalida, NombreFinal + extension);
                if (generar && EstaBloqueado(ruta))
                    return $"Cierra el archivo, está abierto en otro programa:\n{ruta}";
            }

            return null;
        }

        private static bool EstaBloqueado(string ruta)
        {
            if (!File.Exists(ruta)) return false;
            try
            {
                using var _ = File.Open(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return true; }
        }

        private void Escribir(string linea) => Log += linea + Environment.NewLine;



        // Un solo botón: TXT sueltos, ZIP, o una carpeta completa (con TXT y/o ZIP dentro)
        // Mientras convierte o agrega, la lista no se toca (botones desactivados)
        private bool NoEstaOcupado() => !EstaOcupado;

        [RelayCommand(CanExecute = nameof(NoEstaOcupado))]
        private async Task AgregarAsync()
        {
            var rutas = _dialogService.ElegirArchivosOCarpeta(
                "Elige TXT o ZIP, o entra a una carpeta y da Abrir para agregarla completa",
                "Archivos TXT o ZIP (*.txt;*.zip)|*.txt;*.zip");
            await AgregarRutasAsync(rutas);
        }

        [RelayCommand(CanExecute = nameof(PuedeQuitar))]
        private void Quitar(IList? seleccionados)
        {
            if (seleccionados is null) return;

            // Copia aparte: no se puede borrar de una lista mientras se recorre
            var aQuitar = seleccionados.Cast<ArchivoTxt>().ToList();

            if (!_dialogService.Confirmar("Quitar seleccionados",
                    $"¿Quitar {Describir(aQuitar)} de la lista?\n\n" +
                    $"Quedarían {Archivos.Count - aQuitar.Count} archivo(s).\n" +
                    "Los archivos originales no se eliminan del equipo; solo se retiran de la lista."))
                return;

            foreach (var archivo in aQuitar)
                Archivos.Remove(archivo);
        }

        [RelayCommand(CanExecute = nameof(HayArchivos))]
        private void Limpiar()
        {
            if (!_dialogService.Confirmar("Limpiar lista",
                    $"¿Quitar TODOS los archivos de la lista?\n\nSon {Describir(Archivos)}.\n" +
                    "Los archivos originales no se eliminan del equipo; solo se retiran de la lista."))
                return;

            Archivos.Clear();
        }

        // "3 archivos (1,234 registros)"
        private static string Describir(IEnumerable<ArchivoTxt> archivos) =>
            $"{archivos.Count()} archivo(s) ({archivos.Sum(a => a.Registros):N0} registros)";

        private bool HayArchivos() => Archivos.Count > 0 && !EstaOcupado;

        // Solo se activa si hay archivos seleccionados en la lista
        private bool PuedeQuitar(IList? seleccionados) => !EstaOcupado && seleccionados is { Count: > 0 };

        // ---------- Lógica común (botones y arrastrar) ----------

        public async Task AgregarRutasAsync(IEnumerable<string> rutas)
        {
            // Si se arrastran archivos mientras convierte, se ignoran
            if (EstaOcupado) return;

            var lista = rutas.ToList();   // se recorre dos veces: TXT y ZIP
            var omitidos = new List<string>();   // archivos dentro de los ZIP que no son TXT
            EstaOcupado = true;
            try
            {
                foreach (var ruta in _archivoService.ObtenerTxt(lista))
                    await AgregarAsync(ruta, zip: null);

                // Si un ZIP falla se avisa y se sigue con los demás
                foreach (var zip in _archivoService.ObtenerZip(lista))
                {
                    try
                    {
                        var contenido = await ExtraerZipAsync(zip);
                        if (contenido is null) continue;   // canceló la contraseña

                        foreach (var ruta in contenido.Txt)
                            await AgregarAsync(ruta, zip);

                        if (contenido.Ignorados.Count > 0)
                            omitidos.Add($"{Path.GetFileName(zip)}: {string.Join(", ", contenido.Ignorados)}");

                        if (contenido.Txt.Count == 0)
                            _dialogService.MostrarError($"El ZIP no trae archivos TXT:\n{zip}");
                    }
                    catch (InvalidDataException ex)
                    {
                        _dialogService.MostrarError(ex.Message);
                    }
                }

                // Un solo aviso al final con todo lo que no se agregó (máximo 15 líneas)
                if (omitidos.Count > 0)
                    _dialogService.MostrarInfo(
                        "Se agregaron solo los TXT. Estos archivos dentro de los ZIP no son TXT y se dejaron fuera:\n\n" +
                        string.Join("\n", omitidos.Take(15)) +
                        (omitidos.Count > 15 ? $"\n... y {omitidos.Count - 15} ZIP más" : ""));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                _dialogService.MostrarError($"No se pudo leer el archivo:\n{ex.Message}");
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task AgregarAsync(string ruta, string? zip)
        {
            bool yaExiste = Archivos.Any(a => a.Ruta.Equals(ruta, StringComparison.OrdinalIgnoreCase));
            if (!yaExiste)
                Archivos.Add(await _archivoService.LeerInfoAsync(ruta, zip));
        }

        // Pide la contraseña si el ZIP la tiene; si es incorrecta la vuelve a pedir.
        // Regresa null si el usuario cancela.
        private async Task<ResultadoZip?> ExtraerZipAsync(string zip)
        {
            string nombre = Path.GetFileName(zip);
            string? contrasena = null;

            if (_zipService.TieneContrasena(zip))
            {
                contrasena = _dialogService.PedirContrasena(nombre);
                if (contrasena is null) return null;
            }

            while (true)
            {
                var contenido = await _zipService.ExtraerTxtAsync(zip, contrasena);
                if (contenido is not null) return contenido;

                _dialogService.MostrarError($"La contraseña de \"{nombre}\" no es correcta.");
                contrasena = _dialogService.PedirContrasena(nombre);
                if (contrasena is null) return null;
            }
        }
    }
}

