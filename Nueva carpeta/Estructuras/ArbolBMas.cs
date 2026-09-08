using System;
using BibliotecaApp.Modelos;

namespace BibliotecaApp.Estructuras
{
    /// <summary>
    /// Nodo del Árbol B+. Si EsHoja = true, Valores contiene los libros y
    /// Siguiente enlaza con la hoja siguiente (para recorridos secuenciales).
    /// Si EsHoja = false, es un nodo interno de enrutamiento: Hijos[i] agrupa
    /// claves menores que Claves[i], y Hijos[i+1] agrupa claves mayores o iguales.
    ///

    public class NodoB
    {
        public const int MAX_CLAVES = 3; // Árbol B+ de orden 4 (máx. 4 hijos)

        public bool EsHoja;

        // +1 en cada arreglo: espacio extra "de desbordamiento" que permite
        // insertar temporalmente una clave/hijo de más ANTES de dividir el nodo.
        public int[] Claves = new int[MAX_CLAVES + 1];
        public Libro[] Valores = new Libro[MAX_CLAVES + 1]; // solo se usa si EsHoja
        public NodoB[] Hijos = new NodoB[MAX_CLAVES + 2];    // solo se usa si !EsHoja

        public int NumClaves;   // cuántas posiciones de Claves/Valores/Hijos están realmente ocupadas
        public NodoB Siguiente; // enlace entre hojas (propiedad clásica del B+, permite recorrer en orden)
        public NodoB Padre;     // referencia al nodo padre (facilita subir/bajar durante split y merge)

        public NodoB(bool esHoja)
        {
            EsHoja = esHoja;
        }
    }

    /// Justificación de uso: el catálogo se indexa por código único de libro y las
    /// operaciones más frecuentes del sistema son búsqueda puntual por código
    /// (registrar préstamo, consultar un libro) e inserción/eliminación de libros.
    /// El Árbol B+ ofrece O(log n) para estas operaciones y, adicionalmente,
    /// mantiene las hojas enlazadas entre sí, lo que permite recorrer todo el
    /// catálogo en orden de código de forma secuencial en O(n), ideal para
    /// generar listados completos sin necesidad de una lista aparte.
    public class ArbolBMas
    {
        private const int MAX_CLAVES = NodoB.MAX_CLAVES;
        private const int MIN_CLAVES = 1; // orden 4 -> mínimo ceil(4/2)-1 = 1

        private NodoB raiz; // punto de entrada al árbol; null significa árbol vacío

        public bool EstaVacio => raiz == null;

        // ---------------------------------------------------------------
        // BÚSQUEDA
        // ---------------------------------------------------------------

        /// Busca un libro por su código. Baja desde la raíz hasta la hoja
        /// correspondiente y recorre esa hoja en O(orden) buscando la clave.
        public Libro Buscar(int clave)
        {
            if (raiz == null) return null;
            NodoB hoja = EncontrarHoja(clave);
            for (int i = 0; i < hoja.NumClaves; i++)
                if (hoja.Claves[i] == clave) return hoja.Valores[i];
            return null; // no se encontró la clave en la hoja correspondiente
        }

        /// Desciende desde la raíz eligiendo en cada nodo interno el hijo
        /// correcto según la clave buscada, hasta llegar a una hoja.

        private NodoB EncontrarHoja(int clave)
        {
            NodoB actual = raiz;
            while (!actual.EsHoja)
            {
                // Avanza mientras la clave buscada sea mayor o igual que la
                // clave separadora i (o sea, busca el "hueco" correcto entre hijos).
                int i = 0;
                while (i < actual.NumClaves && clave >= actual.Claves[i]) i++;
                actual = actual.Hijos[i];
            }
            return actual;
        }

