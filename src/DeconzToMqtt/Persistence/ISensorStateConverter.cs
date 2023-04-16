using DeconzToMqtt.Model;

namespace DeconzToMqtt.Persistence
{
    public interface ISensorStateConverter
    {
        bool Match(Sensor sensor, string stateName, object originalValue);

        object Convert(object data);
    }
}
