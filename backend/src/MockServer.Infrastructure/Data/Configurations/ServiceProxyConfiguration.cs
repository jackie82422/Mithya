using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data.Configurations;

public class ServiceProxyConfiguration : IEntityTypeConfiguration<ServiceProxy>
{
    public void Configure(EntityTypeBuilder<ServiceProxy> builder)
    {
        builder.ToTable("service_proxies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ServiceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => p.ServiceName)
            .IsUnique();

        builder.Property(p => p.TargetBaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.IsRecording)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.ForwardHeaders)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.AdditionalHeaders)
            .HasColumnType("jsonb");

        builder.Property(p => p.TimeoutMs)
            .IsRequired()
            .HasDefaultValue(10000);

        builder.Property(p => p.StripPathPrefix)
            .HasMaxLength(200);

        builder.Property(p => p.FallbackEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
