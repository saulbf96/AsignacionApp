using System.Globalization;
using System.IO;
using System.Text;

namespace AsignacionApp.Services
{
    public static class EscritorCsv
    {
        public static void Escribir(string ruta, string[] encabezado, IReadOnlyList<object?[]> filas)
        {
            // UTF-8 con BOM para que Excel muestre bien Ñ y acentos
            using var writer = new StreamWriter(ruta, false, new UTF8Encoding(true));

            writer.WriteLine(string.Join(",", encabezado.Select(Escapar)));
            foreach (var fila in filas)
                writer.WriteLine(string.Join(",", fila.Select(v => Escapar(Formatear(v)))));
        }

        private static string Formatear(object? valor) => valor switch
        {
            null => "",
            decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
            DateTime f => f.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            _ => valor.ToString() ?? ""
        };

        // Si el valor trae coma o comillas, va entre  comillas
        private static string Escapar(string valor) =>
            valor.IndexOfAny([',', '"', '\n', '\r']) >= 0
                ? "\"" + valor.Replace("\"", "\"\"") + "\""
                : valor;
    }
}
