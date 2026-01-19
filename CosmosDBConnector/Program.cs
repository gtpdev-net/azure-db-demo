using CosmosDBConnector;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== CosmosDB Connector Demo ===\n");

// Load configuration from appsettings.Development.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

// Read configuration values
var accountEndpoint = configuration["CosmosDb:AccountEndpoint"] 
    ?? throw new Exception("AccountEndpoint not configured in appsettings.Development.json");
var databaseName = configuration["CosmosDb:DatabaseName"] 
    ?? throw new Exception("DatabaseName not configured in appsettings.Development.json");
var containerName = configuration["CosmosDb:ContainerName"] 
    ?? throw new Exception("ContainerName not configured in appsettings.Development.json");
var partitionKeyPath = configuration["CosmosDb:PartitionKeyPath"] ?? "/id";
var connectionMethod = configuration["CosmosDb:ConnectionMethod"] ?? "ManagedIdentity";
var connectionString = configuration["CosmosDb:ConnectionString"];
var accountKey = configuration["CosmosDb:AccountKey"];

Console.WriteLine($"Connection Method: {connectionMethod}");
Console.WriteLine($"Account Endpoint: {accountEndpoint}");
Console.WriteLine($"Database: {databaseName}");
Console.WriteLine($"Container: {containerName}\n");

// Create connector instance
var connector = new CosmosDbConnector(accountEndpoint, databaseName, containerName);

try
{
    // Connect using the configured method
    Microsoft.Azure.Cosmos.CosmosClient client = connectionMethod switch
    {
        "ManagedIdentity" => await connector.ConnectWithManagedIdentityAsync(),
        "Emulator" => await connector.ConnectToEmulatorAsync(),
        "ConnectionString" => await connector.ConnectWithConnectionStringAsync(
            connectionString ?? throw new Exception("ConnectionString not configured")),
        "AccountKey" => await connector.ConnectWithAccountKeyAsync(
            accountKey ?? throw new Exception("AccountKey not configured")),
        _ => throw new Exception($"Unknown connection method: {connectionMethod}")
    };

    // Get or create database and container
    var database = await connector.GetOrCreateDatabaseAsync(client);
    var container = await connector.GetOrCreateContainerAsync(database, containerName, partitionKeyPath);

    // Create a dummy record
    await connector.CreateDummyRecordAsync(container, partitionKeyPath);

    Console.WriteLine("\n✓ All operations completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    return 1;
}

return 0;

