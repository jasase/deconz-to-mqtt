using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace DeconzToMqtt.Mqtt
{
    public class MqttNetLogger : IMqttNetLogger
    {
        private readonly ILoggerFactory _loggerProvider;
        private readonly ILogger _mainLogger;

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public MqttNetLogger(ILoggerFactory loggerProvider)
        {
            _loggerProvider = loggerProvider;
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
            => CreateScopedLogger(source).Publish(logLevel, message, parameters, exception);

        private LogLevel ToMsLogging(MqttNetLogLevel level)
        {
            switch (level)
            {
                case MqttNetLogLevel.Verbose:
                    return LogLevel.Trace;
                case MqttNetLogLevel.Info:
                    return LogLevel.Information;
                case MqttNetLogLevel.Warning:
                    return LogLevel.Warning;
                case MqttNetLogLevel.Error:
                    return LogLevel.Error;
                default:
                    return LogLevel.None;
            }
        }

        public IMqttNetScopedLogger CreateScopedLogger(string source)
            => new MqttNetChildLogger(source, _loggerProvider);

        public class MqttNetChildLogger : IMqttNetScopedLogger
        {
            private readonly string _source;
            private readonly ILogger _logger;
            private readonly ILoggerFactory _loggerProvider;

            public MqttNetChildLogger(string source, ILoggerFactory loggerProvider)
            {
                _source = source ?? string.Empty;
                _logger = loggerProvider.CreateLogger(source);
                _loggerProvider = loggerProvider;
            }

            public IMqttNetScopedLogger CreateScopedLogger(string source)
                => new MqttNetChildLogger(_source + "." + source, _loggerProvider);

            public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
                => _logger.Log(ToMsLogging(logLevel), exception, message, parameters);

            private LogLevel ToMsLogging(MqttNetLogLevel level)
            {
                switch (level)
                {
                    case MqttNetLogLevel.Verbose:
                        return LogLevel.Trace;
                    case MqttNetLogLevel.Info:
                        return LogLevel.Information;
                    case MqttNetLogLevel.Warning:
                        return LogLevel.Warning;
                    case MqttNetLogLevel.Error:
                        return LogLevel.Error;
                    default:
                        return LogLevel.None;
                }
            }
        }
    }
}
