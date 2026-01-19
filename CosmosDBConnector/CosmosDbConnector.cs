using Microsoft.Azure.Cosmos;
using Azure.Identity;

namespace CosmosDBConnector;

/// <summary>
/// Provides multiple methods to connect to Azure Cosmos DB.
/// Follows Azure best practices: uses Managed Identity for authentication,
/// implements proper error handling, and supports various connection scenarios.
/// </summary>
public class CosmosDbConnector
{
    private readonly string _accountEndpoint;
    private readonly string? _databaseName;
    private readonly string? _containerName;

    public CosmosDbConnector(string accountEndpoint, string? databaseName = null, string? containerName = null)
    {
        _accountEndpoint = accountEndpoint ?? throw new ArgumentNullException(nameof(accountEndpoint));
        _databaseName = databaseName;
        _containerName = containerName;
    }

    /// <summary>
    /// Connects using Managed Identity (recommended for Azure-hosted applications).
    /// This is the most secure method as it doesn't require credential management.
    /// </summary>
    public async Task<CosmosClient> ConnectWithManagedIdentityAsync()
    {
        try
        {
            Console.WriteLine("Connecting to Cosmos DB using Managed Identity...");
            
            // Use DefaultAzureCredential which supports:
            // - Managed Identity (in Azure)
            // - Azure CLI (local development)
            // - Visual Studio/VS Code credentials
            var credential = new DefaultAzureCredential();
            
            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };

            var client = new CosmosClient(_accountEndpoint, credential, clientOptions);
            
            // Test the connection
            await ValidateConnectionAsync(client);
            
            Console.WriteLine("✓ Successfully connected using Managed Identity");
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect using Managed Identity: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects using a connection string (for development/testing only).
    /// Not recommended for production due to embedded credentials.
    /// </summary>
    public async Task<CosmosClient> ConnectWithConnectionStringAsync(string connectionString)
    {
        try
        {
            Console.WriteLine("Connecting to Cosmos DB using Connection String...");
            
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };

            var client = new CosmosClient(connectionString, clientOptions);
            
            // Test the connection
            await ValidateConnectionAsync(client);
            
            Console.WriteLine("✓ Successfully connected using Connection String");
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect using Connection String: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects using account key (for development/testing only).
    /// Not recommended for production - use Managed Identity instead.
    /// </summary>
    public async Task<CosmosClient> ConnectWithAccountKeyAsync(string accountKey)
    {
        try
        {
            Console.WriteLine("Connecting to Cosmos DB using Account Key...");
            
            if (string.IsNullOrWhiteSpace(accountKey))
                throw new ArgumentException("Account key cannot be empty", nameof(accountKey));

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };

            var client = new CosmosClient(_accountEndpoint, accountKey, clientOptions);
            
            // Test the connection
            await ValidateConnectionAsync(client);
            
            Console.WriteLine("✓ Successfully connected using Account Key");
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect using Account Key: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects to Cosmos DB Emulator for local development.
    /// Uses the well-known emulator endpoint and key.
    /// </summary>
    public async Task<CosmosClient> ConnectToEmulatorAsync()
    {
        try
        {
            Console.WriteLine("Connecting to Cosmos DB Emulator...");
            
            // Well-known emulator connection details
            const string emulatorEndpoint = "https://localhost:8081";
            const string emulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway, // Emulator requires Gateway mode
                RequestTimeout = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                // Disable SSL validation for local emulator
                HttpClientFactory = () => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                })
            };

            var client = new CosmosClient(emulatorEndpoint, emulatorKey, clientOptions);
            
            // Test the connection
            await ValidateConnectionAsync(client);
            
            Console.WriteLine("✓ Successfully connected to Cosmos DB Emulator");
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to Emulator: {ex.Message}");
            Console.WriteLine("Make sure the Cosmos DB Emulator is running.");
            throw;
        }
    }

