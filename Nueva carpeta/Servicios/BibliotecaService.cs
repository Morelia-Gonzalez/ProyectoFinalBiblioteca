using System;
using System.IO;             // Soporte auxiliar: lectura/escritura de archivos CSV (File, StreamWriter).
using System.Globalization;  // (no se usa activamente, se deja por si se requiere formateo regional de números/fechas)
using BibliotecaApp.Estructuras;
using BibliotecaApp.Modelos;

namespace BibliotecaApp.Servicios
{

    /// Orquesta el catálogo de biblioteca: mantiene el Árbol B+ como estructura
    /// principal de almacenamiento/indexación por código, y construye los
    /// montículos (Min/Max) bajo demanda para generar reportes.
    ///

    public class BibliotecaService
    {
        private readonly ArbolBMas indice; // estructura principal, llave = código de libro
        private int totalLibros;           // contador simple, no una colección

        public BibliotecaService()
        {
            indice = new ArbolBMas();
            totalLibros = 0;
        }

        // ---------------------------------------------------------------
        // OPERACIONES BÁSICAS SOBRE EL CATÁLOGO
        // ---------------------------------------------------------------

   
        /// Valida los datos de entrada y, si son correctos, crea un nuevo
        /// Libro y lo inserta en el Árbol B+ usando el código como clave.
   
        public (bool ok, string mensaje) RegistrarLibro(int codigo, string titulo, string autor, string categoria, int copias)
        {
            // --- Validaciones básicas antes de tocar la estructura ---
            if (codigo <= 0) return (false, "El código debe ser un número positivo.");
            if (string.IsNullOrWhiteSpace(titulo)) return (false, "El título es obligatorio.");
            if (copias < 0) return (false, "La cantidad de copias no puede ser negativa.");

            Libro libro = new Libro(codigo, titulo.Trim(), autor?.Trim() ?? "", categoria?.Trim() ?? "", copias);

            // Insertar() ya valida internamente que el código no esté duplicado (Árbol B+).
            bool insertado = indice.Insertar(codigo, libro);
            if (!insertado) return (false, $"Ya existe un libro con el código {codigo}.");

            totalLibros++;
            return (true, $"Libro '{titulo}' registrado correctamente con código {codigo}.");
        }

        // Búsqueda puntual por código, delegada directamente al Árbol B+ (O(log n)).
        public Libro BuscarLibro(int codigo) => indice.Buscar(codigo);

        
        /// Elimina un libro del catálogo por su código.
  
        public (bool ok, string mensaje) EliminarLibro(int codigo)
        {
            bool eliminado = indice.Eliminar(codigo);
            if (eliminado) totalLibros--;
            return eliminado
                ? (true, $"Libro con código {codigo} eliminado del catálogo.")
                : (false, $"No existe un libro con código {codigo}.");
        }

        // Listado completo en orden de código, aprovechando el enlace entre
        // hojas del Árbol B+ (recorrido secuencial, sin volver a bajar del árbol).
        public Vector<Libro> ListadoCompleto() => indice.RecorrerEnOrden();


        /// Listado de todo el catálogo ordenado por título, usando ordenamiento
        /// por mezcla (merge sort) implementado a mano sobre el Vector propio.

        public Vector<Libro> ListadoOrdenadoPorTitulo()
        {
            Vector<Libro> libros = indice.RecorrerEnOrden();
            libros.OrdenarPorMezcla((a, b) => string.Compare(a.Titulo, b.Titulo, StringComparison.OrdinalIgnoreCase));
            return libros;
        }

        // ---------------------------------------------------------------
        // PRÉSTAMOS Y DEVOLUCIONES
        // ---------------------------------------------------------------


