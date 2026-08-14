namespace DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore; 
public class ApplicationDbContext : DbContext 
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    public DbSet<ScanTarget> ScanTargets { get; set; } 
    public DbSet<Scan> Scans { get; set; } 
    public DbSet<ScanFinding> ScanFindings { get; set; } 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- ScanTarget: alan uzunluğu ve enum dönüşümü ---
        modelBuilder.Entity<ScanTarget>()
            .Property(t => t.TargetValue)
            .HasMaxLength(500);

        modelBuilder.Entity<ScanTarget>()
            .Property(t => t.TargetType)
            .HasConversion<string>();

        // --- Scan: enum dönüşümleri ---
        modelBuilder.Entity<Scan>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Scan>()
            .Property(s => s.RiskLevel)
            .HasConversion<string>();

        // --- ScanFinding: alan uzunlukları ve enum dönüşümü ---
        modelBuilder.Entity<ScanFinding>()
            .Property(f => f.ScannerName)
            .HasMaxLength(100);

        modelBuilder.Entity<ScanFinding>()
            .Property(f => f.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<ScanFinding>()
            .Property(f => f.Description)
            .HasMaxLength(1000);

        modelBuilder.Entity<ScanFinding>()
            .Property(f => f.Source)
            .HasMaxLength(100);

        modelBuilder.Entity<ScanFinding>()
            .Property(f => f.Severity)
            .HasConversion<string>();

        modelBuilder.Entity<Scan>()
            .HasMany(s => s.Targets)
            .WithOne()
            .HasForeignKey(t => t.ScanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Scan>()
            .HasMany(s => s.Findings)
            .WithOne()
            .HasForeignKey(f => f.ScanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}

