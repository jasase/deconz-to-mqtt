using System;
using System.Collections.Generic;
using System.Text;
using DeconzToMqtt.Model;

namespace DeconzToMqtt.Persistence
{
    public class LightRepository : DeconzRepository<Light>
    {
        public LightRepository(string apiKey, Uri baseAddress)
            : base(apiKey, baseAddress)
        { }

        protected override string EndpointName => "lights";
    }
}
