using DeconzToMqtt.Model;
using Framework.Abstraction.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeconzToMqtt.Websocket
{
    public class WebsocketReceiver
    {
        private readonly ILogger _logger;
        private readonly Uri _webSocketUri;
        private readonly HashSet<IWebSocketMessageSubscriber> _subscriber;
        private CancellationTokenSource _cancelToken;
        private ClientWebSocket _socket;

        public WebsocketReceiver(ILogger logger, Uri webSocketUri)
        {
            _logger = logger;
            _webSocketUri = webSocketUri;
            _subscriber = new HashSet<IWebSocketMessageSubscriber>();

        }

        public void Start()
        {
            _logger.Debug("Start WebSocket to {0}", _webSocketUri);
            _cancelToken = new CancellationTokenSource();
            _socket = new ClientWebSocket();
            _socket.ConnectAsync(_webSocketUri, _cancelToken.Token).Wait();

            Task.Factory.StartNew(Run, _cancelToken.Token);
        }

        public void Stop()
        {
            _logger.Debug("Stop WebSocket");
            _cancelToken.Cancel();
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

        private void Run()
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                try
                {
                    if (_socket.State != WebSocketState.Open)
                    {
                        _logger.Warn("Websocket stream closed. Try reconnect");
                        TryReconnectSocket();
                    }
                    var msg = ReceiveFullMessage(_socket, _cancelToken.Token);

                    _logger.Debug("Received web socket message of length '{0}", msg.Item1.Count);
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
                    _logger.Error(ex, "Processing web socket message failed");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private void TryReconnectSocket()
        {
            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            _socket = new ClientWebSocket();
            _socket.ConnectAsync(_webSocketUri, _cancelToken.Token).Wait();
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
    }
}
