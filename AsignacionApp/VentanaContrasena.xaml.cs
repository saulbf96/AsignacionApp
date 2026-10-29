using System.Windows;

namespace AsignacionApp
{
    // Ventana que pide la contraseña de un ZIP. PasswordBox no permite Binding,
    // por eso la contraseña se lee aquí y la regresa DialogService.
    public partial class VentanaContrasena : Window
    {
        public VentanaContrasena(string archivo)
        {
            InitializeComponent();
            TxtMensaje.Text = $"El archivo \"{archivo}\" tiene contraseña. Escríbela:";
            Loaded += (_, _) => CajaContrasena.Focus();
        }

        public string Contrasena => CajaContrasena.Password;

        private void Aceptar_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
