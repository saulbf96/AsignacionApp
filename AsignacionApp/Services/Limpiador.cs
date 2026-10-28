using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AsignacionApp.Services
{

    // Corrige los caracteres dañados sin quitar información
    public partial class Limpiador
    {


        // El orden importa: primero los textos largos ("N´+¢") y al final los sueltos ("¦")
        private static readonly (string Malo, string Bueno)[] ReemplazosPorDefecto =
        [
            ("N´+¢", "Ñ"),
            ("´+¢", "Ñ"),
            ("D¦AZ", "DÍAZ"),
            ("-¦", "°"),      // "1-¦ DE MAYO" -> "1° DE MAYO"
            ("¦", "Á"),       // ATIZAP¦N, TEC¦MAC, JU¦REZ
            ("¼", "Ñ"),       // CA¼ADA, NI¼OS
            ("Ð", "Ñ"),
            ("ê", "É"),       // HêROES
            ("õ", "Ó"),       // CARBõ
        ];

        private readonly IReadOnlyList<(string Malo, string Bueno)> _reemplazos;

        public Limpiador(IReadOnlyList<(string Malo, string Bueno)> reemplazos)
        {
            _reemplazos = reemplazos;
        }

        // Si existe reemplazos.txt (líneas "malo=bueno") se usa ese; si no, la tabla de arriba
        public static Limpiador DesdeArchivo(string ruta)
        {
            if (!File.Exists(ruta))
                return new Limpiador(ReemplazosPorDefecto);

            var reemplazos = File.ReadLines(ruta, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#') && l.Contains('='))
                .Select(l => (l[..l.IndexOf('=')], l[(l.IndexOf('=') + 1)..]))
                .ToList();

            return new Limpiador(reemplazos);
        }

        public string LimpiarTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return "";

            var sb = new StringBuilder(valor);
            foreach (var (malo, bueno) in _reemplazos)
                sb.Replace(malo, bueno);

            // ^, tabuladores y caracteres de control se vuelven espacio
            for (int i = 0; i < sb.Length; i++)
                if (sb[i] == '^' || char.IsControl(sb[i]))
                    sb[i] = ' ';

            return QuitarEspaciosDeMas(sb.ToString());
        }

        // R1Nombre / R2Nombre: además quita "///" y separa el teléfono del nombre
        // "6646696869:JESUS PALOMINO ///" -> "6646696869 JESUS PALOMINO"
        public string LimpiarReferencia(string valor)
        {
            var texto = LimpiarTexto(valor);
            texto = Diagonales().Replace(texto, " ");
            texto = SeparadorJuntoANumero().Replace(texto, " ");
            return QuitarEspaciosDeMas(texto);
        }

        private static string QuitarEspaciosDeMas(string texto) =>
            Espacios().Replace(texto, " ").Trim();

        [GeneratedRegex(@"\s{2,}")]
        private static partial Regex Espacios();

        [GeneratedRegex(@"/{2,}")]
        private static partial Regex Diagonales();

        [GeneratedRegex(@"(?<=\d)[:\-]|[:\-](?=\d)")]
        private static partial Regex SeparadorJuntoANumero();
    }
}

