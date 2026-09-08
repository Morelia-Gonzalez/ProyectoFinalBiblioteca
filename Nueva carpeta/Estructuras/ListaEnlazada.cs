using System;

namespace BibliotecaApp.Estructuras
{

    /// Nodo de una lista simplemente enlazada. Cada nodo guarda un dato de
    /// tipo genérico T y una referencia al siguiente nodo de la cadena.
  
    public class NodoLista<T>
    {
        public T Dato;                 // valor almacenado en este nodo
        public NodoLista<T> Siguiente; // referencia al próximo nodo (o null si es el último)

        public NodoLista(T dato)
        {
            Dato = dato;
            Siguiente = null;
        }
    }

    /// Se usa para almacenar el historial de préstamos de cada libro porque:
    ///  - Las inserciones (nuevo préstamo) siempre ocurren al final o inicio: O(1).
    ///  - Se recorre completa para mostrar el historial: O(n).
    ///  - No se requiere acceso aleatorio ni búsqueda binaria, por lo que
    ///    no se justifica una estructura más compleja (árbol) para este dato.
    public class ListaEnlazada<T>
    {
        // Referencia al primer y al último nodo de la lista.
        private NodoLista<T> cabeza;
        private NodoLista<T> cola;

        // Cantidad de nodos actualmente en la lista.
        private int cantidad;

        public int Cantidad => cantidad;
        public NodoLista<T> Cabeza => cabeza; // expuesto para poder recorrer manualmente desde el servicio

        /// Inserta un nuevo dato al final de la lista (O(1) gracias a que se
        /// mantiene el puntero "cola" actualizado en todo momento).
        public void AgregarFinal(T dato)
        {
            NodoLista<T> nuevo = new NodoLista<T>(dato);
            if (cabeza == null)
            {
                // Lista vacía: el nuevo nodo es a la vez cabeza y cola.
                cabeza = nuevo;
                cola = nuevo;
            }
            else
            {
                // Se engancha el nuevo nodo después del último nodo actual.
                cola.Siguiente = nuevo;
                cola = nuevo;
            }
            cantidad++;
        }

        /// Inserta un nuevo dato al inicio de la lista (O(1)).
  
        public void AgregarInicio(T dato)
        {
            NodoLista<T> nuevo = new NodoLista<T>(dato);
            nuevo.Siguiente = cabeza;
            cabeza = nuevo;
            if (cola == null) cola = nuevo; // la lista estaba vacía: también es la cola
            cantidad++;
        }

        /// <summary>
        /// Recorre la lista de principio a fin y regresa un Vector
        ///  con todos los elementos, para poder mostrarlos
        /// fácilmente en la interfaz
        public Vector<T> Recorrer()
        {
            Vector<T> resultado = new Vector<T>(cantidad == 0 ? 1 : cantidad);
            NodoLista<T> actual = cabeza;
            while (actual != null)
            {
                resultado.Agregar(actual.Dato);
                actual = actual.Siguiente;
            }
            return resultado;
        }
    }
}
