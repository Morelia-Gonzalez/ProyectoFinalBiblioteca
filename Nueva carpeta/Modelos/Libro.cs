using System;
using BibliotecaApp.Estructuras;

namespace BibliotecaApp.Modelos
{

    /// Representa un libro del catálogo de la biblioteca.
    /// El código (Codigo) es la llave única utilizada como clave del Árbol B+
    /// que se implementó a mano en Estructuras/ArbolBMas.cs.
    
    public class Libro
    {
        // Código único del libro (clave de búsqueda en el Árbol B+).
        public int Codigo;

        // Datos descriptivos del libro.
        public string Titulo;
        public string Autor;
        public string Categoria;

        // Cantidad total de ejemplares que posee la biblioteca de este título.
        public int CopiasTotales;

        // Cantidad de ejemplares actualmente disponibles para préstamo.
        public int CopiasDisponibles;

        // Contador histórico de cuántas veces se ha prestado este libro
        // (se usa como clave para el Max Heap del reporte "más prestados").
        public int VecesPrestado;

        // Historial de préstamos de este libro, implementado con la lista
        // enlazada propia (ListaEnlazada<Prestamo>), NO con List<T> de .NET.
        public ListaEnlazada<Prestamo> HistorialPrestamos;

  
        /// Crea un nuevo libro con sus copias disponibles iguales a las totales
        /// (todavía no se ha prestado ningún ejemplar) y el historial vacío.

        public Libro(int codigo, string titulo, string autor, string categoria, int copiasTotales)
        {
            Codigo = codigo;
            Titulo = titulo;
            Autor = autor;
            Categoria = categoria;
            CopiasTotales = copiasTotales;
            CopiasDisponibles = copiasTotales; // al crear el libro, todas las copias están disponibles
            VecesPrestado = 0;
            HistorialPrestamos = new ListaEnlazada<Prestamo>();
        }


        /// Representación en texto usada para mostrar el detalle de un libro
        /// (por ejemplo, en el resultado de la búsqueda por código).

        public override string ToString()
        {
            return $"[{Codigo}] {Titulo} - {Autor} | Categoría: {Categoria} | " +
                   $"Disponibles: {CopiasDisponibles}/{CopiasTotales} | Veces prestado: {VecesPrestado}";
        }


        /// Convierte el libro a una línea de texto separada por punto y coma,
        /// usada para guardar el catálogo en un archivo CSV (soporte auxiliar
        /// de E/S, no reemplaza ninguna estructura de datos solicitada).

        public string ToCsvLine()
        {
            return $"{Codigo};{Titulo};{Autor};{Categoria};{CopiasTotales};{CopiasDisponibles};{VecesPrestado}";
        }
    }
}
