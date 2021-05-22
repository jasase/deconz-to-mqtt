using System;
using DeconzToMqtt.EventHandling;
using DeconzToMqtt.Health;
using DeconzToMqtt.Mqtt;
using DeconzToMqtt.Persistence;
using DeconzToMqtt.Telemetry;
using DeconzToMqtt.Websocket;
using Framework.Abstraction.Extension;
using Framework.Abstraction.IocContainer;
using Framework.Abstraction.Plugins;
using Framework.Abstraction.Services;
using Framework.Abstraction.Services.Scheduling;
using ServiceHost.Contracts;

namespace DeconzToMqtt
{
    public class DeconzToMqttPlugin : Framework.Abstraction.Plugins.Plugin, IServicePlugin
    {
        public override PluginDescription Description { get; }

        public DeconzToMqttPlugin(IDependencyResolver resolver,
                                  IDependencyResolverConfigurator configurator,
                                  IEventService eventService,
                                  ILogger logger)
            : base(resolver, configurator, eventService, logger)
        {
            Description = new AutostartServicePluginDescription()
            {
                Description = "Plugin um Deconz Events per MQTT zu publishen",
                Name = "DeconzToMqtt",
                NeededServices = new[] { typeof(IConfiguration), typeof(ISchedulingService) }
            };
        }

        protected override void ActivateInternal()
        {
            var setting = Resolver.GetInstance<DeconzToMqttSetting>();
            var logManager = Resolver.GetInstance<ILogManager>();
            var metricRecorder = Resolver.GetInstance<IMetricRecorder>();

            var sensorRepository = new SensorRepository(setting.DeconzApiKey, new Uri($"ws://{setting.DeconzAddress}:{setting.DeconzApiPort}"));
            var lightRepository = new LightRepository(setting.DeconzApiKey, new Uri($"ws://{setting.DeconzAddress}:{setting.DeconzApiPort}"));

            var healthCheckService = new HealthCheckService(logManager.GetLogger<HealthCheckService>());
            var websockerReceiver = new WebsocketReceiver(logManager.GetLogger<WebsocketReceiver>(), new Uri($"ws://{setting.DeconzAddress}:{setting.DeconzWebsocketPort}"));
            var mqttClient = new MqttClient(logManager.GetLogger<MqttClient>(), metricRecorder, logManager, setting.MqttAddress, setting.MqttUsername, setting.MqttPassword);
            var eventHandler = new EventHandlingService(logManager.GetLogger<EventHandlingService>(), websockerReceiver, mqttClient, sensorRepository);
            var telemetryService = new TelemetryService(logManager.GetLogger<TelemetryService>(), metricRecorder, new DeconzRepository[] {
                sensorRepository,
                lightRepository
                }, mqttClient);

            healthCheckService.AddHealthCheck(websockerReceiver);
            healthCheckService.AddHealthCheck(mqttClient);

            mqttClient.Start();
            eventHandler.Start();
            websockerReceiver.Start();
            telemetryService.Start();
            healthCheckService.Start();
        }
    }
}
