using AsignacionApp.Models;

namespace AsignacionApp.Services
{
    // Operaciones con archivos ZIP (pueden traer contraseña)
    public interface IZipService
    {
        // true si algún archivo dentro del ZIP está protegido con contraseña
        bool TieneContrasena(string zip);

        // Extrae los TXT a una carpeta temporal y regresa sus rutas, más los nombres de lo que no es TXT.
        // Regresa null si la contraseña no es correcta.
        Task<ResultadoZip?> ExtraerTxtAsync(string zip, string? contrasena);

        // Borra los TXT extraídos (traen datos de clientes, no deben quedarse en el equipo)
        void BorrarTemporales();
    }
}
