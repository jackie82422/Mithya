using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data.Configurations;

public class MockRuleConfiguration : IEntityTypeConfiguration<MockRule>
{
    public void Configure(EntityTypeBuilder<MockRule> builder)
    {
        builder.ToTable("mock_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.EndpointId)
            .IsRequired();

        builder.Property(r => r.RuleName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(r => r.MatchConditions)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(r => r.ResponseStatusCode)
            .IsRequired()
            .HasDefaultValue(200);

        builder.Property(r => r.ResponseBody)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.ResponseHeaders)
            .HasColumnType("jsonb");

        builder.Property(r => r.DelayMs)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.IsTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.IsResponseHeadersTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(r => new { r.EndpointId, r.Priority })
            .HasDatabaseName("idx_rule_endpoint_priority");

        builder.HasOne(r => r.Endpoint)
            .WithMany(e => e.Rules)
            .HasForeignKey(r => r.EndpointId);
    }
}
