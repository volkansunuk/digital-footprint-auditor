using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalFootprintAuditor.Infrastructure.Persistence.Configurations;

public class ScanTargetConfiguration : IEntityTypeConfiguration<ScanTarget>
{
    public void Configure(EntityTypeBuilder<ScanTarget> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TargetType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.TargetValue)
            .HasMaxLength(500)
            .IsRequired();
    }
}