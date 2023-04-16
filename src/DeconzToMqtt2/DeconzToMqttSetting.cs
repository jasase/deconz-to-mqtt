
using System;

namespace DeconzToMqtt
{
    public class DeconzToMqttOption
    {
        public string DeconzAddress { get; set; }
        public int DeconzWebsocketPort { get; set; }
        public int DeconzApiPort { get; set; }
        public string DeconzApiKey { get; set; }


        public string MqttAddress { get; set; }
        public string MqttUsername { get; set; }
        public string MqttPassword { get; set; }


        public DeconzToMqttOption()
        {
            DeconzAddress = "localhost";
            DeconzWebsocketPort = 443;
            DeconzApiPort = 80;
            DeconzApiKey = "";

            MqttAddress = "";
            MqttUsername = "";
            MqttPassword = "";
        }
    }
}
