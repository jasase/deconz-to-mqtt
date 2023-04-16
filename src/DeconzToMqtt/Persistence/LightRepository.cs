using System;
using System.Collections.Generic;
using System.Text;
using DeconzToMqtt.Model;
using Microsoft.Extensions.Options;

namespace DeconzToMqtt.Persistence
{
    public class LightRepository : DeconzRepository<Light>
    {
        public LightRepository(IOptions<DeconzToMqttOption> options)
            : base(options.Value.DeconzApiKey, new Uri($"http://{options.Value.DeconzAddress}:{options.Value.DeconzApiPort}"))
        { }

        protected override string EndpointName => "lights";
    }
}
