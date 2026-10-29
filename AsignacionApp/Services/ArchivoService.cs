using AsignacionApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AsignacionApp.Services
{
    public  class ArchivoService : IArchivoService
    {

        public IEnumerable<string> ObtenerTxt(IEnumerable<string> rutas) => Obtener(rutas, ".txt");

        public IEnumerable<string> ObtenerZip(IEnumerable<string> rutas) => Obtener(rutas, ".zip");

        // Regresa los archivos con esa extensión: los sueltos y los que estén dentro de una carpeta
        private static IEnumerable<string> Obtener(IEnumerable<string> rutas, string extension)
        {
            foreach (var ruta in rutas)
            {
                if(Directory.Exists(ruta))
                {
                    var archivos = Directory.EnumerateFiles(ruta, "*" + extension)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
                    foreach (var archivo in archivos)
                        yield return archivo;

                }
                else if (File.Exists(ruta) && TieneExtension(ruta, extension))
                {
                    yield return ruta;
                }
            }
        }

        

        public Task<ArchivoTxt> LeerInfoAsync(string ruta, string? zip = null) =>
            //task run se hace para que no se congele en segundo plano 

            Task.Run(() =>
            {
                var info = new FileInfo(ruta);
                int lineas = File.ReadLines(ruta).Count(l => !string.IsNullOrWhiteSpace(l));

                return new ArchivoTxt
                {
                    Ruta = ruta,
                    Nombre = info.Name,
                    Bytes = info.Length,
                    Registros = Math.Max(lineas - 1, 0),
                    Zip = zip
                };
            });




        private static bool TieneExtension(string ruta, string extension) =>
            Path.GetExtension(ruta).Equals(extension, StringComparison.OrdinalIgnoreCase);




    }
}
