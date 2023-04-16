using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DeconzToMqtt.Telemetry;
using DeconzToMqtt.Websocket;
using DeconzToMqtt.EventHandling;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;

namespace DeconzToMqtt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddConsole()
                           .SetMinimumLevel(LogLevel.Trace);

            builder.Services.AddHostedService<EventHandlingService>();
            builder.Services.AddHostedService<TelemetryService>();

            builder.Services.AddSingleton<MqttClient>();
            builder.Services.AddHostedService(x => x.GetService<MqttClient>());

            builder.Services.AddSingleton<WebsocketReceiver>();
            builder.Services.AddHostedService(x => x.GetService<WebsocketReceiver>());

            builder.Services.AddSingleton<SensorRepository>();
            builder.Services.AddSingleton<LightRepository>();

            builder.Services.AddOptions<DeconzToMqttOption>()
                            .BindConfiguration("DeconzToMqtt");

            //builder.Services.AddHealthChecks()

            var app = builder.Build();

            app.Run();
        }
    }
}
