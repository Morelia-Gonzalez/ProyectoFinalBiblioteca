using System;
using System.Windows.Forms;
using BibliotecaApp.UI;

namespace BibliotecaApp
{
    /// <summary>
    /// Punto de entrada de la aplicación de escritorio (WinForms).
    /// No contiene lógica de negocio ni estructuras de datos; solo arranca
    /// la interfaz gráfica (MainForm).
    /// </summary>
    internal static class Program
    {
        [STAThread] // requerido por WinForms: la app corre en un solo hilo de interfaz (Single Threaded Apartment)
        private static void Main()
        {
            // Configuración estándar de una app WinForms moderna en .NET.
            Application.SetHighDpiMode(HighDpiMode.SystemAware); // soporta pantallas de alta resolución
            Application.EnableVisualStyles();                    // usa los estilos visuales de Windows
            Application.SetCompatibleTextRenderingDefault(false); // usa GDI+ para renderizar texto

            // Crea y muestra la ventana principal del sistema.
            Application.Run(new MainForm());
        }
    }
}