        // ---------------------------------------------------------------
        // INSERCIÓN
        // ---------------------------------------------------------------
        /// Inserta un libro bajo la clave indicada. Devuelve false si el
        /// código ya existe (no se permiten claves duplicadas).
        public bool Insertar(int clave, Libro valor)
        {
            if (Buscar(clave) != null) return false; // código duplicado, no se permite

            if (raiz == null)
            {
                // Árbol vacío: se crea la primera hoja, que también es la raíz.
                raiz = new NodoB(true);
                raiz.Claves[0] = clave;
                raiz.Valores[0] = valor;
                raiz.NumClaves = 1;
                return true;
            }

            NodoB hoja = EncontrarHoja(clave);
            InsertarOrdenadoEnHoja(hoja, clave, valor);

            // Si la hoja quedó con más claves de las permitidas, se divide.
            if (hoja.NumClaves > MAX_CLAVES)
                DividirHoja(hoja);

            return true;
        }

        /// Inserta la clave/valor dentro de una hoja manteniendo el orden
        /// ascendente, desplazando los elementos mayores una posición a la
        /// derecha (inserción ordenada estilo "insertion sort" de un paso).
        private void InsertarOrdenadoEnHoja(NodoB hoja, int clave, Libro valor)
        {
            int pos = hoja.NumClaves;
            while (pos > 0 && hoja.Claves[pos - 1] > clave)
            {
                hoja.Claves[pos] = hoja.Claves[pos - 1];
                hoja.Valores[pos] = hoja.Valores[pos - 1];
                pos--;
            }
            hoja.Claves[pos] = clave;
            hoja.Valores[pos] = valor;
            hoja.NumClaves++;
        }

        /// Divide una hoja que se desbordó (tiene MAX_CLAVES+1 elementos) en
        /// dos hojas, manteniendo el enlace "Siguiente" entre hojas y subiendo
        /// la clave separadora al nodo padre.
        private void DividirHoja(NodoB hoja)
        {
            NodoB nueva = new NodoB(true);
            int mid = hoja.NumClaves / 2; // con 4 claves -> mid = 2

            // Copia la mitad derecha de la hoja original a la nueva hoja.
            int j = 0;
            for (int i = mid; i < hoja.NumClaves; i++)
            {
                nueva.Claves[j] = hoja.Claves[i];
                nueva.Valores[j] = hoja.Valores[i];
                j++;
            }
            nueva.NumClaves = j;
            hoja.NumClaves = mid; // la hoja original se queda solo con la mitad izquierda

            // Mantiene la cadena de hojas enlazadas (propiedad del B+).
            nueva.Siguiente = hoja.Siguiente;
            hoja.Siguiente = nueva;

            // La primera clave de la nueva hoja sirve de separador en el padre.
            int claveSubir = nueva.Claves[0];
            InsertarEnPadre(hoja, claveSubir, nueva);
        }

        /// Inserta una clave separadora y el nuevo hijo derecho dentro del
        /// nodo padre de "izquierdo". Si "izquierdo" no tenía padre (era la
        /// raíz), se crea una nueva raíz interna que apunta a ambos.
        private void InsertarEnPadre(NodoB izquierdo, int clave, NodoB derecho)
        {
            if (izquierdo.Padre == null)
            {
                // El nodo dividido era la raíz: se crea una nueva raíz de altura +1.
                NodoB nuevaRaiz = new NodoB(false);
                nuevaRaiz.Claves[0] = clave;
                nuevaRaiz.Hijos[0] = izquierdo;
                nuevaRaiz.Hijos[1] = derecho;
                nuevaRaiz.NumClaves = 1;
                izquierdo.Padre = nuevaRaiz;
                derecho.Padre = nuevaRaiz;
                raiz = nuevaRaiz;
                return;
            }

            NodoB padre = izquierdo.Padre;
            int idx = IndiceEnPadre(padre, izquierdo);

            // Abre espacio en el arreglo de claves e hijos del padre,
            // desplazando los elementos existentes hacia la derecha.
            for (int i = padre.NumClaves; i > idx; i--)
                padre.Claves[i] = padre.Claves[i - 1];
            for (int i = padre.NumClaves + 1; i > idx + 1; i--)
                padre.Hijos[i] = padre.Hijos[i - 1];

            padre.Claves[idx] = clave;
            padre.Hijos[idx + 1] = derecho;
            derecho.Padre = padre;
            padre.NumClaves++;

            // Si el padre también se desbordó, se divide recursivamente hacia arriba.
            if (padre.NumClaves > MAX_CLAVES)
                DividirInterno(padre);
        }

