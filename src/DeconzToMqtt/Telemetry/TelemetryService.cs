using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeconzToMqtt.Telemetry
{
    public class TelemetryService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly DeconzRepository[] _repositories;
        private readonly MqttClient _mqttClient;

        public TelemetryService(ILogger<TelemetryService> logger,
                                SensorRepository sensorRepository,
                                LightRepository lightRepository,
                                MqttClient mqttClient)
        {
            _logger = logger;
            _repositories = new DeconzRepository[] { sensorRepository, lightRepository };
            _mqttClient = mqttClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Sending telemetry");
                try
                {
                    foreach (var repository in _repositories)
                    {
                        foreach (var item in repository.GetAllDeconzItems())
                        {
                            try
                            {
                                var msg = new DeconzItemTelemetryMqttMessage
                                {
                                    Content = repository.Serialize(item),
                                    Entity = item
                                };
                                await _mqttClient.SendMessage(msg);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Sending of message for sensor {0} failed", item.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in telemetry processing occurred");
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
