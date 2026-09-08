using System;

namespace BibliotecaApp.Estructuras
{

    /// Se usa únicamente como estructura AUXILIAR para recolectar resultados
    /// que se van a mostrar en pantalla (por ejemplo, el resultado de un
    /// recorrido del Árbol B+, o la lista de errores al leer un CSV).
    /// NO reemplaza la lógica interna del Árbol B+ ni de los montículos:
    /// esas estructuras guardan sus propios datos en arreglos de tamaño fijo
    /// (T[]) manejados a mano dentro de sus propias clases.
  
    public class Vector<T>
    {
        // Arreglo interno donde realmente se guardan los elementos.
        private T[] datos;

        // Cantidad de elementos actualmente ocupados (puede ser menor que datos.Length).
        private int cantidad;

        /// Crea un vector vacío con una capacidad inicial dada (por defecto 8).
        public Vector(int capacidadInicial = 8)
        {
            if (capacidadInicial < 1) capacidadInicial = 1;
            datos = new T[capacidadInicial];
            cantidad = 0;
        }

        // Cantidad de elementos "lógicos" almacenados (no la capacidad física del arreglo).
        public int Cantidad => cantidad;

        /// Indexador: permite usar la sintaxis vector[i] para leer o escribir
        /// un elemento, validando que el índice esté dentro de lo ocupado.
        public T this[int indice]
        {
            get
            {
                if (indice < 0 || indice >= cantidad)
                    throw new IndexOutOfRangeException("Índice fuera de rango en Vector.");
                return datos[indice];
            }
            set
            {
                if (indice < 0 || indice >= cantidad)
                    throw new IndexOutOfRangeException("Índice fuera de rango en Vector.");
                datos[indice] = value;
            }
        }

        /// Agrega un elemento al final. Si el arreglo interno ya está lleno,
        /// primero se duplica su capacidad (Redimensionar) antes de insertar.
        public void Agregar(T elemento)
        {
            if (cantidad == datos.Length)
            {
                Redimensionar(datos.Length * 2);
            }
            datos[cantidad] = elemento;
            cantidad++;
        }

        /// Elimina el elemento en la posición indicada, recorriendo el resto
        /// de los elementos un lugar hacia la izquierda para cerrar el hueco.
        public void RemoverEn(int indice)
        {
            if (indice < 0 || indice >= cantidad)
                throw new IndexOutOfRangeException("Índice fuera de rango en Vector.");
            for (int i = indice; i < cantidad - 1; i++)
            {
                datos[i] = datos[i + 1];
            }
            cantidad--;
        }

        /// Crea un arreglo nuevo más grande y copia manualmente los elementos
        /// existentes (crecimiento dinámico hecho a mano, sin List&lt;T&gt;).
        private void Redimensionar(int nuevaCapacidad)
        {
            T[] nuevo = new T[nuevaCapacidad];
            for (int i = 0; i < cantidad; i++) nuevo[i] = datos[i];
            datos = nuevo;
        }

        /// Vacía el vector lógicamente (no libera el arreglo interno, solo
        /// reinicia el contador de elementos ocupados).
        public void Limpiar()
        {
            cantidad = 0;
        }

        /// Ordenamiento por mezcla (merge sort) implementado manualmente,
        /// usando un comparador provisto por el llamador (por ejemplo, para
        /// ordenar libros por título). No usa Array.Sort ni List.Sort.
        public void OrdenarPorMezcla(Comparison<T> comparador)
        {
            if (cantidad <= 1) return; // 0 o 1 elementos ya están "ordenados"
            T[] auxiliar = new T[cantidad]; // arreglo de apoyo para la mezcla
            MergeSort(0, cantidad - 1, auxiliar, comparador);
        }

        /// Paso recursivo de "dividir": separa el rango [inicio, fin] en dos
        /// mitades, ordena cada una por separado y luego las combina.
        private void MergeSort(int inicio, int fin, T[] auxiliar, Comparison<T> comparador)
        {
            if (inicio >= fin) return; // rango de 0 o 1 elementos, nada que dividir
            int medio = (inicio + fin) / 2;
            MergeSort(inicio, medio, auxiliar, comparador);       // ordena mitad izquierda
            MergeSort(medio + 1, fin, auxiliar, comparador);      // ordena mitad derecha
            Mezclar(inicio, medio, fin, auxiliar, comparador);    // combina ambas mitades ordenadas
        }

        /// Paso de "combinar": mezcla dos sub-arreglos ya ordenados
        /// ([inicio, medio] y [medio+1, fin]) en un solo bloque ordenado,
        /// usando el arreglo auxiliar para no perder datos al sobrescribir.
        private void Mezclar(int inicio, int medio, int fin, T[] auxiliar, Comparison<T> comparador)
        {
            // Copia el rango actual al arreglo auxiliar antes de sobrescribir "datos".
            for (int k = inicio; k <= fin; k++) auxiliar[k] = datos[k];

            int i = inicio;      // puntero dentro de la mitad izquierda
            int j = medio + 1;   // puntero dentro de la mitad derecha

            for (int k = inicio; k <= fin; k++)
            {
                if (i > medio) datos[k] = auxiliar[j++];                              // ya no quedan elementos a la izquierda
                else if (j > fin) datos[k] = auxiliar[i++];                           // ya no quedan elementos a la derecha
                else if (comparador(auxiliar[i], auxiliar[j]) <= 0) datos[k] = auxiliar[i++]; // izquierda es menor o igual
                else datos[k] = auxiliar[j++];                                        // derecha es menor
            }
        }
    }
}
