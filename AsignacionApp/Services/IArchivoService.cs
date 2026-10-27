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

        //lee el tamaño y numero de registro del archivo txt. 

        Task<ArchivoTxt> LeerInfoAsync(string ruta);


        



    }
}
