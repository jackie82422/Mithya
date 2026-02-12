using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mithya.Core.Entities;

namespace Mithya.Infrastructure.Data.Configurations;

public class EndpointGroupConfiguration : IEntityTypeConfiguration<EndpointGroup>
{
    public void Configure(EntityTypeBuilder<EndpointGroup> builder)
    {
        builder.ToTable("endpoint_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasColumnType("text");

        builder.Property(g => g.Color)
            .HasMaxLength(20);

        builder.Property(g => g.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(g => g.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(g => g.Mappings)
            .WithOne(m => m.Group)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EndpointGroupMappingConfiguration : IEntityTypeConfiguration<EndpointGroupMapping>
{
    public void Configure(EntityTypeBuilder<EndpointGroupMapping> builder)
    {
        builder.ToTable("endpoint_group_mappings");

        builder.HasKey(m => new { m.GroupId, m.EndpointId });

        builder.HasOne(m => m.Endpoint)
            .WithMany()
            .HasForeignKey(m => m.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
