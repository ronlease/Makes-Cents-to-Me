using MakesCentsToMe.Api.Infrastructure.Claude;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container. The Claude HTTP endpoint is replaced
/// by <see cref="StubClaudeMessageHandler"/> so no test ever reaches the network.
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Force the host to start now so migration failures surface here with a clear stack trace.
        _ = Services;
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting (not ConfigureAppConfiguration) because Program.cs reads the connection string eagerly.
        builder.UseEnvironment("IntegrationTesting");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());
        builder.UseSetting("Claude:ApiKey", "integration-test-key");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ClaudeRequestRecorder>();
            services.AddHttpClient<IClaudeAnalysisService, ClaudeAnalysisService>()
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    new StubClaudeMessageHandler(serviceProvider.GetRequiredService<ClaudeRequestRecorder>()));
        });
    }
}
