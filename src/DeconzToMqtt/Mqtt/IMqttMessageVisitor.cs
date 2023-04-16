namespace DeconzToMqtt.Mqtt
{
    public interface IMqttMessageVisitor<TReturn>
    {
        TReturn Handle(DeconzItemStateChangedMqttMessage deconzItemStateChangedMqttMessage);
        TReturn Handle(DeconzItemTelemetryMqttMessage deconzItemTelemetryMqttMessage);
    }
}
