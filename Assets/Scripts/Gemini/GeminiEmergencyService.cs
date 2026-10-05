using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EmergencyDispatch
{
    public class GeminiEmergencyService : MonoBehaviour
    {
        public event Action<RelayMessage> Message;
        public bool Ready { get; private set; }
        private ClientWebSocket socket;
        private CancellationTokenSource lifetime;
        private readonly ConcurrentQueue<RelayMessage> incoming = new ConcurrentQueue<RelayMessage>();
        private readonly ConcurrentQueue<string> outgoing = new ConcurrentQueue<string>();
        private int generation;
        private string caseId;
        private void Awake() { GeminiConfig.RemoveLegacyCredential(); }

        public async void Connect(string id)
        {
            Disconnect();
            int epoch = generation;
            caseId = id;
            var source = lifetime = new CancellationTokenSource();
            var ws = socket = new ClientWebSocket();
            try
            {
                if (!Uri.TryCreate(GeminiConfig.BackendUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "wss" && !(uri.Scheme == "ws" && uri.IsLoopback)))
                    throw new InvalidOperationException("Usá ws://127.0.0.1:8787 o un relay wss://.");
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(source.Token))
                {
                    timeout.CancelAfter(8000);
                    await ws.ConnectAsync(uri, timeout.Token);
                }
                if (epoch != generation) return;
                outgoing.Enqueue(JsonUtility.ToJson(new RelayMessage { type = "start", caseId = id }));
                var sender = SendLoop(ws, source.Token);
                try { await ReceiveLoop(ws, source.Token, epoch); }
                finally { source.Cancel(); await sender; }
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException || ex is InvalidOperationException || ex is IOException || ex is ArgumentException)
            {
                if (epoch == generation) incoming.Enqueue(new RelayMessage { type = "error", caseId = id, text = "No se pudo mantener la conexión de voz. Comprobá el backend y su configuración." });
            }
            finally { ws.Dispose(); }
        }
        public void Send(RelayMessage message)
        {
            if (socket?.State != WebSocketState.Open) return;
            message.caseId = caseId;
            if (outgoing.Count < 256) outgoing.Enqueue(JsonUtility.ToJson(message));
            else incoming.Enqueue(new RelayMessage { type = "error", caseId = caseId, text = "La conexión no puede enviar audio a tiempo. Volvé a conectar." });
        }
        private async Task SendLoop(ClientWebSocket ws, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    if (outgoing.TryDequeue(out string json))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
                    }
                    else await Task.Delay(8, ct);
                }
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException || ex is ObjectDisposedException) { }
        }
        private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken ct, int epoch)
        {
            byte[] buffer = new byte[32768];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using (var stream = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (result.MessageType == WebSocketMessageType.Close) throw new IOException("Relay closed.");
                        stream.Write(buffer, 0, result.Count);
                        if (stream.Length > 1024 * 1024) throw new IOException("Relay message too large.");
                    } while (!result.EndOfMessage);
                    // JsonUtility works on plain serializable data on a worker; Unity objects remain on Update.
                    var message = JsonUtility.FromJson<RelayMessage>(Encoding.UTF8.GetString(stream.ToArray()));
                    if (epoch == generation && message != null && message.caseId == caseId) incoming.Enqueue(message);
                }
            }
        }
        private void Update()
        {
            for (int i = 0; i < 128 && incoming.TryDequeue(out var message); i++)
            {
                if (message.type == "ready") Ready = true;
                if (message.type == "error") Ready = false;
                Message?.Invoke(message);
            }
        }
        public void Disconnect()
        {
            generation++;
            Ready = false;
            lifetime?.Cancel();
            socket?.Abort();
            lifetime?.Dispose();
            lifetime = null; socket = null;
            while (incoming.TryDequeue(out _)) { }
            while (outgoing.TryDequeue(out _)) { }
        }
        private void OnDestroy() { Disconnect(); }
    }
}
