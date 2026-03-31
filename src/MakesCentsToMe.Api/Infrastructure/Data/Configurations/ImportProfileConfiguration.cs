using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class ImportProfileConfiguration : IEntityTypeConfiguration<ImportProfile>
{
    public void Configure(EntityTypeBuilder<ImportProfile> builder)
    {
        builder.HasKey(importProfile => importProfile.Id);

        builder.Property(importProfile => importProfile.AmountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(importProfile => importProfile.DateFormat)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasMany(importProfile => importProfile.ColumnMappings)
            .WithOne()
            .HasForeignKey(columnMapping => columnMapping.ImportProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
