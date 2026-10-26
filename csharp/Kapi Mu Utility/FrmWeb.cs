using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using System.Threading;

namespace Kapi_Mu_Utility
{
    public partial class FrmWeb : Form
    {
        #region 1. Variables
        // Duración de click Izquierdo
        private int presionCIMinMs = 25;
        private int presionCIMaxMs = 60;
        private int pausaCIMinMs = 35;
        private int pausaCIMaxMs = 100;

        // Duracion de Click Derecho
        private int presionCDMinMs = 2 * 60 * 1000;  // 2 minutos
        private int presionCDMaxMs = 5 * 60 * 1000;  // 5 minutos
        private int pausaCDMinMs = 2 * 1000;        // 2 segundos
        private int pausaCDMaxMs = 5 * 1000;        // 5 segundos

        private string vvRuta;
        private string vvPerfil;
        private bool vvMobile = false;
        private Point puntoVentana;
        private bool puntoGuardado = false;
        private CancellationTokenSource ctsClicks;
        private bool clicksActivos = false;
        private CancellationTokenSource ctsPulsacion;

        #endregion

        #region 2. Constructor y título
        public FrmWeb(string vvRuta, string vvPerfil)
        {
            InitializeComponent();
            this.MinimizeBox = false;
            this.vvRuta = vvRuta;
            this.vvPerfil = vvPerfil;

            ActualizarTitulo();
        }
        private void ActualizarTitulo()
        {
            this.Text = $"{vvPerfil} {(vvMobile ? "MOB" : "PC")} {(clicksActivos ? "clicking" : "")}";
        }
        #endregion

        #region 3. Eventos del formulario
        private async void FrmWeb_Load(object sender, EventArgs e)
        {
            await CargarWebMainAsync();
        }
        private void FrmWeb_KeyDown(object sender, KeyEventArgs e)
        {
            TeclaApretada(e.KeyCode.ToString());
        }

        #endregion

        #region 4. Inicialización y navegación web
        private async Task CargarWebMainAsync()
        {
            this.Text = $"{vvPerfil} {(vvMobile ? "MOB" : "PC")} {vvRuta}";
            try
            {
                // Crear la carpeta del perfil
                Directory.CreateDirectory(vvRuta);

                // Crear el entorno de WebView2 con el perfil indicado
                var entorno = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: vvRuta
                );

                // Inicializar WebView2
                await webMain.EnsureCoreWebView2Async(entorno);
                webMain.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                //webMain.ZoomFactor = 1.5; // 80 %


                // F5 para ambos modos: MOB y PC
                webMain.CoreWebView2.WebMessageReceived += WebMain_WebMessageReceived;
                await webMain.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    document.addEventListener('keydown', e => {
                        if (e.key === 'F5') {
                            e.preventDefault();
                            window.chrome.webview.postMessage('F5');
                        }
                        if (e.key === 'F7') {
                            e.preventDefault();
                            window.chrome.webview.postMessage('F7');
                        }
                    }, true);
                    window.addEventListener('blur', () => {
                        window.chrome.webview.postMessage('WEB_BLUR');
                    });

                    window.addEventListener('focus', () => {
                        window.chrome.webview.postMessage('WEB_FOCUS');
                    });

                    document.addEventListener('visibilitychange', () => {
                        window.chrome.webview.postMessage(
                            document.hidden ? 'WEB_HIDDEN' : 'WEB_VISIBLE'
                        );
                    });
                ");

                // Configurar el User-Agent
                if (vvMobile)
                {
                    webMain.CoreWebView2.Settings.UserAgent =
                        "Mozilla/5.0 (Linux; Android 13; Pixel 7) " +
                        "AppleWebKit/537.36 (KHTML, like Gecko) " +
                        "Chrome/131.0.0.0 Mobile Safari/537.36";

                    // Registrar cursor móvil y evento F5
                    //await RegistrarPuntero();
                }
                await RegistrarPuntero();
                // Cargar la web
                webMain.CoreWebView2.Navigate(
                    "https://play.mumagdalenas.com/"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar la web: " + ex.Message,
                    "WebView2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private async Task ReiniciarWebMainAsync()
        {
            // Cerrar la navegación actual
            webMain.CoreWebView2?.Stop();

            // Liberar el control y sus recursos
            webMain.Dispose();

            // Crear un nuevo control WebView2
            webMain = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };

            // Agregarlo nuevamente al formulario
            this.Controls.Add(webMain);

            // Volver a inicializarlo con el mismo perfil
            await CargarWebMainAsync();
        }
        private async Task RegistrarPuntero()
        {
            if (webMain.CoreWebView2 == null) return;
            string tamañoCursor = vvMobile ? "20px" : "2px";

            await webMain.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                (() => {{
                    function instalar() {{
                        if (document.getElementById('kapi-cursor-movil')) return;

                        const estilo = document.createElement('style');
                        estilo.textContent = `
                            #kapi-cursor-movil {{
                                position: fixed;
                                width: {tamañoCursor};
                                height: {tamañoCursor};
                                box-sizing: border-box;
                                border: 2px solid white;
                                border-radius: 50%;
                                background: rgba(70,140,255,.30);
                                box-shadow: 0 0 3px black;
                                pointer-events: none;
                                z-index: 2147483647;
                                transform: translate(-50%,-50%);
                                display: none;
                                transition: transform 90ms, background 90ms;
                            }}
                            #kapi-cursor-movil.activo {{
                                background: rgba(40,115,255,.60);
                                transform: translate(-50%,-50%) scale(.80);
                            }}
                        `;

                        document.documentElement.appendChild(estilo);

                        const cursor = document.createElement('div');
                        cursor.id = 'kapi-cursor-movil';
                        document.documentElement.appendChild(cursor);

                        const mover = e => {{
                            cursor.style.left = e.clientX + 'px';
                            cursor.style.top = e.clientY + 'px';
                            cursor.style.display = 'block';
                        }};

                        document.addEventListener('pointermove', mover, true);
                        document.addEventListener('pointerdown', e => {{
                            mover(e);
                            cursor.classList.add('activo');
                        }}, true);
                        document.addEventListener('pointerup', () =>
                            cursor.classList.remove('activo'), true);
                        document.addEventListener('pointerleave', e => {{
                            if (e.target === document.documentElement)
                                cursor.style.display = 'none';
                        }}, true);
                    }}

                    if (document.readyState === 'loading')
                        document.addEventListener('DOMContentLoaded', instalar, {{ once: true }});
                    else instalar();
                }})();
            ");
        }
        #endregion

        #region 5. Control de teclado y modos
        private void WebMain_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje = e.TryGetWebMessageAsString();

            switch (mensaje)
            {
                case "WEB_BLUR":
                    ctsPulsacion?.Cancel();
                    break;
                case "WEB_FOCUS":
                case "WEB_VISIBLE":
                    ActualizarTitulo();
                    break;
                case "WEB_HIDDEN":
                    ctsPulsacion?.Cancel();
                    break;
                default:
                    TeclaApretada(mensaje);
                    break;
            }
        }
        private void WebMain_WebMessageReceived2(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string tecla = e.TryGetWebMessageAsString();
            TeclaApretada(tecla);
        }
        private void TeclaApretada(string tecla)
        {
            if (tecla == "F5")
            {
                DetenerClicks();
                CambiarVista();
            }
            else if (tecla == "F7")
            {
                ObtenerPoint();
                AlternarClicks();
            }
        }
        private async void CambiarVista()
        {
            vvMobile = !vvMobile;
            await ReiniciarWebMainAsync();
        }
        #endregion

