using System;
using System.ComponentModel.Design;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DeconzToMqtt.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace DeconzToMqtt.Mqtt
{
    public class MqttClient : IHostedService, IHealthCheck
    {
        private readonly ILogger _logger;

        private readonly ILoggerFactory _loggerFactory;
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private IManagedMqttClient _client;

        private SemaphoreSlim _semaphore;

        public MqttClient(ILogger<MqttClient> logger, ILoggerFactory logProvider, IOptions<DeconzToMqttOption> options)
        {
            _logger = logger;
            _loggerFactory = logProvider;
            _hostname = options.Value.MqttAddress;
            _username = options.Value.MqttUsername;
            _password = options.Value.MqttPassword;
            _semaphore = new SemaphoreSlim(1);
        }

        public bool IsConnected => _client != null &&
                                   _client.IsStarted &&
                                   _client.IsConnected;

        public async Task StartAsync(CancellationToken cancellationToken)
            => await Connect();

        public async Task StopAsync(CancellationToken cancellationToken)
            => await _client.StopAsync();

        public async Task SendMessage(MqttMessage message)
        {
            if (!IsConnected)
            {
                await Connect();
            }

            var visitor = new MqttMessageBuilderVisitor();
            var messageConverted = message.Accept(visitor);

            await _client.PublishAsync(messageConverted);
        }

        private async Task Connect()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (IsConnected)
                {
                    return;
                }
                if (_client != null && _client.IsStarted)
                {
                    return;
                }

                if (_client != null)
                {
                    _client.Dispose();
                }
                _client = null;

                _logger.LogInformation("MQTT Client not connected. Do connect");

                var clientOptions = new MqttClientOptionsBuilder()
                  .WithClientId("DeconzToMqtt")
                  .WithTcpServer(_hostname)
                  .WithWillMessage(new MqttApplicationMessageBuilder()
                                       .WithRetainFlag(true)
                                       .WithTopic("tele/deconztomqtt/LWT")
                                       .WithPayload("offline")
                                       .Build());

                if (!string.IsNullOrWhiteSpace(_username))
                {
                    clientOptions.WithCredentials(_username, _password);
                }


                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(clientOptions);

                var factory = new MqttFactory(new MqttNetLogger(_loggerFactory));
                _client = factory.CreateManagedMqttClient();

                _client.UseDisconnectedHandler(async e =>
                {
                    _logger.LogWarning("Disconnected from MQTT server. Try reconnect...");
                });
                _client.UseConnectedHandler(async e =>
                {
                    _logger.LogInformation("Connected to MQTT server");

                    await _client.PublishAsync(new MqttApplicationMessageBuilder()
                                        .WithRetainFlag(true)
                                        .WithTopic("tele/deconztomqtt/LWT")
                                        .WithPayload("online")
                                        .Build());
                });

                _logger.LogInformation("Connecting to MQTT server '{0}'", _hostname);
                await _client.StartAsync(managedOptions.Build());
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_client != null &&
                           _client.IsStarted &&
                           _client.IsConnected
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy());


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
