using Microsoft.VisualBasic;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Kapi_Mu_Utility
{
    public partial class FrmMain : Form
    {
        //
        public static bool PassKey = false;

        // Almacena los formularios web abiertos por perfil.
        private readonly Dictionary<string, FrmWeb> frmWebAbiertos =
            new Dictionary<string, FrmWeb>(StringComparer.OrdinalIgnoreCase);

        // Define la carpeta temporal de configuración.
        private readonly string configDir = Path.Combine(Path.GetTempPath(), "Mu-Util");

        // Obtiene la ruta del archivo de configuración JSON.
        private string ConfigPath => Path.Combine(configDir, "config.json");

        // Obtiene la ruta de la carpeta de perfiles.
        private string PerfilDir => Path.Combine(configDir, "Perfil");

        //
        private string TessdataPath => Path.Combine(configDir, "tessdata");

        // Almacena el nombre y la ruta de cada perfil.
        public class PerfilConfig
        {
            public string Perfil { get; set; } = ""; // Nombre del perfil.
            public string Carpeta { get; set; } = ""; // Ruta de la carpeta del perfil.
        }

        // Indica si el clic derecho automático está activo.
        private bool clickDerechoActivo = false;

        // Indica si los formularios secundarios están ocultos.
        private bool formulariosOcultos = false;
        private bool cambiandoEstado = false;

        // Perfiles que ya tienen una ventana abierta.
        private readonly HashSet<string> perfilesBloqueados =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private OCRService servicioOCR;
        //

        public FrmMain()
        {
            InitializeComponent();
            gvMain.CellDoubleClick += gvMain_CellDoubleClick;
            ConfigurarGridPerfiles();
            CargarPerfiles();
            UnpackData();
            servicioOCR = new OCRService(TessdataPath);
        }
        // ==================== EVENTOS DEL FORMULARIO ====================
        private void FrmMain_Load(object sender, EventArgs e)
        {
            if (!PassKey)
            {
                EvalForm evalForm = new EvalForm();
                evalForm.FormClosed += EvalForm_FormClosed;
                evalForm.ShowDialog();
            }

        }
        private void EvalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!PassKey)
                this.Close();
        }
        private void FrmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                loadWebForm();
            }
        }
        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }
        // ==================== EVENTOS DE LOS BOTONES ====================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Mostrar una ventana flotante para ingresar el nombre
            string nombrePerfil = Interaction.InputBox(
                "Ingresa el nombre del nuevo perfil:",
                "Agregar perfil",
                ""
            ).Trim();

            // Si cancela o deja el campo vacío, no hacer nada
            if (string.IsNullOrWhiteSpace(nombrePerfil))
                return;

            // Validar caracteres no permitidos en nombres de carpetas
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nombrePerfil = nombrePerfil.Replace(c, '_');
            }

            nombrePerfil = nombrePerfil.Trim().TrimEnd('.');

            if (string.IsNullOrWhiteSpace(nombrePerfil))
            {
                MessageBox.Show("El nombre del perfil no es válido.");
                return;
            }

            try
            {
                List<PerfilConfig> perfiles = LeerPerfiles();

                // Comprobar si el perfil ya existe
                if (perfiles.Exists(p =>
                    string.Equals(
                        p.Perfil,
                        nombrePerfil,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Este perfil ya existe.");
                    return;
                }

                // Crear Temp\Mu-Util\Perfil\NombreDelPerfil
                Directory.CreateDirectory(PerfilDir);

                string carpetaPerfil = Path.Combine(
                    PerfilDir,
                    nombrePerfil
                );

                Directory.CreateDirectory(carpetaPerfil);

                // Guardar el nuevo perfil
                perfiles.Add(new PerfilConfig
                {
                    Perfil = nombrePerfil,
                    Carpeta = carpetaPerfil
                });

                GuardarPerfiles(perfiles);

                // Actualizar el ComboBox y seleccionar el nuevo perfil
                CargarPerfiles();
                cboPerfil.SelectedValue = carpetaPerfil;

                MessageBox.Show(
                    "Perfil agregado correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al agregar el perfil:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void btnQuitar_Click(object sender, EventArgs e)
        {
            // Comprobar que haya un perfil seleccionado
            if (cboPerfil.SelectedValue == null)
            {
                MessageBox.Show("Selecciona un perfil para eliminar.");
                return;
            }

            string carpetaPerfil = cboPerfil.SelectedValue.ToString();

            try
            {
                List<PerfilConfig> perfiles = LeerPerfiles();

                // Buscar el perfil seleccionado por su carpeta
                PerfilConfig perfil = perfiles.Find(p =>
                    string.Equals(
                        p.Carpeta,
                        carpetaPerfil,
                        StringComparison.OrdinalIgnoreCase));

                if (perfil == null)
                {
                    MessageBox.Show("No se encontró el perfil seleccionado.");
                    return;
                }

                // Pedir confirmación
                DialogResult resultado = MessageBox.Show(
                    $"¿Estás seguro de eliminar el perfil '{perfil.Perfil}'?",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (resultado != DialogResult.Yes)
                    return;

                // Eliminar la carpeta del perfil, si existe
                if (Directory.Exists(perfil.Carpeta))
                {
                    Directory.Delete(perfil.Carpeta, true);
                }

                // Eliminar el perfil de la lista
                perfiles.Remove(perfil);

                // Guardar los cambios
                GuardarPerfiles(perfiles);

                // Recargar el ComboBox
                CargarPerfiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al eliminar el perfil:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            loadWebForm();
        }
        private void loadWebForm()
        {
            if (cboPerfil.SelectedValue == null)
            {
                MessageBox.Show("No hay perfiles disponibles.");
                return;
            }

            string carpetaPerfil = cboPerfil.SelectedValue.ToString();
            string nombrePerfil = cboPerfil.Text;

            // Si ya está abierto, activar su ventana.
            if (frmWebAbiertos.TryGetValue(carpetaPerfil, out FrmWeb frm))
            {
                if (!frm.IsDisposed)
                {
                    frm.Show();
                    frm.Activate();
                    SeleccionarSiguientePerfil();
                    return;
                }

                frmWebAbiertos.Remove(carpetaPerfil);
                QuitarPerfilGrid(carpetaPerfil);
                perfilesBloqueados.Remove(carpetaPerfil);
            }

            FrmWeb nuevoFrm = new FrmWeb(carpetaPerfil, nombrePerfil, servicioOCR);

            RegistrarPerfilGrid(carpetaPerfil, nombrePerfil);

            frmWebAbiertos[carpetaPerfil] = nuevoFrm;
            // Bloquear el perfil que acabamos de abrir.
            perfilesBloqueados.Add(carpetaPerfil);

            // Quitar el perfil del ComboBox y seleccionar el siguiente.
            CargarPerfiles();
            SeleccionarSiguientePerfil();

            nuevoFrm.FormClosed += (s, args) =>
            {
                if (frmWebAbiertos.TryGetValue(
                        carpetaPerfil, out FrmWeb actual)
                    && ReferenceEquals(actual, nuevoFrm))
                {
                    frmWebAbiertos.Remove(carpetaPerfil);
                    QuitarPerfilGrid(carpetaPerfil);
                }

                // Desbloquear el perfil cuando se cierre su ventana.
                perfilesBloqueados.Remove(carpetaPerfil);

                // Actualizar el ComboBox desde el hilo de la interfaz.
                if (!IsDisposed)
                {
                    CargarPerfiles();
                    SeleccionarSiguientePerfil();
                }
            };

            nuevoFrm.Show();
            nuevoFrm.Activate();

            // Seleccionar automáticamente el siguiente perfil disponible.
            SeleccionarSiguientePerfil();
        }
        private void gvMain_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ignorar el encabezado del GridView.
            if (e.RowIndex < 0)
                return;

            DataGridViewRow fila = gvMain.Rows[e.RowIndex];

            // Obtener la ruta del perfil.
            string carpetaPerfil = fila.Tag as string;

            if (string.IsNullOrEmpty(carpetaPerfil))
                return;

            // Buscar la ventana correspondiente al perfil.
            if (frmWebAbiertos.TryGetValue(carpetaPerfil, out FrmWeb frm))
            {
                if (!frm.IsDisposed)
                {
                    // Mostrar y activar la ventana.
                    frm.Show();

                    if (frm.WindowState == FormWindowState.Minimized)
                        frm.WindowState = FormWindowState.Normal;

                    frm.Activate();
                    frm.BringToFront();
                }
            }
        }
        private void btnHideShow_Click(object sender, EventArgs e)
        {
            if (!formulariosOcultos)
            {
                // Ocultar todos los formularios
                OcultarTodosLosHijos();

                formulariosOcultos = true;
                btnHideShow.Text = "Mostrar";
            }
            else
            {
                // Mostrar todos los formularios que siguen abiertos
                foreach (FrmWeb frm in frmWebAbiertos.Values.ToList())
                {
                    if (!frm.IsDisposed)
                    {
                        frm.Show();
                    }
                }
                formulariosOcultos = false;
                btnHideShow.Text = "Ocultar";
            }
        }
        private void btnActive_Click(object sender, EventArgs e)
        {
            OrganizarVentanas();
        }
        // ==================== MÉTODOS AUXILIARES ====================
        private void CargarPerfiles()
        {
            try
            {
                List<PerfilConfig> perfiles = LeerPerfiles();

                // Mostrar solamente los perfiles que no están abiertos.
                List<PerfilConfig> disponibles = perfiles
                    .Where(p => !perfilesBloqueados.Contains(p.Carpeta))
                    .ToList();

                cboPerfil.DataSource = null;
                cboPerfil.DisplayMember = "Perfil";
                cboPerfil.ValueMember = "Carpeta";
                cboPerfil.DataSource = disponibles;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los perfiles:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private List<PerfilConfig> LeerPerfiles()
        {
            Directory.CreateDirectory(configDir);

            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, "[]");
            }

            string json = File.ReadAllText(ConfigPath);

            return JsonSerializer.Deserialize<List<PerfilConfig>>(json)
                   ?? new List<PerfilConfig>();
        }
        private void GuardarPerfiles(List<PerfilConfig> perfiles)
        {
            Directory.CreateDirectory(configDir);

            string json = JsonSerializer.Serialize(
                perfiles,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(ConfigPath, json);
        }
        private void OcultarTodosLosHijos()
        {
            foreach (FrmWeb frm in frmWebAbiertos.Values.ToList())
            {
                if (!frm.IsDisposed)
                {
                    frm.Hide();
                }
            }
        }
        
        private void OrganizarVentanas()
        {
            var ventanasActivas = new List<FrmWeb>();

            foreach (DataGridViewRow fila in gvMain.Rows)
            {
                if (fila.IsNewRow)
                    continue;

                bool participa = fila.Cells[1].Value is bool valor && valor;

                string carpetaPerfil = fila.Tag as string;

                if (string.IsNullOrEmpty(carpetaPerfil))
                    continue;

                if (!frmWebAbiertos.TryGetValue(carpetaPerfil, out FrmWeb frm))
                    continue;

                if (frm.IsDisposed)
                    continue;

                if (participa)
                {
                    frm.Show();
                    ventanasActivas.Add(frm);
                }
                else
                {
                    frm.Hide();
                }
            }

            int cantidad = ventanasActivas.Count;

            if (cantidad == 0)
                return;

            Rectangle area = Screen.FromControl(this).WorkingArea;

            // Siempre usar como mínimo una cuadrícula de 2x2.
            int columnas;
            int filas;

            if (cantidad <= 4)
            {
                columnas = 2;
                filas = 2;
            }
            else
            {
                columnas = (int)Math.Ceiling(Math.Sqrt(cantidad));
                filas = (int)Math.Ceiling((double)cantidad / columnas);
            }

            int ancho = area.Width / columnas;
            int alto = area.Height / filas;

            for (int i = 0; i < cantidad; i++)
            {
                // Distribuir de arriba hacia abajo por columna.
                int fila = i % filas;
                int columna = i / filas;

                int x = area.Left + columna * ancho;
                int y = area.Top + fila * alto;

                int anchoMaximo = (columna == columnas - 1)
                    ? area.Right - x
                    : ancho;

                int altoMaximo = (fila == filas - 1)
                    ? area.Bottom - y
                    : alto;

                FrmWeb ventana = ventanasActivas[i];

                // Restaurar antes de modificar el tamaño.
                ventana.WindowState = FormWindowState.Normal;

                // Obtener la relación de aspecto actual.
                int anchoOriginal = ventana.Width;
                int altoOriginal = ventana.Height;

                if (anchoOriginal <= 0 || altoOriginal <= 0)
                    continue;

                double relacion =
                    (double)anchoOriginal / altoOriginal;

                // Ajustar la ventana al tamaño máximo de la celda
                // sin alterar su relación de aspecto.
                int anchoVentana = anchoMaximo;
                int altoVentana = (int)(anchoVentana / relacion);

                if (altoVentana > altoMaximo)
                {
                    altoVentana = altoMaximo;
                    anchoVentana = (int)(altoVentana * relacion);
                }

                // Centrar la ventana dentro de la celda.
                int xFinal = x + (anchoMaximo - anchoVentana) / 2;
                int yFinal = y + (altoMaximo - altoVentana) / 2;

                ventana.Bounds = new Rectangle(
                    xFinal,
                    yFinal,
                    anchoVentana,
                    altoVentana
                );

                ventana.BringToFront();
            }
        }
        private void ConfigurarGridPerfiles()
        {
            gvMain.Columns.Clear();
            gvMain.AutoGenerateColumns = false;
            gvMain.AllowUserToAddRows = false;
            gvMain.AllowUserToDeleteRows = false;
            gvMain.MultiSelect = false;
            gvMain.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gvMain.RowHeadersVisible = false;

            gvMain.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Perfil",
                HeaderText = "Perfil",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            gvMain.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Organizar",
                HeaderText = "Organiar",
                ThreeState = false
            });

            // Aplicar inmediatamente el cambio del CheckBox.
            gvMain.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gvMain.IsCurrentCellDirty)
                {
                    gvMain.CommitEdit(
                        DataGridViewDataErrorContexts.Commit);
                }
            };
        }
        private void SeleccionarSiguientePerfil()
        {
            if (cboPerfil.Items.Count > 0)
                cboPerfil.SelectedIndex = 0;
            else
                cboPerfil.SelectedIndex = -1;
        }
        private void RegistrarPerfilGrid(
            string carpetaPerfil,
            string nombrePerfil)
        {
            foreach (DataGridViewRow fila in gvMain.Rows)
            {
                if (string.Equals(
                    fila.Tag as string,
                    carpetaPerfil,
                    StringComparison.OrdinalIgnoreCase))
                {
                    fila.Cells["Perfil"].Value = nombrePerfil;
                    return;
                }
            }

            int indice = gvMain.Rows.Add(nombrePerfil, true);

            // Guardamos la ruta como identificador de la fila.
            gvMain.Rows[indice].Tag = carpetaPerfil;
        }

        private void QuitarPerfilGrid(string carpetaPerfil)
        {
            foreach (DataGridViewRow fila in gvMain.Rows)
            {
                if (string.Equals(
                    fila.Tag as string,
                    carpetaPerfil,
                    StringComparison.OrdinalIgnoreCase))
                {
                    gvMain.Rows.Remove(fila);
                    return;
                }
            }
        }
        private void UnpackData()
        {
            Directory.CreateDirectory(TessdataPath);

            foreach (string language in new[] { "spa", "eng" })
            {
                string outputFile = Path.Combine(
                    TessdataPath,
                    $"{language}.traineddata"
                );

                // Evitar extraer nuevamente si ya existe
                if (File.Exists(outputFile))
                    continue;



                using (Stream resource = CargarRecursoStream($"{language}.traineddata"))
                using (FileStream file = File.Create(outputFile))
                {
                    resource.CopyTo(file);
                }
            }
        }
        private static Stream CargarRecursoStream(string nombreArchivo)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string nombreRecurso = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(nombre =>
                    nombre.EndsWith(
                        $"recursos.{nombreArchivo}",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (nombreRecurso == null)
                throw new FileNotFoundException(
                    $"No se encontró el recurso: {nombreArchivo}"
                );

            return assembly.GetManifestResourceStream(nombreRecurso);
        }
        
    }
}
