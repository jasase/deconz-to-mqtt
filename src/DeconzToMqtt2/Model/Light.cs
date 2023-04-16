using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeconzToMqtt.Model
{
    public class Light : DeconzItem
    {
        public string Type { get; set; }

        public LightState State { get; set; }
    }

    public class LightState
    {
        [JsonExtensionData]
        public Dictionary<string, object> Data { get; set; }
    }
}
