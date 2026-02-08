using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data.Configurations;

public class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.ToTable("scenarios");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasColumnType("text");

        builder.Property(s => s.InitialState)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.CurrentState)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(s => s.Steps)
            .WithOne(st => st.Scenario)
            .HasForeignKey(st => st.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScenarioStepConfiguration : IEntityTypeConfiguration<ScenarioStep>
{
    public void Configure(EntityTypeBuilder<ScenarioStep> builder)
    {
        builder.ToTable("scenario_steps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StateName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.MatchConditions)
            .HasColumnType("jsonb");

        builder.Property(s => s.ResponseStatusCode)
            .IsRequired()
            .HasDefaultValue(200);

        builder.Property(s => s.ResponseBody)
            .HasColumnType("text");

        builder.Property(s => s.ResponseHeaders)
            .HasColumnType("jsonb");

        builder.Property(s => s.IsTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.DelayMs)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.NextState)
            .HasMaxLength(100);

        builder.Property(s => s.Priority)
            .IsRequired()
            .HasDefaultValue(100);

        builder.HasOne(s => s.Endpoint)
            .WithMany()
            .HasForeignKey(s => s.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
