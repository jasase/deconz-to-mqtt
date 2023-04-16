namespace DeconzToMqtt.Mqtt
{
    public abstract class MqttMessage
    {
        public string Content { get; set; }

        public abstract TReturn Accept<TReturn>(IMqttMessageVisitor<TReturn> visitor);
    }
}
