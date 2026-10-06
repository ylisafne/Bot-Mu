using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Kapi_Mu_Utility;

public sealed class MuAutomation : IAsyncDisposable
{
    private const int ClienteDebugPort = 9223;
    private const string ClienteExe = @"C:\Users\ylisa\AppData\Local\Programs\MU Magdalenas\MU Magdalenas.exe";
    private const int SleepMs = 60;

    private readonly HttpClient _httpClient = new();
    private readonly object _connectionLock = new();
    private AutomationSettings _settings;
    private CdpClient? _cdp;
    private Process? _clienteProceso;
    private CancellationTokenSource? _loopCts;
    private Task? _monitorConexionTask;
    private int _numeroVuelta = 1;
    private bool _limpiando;
    private bool _preparando;

    public bool Activa { get; private set; }
    public bool CdpConectado => _cdp?.IsConnected == true;
    public event Action<string>? Log;
    public event Action<bool>? ConexionCambiada;

    public MuAutomation(AutomationSettings settings) { _settings = settings; }

    public void ActualizarConfiguracion(AutomationSettings settings)
    {
        if (!Activa) _settings = settings;
    }

    private void LogMessage(string mensaje) => Log?.Invoke(mensaje);

    private void ConexionPerdida(string motivo)
    {
        if (_limpiando) return;
        LogMessage($"CONEXIÓN PERDIDA: {motivo}");
        Activa = false;
        try { _loopCts?.Cancel(); } catch { }
        SoltarClickDerecho();
        ConexionCambiada?.Invoke(false);
    }

