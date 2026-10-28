using AsignacionApp.Models;
using System.Globalization;
using System.IO;
using System.Text;

namespace AsignacionApp.Services
{
    public class ConversionService : IConversionService
    {
        private readonly Limpiador _limpiador;

        public ConversionService(Limpiador limpiador)
        {
            _limpiador = limpiador;
        }

        public Task<ResultadoConversion> ConvertirAsync(
            OpcionesConversion opciones, IProgress<int>? progreso = null, IProgress<string>? log = null) =>
            Task.Run(() => Convertir(opciones, progreso, log));

        private ResultadoConversion Convertir(OpcionesConversion op, IProgress<int>? progreso, IProgress<string>? log)
        {
            var resultado = new ResultadoConversion();
            var filas = new List<object?[]>();

            // ---------- 0. Decidir el formato ----------
            // No está amarrado a 54 columnas: manda el encabezado que comparten más archivos
            var (encabezado, cuantos) = EncabezadoMasComun(op.Archivos)
                ?? throw new InvalidOperationException("Ningún archivo tiene encabezado (columnas separadas por |).");
            log?.Report($"Formato: {encabezado.Length} columnas ({encabezado[0]}|{encabezado[1]}|...), lo traen {cuantos} de {op.Archivos.Count} archivos");

            //  Leer y limpiar cada TXT
            for (int i = 0; i < op.Archivos.Count; i++)
            {
                string ruta = op.Archivos[i];
                string nombre = Path.GetFileName(ruta);
                int numLinea = 0, registrosArchivo = 0;
                bool esPrimeraLinea = true, omitido = false;

                foreach (var linea in File.ReadLines(ruta, Encoding.Latin1))
                {
                    numLinea++;
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var campos = linea.Split('|');   // solo "|" separa columnas

                    // Solo se unen los archivos con el mismo encabezado que la mayoría.
                    // Un TXT ajeno (ej. "leeme.txt" dentro de un ZIP) o de otro formato no se mezcla.
                    if (esPrimeraLinea)
                    {
                        esPrimeraLinea = false;
                        var encabezadoArchivo = PartirEncabezado(linea);
                        string? motivo =
                            !PareceEncabezado(linea, encabezadoArchivo) ? "su primera línea no es un encabezado (columnas separadas por |)" :
                            !encabezado.SequenceEqual(encabezadoArchivo) ? "su encabezado es distinto al de los demás archivos" :
                            null;

                        if (motivo is not null)
                        {
                            resultado.Advertir("Archivo NO unido", $"{nombre}: {motivo}");
                            log?.Report($"{nombre}: OMITIDO, {motivo}");
                            resultado.ArchivosOmitidos++;
                            omitido = true;
                            break;
                        }

                        continue;
                    }

                    resultado.RegistrosLeidos++;
                    registrosArchivo++;
                    campos = AjustarColumnas(campos, encabezado.Length, nombre, numLinea, resultado);

                    var fila = new object?[encabezado.Length + (op.AgregarColumnaArchivo ? 1 : 0)];
                    for (int c = 0; c < encabezado.Length; c++)
                        fila[c] = ConvertirValor(campos[c], encabezado[c], nombre, numLinea, resultado);

                    if (op.AgregarColumnaArchivo)
                        fila[^1] = nombre;

                    filas.Add(fila);
                }

                if (!omitido)
                    log?.Report($"{nombre}: {registrosArchivo:N0} registros");
                progreso?.Report((i + 1) * 70 / op.Archivos.Count);
            }



            if (filas.Count == 0)
                throw new InvalidOperationException("Los archivos no traen registros (solo encabezado o están vacíos).");

            if (op.GenerarExcel && filas.Count > EscritorExcel.MaxRegistros)
                throw new InvalidOperationException(
                    $"Excel solo admite {EscritorExcel.MaxRegistros:N0} registros por hoja y hay {filas.Count:N0}.\n" +
                    "Desmarca Excel y genera solo CSV, o convierte menos archivos a la vez.");

            if (op.AgregarColumnaArchivo)
                encabezado = [.. encabezado, "ArchivoOrigen"];

            resultado.RegistrosEscritos = filas.Count;

            // Escribir las salidas
            Directory.CreateDirectory(op.CarpetaSalida);

            if (op.GenerarExcel)
            {
                log?.Report("Generando Excel...");
                string rutaExcel = Path.Combine(op.CarpetaSalida, op.NombreArchivo + ".xlsx");
                EscribirSinDejarBasura(rutaExcel, () => EscritorExcel.Escribir(rutaExcel, op.NombreHoja, encabezado, filas));
                resultado.ArchivosGenerados.Add(rutaExcel);
            }
            progreso?.Report(90);

            if (op.GenerarCsv)
            {
                log?.Report("Generando CSV...");
                string rutaCsv = Path.Combine(op.CarpetaSalida, op.NombreArchivo + ".csv");
                EscribirSinDejarBasura(rutaCsv, () => EscritorCsv.Escribir(rutaCsv, encabezado, filas));
                resultado.ArchivosGenerados.Add(rutaCsv);
            }
            progreso?.Report(100);

            return resultado;
        }

