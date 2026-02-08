using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data.Configurations;

public class MockRequestLogConfiguration : IEntityTypeConfiguration<MockRequestLog>
{
    public void Configure(EntityTypeBuilder<MockRequestLog> builder)
    {
        builder.ToTable("mock_request_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.EndpointId);
        builder.Property(l => l.RuleId);

        builder.Property(l => l.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(l => l.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(l => l.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.QueryString)
            .HasMaxLength(2000);

        builder.Property(l => l.Headers)
            .HasColumnType("jsonb");

        builder.Property(l => l.Body)
            .HasColumnType("text");

        builder.Property(l => l.ResponseStatusCode)
            .IsRequired();

        builder.Property(l => l.ResponseBody)
            .HasColumnType("text");

        builder.Property(l => l.ResponseTimeMs)
            .IsRequired();

        builder.Property(l => l.IsMatched)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.FaultTypeApplied);

        builder.HasIndex(l => l.Timestamp)
            .HasDatabaseName("idx_log_timestamp");

        builder.HasIndex(l => l.EndpointId)
            .HasDatabaseName("idx_log_endpoint");
    }
}
