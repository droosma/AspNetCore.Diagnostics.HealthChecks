using FluentAssertions;
using FunctionalTests.Base;
using HealthChecks.Cache.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FunctionalTests.HealthChecks.Cache
{
    [Collection("execution")]
    public class cached_healthcheck_should
    {
        private readonly ExecutionFixture _fixture;

        public cached_healthcheck_should(ExecutionFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        [Fact]
        public async Task return_cached_check_result_when_within_cache_window()
        {
            TimeSpan oneSecond = TimeSpan.FromSeconds(1);
            var now = new DateTimeOffset(2020, 12, 1, 10, 0, 0, TimeSpan.Zero);
            var cacheDuration = TimeSpan.FromSeconds(30);
            var healthCheckSubstitute = Substitute.For<IHealthCheck>();

            var webHostBuilder = new WebHostBuilder()
               .UseStartup<DefaultStartup>()
               .ConfigureServices(services =>
               {
                   services.AddHealthChecks()
                           .AddCached("cachedcheck", 
                                      healthCheckSubstitute, 
                                      cacheDuration, 
                                      () => now,
                                      tags: new string[] { "cachedcheck" });
               }).Configure(app =>
               {
                   app.UseHealthChecks("/health", new HealthCheckOptions()
                   {
                       Predicate = r => r.Tags.Contains("cachedcheck")
                   });
               });

            var server = new TestServer(webHostBuilder);
            await server.CreateRequest("/health").GetAsync();

            now = now.Add(cacheDuration.Subtract(oneSecond));
            await server.CreateRequest("/health").GetAsync();

            await healthCheckSubstitute.Received(1)
                                       .CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
                                       .ConfigureAwait(false);
        }

        [Fact]
        public async Task return_new_check_result_when_outside_cache_window()
        {
            TimeSpan oneSecond = TimeSpan.FromSeconds(1);
            var now = new DateTimeOffset(2020, 12, 1, 10, 0, 0, TimeSpan.Zero);
            var cacheDuration = TimeSpan.FromSeconds(30);
            var healthCheckSubstitute = Substitute.For<IHealthCheck>();

            var webHostBuilder = new WebHostBuilder()
               .UseStartup<DefaultStartup>()
               .ConfigureServices(services =>
               {
                   services.AddHealthChecks()
                           .AddCached("cachedcheck",
                                      healthCheckSubstitute,
                                      cacheDuration,
                                      () => now,
                                      tags: new string[] { "cachedcheck" });
               }).Configure(app =>
               {
                   app.UseHealthChecks("/health", new HealthCheckOptions()
                   {
                       Predicate = r => r.Tags.Contains("cachedcheck")
                   });
               });

            var server = new TestServer(webHostBuilder);
            await server.CreateRequest("/health").GetAsync();

            now = now.Add(cacheDuration.Add(oneSecond));
            await server.CreateRequest("/health").GetAsync();

            await healthCheckSubstitute.Received(2)
                                       .CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
                                       .ConfigureAwait(false);
        }

        [Fact]
        public async Task return_inner_health_check_result()
        {
            var healthCheckSubstitute = Substitute.For<IHealthCheck>();
            healthCheckSubstitute.CheckHealthAsync(default, default).ReturnsForAnyArgs(new HealthCheckResult(HealthStatus.Healthy));
            var webHostBuilder = new WebHostBuilder()
               .UseStartup<DefaultStartup>()
               .ConfigureServices(services =>
               {
                   services.AddHealthChecks()
                           .AddCached("cachedcheck",
                                      healthCheckSubstitute,
                                      TimeSpan.Zero,
                                      tags: new string[] { "cachedcheck" });
               }).Configure(app =>
               {
                   app.UseHealthChecks("/health", new HealthCheckOptions()
                   {
                       Predicate = r => r.Tags.Contains("cachedcheck")
                   });
               });

            var server = new TestServer(webHostBuilder);
            var result = await server.CreateRequest("/health").GetAsync();

            result.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
