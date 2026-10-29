using Microsoft.Web.WebView2.Core;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using static Kapi_Mu_Utility.HelperResponse;

namespace Kapi_Mu_Utility
{
    public partial class FrmWeb : Form
    {
        #region 1. Variables
        // Duración de click Izquierdo
        private readonly int presionCIMinMs = 25;
        private readonly int presionCIMaxMs = 60;
        private readonly int pausaCIMinMs = 35;
        private readonly int pausaCIMaxMs = 100;

        // Duracion de Click Derecho
        private readonly int presionCDMinMs = 2 * 60 * 1000;  // 2 minutos
        private readonly int presionCDMaxMs = 5 * 60 * 1000;  // 5 minutos
        private readonly int pausaCDMinMs = 2 * 1000;        // 2 segundos
        private readonly int pausaCDMaxMs = 5 * 1000;        // 5 segundos
        //
        private int enProceso = 0;
        private string vvRuta;
        private string vvPerfil;
        private bool vvMobile = false;
        private Point puntoVentana;
        private bool puntoGuardado = false;
        private CancellationTokenSource? ctsClicks;
        private bool clicksActivos = false;
        private CancellationTokenSource? ctsPulsacion;
        private readonly OCRHelper ocr;
        private CancellationTokenSource? ctsOCR;
        private readonly SemaphoreSlim semaforoOCR = new SemaphoreSlim(1, 1);
        private readonly System.Collections.Concurrent.ConcurrentQueue<AccionOCR> colaAcciones = new();

        private readonly SemaphoreSlim senalAcciones = new(0);

        // Constantes de Windows API
        private const int WM_SIZING = 0x0214;
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_BOTTOM = 6;

        // Estructura para capturar los límites del formulario
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        //Estilo de Formulario
        private bool ventanaConFoco = true;
        private bool redimensionamientoBloqueado = false;
        private FormBorderStyle estiloBordeOriginal;
        private readonly OCRService servicioOCR;

        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int atributo,
            ref int valor,
            int tamano);

        #endregion