        /// Registra un nuevo préstamo para el libro indicado, guardando el
        /// nombre de la persona que lo solicita (nombrePrestatario).
        /// Descuenta una copia disponible y agrega el préstamo al historial
        /// del libro (lista enlazada propia).
        /// <param name="codigo">Código del libro a prestar.</param>
        /// <param name="nombrePrestatario">Nombre de quien retira el libro.</param>
        public (bool ok, string mensaje) RegistrarPrestamo(int codigo, string nombrePrestatario)
        {
            Libro libro = indice.Buscar(codigo);
            if (libro == null) return (false, $"No existe un libro con código {codigo}.");
            if (string.IsNullOrWhiteSpace(nombrePrestatario)) return (false, "Debe indicar el nombre de quien presta el libro.");
            if (libro.CopiasDisponibles <= 0) return (false, $"No hay copias disponibles de '{libro.Titulo}'.");

            libro.CopiasDisponibles--;
            libro.VecesPrestado++;

            // Se agrega el nuevo préstamo (con su nombre) al final del historial del libro.
            libro.HistorialPrestamos.AgregarFinal(new Prestamo(codigo, nombrePrestatario.Trim(), DateTime.Now));

            return (true, $"Préstamo registrado para '{libro.Titulo}' a nombre de {nombrePrestatario}. Copias disponibles ahora: {libro.CopiasDisponibles}.");
        }

        /// Registra la devolución de un ejemplar. Se busca en el historial
        /// (recorriendo la lista enlazada manualmente) el préstamo pendiente
        /// más antiguo que coincida con el nombre del prestatario indicado.