        /// Divide un nodo interno desbordado en dos, subiendo la clave del
        /// medio al padre (a diferencia de la hoja, esta clave del medio NO
        /// se duplica, porque los nodos internos solo enrutan, no almacenan datos).
        private void DividirInterno(NodoB nodo)
        {
            int mid = nodo.NumClaves / 2; // con 4 claves -> mid = 2
            int claveSubir = nodo.Claves[mid];

            NodoB nuevo = new NodoB(false);

            // Copia las claves a la derecha del medio al nuevo nodo.
            int j = 0;
            for (int i = mid + 1; i < nodo.NumClaves; i++)
                nuevo.Claves[j++] = nodo.Claves[i];
            nuevo.NumClaves = j;

            // Copia también los hijos correspondientes y actualiza su padre.
            j = 0;
            for (int i = mid + 1; i <= nodo.NumClaves; i++)
            {
                nuevo.Hijos[j] = nodo.Hijos[i];
                nuevo.Hijos[j].Padre = nuevo;
                j++;
            }

            nodo.NumClaves = mid; // el nodo original se queda con la mitad izquierda

            InsertarEnPadre(nodo, claveSubir, nuevo);
        }

        /// Busca en qué posición del arreglo Hijos del padre está el nodo hijo dado.
        private int IndiceEnPadre(NodoB padre, NodoB hijo)
        {
            for (int i = 0; i <= padre.NumClaves; i++)
                if (padre.Hijos[i] == hijo) return i;
            return -1; // no debería ocurrir si la estructura está consistente
        }

        // ---------------------------------------------------------------
        // ELIMINACIÓN
        // ---------------------------------------------------------------

        /// Elimina el libro con la clave indicada. Devuelve false si no existe.
        /// Tras quitar la clave de la hoja, si el nodo queda por debajo del
        /// mínimo permitido, se rebalancea (préstamo o fusión con hermanos).
        public bool Eliminar(int clave)
        {
            if (raiz == null) return false;

            NodoB hoja = EncontrarHoja(clave);
            int idx = -1;
            for (int i = 0; i < hoja.NumClaves; i++)
                if (hoja.Claves[i] == clave) { idx = i; break; }
            if (idx == -1) return false; // la clave no existe en el árbol

            // Desplaza los elementos posteriores una posición a la izquierda
            // para "tapar" el hueco dejado por el elemento eliminado.
            for (int i = idx; i < hoja.NumClaves - 1; i++)
            {
                hoja.Claves[i] = hoja.Claves[i + 1];
                hoja.Valores[i] = hoja.Valores[i + 1];
            }
            hoja.NumClaves--;

            // Si se eliminó la clave más pequeña de la hoja, hay que corregir
            // el separador correspondiente en los ancestros.
            if (idx == 0 && hoja.NumClaves > 0)
                ActualizarSeparadorAncestro(hoja);

            if (hoja == raiz)
            {
                // Caso especial: la raíz es también una hoja (árbol pequeño).
                if (hoja.NumClaves == 0) raiz = null;
                return true;
            }

            // Si la hoja quedó por debajo del mínimo de claves, se rebalancea.
            if (hoja.NumClaves < MIN_CLAVES)
                ManejarSubdesbordamiento(hoja);

            return true;
        }

        /// Sube por los ancestros corrigiendo la primera clave separadora que
        /// haga referencia al valor mínimo antiguo de la hoja, reemplazándola
        /// por el nuevo valor mínimo tras la eliminación.
        private void ActualizarSeparadorAncestro(NodoB hoja)
        {
            int nuevaMinima = hoja.Claves[0];
            NodoB actual = hoja;
            NodoB padre = actual.Padre;
            while (padre != null)
            {
                int idx = IndiceEnPadre(padre, actual);
                if (idx > 0)
                {
                    padre.Claves[idx - 1] = nuevaMinima;
                    return;
                }
                actual = padre;
                padre = actual.Padre;
            }
        }

