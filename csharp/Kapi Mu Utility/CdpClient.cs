using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Kapi_Mu_Utility;

public sealed class CdpClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();
    private int _messageId;

    public bool IsConnected =>
        _socket.State == WebSocketState.Open;

    public async Task ConnectAsync(
        string webSocketUrl,
        CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(
            new Uri(webSocketUrl),
            cancellationToken);
    }

    public async Task<JsonDocument> SendCommandAsync(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (_socket.State != WebSocketState.Open)
            throw new InvalidOperationException(
                "CDP WebSocket no está conectado.");

        int id = Interlocked.Increment(ref _messageId);

        var message = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["method"] = method
        };

        if (parameters != null)
            message["params"] = parameters;

        string json = JsonSerializer.Serialize(message);

        byte[] data = Encoding.UTF8.GetBytes(json);

        await _socket.SendAsync(
            data,
            WebSocketMessageType.Text,
            true,
            cancellationToken);

        while (true)
        {
            string response =
                await ReceiveMessageAsync(cancellationToken);

            using JsonDocument document =
                JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty(
                    "id",
                    out JsonElement responseId))
            {
                // Evento CDP sin ID.
                // Lo ignoramos.
                continue;
            }

            if (responseId.GetInt32() != id)
                continue;

            return JsonDocument.Parse(response);
        }
    }

    private async Task<string> ReceiveMessageAsync(
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];

        using MemoryStream stream = new();

        while (true)
        {
            WebSocketReceiveResult result =
                await _socket.ReceiveAsync(
                    buffer,
                    cancellationToken);

            if (result.MessageType ==
                WebSocketMessageType.Close)
            {
                throw new WebSocketException(
                    "Chrome cerró la conexión CDP.");
            }

            stream.Write(
                buffer,
                0,
                result.Count);

            if (result.EndOfMessage)
                break;
        }

        return Encoding.UTF8.GetString(
            stream.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Cierre normal",
                    CancellationToken.None);
            }
        }
        catch
        {
        }

        _socket.Dispose();
    }
}
