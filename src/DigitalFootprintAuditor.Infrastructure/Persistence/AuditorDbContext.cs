using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalFootprintAuditor.Infrastructure.Persistence;

public class AuditorDbContext : DbContext
{
    public AuditorDbContext(DbContextOptions<AuditorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<ScanTarget> ScanTargets => Set<ScanTarget>();
    public DbSet<ScanFinding> ScanFindings => Set<ScanFinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditorDbContext).Assembly);
    }
}