        /// <param name="codigo">Código del libro que se devuelve.</param>
        /// <param name="nombrePrestatario">Nombre de quien había pedido el libro, para identificar el préstamo correcto.</param>
        public (bool ok, string mensaje) RegistrarDevolucion(int codigo, string nombrePrestatario)
        {
            Libro libro = indice.Buscar(codigo);
            if (libro == null) return (false, $"No existe un libro con código {codigo}.");
            if (string.IsNullOrWhiteSpace(nombrePrestatario)) return (false, "Debe indicar el nombre de quien presta el libro.");

            // Recorrido manual de la lista enlazada (NodoLista<Prestamo>) buscando
            // el préstamo pendiente más antiguo cuyo nombre coincida.
            NodoLista<Prestamo> actual = libro.HistorialPrestamos.Cabeza;
            Prestamo pendiente = null;
            while (actual != null)
            {
                bool coincideNombre = string.Equals(
                    actual.Dato.NombrePrestatario?.Trim(),
                    nombrePrestatario.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                if (!actual.Dato.Devuelto && coincideNombre) { pendiente = actual.Dato; break; }
                actual = actual.Siguiente;
            }

            if (pendiente == null)
                return (false, $"No hay préstamos pendientes de devolución para '{libro.Titulo}' a nombre de {nombrePrestatario}.");

            pendiente.FechaDevolucion = DateTime.Now;
            if (libro.CopiasDisponibles < libro.CopiasTotales) libro.CopiasDisponibles++;

            return (true, $"Devolución registrada para '{libro.Titulo}' (prestado a {nombrePrestatario}). Copias disponibles ahora: {libro.CopiasDisponibles}.");
        }

        // ---------------------------------------------------------------
        // REPORTES CON MONTÍCULOS
        // ---------------------------------------------------------------
        /// Top N de libros más prestados usando un Max Heap construido sobre
        /// el campo VecesPrestado.
        public Vector<Libro> TopMasPrestados(int n)
        {
            Vector<Libro> todos = indice.RecorrerEnOrden();
            MaxHeap heap = MaxHeap.ConstruirDesde(todos, l => l.VecesPrestado);

            Vector<Libro> resultado = new Vector<Libro>(n);
            for (int i = 0; i < n && heap.Cantidad > 0; i++)
                resultado.Agregar(heap.ExtraerMaximo());
            return resultado;
        }


        /// Libros con menor cantidad de copias disponibles (alerta de reabastecimiento)
        /// usando un Min Heap construido sobre el campo CopiasDisponibles (sin PriorityQueue de .NET).
  
        public Vector<Libro> AlertaBajoStock(int n)
        {
            Vector<Libro> todos = indice.RecorrerEnOrden();
            MinHeap heap = MinHeap.ConstruirDesde(todos, l => l.CopiasDisponibles);

            Vector<Libro> resultado = new Vector<Libro>(n);
            for (int i = 0; i < n && heap.Cantidad > 0; i++)
                resultado.Agregar(heap.ExtraerMinimo());
            return resultado;
        }

        public int TotalLibros => totalLibros;

        // ---------------------------------------------------------------
        // CARGA / GUARDADO EN ARCHIVO (datos dinámicos, no quemados en código)
        // ---------------------------------------------------------------

        /// Carga libros desde un archivo CSV con formato:
        /// codigo;titulo;autor;categoria;copiasTotales[;copiasDisponibles;vecesPrestado]
        /// Las dos últimas columnas son opcionales (para restaurar un catálogo ya usado).

        public (int cargados, int errores, Vector<string> detalleErrores) CargarDesdeCsv(string rutaArchivo)
        {
            Vector<string> errores = new Vector<string>();
            int cargados = 0;

            if (!File.Exists(rutaArchivo))
            {
                errores.Agregar($"El archivo '{rutaArchivo}' no existe.");
                return (0, 1, errores);
            }

            string[] lineas = File.ReadAllLines(rutaArchivo); // soporte auxiliar de E/S
            for (int i = 0; i < lineas.Length; i++)
            {
                string linea = lineas[i].Trim();
                if (linea.Length == 0) continue; // ignora líneas vacías
                if (i == 0 && linea.StartsWith("codigo", StringComparison.OrdinalIgnoreCase)) continue; // encabezado opcional

                string[] campos = linea.Split(';'); // soporte auxiliar de manejo de cadenas
                if (campos.Length < 5)
                {
                    errores.Agregar($"Línea {i + 1}: formato inválido (se esperan al menos 5 campos).");
                    continue;
                }

                try
                {
                    int codigo = int.Parse(campos[0].Trim());
                    string titulo = campos[1].Trim();
                    string autor = campos[2].Trim();
                    string categoria = campos[3].Trim();
                    int copiasTotales = int.Parse(campos[4].Trim());

                    Libro libro = new Libro(codigo, titulo, autor, categoria, copiasTotales);

                    // Columnas opcionales: si vienen, se respeta el estado guardado previamente.
                    if (campos.Length >= 7)
                    {
                        libro.CopiasDisponibles = int.Parse(campos[5].Trim());
                        libro.VecesPrestado = int.Parse(campos[6].Trim());
                    }

                    // La inserción real en la estructura de datos sigue siendo el Árbol B+.
                    if (indice.Insertar(codigo, libro))
                    {
                        cargados++;
                        totalLibros++;
                    }
                    else
                    {
                        errores.Agregar($"Línea {i + 1}: código {codigo} duplicado, se omitió.");
                    }
                }
                catch (Exception ex)
                {
                    errores.Agregar($"Línea {i + 1}: {ex.Message}");
                }
            }

            return (cargados, errores.Cantidad, errores);
        }

 
        /// Guarda el catálogo completo en un archivo CSV, recorriendo el
        /// Árbol B+ en orden mediante RecorrerEnOrden() (Vector propio).
        /// StreamWriter es soporte auxiliar de E/S, no una estructura de datos.
        public bool GuardarEnCsv(string rutaArchivo)
        {
            try
            {
                Vector<Libro> todos = indice.RecorrerEnOrden();
                using (StreamWriter escritor = new StreamWriter(rutaArchivo, false))
                {
                    escritor.WriteLine("codigo;titulo;autor;categoria;copiasTotales;copiasDisponibles;vecesPrestado");
                    for (int i = 0; i < todos.Cantidad; i++)
                        escritor.WriteLine(todos[i].ToCsvLine());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
