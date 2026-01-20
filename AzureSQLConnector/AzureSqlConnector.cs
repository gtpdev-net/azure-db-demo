using Microsoft.Data.SqlClient;
using Azure.Identity;
using Azure.Core;

namespace AzureSQLConnector;

/// <summary>
/// Provides multiple methods to connect to Azure SQL Database.
/// Follows Azure best practices: uses Managed Identity for authentication,
/// implements proper error handling, and supports various connection scenarios.
/// </summary>
public class AzureSqlConnector
{
    private readonly string _serverName;
    private readonly string? _databaseName;
    private readonly string? _tableName;

    public AzureSqlConnector(string serverName, string? databaseName = null, string? tableName = null)
    {
        _serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
        _databaseName = databaseName;
        _tableName = tableName;
    }

    /// <summary>
    /// Connects using Managed Identity (recommended for Azure-hosted applications).
    /// This is the most secure method as it doesn't require credential management.
    /// Requires Entra ID authentication to be enabled on the Azure SQL server.
    /// </summary>
    public async Task<SqlConnection> ConnectWithManagedIdentityAsync()
    {
        try
        {
            Console.WriteLine("Connecting to Azure SQL Database using Managed Identity...");
            
            // Use DefaultAzureCredential which supports:
            // - Managed Identity (in Azure)
            // - Azure CLI (local development)
            // - Visual Studio/VS Code credentials
            var credential = new DefaultAzureCredential();
            
            // Get access token for Azure SQL Database
            var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);

            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = _serverName,
                InitialCatalog = _databaseName,
                Encrypt = true,
                TrustServerCertificate = false,
                ConnectTimeout = 30,
                ConnectRetryCount = 3,
                ConnectRetryInterval = 10
            };

            var connection = new SqlConnection(connectionStringBuilder.ConnectionString)
            {
                AccessToken = token.Token
            };

            await connection.OpenAsync();
            
            // Test the connection
            await ValidateConnectionAsync(connection);
            
