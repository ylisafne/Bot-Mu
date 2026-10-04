using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Kapi_Mu_Utility;

public sealed class MuAutomation : IAsyncDisposable
{
    private const int ChromeDebugPort = 9222;

    private const string ChromeExe =
        @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    private const string ChromeProfile =
        @"C:\ChromeDebug";

    private const string GameUrl =
        "https://play.mumagdalenas.com/";

    private const string GameDomain =
        "mumagdalenas.com";

    private const int SleepMs = 100;

    private readonly HttpClient _httpClient = new();

    private CdpClient? _cdp;

    private CancellationTokenSource? _loopCts;

    private readonly AutomationSettings _settings;

    private int _numeroVuelta = 1;

    public bool Activa { get; private set; }

    public event Action<string>? Log;

    public AutomationSettings Settings =>
        _settings;

    public MuAutomation(AutomationSettings settings)
    {
        _settings = settings;
    }

    // =========================================================
    // CHROME
    // =========================================================

    public static void LanzarChrome()
    {
        const string chromeExe =
            @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        const int chromeDebugPort = 9222;

        const string chromeProfile =
            @"C:\ChromeDebug";

        const string gameUrl =
            "https://play.mumagdalenas.com/";

        if (!File.Exists(chromeExe))
        {
            throw new FileNotFoundException(
                "No se encontró Google Chrome.",
                chromeExe);
        }

        var psi = new ProcessStartInfo
        {
            FileName = chromeExe,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add(
            $"--remote-debugging-port={chromeDebugPort}");

        psi.ArgumentList.Add(
            "--remote-allow-origins=*");

        psi.ArgumentList.Add(
            $"--user-data-dir={chromeProfile}");

        psi.ArgumentList.Add(gameUrl);

        Process.Start(psi);
    }


    // =========================================================
    // BUSCAR PESTAÑA
    // =========================================================

    private async Task<JsonElement?> BuscarJuegoAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url =
                $"http://127.0.0.1:{ChromeDebugPort}/json";

            string json =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            using JsonDocument document =
                JsonDocument.Parse(json);

            foreach (JsonElement tab
                     in document.RootElement.EnumerateArray())
            {
                if (!tab.TryGetProperty(
                        "type",
                        out JsonElement type))
                    continue;

                if (!tab.TryGetProperty(
                        "url",
                        out JsonElement urlElement))
                    continue;

                string? typeValue =
                    type.GetString();

                string urlValue =
                    urlElement.GetString() ?? "";

                if (typeValue != "page")
                    continue;

                if (!urlValue.Contains(
                        GameDomain,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                return tab.Clone();
            }
        }
        catch
        {
            // Chrome/CDP todavía no disponible.
        }

        return null;
    }

    // =========================================================
    // OBTENER JUEGO
    // =========================================================

    private async Task<JsonElement?> ObtenerJuegoAsync(
        CancellationToken cancellationToken)
    {
        LogMessage("Buscando MuMagdalenas...");

        JsonElement? juego =
            await BuscarJuegoAsync(cancellationToken);

        if (juego.HasValue)
        {
            LogMessage("Pestaña de MuMagdalenas encontrada.");

            return juego;
        }

        LogMessage(
            "MuMagdalenas no está abierto.");

        LogMessage(
            "Lanzando Chrome...");

        LanzarChrome();

        LogMessage(
            "Esperando Chrome...");

        for (int i = 0; i < 40; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            juego =
                await BuscarJuegoAsync(
                    cancellationToken);

            if (juego.HasValue)
                break;

            await Task.Delay(
                500,
                cancellationToken);
        }

        if (!juego.HasValue)
        {
            LogMessage(
                "No se encontró Chrome/CDP.");

            return null;
        }

        LogMessage(
            "Esperando MuMagdalenas...");

        for (int i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            juego =
                await BuscarJuegoAsync(
                    cancellationToken);

            if (juego.HasValue)
            {
                LogMessage(
                    "Pestaña encontrada.");

                return juego;
            }

            await Task.Delay(
                500,
                cancellationToken);
        }

        return null;
    }

    // =========================================================
    // CONECTAR CDP
    // =========================================================

    private async Task ConectarCdpAsync(
        CancellationToken cancellationToken)
    {
        JsonElement? juego =
            await ObtenerJuegoAsync(
                cancellationToken);

        if (!juego.HasValue)
            throw new Exception(
                "No se pudo encontrar MuMagdalenas.");

        if (!juego.Value.TryGetProperty(
                "webSocketDebuggerUrl",
                out JsonElement wsElement))
        {
            throw new Exception(
                "La pestaña no tiene WebSocket CDP.");
        }

        string wsUrl =
            wsElement.GetString()
            ?? throw new Exception(
                "WebSocket CDP inválido.");

        _cdp = new CdpClient();

        LogMessage(
            "Conectando con Chrome DevTools...");

        await _cdp.ConnectAsync(
            wsUrl,
            cancellationToken);

        LogMessage(
            "CDP conectado.");
    }

    // =========================================================
    // ESPERAR CANVAS
    // =========================================================

    private async Task EsperarCanvasAsync(
        CancellationToken cancellationToken)
    {
        if (_cdp == null)
            throw new InvalidOperationException(
                "CDP no conectado.");

        LogMessage(
            "Esperando canvas...");

        for (int i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using JsonDocument result =
                await _cdp.SendCommandAsync(
                    "Runtime.evaluate",
                    new
                    {
                        expression =
                            "!!document.getElementById('canvas')",

                        returnByValue = true
                    },
                    cancellationToken);

            try
            {
                bool existe =
                    result.RootElement
                        .GetProperty("result")
                        .GetProperty("result")
                        .GetProperty("value")
                        .GetBoolean();

                if (existe)
                {
                    LogMessage(
                        "Canvas encontrado.");

                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(
                500,
                cancellationToken);
        }

        throw new Exception(
            "No se encontró el canvas.");
    }

    // =========================================================
    // ENFOCAR CANVAS
    // =========================================================

    private async Task EnfocarCanvasAsync(
        CancellationToken cancellationToken)
    {
        if (_cdp == null)
            return;

        using JsonDocument result =
            await _cdp.SendCommandAsync(
                "Runtime.evaluate",
                new
                {
                    expression = """
                    (() => {
                        const canvas =
                            document.getElementById("canvas");

                        if (!canvas)
                            return false;

                        canvas.focus();

                        return document.activeElement === canvas;
                    })()
                    """,

                    returnByValue = true
                },
                cancellationToken);

        try
        {
            bool enfocado =
                result.RootElement
                    .GetProperty("result")
                    .GetProperty("result")
                    .GetProperty("value")
                    .GetBoolean();

            LogMessage(
                enfocado
                    ? "Canvas enfocado."
                    : "No se pudo confirmar el foco.");
        }
        catch
        {
        }
    }

    // =========================================================
    // TECLAS CDP
    // =========================================================

    private async Task EnviarTeclaCdpAsync(
        string key,
        string code,
        int virtualKey,
        CancellationToken cancellationToken)
    {
        if (_cdp == null)
            return;

        await _cdp.SendCommandAsync(
            "Input.dispatchKeyEvent",
            new
            {
                type = "rawKeyDown",

                windowsVirtualKeyCode =
                    virtualKey,

                nativeVirtualKeyCode =
                    virtualKey,

                key,
                code,

                text = "",
                unmodifiedText = ""
            },
            cancellationToken);

        await Task.Delay(
            SleepMs,
            cancellationToken);

        await _cdp.SendCommandAsync(
            "Input.dispatchKeyEvent",
            new
            {
                type = "keyUp",

                windowsVirtualKeyCode =
                    virtualKey,

                nativeVirtualKeyCode =
                    virtualKey,

                key,
                code
            },
            cancellationToken);

        await Task.Delay(
            SleepMs,
            cancellationToken);
    }

    // =========================================================
    // ESC
    // =========================================================

    private async Task EnviarEscapeAsync(
        CancellationToken cancellationToken)
    {
        LogMessage("  -> ESC x2");

        await EnviarTeclaCdpAsync(
            "Escape",
            "Escape",
            27,
            cancellationToken);

        await EnviarTeclaCdpAsync(
            "Escape",
            "Escape",
            27,
            cancellationToken);
    }

    // =========================================================
    // ENTER
    // =========================================================

    private async Task EnviarEnterAsync(
        CancellationToken cancellationToken)
    {
        LogMessage("  -> ENTER");

        await EnviarTeclaCdpAsync(
            "Enter",
            "Enter",
            13,
            cancellationToken);
    }

    // =========================================================
    // VENTANA MU
    // =========================================================

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(
        IntPtr hWnd,
        StringBuilder lpString,
        int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(
        IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(
        EnumWindowsProc lpEnumFunc,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(
        IntPtr hWnd,
        int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(
        IntPtr hWnd);

    private delegate bool EnumWindowsProc(
        IntPtr hWnd,
        IntPtr lParam);

    private IntPtr BuscarVentanaMu()
    {
        IntPtr encontrada = IntPtr.Zero;

        EnumWindows(
            (hwnd, _) =>
            {
                int length =
                    GetWindowTextLength(hwnd);

                if (length == 0)
                    return true;

                var buffer =
                    new StringBuilder(
                        length + 1);

                GetWindowText(
                    hwnd,
                    buffer,
                    buffer.Capacity);

                string titulo =
                    buffer.ToString();

                if (titulo.Contains(
                        "MU Online",
                        StringComparison.OrdinalIgnoreCase))
                {
                    encontrada = hwnd;

                    return false;
                }

                return true;
            },
            IntPtr.Zero);

        return encontrada;
    }

    private bool ActivarVentanaMu()
    {
        IntPtr hwnd =
            BuscarVentanaMu();

        if (hwnd == IntPtr.Zero)
        {
            LogMessage(
                "No se encontró la ventana de MU Online.");

            return false;
        }

        // SW_RESTORE
        ShowWindow(hwnd, 9);

        SetForegroundWindow(hwnd);

        LogMessage(
            "Ventana de MU Online activada.");

        return true;
    }

    // =========================================================
    // SEND INPUT - TEXTO
    // =========================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const uint KEYEVENTF_UNICODE = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize);

    private static void SendUnicodeChar(
        char character)
    {
        ushort scanCode =
            character;

        INPUT down = new()
        {
            type = INPUT_KEYBOARD,

            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags =
                        KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        INPUT up = new()
        {
            type = INPUT_KEYBOARD,

            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags =
                        KEYEVENTF_UNICODE |
                        KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        INPUT[] inputs =
        [
            down,
            up
        ];

        SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<INPUT>());
    }

    private async Task EscribirTextoAsync(
        string texto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        LogMessage(
            $"  -> ESCRIBIENDO: {texto}");

        foreach (char character in texto)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SendUnicodeChar(character);

            await Task.Delay(
                SleepMs,
                cancellationToken);
        }
    }

    // =========================================================
    // MOUSE DERECHO
    // =========================================================

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_RIGHTDOWN =
        0x0008;

    private const uint MOUSEEVENTF_RIGHTUP =
        0x0010;

    private void MantenerClickDerecho()
    {
        LogMessage(
            "  -> BOTÓN DERECHO PRESIONADO");

        mouse_event(
            MOUSEEVENTF_RIGHTDOWN,
            0,
            0,
            0,
            UIntPtr.Zero);
    }

    private void SoltarClickDerecho()
    {
        mouse_event(
            MOUSEEVENTF_RIGHTUP,
            0,
            0,
            0,
            UIntPtr.Zero);

        LogMessage(
            "  -> BOTÓN DERECHO LIBERADO");
    }

    // =========================================================
    // UNA VUELTA
    // =========================================================

    private async Task<bool> EjecutarUnaVueltaAsync(
        int numero,
        CancellationToken cancellationToken)
    {
        LogMessage("");
        LogMessage("--------------------------------------");
        LogMessage(
            $" VUELTA #{numero}");
        LogMessage("--------------------------------------");

        cancellationToken.ThrowIfCancellationRequested();

        if (!ActivarVentanaMu())
        {
            LogMessage(
                "No se puede continuar.");

            return false;
        }

        await EnfocarCanvasAsync(
            cancellationToken);

        await Task.Delay(
            SleepMs,
            cancellationToken);

        await EnviarEscapeAsync(
            cancellationToken);

        foreach (string comando
                 in _settings.Comandos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogMessage("");
            LogMessage(
                $"-> {comando}");

            await EnviarEnterAsync(
                cancellationToken);

            await EscribirTextoAsync(
                comando,
                cancellationToken);

            await Task.Delay(
                SleepMs,
                cancellationToken);

            await EnviarEnterAsync(
                cancellationToken);
        }

        // Texto adicional.
        if (!string.IsNullOrWhiteSpace(
                _settings.Otros))
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogMessage("");

            await EnviarEnterAsync(
                cancellationToken);

            await EscribirTextoAsync(
                _settings.Otros,
                cancellationToken);

            await Task.Delay(
                SleepMs,
                cancellationToken);

            await EnviarEnterAsync(
                cancellationToken);
        }

        // TODO:
        // chkVolver
        // nudX
        // nudY
        // txtMapa

        return true;
    }

    // =========================================================
    // INTERVALO ALEATORIO
    // =========================================================

    private int ObtenerIntervaloMs()
    {
        decimal from =
            _settings.FromSeconds;

        decimal to =
            _settings.ToSeconds;

        if (to < from)
            (from, to) = (to, from);

        if (from == to)
            return (int)(from * 1000);

        double valor =
            Random.Shared.NextDouble();

        double segundos =
            (double)from +
            ((double)to - (double)from) *
            valor;

        return (int)(segundos * 1000);
    }

    // =========================================================
    // INICIAR
    // =========================================================

    public async Task StartAsync()
    {
        if (Activa)
            return;

        Activa = true;

        _numeroVuelta = 1;

        _loopCts =
            new CancellationTokenSource();

        CancellationToken token =
            _loopCts.Token;

        try
        {
            await ConectarCdpAsync(token);

            await EsperarCanvasAsync(token);

            LogMessage("");
            LogMessage(
                "======================================");

            LogMessage(
                " LOOP INICIADO");

            LogMessage(
                "======================================");

            LogMessage(
                $"Intervalo: {_settings.FromSeconds} - " +
                $"{_settings.ToSeconds} segundos");

            MantenerClickDerecho();

            while (
                Activa &&
                !token.IsCancellationRequested)
            {
                bool resultado =
                    await EjecutarUnaVueltaAsync(
                        _numeroVuelta,
                        token);

                if (!resultado)
                    break;

                _numeroVuelta++;

                int intervalo =
                    ObtenerIntervaloMs();

                LogMessage("");

                LogMessage(
                    $"Esperando " +
                    $"{intervalo / 1000.0:0.00} segundos...");

                await Task.Delay(
                    intervalo,
                    token);
            }
        }
        catch (OperationCanceledException)
        {
            LogMessage(
                "Loop cancelado.");
        }
        catch (Exception ex)
        {
            LogMessage(
                $"ERROR: {ex.Message}");
        }
        finally
        {
            Activa = false;

            SoltarClickDerecho();

            _loopCts?.Dispose();

            _loopCts = null;

            LogMessage("");

            LogMessage(
                "======================================");

            LogMessage(
                " LOOP DETENIDO");

            LogMessage(
                "======================================");
        }
    }

    // =========================================================
    // DETENER
    // =========================================================

    public void Stop()
    {
        if (!Activa)
            return;

        LogMessage(
            "Deteniendo...");

        Activa = false;

        _loopCts?.Cancel();

        // Seguridad.
        SoltarClickDerecho();
    }

    // =========================================================
    // LOG
    // =========================================================

    private void LogMessage(string message)
    {
        Log?.Invoke(message);
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public async ValueTask DisposeAsync()
    {
        Stop();

        if (_cdp != null)
        {
            await _cdp.DisposeAsync();
            _cdp = null;
        }

        _httpClient.Dispose();
    }
}


// =============================================================
// CONFIGURACIÓN
// =============================================================

public sealed class AutomationSettings
{
    public List<string> Comandos { get; init; } = [];

    public string Otros { get; init; } = "";

    public decimal FromSeconds { get; init; }

    public decimal ToSeconds { get; init; }

    public bool Volver { get; init; }

    public decimal X { get; init; }

    public decimal Y { get; init; }

    public string Mapa { get; init; } = "";
}