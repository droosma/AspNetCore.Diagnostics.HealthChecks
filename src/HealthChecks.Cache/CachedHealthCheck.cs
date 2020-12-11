using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.Cache
{
    public class CachedHealthCheck : IHealthCheck
    {
        private readonly IHealthCheck _healthCheck;
        private readonly TimeSpan _timeToLive;
        private readonly Func<DateTimeOffset> _dateTimeProvider;
        private HealthCheckResult _cachedHealthCheckResult;
        private DateTimeOffset _updatedAt;

        public CachedHealthCheck(IHealthCheck healthCheck,
                                 TimeSpan timeToLive,
                                 Func<DateTimeOffset> dateTimeProvider)
        {
            _healthCheck = healthCheck;
            _timeToLive = timeToLive;
            _dateTimeProvider = dateTimeProvider;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                                                        CancellationToken cancellationToken = new CancellationToken())
        {
            var expiresAt = _updatedAt.Add(_timeToLive);
            if (_updatedAt != DateTimeOffset.MinValue
               && expiresAt >= _dateTimeProvider())
            {
                return Task.FromResult(_cachedHealthCheckResult);
            }

            _updatedAt = _dateTimeProvider();
            return _healthCheck.CheckHealthAsync(context, cancellationToken);
        }
    }
}
