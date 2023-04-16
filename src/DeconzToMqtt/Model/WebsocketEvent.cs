using Newtonsoft.Json;
using System.Collections.Generic;

namespace DeconzToMqtt.Model
{
    public class WebsocketEvent
    {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("r")]
        public string ResourceType { get; set; }

        public string Name { get; set; }

        public int Id { get; set; }

        public string UniqueId { get; set; }

        public Dictionary<string, object> State { get; set; }

        public Sensor Sensor { get; set; }
    }
}


