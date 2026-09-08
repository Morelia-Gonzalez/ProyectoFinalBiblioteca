using System;
using System.Drawing;
using System.Windows.Forms;
using BibliotecaApp.Estructuras;
using BibliotecaApp.Modelos;
using BibliotecaApp.Servicios;

namespace BibliotecaApp.UI
{
  
    /// Ventana principal de la aplicación. Contiene únicamente lógica de
    /// interfaz (armado de controles, eventos de clic, formateo de texto).
    /// TODA la lógica de negocio y de estructuras de datos vive en
    /// BibliotecaService y en las clases de Estructuras/, esta clase solo
    /// las consume a través de "servicio".

    public class MainForm : Form
    {
        // Única instancia del servicio que orquesta el catálogo (Árbol B+,
        // heaps, listas enlazadas, etc.). La ventana no conoce esos detalles.
        private readonly BibliotecaService servicio = new BibliotecaService();

        // --- Paleta de colores (igual a la maqueta azul proporcionada) ---
        private readonly Color ColorBarra = ColorTranslator.FromHtml("#2E6099");             // azul de pestañas/botones
        private readonly Color ColorBarraSeleccionada = ColorTranslator.FromHtml("#3E7CB8"); // azul más claro para la pestaña activa
        private readonly Color ColorBarraTexto = Color.White;                                // texto de las pestañas
        private readonly Color ColorEncabezadoTabla = ColorTranslator.FromHtml("#DCE9F6");   // azul claro de encabezados de tabla

        // Controles compartidos por toda la ventana.
        private TabControl tabs;   // contenedor de las 5 pestañas del sistema
        private Label lblEstado;   // barra de estado inferior (mensajes de éxito/error)

        // --- Controles de la pestaña "Catálogo" ---
        private TextBox txtCodigo, txtTitulo, txtAutor, txtCategoria, txtCopias; // formulario de registro
        private TextBox txtCodigoBuscar;      // código a buscar
        private TextBox txtCodigoEliminar;    // código a eliminar
        private TextBox txtResultadoBusqueda; // muestra el detalle del libro encontrado

        // --- Controles de la pestaña "Préstamos" ---
        private TextBox txtCodigoPrestamo, txtCodigoDevolucion;     // código del libro
        private TextBox txtNombrePrestamo, txtNombreDevolucion;     // nombre de quien presta el libro
        private ListBox lstHistorial;         // historial de préstamos de un libro
        private TextBox txtCodigoHistorial;   // código a consultar en el historial

        // --- Controles de la pestaña "Listados" ---
        private DataGridView gridListado; // tabla con el catálogo completo

        // --- Controles de la pestaña "Reportes" ---
        private DataGridView gridReporte; // tabla con el top-N generado
        private NumericUpDown numTopN;    // cuántos elementos mostrar en el reporte

        // --- Controles de la pestaña "Archivo" ---
        private TextBox txtRutaArchivo; // ruta del CSV a cargar/guardar
        private TextBox txtLogArchivo;  // bitácora de la carga/guardado

        /// <summary>
        /// Constructor: configura las propiedades básicas de la ventana y
        /// delega el armado de toda la interfaz a ConstruirInterfaz().
        /// </summary>
        public MainForm()
        {
            Text = "Sistema de Catálogo de Biblioteca - Estructura de Datos II";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            ConstruirInterfaz();
        }

        /// <summary>
        /// Crea el TabControl con las 5 pestañas del sistema y la barra de
        /// estado inferior, y los agrega a la ventana.
        /// </summary>
        private void ConstruirInterfaz()
        {
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed, // permite dibujar las pestañas manualmente (para pintarlas de azul)
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(160, 32)
            };
            tabs.DrawItem += Tabs_DrawItem; // evento que pinta cada pestaña