        // Lee la primera línea de cada archivo y regresa el encabezado que más se repite
        // (si hay empate, el del archivo que va primero en la lista). null si ninguno parece encabezado.
        private static (string[] Encabezado, int Cuantos)? EncabezadoMasComun(IReadOnlyList<string> archivos)
        {
            var masComun = archivos
                .Select(LeerPrimeraLinea)
                .OfType<string>()
                .Select(linea => (Linea: linea, Nombres: PartirEncabezado(linea)))
                .Where(e => PareceEncabezado(e.Linea, e.Nombres))
                .GroupBy(e => string.Join("|", e.Nombres))
                .OrderByDescending(g => g.Count())   // OrderBy es estable: en empate respeta el orden de la lista
                .FirstOrDefault();

            return masComun is null ? null : (masComun.First().Nombres, masComun.Count());
        }

        private static string? LeerPrimeraLinea(string ruta) =>
            File.ReadLines(ruta, Encoding.Latin1).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

        // Separa los nombres de columna y quita caracteres de control (Excel no los acepta)
        private static string[] PartirEncabezado(string linea) =>
            linea.Split('|')
                .Select(c => new string(c.Select(ch => char.IsControl(ch) ? ' ' : ch).ToArray()).Trim())
                .ToArray();

        // Si falla a medio escribir, borra el archivo para no dejar un Excel o CSV dañado
        private static void EscribirSinDejarBasura(string ruta, Action escribir)
        {
            try
            {
                escribir();
            }
            catch (Exception)
            {
                try { File.Delete(ruta); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                throw;
            }
        }

        // Un encabezado trae varias columnas separadas por "|", todas con un nombre corto, y no trae
        // el carácter nulo (\0), que solo aparece en archivos binarios renombrados a .txt (ZIP, PDF, Excel)
        private static bool PareceEncabezado(string linea, string[] nombres) =>
            !linea.Contains('\0') &&
            nombres.Length >= 2 &&
            nombres.All(c => c.Length is > 0 and <= 100);

        // Si una fila trae columnas de menos se rellenan vacías; si trae de más,
        // lo sobrante se pega a la última columna. Así nunca se pierde un registro.
        private static string[] AjustarColumnas(string[] campos, int total, string archivo, int linea, ResultadoConversion r)
        {
            if (campos.Length == total) return campos;

            r.Advertir($"Fila con {(campos.Length < total ? "columnas de menos (se rellenan vacías)" : "columnas de más (se juntan en la última)")}",
                       $"{archivo} línea {linea}: trae {campos.Length} en lugar de {total}");

            if (campos.Length < total)
                return [.. campos, .. Enumerable.Repeat("", total - campos.Length)];

            var ajustados = campos[..total];
            ajustados[^1] = string.Join(" ", campos[(total - 1)..]);
            return ajustados;
        }

        private object? ConvertirValor(string valor, string columna, string archivo, int linea, ResultadoConversion r)
        {
            string limpio = valor.Trim();
            if (limpio.Length == 0) return null;

            switch (Columnas.TipoDe(columna))
            {
                case TipoColumna.Numero:
                    // "0000000217.80" -> 217.80
                    if (decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                        return numero;
                    r.Advertir($"{columna}: no es un importe (se deja como texto)", $"{archivo} línea {linea}: \"{Recortar(limpio)}\"");
                    return _limpiador.LimpiarTexto(valor);

                case TipoColumna.Fecha:
                    if (DateTime.TryParseExact(limpio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                        return fecha;
                    r.Advertir($"{columna}: no es fecha dd/MM/yyyy (se deja como texto)", $"{archivo} línea {linea}: \"{Recortar(limpio)}\"");
                    return _limpiador.LimpiarTexto(valor);

                case TipoColumna.Telefono:
                    // "664 123-4567" -> "6641234567"; si trae letras se deja como viene
                    string digitos = new(limpio.Where(char.IsDigit).ToArray());
                    string telefono = digitos.Length > 0 && limpio.All(ch => char.IsDigit(ch) || " -().".Contains(ch))
                        ? digitos
                        : _limpiador.LimpiarTexto(valor);
                    return Validado(telefono, limpio, columna, archivo, linea, r);

                default:
                    string texto = Columnas.EsReferencia(columna)
                        ? _limpiador.LimpiarReferencia(valor)
                        : _limpiador.LimpiarTexto(valor);
                    return Validado(texto, limpio, columna, archivo, linea, r);
            }
        }

        // Cuenta la corrección y revisa que el dato tenga sentido para su columna 
        
        private static string? Validado(string texto, string original, string columna, string archivo, int linea, ResultadoConversion r)
        {
            if (texto != original) r.ValoresCorregidos++;

            if (texto.Length > EscritorExcel.MaxCaracteresCelda)
                r.Advertir($"{columna}: más de {EscritorExcel.MaxCaracteresCelda:N0} caracteres (en el Excel se corta, el CSV va completo)",
                           $"{archivo} línea {linea}: {texto.Length:N0} caracteres");

            string? problema = Columnas.Validar(columna, texto);
            if (problema is not null)
                r.Advertir($"{columna}: {problema}", $"{archivo} línea {linea}: \"{Recortar(texto)}\"");

            return texto.Length == 0 ? null : texto;
        }

        // Para que un ejemplo muy largo no llene la bitácora
        private static string Recortar(string texto) => texto.Length > 60 ? texto[..60] + "..." : texto;
    }
}
