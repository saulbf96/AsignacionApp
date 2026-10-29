using AsignacionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionApp.Services
{
    //Operacion con archivo del disco 
    public interface IArchivoService
    {

        //Convertir la ruta 
        IEnumerable<string> ObtenerTxt (IEnumerable<string> rutas);

        // Igual que ObtenerTxt pero para los .zip sueltos o dentro de una carpeta
        IEnumerable<string> ObtenerZip(IEnumerable<string> rutas);

        //lee el tamaño y numero de registro del archivo txt.
        // zip: ruta del ZIP de donde se extrajo (null si es un TXT suelto)
        Task<ArchivoTxt> LeerInfoAsync(string ruta, string? zip = null);


        



    }
}