            tabs.TabPages.Add(ConstruirTabCatalogo());
            tabs.TabPages.Add(ConstruirTabPrestamos());
            tabs.TabPages.Add(ConstruirTabListados());
            tabs.TabPages.Add(ConstruirTabReportes());
            tabs.TabPages.Add(ConstruirTabArchivo());

            lblEstado = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.WhiteSmoke,
                Text = "Listo."
            };

            Controls.Add(tabs);
            Controls.Add(lblEstado);
        }

        /// <summary>
        /// Dibuja manualmente el fondo y el texto de cada pestaña del
        /// TabControl, usando el color azul de la maqueta (la pestaña activa
        /// se pinta con un tono más claro para distinguirla).
        /// </summary>
        private void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage pagina = tabs.TabPages[e.Index];
            Rectangle rect = tabs.GetTabRect(e.Index);
            bool seleccionada = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (SolidBrush fondo = new SolidBrush(seleccionada ? ColorBarraSeleccionada : ColorBarra))
                e.Graphics.FillRectangle(fondo, rect);

            TextRenderer.DrawText(e.Graphics, pagina.Text, tabs.Font, rect, ColorBarraTexto,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>
        /// Aplica el estilo azul plano (fondo azul, texto blanco, sin borde)
        /// a un botón, para que combine con el resto de la interfaz.
        /// </summary>
        private void EstilizarBoton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ColorBarra;
            btn.ForeColor = Color.White;
            btn.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Muestra un mensaje en la barra de estado inferior, en verde si es
        /// informativo o en rojo oscuro si representa un error.
        /// </summary>
        private void MostrarEstado(string mensaje, bool esError = false)
        {
            lblEstado.Text = mensaje;
            lblEstado.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
        }

        // =================================================================
        // TAB 1: CATÁLOGO (registrar / buscar / eliminar libros)
        // =================================================================

        /// <summary>
        /// Arma la pestaña "Catálogo": formulario de registro, buscador por
        /// código y eliminación de libros.
        /// </summary>
        private TabPage ConstruirTabCatalogo()
        {
            TabPage tab = new TabPage("Catálogo");

            // --- Panel: registrar libro ---
            GroupBox grpRegistrar = new GroupBox { Text = "Registrar libro", Left = 15, Top = 15, Width = 430, Height = 240 };
            int y = 30;
            AgregarCampo(grpRegistrar, "Código:", ref txtCodigo, y); y += 32;
            AgregarCampo(grpRegistrar, "Título:", ref txtTitulo, y); y += 32;
            AgregarCampo(grpRegistrar, "Autor:", ref txtAutor, y); y += 32;
            AgregarCampo(grpRegistrar, "Categoría:", ref txtCategoria, y); y += 32;
            AgregarCampo(grpRegistrar, "Cantidad de copias:", ref txtCopias, y); y += 40;

            Button btnRegistrar = new Button { Text = "Registrar libro", Left = 130, Top = y, Width = 160, Height = 32 };
            EstilizarBoton(btnRegistrar);
            btnRegistrar.Click += (s, e) => RegistrarLibro();
            grpRegistrar.Controls.Add(btnRegistrar);

            // --- Panel: buscar libro por código ---
            GroupBox grpBuscar = new GroupBox { Text = "Buscar libro por código", Left = 460, Top = 15, Width = 480, Height = 240 };
            Label lblCB = new Label { Text = "Código:", Left = 15, Top = 30, Width = 60 };
            txtCodigoBuscar = new TextBox { Left = 80, Top = 27, Width = 100 };
            Button btnBuscar = new Button { Text = "Buscar", Left = 190, Top = 25, Width = 90, Height = 28 };
            EstilizarBoton(btnBuscar);
            btnBuscar.Click += (s, e) => BuscarLibro();
            txtResultadoBusqueda = new TextBox
            {
                Left = 15, Top = 65, Width = 445, Height = 160,
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
            };
            grpBuscar.Controls.Add(lblCB);
            grpBuscar.Controls.Add(txtCodigoBuscar);
            grpBuscar.Controls.Add(btnBuscar);
            grpBuscar.Controls.Add(txtResultadoBusqueda);

            // --- Panel: eliminar libro ---
            GroupBox grpEliminar = new GroupBox { Text = "Eliminar libro", Left = 15, Top = 270, Width = 430, Height = 90 };
            Label lblCE = new Label { Text = "Código:", Left = 15, Top = 33, Width = 60 };
            txtCodigoEliminar = new TextBox { Left = 80, Top = 30, Width = 100 };
            Button btnEliminar = new Button { Text = "Eliminar", Left = 190, Top = 28, Width = 90, Height = 28 };
            EstilizarBoton(btnEliminar);
            btnEliminar.Click += (s, e) => EliminarLibro();
            grpEliminar.Controls.Add(lblCE);
            grpEliminar.Controls.Add(txtCodigoEliminar);
            grpEliminar.Controls.Add(btnEliminar);

            tab.Controls.Add(grpRegistrar);
            tab.Controls.Add(grpBuscar);
            tab.Controls.Add(grpEliminar);
            return tab;
        }

        /// <summary>
        /// Helper que crea una etiqueta + un TextBox alineados dentro de un
        /// contenedor, evitando repetir el mismo código para cada campo del
        /// formulario de registro.
        /// </summary>
        private void AgregarCampo(Control padre, string etiqueta, ref TextBox caja, int top)
        {
            Label lbl = new Label { Text = etiqueta, Left = 15, Top = top + 3, Width = 130 };
            TextBox txt = new TextBox { Left = 150, Top = top, Width = 250 };
            padre.Controls.Add(lbl);
            padre.Controls.Add(txt);
            caja = txt;
        }

        /// <summary>
        /// Lee los campos del formulario, valida el tipo de dato numérico y
        /// delega el registro real al servicio (que inserta en el Árbol B+).
        /// </summary>
        private void RegistrarLibro()
        {
            if (!int.TryParse(txtCodigo.Text.Trim(), out int codigo))
            {
                MostrarEstado("El código debe ser numérico.", true);
                return;
            }
            if (!int.TryParse(txtCopias.Text.Trim(), out int copias))
            {
                MostrarEstado("La cantidad de copias debe ser numérica.", true);
                return;
            }

            var (ok, mensaje) = servicio.RegistrarLibro(codigo, txtTitulo.Text, txtAutor.Text, txtCategoria.Text, copias);
            MostrarEstado(mensaje, !ok);
            if (ok)
            {
                // Limpia el formulario para facilitar el siguiente registro.
                txtCodigo.Clear(); txtTitulo.Clear(); txtAutor.Clear(); txtCategoria.Clear(); txtCopias.Clear();
                txtCodigo.Focus();
            }
        }

        /// <summary>
        /// Busca un libro por código y muestra su detalle completo en el
        /// cuadro de texto de resultados.
        /// </summary>
        private void BuscarLibro()
        {
            if (!int.TryParse(txtCodigoBuscar.Text.Trim(), out int codigo))
            {
                MostrarEstado("Ingrese un código numérico válido.", true);
                return;
            }
            Libro libro = servicio.BuscarLibro(codigo);
            if (libro == null)
            {
                txtResultadoBusqueda.Text = "No se encontró ningún libro con ese código.";
                MostrarEstado("Libro no encontrado.", true);
                return;
            }

            txtResultadoBusqueda.Text =
                $"Código:        {libro.Codigo}\r\n" +
                $"Título:        {libro.Titulo}\r\n" +
                $"Autor:         {libro.Autor}\r\n" +
                $"Categoría:     {libro.Categoria}\r\n" +
                $"Copias totales:      {libro.CopiasTotales}\r\n" +
                $"Copias disponibles:  {libro.CopiasDisponibles}\r\n" +
                $"Veces prestado:      {libro.VecesPrestado}\r\n" +
                $"Préstamos registrados: {libro.HistorialPrestamos.Cantidad}";
            MostrarEstado("Libro encontrado.");
        }

        /// <summary>
        /// Elimina un libro del catálogo por su código.
        /// </summary>
        private void EliminarLibro()
        {
            if (!int.TryParse(txtCodigoEliminar.Text.Trim(), out int codigo))
            {
                MostrarEstado("Ingrese un código numérico válido.", true);
                return;
            }
            var (ok, mensaje) = servicio.EliminarLibro(codigo);
            MostrarEstado(mensaje, !ok);
            if (ok) txtCodigoEliminar.Clear();
        }

        // =================================================================
        // TAB 2: PRÉSTAMOS Y DEVOLUCIONES
        // =================================================================

        /// <summary>
        /// Arma la pestaña "Préstamos": registrar préstamo (con nombre del
        /// prestatario), registrar devolución (con nombre del prestatario) y
        /// consultar el historial de préstamos de un libro.
        /// </summary>
        private TabPage ConstruirTabPrestamos()
        {
            TabPage tab = new TabPage("Préstamos");

            // --- Panel: registrar préstamo (incluye nombre de quien presta) ---
            GroupBox grpPrestamo = new GroupBox { Text = "Registrar préstamo", Left = 15, Top = 15, Width = 430, Height = 140 };
            Label lbl1 = new Label { Text = "Código:", Left = 15, Top = 33, Width = 60 };
            txtCodigoPrestamo = new TextBox { Left = 80, Top = 30, Width = 100 };

            Label lblNombrePrestamo = new Label { Text = "Nombre de quien presta:", Left = 15, Top = 68, Width = 150 };
            txtNombrePrestamo = new TextBox { Left = 170, Top = 65, Width = 240 };

            Button btnPrestamo = new Button { Text = "Registrar préstamo", Left = 130, Top = 100, Width = 180, Height = 30 };
            EstilizarBoton(btnPrestamo);
            btnPrestamo.Click += (s, e) =>
            {
                if (!int.TryParse(txtCodigoPrestamo.Text.Trim(), out int codigo)) { MostrarEstado("Código inválido.", true); return; }
                string nombre = txtNombrePrestamo.Text.Trim();
                if (string.IsNullOrEmpty(nombre)) { MostrarEstado("Ingrese el nombre de quien presta el libro.", true); return; }

                // El servicio valida disponibilidad, descuenta copia y guarda el préstamo en el historial.
                var (ok, mensaje) = servicio.RegistrarPrestamo(codigo, nombre);
                MostrarEstado(mensaje, !ok);
                if (ok) { txtCodigoPrestamo.Clear(); txtNombrePrestamo.Clear(); }
            };
            grpPrestamo.Controls.Add(lbl1);
            grpPrestamo.Controls.Add(txtCodigoPrestamo);
            grpPrestamo.Controls.Add(lblNombrePrestamo);
            grpPrestamo.Controls.Add(txtNombrePrestamo);
            grpPrestamo.Controls.Add(btnPrestamo);

            // --- Panel: registrar devolución (incluye nombre de quien presta) ---
            GroupBox grpDevolucion = new GroupBox { Text = "Registrar devolución", Left = 15, Top = 165, Width = 430, Height = 140 };
            Label lbl2 = new Label { Text = "Código:", Left = 15, Top = 33, Width = 60 };
            txtCodigoDevolucion = new TextBox { Left = 80, Top = 30, Width = 100 };

            Label lblNombreDevolucion = new Label { Text = "Nombre de quien presta:", Left = 15, Top = 68, Width = 150 };
            txtNombreDevolucion = new TextBox { Left = 170, Top = 65, Width = 240 };

            Button btnDevolucion = new Button { Text = "Registrar devolución", Left = 130, Top = 100, Width = 180, Height = 30 };
            EstilizarBoton(btnDevolucion);
            btnDevolucion.Click += (s, e) =>
            {
                if (!int.TryParse(txtCodigoDevolucion.Text.Trim(), out int codigo)) { MostrarEstado("Código inválido.", true); return; }
                string nombre = txtNombreDevolucion.Text.Trim();
                if (string.IsNullOrEmpty(nombre)) { MostrarEstado("Ingrese el nombre de quien presta el libro.", true); return; }

                // El servicio busca en el historial (lista enlazada) el préstamo
                // pendiente de ese libro a nombre de esa persona.
                var (ok, mensaje) = servicio.RegistrarDevolucion(codigo, nombre);
                MostrarEstado(mensaje, !ok);
                if (ok) { txtCodigoDevolucion.Clear(); txtNombreDevolucion.Clear(); }
            };
            grpDevolucion.Controls.Add(lbl2);
            grpDevolucion.Controls.Add(txtCodigoDevolucion);
            grpDevolucion.Controls.Add(lblNombreDevolucion);
            grpDevolucion.Controls.Add(txtNombreDevolucion);
            grpDevolucion.Controls.Add(btnDevolucion);

            // --- Panel: historial de préstamos de un libro ---
            GroupBox grpHistorial = new GroupBox { Text = "Historial de préstamos de un libro", Left = 460, Top = 15, Width = 480, Height = 480 };
            Label lbl3 = new Label { Text = "Código:", Left = 15, Top = 33, Width = 60 };
            txtCodigoHistorial = new TextBox { Left = 80, Top = 30, Width = 100 };
            Button btnHistorial = new Button { Text = "Ver historial", Left = 190, Top = 28, Width = 120, Height = 28 };
            EstilizarBoton(btnHistorial);
            lstHistorial = new ListBox { Left = 15, Top = 70, Width = 445, Height = 390 };
            btnHistorial.Click += (s, e) =>
            {
                lstHistorial.Items.Clear();
                if (!int.TryParse(txtCodigoHistorial.Text.Trim(), out int codigo)) { MostrarEstado("Código inválido.", true); return; }
                Libro libro = servicio.BuscarLibro(codigo);
                if (libro == null) { MostrarEstado("Libro no encontrado.", true); return; }

                // Recorrer() convierte la lista enlazada del historial en un
                // Vector propio, solo para poder iterarlo aquí con un for.
                Vector<Prestamo> historial = libro.HistorialPrestamos.Recorrer();
                if (historial.Cantidad == 0)
                {
                    lstHistorial.Items.Add("Este libro no tiene préstamos registrados.");
                }
                else
                {
                    for (int i = 0; i < historial.Cantidad; i++)
                        lstHistorial.Items.Add(historial[i].ToString()); // incluye nombre del prestatario
                }
                MostrarEstado($"Historial de '{libro.Titulo}' cargado ({historial.Cantidad} préstamo(s)).");
            };
            grpHistorial.Controls.Add(lbl3);
            grpHistorial.Controls.Add(txtCodigoHistorial);
            grpHistorial.Controls.Add(btnHistorial);
            grpHistorial.Controls.Add(lstHistorial);

            tab.Controls.Add(grpPrestamo);
            tab.Controls.Add(grpDevolucion);
            tab.Controls.Add(grpHistorial);
            return tab;
        }

        // =================================================================
        // TAB 3: LISTADOS (recorrido del Árbol B+)
        // =================================================================

        /// <summary>
        /// Arma la pestaña "Listados": dos botones para mostrar el catálogo
        /// completo, ordenado por código (recorrido nativo del Árbol B+) o
        /// por título (merge sort sobre el Vector propio).
        /// </summary>
        private TabPage ConstruirTabListados()
        {
            TabPage tab = new TabPage("Listados");

            Button btnPorTitulo = new Button { Text = "Listado ordenado por título", Left = 15, Top = 15, Width = 220, Height = 32 };
            Button btnPorCodigo = new Button { Text = "Listado ordenado por código", Left = 245, Top = 15, Width = 220, Height = 32 };
            EstilizarBoton(btnPorTitulo);
            EstilizarBoton(btnPorCodigo);

            gridListado = CrearGrid();
            gridListado.Top = 60;

            btnPorTitulo.Click += (s, e) =>
            {
                CargarGrid(gridListado, servicio.ListadoOrdenadoPorTitulo());
                MostrarEstado("Listado ordenado por título (ordenamiento por mezcla).");
            };
            btnPorCodigo.Click += (s, e) =>
            {
                CargarGrid(gridListado, servicio.ListadoCompleto());
                MostrarEstado("Listado ordenado por código (recorrido de hojas del Árbol B+).");
            };

            tab.Controls.Add(btnPorTitulo);
            tab.Controls.Add(btnPorCodigo);
            tab.Controls.Add(gridListado);
            return tab;
        }

        // =================================================================
        // TAB 4: REPORTES (Max Heap / Min Heap)
        // =================================================================

        /// <summary>
        /// Arma la pestaña "Reportes": top-N de libros más prestados (Max
        /// Heap) y alerta de bajo stock (Min Heap), según la cantidad "N"
        /// elegida por el usuario.
        /// </summary>
        private TabPage ConstruirTabReportes()
        {
            TabPage tab = new TabPage("Reportes");

            Label lblN = new Label { Text = "Top N:", Left = 15, Top = 20, Width = 50 };
            numTopN = new NumericUpDown { Left = 65, Top = 17, Width = 60, Minimum = 1, Maximum = 100, Value = 5 };

            Button btnMasPrestados = new Button { Text = "Top más prestados (Max Heap)", Left = 140, Top = 15, Width = 240, Height = 32 };
            Button btnBajoStock = new Button { Text = "Alerta bajo stock (Min Heap)", Left = 390, Top = 15, Width = 240, Height = 32 };
            EstilizarBoton(btnMasPrestados);
            EstilizarBoton(btnBajoStock);

            gridReporte = CrearGrid();
            gridReporte.Top = 60;

            btnMasPrestados.Click += (s, e) =>
            {
                CargarGrid(gridReporte, servicio.TopMasPrestados((int)numTopN.Value));
                MostrarEstado("Top de libros más prestados generado con Max Heap.");
            };
            btnBajoStock.Click += (s, e) =>
            {
                CargarGrid(gridReporte, servicio.AlertaBajoStock((int)numTopN.Value));
                MostrarEstado("Alerta de bajo stock generada con Min Heap.");
            };

            tab.Controls.Add(lblN);
            tab.Controls.Add(numTopN);
            tab.Controls.Add(btnMasPrestados);
            tab.Controls.Add(btnBajoStock);
            tab.Controls.Add(gridReporte);
            return tab;
        }

        // =================================================================
        // TAB 5: ARCHIVO (carga / guardado en CSV — datos dinámicos)
        // =================================================================

        /// <summary>
        /// Arma la pestaña "Archivo": permite elegir un CSV, cargarlo al
        /// catálogo (insertándolo en el Árbol B+) o guardar el catálogo
        /// actual a un CSV.
        /// </summary>
        private TabPage ConstruirTabArchivo()
        {
            TabPage tab = new TabPage("Archivo");

            Label lblRuta = new Label { Text = "Ruta del archivo CSV:", Left = 15, Top = 20, Width = 150 };
            txtRutaArchivo = new TextBox { Left = 165, Top = 17, Width = 430 };
            Button btnExaminar = new Button { Text = "Examinar...", Left = 605, Top = 15, Width = 100, Height = 26 };
            EstilizarBoton(btnExaminar);
            btnExaminar.Click += (s, e) =>
            {
                using OpenFileDialog dlg = new OpenFileDialog { Filter = "Archivos CSV (*.csv;*.txt)|*.csv;*.txt|Todos (*.*)|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK) txtRutaArchivo.Text = dlg.FileName;
            };

            Button btnCargar = new Button { Text = "Cargar catálogo desde CSV", Left = 15, Top = 55, Width = 210, Height = 32 };
            Button btnGuardar = new Button { Text = "Guardar catálogo en CSV", Left = 235, Top = 55, Width = 210, Height = 32 };
            EstilizarBoton(btnCargar);
            EstilizarBoton(btnGuardar);

            txtLogArchivo = new TextBox
            {
                Left = 15, Top = 100, Width = 690, Height = 400,
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
            };

            btnCargar.Click += (s, e) =>
            {
                string ruta = txtRutaArchivo.Text.Trim();
                if (string.IsNullOrEmpty(ruta)) { MostrarEstado("Especifique la ruta del archivo.", true); return; }

                // detalle es un Vector<string> propio con los errores/omisiones encontrados.
                var (cargados, errores, detalle) = servicio.CargarDesdeCsv(ruta);
                txtLogArchivo.Clear();
                txtLogArchivo.AppendText($"Libros cargados correctamente: {cargados}\r\n");
                txtLogArchivo.AppendText($"Errores/omisiones: {errores}\r\n\r\n");
                for (int i = 0; i < detalle.Cantidad; i++)
                    txtLogArchivo.AppendText(detalle[i] + "\r\n");

                MostrarEstado($"Carga finalizada: {cargados} libro(s) cargado(s), {errores} error(es).", errores > 0 && cargados == 0);
            };

            btnGuardar.Click += (s, e) =>
            {
                string ruta = txtRutaArchivo.Text.Trim();
                if (string.IsNullOrEmpty(ruta)) { MostrarEstado("Especifique la ruta del archivo.", true); return; }
                bool ok = servicio.GuardarEnCsv(ruta);
                MostrarEstado(ok ? $"Catálogo guardado en '{ruta}'." : "Ocurrió un error al guardar el archivo.", !ok);
            };

            tab.Controls.Add(lblRuta);
            tab.Controls.Add(txtRutaArchivo);
            tab.Controls.Add(btnExaminar);
            tab.Controls.Add(btnCargar);
            tab.Controls.Add(btnGuardar);
            tab.Controls.Add(txtLogArchivo);
            return tab;
        }

        // =================================================================
        // UTILIDADES DE INTERFAZ
        // =================================================================

        /// <summary>
        /// Crea y estiliza (colores de la maqueta) un DataGridView de solo
        /// lectura con las columnas estándar usadas en Listados y Reportes.
        /// </summary>
        private DataGridView CrearGrid()
        {
            DataGridView grid = new DataGridView
            {
                Left = 15,
                Width = 940,
                Height = 550,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false, // necesario para poder cambiar el color del encabezado
                BackgroundColor = Color.White,
                GridColor = Color.LightGray
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColorEncabezadoTabla;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            grid.ColumnHeadersHeight = 32;
            grid.DefaultCellStyle.SelectionBackColor = ColorBarra;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            grid.Columns.Add("Codigo", "Código");
            grid.Columns.Add("Titulo", "Título");
            grid.Columns.Add("Autor", "Autor");
            grid.Columns.Add("Categoria", "Categoría");
            grid.Columns.Add("Disponibles", "Disponibles");
            grid.Columns.Add("Totales", "Totales");
            grid.Columns.Add("VecesPrestado", "Veces prestado");
            return grid;
        }

        /// <summary>
        /// Vuelca el contenido de un Vector&lt;Libro&gt; (resultado de un
        /// recorrido del Árbol B+ o de un heap) a las filas de un DataGridView.
        /// </summary>
        private void CargarGrid(DataGridView grid, Vector<Libro> libros)
        {
            grid.Rows.Clear();
            for (int i = 0; i < libros.Cantidad; i++)
            {
                Libro l = libros[i];
                grid.Rows.Add(l.Codigo, l.Titulo, l.Autor, l.Categoria, l.CopiasDisponibles, l.CopiasTotales, l.VecesPrestado);
            }
        }
    }
}
