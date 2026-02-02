using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting.Database.Configurations;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YamlDotNet.Serialization;

namespace FinanceManager.Crosscutting.Database
{
    public class FinanceManagerDbContext(
        DbContextOptions<FinanceManagerDbContext> options,
        IHttpContextAccessor httpContextAccessor) : DbContext(options)
    {

        public DbSet<User> Users { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<TransactionCategory> TransactionCategories { get; set; }

        public async Task<TransactionCategory?> GetCategoryAsync(string categoryId, CancellationToken cancellationToken)
        {
            return await TransactionCategories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BankAccountConfiguration());
            modelBuilder.ApplyConfiguration(new BudgetAlertConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());

            modelBuilder
                .Entity<BankAccount>()
                .HasQueryFilter(b => b.User.Email == httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.Email)!.Value);
        }
    }
}
