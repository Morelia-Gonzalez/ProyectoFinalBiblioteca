using System;

namespace BibliotecaApp.Modelos
{

    /// Representa un préstamo individual de una copia de un libro.
    /// Cada vez que se presta un ejemplar se crea un objeto de esta clase y
    /// se agrega al historial (ListaEnlazada&lt;Prestamo&gt;) del libro correspondiente.
  
    public class Prestamo
    {
        // Código del libro al que pertenece este préstamo (clave del Árbol B+).
        public int CodigoLibro;

        // Nombre de la persona que solicita el préstamo (quien "presta" el libro).
        public string NombrePrestatario;

        // Fecha y hora en que se registró el préstamo.
        public DateTime FechaPrestamo;

        // Fecha y hora en que se devolvió el libro. Es "null" (Nothing en VB)
        // mientras el ejemplar sigue en poder del prestatario.
        public DateTime? FechaDevolucion;

        // Propiedad calculada: true si ya existe una fecha de devolución registrada.
        public bool Devuelto => FechaDevolucion.HasValue;


        /// <param name="codigoLibro">Código del libro prestado.</param>
        /// <param name="nombrePrestatario">Nombre de quien retira el libro.</param>
        /// <param name="fechaPrestamo">Fecha/hora en que se realiza el préstamo.</param>
        public Prestamo(int codigoLibro, string nombrePrestatario, DateTime fechaPrestamo)
        {
            CodigoLibro = codigoLibro;
            NombrePrestatario = nombrePrestatario;
            FechaPrestamo = fechaPrestamo;
            FechaDevolucion = null; // todavía no ha sido devuelto
        }


        /// Representación en texto usada para mostrar el historial en pantalla
        /// (por ejemplo, dentro del ListBox de historial en MainForm).
  
        public override string ToString()
        {
            // Arma el texto de estado según si ya fue devuelto o sigue pendiente.
            string estado = Devuelto
                ? $"Devuelto el {FechaDevolucion:dd/MM/yyyy}"
                : "Prestado (pendiente de devolución)";

            return $"Prestado a: {NombrePrestatario} | Fecha: {FechaPrestamo:dd/MM/yyyy} -> {estado}";
        }
    }
}
