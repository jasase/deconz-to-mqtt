using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;
using Framework.Abstraction.Extension;

namespace DeconzToMqtt.Telemetry
{
    public class TelemetryService
    {
        private readonly ILogger _logger;
        private readonly IMetricRecorder _metricRecorder;
        private readonly DeconzRepository[] _repositories;
        private readonly MqttClient _mqttClient;
        private CancellationTokenSource _cancelationToken;
        private Task _task;

        public TelemetryService(ILogger logger,
                                IMetricRecorder metricRecorder,
                                DeconzRepository[] repositories,
                                MqttClient mqttClient)
        {
            _logger = logger;
            _metricRecorder = metricRecorder;
            _repositories = repositories;
            _mqttClient = mqttClient;
        }

        public void Start()
        {
            _cancelationToken = new CancellationTokenSource();
            _task = Task.Factory.StartNew(Run);
        }

        public void Stop()
        {
            if (_cancelationToken != null)
            {
                _cancelationToken.Cancel();
            }

            _cancelationToken = null;
        }

        private void Run()
        {
            while (!_cancelationToken.IsCancellationRequested)
            {
                _logger.Info("Sending telemetry");
                _metricRecorder.CountEvent(DeconzToMqttIdentifier.SendInterval());
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
                                _mqttClient.SendMessage(msg);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "Sending of message for sensor {0} failed", item.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error in telemetry processing occurred");
                }

                Thread.Sleep(TimeSpan.FromSeconds(60));
            }

            _task = null;
        }
    }
}
