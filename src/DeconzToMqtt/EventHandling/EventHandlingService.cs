using DeconzToMqtt.Model;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;
using DeconzToMqtt.Websocket;
using Framework.Abstraction.Extension;

namespace DeconzToMqtt.EventHandling
{
    public class EventHandlingService : IWebSocketMessageSubscriber
    {
        private readonly ILogger _logger;
        private readonly WebsocketReceiver _websocketReceiver;
        private readonly MqttClient _mqttClient;
        private readonly SensorRepository _sensorRepository;

        public EventHandlingService(ILogger logger,
                                    WebsocketReceiver websocketReceiver,
                                    MqttClient mqttClient,
                                    SensorRepository sensorRepository)
        {
            _logger = logger;
            _websocketReceiver = websocketReceiver;
            _mqttClient = mqttClient;
            _sensorRepository = sensorRepository;
        }

        public void Handle(WebsocketEvent message)
        {
            if (message.ResourceType.ToLowerInvariant() == "sensors")
            {
                HandlingSensorResource(message);
            }
        }

        public void Start()
            => _websocketReceiver.Subscribe(this);

        public void Stop()
            => _websocketReceiver.Unsubscribe(this);

        private void HandlingSensorResource(WebsocketEvent message)
        {
            _logger.Debug("Received websocket message of type {0}", message.EventType);
            if (message.EventType.ToLowerInvariant() == "changed")
            {
                if (message.State == null)
                {
                    _logger.Warn("Received empty message state in websocket msg. Dismiss message");
                    return;
                }

                var entity = _sensorRepository.GetById(message.Id);
                _sensorRepository.RefreshState(entity, message.State);

                if (entity != null)
                {
                    var name = entity.Name;
                    foreach (var state in message.State)
                    {
                        var key = state.Key.ToLowerInvariant();
                        if (key == "lastupdated") continue;


                        var mqttMessage = new DeconzItemStateChangedMqttMessage
                        {
                            Entity = entity,
                            PropertyName = key,
                            Content = entity.State.Data[state.Key].ToString().ToLowerInvariant()

                        };
                        _logger.Debug("Publishing change for entity '{0}' on state '{1}' to value '{2}'",
                            mqttMessage.Entity.Name,
                            mqttMessage.PropertyName,
                            mqttMessage.Content);
                        _mqttClient.SendMessage(mqttMessage);
                    }
                }
                else
                {
                    _logger.Warn("Got websocket message with invalid entity id {0}", message.Id);
                }

                _sensorRepository.NotifyChange(message.Id);
            }
        }
    }
}