        /// Corrige un nodo que quedó con menos claves de las permitidas,
        /// intentando primero "pedir prestado" a un hermano vecino y, si
        /// ninguno tiene de sobra, fusionándolo con un hermano.
        private void ManejarSubdesbordamiento(NodoB nodo)
        {
            if (nodo == raiz)
            {
                // La raíz puede tener menos del mínimo de claves sin problema,
                // salvo que se quede completamente vacía.
                if (nodo.NumClaves == 0)
                {
                    if (!nodo.EsHoja)
                    {
                        // La raíz interna se queda sin claves: su único hijo pasa a ser la nueva raíz.
                        raiz = nodo.Hijos[0];
                        raiz.Padre = null;
                    }
                    else
                    {
                        raiz = null; // el árbol quedó completamente vacío
                    }
                }
                return;
            }

            NodoB padre = nodo.Padre;
            int idx = IndiceEnPadre(padre, nodo);

            // 1) Intenta pedir prestado al hermano izquierdo si tiene claves de sobra.
            if (idx > 0 && padre.Hijos[idx - 1].NumClaves > MIN_CLAVES)
            {
                PrestarDeIzquierda(padre, idx, padre.Hijos[idx - 1], nodo);
                return;
            }
            // 2) Si no, intenta pedir prestado al hermano derecho.
            if (idx < padre.NumClaves && padre.Hijos[idx + 1].NumClaves > MIN_CLAVES)
            {
                PrestarDeDerecha(padre, idx, nodo, padre.Hijos[idx + 1]);
                return;
            }

            // 3) Ningún hermano tiene de sobra: se fusiona con uno de ellos
            //    (esto puede propagar el subdesbordamiento hacia el padre).
            if (idx > 0)
                Fusionar(padre, idx - 1, padre.Hijos[idx - 1], nodo);
            else
                Fusionar(padre, idx, nodo, padre.Hijos[idx + 1]);
        }

        /// Toma prestada la última clave/hijo del hermano izquierdo y la
        /// mueve al inicio de "nodo", actualizando el separador en el padre.

        private void PrestarDeIzquierda(NodoB padre, int idx, NodoB izq, NodoB nodo)
        {
            if (nodo.EsHoja)
            {
                // Abre espacio al inicio de la hoja desplazando todo a la derecha.
                for (int i = nodo.NumClaves; i > 0; i--)
                {
                    nodo.Claves[i] = nodo.Claves[i - 1];
                    nodo.Valores[i] = nodo.Valores[i - 1];
                }
                nodo.Claves[0] = izq.Claves[izq.NumClaves - 1];
                nodo.Valores[0] = izq.Valores[izq.NumClaves - 1];
                nodo.NumClaves++;
                izq.NumClaves--;
                padre.Claves[idx - 1] = nodo.Claves[0]; // nuevo separador
            }
            else
            {
                // Mismo principio pero moviendo también un hijo (nodo interno).
                for (int i = nodo.NumClaves; i > 0; i--)
                    nodo.Claves[i] = nodo.Claves[i - 1];
                for (int i = nodo.NumClaves + 1; i > 0; i--)
                    nodo.Hijos[i] = nodo.Hijos[i - 1];

                nodo.Claves[0] = padre.Claves[idx - 1];
                nodo.Hijos[0] = izq.Hijos[izq.NumClaves];
                nodo.Hijos[0].Padre = nodo;
                nodo.NumClaves++;

                padre.Claves[idx - 1] = izq.Claves[izq.NumClaves - 1];
                izq.NumClaves--;
            }
        }

        /// Toma prestada la primera clave/hijo del hermano derecho y la
        /// mueve al final de "nodo", actualizando el separador en el padre.

        private void PrestarDeDerecha(NodoB padre, int idx, NodoB nodo, NodoB der)
        {
            if (nodo.EsHoja)
            {
                nodo.Claves[nodo.NumClaves] = der.Claves[0];
                nodo.Valores[nodo.NumClaves] = der.Valores[0];
                nodo.NumClaves++;

                // Cierra el hueco dejado en el hermano derecho.
                for (int i = 0; i < der.NumClaves - 1; i++)
                {
                    der.Claves[i] = der.Claves[i + 1];
                    der.Valores[i] = der.Valores[i + 1];
                }
                der.NumClaves--;

                padre.Claves[idx] = der.Claves[0]; // nuevo separador
            }
            else
            {
                nodo.Claves[nodo.NumClaves] = padre.Claves[idx];
                nodo.Hijos[nodo.NumClaves + 1] = der.Hijos[0];
                nodo.Hijos[nodo.NumClaves + 1].Padre = nodo;
                nodo.NumClaves++;

                padre.Claves[idx] = der.Claves[0];

                for (int i = 0; i < der.NumClaves - 1; i++)
                    der.Claves[i] = der.Claves[i + 1];
                for (int i = 0; i < der.NumClaves; i++)
                    der.Hijos[i] = der.Hijos[i + 1];
                der.NumClaves--;
            }
        }

