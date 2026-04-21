namespace KarmaBanking.App.Data;

using Microsoft.Data.SqlClient;

public static class DatabaseConfig
{
    // Redenumit din ConnectionString in DatabaseConnectionString (No Abbreviations)
    public static readonly string DatabaseConnectionString =
        @"Server=DESKTOP-BIG8P7V\SQLEXPRESS;Database=KarmaBankingDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public static SqlConnection GetDatabaseConnection()
    {
        return new SqlConnection(DatabaseConnectionString);
    }
}