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

Console.WriteLine($"Account Endpoint: {accountEndpoint}");
Console.WriteLine($"Database: {databaseName}");
Console.WriteLine($"Container: {containerName}\n");

// Create connector instance
var connector = new CosmosDbConnector(accountEndpoint, databaseName, containerName);

// Track results
var successfulMethods = new List<string>();
var failedMethods = new List<(string method, string error)>();

if (connectionMethod == "TestAll")
{
    Console.WriteLine("Mode: Testing all connection methods\n");
    Console.WriteLine("=" + new string('=', 70) + "\n");
    
    // Test each method
    await TestConnectionMethod("ManagedIdentity");
    await TestConnectionMethod("ConnectionString");
    await TestConnectionMethod("AccountKey");
    await TestConnectionMethod("Emulator");
    
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
        var enabledConfig = configuration[$"CosmosDb:{method}:Enabled"];
        if (enabledConfig != null && !bool.Parse(enabledConfig))
        {
            Console.WriteLine($"⊘ {method} is disabled in configuration. Skipping...\n");
            return false;
        }
        
        Microsoft.Azure.Cosmos.CosmosClient? client = null;
        
        // Connect using the specified method
        client = method switch
        {
            "ManagedIdentity" => await connector.ConnectWithManagedIdentityAsync(),
            "Emulator" => await connector.ConnectToEmulatorAsync(),
            "ConnectionString" => await connector.ConnectWithConnectionStringAsync(
                configuration["CosmosDb:ConnectionString:Value"] 
                ?? throw new Exception("ConnectionString.Value not configured")),
            "AccountKey" => await connector.ConnectWithAccountKeyAsync(
                configuration["CosmosDb:AccountKey:Value"] 
                ?? throw new Exception("AccountKey.Value not configured")),
            _ => throw new Exception($"Unknown connection method: {method}")
        };

        // Get or create database and container
        var database = await connector.GetOrCreateDatabaseAsync(client);
        var container = await connector.GetOrCreateContainerAsync(database, containerName, partitionKeyPath);

        // Create a dummy record
        await connector.CreateDummyRecordAsync(container, partitionKeyPath);

        Console.WriteLine($"\n✓ {method} - All operations completed successfully!\n");
        
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
