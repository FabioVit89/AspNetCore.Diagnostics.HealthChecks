using System.Net;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HealthChecks.IbmDb2.Tests.Functional;

public class DBConfigSetting
{
    public string ConnectionString { get; set; } = null!;
}

public class ibmdb2_healthcheck_should(Db2ContainerFixture db2ContainerFixture) : IClassFixture<Db2ContainerFixture>
{
    [Fact]
    public async Task be_healthy_if_ibmdb2_is_available()
    {
        var connectionString = db2ContainerFixture.GetConnectionString();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHealthChecks()
                .AddDb2(connectionString, tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task be_unhealthy_if_sql_query_is_not_valid()
    {
        var connectionString = db2ContainerFixture.GetConnectionString();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHealthChecks()
                .AddDb2(connectionString, "SELECT 1 FROM SYS", tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task be_unhealthy_if_ibmdb2_is_not_available()
    {
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHealthChecks()
                .AddDb2("Server=10.0.0.1;Port=8010;User ID=db2inst1;Password=Password12!;database=testdb", tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task be_healthy_if_ibmdb2_is_available_by_iServiceProvider_registered()
    {
        var connectionString = db2ContainerFixture.GetConnectionString();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(new DBConfigSetting
                {
                    ConnectionString = connectionString
                });

                services.AddHealthChecks()
                        .AddDb2(_ => _.GetRequiredService<DBConfigSetting>().ConnectionString, tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task be_unhealthy_if_ibmdb2_is_not_available_registered()
    {
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(new DBConfigSetting
                {
                    ConnectionString = "Server=10.0.0.1;User ID=db2inst1;Password=Password12!;database=testdb"
                });

                services.AddHealthChecks()
                        .AddDb2(_ => _.GetRequiredService<DBConfigSetting>().ConnectionString, tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task unhealthy_check_log_detailed_messages()
    {
        var connectionString = db2ContainerFixture.GetConnectionString();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services
                .AddLogging(b =>
                        b.ClearProviders()
                        .Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TestLoggerProvider>())
                    )
                .AddHealthChecks()
                .AddDb2(connectionString, "SELECT 1 FROM SYS", tags: ["db2"]);
            })
            .Configure(app =>
            {
                app.UseHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = r => r.Tags.Contains("db2")
                });
            });

        using var server = new TestServer(webHostBuilder);

        using var response = await server.CreateRequest("/health").GetAsync();

        var testLoggerProvider = (TestLoggerProvider)server.Services.GetRequiredService<ILoggerProvider>();

        testLoggerProvider.ShouldNotBeNull();
        var logger = testLoggerProvider.GetLogger("Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService");

        logger.ShouldNotBeNull();
        logger?.EventLog[0].Item2.ShouldNotContain("with message '(null)'");
    }
}
