using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MakesCentsToMe.Api.Infrastructure.Data.Configurations;

public class LearnedRuleConfiguration : IEntityTypeConfiguration<LearnedRule>
{
    public void Configure(EntityTypeBuilder<LearnedRule> builder)
    {
        builder.HasKey(learnedRule => learnedRule.Id);

        builder.HasOne(learnedRule => learnedRule.Category)
            .WithMany(category => category.LearnedRules)
            .HasForeignKey(learnedRule => learnedRule.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(learnedRule => learnedRule.CreatedAt)
            .IsRequired();

        builder.Property(learnedRule => learnedRule.NormalizedVendor)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(learnedRule => learnedRule.Pattern)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(learnedRule => learnedRule.UpdatedAt)
            .IsRequired();

        builder.HasIndex(learnedRule => learnedRule.Pattern)
            .IsUnique()
            .HasDatabaseName("IX_LearnedRules_Pattern");
    }
}
