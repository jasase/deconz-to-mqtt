using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Framework.Abstraction.Extension;

namespace DeconzToMqtt.Health
{
    public class HealthCheckService
    {
        private readonly ILogger _logger;
        private readonly ConcurrentBag<IHealthCheck> _healthChecks;
        private CancellationTokenSource _cancelationToken;
        private object _task;

        public HealthCheckService(ILogger logger)
        {
            _logger = logger;
            _healthChecks = new ConcurrentBag<IHealthCheck>();
        }

        public void Start()
        {
            _cancelationToken = new CancellationTokenSource();
            _task = Task.Factory.StartNew(Run);
        }


        public void Stop()
        {
            if (_cancelationToken != null)
            {
                _cancelationToken.Cancel();
            }

            _cancelationToken = null;
        }

        public void AddHealthCheck(IHealthCheck healthCheck)
            => _healthChecks.Add(healthCheck);

        private async void Run()
        {
            while (!_cancelationToken.IsCancellationRequested)
            {
                _logger.Info("Health check");
                try
                {
                    var healthCheck = true;
                    foreach (var check in _healthChecks)
                    {
                        healthCheck &= check.Healthy();
                    }

                    File.WriteAllText("health", DateTime.Now.ToString("o"));

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error in health check");
                }

                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            _task = null;
        }
    }
}
