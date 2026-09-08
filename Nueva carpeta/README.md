# Sistema de Catálogo de Biblioteca — Estructura de Datos II

Proyecto de Laboratorio No. 1 (escenario **Catálogo de biblioteca**), desarrollado en
**C#** con interfaz gráfica **Windows Forms**.

## 1. Cómo compilar y ejecutar

Este proyecto usa Windows Forms, por lo que **requiere Windows** para compilarse y
ejecutarse (Windows Forms no corre en Linux/Mac). Necesitas el **.NET 8 SDK**
(descárgalo de https://dotnet.microsoft.com si no lo tienes).

Desde la carpeta del proyecto (donde está `BibliotecaApp.csproj`):

```bash
dotnet restore
dotnet build
dotnet run
```

También puedes abrir la carpeta directamente con **Visual Studio 2022** (con la
carga de trabajo "Desarrollo de escritorio de .NET") y presionar F5.

Al iniciar, la aplicación abre con el catálogo vacío. Ve a la pestaña **Archivo**
y carga el archivo de ejemplo `Data/libros.csv` (o ingresa libros manualmente en
la pestaña **Catálogo**) para empezar a probar todas las funciones.

## 2. Estructuras de datos utilizadas y justificación

| Estructura | Dónde se usa | Por qué |
|---|---|---|
| **Árbol B+** (`Estructuras/ArbolBMas.cs`) | Índice principal del catálogo, llave = código único del libro | Las operaciones más frecuentes son búsqueda puntual por código (al prestar, devolver o consultar un libro) e inserción/eliminación. El Árbol B+ da O(log n) en las tres. Además, sus hojas están enlazadas entre sí, lo que permite recorrer *todo* el catálogo en orden de código en O(n) sin necesitar una estructura adicional para el listado general. |
| **Max Heap** (`Estructuras/MaxHeap.cs`) | Reporte "Top N libros más prestados" | Se necesita el/los elemento(s) de mayor `VecesPrestado` sin mantener todo ordenado permanentemente. Construir el heap es O(n) y cada extracción del máximo O(log n): más eficiente que ordenar todo el catálogo cuando solo interesa un top-N. |
| **Min Heap** (`Estructuras/MinHeap.cs`) | Reporte "Alerta de bajo stock" (libros con menos copias disponibles) | Misma lógica que el Max Heap pero para encontrar los valores mínimos de `CopiasDisponibles`, útil para decidir qué libros reabastecer primero. |
| **Lista enlazada simple** (`Estructuras/ListaEnlazada.cs`) | Historial de préstamos de cada libro (`Libro.HistorialPrestamos`) | Cada libro puede tener múltiples préstamos a lo largo del tiempo. Solo se necesita insertar al final y recorrer secuencialmente para mostrar el historial; no se requiere búsqueda binaria ni acceso aleatorio, por lo que una lista enlazada es suficiente y más simple que un árbol. |
| **Vector propio** (`Estructuras/Vector.cs`) | Arreglo dinámico auxiliar usado para recolectar resultados a imprimir (listados, resultados de recorridos) y para el ordenamiento por título (merge sort implementado a mano) | Se implementó en lugar de `List<T>` para no depender de colecciones nativas de .NET en ninguna parte de la lógica de negocio, cumpliendo el requisito de implementación propia. |

**Nota sobre el requisito de implementación propia:** ninguna de las estructuras
obligatorias (Árbol B+, Min Heap, Max Heap) usa `List<T>`, `Dictionary`,
`SortedSet`, `SortedDictionary` ni `PriorityQueue<TElement,TPriority>` de .NET. Los
únicos tipos nativos usados son arreglos (`T[]`), `string`, `DateTime` y las clases
de E/S de archivos (`File`, `StreamReader`/`StreamWriter`) para la carga y guardado
de datos, tal como lo permite el enunciado.

## 3. Estructura del proyecto

```
BibliotecaApp/
├── BibliotecaApp.csproj
├── Program.cs                  -> punto de entrada de la app (WinForms)
├── Modelos/
│   ├── Libro.cs
│   └── Prestamo.cs
├── Estructuras/
│   ├── Vector.cs                -> arreglo dinámico propio
│   ├── ListaEnlazada.cs         -> lista enlazada propia
│   ├── ArbolBMas.cs             -> Árbol B+ (orden 4) propio
│   ├── MaxHeap.cs               -> Max Heap propio
│   └── MinHeap.cs               -> Min Heap propio
├── Servicios/
│   └── BibliotecaService.cs     -> lógica de negocio (usa las estructuras)
├── UI/
│   └── MainForm.cs              -> interfaz gráfica (Windows Forms)
└── Data/
    └── libros.csv               -> datos de ejemplo para carga dinámica
```

## 4. Funcionalidades que cubre la interfaz

- **Catálogo:** registrar libro, buscar libro por código, eliminar libro.
- **Préstamos:** registrar préstamo, registrar devolución, ver historial de
  préstamos de un libro específico.
- **Listados:** listado completo ordenado por código (recorrido de hojas del
  Árbol B+) y listado ordenado por título (merge sort propio).
- **Reportes:** top N libros más prestados (Max Heap) y alerta de bajo stock
  (Min Heap), con N configurable.
- **Archivo:** carga de libros desde un archivo `.csv` (datos dinámicos, no
  quemados en el código) y guardado del catálogo actual a `.csv`.

## 5. Formato del archivo CSV

```
codigo;titulo;autor;categoria;copiasTotales
101;Cien años de soledad;Gabriel García Márquez;Novela;3
```

También acepta un formato extendido de 7 columnas (agregando `copiasDisponibles`
y `vecesPrestado` al final) para restaurar un catálogo previamente guardado desde
la propia aplicación.

## 6. Pendiente para la entrega completa

Este repositorio cubre el código fuente y su documentación técnica en este
README. Para la entrega según el enunciado aún debes preparar, si no lo has
hecho:
- Manual técnico en PDF (diagrama de flujo, diagrama de clases, justificación
  ampliada) — puedo ayudarte a generarlo.
- Manual de usuario con capturas de pantalla del programa en ejecución.
- Preparar tu defensa individual: asegúrate de poder explicar cómo funciona
  la inserción/eliminación del Árbol B+ y el heapify de los montículos, ya que
  son la base de la calificación del criterio de "Presentación y dominio
  individual".
