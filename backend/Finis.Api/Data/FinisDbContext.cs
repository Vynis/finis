using Finis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Data;

public class FinisDbContext : DbContext
{
    public FinisDbContext(DbContextOptions<FinisDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.InitialBalance).HasPrecision(18, 2);
        });

        // Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(100).IsRequired();
            entity.Property(a => a.InitialBalance).HasPrecision(18, 2);
            entity.Property(a => a.ColorHex).HasMaxLength(20);
            entity.Property(a => a.Icon).HasMaxLength(50);

            entity.HasOne(a => a.User)
                  .WithMany(u => u.Accounts)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.ColorHex).HasMaxLength(20);
            entity.Property(c => c.Icon).HasMaxLength(50);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Categories)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CreditCard
        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(card => card.Id);
            entity.Property(card => card.Name).HasMaxLength(100).IsRequired();
            entity.Property(card => card.Limit).HasPrecision(18, 2).IsRequired();
            entity.Property(card => card.ColorHex).HasMaxLength(20);
            entity.Property(card => card.Brand).HasMaxLength(50);

            entity.HasOne(card => card.User)
                  .WithMany(u => u.CreditCards)
                  .HasForeignKey(card => card.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(card => card.Account)
                  .WithMany(a => a.CreditCards)
                  .HasForeignKey(card => card.AccountId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Notes).HasMaxLength(500);
            entity.Property(t => t.RecurrenceGroupId).HasMaxLength(50);

            entity.HasIndex(t => new { t.UserId, t.Date });
            entity.HasIndex(t => new { t.UserId, t.AccountId, t.Date });
            entity.HasIndex(t => new { t.UserId, t.CreditCardId, t.InvoiceYear, t.InvoiceMonth });
            entity.HasIndex(t => new { t.UserId, t.RecurrenceGroupId });

            entity.HasOne(t => t.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Account)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.CreditCard)
                  .WithMany(card => card.Transactions)
                  .HasForeignKey(t => t.CreditCardId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
