using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DeconzToMqtt.Telemetry;
using DeconzToMqtt.Websocket;
using DeconzToMqtt.EventHandling;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace DeconzToMqtt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);            

            builder.Logging.SetMinimumLevel(LogLevel.Trace).AddConsole();

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

            builder.Services.AddHealthChecks()
                            .AddCheck<DepedencyInjectionHealthCheck<WebsocketReceiver>>("WebsocketReceiver")
                            .AddCheck<DepedencyInjectionHealthCheck<MqttClient>>("MqttClient");

            var app = builder.Build();

            app.MapHealthChecks("/health");
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = WriteResponse
            });

            app.Run();
        }

        private static Task WriteResponse(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteStartObject("results");

                foreach (var healthReportEntry in healthReport.Entries)
                {
                    jsonWriter.WriteStartObject(healthReportEntry.Key);
                    jsonWriter.WriteString("status",
                        healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("description",
                        healthReportEntry.Value.Description);
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in healthReportEntry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);

                        JsonSerializer.Serialize(jsonWriter, item.Value,
                            item.Value?.GetType() ?? typeof(object));
                    }

                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }
}
