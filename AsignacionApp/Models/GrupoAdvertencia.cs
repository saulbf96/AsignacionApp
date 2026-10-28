namespace AsignacionApp.Models
{
    // Un tipo de advertencia: cuántas veces apareció por ejemplo  "L091-20325.TXT línea 26: "0""
    public class GrupoAdvertencia
    {
        public int Cantidad { get; set; }
        public List<string> Ejemplos { get; } = new();
    }
}
