using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    public partial class FrmTest : Form
    {
        private string nombrePerfil = "";
        private string rutaPerfil = "";
        private bool mobileView = false;

        private FrmPerfil frmPerfil;

        public FrmTest()
        {
            InitializeComponent();
        }

        private void FrmTest_Load(object sender, EventArgs e)
        {
            // Abrir el formulario de selección de perfil
            frmPerfil = new FrmPerfil();

            frmPerfil.PerfilSeleccionado += async (nombre, ruta) =>
            {
                nombrePerfil = nombre;
                rutaPerfil = ruta;

                // Inicializar WebView2 con el perfil seleccionado
                await IniciarAplicacion();
            };

            // Ocultar FrmTest mientras se selecciona el perfil
            Hide();

            frmPerfil.Show(this);
        }

        private async Task IniciarAplicacion()
        {
            this.Text +=" - " + nombrePerfil;
            try
            {
                // Validar la ruta recibida
                if (string.IsNullOrWhiteSpace(rutaPerfil))
                    return;

                // Carpeta de datos WebView2 específica del perfil
                string cachePath = Path.Combine(
                    rutaPerfil
                );

                Directory.CreateDirectory(cachePath);

                // Crear entorno independiente para este perfil
                var entorno = await CoreWebView2Environment.CreateAsync(
                    null,
                    cachePath
                );

                // Inicializar WebView2
                await webMain.EnsureCoreWebView2Async(entorno);

                // Navegar a la página
                webMain.Source = new Uri(
                    "https://play.mumagdalenas.com"
                );

                // Mostrar FrmTest cuando WebView2 esté inicializado
                Show();
                Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al iniciar WebView2: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Close();
            }
        }
    }
}