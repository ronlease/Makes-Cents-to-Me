using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(transaction => transaction.Balance)
            .HasPrecision(18, 4);

        builder.Property(transaction => transaction.Category)
            .HasMaxLength(200);

        builder.Property(transaction => transaction.CheckNumber)
            .HasMaxLength(50);

        builder.Property(transaction => transaction.Date)
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(transaction => transaction.Fees)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(transaction => transaction.Interest)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(transaction => transaction.Principal)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(transaction => transaction.RawCsvRow)
            .IsRequired();

        builder.Property(transaction => transaction.RawData)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
