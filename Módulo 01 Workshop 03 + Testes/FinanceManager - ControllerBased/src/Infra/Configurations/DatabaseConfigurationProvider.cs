using Dapper;
using Microsoft.Data.SqlClient;

namespace FinanceManager.Infra.Configurations
{
    public class DatabaseConfigurationSource(string connectionString) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(connectionString);
    }

    public class DatabaseConfigurationProvider(string connectionString) : ConfigurationProvider
    {
        public override void Load()
        {
            var connection = new SqlConnection(connectionString);

            var authorizationSecret = connection.QueryFirstOrDefault<string>("SELECT TOP 1 AuthenticationSecret FROM Settings");

            Data = new Dictionary<string, string?>
            {
                {  "Authentication:Secret", authorizationSecret },
            };
        }
    }
}
