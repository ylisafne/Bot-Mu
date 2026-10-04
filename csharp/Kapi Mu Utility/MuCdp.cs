using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kapi_Mu_Utility
{
    public class Mucdp
    {
        private const int CDP_PORT = 9222;

        private const string MU_EXE =
            @"C:\Users\ylisa\AppData\Local\Programs\MU Magdalenas\MU Magdalenas.exe";

        private const string MU_DIRECTORY =
            @"C:\Users\ylisa\AppData\Local\Programs\MU Magdalenas";


        private readonly HttpClient httpClient;

        private ClientWebSocket? socket;

        private Process? procesoMu;

        private int contadorCdp;


        // =========================================================
        // ESTADO
        // =========================================================

        public bool EstaConectado
        {
            get
            {
                return socket != null &&
                       socket.State == WebSocketState.Open;
            }
        }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public Mucdp()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            contadorCdp = 0;
        }


        // =========================================================
        // CONECTAR O LANZAR
        // =========================================================

        public async Task ConectarOIniciarAsync()
        {
            Debug.WriteLine(
                "======================================");

            Debug.WriteLine(
                " Mucdp.ConectarOIniciarAsync");

            Debug.WriteLine(
                "======================================");


            // -----------------------------------------------------
            // 1. ¿YA EXISTE CDP?
            // -----------------------------------------------------

            bool cdpExiste =
                await ExisteCdpAsync();


            if (!cdpExiste)
            {
                // -------------------------------------------------
                // 2. NO EXISTE → LANZAR CLIENTE
                // -------------------------------------------------

                Debug.WriteLine(
                    "CDP no encontrado.");

                LanzarMu();


                // -------------------------------------------------
                // 3. ESPERAR CDP
                // -------------------------------------------------

                await EsperarCdpAsync();
            }


            // -----------------------------------------------------
            // 4. CONECTAR WEBSOCKET
            // -----------------------------------------------------

            await ConectarWebSocketAsync();


            Debug.WriteLine(
                "======================================");

            Debug.WriteLine(
                " MU CONECTADO CORRECTAMENTE");

            Debug.WriteLine(
                "======================================");
        }


        // =========================================================
        // COMPROBAR CDP
        // =========================================================

        public async Task<bool> ExisteCdpAsync()
        {
            string url =
                $"http://127.0.0.1:{CDP_PORT}/json";

            Debug.WriteLine(
                $"Comprobando CDP: {url}");


            try
            {
                HttpResponseMessage response =
                    await httpClient.GetAsync(url);


                Debug.WriteLine(
                    $"CDP HTTP: {(int)response.StatusCode}");


                if (!response.IsSuccessStatusCode)
                    return false;


                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }


        // =========================================================
        // LANZAR MU
        // =========================================================

        private void LanzarMu()
        {
            if (!File.Exists(MU_EXE))
            {
                throw new FileNotFoundException(
                    "No se encontró MU Magdalenas.",
                    MU_EXE);
            }


            Debug.WriteLine(
                "======================================");

            Debug.WriteLine(
                " LANZANDO MU MAGDALENAS");

            Debug.WriteLine(
                "======================================");


            ProcessStartInfo psi =
                new ProcessStartInfo
                {
                    FileName = MU_EXE,

                    Arguments =
                        $"--remote-debugging-port={CDP_PORT}",

                    WorkingDirectory =
                        MU_DIRECTORY,

                    UseShellExecute = true
                };


            Debug.WriteLine(
                $"EXE: {psi.FileName}");

            Debug.WriteLine(
                $"ARGS: {psi.Arguments}");

            Debug.WriteLine(
                $"DIR: {psi.WorkingDirectory}");


            procesoMu =
                Process.Start(psi);


            if (procesoMu == null)
            {
                throw new InvalidOperationException(
                    "No se pudo iniciar MU Magdalenas.");
            }


            Debug.WriteLine(
                $"PID MU: {procesoMu.Id}");
        }


        // =========================================================
        // ESPERAR CDP
        // =========================================================

        private async Task EsperarCdpAsync()
        {
            Debug.WriteLine(
                "Esperando CDP...");


            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(500);


                Debug.WriteLine(
                    $"CDP intento {i + 1}/60");


                if (await ExisteCdpAsync())
                {
                    Debug.WriteLine(
                        "CDP DISPONIBLE.");

                    return;
                }


                if (
                    procesoMu != null &&
                    procesoMu.HasExited)
                {
                    throw new InvalidOperationException(
                        $"MU Magdalenas se cerró. " +
                        $"ExitCode: {procesoMu.ExitCode}");
                }
            }


            throw new TimeoutException(
                "MU Magdalenas no abrió CDP " +
                $"en el puerto {CDP_PORT}.");
        }


        // =========================================================
        // OBTENER TARGETS
        // =========================================================

        private async Task<CdpTarget[]> ObtenerTargetsAsync()
        {
            string url =
                $"http://127.0.0.1:{CDP_PORT}/json";


            Debug.WriteLine(
                $"GET {url}");


            HttpResponseMessage response =
                await httpClient.GetAsync(url);


            response.EnsureSuccessStatusCode();


            string json =
                await response.Content.ReadAsStringAsync();


            Debug.WriteLine(
                "TARGETS:");

            Debug.WriteLine(json);


            CdpTarget[]? targets =
                JsonSerializer.Deserialize<CdpTarget[]>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });


            return targets ??
                   Array.Empty<CdpTarget>();
        }


        // =========================================================
        // CONECTAR WEBSOCKET
        // =========================================================

        private async Task ConectarWebSocketAsync()
        {
            CdpTarget[] targets =
                await ObtenerTargetsAsync();


            Debug.WriteLine(
                $"Targets encontrados: {targets.Length}");


            CdpTarget? targetSeleccionado = null;


            foreach (CdpTarget target in targets)
            {
                Debug.WriteLine(
                    $"TYPE: {target.Type}");

                Debug.WriteLine(
                    $"TITLE: {target.Title}");

                Debug.WriteLine(
                    $"URL: {target.Url}");

                Debug.WriteLine(
                    $"WS: {target.WebSocketDebuggerUrl}");

                Debug.WriteLine(
                    "--------------------------------");


                if (
                    target.Type == "page" &&
                    !string.IsNullOrWhiteSpace(
                        target.WebSocketDebuggerUrl))
                {
                    targetSeleccionado =
                        target;

                    break;
                }
            }


            if (targetSeleccionado == null)
            {
                throw new InvalidOperationException(
                    "No se encontró un target CDP.");
            }


            await DesconectarAsync();


            socket =
                new ClientWebSocket();


            Debug.WriteLine(
                "Conectando WebSocket...");


            await socket.ConnectAsync(
                new Uri(
                    targetSeleccionado.WebSocketDebuggerUrl!),
                CancellationToken.None);


            Debug.WriteLine(
                $"WebSocket: {socket.State}");
        }


        // =========================================================
        // ENVIAR CDP
        // =========================================================

        public async Task<JsonDocument> EnviarCdpAsync(
            string method,
            object? parameters = null)
        {
            if (socket == null)
            {
                throw new InvalidOperationException(
                    "No existe conexión CDP.");
            }


            if (socket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException(
                    $"WebSocket cerrado: {socket.State}");
            }


            contadorCdp++;


            var mensaje =
                new
                {
                    id = contadorCdp,

                    method = method,

                    @params = parameters
                };


            string json =
                JsonSerializer.Serialize(mensaje);


            Debug.WriteLine(
                "CDP SEND:");

            Debug.WriteLine(json);


            byte[] bytes =
                Encoding.UTF8.GetBytes(json);


            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);


            byte[] buffer =
                new byte[1024 * 1024];


            using MemoryStream memoria =
                new MemoryStream();


            while (true)
            {
                WebSocketReceiveResult resultado =
                    await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);


                memoria.Write(
                    buffer,
                    0,
                    resultado.Count);


                if (resultado.EndOfMessage)
                    break;
            }


            string respuesta =
                Encoding.UTF8.GetString(
                    memoria.ToArray());


            Debug.WriteLine(
                "CDP RECEIVE:");

            Debug.WriteLine(respuesta);


            return JsonDocument.Parse(
                respuesta);
        }


        // =========================================================
        // JAVASCRIPT
        // =========================================================

        public async Task<JsonDocument> EvaluateAsync(
            string javascript)
        {
            return await EnviarCdpAsync(
                "Runtime.evaluate",
                new
                {
                    expression = javascript,

                    returnByValue = true
                });
        }


        // =========================================================
        // DESCONECTAR
        // =========================================================

        public async Task DesconectarAsync()
        {
            if (socket == null)
                return;


            if (
                socket.State ==
                WebSocketState.Open)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Cierre normal",
                    CancellationToken.None);
            }


            socket.Dispose();

            socket = null;
        }
    }


    // =============================================================
    // CDP TARGET
    // =============================================================

    public class CdpTarget
    {
        public string? Description { get; set; }

        public string? DevtoolsFrontendUrl { get; set; }

        public string? FaviconUrl { get; set; }

        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Type { get; set; }

        public string? Url { get; set; }

        public string? WebSocketDebuggerUrl { get; set; }
    }
}
