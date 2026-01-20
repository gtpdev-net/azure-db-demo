using AzureSQLConnector;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== Azure SQL Database Connector Demo ===\n");

// Load configuration from appsettings.Development.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

// Read configuration values
var serverName = configuration["AzureSql:ServerName"] 
    ?? throw new Exception("ServerName not configured in appsettings.Development.json");
var databaseName = configuration["AzureSql:DatabaseName"] 
    ?? throw new Exception("DatabaseName not configured in appsettings.Development.json");
var tableName = configuration["AzureSql:TableName"] ?? "TestTable";
var connectionMethod = configuration["AzureSql:ConnectionMethod"] ?? "ManagedIdentity";
var connectionString = configuration["AzureSql:ConnectionString"];
var username = configuration["AzureSql:Username"];
var password = configuration["AzureSql:Password"];

Console.WriteLine($"Connection Method: {connectionMethod}");
Console.WriteLine($"Server: {serverName}");
Console.WriteLine($"Database: {databaseName}");
Console.WriteLine($"Table: {tableName}\n");

// Create connector instance
var connector = new AzureSqlConnector(serverName, databaseName, tableName);

try
{
    // Connect using the configured method
    Microsoft.Data.SqlClient.SqlConnection connection = connectionMethod switch
    {
        "ManagedIdentity" => await connector.ConnectWithManagedIdentityAsync(),
        "ConnectionString" => await connector.ConnectWithConnectionStringAsync(
            connectionString ?? throw new Exception("ConnectionString not configured")),
        "SqlAuthentication" => await connector.ConnectWithSqlAuthenticationAsync(
            username ?? throw new Exception("Username not configured"),
            password ?? throw new Exception("Password not configured")),
        _ => throw new Exception($"Unknown connection method: {connectionMethod}")
    };

    using (connection)
    {
        // Create table if it doesn't exist
        await connector.CreateTableIfNotExistsAsync(connection, tableName);

        // Create a dummy record
        await connector.CreateDummyRecordAsync(connection, tableName);

        Console.WriteLine("\n✓ All operations completed successfully!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    return 1;
}

return 0;
