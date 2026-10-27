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
        string[] ElegirArchivos(string titulo, string filtro);
        void MostrarError(string Mensaje);

    }
}