    /// <summary>
    /// Gets a database reference. Creates the database if it doesn't exist.
    /// </summary>
    public async Task<Database> GetOrCreateDatabaseAsync(CosmosClient client, string? databaseName = null)
    {
        try
        {
            var dbName = databaseName ?? _databaseName ?? throw new ArgumentException("Database name must be provided");
            
            Console.WriteLine($"Getting or creating database: {dbName}");
            
            var response = await client.CreateDatabaseIfNotExistsAsync(
                id: dbName,
                throughput: 400 // Minimum RU/s for manual throughput
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
                Console.WriteLine($"✓ Database '{dbName}' created");
            else
                Console.WriteLine($"✓ Database '{dbName}' already exists");

            return response.Database;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to get/create database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a container reference. Creates the container if it doesn't exist.
    /// </summary>
    public async Task<Container> GetOrCreateContainerAsync(
        Database database, 
        string? containerName = null, 
        string partitionKeyPath = "/id")
    {
        try
        {
            var ctrName = containerName ?? _containerName ?? throw new ArgumentException("Container name must be provided");
            
            Console.WriteLine($"Getting or creating container: {ctrName}");

            var containerProperties = new ContainerProperties(ctrName, partitionKeyPath);
            
            // Don't provision throughput - share from database level
            var response = await database.CreateContainerIfNotExistsAsync(containerProperties);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
                Console.WriteLine($"✓ Container '{ctrName}' created with partition key '{partitionKeyPath}' (sharing database throughput)");
            else
                Console.WriteLine($"✓ Container '{ctrName}' already exists");

            return response.Container;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to get/create container: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates the connection by attempting to read account properties.
    /// </summary>
    private async Task ValidateConnectionAsync(CosmosClient client)
    {
        try
        {
            var response = await client.ReadAccountAsync();
            Console.WriteLine($"  Account: {response.Id}");
            Console.WriteLine($"  Regions: {string.Join(", ", response.ReadableRegions.Select(r => r.Name))}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to validate connection to Cosmos DB", ex);
        }
    }

    /// <summary>
    /// Creates a dummy record in the container for testing purposes.
    /// </summary>
    public async Task<dynamic> CreateDummyRecordAsync(Container container, string partitionKeyPath = "/PartitionKeyValue")
    {
        try
        {
            Console.WriteLine("\n--- Creating Dummy Record ---");

            // Generate unique ID
            var recordId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            // Extract partition key property name (remove the leading '/')
            var partitionKeyProperty = partitionKeyPath.TrimStart('/');

            // Create dummy record with dynamic properties
            var dummyRecord = new
            {
                id = recordId,
                PartitionKeyValue = recordId, // Use the partition key property
                Name = "Sample Record",
                Description = "This is a dummy record created for testing",
                Category = "Test",
                Status = "Active",
                CreatedAt = timestamp,
                Value = 42,
                Tags = new[] { "sample", "test", "dummy" },
                Metadata = new
                {
                    Source = "CosmosDB Connector Demo",
                    Version = "1.0"
                }
            };

            Console.WriteLine($"Creating dummy record with id: {recordId}");
            var response = await container.CreateItemAsync(dummyRecord, new PartitionKey(recordId));
            
            Console.WriteLine($"✓ Dummy record created successfully!");
            Console.WriteLine($"  Record ID: {recordId}");
            Console.WriteLine($"  RU consumed: {response.RequestCharge}");
            Console.WriteLine($"  Status Code: {response.StatusCode}");

            return dummyRecord;
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"✗ Cosmos DB error: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error creating dummy record: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates basic CRUD operations on a container.
    /// </summary>
    public async Task DemonstrateCrudOperationsAsync<T>(Container container, T item, string id, string partitionKeyValue) where T : class
    {
        try
        {
            Console.WriteLine("\n--- CRUD Operations Demo ---");

            // Create
            Console.WriteLine($"Creating item with id: {id}");
            var createResponse = await container.CreateItemAsync(item, new PartitionKey(partitionKeyValue));
            Console.WriteLine($"✓ Item created. RU consumed: {createResponse.RequestCharge}");

            // Read
            Console.WriteLine($"Reading item with id: {id}");
            var readResponse = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKeyValue));
            Console.WriteLine($"✓ Item read. RU consumed: {readResponse.RequestCharge}");

            // Update (upsert)
            Console.WriteLine($"Updating item with id: {id}");
            var upsertResponse = await container.UpsertItemAsync(item, new PartitionKey(partitionKeyValue));
            Console.WriteLine($"✓ Item updated. RU consumed: {upsertResponse.RequestCharge}");

            // Delete
            Console.WriteLine($"Deleting item with id: {id}");
            var deleteResponse = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionKeyValue));
            Console.WriteLine($"✓ Item deleted. RU consumed: {deleteResponse.RequestCharge}");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"✗ Cosmos DB error: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error during CRUD operations: {ex.Message}");
            throw;
        }
    }
}
