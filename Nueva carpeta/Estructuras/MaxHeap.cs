using System;
using BibliotecaApp.Modelos;

namespace BibliotecaApp.Estructuras
{
    /// Montículo máximo (Max Heap)El arreglo es de tamaño fijo y crece manualmente mediante
    /// Redimensionar() cuando se necesita más espacio.
    ///
    /// Justificación de uso: para responder "¿cuáles son los libros más
    /// prestados?" se necesita repetidamente obtener el elemento de mayor
    /// valor (veces prestado) sin mantener la colección completa ordenada.
    /// Construir el heap es O(n) y cada extracción del máximo es O(log n),
    /// lo cual es más eficiente que ordenar todo el catálogo cuando solo se
    /// necesita un top-N.

    public class MaxHeap
    {
        // Arreglo donde se almacena el heap en representación implícita
        // (el hijo izquierdo de i está en 2i+1, el derecho en 2i+2, el padre en (i-1)/2).
        private Libro[] datos;

        // Cantidad de elementos actualmente ocupados en "datos".
        private int cantidad;

        // Función que indica sobre qué campo del libro se compara
        // (por ejemplo, l => l.VecesPrestado). Permite reutilizar la misma
        // clase de heap para distintos criterios sin duplicar código.
        private readonly Func<Libro, int> obtenerClave;

        public int Cantidad => cantidad;

        /// Crea un heap vacío con la capacidad inicial indicada.
  
        public MaxHeap(int capacidad, Func<Libro, int> obtenerClave)
        {
            if (capacidad < 1) capacidad = 1;
            datos = new Libro[capacidad];
            cantidad = 0;
            this.obtenerClave = obtenerClave;
        }


        /// Construye un heap a partir de todos los libros del catálogo en O(n),
        /// usando el algoritmo clásico de "heapify" (hundir desde la mitad
        /// del arreglo hacia el inicio).
  
        public static MaxHeap ConstruirDesde(Vector<Libro> libros, Func<Libro, int> obtenerClave)
        {
            int n = libros.Cantidad;
            MaxHeap heap = new MaxHeap(n == 0 ? 1 : n, obtenerClave);

            // Copia todos los libros al arreglo interno del heap (todavía sin
            // garantizar la propiedad de montículo).
            for (int i = 0; i < n; i++)
            {
                heap.datos[i] = libros[i];
                heap.cantidad++;
            }

            // Heapify: hunde cada nodo interno desde el último hacia la raíz,
            // así se garantiza la propiedad de "padre >= hijos" en todo el arreglo.
            for (int i = heap.cantidad / 2 - 1; i >= 0; i--)
                heap.Hundir(i);

            return heap;
        }


        /// Inserta un nuevo libro al heap: se coloca al final y se "flota"
        /// hacia arriba hasta restaurar la propiedad de montículo.
        public void Insertar(Libro libro)
        {
            if (cantidad == datos.Length) Redimensionar();
            datos[cantidad] = libro;
            Flotar(cantidad);
            cantidad++;
        }


        /// Extrae y elimina el elemento máximo (la raíz). Se sustituye la raíz
        /// por el último elemento y se "hunde" para restaurar la propiedad.

        public Libro ExtraerMaximo()
        {
            if (cantidad == 0) return null;
            Libro max = datos[0];
            cantidad--;
            datos[0] = datos[cantidad]; // mueve el último elemento a la raíz
            datos[cantidad] = null;     // limpia la posición sobrante
            if (cantidad > 0) Hundir(0);
            return max;
        }

        // Devuelve el máximo actual sin eliminarlo (o null si el heap está vacío).
        public Libro VerMaximo() => cantidad > 0 ? datos[0] : null;

 
        /// Sube el elemento en la posición i mientras sea mayor que su padre
        /// (usado tras una inserción, cuando el nuevo elemento puede violar
        /// la propiedad de montículo hacia arriba).

        private void Flotar(int i)
        {
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (obtenerClave(datos[i]) <= obtenerClave(datos[padre])) break; // ya cumple la propiedad
                Intercambiar(i, padre);
                i = padre;
            }
        }

 
        /// Baja el elemento en la posición i intercambiándolo con su hijo
        /// mayor mientras sea menor que alguno de sus hijos (usado tras
        /// eliminar la raíz o durante el heapify inicial).

        private void Hundir(int i)
        {
            while (true)
            {
                int izq = 2 * i + 1;
                int der = 2 * i + 2;
                int mayor = i;

                if (izq < cantidad && obtenerClave(datos[izq]) > obtenerClave(datos[mayor])) mayor = izq;
                if (der < cantidad && obtenerClave(datos[der]) > obtenerClave(datos[mayor])) mayor = der;

                if (mayor == i) break; // ya no viola la propiedad, se detiene
                Intercambiar(i, mayor);
                i = mayor;
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
