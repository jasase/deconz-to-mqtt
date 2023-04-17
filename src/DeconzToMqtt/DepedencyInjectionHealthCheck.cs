using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DeconzToMqtt
{
    public class DepedencyInjectionHealthCheck<THealthCheck> : IHealthCheck
        where THealthCheck : IHealthCheck
    {
        private readonly THealthCheck _healthCheck;

        public DepedencyInjectionHealthCheck(THealthCheck healthCheck)
        {
            _healthCheck = healthCheck;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => _healthCheck.CheckHealthAsync(context, cancellationToken);
    }
}
