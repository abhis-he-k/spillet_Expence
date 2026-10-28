using Microsoft.EntityFrameworkCore;
using ExpenseSplitterApi.Models;
using System.Diagnostics.Tracing;

namespace ExpenseSplitterApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }

        public DbSet<Expense> Expenses { get; set; }

        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }

        public DbSet<Settlement> Settlements {get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>()
            .HasOne(g => g.CreatedBy)
            .WithMany()
            .HasForeignKey(g => g.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Group>()
            .HasMany(g => g.Members)
            .WithMany(u => u.Groups);
        
        modelBuilder.Entity<ExpenseSplit>()
            .HasOne(es => es.User)
            .WithMany()
            .HasForeignKey(es => es.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExpenseSplit>()
            .HasOne(es => es.Expense)
             
            .WithMany(e => e.Splits)
            .HasForeignKey(es => es.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Settlement>()
            .HasOne(s => s.FromUser)
            .WithMany()
            .HasForeignKey(s => s.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Settlement>()
            .HasOne(s => s.ToUser)
            .WithMany()
            .HasForeignKey(s => s.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Settlement>()
            .HasOne(s => s.Group)
            .WithMany()
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}