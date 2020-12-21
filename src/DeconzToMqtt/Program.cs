using Extension.TelemetryApplicationInsights;
using Framework.Abstraction.Plugins;
using ServiceHost.Docker;

namespace DeconzToMqtt
{
    class Program : Startup
    {
        static void Main(string[] args)
            => new Program().Run(args, BootstrapInCodeConfiguration.Default());
                                                                   //.ConfigureTelemetry(x => x.InstrumentationKey = "30e4f76d-e462-4f0a-9f0a-9fbf4939558d"));
    }
}
