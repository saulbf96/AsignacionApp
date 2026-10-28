namespace AsignacionApp.Models
{
    // Lo que salió de un ZIP: los TXT extraídos y los archivos que se dejaron fuera  ejemplo si trae un pdf 
    public record ResultadoZip(IReadOnlyList<string> Txt, IReadOnlyList<string> Ignorados);
}
