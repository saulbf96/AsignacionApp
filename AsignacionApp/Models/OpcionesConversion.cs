using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionApp.Models
{
    //lo que el usuario eligio en pantalla en un solo paquete para el servicio
    public class OpcionesConversion
    {

        public required IReadOnlyList<string> Archivos { get; init; }
        public required string CarpetaSalida { get; init; }
        public required string NombreArchivo { get; init; }

        public required string NombreHoja { get; init; }

        public bool GenerarExcel { get; init; } 
        public bool GenerarCsv { get; init; }

        public bool AgregarColumnaArchivo { get; init; }




    }
}
