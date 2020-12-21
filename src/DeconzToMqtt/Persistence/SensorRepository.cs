using DeconzToMqtt.Model;
using DeconzToMqtt.Persistence.StateConverters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeconzToMqtt.Persistence
{
    public class SensorRepository : DeconzRepository<Sensor>
    {
        private readonly ISensorStateConverter[] _converters;

        protected override string EndpointName => "sensors";

        public SensorRepository(string apiKey, Uri address)
            : base(apiKey, address)
        {
            _converters = new ISensorStateConverter[]
            {
                new TemperatureToDoubleConverter(),
                new HumidityToDoubleConverter()
            };
        }

        protected override void CreateHock(Sensor item)
        {
            base.CreateHock(item);
            ConvertAllStates(item);
        }

        public void RefreshState(Sensor sensor, Dictionary<string, object> newState)
        {
            foreach (var state in newState)
            {
                sensor.State.Data[state.Key] = ConvertState(sensor, state.Key, state.Value);
            }
        }

        private void ConvertAllStates(Sensor sensor)
        {
            foreach (var state in sensor.State.Data.ToArray())
            {
                sensor.State.Data[state.Key] = ConvertState(sensor, state.Key, state.Value);
            }
        }

        private object ConvertState(Sensor sensor, string stateName, object originalValue)
        {
            var convertedValue = originalValue;
            foreach (var converter in _converters)
            {
                if (converter.Match(sensor, stateName, convertedValue))
                {
                    convertedValue = converter.Convert(convertedValue);
                }
            }

            return convertedValue;
        }
    }
}
