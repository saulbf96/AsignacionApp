using AsignacionApp.Models;

namespace AsignacionApp.Services
{
    // Une los TXT, los limpia y genera el Excel y/o CSV
    public interface IConversionService
    {
        Task<ResultadoConversion> ConvertirAsync(
            OpcionesConversion opciones,
            IProgress<int>? progreso = null,
            IProgress<string>? log = null);
    }
}
