using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gymerp.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // Ajuste a string de conexão conforme necessário
            optionsBuilder.UseNpgsql("Host=localhost;Database=gymerp;Username=postgres;Password=postgres");
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
} 