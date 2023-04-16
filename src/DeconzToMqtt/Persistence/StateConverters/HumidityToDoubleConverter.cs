using DeconzToMqtt.Model;

namespace DeconzToMqtt.Persistence.StateConverters
{
    public class HumidityToDoubleConverter : ISensorStateConverter
    {
        public object Convert(object data)
        {
            var doubleData = System.Convert.ToDouble(data);
            return doubleData / 100;
        }

        public bool Match(Sensor sensor, string stateName, object originalValue)
            => sensor.ManufacturerName == "LUMI" &&
                sensor.ModelId == "lumi.weather" &&
                sensor.Type == "ZHAHumidity" &&
                stateName == "humidity" &&
                (originalValue is long || originalValue is int);
    }
}
