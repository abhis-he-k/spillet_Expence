using ExpenseSplitterApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseSplitterApi.Data
{
public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripMember> TripMembers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TripMember>(entity =>
            {
                entity.HasKey(tm => tm.Id);

                entity.HasIndex(tm => new { tm.TripId, tm.UserId }).IsUnique();

                entity.HasOne(tm => tm.Trip)
                    .WithMany(t => t.Members)
                    .HasForeignKey(tm => tm.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tm => tm.User)
                    .WithMany(u => u.TripMembers)
                    .HasForeignKey(tm => tm.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Trip)
                    .WithMany(t => t.Expenses)
                    .HasForeignKey(e => e.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PaidByUser)
                    .WithMany(u => u.ExpensesPaid)
                    .HasForeignKey(e => e.PaidByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExpenseSplit>(entity =>
            {
                entity.Property(es => es.AmountOwed)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(es => es.Expense)
                    .WithMany(e => e.Splits)
                    .HasForeignKey(es => es.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(es => es.User)
                    .WithMany(u => u.ExpenseSplits)
                    .HasForeignKey(es => es.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
