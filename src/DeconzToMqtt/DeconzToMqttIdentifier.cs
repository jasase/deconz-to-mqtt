using System;
using System.Collections.Generic;
using System.Text;
using Framework.Abstraction.Extension;

namespace DeconzToMqtt
{
    public class DeconzToMqttIdentifier : MetricIdentifier
    {


        private DeconzToMqttIdentifier(string identifier)
            : base("DeconzToMqtt", identifier)
        { }

        public static DeconzToMqttIdentifier SendInterval()
            => new DeconzToMqttIdentifier("Interval");

        public static MetricIdentifier SendMessage()
            => new DeconzToMqttIdentifier("MqttMessage");

        public static MetricIdentifier MessageSize()
            => new DeconzToMqttIdentifier("MessageSize");
    }
}
