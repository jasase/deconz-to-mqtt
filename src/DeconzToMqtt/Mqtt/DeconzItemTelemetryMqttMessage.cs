using DeconzToMqtt.Model;

namespace DeconzToMqtt.Mqtt
{
    public class DeconzItemTelemetryMqttMessage : MqttMessage
    {
        public DeconzItem Entity { get; set; }

        public override TReturn Accept<TReturn>(IMqttMessageVisitor<TReturn> visitor)
            => visitor.Handle(this);
    }
}