            Console.WriteLine("✓ Successfully connected using Managed Identity");
            return connection;
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
    public async Task<SqlConnection> ConnectWithConnectionStringAsync(string connectionString)
    {
        try
        {
            Console.WriteLine("Connecting to Azure SQL Database using Connection String...");
            
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Test the connection
            await ValidateConnectionAsync(connection);
            
            Console.WriteLine("✓ Successfully connected using Connection String");
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect using Connection String: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects using SQL Authentication with username and password (for development/testing only).
    /// Not recommended for production - use Managed Identity instead.
    /// </summary>
    public async Task<SqlConnection> ConnectWithSqlAuthenticationAsync(string username, string password)
    {
        try
        {
            Console.WriteLine("Connecting to Azure SQL Database using SQL Authentication...");
            
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = _serverName,
                InitialCatalog = _databaseName,
                UserID = username,
                Password = password,
                Encrypt = true,
                TrustServerCertificate = false,
                ConnectTimeout = 30,
                ConnectRetryCount = 3,
                ConnectRetryInterval = 10
            };

            var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();
            
            // Test the connection
            await ValidateConnectionAsync(connection);
            
            Console.WriteLine("✓ Successfully connected using SQL Authentication");
            return connection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect using SQL Authentication: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates the connection by querying the database version.
    /// </summary>
    private async Task ValidateConnectionAsync(SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand("SELECT @@VERSION as Version, DB_NAME() as DatabaseName", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var version = reader["Version"].ToString();
                var dbName = reader["DatabaseName"].ToString();
                
                // Extract just the SQL Server version info
                var versionInfo = version?.Split('\n')[0] ?? "Unknown";
                Console.WriteLine($"  Database: {dbName}");
                Console.WriteLine($"  Version: {versionInfo}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to validate connection to Azure SQL Database", ex);
        }
    }

    /// <summary>
    /// Creates a test table if it doesn't exist.
    /// </summary>
    public async Task CreateTableIfNotExistsAsync(SqlConnection connection, string? tableName = null)
    {
        try
        {
            var tblName = tableName ?? _tableName ?? throw new ArgumentException("Table name must be provided");
            
            Console.WriteLine($"\nChecking if table '{tblName}' exists...");

            // Check if table exists
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

            using var checkCommand = new SqlCommand(checkTableQuery, connection);
            checkCommand.Parameters.AddWithValue("@TableName", tblName);
            
            var result = await checkCommand.ExecuteScalarAsync();
            var tableExists = result != null && (int)result > 0;

            if (!tableExists)
            {
                Console.WriteLine($"Creating table '{tblName}'...");
                
                var createTableQuery = $@"
                    CREATE TABLE [{tblName}] (
                        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        Name NVARCHAR(255) NOT NULL,
                        Description NVARCHAR(MAX),
                        Category NVARCHAR(100),
                        Status NVARCHAR(50),
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        Value INT,
                        Tags NVARCHAR(500)
                    )";

                using var createCommand = new SqlCommand(createTableQuery, connection);
                await createCommand.ExecuteNonQueryAsync();
                
                Console.WriteLine($"✓ Table '{tblName}' created successfully");
            }
            else
            {
                Console.WriteLine($"✓ Table '{tblName}' already exists");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to create table: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a dummy record in the table for testing purposes.
    /// </summary>
    public async Task<Guid> CreateDummyRecordAsync(SqlConnection connection, string? tableName = null)
    {
        try
        {
            var tblName = tableName ?? _tableName ?? throw new ArgumentException("Table name must be provided");
            
            Console.WriteLine("\n--- Creating Dummy Record ---");

            var recordId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;

            var insertQuery = $@"
                INSERT INTO [{tblName}] (Id, Name, Description, Category, Status, CreatedAt, Value, Tags)
                VALUES (@Id, @Name, @Description, @Category, @Status, @CreatedAt, @Value, @Tags)";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Id", recordId);
            command.Parameters.AddWithValue("@Name", "Sample Record");
            command.Parameters.AddWithValue("@Description", "This is a dummy record created for testing");
            command.Parameters.AddWithValue("@Category", "Test");
            command.Parameters.AddWithValue("@Status", "Active");
            command.Parameters.AddWithValue("@CreatedAt", timestamp);
            command.Parameters.AddWithValue("@Value", 42);
            command.Parameters.AddWithValue("@Tags", "sample,test,dummy");

            Console.WriteLine($"Creating dummy record with id: {recordId}");
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            Console.WriteLine($"✓ Dummy record created successfully!");
            Console.WriteLine($"  Record ID: {recordId}");
            Console.WriteLine($"  Rows affected: {rowsAffected}");

            return recordId;
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"✗ SQL error: {ex.Number} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error creating dummy record: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates basic CRUD operations on a table.
    /// </summary>
    public async Task DemonstrateCrudOperationsAsync(SqlConnection connection, string tableName, Guid id)
    {
        try
        {
            Console.WriteLine("\n--- CRUD Operations Demo ---");

            // Create
            Console.WriteLine($"Creating record with id: {id}");
            var insertQuery = $@"
                INSERT INTO [{tableName}] (Id, Name, Description, Category, Status, Value)
                VALUES (@Id, @Name, @Description, @Category, @Status, @Value)";
            
            using (var insertCommand = new SqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@Id", id);
                insertCommand.Parameters.AddWithValue("@Name", "CRUD Test Record");
                insertCommand.Parameters.AddWithValue("@Description", "Testing CRUD operations");
                insertCommand.Parameters.AddWithValue("@Category", "Test");
                insertCommand.Parameters.AddWithValue("@Status", "Active");
                insertCommand.Parameters.AddWithValue("@Value", 100);
                
                await insertCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Record created");
            }

            // Read
            Console.WriteLine($"Reading record with id: {id}");
            var selectQuery = $"SELECT * FROM [{tableName}] WHERE Id = @Id";
            
            using (var selectCommand = new SqlCommand(selectQuery, connection))
            {
                selectCommand.Parameters.AddWithValue("@Id", id);
                using var reader = await selectCommand.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    Console.WriteLine($"✓ Record read: Name = {reader["Name"]}");
                }
            }

            // Update
            Console.WriteLine($"Updating record with id: {id}");
            var updateQuery = $@"
                UPDATE [{tableName}] 
                SET Status = @Status, Value = @Value 
                WHERE Id = @Id";
            
            using (var updateCommand = new SqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@Id", id);
                updateCommand.Parameters.AddWithValue("@Status", "Updated");
                updateCommand.Parameters.AddWithValue("@Value", 200);
                
                await updateCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Record updated");
            }

            // Delete
            Console.WriteLine($"Deleting record with id: {id}");
            var deleteQuery = $"DELETE FROM [{tableName}] WHERE Id = @Id";
            
            using (var deleteCommand = new SqlCommand(deleteQuery, connection))
            {
                deleteCommand.Parameters.AddWithValue("@Id", id);
                var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Record deleted. Rows affected: {rowsAffected}");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"✗ SQL error: {ex.Number} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error during CRUD operations: {ex.Message}");
            throw;
        }
    }
}
