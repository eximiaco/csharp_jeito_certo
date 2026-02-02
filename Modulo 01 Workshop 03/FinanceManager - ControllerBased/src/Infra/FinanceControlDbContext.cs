using FinanceManager.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infra
{
    public class FinanceManagerDbContext : DbContext
    {
        public FinanceManagerDbContext(DbContextOptions<FinanceManagerDbContext> options)
            : base(options)
        {
            
        }

        public FinanceManagerDbContext(string connectionString)
        {
            Database.SetConnectionString(connectionString);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<TransactionCategory> TransactionCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(u => u.BankAccounts)
                .WithOne();

            modelBuilder.Entity<BankAccount>()
                .HasMany(b => b.Transactions)
                .WithOne();
        }
    }
}
