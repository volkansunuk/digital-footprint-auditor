using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalFootprintAuditor.Infrastructure.Persistence.Configurations;

public class ScanConfiguration : IEntityTypeConfiguration<Scan>
{
    public void Configure(EntityTypeBuilder<Scan> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(s => s.Targets)
            .WithOne(t => t.Scan)
            .HasForeignKey(t => t.ScanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Findings)
            .WithOne(f => f.Scan)
            .HasForeignKey(f => f.ScanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}