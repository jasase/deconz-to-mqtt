using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeconzToMqtt.Model
{
    public class Sensor : DeconzItem
    {
        public SensorConfig Config { get; set; }
        public SensorState State { get; set; }

        public string Type { get; set; }
    }

    public class SensorConfig
    {
        public bool On { get; set; }
        public bool Reachable { get; set; }
        public int? Battery { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Data { get; set; }
    }

    public class SensorState
    {
        public DateTime? LastUpdated { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Data { get; set; }
    }
}
