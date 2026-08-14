using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalFootprintAuditor.Infrastructure.Persistence.Configurations;

public class ScanFindingConfiguration : IEntityTypeConfiguration<ScanFinding>
{
    public void Configure(EntityTypeBuilder<ScanFinding> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.ScannerName).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Title).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(2000).IsRequired();
        builder.Property(f => f.Source).HasMaxLength(200);

        builder.Property(f => f.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}