        /// Fusiona dos hermanos (izq y der) en un solo nodo cuando ninguno
        /// tenía claves de sobra para prestar. Si el padre queda por debajo
        /// del mínimo tras la fusión, el subdesbordamiento se propaga hacia arriba.

        private void Fusionar(NodoB padre, int idxIzq, NodoB izq, NodoB der)
        {
            if (izq.EsHoja)
            {
                // Copia todas las claves/valores de "der" al final de "izq".
                for (int i = 0; i < der.NumClaves; i++)
                {
                    izq.Claves[izq.NumClaves + i] = der.Claves[i];
                    izq.Valores[izq.NumClaves + i] = der.Valores[i];
                }
                izq.NumClaves += der.NumClaves;
                izq.Siguiente = der.Siguiente; // mantiene la cadena de hojas enlazadas
            }
            else
            {
                // En nodos internos, la clave separadora del padre "baja" al
                // nodo fusionado antes de copiar las claves de "der".
                izq.Claves[izq.NumClaves] = padre.Claves[idxIzq];
                izq.NumClaves++;

                for (int i = 0; i < der.NumClaves; i++)
                    izq.Claves[izq.NumClaves + i] = der.Claves[i];
                for (int i = 0; i <= der.NumClaves; i++)
                {
                    izq.Hijos[izq.NumClaves + i] = der.Hijos[i];
                    izq.Hijos[izq.NumClaves + i].Padre = izq;
                }
                izq.NumClaves += der.NumClaves;
            }

            // Quita del padre la clave separadora y el hijo derecho que ya se fusionaron.
            for (int i = idxIzq; i < padre.NumClaves - 1; i++)
                padre.Claves[i] = padre.Claves[i + 1];
            for (int i = idxIzq + 1; i < padre.NumClaves; i++)
                padre.Hijos[i] = padre.Hijos[i + 1];
            padre.NumClaves--;

            if (padre == raiz)
            {
                // Si la raíz se quedó sin claves tras la fusión, el nodo
                // fusionado ("izq") pasa a ser la nueva raíz.
                if (padre.NumClaves == 0)
                {
                    raiz = izq;
                    izq.Padre = null;
                }
            }
            else if (padre.NumClaves < MIN_CLAVES)
            {
                // El padre también quedó por debajo del mínimo: se rebalancea recursivamente.
                ManejarSubdesbordamiento(padre);
            }
        }

        // ---------------------------------------------------------------
        // RECORRIDOS
        // ---------------------------------------------------------------

        /// Recorre todas las hojas en orden ascendente de código, usando el
        /// enlace entre hojas (propiedad característica del Árbol B+). Este
        /// recorrido es O(n) y no requiere volver a bajar desde la raíz en
        /// cada paso, ya que las hojas están encadenadas entre sí.
        /// El resultado se acumula en un Vector propio (no List&lt;T&gt;).

        public Vector<Libro> RecorrerEnOrden()
        {
            Vector<Libro> resultado = new Vector<Libro>();
            if (raiz == null) return resultado;

            // Desciende siempre por el hijo más a la izquierda hasta llegar
            // a la primera hoja (la que contiene las claves más pequeñas).
            NodoB actual = raiz;
            while (!actual.EsHoja) actual = actual.Hijos[0];

            // Recorre la cadena de hojas de izquierda a derecha.
            while (actual != null)
            {
                for (int i = 0; i < actual.NumClaves; i++)
                    resultado.Agregar(actual.Valores[i]);
                actual = actual.Siguiente;
            }
            return resultado;
        }
    }
}
