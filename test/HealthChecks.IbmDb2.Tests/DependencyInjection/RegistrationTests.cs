namespace HealthChecks.IbmDb2.Tests.DependencyInjection;

public class ibmdb2_registration_should
{
    [Fact]
    public void add_health_check_when_properly_configured()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddDb2("Server=localhost");

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.First();
        var check = registration.Factory(serviceProvider);

        registration.Name.ShouldBe("db2");
        check.ShouldBeOfType<IbmDb2HealthCheck>();
    }

    [Fact]
    public void add_named_health_check_when_properly_configured()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddDb2("Server=localhost", name: "my-db2-1");

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.First();
        var check = registration.Factory(serviceProvider);

        registration.Name.ShouldBe("my-db2-1");
        check.ShouldBeOfType<IbmDb2HealthCheck>();
    }

    [Fact]
    public void add_default_health_check_with_connection_string_factory_when_properly_configured()
    {
        var services = new ServiceCollection();
        var factoryCalled = false;
        services.AddHealthChecks()
            .AddDb2(_ =>
            {
                factoryCalled = true;
                return "Server=localhost";
            }, name: "my-db2-1");

        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.First();
        var check = registration.Factory(serviceProvider);

        registration.Name.ShouldBe("my-db2-1");
        check.ShouldBeOfType<IbmDb2HealthCheck>();
        factoryCalled.ShouldBeTrue();
    }

    [Fact]
    public void add_default_health_check()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddDb2(_ => "connectionstring");

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        var registration = options.Value.Registrations.First();
        var check = registration.Factory(serviceProvider);

        registration.Name.ShouldBe("db2");
        check.ShouldBeOfType<IbmDb2HealthCheck>();
    }
}
