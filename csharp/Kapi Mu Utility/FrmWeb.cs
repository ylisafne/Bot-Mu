using Microsoft.Web.WebView2.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private string NombrePersonaje;
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
        private TaskCompletionSource<bool>? _muInitializedTcs;
        private readonly HashSet<string> retentionProcesadas = new();

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
        private const int DWMWA_BORDER_COLOR = 34;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int atributo,
            ref int valor,
            int tamano);

        private volatile bool esperaReconexion = false;
        private static readonly object _lockConsoleLog = new();
        //Variables Para Color
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("User32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);



        private Icon? FrmIcon = null;
        //
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
            ocr = new OCRHelper(servicioOCR, vvPerfil);
            //this.Width = 960;
            //this.Height = 540;
            ActualizarTitulo("");
        }
        private void ActualizarTitulo(string estado)
        {
            this.Text = $"{vvPerfil} {(vvMobile ? "MOB" : "PC")} " +
                $"{this.Width} x {this.Height} " +
                $"{NombrePersonaje} " +
                $"{estado}";   
        }
        #endregion

        #region 3. Eventos del formulario
        private async void FrmWeb_Load(object sender, EventArgs e)
        {
            var formHeaderBackgroundColor = Color.FromArgb(32, 32, 32);
            var formHeaderTextColor = Color.FromArgb(255, 255, 255);

            var formHeaderBackgroundColorValue = (formHeaderBackgroundColor.B << 16) | (formHeaderBackgroundColor.G << 8) | formHeaderBackgroundColor.R;
            var formHeaderTextColorValue = (formHeaderTextColor.B << 16) | (formHeaderTextColor.G << 8) | formHeaderTextColor.R;
            DwmSetWindowAttribute(this.Handle, 35, ref formHeaderBackgroundColorValue, System.Runtime.InteropServices.Marshal.SizeOf(formHeaderBackgroundColorValue));
            DwmSetWindowAttribute(this.Handle, 36, ref formHeaderTextColorValue, System.Runtime.InteropServices.Marshal.SizeOf(formHeaderTextColorValue));
            
            
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
            ActualizarTitulo("Loading...");
            try
            {
                // Crear la carpeta del perfil
                Directory.CreateDirectory(vvRuta);

                // Crear el entorno de WebView2 con el perfil indicado
                var entorno = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: vvRuta
                );

                await webMain.EnsureCoreWebView2Async(entorno);

                //RegistrarTraficoWebView2();
                //RegistrarMonitorRecompensas();

                webMain.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                //webMain.ZoomFactor = 1.5; // 80 %

                webMain.CoreWebView2.WebMessageReceived -= WebMain_WebMessageReceived;
                webMain.CoreWebView2.WebMessageReceived += WebMain_WebMessageReceived;

                // F5 para ambos modos: MOB y PC
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
                        //window.chrome.webview.postMessage('WEB_FOCUS');
                    });

                    document.addEventListener('visibilitychange', () => {
                        window.chrome.webview.postMessage(
                            document.hidden ? 'WEB_HIDDEN' : 'WEB_VISIBLE'
                        );
                    });
                    function sendPostMessage(message) {
                        window.chrome.webview.postMessage(message);
                    }
                    /// 
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
                webMain.CoreWebView2.NavigationCompleted += async (sender, e) =>
                {
                    if (e.IsSuccess) {
                        await InstallAutoHelper();
                        await InstallAutoSessionReload();
                        await InstallRareAutoClaim();
                        await InstallAutoErrorReload();
                        ActualizarTitulo("");
                    }
                };
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
        private async Task InstallAutoSessionReload()
        {
            string script = @"
                (function () {
                    if (window.__autoSessionReload) return;
                    window.__autoSessionReload = true;
                    let reconnectTimer = null;

                    function checkModal() {
                        const modal = document.querySelector('.session-ended');
                        if (!modal) return;
                        if (modal.classList.contains('hidden')) {
                            clearTimeout(reconnectTimer);
                            reconnectTimer = null;
                            return;
                        }
                        const buttons = modal.querySelectorAll('button');
                        buttons.forEach(button => {
                            const text = button.textContent.trim();
                            if (
                                text === 'Volver a entrar' ||
                                text === 'Reload' ||
                                text === 'Reenter'
                            ) {
                                if (!button.disabled && !button.dataset.autoClicked) {
                                    if (reconnectTimer) return;
                                    reconnectTimer = setTimeout(() => {
                                        reconnectTimer = null;
                                        if (
                                            !modal.classList.contains('hidden') &&
                                            !button.disabled &&
                                            button.isConnected
                                        ) {
                                            window.chrome.webview.postMessage('SESSION_RECONNECT_CLICK');
                                            button.dataset.autoClicked = '1';
                                            button.click();
                                        }
                                    }, 30000);
                                }
                            }
                        });
                    }
                    const observer = new MutationObserver(checkModal);
                    observer.observe(document.documentElement, {
                        childList: true,
                        subtree: true,
                        attributes: true,
                        attributeFilter: ['class', 'style', 'disabled']
                    });
                    checkModal();
                })();";

            await webMain.CoreWebView2.ExecuteScriptAsync(script);
        }
        private async Task InstallAutoHelper()
        {
            string script = @"
            (function () {
                if (window.__autoHelperMode) return;
                window.__autoHelperMode = true;

                let timer = null;
                let confirmTimer = null;
                let attempts = 0;
                let confirming = false;
                let finished = false;

                const MAX_ATTEMPTS = 5;

                function visible(el) {
                    if (!el || !el.isConnected) return false;
                    const s = getComputedStyle(el);
                    const r = el.getBoundingClientRect();
                    return s.display !== 'none' &&
                           s.visibility !== 'hidden' &&
                           Number(s.opacity) > 0 &&
                           r.width > 0 && r.height > 0 &&
                           r.bottom > 0 && r.right > 0 &&
                           r.top < innerHeight && r.left < innerWidth;
                }

                function mode() {
                    return document.getElementById('mu-mobile-helper-auto')
                        ?.querySelector('span')?.textContent.trim().toUpperCase();
                }

                function send(message) {
                    try { window.chrome.webview.postMessage(message); } catch (e) {}
                }

                function success() {
                    if (finished) return;
                    finished = true;
                    clearTimeout(timer);
                    clearInterval(confirmTimer);
                    send('MU_INITIALIZED');
                }

                function error() {
                    if (finished) return;
                    finished = true;
                    clearTimeout(timer);
                    clearInterval(confirmTimer);
                    send('MU_ERROR');
                }

                function retry() {
                    confirming = false;
                    if (attempts >= MAX_ATTEMPTS) {
                        error();
                        return;
                    }
                    if (!timer) timer = setTimeout(() => {
                        timer = null;
                        activate();
                    }, 5000);
                }

                function activate() {
                    if (finished || confirming) return;

                    if (!visible(document.getElementById('mu-web-autoatk-btn')))
                        return;

                    if (mode() === 'AUTO') {
                        success();
                        return;
                    }

                    if (attempts >= MAX_ATTEMPTS) {
                        error();
                        return;
                    }

                    attempts++;

                    const button = document.getElementById('mu-mobile-helper-auto');

                    if (!button || mode() !== 'MANUAL') {
                        retry();
                        return;
                    }

                    confirming = true;
                    button.click();

                    const start = Date.now();

                    confirmTimer = setInterval(() => {
                        if (mode() === 'AUTO') {
                            clearInterval(confirmTimer);
                            confirmTimer = null;
                            success();
                            return;
                        }

                        const yes = document.querySelector('[data-confirm=""yes""]');

                        if (yes) {
                            clearInterval(confirmTimer);
                            confirmTimer = null;

                            yes.dispatchEvent(new PointerEvent('pointerdown', {
                                bubbles: true,
                                cancelable: true,
                                pointerType: 'mouse',
                                button: 0,
                                buttons: 1
                            }));

                            setTimeout(() => {
                                if (mode() === 'AUTO') success();
                                else retry();
                            }, 500);

                            return;
                        }

                        if (Date.now() - start > 5000) {
                            clearInterval(confirmTimer);
                            confirmTimer = null;
                            retry();
                        }
                    }, 100);
                }

                function check() {
                    if (finished) return;

                    const gameButton = document.getElementById('mu-web-autoatk-btn');

                    if (!visible(gameButton)) {
                        clearTimeout(timer);
                        timer = null;
                        clearInterval(confirmTimer);
                        confirmTimer = null;
                        confirming = false;
                        attempts = 0;
                        return;
                    }

                    if (mode() === 'AUTO') {
                        success();
                        return;
                    }

                    if (!timer && !confirming && attempts === 0) {
                        timer = setTimeout(() => {
                            timer = null;
                            activate();
                        }, 5000);
                    }
                }

                const observer = new MutationObserver(check);
                observer.observe(document.documentElement, {
                    childList: true,
                    subtree: true,
                    characterData: true,
                    attributes: true,
                    attributeFilter: ['style', 'class', 'hidden', 'disabled', 'aria-pressed']
                });

                window.addEventListener('resize', check);
                window.addEventListener('scroll', check, true);

                check();
            })();";
            await webMain.CoreWebView2.ExecuteScriptAsync(script);
        }
        private async Task InstallRareAutoClaim()
        {
            string script = @"
                (function () {
                    if (window.__rareAutoClaim) return;
                    window.__rareAutoClaim = true;

                    function checkRare() {
                        const button = document.querySelector(
                            '#mu-retention button[data-kind=""rare""]'
                        );

                        if (!button || button.disabled) return;
                        if (button.dataset.autoClicked === '1') return;
                        button.dataset.autoClicked = '1';
                        window.chrome.webview.postMessage('RARE_BUTTON_ENABLED');
                        button.click();
                    }
                    const observer = new MutationObserver(checkRare);
                    observer.observe(document.documentElement, {
                        childList: true,
                        subtree: true,
                        attributes: true,
                        attributeFilter: ['disabled', 'hidden', 'class']
                    });
                    checkRare();
                })();
                ";

            await webMain.CoreWebView2.ExecuteScriptAsync(script);
        }
        private async Task InstallAutoErrorReload()
        {
            string script = @"
                (function () {
                    if (window.__autoErrorReload) return;
                    window.__autoErrorReload = true;

                    function checkReload() {
                        const button = document.getElementById('mu-err-reload');

                        if (!button || button.disabled) return;

                        if (button.dataset.autoDetected === '1') return;

                        button.dataset.autoDetected = '1';

                        window.chrome.webview.postMessage('MU_ERROR_RELOAD_AVAILABLE');
                    }

                    const observer = new MutationObserver(checkReload);

                    observer.observe(document.documentElement, {
                        childList: true,
                        subtree: true,
                        attributes: true,
                        attributeFilter: ['disabled', 'style', 'class']
                    });

                    checkReload();
                })();";

            await webMain.CoreWebView2.ExecuteScriptAsync(script);
        }
        private async Task<bool> EsperarMuInicializadoAsync(CancellationToken token)
        {
            _muInitializedTcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            try
            {
                return await _muInitializedTcs.Task.WaitAsync(token);
            }
            finally
            {
                _muInitializedTcs = null;
            }
        }
        private async Task ReiniciarWebMainAsync()
        {
            _muInitializedTcs = new TaskCompletionSource<bool>(
                                    TaskCreationOptions.RunContinuationsAsynchronously
                                );
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
        private async Task GetCurrentCharacter()
        {
            try
            {
                string script = @"
                    (() => {
                        let name = '';

                        try {
                            name = muCallExportString(
                                '_MuWebGetHeroNameUtf8',
                                ''
                            ) || '';
                        } catch(e) {}

                        if (!name && typeof menuCurrentCharacterName !== 'undefined')
                            name = menuCurrentCharacterName || '';

                        return String(name).trim();
                    })();
                ";
                string resultado = await webMain.CoreWebView2.ExecuteScriptAsync(script);

                NombrePersonaje = System.Text.Json.JsonSerializer.Deserialize<string>(resultado) ?? "";
                Debug.WriteLine($"{vvPerfil} Personaje: {NombrePersonaje}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error obteniendo personaje: " + ex.Message);
            }
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
                case "MU_INITIALIZED":
                    _ = GetCurrentCharacter();
                    _muInitializedTcs?.TrySetResult(true);
                    break;
                case "MU_ERROR":
                    ReconexionEvent(new Point(10, 10));
                    _ = ReiniciarWebMainAsync();
                    break;
                case "MU_ERROR_RELOAD_AVAILABLE":
                    ReconexionEvent(new Point(10, 10));
                    _ = ReiniciarWebMainAsync();
                    break;
                case "RARE_BUTTON_ENABLED":
                    Debug.WriteLine($"{vvPerfil} Claim Jewel of Rare");
                    break;
                    
                case "SESSION_RECONNECT_CLICK":
                    ReconexionEvent(new Point(10,10));
                    break;
                case "WEB_BLUR":
                    ctsPulsacion?.Cancel();
                    break;
                case "WEB_FOCUS":
                case "WEB_VISIBLE":

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
        private async Task GuardarSkill(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                DatosCaptura imagen = await CapturarImagenAsync();

                bool skill = false;

                for (int i = 0; i < 5 && !skill; i++)
                {
                    token.ThrowIfCancellationRequested();

                    skill = await ocr.GuardarSkill(imagen.Imagen, vvRuta);

                    if (!skill && i < 4)
                        await Task.Delay(500, token);
                }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal: no mostrar errores.
            }
        }
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
            CambiarIcono();
            BloquearRedimensionamiento(true);
            
            ActualizarEstadoVisual();
            webMain.Enabled = false;
            //Inicia OCR
            ctsOCR = new CancellationTokenSource();
            _ = EjecutarOCR(ctsOCR.Token);
            _ = GuardarSkill(ctsOCR.Token);

            //Inicia Clicks
            ctsClicks = new CancellationTokenSource();
            CancellationTokenSource ctsActual = ctsClicks;
            CancellationToken token = ctsActual.Token;
            //_ = GetPersonaje(token);

            ActualizarTitulo("Clicking");
            await GetCurrentCharacter();

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
                        ActualizarTitulo("Clicking");
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
                }
            }
        }
        private async Task<string> DiagnosticarFetch(string url)
        {
            await webMain.EnsureCoreWebView2Async();

            string urlJS = System.Text.Json.JsonSerializer.Serialize(url);

            string script = $@"
                (async () => {{
                    const resultados = {{}};

                    try {{
                        const response = await fetch({urlJS}, {{
                            method: 'GET',
                            credentials: 'include',
                            mode: 'cors',
                            cache: 'no-store',
                            headers: {{
                                'Accept': 'application/json'
                            }}
                        }});

                        resultados.status = response.status;
                        resultados.statusText = response.statusText;
                        resultados.headers = Object.fromEntries(response.headers.entries());
                        resultados.body = await response.text();

                    }} catch (error) {{
                        resultados.error = error.message;
                        resultados.name = error.name;
                        resultados.stack = error.stack;
                    }}

                    window.chrome.webview.postMessage(
                        JSON.stringify(resultados)
                    );
                }})();
            ";

            var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            void Handler(
                object sender,
                Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
            {
                tcs.TrySetResult(e.TryGetWebMessageAsString());
            }

            webMain.CoreWebView2.WebMessageReceived += Handler;

            try
            {
                await webMain.CoreWebView2.ExecuteScriptAsync(script);

                string resultado = await tcs.Task.WaitAsync(
                    TimeSpan.FromSeconds(30)
                );
                using var json = System.Text.Json.JsonDocument.Parse(resultado);

                var root = json.RootElement;

                if (root.TryGetProperty("error", out var error))
                    throw new Exception(error.GetString());

                string body = root.GetProperty("body").GetString();
                return body;
            }
            finally
            {
                webMain.CoreWebView2.WebMessageReceived -= Handler;
            }
        }
        private async Task<string> FetchWebMain(string url, string metodo = "GET")
        {
            try
            {
                await webMain.EnsureCoreWebView2Async();
                //
                var cookies = await webMain.CoreWebView2.CookieManager.GetCookiesAsync(
    "https://server-new.mumagdalenas.com"
);

                foreach (var cookie in cookies)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"COOKIE: {cookie.Name} | Domain: {cookie.Domain} | " +
                        $"HttpOnly: {cookie.IsHttpOnly} | Secure: {cookie.IsSecure}"
                    );
                }
                //



                string origen = await webMain.CoreWebView2.ExecuteScriptAsync(
                    "window.location.origin"
                );

                string pagina = await webMain.CoreWebView2.ExecuteScriptAsync(
                    "window.location.href"
                );

                System.Diagnostics.Debug.WriteLine("========== WEBVIEW ==========");
                System.Diagnostics.Debug.WriteLine("ORIGEN ACTUAL: " + origen);
                System.Diagnostics.Debug.WriteLine("PÁGINA ACTUAL: " + pagina);
                System.Diagnostics.Debug.WriteLine("=============================");

                string urlJS = System.Text.Json.JsonSerializer.Serialize(url);
                string metodoJS = System.Text.Json.JsonSerializer.Serialize(metodo);

                string script = $@"
            (async () => {{
                try {{
                    const response = await fetch(
                        {urlJS},
                        {{
                            method: {metodoJS},
                            credentials: 'include',
                            headers: {{
                                'Accept': 'application/json'
                            }}
                        }}
                    );

                    const contenido = await response.text();

                    window.chrome.webview.postMessage(JSON.stringify({{
                        success: true,
                        status: response.status,
                        statusText: response.statusText,
                        ok: response.ok,
                        url: response.url,
                        headers: Object.fromEntries(response.headers.entries()),
                        body: contenido,
                        origin: window.location.origin,
                        page: window.location.href
                    }}));

                }} catch (error) {{
                    window.chrome.webview.postMessage(JSON.stringify({{
                        success: false,
                        error: error.message,
                        name: error.name,
                        stack: error.stack,
                        origin: window.location.origin,
                        page: window.location.href,
                        target: {urlJS},
                        cookiesAvailable: document.cookie.length > 0
                    }}));
                }}
            }})();
        ";

                var tcs = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously
                );

                void RecibirMensaje(
                    object sender,
                    Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
                {
                    string respuesta = e.TryGetWebMessageAsString();

                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine("========== RESPONSE FETCH ==========");
                    System.Diagnostics.Debug.WriteLine(respuesta);
                    System.Diagnostics.Debug.WriteLine("====================================");

                    tcs.TrySetResult(respuesta);
                }

                webMain.CoreWebView2.WebMessageReceived += RecibirMensaje;

                try
                {
                    System.Diagnostics.Debug.WriteLine("EJECUTANDO FETCH: " + url);

                    string resultadoJS =
                        await webMain.CoreWebView2.ExecuteScriptAsync(script);

                    System.Diagnostics.Debug.WriteLine(
                        "RESULTADO EJECUCION JS: " + resultadoJS
                    );

                    return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
                finally
                {
                    webMain.CoreWebView2.WebMessageReceived -= RecibirMensaje;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR FETCH: " + ex);
                return "";
            }
        }
        private void DetenerClicks()
        {
            clicksActivos = false;
            CambiarIcono();
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
            ActualizarTitulo("");

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
                    if (tiempoPresionMs <= 0)
                    {
                        await Task.Delay(tiempoPausaMs, token);
                        return;
                    }

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
        private async Task<DatosCaptura> CapturarImagenAsync()
        {
            if (webMain.CoreWebView2 == null)
                throw new InvalidOperationException(
                    "WebView2 todavía no está inicializado.");

            using (var stream = new MemoryStream())
            {
                await webMain.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream);

                byte[] imagen = stream.ToArray();

                int anchoCaptura;
                int altoCaptura;

                using (var ms = new MemoryStream(imagen))
                using (var bitmap = new Bitmap(ms))
                {
                    anchoCaptura = bitmap.Width;
                    altoCaptura = bitmap.Height;
                }

                string dimensiones =
                    await webMain.CoreWebView2.ExecuteScriptAsync(
                        "JSON.stringify({" +
                        "width: window.innerWidth," +
                        "height: window.innerHeight" +
                        "})");

                string json = System.Text.Json.JsonSerializer.Deserialize<string>(
                                                dimensiones);

                using var doc = System.Text.Json.JsonDocument.Parse(json);

                int anchoCSS = doc.RootElement
                    .GetProperty("width").GetInt32();

                int altoCSS = doc.RootElement
                    .GetProperty("height").GetInt32();

                return new DatosCaptura
                {
                    Imagen = imagen,
                    AnchoCaptura = anchoCaptura,
                    AltoCaptura = altoCaptura,
                    AnchoCSS = anchoCSS,
                    AltoCSS = altoCSS
                };
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

                DatosCaptura captura = await CapturarImagenAsync();

                token.ThrowIfCancellationRequested();

                HelperResponse resultado = await Task.Run(
                    () => ocr.ValidarImagen(captura.Imagen),
                    token);

                token.ThrowIfCancellationRequested();

                if (resultado == null)
                    return false;

                if (resultado.Cursor.HasValue)
                {
                    Point puntoCSS = ConvertirCapturaACSS(
                        resultado.Cursor.Value,
                        captura.AnchoCaptura,
                        captura.AltoCaptura,
                        captura.AnchoCSS,
                        captura.AltoCSS);

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
                    $"{vvPerfil} Error al evaluar imagen: {ex}");

                return false;
            }
        }
        private void ReconexionEvent(Point puntoCSS)
        {
            // Impedir duplicados mientras el Trade esté pendiente o ejecutándose.
            ActualizarTitulo("Reconnecting...");
            if (Interlocked.CompareExchange(ref enProceso, 1, 0) != 0)
                return;

            if (clicksActivos)
            {
                esperaReconexion = true;

                EncolarAccionOCR(new AccionOCR
                {
                    Cursor = puntoCSS,
                    ClickDerecho = false,
                    DuracionMs = 5,
                    PausaMs = 500,
                    EsperarCarga = true
                });
            }
            else {
                _ = EjecutarReconexionAsync();
            }
            //TODO EvaluarImagen si es que aun tiene el boton de trade

        }
        private async Task EjecutarReconexionAsync()
        {
            try
            {
                await EsperarCargaJuegoAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{vvPerfil} Error reconectando: {ex}");
            }
            finally
            {
                ActualizarTitulo("");
                Interlocked.Exchange(ref enProceso, 0);
            }
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
                if (clicksActivos && !esperaReconexion)
                {
                    try
                    {
                        await ControlEvaluarImagen(token);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"{vvPerfil} Error OCR: {ex.Message}");
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
                        await Task.Delay(1000, token);
                        esperaReconexion = false;
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
            while (true)
            {
                token.ThrowIfCancellationRequested();

                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil} Iniciando Juego Nuevamente"
                );

                using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);

                using var ctsEventos = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token);

                ctsTimeout.CancelAfter(TimeSpan.FromMinutes(1));

                try
                {
                    var tareaInicializacion = EsperarMuInicializadoAsync(ctsEventos.Token);
                    
                    var tareaImagen = BuscaPantallaSeleccion(
                        ctsEventos.Token
                    );

                    var tareaTimeout = Task.Delay(
                        Timeout.Infinite,
                        ctsTimeout.Token
                    );

                    var completada = await Task.WhenAny(
                        tareaInicializacion,
                        tareaImagen,
                        tareaTimeout
                    );

                    token.ThrowIfCancellationRequested();

                    // TIMEOUT
                    if (completada == tareaTimeout)
                    {
                        ctsEventos.Cancel();

                        try
                        {
                            await Task.WhenAll(tareaInicializacion, tareaImagen);
                        }
                        catch (OperationCanceledException) { }

                        System.Diagnostics.Debug.WriteLine(
                            $"{vvPerfil} Timeout de carga."
                        );

                        await ReiniciarWebMainAsync();
                        continue;
                    }

                    // CANCELAR EL EVENTO RESTANTE
                    ctsEventos.Cancel();

                    try
                    {
                        await Task.WhenAll(tareaInicializacion, tareaImagen);
                    }
                    catch (OperationCanceledException) { }

                    // EVENTO DE CONSOLA
                    if (completada == tareaInicializacion)
                    {
                        await tareaInicializacion;

                        System.Diagnostics.Debug.WriteLine(
                            $"{vvPerfil} Mensaje de consola recibido."
                        );

                        break;
                    }

                    // EVENTO DE IMAGEN
                    var punto = await tareaImagen;

                    System.Diagnostics.Debug.WriteLine(
                        $"{vvPerfil} Punto recibido desde pantalla de PJ."
                    );

                    if (punto == null)
                        continue;

                    // SELECCIONAR PERSONAJE ENVIANDO EL PUNTO
                    System.Diagnostics.Debug.WriteLine(
                        $"{vvPerfil} Seleccionando Personaje."
                    );

                    bool encontrado = await SeleccinaPersonaje(
                        punto.Value,
                        token
                    );

                    if (encontrado)
                        break;

                    await ReiniciarWebMainAsync();
                }
                catch (OperationCanceledException)
                    when (!token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"{vvPerfil} Timeout de carga."
                    );

                    await ReiniciarWebMainAsync();
                }
            }

            token.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine(
                $"{vvPerfil} Comparando Skill"
            );

            await SeleccionaSKill(token);
        }
        private async Task<Point?> BuscaPantallaSeleccion(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                DatosCaptura captura = await CapturarImagenAsync();

                token.ThrowIfCancellationRequested();

                HelperResponse resultado = await Task.Run(
                    () => ocr.DetectarSeleccionPj(captura.Imagen),
                    token
                );

                token.ThrowIfCancellationRequested();

                if (resultado != null)
                {
                    if (resultado.Cursor.HasValue)
                    {
                        Point puntoCSS = ConvertirCapturaACSS(
                            resultado.Cursor.Value,
                            captura.AnchoCaptura,
                            captura.AltoCaptura,
                            captura.AnchoCSS,
                            captura.AltoCSS);
                        //Mueve Mouse
                        await EjecutarClickAsync(
                            puntoCSS.X,
                            puntoCSS.Y,
                            false,
                            0,
                            500,
                            token
                        );
                        System.Diagnostics.Debug.WriteLine(
                            $"{vvPerfil}: Pantalla Personajes Encontrado"
                        );
                        return puntoCSS;
                    }
                }
                // Esperar un segundo antes de volver a buscar
                await Task.Delay(1000, token);
            }
        }


        private async Task<bool> SeleccinaPersonaje(Point puntoConnect, CancellationToken token)
        {
            ActualizarTitulo("Character Select...");
            for (int intento = 1; intento <= 5; intento++)
            {
                token.ThrowIfCancellationRequested();

                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil}: Buscando {NombrePersonaje} - Intento {intento}/5"
                );

                DatosCaptura imagen = await CapturarImagenAsync();

                Point? punto = await ocr.ObtenerPosicionPersonajeAsync(
                    imagen.Imagen,
                    NombrePersonaje,
                    token
                );
                
                if (punto.HasValue)
                {
                    Point puntoCSS = ConvertirCapturaACSS(
                        punto.Value,
                        imagen.AnchoCaptura,
                        imagen.AltoCaptura,
                        imagen.AnchoCSS,
                        imagen.AltoCSS
                    );

                    await EjecutarClickAsync(
                        puntoCSS.X,
                        puntoCSS.Y,
                        false,
                        0,
                        500,
                        token
                    );
                    await EjecutarClickAsync(
                        puntoCSS.X,
                        puntoCSS.Y,
                        false,
                        100,
                        2000,
                        token
                    );

                    await EjecutarClickAsync(
                        puntoConnect.X,
                        puntoConnect.Y,
                        false,
                        0,
                        500,
                        token
                    );
                    await EjecutarClickAsync(
                        puntoConnect.X,
                        puntoConnect.Y,
                        false,
                        200,
                        2000,
                        token
                    );
                    System.Diagnostics.Debug.WriteLine(
                        $"{vvPerfil}: Personaje encontrado en intento {intento}. " +
                        $"Punto {puntoCSS.X}, {puntoCSS.Y}"
                    );

                    return true;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil}: Personaje no encontrado en intento {intento}"
                );

                if (intento < 5)
                    await Task.Delay(1000, token);
            }

            System.Diagnostics.Debug.WriteLine(
                $"{vvPerfil}: Personaje no encontrado después de 5 intentos"
            );


            return false;
        }
        private async Task SeleccionaSKill(CancellationToken token)
        {
            const int maxIntentos = 5;

            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                token.ThrowIfCancellationRequested();

                DatosCaptura imagen = await CapturarImagenAsync();

                token.ThrowIfCancellationRequested();

                Point? punto = await ocr.ComparaSkill(imagen.Imagen, vvRuta);

                // Si devuelve null, el skill ya es el mismo.
                if (!punto.HasValue)
                {
                    Debug.WriteLine($"{vvPerfil} Skill correcto.");
                    return;
                }

                Debug.WriteLine($"{vvPerfil} Skill Diferente. Intento {intento}/{maxIntentos}");

                ActualizarTitulo($"Skill Select... ({intento}/{maxIntentos})");

                Point puntoCSS = ConvertirCapturaACSS(
                    punto.Value,
                    imagen.AnchoCaptura,
                    imagen.AltoCaptura,
                    imagen.AnchoCSS,
                    imagen.AltoCSS);

                await EjecutarClickAsync(
                    puntoCSS.X,
                    puntoCSS.Y,
                    false,
                    200,
                    1000,
                    token);

                await EjecutarClickAsync(
                    imagen.AnchoCSS / 2,
                    imagen.AltoCSS / 2,
                    false,
                    0,
                    500,
                    token);

                // Buscar el skill dentro del menú desplegado.
                DatosCaptura imagenSkills = await CapturarImagenAsync();

                Point? skillLocation = await ocr.BuscaSKill(
                    imagenSkills.Imagen,
                    vvRuta);

                if (skillLocation.HasValue)
                {
                    Point puntoSkillCSS = ConvertirCapturaACSS(
                        skillLocation.Value,
                        imagenSkills.AnchoCaptura,
                        imagenSkills.AltoCaptura,
                        imagenSkills.AnchoCSS,
                        imagenSkills.AltoCSS);

                    await EjecutarClickAsync(
                        puntoSkillCSS.X,
                        puntoSkillCSS.Y,
                        false,
                        0,
                        500,
                        token);

                    await EjecutarClickAsync(
                        puntoSkillCSS.X,
                        puntoSkillCSS.Y,
                        false,
                        50,
                        500,
                        token);
                }

                // Esperar antes de volver a comprobar.
                await Task.Delay(500, token);
            }
        }
        private async Task<string> EsperarMensajeConsolaAsync(  string palabra,
                                                                CancellationToken token)
        {
            string logPath = Path.Combine(vvRuta, "Console.log");
            Directory.CreateDirectory(vvRuta);

            var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void EscribirLog(string mensaje)
            {
                try
                {
                    lock (_lockConsoleLog)
                    {
                        File.AppendAllText(
                            logPath,
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensaje}{Environment.NewLine}",
                            Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error escribiendo Console.log: {ex}");
                }
            }

            void Handler(
                object? sender,
                CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
            {
                try
                {
                    string json = e.ParameterObjectAsJson;

                    // Registrar el evento completo para depuración.
                    //EscribirLog($"[EVENTO] {json}");

                    // Buscar también dentro de objetos, números y otros argumentos.
                    if (json.Contains(
                        palabra,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        tcs.TrySetResult(json);
                    }
                }
                catch (Exception ex)
                {
                    //EscribirLog($"[ERROR] {ex}");
                }
            }

            var consola = webMain.CoreWebView2
                .GetDevToolsProtocolEventReceiver(
                    "Runtime.consoleAPICalled");

            // Suscribirse antes de habilitar Runtime.
            consola.DevToolsProtocolEventReceived += Handler;

            try
            {
                // Crear el archivo desde el inicio de la búsqueda.
                //EscribirLog($"[INICIO] Buscando: '{palabra}'");

                await webMain.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Runtime.enable", "{}");

                return await tcs.Task.WaitAsync(token);
            }
            finally
            {
                consola.DevToolsProtocolEventReceived -= Handler;

                //EscribirLog($"[FIN] Búsqueda terminada: '{palabra}'");
            }
        }
        //
        private void RegistrarMonitorRecompensas()
        {
            //return;
            webMain.CoreWebView2.WebResourceResponseReceived += async (s, e) =>
            {
                try
                {
                    //
                    if (!e.Request.Uri.Contains("/api/auth/retention",
                            StringComparison.OrdinalIgnoreCase))
                        return;

                    string vvUrl = e.Request.Uri;

                    if (e.Response.StatusCode < 200 ||
                        e.Response.StatusCode >= 300)
                        return;

                    using var stream = await e.Response.GetContentAsync();

                    if (stream == null)
                        return;

                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory);

                    string json = Encoding.UTF8.GetString(memory.ToArray());

                    using var doc = JsonDocument.Parse(json);

                    if (!doc.RootElement.TryGetProperty("boxes", out var boxes))
                        return;

                    if (boxes.ValueKind != JsonValueKind.Array ||
                        boxes.GetArrayLength() == 0)
                        return;

                    foreach (var box in boxes.EnumerateArray())
                    {
                        string kind = box.TryGetProperty("kind", out var k)
                            ? k.GetString() ?? ""
                            : "";

                        string id = box.TryGetProperty("id", out var i)
                            ? i.ToString()
                            : "";

                        

                        if (kind == "rare"){
                            Debug.WriteLine($"{vvPerfil} Recompensa {kind} encontrada ID: {id}");
                            string result = await webMain.ExecuteScriptAsync(@"
                                (() => {
                                    const button = document.querySelector(
                                        '#mu-retention button[data-kind=""rare""]'
                                    );

                                    if (!button)
                                        return 'NOT_FOUND';

                                    if (button.disabled)
                                        return 'DISABLED';

                                    button.click();

                                    return 'CLICK_EXECUTED';
                                })()
                                ");

                            Console.WriteLine(result);

                            //string result = await webMain.CoreWebView2.ExecuteScriptAsync(script);

                            Debug.WriteLine($"{vvPerfil} Recompensa Reclamada {result}");
                        }
                    }
                    //
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{vvPerfil} Retention: {ex.Message}");
                }
            };
        }
        //

        private void CambiarIcono()
        {
            if (FrmIcon != null)
                FrmIcon = this.Icon;
            if (clicksActivos)
                this.Icon = System.Drawing.SystemIcons.Warning;
            else
                this.Icon = FrmIcon;
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
        
        private async Task MonitorearConsola()
        {
            string logPath = Path.Combine(vvRuta, "Console.log");

            var consola = webMain.CoreWebView2.GetDevToolsProtocolEventReceiver(
                "Runtime.consoleAPICalled");

            consola.DevToolsProtocolEventReceived += (s, e) =>
            {
                File.AppendAllText(
                    logPath,
                    $"[{DateTime.Now:HH:mm:ss}] {e.ParameterObjectAsJson}{Environment.NewLine}",
                    Encoding.UTF8);
            };

            await webMain.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Runtime.enable", "{}");
        }
        private async void MonitorearTrafico()
        {
            string logPath = Path.Combine(vvRuta, "WebView.log");

            // Semáforo local para sincronizar las escrituras
            var logSemaphore = new SemaphoreSlim(1, 1);

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

                        //
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

                                await stream.CopyToAsync(ms);

                                sb.AppendLine("\n--- RESPONSE BODY ---");
                                sb.AppendLine(Encoding.UTF8.GetString(ms.ToArray()));
                            }
                        }

                        sb.AppendLine();

                        // Escritura sincronizada
                        await logSemaphore.WaitAsync();

                        try
                        {
                            await File.AppendAllTextAsync(
                                logPath,
                                sb.ToString(),
                                Encoding.UTF8);
                        }
                        finally
                        {
                            logSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await logSemaphore.WaitAsync();

                            try
                            {
                                await File.AppendAllTextAsync(
                                    logPath,
                                    $"Error registrando tráfico: {ex.Message}\n",
                                    Encoding.UTF8);
                            }
                            finally
                            {
                                logSemaphore.Release();
                            }
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
                // Guardar los nuevos valores calculados en la memoria del mensaje
                Marshal.StructureToPtr(rc, m.LParam, false);
            }

            base.WndProc(ref m);
        }

    }
}