        #region 6. Clic automático
        private async void AlternarClicks()
        {
            if (clicksActivos)
            {
                DetenerClicks();
                return;
            }

            if (!puntoGuardado)
            {
                MessageBox.Show(
                    "Primero guarda un punto",
                    "Clic automático",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            clicksActivos = true;
            webMain.Enabled = false;
            ctsClicks = new CancellationTokenSource();

            CancellationTokenSource ctsActual = ctsClicks;
            CancellationToken token = ctsActual.Token;

            ActualizarTitulo();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await ClickCanvasAsync(token);
                    }
                    catch (OperationCanceledException)
                        when (!token.IsCancellationRequested)
                    {
                        // Se canceló solo la pulsación por pérdida de foco.
                        // Reintentar inmediatamente.
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    Random random = new Random();

                    int pausa = vvMobile
                        ? random.Next(pausaCIMinMs, pausaCIMaxMs + 1)
                        : random.Next(pausaCDMinMs, pausaCDMaxMs + 1);

                    await Task.Delay(pausa, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal mediante F7 o F5.
            }
            finally
            {
                if (ReferenceEquals(ctsClicks, ctsActual))
                {
                    ctsPulsacion?.Cancel();

                    ctsClicks = null;
                    clicksActivos = false;

                    ctsActual.Dispose();

                    webMain.Enabled = true;
                    ActualizarTitulo();
                }
            }
        }
        private void DetenerClicks()
        {
            clicksActivos = false;

            // Cancelar la pulsación actual y el ciclo completo.
            ctsPulsacion?.Cancel();
            ctsClicks?.Cancel();

            webMain.Enabled = true;
            webMain.Focus();
            Point centro = new Point(
                webMain.ClientSize.Width / 2,
                webMain.ClientSize.Height / 2
            );
            Point posicionPantalla = webMain.PointToScreen(centro);

            // Mover el mouse físico al centro de WebView2
            Cursor.Position = posicionPantalla;
            ActualizarTitulo();

        }

        #endregion

        #region 7. Coordenadas y acciones del ratón
        private void ObtenerPoint()
        {
            // Obtener la posición del mouse relativa a webMain
            Point punto = webMain.PointToClient(Cursor.Position);

            // Validar que el mouse esté dentro del control
            if (!webMain.ClientRectangle.Contains(punto))
            {
                puntoGuardado = false;

                MessageBox.Show(
                    "El cursor del mouse debe estar dentro del área del Juego.",
                    "Cursor fuera de la página",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            // Guardar el punto
            puntoVentana = punto;
            puntoGuardado = true;
        }
        private async Task ClickCanvasAsync(CancellationToken token)
        {
            if (webMain.CoreWebView2 == null || !puntoGuardado)
                return;

            try
            {
                // Obtener las dimensiones del viewport de Chromium.
                string resultado =
                    await webMain.CoreWebView2.ExecuteScriptAsync(
                        "JSON.stringify({" +
                        "w: window.innerWidth," +
                        "h: window.innerHeight," +
                        "dpr: window.devicePixelRatio" +
                        "})");

                string json =
                    System.Text.Json.JsonSerializer.Deserialize<string>(resultado);

                using var datos = System.Text.Json.JsonDocument.Parse(json);

                double ancho = datos.RootElement.GetProperty("w").GetDouble();
                double alto = datos.RootElement.GetProperty("h").GetDouble();
                double dpr = datos.RootElement.GetProperty("dpr").GetDouble();

                // Convertir las coordenadas de WinForms al viewport.
                double escalaDpi = dpr * webMain.ZoomFactor;

                int x = (int)Math.Round(puntoVentana.X / escalaDpi);
                int y = (int)Math.Round(puntoVentana.Y / escalaDpi);

                // Comprobar que el punto esté dentro del viewport.
                if (x < 0 || y < 0 || x >= ancho || y >= alto)
                {
                    MessageBox.Show(
                        $"Punto fuera del viewport.\n" +
                        $"Punto: ({x}, {y})\n" +
                        $"Viewport: ({ancho}, {alto})\n" +
                        $"DPR: {dpr}",
                        "Coordenadas");

                    return;
                }

                token.ThrowIfCancellationRequested();

                // Mantener el comportamiento original:
                // MOB = clic izquierdo; PC = clic derecho.
                bool clickDerecho = !vvMobile;

                Random random = new Random();

                int duracion;
                int pausa;

                if (vvMobile)
                {
                    // MOB: clic izquierdo
                    duracion = random.Next(
                        presionCIMinMs,
                        presionCIMaxMs + 1);

                    pausa = random.Next(
                        pausaCIMinMs,
                        pausaCIMaxMs + 1);
                }
                else
                {
                    // PC: clic derecho
                    duracion = random.Next(
                        presionCDMinMs,
                        presionCDMaxMs + 1);

                    pausa = random.Next(
                        pausaCDMinMs,
                        pausaCDMaxMs + 1);
                }

                // Delegar la ejecución del clic.
                await EjecutarClickAsync(
                    x,
                    y,
                    clickDerecho,
                    duracion,
                    pausa,
                    token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al hacer clic: " + ex.Message,
                    "WebView2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async Task EjecutarClickAsync(  int x,
                                                int y,
                                                bool clickDerecho,
                                                int tiempoPresionMs,
                                                int tiempoPausaMs,
                                                CancellationToken token)
        {
            if (webMain.CoreWebView2 == null)
                return;

            token.ThrowIfCancellationRequested();

            string boton = clickDerecho ? "right" : "left";
            int botones = clickDerecho ? 2 : 1;

            bool presionado = false;
            bool canceladoPorPerdidaFoco = false;

            using (var ctsLocal =
                CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                ctsPulsacion = ctsLocal;

                try
                {
                    await webMain.CoreWebView2.CallDevToolsProtocolMethodAsync(
                        "Input.dispatchMouseEvent",
                        $@"{{
                    ""type"": ""mousePressed"",
                    ""x"": {x},
                    ""y"": {y},
                    ""button"": ""{boton}"",
                    ""buttons"": {botones},
                    ""clickCount"": 1
                }}");

                    presionado = true;

                    await Task.Delay(tiempoPresionMs, ctsLocal.Token);
                }
                catch (OperationCanceledException)
                {
                    canceladoPorPerdidaFoco = !token.IsCancellationRequested;
                    throw;
                }
                finally
                {
                    try
                    {
                        if (presionado && webMain.CoreWebView2 != null)
                        {
                            await webMain.CoreWebView2.CallDevToolsProtocolMethodAsync(
                                "Input.dispatchMouseEvent",
                                $@"{{
                                    ""type"": ""mouseReleased"",
                                    ""x"": {x},
                                    ""y"": {y},
                                    ""button"": ""{boton}"",
                                    ""buttons"": 0,
                                    ""clickCount"": 1
                                }}");
                        }
                    }
                    finally
                    {
                        if (ReferenceEquals(ctsPulsacion, ctsLocal))
                            ctsPulsacion = null;
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            if (canceladoPorPerdidaFoco)
                return;

            await Task.Delay(tiempoPausaMs, token);
        }
        #endregion

    }
}
