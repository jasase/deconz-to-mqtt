using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeconzToMqtt.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DeconzToMqtt.Websocket
{
    public class WebsocketReceiver : BackgroundService, IHealthCheck
    {
        private readonly ILogger _logger;
        private readonly Uri _webSocketUri;
        private readonly HashSet<IWebSocketMessageSubscriber> _subscriber;
        private ClientWebSocket _socket;

        public WebsocketReceiver(ILogger<WebsocketReceiver> logger, IOptions<DeconzToMqttOption> options)
        {
            _logger = logger;
            _webSocketUri = new Uri($"ws://{options.Value.DeconzAddress}:{options.Value.DeconzWebsocketPort}");
            _subscriber = new HashSet<IWebSocketMessageSubscriber>();

        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Start WebSocket to {0}", _webSocketUri);
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_webSocketUri, cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            _logger.LogDebug("Stop WebSocket");
            _socket.Abort();
            _socket.Dispose();
        }

        public void Subscribe(IWebSocketMessageSubscriber subscriber)
        {
            lock (_subscriber)
            {
                _subscriber.Add(subscriber);
            }
        }

        public void Unsubscribe(IWebSocketMessageSubscriber subscriber)
        {
            lock (_subscriber)
            {
                _subscriber.Remove(subscriber);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_socket.State != WebSocketState.Open)
                    {
                        _logger.LogWarning("Websocket stream closed. Try reconnect");
                        TryReconnectSocket(stoppingToken);
                    }
                    var msg = ReceiveFullMessage(_socket, stoppingToken);

                    _logger.LogDebug("Received web socket message of length '{0}", msg.Item1.Count);
                    var stringData = Encoding.Default.GetString(msg.Item2);
                    var msgData = JsonConvert.DeserializeObject<WebsocketEvent>(stringData);

                    lock (_subscriber)
                    {
                        foreach (var subscriber in _subscriber)
                        {
                            subscriber.Handle(msgData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processing web socket message failed");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }

            return Task.CompletedTask;
        }

        private async Task TryReconnectSocket(CancellationToken stoppingToken)
        {
            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_webSocketUri, stoppingToken);
        }

        private (WebSocketReceiveResult, byte[]) ReceiveFullMessage(WebSocket socket, CancellationToken cancelToken)
        {
            WebSocketReceiveResult response;
            var message = new List<byte>();

            var buffer = new byte[4096];
            do
            {
                response = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken).Result;
                message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
            } while (!response.EndOfMessage);

            return (response, message.ToArray());
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(_socket != null && _socket.State == WebSocketState.Open
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy());


    }
}
