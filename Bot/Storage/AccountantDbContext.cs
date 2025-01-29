using Bot.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Storage;

public class AccountantDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    
    public AccountantDbContext(DbContextOptions<AccountantDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder
            .Entity<Category>()
            .HasMany(c => c.Purchases)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId);
        builder.Entity<Category>()
            .HasKey(c => c.Id);
        builder.Entity<Category>()
            .Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Entity<Purchase>()
            .HasKey(p => p.Id);
        builder.Entity<Purchase>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
        builder.Entity<Purchase>()
            .HasIndex(p => p.Date);
        builder.Entity<Purchase>()
            .HasIndex(p => p.CategoryId);
        builder.Entity<Purchase>()
            .Property(p => p.Date)
            .ValueGeneratedOnAdd();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetCreatedDate();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetCreatedDate()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: Purchase, State: EntityState.Added });

        foreach (var entry in entries)
        {
            ((Purchase)entry.Entity).Date = DateTime.UtcNow;
        }
    }
}