using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace RevitMCP.Plugin
{
    public class SocketServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        public void Start(int port)
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClient(client, token);
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested) break;
                }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                    Logger.Log($"Received: {line}");
                        var request = JsonConvert.DeserializeObject<McpRequest>(line);
                        
                        // Queue task for Revit API context
                        App.ModelingHandler.RequestQueue.Enqueue(request);
                        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                        App.ModelingHandler.ResponseMap[request.Id] = tcs;
                        
                        // Signal External Event
                        Logger.Log("Raising External Event");
                        App.ModelingEvent.Raise();

                        // Wait for result
                        Logger.Log($"Waiting for result {request.Id}...");
                        var result = await tcs.Task;
                        Logger.Log($"Got result for {request.Id}");
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(@"D:\MCP\REVIT\socket_error.log", DateTime.Now + ": " + ex.ToString() + Environment.NewLine); } catch {}
                }
            }
        }
    }

    public class McpRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Command { get; set; }
        public dynamic Args { get; set; }
    }
}
