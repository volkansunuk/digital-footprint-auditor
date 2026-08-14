/*Veritabanı haritasıdır
  Bu sınıf, projenin veritabanındaki karşılığıdır.
    "Benim veritabanımda ScanTargets adında bir tablo olsun ve
    bu tablo C#'taki ScanTarget sınıfına denk gelsin" dediğimiz yerdir.
    EF Core'un veritabanımızla konuşmasını sağlayacak olan ana köprü
*/

namespace DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.EntityFrameworkCore; //EF Core kütüphanesini kullanacağımızı belirtiyoruz
public class ApplicationDbContext : DbContext /* EF Core'un veritabanı yeteneklerini kullanabilmek
 için sınıfımızı DbContext sınıfından türetiyoruz (miras alıyoruz) */
{
    /* yapıcı metod ekliyoruz ki veritabanı ayarlarını dıaşrdan parametre olarak alalım ve temel
    DbContext sınıfına iletelim*/
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    public DbSet<ScanTarget> ScanTargets { get; set; } /* ScanTarget sınıfı ile ScanTargets 
    tablosunu eşleştiriyoruz. */
    public DbSet<Scan> Scans { get; set; } /* Scan sınıfı ile Scans tablosunu eşleştiriyoruz. */
    public DbSet<ScanFinding> ScanFindings { get; set; } /* ScanFinding sınıfı ile 
    ScanFindings tablosunu eşleştiriyoruz. */

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

    // --- İlişki tipleri (foreign key + cascade delete) ---
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

