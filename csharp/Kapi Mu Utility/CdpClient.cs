using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Kapi_Mu_Utility;

public sealed class CdpClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private int _messageId;

    private int _disposed;

    public bool IsConnected =>
        Volatile.Read(ref _disposed) == 0 &&
        _socket.State == WebSocketState.Open;

    public async Task ConnectAsync(
        string webSocketUrl,
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(
                nameof(CdpClient));
        }

        await _socket.ConnectAsync(
            new Uri(webSocketUrl),
            cancellationToken);

        if (_socket.State != WebSocketState.Open)
        {
            throw new WebSocketException(
                $"WebSocket no conectado. Estado: {_socket.State}");
        }
    }

    public async Task<JsonDocument> SendCommandAsync(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(
            cancellationToken);

        try
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(CdpClient));
            }

            if (_socket.State !=
                WebSocketState.Open)
            {
                throw new WebSocketException(
                    "El WebSocket CDP no está conectado.");
            }

            int id =
                Interlocked.Increment(
                    ref _messageId);

            var message =
                new Dictionary<string, object?>
                {
                    ["id"] = id,
                    ["method"] = method
                };

            if (parameters != null)
            {
                message["params"] = parameters;
            }

            string json =
                JsonSerializer.Serialize(
                    message);

            byte[] data =
                Encoding.UTF8.GetBytes(json);

            await _socket.SendAsync(
                data,
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            while (true)
            {
                string response =
                    await ReceiveMessageAsync(
                        cancellationToken);

                using JsonDocument document =
                    JsonDocument.Parse(response);

                if (!document.RootElement.TryGetProperty(
                        "id",
                        out JsonElement responseId))
                {
                    continue;
                }

                if (responseId.ValueKind !=
                    JsonValueKind.Number)
                {
                    continue;
                }

                if (responseId.GetInt32() != id)
                {
                    continue;
                }

                return JsonDocument.Parse(
                    response);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<bool> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            using JsonDocument response =
                await SendCommandAsync(
                    "Browser.getVersion",
                    null,
                    cancellationToken);

            return response.RootElement.TryGetProperty(
                "result",
                out _);
        }
        catch (
            OperationCanceledException)
        {
            throw;
        }
        catch (
            ObjectDisposedException)
        {
            return false;
        }
        catch (
            WebSocketException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> ReceiveMessageAsync(
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(
                nameof(CdpClient));
        }

        byte[] buffer =
            new byte[8192];

        using MemoryStream stream =
            new();

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
                    "Chrome cerró el WebSocket.");
            }

            if (result.MessageType !=
                WebSocketMessageType.Text)
            {
                continue;
            }

            stream.Write(
                buffer,
                0,
                result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(
            stream.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(
                ref _disposed,
                1) != 0)
        {
            return;
        }

        try
        {
            await _sendLock.WaitAsync();
        }
        catch
        {
            return;
        }

        try
        {
            if (_socket.State ==
                WebSocketState.Open)
            {
                try
                {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Cierre",
                        CancellationToken.None);
                }
                catch
                {
                }
            }
        }
        finally
        {
            _socket.Dispose();

            _sendLock.Release();

            _sendLock.Dispose();
        }
    }
}
