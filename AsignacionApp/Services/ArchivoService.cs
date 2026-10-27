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

        public IEnumerable<string> ObtenerTxt(IEnumerable<string> rutas)
        {
            foreach (var ruta in rutas)
            {
                if(Directory.Exists(ruta))
                {
                    var txts= Directory.EnumerateFiles(ruta, "*.txt")
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
                    foreach (var txt in txts)
                        yield return txt;

                }
                else if (File.Exists(ruta) && Estxt(ruta))
                {
                    yield return ruta;
                }
            }
        }

        public Task<ArchivoTxt> LeerInfoAsync(string ruta) =>
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
                    Registros = Math.Max(lineas - 1, 0)


                };
            });


        private static bool Estxt(string ruta) =>
            Path.GetExtension(ruta).Equals(".txt", StringComparison.OrdinalIgnoreCase);


    }
}
