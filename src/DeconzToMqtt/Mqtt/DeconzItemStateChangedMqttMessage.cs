using DeconzToMqtt.Model;

namespace DeconzToMqtt.Mqtt
{
    public class DeconzItemStateChangedMqttMessage : MqttMessage
    {
        public DeconzItem Entity { get; set; }
        public string PropertyName { get; set; }

        public override TReturn Accept<TReturn>(IMqttMessageVisitor<TReturn> visitor)
            => visitor.Handle(this);
    }
}
