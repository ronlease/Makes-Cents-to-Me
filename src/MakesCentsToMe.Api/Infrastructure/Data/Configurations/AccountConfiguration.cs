using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.AccountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(account => account.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(account => new { account.InstitutionId, account.Name })
            .IsUnique();

        builder.HasOne(account => account.Institution)
            .WithMany(institution => institution.Accounts)
            .HasForeignKey(account => account.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(account => account.ImportProfile)
            .WithOne(importProfile => importProfile.Account)
            .HasForeignKey<ImportProfile>(importProfile => importProfile.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(account => account.Transactions)
            .WithOne(transaction => transaction.Account)
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
