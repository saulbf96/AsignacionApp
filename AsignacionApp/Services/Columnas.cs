using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AsignacionApp.Services
{

    public enum TipoColumna { Texto, Numero, Fecha, Telefono }
    public static partial class Columnas
    {
        public const int Total = 54;

        private static readonly HashSet<string> Numeros = new(StringComparer.OrdinalIgnoreCase)
        {
            "totReclamado", "Consumo", "PenaEquipo", "PenaServicio", "PenaServicio20", "ImporteUltimoPago"
        };

        private static readonly HashSet<string> Fechas = new(StringComparer.OrdinalIgnoreCase)
        {
            "F_carga", "FechaNacimientoPF", "fecUltimoPago"
        };

        private static readonly HashSet<string> Telefonos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Telefono", "Telefono1", "Telefono2", "FTelefono1", "FTelefono2", "R1Telefono", "R2Telefono"
        };

        // Campos de referencias: vienen como texto libre (teléfono:nombre teléfono:nombre ///)
        private static readonly HashSet<string> Referencias = new(StringComparer.OrdinalIgnoreCase)
        {
            "R1Nombre", "R2Nombre"
        };

        // Todo lo demás (Cuenta, CP, RFC...) es texto para no perder los ceros a la izquierda
        public static TipoColumna TipoDe(string columna) =>
            Numeros.Contains(columna) ? TipoColumna.Numero :
            Fechas.Contains(columna) ? TipoColumna.Fecha :
            Telefonos.Contains(columna) ? TipoColumna.Telefono :
            TipoColumna.Texto;

        public static bool EsReferencia(string columna) => Referencias.Contains(columna);

        // Regresa null si el valor está bien, o el motivo si se ve mal.
        // Solo avisa: el dato se conserva para que no falte información.
        public static string? Validar(string columna, string valor)
        {
            if (valor.Length == 0)
                return columna.Equals("Cuenta", StringComparison.OrdinalIgnoreCase) ? "Cuenta vacía" : null;

            return columna.ToLowerInvariant() switch
            {
                "cuenta" when !DiezDigitos().IsMatch(valor) => "la cuenta debe tener 10 dígitos",
                "cp" or "fcp" when !CincoDigitos().IsMatch(valor) => "el CP debe tener 5 dígitos",
                "region" when !SoloDigitos().IsMatch(valor) => "la región debe ser numérica",
                "correo" when !Correo().IsMatch(valor) => "el correo no es válido",
                _ when Telefonos.Contains(columna) => ProblemaTelefono(valor),
                _ => null
            };
        }

        // Separa los teléfonos malos por tipo para que el resumen de la bitácora se entienda
        private static string? ProblemaTelefono(string valor) => valor switch
        {
            _ when valor.Length is 8 or 10 && SoloDigitos().IsMatch(valor) => null,
            _ when valor.All(ch => ch == '0') => "viene en 0 (sin teléfono)",
            _ when !SoloDigitos().IsMatch(valor) => "trae letras o signos",
            { Length: < 5 } => "incompleto (1 a 4 dígitos, a veces solo la lada)",
            { Length: < 8 } => "incompleto (5 a 7 dígitos)",
            { Length: 9 } => "9 dígitos (le falta o le sobra uno)",
            _ => "más de 10 dígitos"
        };

        [GeneratedRegex(@"^\d{10}$")]
        private static partial Regex DiezDigitos();

        [GeneratedRegex(@"^\d{5}$")]
        private static partial Regex CincoDigitos();

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex SoloDigitos();

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex Correo();
    }
}

