using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data.Configurations;

public class MockEndpointConfiguration : IEntityTypeConfiguration<MockEndpoint>
{
    public void Configure(EntityTypeBuilder<MockEndpoint> builder)
    {
        builder.ToTable("mock_endpoints");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ServiceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Protocol)
            .IsRequired();

        builder.Property(e => e.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.DefaultResponse)
            .HasColumnType("text");

        builder.Property(e => e.DefaultStatusCode);

        builder.Property(e => e.ProtocolSettings)
            .HasColumnType("jsonb");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(e => new { e.Path, e.HttpMethod, e.IsActive })
            .HasDatabaseName("idx_endpoint_path_method_active");

        builder.HasMany(e => e.Rules)
            .WithOne(r => r.Endpoint)
            .HasForeignKey(r => r.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
