
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionApp.Models
{
    public class ResultadoConversion
    {
        //reporte de lo que se convirtio 

        public int RegistrosLeidos { get; set; }
        public int RegistrosEscritos { get; set; }

        public int ValoresCorregidos { get; set; }

        // TXT que no se unieron porque no tienen el formato de Inbursa
        public int ArchivosOmitidos { get; set; }

        public int TotalAdvertencias { get; private set; }

        // Advertencias agrupadas por tipo (ej. "FTelefono1: viene en 0 (sin teléfono)"),
        // con cuántas veces pasó y unos ejemplos para ir a buscarlos al TXT
        public Dictionary<string, GrupoAdvertencia> Advertencias { get; } = new();

        private const int MaxEjemplos = 3;

        public void Advertir(string tipo, string ejemplo)
        {
            TotalAdvertencias++;
            if (!Advertencias.TryGetValue(tipo, out var grupo))
                Advertencias[tipo] = grupo = new GrupoAdvertencia();

            grupo.Cantidad++;
            if (grupo.Ejemplos.Count < MaxEjemplos)
                grupo.Ejemplos.Add(ejemplo);
        }

        public List<string> ArchivosGenerados { get; } = new();




    }
}
