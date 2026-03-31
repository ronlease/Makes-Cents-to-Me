using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace MakesCentsToMe.Unit.Infrastructure;

/// <summary>
/// Creates AppDbContext instances backed by the EF Core in-memory provider.
/// The in-memory provider does not support PostgreSQL-specific column types such as "jsonb",
/// and cannot handle Dictionary properties without a value converter.
/// This factory subclasses AppDbContext to register a JSON value converter for
/// Transaction.RawData so that the in-memory provider can store it as a string.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new context backed by a uniquely-named in-memory database.
    /// Each call gets its own isolated store — suitable for the majority of tests.
    /// </summary>
    public static AppDbContext Create() => CreateWithDatabaseName(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates a context backed by an in-memory database with the specified name.
    /// Use this when multiple context instances need to share the same store — for
    /// example, when seeding through one context and exercising the service through another
    /// to avoid EF change-tracker conflicts.
    /// </summary>
    public static AppDbContext CreateWithDatabaseName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TestAppDbContext(options);
    }

    /// <summary>
    /// Overrides OnModelCreating to replace the PostgreSQL "jsonb" column type on
    /// Transaction.RawData with a JSON string value converter compatible with the in-memory provider.
    /// </summary>
    private sealed class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // The production configuration sets HasColumnType("jsonb") on Transaction.RawData.
            // The in-memory provider cannot resolve "jsonb" — it requires a value converter
            // to know how to store Dictionary<string, string> in memory.
            var jsonConverter = new ValueConverter<Dictionary<string, string>, string>(
                dictionaryValue => JsonSerializer.Serialize(dictionaryValue, (JsonSerializerOptions?)null),
                stringValue => JsonSerializer.Deserialize<Dictionary<string, string>>(stringValue, (JsonSerializerOptions?)null)
                               ?? new Dictionary<string, string>());

            modelBuilder.Entity<Transaction>()
                .Property(transaction => transaction.RawData)
                .HasConversion(jsonConverter);
        }
    }
}
