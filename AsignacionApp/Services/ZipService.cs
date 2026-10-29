using AsignacionApp.Models;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AsignacionApp.Services
{
    // Usa SharpZipLib porque el ZIP que trae .NET no abre archivos con contraseña
    public class ZipService : IZipService
    {
        private static readonly string CarpetaTemporal = Path.Combine(Path.GetTempPath(), "AsignacionApp");

        public bool TieneContrasena(string zip)
        {
            try
            {
                using var archivo = new ZipFile(zip);
                return archivo.Cast<ZipEntry>().Any(e => e.IsFile && e.IsCrypted);
            }
            catch (ZipException)
            {
                throw new InvalidDataException($"El ZIP está dañado o no es un ZIP válido:\n{zip}");
            }
        }

        public Task<ResultadoZip?> ExtraerTxtAsync(string zip, string? contrasena) =>
            Task.Run<ResultadoZip?>(() =>
            {
                // El mismo ZIP siempre va a la misma carpeta: si se agrega dos veces no se duplica en la lista
                string destino = Path.Combine(CarpetaTemporal, Path.GetFileNameWithoutExtension(zip) + "_" + CodigoDe(zip));
                var extraidos = new List<string>();
                var ignorados = new List<string>();

                using var archivo = new ZipFile(zip) { Password = contrasena };
                foreach (ZipEntry entrada in archivo)
                {
                    if (!entrada.IsFile)
                        continue;

                    // Solo se extraen los TXT; lo demás (PDF, Excel, otro ZIP...) se reporta y se deja dentro
                    if (!entrada.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        ignorados.Add(entrada.Name);
                        continue;
                    }

                    // Seguridad: un ZIP malicioso podría traer rutas como "..\..\archivo"
                    string ruta = Path.GetFullPath(Path.Combine(destino, entrada.Name));
                    if (!ruta.StartsWith(destino + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
                    try
                    {
                        using var origen = archivo.GetInputStream(entrada);
                        using var salida = File.Create(ruta);
                        CopiarRevisandoCrc(origen, salida, entrada);
                    }
                    catch (ZipException) when (entrada.IsCrypted)
                    {
                        // Contraseña vacía o incorrecta
                        File.Delete(ruta);
                        return null;
                    }
                    catch (ZipException ex)
                    {
                        // Archivo dañado por dentro (CRC) o con una compresión que no se puede leer
                        File.Delete(ruta);
                        throw new InvalidDataException($"El ZIP está dañado o usa un formato no soportado:\n{zip}\n({entrada.Name}: {ex.Message})");
                    }
                    extraidos.Add(ruta);
                }

                return new ResultadoZip(extraidos.Order(StringComparer.OrdinalIgnoreCase).ToList(), ignorados);
            });

        // SharpZipLib no revisa el CRC al extraer así: se calcula aquí para no meter datos dañados al Excel
        private static void CopiarRevisandoCrc(Stream origen, Stream salida, ZipEntry entrada)
        {
            var crc = new Crc32();
            var buffer = new byte[81920];
            int leidos;
            while ((leidos = origen.Read(buffer, 0, buffer.Length)) > 0)
            {
                crc.Update(new ArraySegment<byte>(buffer, 0, leidos));
                salida.Write(buffer, 0, leidos);
            }

            if (entrada.HasCrc && crc.Value != entrada.Crc)
                throw new ZipException($"CRC no coincide, el archivo está dañado");
        }

        public void BorrarTemporales()
        {
            try
            {
                if (Directory.Exists(CarpetaTemporal))
                    Directory.Delete(CarpetaTemporal, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Si algún archivo está en uso se queda; Windows limpia la carpeta temporal después
            }
        }

        // Código corto que identifica la ruta completa del ZIP (dos ZIP con el mismo nombre no se mezclan)
        private static string CodigoDe(string zip) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(zip.ToUpperInvariant())))[..8];
    }
}
