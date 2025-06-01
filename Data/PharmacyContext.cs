using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace PharmacyAPI.Data
{
    public class PharmacyContext : DbContext
    {
        public PharmacyContext(DbContextOptions<PharmacyContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PostgreSQL specific setup
            modelBuilder.HasPostgresExtension("citext");
            
            // Configure decimal precision and case-insensitive text
            modelBuilder.Entity<Medicine>()
                .Property(m => m.Name)
                .HasColumnType("citext");
            
            modelBuilder.Entity<Medicine>()
                .Property(m => m.Category)
                .HasColumnType("citext");
            
            modelBuilder.Entity<Medicine>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);
            
            modelBuilder.Entity<Purchase>()
                .Property(p => p.TotalCost)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Purchase>()
                .Property(p => p.UnitCost)
                .HasPrecision(18, 2);
            
            modelBuilder.Entity<Sale>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);
            
            modelBuilder.Entity<SaleItem>()
                .Property(si => si.TotalPrice)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<SaleItem>()
                .Property(si => si.UnitPrice)
                .HasPrecision(18, 2);

            // Configure relationships
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId);
                
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId);
                
            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId);
                
            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Medicine)
                .WithMany()
                .HasForeignKey(si => si.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Medicine)
                .WithMany()
                .HasForeignKey(p => p.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);
            
            // Add this for PostgreSQL serial columns
            modelBuilder.UseIdentityColumns();
            
            // Configure concurrency token
            modelBuilder.Entity<Medicine>()
                .Property(m => m.LockVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}