        #region 2. Constructor y título
        public FrmWeb(string vvRuta, string vvPerfil, OCRService servicioOCR)
        {
            InitializeComponent();
            //
            estiloBordeOriginal = this.FormBorderStyle;

            this.Activated += (s, e) =>
            {
                ventanaConFoco = true;
                ActualizarEstadoVisual();
            };

            this.Deactivate += (s, e) =>
            {
                ventanaConFoco = false;
                ActualizarEstadoVisual();
            };
            //

            this.MinimizeBox = false;
            this.vvRuta = vvRuta;
            this.vvPerfil = vvPerfil;
            ocr = new OCRHelper(servicioOCR);
            //this.Width = 960;
            //this.Height = 540;
            ActualizarTitulo();
        }
        private void ActualizarTitulo()
        {
            this.Text = $"{vvPerfil} {(vvMobile ? "MOB" : "PC")} " +
                $"{this.Width} x {this.Height} " +
                $"{(clicksActivos ? "clicking" : "")}";
                        
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
            ActualizarTitulo();
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
                // Registrar tráfico HTTP/HTTPS en el log
                //RegistrarTraficoWebView2();

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
            ObtenerPoint();
            if (!puntoGuardado)
            {
                MessageBox.Show(
                    "Primero guarda un punto",
                    "Clic automático",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }
            LimpiarColaAccionesOCR();

            clicksActivos = true;
            BloquearRedimensionamiento(true);
            ActualizarEstadoVisual();
            webMain.Enabled = false;
            //Inicia OCR
            ctsOCR = new CancellationTokenSource();
            _ = EjecutarOCR(ctsOCR.Token);

            //Inicia Clicks
            ctsClicks = new CancellationTokenSource();
            CancellationTokenSource ctsActual = ctsClicks;
            CancellationToken token = ctsActual.Token;

            ActualizarTitulo();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();

                    // 1. La cola tiene prioridad absoluta.
                    bool procesada = await ProcesarAccionesOCR(token);

                    if (procesada)
                        continue;

                    try
                    {
                        // 2. Solo si no hay acciones pendientes,
                        // ejecutar el clic principal.
                        await ClickCanvasAsync(token);
                    }
                    catch (OperationCanceledException)
                        when (!token.IsCancellationRequested)
                    {
                        // Una acción OCR interrumpió el clic.
                        // Volver al inicio para atender la cola.
                        continue;
                    }

                    token.ThrowIfCancellationRequested();
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
                    BloquearRedimensionamiento(false);
                    ActualizarEstadoVisual();

                    ctsActual.Dispose();

                    webMain.Enabled = true;
                    ActualizarTitulo();
                }
            }
        }
        private void DetenerClicks()
        {
            clicksActivos = false;

            //Detener OCR
            ctsOCR?.Cancel();
            ctsOCR?.Dispose();
            ctsOCR = null;

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
                    // Mover el puntero dentro del viewport de Chromium.
                    await webMain.CoreWebView2.CallDevToolsProtocolMethodAsync(
                        "Input.dispatchMouseEvent",
                        $@"{{
                            ""type"": ""mouseMoved"",
                            ""x"": {x},
                            ""y"": {y},
                            ""button"": ""none"",
                            ""buttons"": 0
                        }}");

                    ctsLocal.Token.ThrowIfCancellationRequested();

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

        #region Captura Imagen
        private async Task<byte[]> CapturarImagenAsync()
        {
            if (webMain.CoreWebView2 == null)
                throw new InvalidOperationException(
                    "WebView2 todavía no está inicializado.");

            using (var stream = new MemoryStream())
            {
                await webMain.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream);

                return stream.ToArray();
            }
        }
        private async Task ControlEvaluarImagen(CancellationToken token)
        {
            if (!await semaforoOCR.WaitAsync(0))
                return;

            try
            {
                bool resultadoDetectado = await EvaluarImagen(token);

                if (resultadoDetectado)
                {
                    // Mantener el semáforo ocupado durante 3 segundos.
                    await Task.Delay(3000, token);
                }
            }
            finally
            {
                semaforoOCR.Release();
            }
        }
        private async Task<bool> EvaluarImagen(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                byte[] imagen = await CapturarImagenAsync();

                int anchoCaptura;
                int altoCaptura;

                using (var ms = new MemoryStream(imagen))
                using (var bitmap = new Bitmap(ms))
                {
                    anchoCaptura = bitmap.Width;
                    altoCaptura = bitmap.Height;
                }

                // Obtener las dimensiones CSS del viewport.
                string dimensiones =
                    await webMain.CoreWebView2.ExecuteScriptAsync(
                        "JSON.stringify({" +
                        "width: window.innerWidth," +
                        "height: window.innerHeight" +
                        "})");

                // Primero deserializar la cadena devuelta por WebView2.
                string json = System.Text.Json.JsonSerializer.Deserialize<string>(
                            dimensiones);

                using var doc = System.Text.Json.JsonDocument.Parse(json);

                int anchoCSS = doc.RootElement.GetProperty("width").GetInt32();

                int altoCSS = doc.RootElement.GetProperty("height").GetInt32();

                token.ThrowIfCancellationRequested();

                HelperResponse resultado = await Task.Run(
                    () => ocr.ValidarImagen(imagen),
                    token);

                token.ThrowIfCancellationRequested();

                // Validar antes de acceder a IdAction.
                if (resultado == null)
                    return false;

                if (resultado.Cursor.HasValue)
                {
                    Point puntoCSS = ConvertirCapturaACSS(
                        resultado.Cursor.Value,
                        anchoCaptura,
                        altoCaptura,
                        anchoCSS,
                        altoCSS);
                    switch (resultado.IdAction)
                    {
                        case ActionType.Trade:
                            TradeEvent(puntoCSS);
                            return true;

                        case ActionType.Reconexion:
                            ReconexionEvent(puntoCSS);
                            return true;

                        default:
                            return false;
                    }
                }
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al evaluar imagen: {ex}");
                return false;
            }
        }
        private void ReconexionEvent(Point puntoCSS)
        {
            // Impedir duplicados mientras el Trade esté pendiente o ejecutándose.
            if (Interlocked.CompareExchange(ref enProceso, 1, 0) != 0)
                return;

            EncolarAccionOCR(new AccionOCR
            {
                Cursor = puntoCSS,
                ClickDerecho = false,
                DuracionMs = 5,
                PausaMs = 500,
                EsperarCarga = true
            });
            //TODO EvaluarImagen si es que aun tiene el boton de trade

        }
        private void TradeEvent(Point puntoCSS)
        {
            // Impedir duplicados mientras el Trade esté pendiente o ejecutándose.
            if (Interlocked.CompareExchange(ref enProceso, 1, 0) != 0)
                return;

            EncolarAccionOCR(new AccionOCR
            {
                Cursor = puntoCSS,
                ClickDerecho = false,
                DuracionMs = 5,
                PausaMs = 500
            });
            //TODO EvaluarImagen si es que aun tiene el boton de trade

        }
        private Point ConvertirCapturaACSS(
                                            Point punto,
                                            int anchoCaptura,
                                            int altoCaptura,
                                            int anchoCSS,
                                            int altoCSS)
        {
            int x = (int)Math.Round(
                (double)punto.X * anchoCSS / anchoCaptura);

            int y = (int)Math.Round(
                (double)punto.Y * altoCSS / altoCaptura);

            return new Point(x, y);
        }
        private async Task EjecutarOCR(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (clicksActivos)
                {
                    try
                    {
                        await ControlEvaluarImagen(token);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Error OCR: {ex.Message}");
                    }
                }

                await Task.Delay(1000, token);
            }
        }
        private void EncolarAccionOCR(AccionOCR accion)
        {
            if (!colaAcciones.IsEmpty)
                return;
            colaAcciones.Enqueue(accion);

            // Interrumpir la pulsación en curso.
            ctsPulsacion?.Cancel();

            // Despertar al controlador principal.
            senalAcciones.Release();
        }

        private async Task<bool> ProcesarAccionesOCR(CancellationToken token)
        {
            bool procesada = false;

            while (colaAcciones.TryDequeue(out AccionOCR accion))
            {
                token.ThrowIfCancellationRequested();

                procesada = true;

                try
                {
                    // Espera inicial antes de ejecutar la acción OCR.
                    await Task.Delay(1000, token);
                    token.ThrowIfCancellationRequested();

                    if (accion.Cursor.HasValue)
                    {
                        Point punto = accion.Cursor.Value;

                        await EjecutarClickAsync(
                            punto.X,
                            punto.Y,
                            accion.ClickDerecho,
                            accion.DuracionMs,
                            accion.PausaMs,
                            token);
                    }

                    // Si es una reconexión, esperar a que el juego
                    // vuelva a estar cargado antes de continuar.
                    if (accion.EsperarCarga)
                    {
                        await EsperarCargaJuegoAsync(token);
                    }
                }
                finally
                {
                    // Liberar el bloqueo incluso si se cancela la operación.
                    Interlocked.Exchange(ref enProceso, 0);
                }
            }

            return procesada;
        }
        private async Task EsperarCargaJuegoAsync(CancellationToken token)
        {
            if (webMain.CoreWebView2 == null)
                return;

            bool resultado = false;

            while (!resultado)
            {
                token.ThrowIfCancellationRequested();

                byte[] imagen = await CapturarImagenAsync();

                resultado = await Task.Run(
                    () => ocr.ValidarJuego(imagen),
                    token
                );

                if (!resultado)
                {
                    await Task.Delay(500, token); // Espera 500 ms antes de reintentar
                }
            }

            // El juego ya está validado; continuar con el siguiente proceso.
        }
        private void LimpiarColaAccionesOCR()
        {
            while (colaAcciones.TryDequeue(out _))
            {
                // Descartar todas las acciones pendientes.
            }

            // Eliminar las señales pendientes acumuladas.
            while (senalAcciones.Wait(0))
            {
                // Consumir señales antiguas.
            }
        }

        #endregion

        private async void RegistrarTraficoWebView2()
        {
            string logPath = Path.Combine(vvRuta, "WebView.log");

            await webMain.EnsureCoreWebView2Async();

            webMain.CoreWebView2.WebResourceResponseReceived +=
                async (sender, e) =>
                {
                    try
                    {
                        var req = e.Request;
                        var res = e.Response;
                        var sb = new StringBuilder();

                        sb.AppendLine("========================================");
                        sb.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        sb.AppendLine($"Método: {req.Method}");
                        sb.AppendLine($"URL destino: {req.Uri}");

                        sb.AppendLine("\n--- REQUEST HEADERS ---");
                        foreach (var h in req.Headers)
                            sb.AppendLine($"{h.Key}: {h.Value}");

                        sb.AppendLine("\n--- RESPONSE ---");
                        sb.AppendLine($"Estado HTTP: {res.StatusCode}");
                        sb.AppendLine($"Descripción: {res.ReasonPhrase}");

                        sb.AppendLine("\n--- RESPONSE HEADERS ---");
                        foreach (var h in res.Headers)
                            sb.AppendLine($"{h.Key}: {h.Value}");

                        string contentType = "";
                        try
                        {
                            contentType = res.Headers.GetHeader("Content-Type") ?? "";
                        }
                        catch { }

                        if (contentType.Contains("text/") ||
                        contentType.Contains("json") ||
                        contentType.Contains("xml") ||
                        contentType.Contains("javascript"))
                        {
                            using Stream stream = await res.GetContentAsync();

                            if (stream != null)
                            {
                                using var ms = new MemoryStream();
                                byte[] buffer = new byte[8192];
                                int total = 0, read;

                                while (total < 1024 * 1024 &&
                            (read = await stream.ReadAsync(
                                buffer, 0,
                                Math.Min(buffer.Length,
                                    1024 * 1024 - total))) > 0)
                                {
                                    await ms.WriteAsync(buffer, 0, read);
                                    total += read;
                                }

                                sb.AppendLine("\n--- RESPONSE BODY ---");
                                sb.AppendLine(Encoding.UTF8.GetString(ms.ToArray()));

                                if (total >= 1024 * 1024)
                                    sb.AppendLine("[Truncado: límite 1 MB]");
                            }
                        }

                        sb.AppendLine();

                        await File.AppendAllTextAsync(
                    logPath, sb.ToString(), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await File.AppendAllTextAsync(
                        logPath,
                        $"Error registrando tráfico: {ex.Message}\n",
                        Encoding.UTF8);
                        }
                        catch { }
                    }
                };
        }
        private void ActualizarEstadoVisual()
        {
            if (!IsHandleCreated)
                return;

            Color color;

            if (clicksActivos)
            {
                color = ventanaConFoco
                    ? Color.FromArgb(120, 255, 140)  // Verde claro
                    : Color.FromArgb(0, 100, 45);    // Verde oscuro
            }
            else
            {
                color = ventanaConFoco
                    ? Color.FromArgb(75, 75, 75)     // Gris oscuro
                    : Color.FromArgb(128, 128, 128);  // Gris normal
            }

            int colorBgr =
                color.R | (color.G << 8) | (color.B << 16);

            DwmSetWindowAttribute(
                Handle,
                DWMWA_BORDER_COLOR,
                ref colorBgr,
                sizeof(int));

            // El brillo real no está disponible mediante esta API.
            // Se puede simular con un borde personalizado si se desea.
        }
        private void BloquearRedimensionamiento(bool bloquear)
        {
            if (redimensionamientoBloqueado == bloquear)
                return;

            redimensionamientoBloqueado = bloquear;

            FormBorderStyle = bloquear
                ? FormBorderStyle.FixedSingle
                : estiloBordeOriginal;
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SIZING)
            {
                RECT rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));

                int anchoActual = rc.Right - rc.Left;
                int altoActual = rc.Bottom - rc.Top;

                // Relación de aspecto deseada (16:9 -> 16 / 9 = 1.77777778)
                double relacionAspecto = 16.0 / 9.0;

                // Determinar qué borde está moviendo el usuario para ajustar el otro lado
                int ladoModificado = m.WParam.ToInt32();

                if (ladoModificado == WMSZ_LEFT || ladoModificado == WMSZ_RIGHT)
                {
                    // Si cambia el ancho, ajustamos el alto
                    rc.Bottom = rc.Top + (int)(anchoActual / relacionAspecto);
                }
                else if (ladoModificado == WMSZ_TOP || ladoModificado == WMSZ_BOTTOM)
                {
                    // Si cambia el alto, ajustamos el ancho
                    rc.Right = rc.Left + (int)(altoActual * relacionAspecto);
                }
                else
                {
                    // Si arrastra desde una esquina, priorizamos ajustar por el ancho
                    rc.Bottom = rc.Top + (int)(anchoActual / relacionAspecto);
                }

                ActualizarTitulo();
                // Guardar los nuevos valores calculados en la memoria del mensaje
                Marshal.StructureToPtr(rc, m.LParam, false);
            }

            base.WndProc(ref m);
        }

    }
}
