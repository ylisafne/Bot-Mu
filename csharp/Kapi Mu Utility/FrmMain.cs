using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Kapi_Mu_Utility
{
    public partial class FrmMain : Form
    {
        //
        // Define F8 como tecla de activación.
        //private Keys key = Keys.F8;

        // Define el intervalo entre clics en milisegundos.
        private const int INTERVALO_CLICK_MS = 50;

        //Tiempo de Presion del Click Derecho
        private const int INTERVALO_PRESS_MS = 5;

        // Declara la función para registrar una tecla global.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Declara la función para desregistrar una tecla global.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Controla la cancelación de los clics automáticos.
        private CancellationTokenSource clickCts;

        // Almacena la tarea que ejecuta los clics automáticos.
        private Task tareaClick;

        // Identificador único del atajo de teclado.
        private const int HOTKEY_ID = 9000;

        // Indica que la tecla no necesita modificadores.
        private const uint MOD_NONE = 0;

        // Identifica el mensaje de tecla global presionada.
        private const int WM_HOTKEY = 0x0312;

        // Indica si F8 fue registrado correctamente.
        private bool f8Registrado;

        // Envía eventos de entrada al sistema operativo.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        //
        public static bool PassKey = false;

        // Estructura que representa una entrada simulada.
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        // Contiene los datos del evento del mouse.
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Identifica una entrada simulada del mouse.
        private const uint INPUT_MOUSE = 0;

        // Simula la presión del botón derecho.
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;

        // Simula la liberación del botón derecho.
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        // Almacena los formularios web abiertos por perfil.
        private readonly Dictionary<string, FrmWeb> frmWebAbiertos =
            new Dictionary<string, FrmWeb>(StringComparer.OrdinalIgnoreCase);

        // Define la carpeta temporal de configuración.
        private readonly string configDir =
            Path.Combine(Path.GetTempPath(), "Mu-Util");

        // Obtiene la ruta del archivo de configuración JSON.
        private string ConfigPath => Path.Combine(configDir, "config.json");

        // Obtiene la ruta de la carpeta de perfiles.
        private string PerfilDir => Path.Combine(configDir, "Perfil");

        // Almacena el nombre y la ruta de cada perfil.
        public class PerfilConfig
        {
            public string Perfil { get; set; } = ""; // Nombre del perfil.
            public string Carpeta { get; set; } = ""; // Ruta de la carpeta del perfil.
        }

        // Indica si el clic derecho automático está activo.
        private bool clickDerechoActivo = false;

        // Ejecuta acciones periódicas para los clics automáticos.
        private readonly System.Windows.Forms.Timer timerClickDerecho =
            new System.Windows.Forms.Timer();

        // Indica si las ventanas se están distribuyendo.
        private bool distribuirVentanas = false;

        // Indica si los formularios secundarios están ocultos.
        private bool formulariosOcultos = false;
        private bool cambiandoEstado = false;

        // Perfiles que ya tienen una ventana abierta.
        private readonly HashSet<string> perfilesBloqueados =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //

        public FrmMain()
        {
            InitializeComponent();
            gvMain.CellDoubleClick += gvMain_CellDoubleClick;
            ConfigurarGridPerfiles();
            CargarPerfiles();
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
            if (f8Registrado)
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                f8Registrado = false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                loadWebForm();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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

            FrmWeb nuevoFrm = new FrmWeb(carpetaPerfil, nombrePerfil);

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
        private static async Task<bool> ClickDerechoEnAsync(
    int x,
    int y,
    CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Cursor.Position = new Point(x, y);

            INPUT[] presionar =
            {
        new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dwFlags = MOUSEEVENTF_RIGHTDOWN
            }
        }
    };

            uint enviados = SendInput(
                1,
                presionar,
                Marshal.SizeOf<INPUT>());

            if (enviados != 1)
                return false;

            try
            {
                await Task.Delay(INTERVALO_PRESS_MS, token);
                return true;
            }
            finally
            {
                INPUT[] soltar =
                {
            new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSEEVENTF_RIGHTUP
                }
            }
        };

                SendInput(
                    1,
                    soltar,
                    Marshal.SizeOf<INPUT>());
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
        private void DetenerClickDerecho()
        {
            clickDerechoActivo = false;
            clickCts?.Cancel();
        }
        private async Task EjecutarClicksAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                await ClickDerechoCentroVentanasAsync(token);

                await Task.Delay(INTERVALO_CLICK_MS, token);
            }
        }
        private async Task ClickDerechoCentroVentanasAsync(CancellationToken token)
        {
            foreach (DataGridViewRow fila in gvMain.Rows)
            {
                token.ThrowIfCancellationRequested();

                if (fila.IsNewRow)
                    continue;

                bool participa = fila.Cells[1].Value is bool valor && valor;

                if (!participa)
                    continue;

                string carpetaPerfil = fila.Tag as string;

                if (string.IsNullOrEmpty(carpetaPerfil))
                    continue;

                if (!frmWebAbiertos.TryGetValue(
                        carpetaPerfil, out FrmWeb frm))
                    continue;

                if (frm.IsDisposed || !frm.Visible)
                    continue;

                Point centro = new Point(
                    frm.ClientSize.Width / 2,
                    frm.ClientSize.Height / 2);

                Point posicion = frm.PointToScreen(centro);

                token.ThrowIfCancellationRequested();

                // Primer clic derecho.
                bool realizado = await ClickDerechoEnAsync(
                    posicion.X,
                    posicion.Y,
                    token);

                if (!realizado)
                    continue;

                // Pequeña pausa entre los dos clics.
                await Task.Delay(10, token);

                // Segundo clic derecho.
                realizado = await ClickDerechoEnAsync(
                    posicion.X,
                    posicion.Y,
                    token);

                if (!realizado)
                    continue;

                // Dar tiempo para que la aplicación procese el clic.
                await Task.Delay(10, token);
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
            // Si hay más de 4 ventanas, ampliar automáticamente.
            int columnas;
            int filas;

            if (cantidad <= 4)
            {
                columnas = 2;
                filas = 2;
            }
            else
            {
                // Ampliar la cuadrícula para las ventanas adicionales.
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

                int anchoVentana = (columna == columnas - 1)
                    ? area.Right - x
                    : ancho;

                int altoVentana = (fila == filas - 1)
                    ? area.Bottom - y
                    : alto;

                FrmWeb ventana = ventanasActivas[i];

                ventana.WindowState = FormWindowState.Normal;

                ventana.Bounds = new Rectangle(
                    x,
                    y,
                    anchoVentana,
                    altoVentana
                );

                ventana.BringToFront();
            }
        }
        private void RestaurarVentanas()
        {
            foreach (FrmWeb frm in frmWebAbiertos.Values.ToList())
            {
                if (!frm.IsDisposed)
                {
                    frm.Show();
                }
            }
        }

        private void CambiarIconoTodasLasVentanas(Icon icono)
        {
            // Cambiar el icono del formulario principal
            this.Icon = icono;

            // Cambiar el icono de todos los formularios secundarios
            foreach (FrmWeb frm in frmWebAbiertos.Values.ToList())
            {
                if (!frm.IsDisposed)
                {
                    frm.Icon = icono;
                }
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
                Name = "Ocultar",
                HeaderText = "Click",
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
        private bool HayVentanasSeleccionadas()
        {
            foreach (DataGridViewRow fila in gvMain.Rows)
            {
                if (fila.IsNewRow)
                    continue;

                if (fila.Cells[1].Value is bool participa
                    && participa)
                {
                    return true;
                }
            }

            return false;
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
        private async void ToogleBtnActive()
        {
            // Si ya está activo, F8 lo detiene inmediatamente.
            if (clickDerechoActivo)
            {
                DetenerClickDerecho();
                return;
            }

            // Evitar iniciar dos procesos simultáneos.
            if (cambiandoEstado)
                return;

            // Validar que al menos una ventana tenga marcado Click.
            if (!HayVentanasSeleccionadas())
            {
                MessageBox.Show(
                    "Debes activar al menos una ventana en la columna Click.",
                    "Selecciona una ventana",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            cambiandoEstado = true;

            var cts = new CancellationTokenSource();
            clickCts = cts;
            CancellationToken token = cts.Token;

            clickDerechoActivo = true;
            btnActive.Text = "Detener (F8)";

            try
            {
                this.Icon = SystemIcons.Warning;
                CambiarIconoTodasLasVentanas(SystemIcons.Warning);

                // Organizar antes de calcular las posiciones.
                OrganizarVentanas();

                // Esperar a que Windows procese las posiciones.
                await Task.Delay(300, token);

                token.ThrowIfCancellationRequested();

                // Iniciar los clics.
                await EjecutarClicksAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal mediante F8.
            }
            finally
            {
                clickDerechoActivo = false;

                if (ReferenceEquals(clickCts, cts))
                    clickCts = null;

                cts.Dispose();

                this.Icon = SystemIcons.Application;
                CambiarIconoTodasLasVentanas(SystemIcons.Application);

                RestaurarVentanas();

                btnActive.Text = "Activar F8";
                cambiandoEstado = false;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY &&
                m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToogleBtnActive();
            }

            base.WndProc(ref m);
        }

        
    }
}
