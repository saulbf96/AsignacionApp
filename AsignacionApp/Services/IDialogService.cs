using AsignacionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionApp.Services
{
    public interface IDialogService
    {
        string? ElegirCarpeta(string titulo);
        // Regresa los archivos elegidos, o la carpeta si el usuario eligió "usar esta carpeta completa"
        string[] ElegirArchivosOCarpeta(string titulo, string filtro);
        void MostrarError(string Mensaje);
        void MostrarInfo(string mensaje);
        void AbrirCarpeta(string ruta);

        // Pregunta Sí / No; regresa true solo si el usuario dijo que Sí
        bool Confirmar(string titulo, string mensaje);

        // Regresa la contraseña escrita, o null si el usuario canceló
        string? PedirContrasena(string archivo);

        // Ventana del tamaño de la app con el resultado y la bitácora completa
        void MostrarReporte(ResultadoConversion resultado, string bitacora);

    }
}
