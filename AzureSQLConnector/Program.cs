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

Console.WriteLine($"Server: {serverName}");
Console.WriteLine($"Database: {databaseName}");
Console.WriteLine($"Table: {tableName}\n");

// Create connector instance
var connector = new AzureSqlConnector(serverName, databaseName, tableName);

// Track results
var successfulMethods = new List<string>();
var failedMethods = new List<(string method, string error)>();

if (connectionMethod == "TestAll")
{
    Console.WriteLine("Mode: Testing all connection methods\n");
    Console.WriteLine("=" + new string('=', 70) + "\n");
    
    // Test each method
    await TestConnectionMethod("ManagedIdentity");
    await TestConnectionMethod("SqlAuthentication");
    await TestConnectionMethod("ConnectionString");
    
    // Summary
    Console.WriteLine("\n" + new string('=', 72));
    Console.WriteLine("SUMMARY");
    Console.WriteLine(new string('=', 72));
    
    if (successfulMethods.Count > 0)
    {
        Console.WriteLine($"\n✓ Successful connections ({successfulMethods.Count}):");
        foreach (var method in successfulMethods)
        {
            Console.WriteLine($"  • {method}");
        }
    }
    
    if (failedMethods.Count > 0)
    {
        Console.WriteLine($"\n✗ Failed connections ({failedMethods.Count}):");
        foreach (var (method, error) in failedMethods)
        {
            Console.WriteLine($"  • {method}");
            Console.WriteLine($"    Reason: {error}");
        }
    }
    
    Console.WriteLine($"\nTotal: {successfulMethods.Count}/{successfulMethods.Count + failedMethods.Count} connection methods succeeded");
    
    return failedMethods.Count > 0 ? 1 : 0;
}
else
{
    // Single method test
    Console.WriteLine($"Mode: Testing single connection method - {connectionMethod}\n");
    return await TestConnectionMethod(connectionMethod) ? 0 : 1;
}

async Task<bool> TestConnectionMethod(string method)
{
    try
    {
        Console.WriteLine($"--- Testing: {method} ---\n");
        
        // Check if method is enabled
        var enabledConfig = configuration[$"AzureSql:{method}:Enabled"];
        if (enabledConfig != null && !bool.Parse(enabledConfig))
        {
            Console.WriteLine($"⊘ {method} is disabled in configuration. Skipping...\n");
            return false;
        }
        
        Microsoft.Data.SqlClient.SqlConnection? connection = null;
        
        // Connect using the specified method
        connection = method switch
        {
            "ManagedIdentity" => await connector.ConnectWithManagedIdentityAsync(),
            "ConnectionString" => await connector.ConnectWithConnectionStringAsync(
                configuration["AzureSql:ConnectionString:Value"] 
                ?? throw new Exception("ConnectionString.Value not configured")),
            "SqlAuthentication" => await connector.ConnectWithSqlAuthenticationAsync(
                configuration["AzureSql:SqlAuthentication:Username"] 
                ?? throw new Exception("SqlAuthentication.Username not configured"),
                configuration["AzureSql:SqlAuthentication:Password"] 
                ?? throw new Exception("SqlAuthentication.Password not configured")),
            _ => throw new Exception($"Unknown connection method: {method}")
        };

        using (connection)
        {
            // Create table if it doesn't exist
            await connector.CreateTableIfNotExistsAsync(connection, tableName);

            // Create a dummy record
            await connector.CreateDummyRecordAsync(connection, tableName);

            Console.WriteLine($"\n✓ {method} - All operations completed successfully!\n");
        }
        
        successfulMethods.Add(method);
        return true;
    }
    catch (Exception ex)
    {
        var errorMessage = ex.Message.Split('\n')[0]; // Get first line of error
        if (errorMessage.Length > 100)
            errorMessage = errorMessage.Substring(0, 97) + "...";
            
        Console.WriteLine($"\n✗ {method} - Failed: {errorMessage}\n");
        failedMethods.Add((method, errorMessage));
        return false;
    }
}
