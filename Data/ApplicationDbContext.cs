using Microsoft.EntityFrameworkCore;
using MxmChallenge.Models;

namespace MxmChallenge.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FinanceSnapshot> FinanceSnapshots { get; set; }
        public DbSet<FinanceSyncConflict> FinanceSyncConflicts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinanceSnapshot>(entity =>
            {
                entity.HasIndex(snapshot => snapshot.UserId).IsUnique();
                entity.Property(snapshot => snapshot.StateJson).HasColumnType("nvarchar(max)");
                entity.HasOne(snapshot => snapshot.User)
                    .WithOne()
                    .HasForeignKey<FinanceSnapshot>(snapshot => snapshot.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinanceSyncConflict>(entity =>
            {
                entity.HasIndex(conflict => new { conflict.UserId, conflict.Resolved });
                entity.Property(conflict => conflict.LocalValueJson).HasColumnType("nvarchar(max)");
                entity.Property(conflict => conflict.RemoteValueJson).HasColumnType("nvarchar(max)");
                entity.HasOne(conflict => conflict.User)
                    .WithMany()
                    .HasForeignKey(conflict => conflict.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
