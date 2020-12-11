using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace HealthChecks.Cache.DependencyInjection
{
    public static class CachedHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddCached(this IHealthChecksBuilder builder, 
                                                     string name,
                                                     IHealthCheck healthCheck, 
                                                     TimeSpan cachePeriod,
                                                     Func<DateTimeOffset> dateTimeProvider = null,
                                                     HealthStatus? failureStatus = default, 
                                                     IEnumerable<string> tags = default, 
                                                     TimeSpan? timeout = default)
        {
            dateTimeProvider ??= (() => DateTimeOffset.UtcNow);
            var cachedHealthCheck = new CachedHealthCheck(healthCheck,
                                                          cachePeriod,
                                                          dateTimeProvider);
            return builder.Add(new HealthCheckRegistration(name, 
                                                           cachedHealthCheck, 
                                                           failureStatus, 
                                                           tags, 
                                                           timeout));
        }
    }
}
