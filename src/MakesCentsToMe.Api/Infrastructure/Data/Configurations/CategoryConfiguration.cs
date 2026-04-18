using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.HasData(DefaultCategories());
    }

    private static Category[] DefaultCategories() =>
    [
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), IsDefault = true, Name = "Dining" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), IsDefault = true, Name = "Education" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), IsDefault = true, Name = "Entertainment" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), IsDefault = true, Name = "Fees and Charges" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), IsDefault = true, Name = "Gifts and Donations" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"), IsDefault = true, Name = "Groceries" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"), IsDefault = true, Name = "Healthcare" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"), IsDefault = true, Name = "Housing" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"), IsDefault = true, Name = "Income" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000a"), IsDefault = true, Name = "Insurance" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000b"), IsDefault = true, Name = "Personal Care" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000c"), IsDefault = true, Name = "Shopping" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000d"), IsDefault = true, Name = "Transfer" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000e"), IsDefault = true, Name = "Transportation" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000f"), IsDefault = true, Name = "Travel" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000010"), IsDefault = true, Name = "Uncategorized" },
        new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000011"), IsDefault = true, Name = "Utilities" },
    ];
}
