using Microsoft.Data.SqlClient;

namespace KarmaBanking.App.Data
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection()
        {
            _connectionString = "Server=localhost;Database=KarmaBankingDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public SqlConnection GetDatabaseConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}