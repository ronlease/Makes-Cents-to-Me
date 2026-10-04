using MakesCentsToMe.Api.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// A DI scope paired with the <see cref="AppDbContext"/> resolved from it. Dispose to release both.
/// </summary>
public sealed class DatabaseScope(AsyncServiceScope scope) : IAsyncDisposable
{
    public AppDbContext DbContext { get; } = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
