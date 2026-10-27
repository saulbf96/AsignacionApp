using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace AsignacionApp.Models
{
    //Aerchivo agregado 
    public  class ArchivoTxt
    {

        //variables 
        public required string Ruta { get; init; }
        public required string Nombre { get; init; }

        public long Bytes { get; init; }

        public int Registros { get; init; }

        public string Tamano => $"{Bytes / 1024:N0} KB";
    }
}