    private async Task MonitorConexionAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                CdpClient? cdp;
                lock (_connectionLock) cdp = _cdp;
                if (cdp == null) { ConexionPerdida("CDP no disponible."); return; }
                if (!cdp.IsConnected) { ConexionPerdida("WebSocket desconectado."); return; }
                await Task.Delay(250, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested) ConexionPerdida($"Error monitor CDP: {ex.Message}");
        }
    }

    private static Process? BuscarProcesoCliente()
    {
        string nombre = Path.GetFileNameWithoutExtension(ClienteExe);
        foreach (Process proceso in Process.GetProcessesByName(nombre))
        {
            try { if (!proceso.HasExited) return proceso; } catch { }
        }
        return null;
    }

    private static IntPtr BuscarVentanaProceso(Process proceso)
    {
        IntPtr encontrada = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId != proceso.Id || !IsWindowVisible(hwnd)) return true;
            int length = GetWindowTextLength(hwnd);
            if (length <= 0) return true;
            StringBuilder titulo = new(length + 1);
            GetWindowText(hwnd, titulo, titulo.Capacity);
            if (titulo.ToString().Equals("MU Online", StringComparison.OrdinalIgnoreCase))
            {
                encontrada = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return encontrada;
    }

    private static async Task<bool> EsperarVentanaAsync(Process proceso, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { if (proceso.HasExited) return false; } catch { return false; }
            proceso.Refresh();
            if (BuscarVentanaProceso(proceso) != IntPtr.Zero) return true;
            await Task.Delay(500, cancellationToken);
        }
        return false;
    }

    private static async Task CerrarProcesoAsync(Process proceso, CancellationToken cancellationToken)
    {
        try
        {
            if (proceso.HasExited) return;
            IntPtr hwnd = BuscarVentanaProceso(proceso);
            if (hwnd != IntPtr.Zero) { try { proceso.CloseMainWindow(); } catch { } }

            for (int i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { if (proceso.HasExited) return; } catch { return; }
                await Task.Delay(250, cancellationToken);
            }

            try { if (!proceso.HasExited) proceso.Kill(true); } catch { }
            try { await proceso.WaitForExitAsync(cancellationToken); } catch { }
        }
        catch { }
    }

    private static async Task<Process> LanzarClienteAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ClienteExe))
            throw new FileNotFoundException("No se encontró MU Magdalenas.", ClienteExe);

        var psi = new ProcessStartInfo
        {
            FileName = ClienteExe,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(ClienteExe) ?? Environment.CurrentDirectory
        };

        psi.ArgumentList.Add($"--remote-debugging-port={ClienteDebugPort}");

        Process? proceso = Process.Start(psi);
        if (proceso == null) throw new Exception("No se pudo iniciar MU Magdalenas.");

        await EsperarVentanaAsync(proceso, cancellationToken);

        IntPtr hwnd = BuscarVentanaProceso(proceso);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }

        return proceso;
    }

    private async Task<JsonElement?> BuscarClienteCdpAsync(CancellationToken cancellationToken)
    {
        string url = $"http://127.0.0.1:{ClienteDebugPort}/json";

        try
        {
            string json = await _httpClient.GetStringAsync(url, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            foreach (JsonElement tab in document.RootElement.EnumerateArray())
            {
                if (!tab.TryGetProperty("type", out JsonElement type)) continue;
                if (type.GetString() != "page") continue;
                if (!tab.TryGetProperty("webSocketDebuggerUrl", out _)) continue;
                return tab.Clone();
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        return null;
    }

    private async Task<bool> ConectarClienteAsync(CancellationToken cancellationToken)
    {
        CdpClient? nuevo = null;

        try
        {
            JsonElement? cliente = await BuscarClienteCdpAsync(cancellationToken);
            if (!cliente.HasValue) return false;

            if (!cliente.Value.TryGetProperty("webSocketDebuggerUrl", out JsonElement wsElement))
                return false;

            string? wsUrl = wsElement.GetString();
            if (string.IsNullOrWhiteSpace(wsUrl)) return false;

            nuevo = new CdpClient();
            await nuevo.ConnectAsync(wsUrl, cancellationToken);

            CdpClient? anterior;
            lock (_connectionLock)
            {
                anterior = _cdp;
                _cdp = nuevo;
                nuevo = null;
            }

            if (anterior != null) await anterior.DisposeAsync();

            ConexionCambiada?.Invoke(true);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch { return false; }
        finally
        {
            if (nuevo != null)
            {
                try { await nuevo.DisposeAsync(); } catch { }
            }
        }
    }

    public async Task<bool> PrepararClienteAsync(CancellationToken cancellationToken = default)
    {
        if (_preparando) return CdpConectado;
        _preparando = true;

        try
        {
            CdpClient? anterior;
            lock (_connectionLock)
            {
                anterior = _cdp;
                _cdp = null;
            }

            if (anterior != null) await anterior.DisposeAsync();

            LogMessage("Buscando MU Magdalenas.exe...");
            Process? proceso = BuscarProcesoCliente();

            if (proceso != null)
            {
                LogMessage($"Proceso encontrado. PID: {proceso.Id}");
                _clienteProceso = proceso;

                IntPtr hwnd = BuscarVentanaProceso(proceso);
                if (hwnd != IntPtr.Zero)
                {
                    LogMessage("Ventana MU Online encontrada.");
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                }

                LogMessage("Intentando conectar al CDP 9223...");

                for (int i = 0; i < 5; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (await ConectarClienteAsync(cancellationToken))
                    {
                        LogMessage("Cliente existente conectado.");
                        return true;
                    }

                    await Task.Delay(300, cancellationToken);
                }

                LogMessage("El cliente existente no responde por CDP.");
                LogMessage("Cerrando cliente...");
                await CerrarProcesoAsync(proceso, cancellationToken);
                _clienteProceso = null;
            }

            for (int i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (BuscarProcesoCliente() == null) break;
                await Task.Delay(250, cancellationToken);
            }

            LogMessage("Iniciando MU Magdalenas con CDP 9223...");
            _clienteProceso = await LanzarClienteAsync(cancellationToken);
            LogMessage($"Cliente iniciado. PID: {_clienteProceso.Id}");

            for (int i = 0; i < 60; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await ConectarClienteAsync(cancellationToken))
                {
                    LogMessage("CDP conectado correctamente.");

                    IntPtr hwnd = BuscarVentanaProceso(_clienteProceso);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_RESTORE);
                        SetForegroundWindow(hwnd);
                    }

                    return true;
                }

                await Task.Delay(500, cancellationToken);
            }

            LogMessage("No se pudo conectar al CDP.");
            ConexionCambiada?.Invoke(false);
            return false;
        }
        finally
        {
            _preparando = false;
        }
    }

    private async Task EnfocarClienteAsync(CancellationToken cancellationToken)
    {
        if (_clienteProceso == null) throw new Exception("El proceso del cliente no está disponible.");

        IntPtr hwnd = BuscarVentanaProceso(_clienteProceso);
        if (hwnd == IntPtr.Zero) throw new Exception("No se encontró la ventana MU Online.");

        ShowWindow(hwnd, SW_RESTORE);
        await Task.Delay(100, cancellationToken);
        SetForegroundWindow(hwnd);
        await Task.Delay(100, cancellationToken);
    }

    private async Task EnviarTeclaCdpAsync(string key, string code, int virtualKey, CancellationToken cancellationToken)
    {
        CdpClient? cdp;
        lock (_connectionLock) cdp = _cdp;

        if (cdp == null || !cdp.IsConnected)
            throw new WebSocketException("CDP no está conectado.");

        LogMessage($"CDP KEY DOWN: {key}");

        await cdp.SendCommandAsync("Input.dispatchKeyEvent", new
        {
            type = "keyDown",
            key,
            code,
            windowsVirtualKeyCode = virtualKey,
            nativeVirtualKeyCode = virtualKey,
            text = "",
            unmodifiedText = ""
        }, cancellationToken);

        await Task.Delay(SleepMs, cancellationToken);

        LogMessage($"CDP KEY UP: {key}");

        await cdp.SendCommandAsync("Input.dispatchKeyEvent", new
        {
            type = "keyUp",
            key,
            code,
            windowsVirtualKeyCode = virtualKey,
            nativeVirtualKeyCode = virtualKey
        }, cancellationToken);

        await Task.Delay(SleepMs, cancellationToken);
    }

    private async Task EnviarEscapeAsync(CancellationToken cancellationToken)
    {
        LogMessage("ENVIANDO ESC x2 POR CDP");
        await EnviarTeclaCdpAsync("Escape", "Escape", 27, cancellationToken);
        await EnviarTeclaCdpAsync("Escape", "Escape", 27, cancellationToken);
    }

    private async Task EnviarEnterAsync(CancellationToken cancellationToken)
    {
        LogMessage("ENVIANDO ENTER POR CDP");
        await EnviarTeclaCdpAsync("Enter", "Enter", 13, cancellationToken);
    }

    private static (string key, string code, int vk) ObtenerTecla(char caracter)
    {
        if (caracter >= 'a' && caracter <= 'z')
        {
            char mayuscula = char.ToUpperInvariant(caracter);
            return (mayuscula.ToString(), $"Key{mayuscula}", mayuscula);
        }

        if (caracter >= 'A' && caracter <= 'Z')
            return (caracter.ToString(), $"Key{caracter}", caracter);

        if (caracter >= '0' && caracter <= '9')
            return (caracter.ToString(), $"Digit{caracter}", caracter);

        return caracter switch
        {
            ' ' => (" ", "Space", 32),
            '/' => ("/", "Slash", 191),
            '-' => ("-", "Minus", 189),
            '.' => (".", "Period", 190),
            ':' => (":", "Semicolon", 186),
            '_' => ("_", "Minus", 189),
            _ => (caracter.ToString(), "", caracter)
        };
    }

    private async Task EscribirTextoCdpAsync(string texto, CancellationToken cancellationToken)
    {
        LogMessage($"ESCRIBIENDO POR CDP: [{texto}]");

        foreach (char caracter in texto)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CdpClient? cdp;
            lock (_connectionLock) cdp = _cdp;

            if (cdp == null || !cdp.IsConnected)
                throw new WebSocketException("CDP se desconectó.");

            var tecla = ObtenerTecla(caracter);

            LogMessage($"CDP TEXTO: {caracter}");

            await cdp.SendCommandAsync("Input.dispatchKeyEvent", new
            {
                type = "keyDown",
                key = tecla.key,
                code = tecla.code,
                windowsVirtualKeyCode = tecla.vk,
                nativeVirtualKeyCode = tecla.vk,
                text = caracter.ToString(),
                unmodifiedText = caracter.ToString()
            }, cancellationToken);

            await Task.Delay(SleepMs, cancellationToken);

            await cdp.SendCommandAsync("Input.dispatchKeyEvent", new
            {
                type = "keyUp",
                key = tecla.key,
                code = tecla.code,
                windowsVirtualKeyCode = tecla.vk,
                nativeVirtualKeyCode = tecla.vk
            }, cancellationToken);

            await Task.Delay(SleepMs, cancellationToken);
        }

        LogMessage("FIN ESCRIBIR CDP");
    }

    private async Task EnfocarCanvasAsync(CancellationToken cancellationToken)
    {
        CdpClient? cdp;
        lock (_connectionLock) cdp = _cdp;

        if (cdp == null || !cdp.IsConnected)
            throw new WebSocketException("CDP no está conectado.");

        LogMessage("Enfocando canvas WebGL...");

        await cdp.SendCommandAsync("Runtime.evaluate", new
        {
            expression = """
            (() => {
                const canvas = document.getElementById("canvas");
                if (!canvas) return false;
                canvas.focus();
                return document.activeElement === canvas;
            })()
            """,
            returnByValue = true
        }, cancellationToken);

        await Task.Delay(100, cancellationToken);
    }

    private async Task EjecutarUnaVueltaAsync(CancellationToken cancellationToken)
    {
        LogMessage($"VUELTA #{_numeroVuelta}");

        await EnfocarClienteAsync(cancellationToken);
        await EnfocarCanvasAsync(cancellationToken);
        await EnviarEscapeAsync(cancellationToken);

        foreach (string comando in _settings.Comandos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogMessage($"ENVIANDO: {comando}");

            await EnviarEnterAsync(cancellationToken);
            await EscribirTextoCdpAsync(comando, cancellationToken);
            await EnviarEnterAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(_settings.Otros))
        {
            await EnviarOtrosComoTeclasAsync(
                _settings.Otros,
                cancellationToken);
        }
    }

    private async Task EnviarOtrosComoTeclasAsync(string texto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return;

        LogMessage($"ENVIANDO TECLAS FINALES: [{texto}]");

        foreach (char caracter in texto)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (char.IsDigit(caracter))
            {
                string key = caracter.ToString();
                await EnviarTeclaCdpAsync(key, $"Digit{caracter}", caracter, cancellationToken);
                continue;
            }

            if (char.IsLetter(caracter))
            {
                char letra = char.ToUpperInvariant(caracter);
                await EnviarTeclaCdpAsync(letra.ToString(), $"Key{letra}", letra, cancellationToken);
                continue;
            }

            switch (caracter)
            {
                case ' ':
                    await EnviarTeclaCdpAsync(" ", "Space", 32, cancellationToken);
                    break;

                case '/':
                    await EnviarTeclaCdpAsync("/", "Slash", 191, cancellationToken);
                    break;

                case '-':
                    await EnviarTeclaCdpAsync("-", "Minus", 189, cancellationToken);
                    break;

                case '.':
                    await EnviarTeclaCdpAsync(".", "Period", 190, cancellationToken);
                    break;

                default:
                    LogMessage($"TECLA FINAL NO SOPORTADA: [{caracter}]");
                    break;
            }
        }

        LogMessage("FIN TECLAS FINALES");
    }


    private int ObtenerIntervaloMs()
    {
        decimal from = _settings.FromSeconds;
        decimal to = _settings.ToSeconds;

        if (to < from) (from, to) = (to, from);
        if (from == to) return (int)(from * 1000);

        double valor = Random.Shared.NextDouble();
        double segundos = (double)from + ((double)to - (double)from) * valor;
        return (int)(segundos * 1000);
    }

    private void MantenerClickDerecho()
    {
        if (_settings.clickDerecho)
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
    }

    private void SoltarClickDerecho()
    {
        if (_settings.clickDerecho)
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public async Task StartAsync()
    {
        if (Activa) return;

        if (!CdpConectado)
        {
            LogMessage("No se puede iniciar: CDP desconectado.");
            ConexionPerdida("CDP desconectado.");
            return;
        }

        LogMessage("========== INICIAR ==========");

        foreach (string comando in _settings.Comandos)
            LogMessage($"COMANDO: {comando}");

        LogMessage($"OTROS: [{_settings.Otros}]");

        Activa = true;
        _numeroVuelta = 1;
        _loopCts = new CancellationTokenSource();
        CancellationToken token = _loopCts.Token;

        _monitorConexionTask = MonitorConexionAsync(token);

        try
        {
            MantenerClickDerecho();

            while (Activa && !token.IsCancellationRequested)
            {
                await EjecutarUnaVueltaAsync(token);
                _numeroVuelta++;

                if (!Activa || token.IsCancellationRequested) break;

                int espera = ObtenerIntervaloMs();
                LogMessage($"Esperando {espera} ms.");
                await Task.Delay(espera, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                ConexionPerdida(ex.Message);
        }
        finally
        {
            Activa = false;
            SoltarClickDerecho();

            if (_monitorConexionTask != null)
            {
                try { await _monitorConexionTask; } catch { }
                _monitorConexionTask = null;
            }

            _loopCts?.Dispose();
            _loopCts = null;
        }
    }

    public void Stop()
    {
        Activa = false;
        try { _loopCts?.Cancel(); } catch { }
        SoltarClickDerecho();
    }

    public async ValueTask DisposeAsync()
    {
        _limpiando = true;
        Stop();

        if (_monitorConexionTask != null)
        {
            try { await _monitorConexionTask; } catch { }
            _monitorConexionTask = null;
        }

        CdpClient? cdp;
        lock (_connectionLock)
        {
            cdp = _cdp;
            _cdp = null;
        }

        if (cdp != null)
        {
            try { await cdp.DisposeAsync(); } catch { }
        }

        _httpClient.Dispose();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int SW_RESTORE = 9;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}

public sealed class AutomationSettings
{
    public List<string> Comandos { get; set; } = new();
    public string Otros { get; set; } = "";
    public decimal FromSeconds { get; set; }
    public decimal ToSeconds { get; set; }
    public bool Volver { get; set; }
    public bool clickDerecho { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string Mapa { get; set; } = "";
}
