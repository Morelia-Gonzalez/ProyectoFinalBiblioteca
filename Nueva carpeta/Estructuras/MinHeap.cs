using System;
using BibliotecaApp.Modelos;

namespace BibliotecaApp.Estructuras
{
 
    /// Justificación de uso: para generar una alerta de "libros con menos
    /// copias disponibles" (candidatos a reabastecer) se necesita obtener
    /// repetidamente el elemento de menor valor (copias disponibles) de forma
    /// eficiente. Igual que el Max Heap, construirlo es O(n) y cada extracción
    /// del mínimo es O(log n).
 
    public class MinHeap
    {
        // Arreglo donde se almacena el heap en representación implícita.
        private Libro[] datos;

        // Cantidad de elementos actualmente ocupados en "datos".
        private int cantidad;

        // Función que indica sobre qué campo del libro se compara
        // (por ejemplo, l => l.CopiasDisponibles).
        private readonly Func<Libro, int> obtenerClave;

        public int Cantidad => cantidad;

        /// Crea un heap vacío con la capacidad inicial indicada.
        public MinHeap(int capacidad, Func<Libro, int> obtenerClave)
        {
            if (capacidad < 1) capacidad = 1;
            datos = new Libro[capacidad];
            cantidad = 0;
            this.obtenerClave = obtenerClave;
        }


        /// Construye un heap a partir de todos los libros del catálogo en O(n)
        /// usando el algoritmo clásico de "heapify".

        public static MinHeap ConstruirDesde(Vector<Libro> libros, Func<Libro, int> obtenerClave)
        {
            int n = libros.Cantidad;
            MinHeap heap = new MinHeap(n == 0 ? 1 : n, obtenerClave);

            for (int i = 0; i < n; i++)
            {
                heap.datos[i] = libros[i];
                heap.cantidad++;
            }

            for (int i = heap.cantidad / 2 - 1; i >= 0; i--)
                heap.Hundir(i);

            return heap;
        }


        /// Inserta un nuevo libro: se coloca al final y se "flota" hacia
        /// arriba hasta restaurar la propiedad de montículo mínimo.

        public void Insertar(Libro libro)
        {
            if (cantidad == datos.Length) Redimensionar();
            datos[cantidad] = libro;
            Flotar(cantidad);
            cantidad++;
        }


        /// Extrae y elimina el elemento mínimo (la raíz). Se sustituye la raíz
        /// por el último elemento y se "hunde" para restaurar la propiedad.

        public Libro ExtraerMinimo()
        {
            if (cantidad == 0) return null;
            Libro min = datos[0];
            cantidad--;
            datos[0] = datos[cantidad];
            datos[cantidad] = null;
            if (cantidad > 0) Hundir(0);
            return min;
        }

        // Devuelve el mínimo actual sin eliminarlo (o null si el heap está vacío).
        public Libro VerMinimo() => cantidad > 0 ? datos[0] : null;

  
        /// Sube el elemento en la posición i mientras sea menor que su padre.

        private void Flotar(int i)
        {
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (obtenerClave(datos[i]) >= obtenerClave(datos[padre])) break; // ya cumple la propiedad
                Intercambiar(i, padre);
                i = padre;
            }
        }

        /// Baja el elemento en la posición i intercambiándolo con su hijo
        /// menor mientras sea mayor que alguno de sus hijos.
    
        private void Hundir(int i)
        {
            while (true)
            {
                int izq = 2 * i + 1;
                int der = 2 * i + 2;
                int menor = i;

                if (izq < cantidad && obtenerClave(datos[izq]) < obtenerClave(datos[menor])) menor = izq;
                if (der < cantidad && obtenerClave(datos[der]) < obtenerClave(datos[menor])) menor = der;

                if (menor == i) break; // ya no viola la propiedad, se detiene
                Intercambiar(i, menor);
                i = menor;
            }
        }

        // Intercambia dos posiciones del arreglo interno.
        private void Intercambiar(int a, int b)
        {
            Libro tmp = datos[a];
            datos[a] = datos[b];
            datos[b] = tmp;
        }

        /// Duplica la capacidad del arreglo interno cuando se llena,
        /// copiando manualmente los elementos existentes (sin List&lt;T&gt;).
        private void Redimensionar()
        {
            Libro[] nuevo = new Libro[datos.Length * 2];
            for (int i = 0; i < datos.Length; i++) nuevo[i] = datos[i];
            datos = nuevo;
        }
    }
}
