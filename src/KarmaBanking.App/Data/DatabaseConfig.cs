using Microsoft.Data.SqlClient;

public static class DatabaseConfig
{
    public static readonly string ConnectionString =
        "Server=localhost;Database=KarmaBankingDb;Trusted_Connection=True;TrustServerCertificate=True;";
    public static SqlConnection GetDatabaseConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}