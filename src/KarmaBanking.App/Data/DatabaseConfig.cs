namespace KarmaBanking.App.Data
{
    using Microsoft.Data.SqlClient;

    public static class DatabaseConfig
    {
        // Redenumit din ConnectionString in DatabaseConnectionString (No Abbreviations)
        public static readonly string DatabaseConnectionString =
            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=KarmaBankingDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

        public static SqlConnection GetDatabaseConnection()
        {
            return new SqlConnection(DatabaseConnectionString);
        }
    }
}