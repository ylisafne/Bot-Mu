using Microsoft.VisualBasic;
using System.Text.Json;

namespace Kapi_Mu_Utility
{
    public partial class FrmPerfil : Form
    {
        // Carpeta temporal de la aplicación
        private readonly string configDir =
            Path.Combine(Path.GetTempPath(), "Mu-Util");

        // Archivo JSON de configuración
        private string ConfigPath =>
            Path.Combine(configDir, "config.json");

        // Carpeta donde se almacenarán los perfiles
        private string PerfilDir =>
            Path.Combine(configDir, "Perfil");

        public class PerfilConfig
        {
            public string Perfil { get; set; } = "";
            public string Carpeta { get; set; } = "";
        }

        public string NombrePerfil { get; private set; } = "";
        public string RutaPerfil { get; private set; } = "";
        public event Action<string, string> PerfilSeleccionado;

        public FrmPerfil()
        {
            InitializeComponent();
            CargarPerfiles();
        }

        // Lee los perfiles del archivo JSON.
        // Si no existe, lo crea vacío.
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

        // Guarda los perfiles en el archivo JSON.
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

        // Carga los perfiles en el ComboBox.
        private void CargarPerfiles()
        {
            try
            {
                List<PerfilConfig> perfiles = LeerPerfiles();

                cboPerfil.DataSource = null;
                cboPerfil.DisplayMember = "Perfil";
                cboPerfil.ValueMember = "Carpeta";
                cboPerfil.DataSource = perfiles;
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

        // Agrega un nuevo perfil.
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

        // Recargar manualmente los perfiles
        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (cboPerfil.SelectedItem is PerfilConfig perfil)
            {
                NombrePerfil = perfil.Perfil;
                RutaPerfil = perfil.Carpeta;

                PerfilSeleccionado?.Invoke(NombrePerfil, RutaPerfil);

                Close();
            }
        }
    }
}