using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class ColumnMappingConfiguration : IEntityTypeConfiguration<ColumnMapping>
{
    public void Configure(EntityTypeBuilder<ColumnMapping> builder)
    {
        builder.HasKey(columnMapping => columnMapping.Id);

        builder.Property(columnMapping => columnMapping.ApplicationField)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(columnMapping => columnMapping.CsvColumnName)
            .HasMaxLength(200)
            .IsRequired();
    }
}
