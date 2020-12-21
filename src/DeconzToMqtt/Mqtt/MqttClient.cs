using DeconzToMqtt.Model;
using Framework.Abstraction.Extension;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using NLog.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeconzToMqtt.Mqtt
{
    public class MqttClient
    {
        private readonly object _sendLock;
        private readonly ILogger _logger;
        private readonly IMetricRecorder _metricRecorder;
        private readonly ILogManager _logProvider;
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private CancellationTokenSource _cancelationToken;
        private IMqttClient _client;

        public MqttClient(ILogger logger, IMetricRecorder metricRecorder, ILogManager logProvider, string hostname, string username, string password)
        {
            _sendLock = new object();
            _logger = logger;
            _metricRecorder = metricRecorder;
            _logProvider = logProvider;
            _hostname = hostname;
            _username = username;
            _password = password;
        }

        public void Start()
        {
            if (_cancelationToken != null)
            {
                _cancelationToken.Cancel();
            }


            _cancelationToken = new CancellationTokenSource();

            Connect();
        }

        public void Stop()
            => _client.DisconnectAsync().Wait();

        public void SendMessage(MqttMessage message)
        {
            _metricRecorder.CountEvent(DeconzToMqttIdentifier.SendMessage());
            lock (_sendLock)
            {
                if (!_client.IsConnected)
                {
                    _logger.Warn("MQTT Client not connected. Try reconnect");
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _client = null;

                    Connect();
                }

                var visitor = new MqttMessageBuilderVisitor();
                var messageConverted = message.Accept(visitor);

                _metricRecorder.Measure(DeconzToMqttIdentifier.MessageSize(), messageConverted.Payload.Length);
                _client.PublishAsync(messageConverted, _cancelationToken.Token).Wait();
            }
        }

        private void Connect()
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
               //.WithClientId("DeconzToMqtt")
               .WithClientId("DeconzToMqtt2")
               .WithTcpServer(_hostname)
               .WithWillMessage(new MqttApplicationMessageBuilder()
                                    .WithRetainFlag(true)
                                    .WithTopic("tele/deconztomqtt/LWT")
                                    .WithPayload("offline")
                                    .Build());

            if (!string.IsNullOrWhiteSpace(_username))
            {
                optionsBuilder.WithCredentials(_username, _password);
            }

            var options = optionsBuilder.Build();
            var factory = new MqttFactory(new MqttNetLogger(_logProvider));
            _client = factory.CreateMqttClient();



            _client.UseDisconnectedHandler(async e =>
            {
                _logger.Warn("Disconnected from MQTT server. Try reconnect...");
                await Task.Delay(TimeSpan.FromSeconds(15));

                try
                {
                    await _client.ConnectAsync(options);
                }
                catch
                {
                    _logger.Error("Reconnect failed. Resetting client");
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _client = null;
                }
            });
            _client.UseConnectedHandler(async e =>
            {
                _logger.Info("Connected to MQTT server");

                await _client.PublishAsync(new MqttApplicationMessageBuilder()
                                    .WithRetainFlag(true)
                                    .WithTopic("tele/deconztomqtt/LWT")
                                    .WithPayload("online")
                                    .Build());
            });

            _logger.Info("Connecting to MQTT server '{0}'", _hostname);
            _client.ConnectAsync(options, _cancelationToken.Token).Wait();
        }

        class MqttMessageBuilderVisitor : IMqttMessageVisitor<MqttApplicationMessage>
        {
            private readonly MqttApplicationMessageBuilder _builder;

            public MqttMessageBuilderVisitor()
            {
                _builder = new MqttApplicationMessageBuilder()
                    .WithAtLeastOnceQoS();
            }

            public MqttApplicationMessage Handle(DeconzItemStateChangedMqttMessage message)
                => _builder.WithTopic($"deconz/stat/{CreateEntityTypeTopic(message.Entity)}/{message.Entity.Name}/{message.PropertyName}")
                           .WithPayload(message.Content)
                           .Build();

            public MqttApplicationMessage Handle(DeconzItemTelemetryMqttMessage message)
                => _builder.WithTopic($"deconz/tele/{CreateEntityTypeTopic(message.Entity)}/{message.Entity.Name}/state")
                           .WithPayload(message.Content)
                           .Build();

            private string CreateEntityTypeTopic(DeconzItem deconzItem)
            {
                if (deconzItem is Sensor)
                {
                    return "sensor";
                }
                if (deconzItem is Light)
                {
                    return "light";
                }

                throw new NotImplementedException("Unknown DeconzItem type. Please implement");
            }
        }
    }
}
