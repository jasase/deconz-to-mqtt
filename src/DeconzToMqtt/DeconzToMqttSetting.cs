using Framework.Abstraction.Extension;

namespace DeconzToMqtt
{
    public class DeconzToMqttSetting : ISetting
    {
        public string DeconzAddress { get; set; }
        public int DeconzWebsocketPort { get; set; }
        public int DeconzApiPort { get; set; }
        public string DeconzApiKey { get; set; }


        public string MqttAddress { get; set; }
        public string MqttUsername { get; set; }
        public string MqttPassword { get; set; }


        public DeconzToMqttSetting()
        {
            DeconzAddress = "";
            DeconzWebsocketPort = 443;
            DeconzApiPort = 80;
            DeconzApiKey = "";

            MqttAddress = "";
            MqttUsername = "";
            MqttPassword = "";
        }
    }
}
