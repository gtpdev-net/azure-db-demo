# Azure SQL Database Connector Demo
A simple demo application that connects to Azure SQL Database using various authentication methods.

## Prerequisites
- .NET 8.0 SDK
- Azure SQL Database

## Configuration

### Setting up appsettings.Development.json

The application reads configuration from [appsettings.Development.json](appsettings.Development.json). You'll need to populate this file with your Azure SQL Database connection details.

#### Required Settings

1. **ServerName** (Required)
   - Your Azure SQL Server name
   - Find in Azure Portal: Navigate to your SQL Server → **Overview** → **Server name**
   - Format: `your-server.database.windows.net`

2. **DatabaseName** (Required)
   - Name of your database in the Azure SQL Server
   - Must be an existing database in your SQL Server

3. **TableName** (Optional, default: `TestTable`)
   - Name of the table to use for testing
   - The application will create this table if it doesn't exist

4. **ConnectionMethod** (Required)
   - Specifies how to authenticate with Azure SQL Database
   - Options:
     - `TestAll` - **Test all available connection methods** (recommended for initial setup)
     - `ManagedIdentity` - For Azure-hosted apps (requires Entra ID authentication)
     - `ConnectionString` - For development with full connection string
     - `SqlAuthentication` - For development with SQL username/password

#### Connection Method-Specific Settings

Each connection method can be individually enabled/disabled and configured:

##### For ManagedIdentity method:
```json
"ManagedIdentity": {
  "Enabled": true
}
```
- No additional credentials required
- Uses `DefaultAzureCredential` which supports:
  - Managed Identity (when running in Azure)
  - Azure CLI credentials (for local development with `az login`)
  - Visual Studio/VS Code credentials

##### For ConnectionString method:
```json
"ConnectionString": {
  "Enabled": true,
  "Value": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;User Id=your-username;Password=your-password;Encrypt=True;"
}
```
- **Value**
  - Find in Azure Portal: Your SQL Database → **Settings** → **Connection strings** → **ADO.NET**
  - Replace `{your_username}` and `{your_password}` with actual credentials
  - ⚠️ Keep this secure - do not commit to source control

##### For SqlAuthentication method:
```json
"SqlAuthentication": {
  "Enabled": true,
  "Username": "your-username",
  "Password": "your-password"
}
```
- **Username** - SQL Server authentication username
- **Password** - SQL Server authentication password
  - Find in Azure Portal: Create during SQL Server setup or reset under **Settings** → **SQL databases** → **Set admin password**
  - ⚠️ Keep these secure - do not commit to source control

##### For ManagedIdentity method:
- No additional settings required in appsettings
- **Requires Entra ID (Azure Active Directory) authentication** to be configured on your Azure SQL Server
- Uses `DefaultAzureCredential` which supports:
  - Managed Identity (when running in Azure)
  - Azure CLI credentials (for local development with `az login`)
  - Visual Studio/VS Code credentials

**Setting up Entra ID Authentication:**

1. **Set an Entra ID admin** for your SQL Server:
   ```bash
   az sql server ad-admin create \
     --resource-group <your-rg> \
     --server-name <your-server> \
     --display-name <admin-user-or-group> \
     --object-id <user-or-group-object-id>
   ```

2. **Grant database access** to your user or managed identity:
   - Connect to your database using SQL Server Management Studio or Azure Data Studio with Entra ID authentication
   - Run the following SQL commands:
   ```sql
   CREATE USER [your-user@domain.com] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [your-user@domain.com];
   ALTER ROLE db_datawriter ADD MEMBER [your-user@domain.com];
   ALTER ROLE db_ddladmin ADD MEMBER [your-user@domain.com];
   ```

**For local development**, if you don't want to set up Entra ID authentication, use `ConnectionString` or `SqlAuthentication` methods instead.

### Example Configuration

#### Test All Methods (Recommended for initial setup)
```json
{
  "AzureSql": {
    "ServerName": "your-server.database.windows.net",
    "DatabaseName": "MyDatabase",
    "TableName": "TestTable",
    "ConnectionMethod": "TestAll",
    
    "ManagedIdentity": {
      "Enabled": true
    },
    
    "ConnectionString": {
      "Enabled": true,
      "Value": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=MyDatabase;User Id=sqladmin;Password=YourPassword123!;Encrypt=True;"
    },
    
    "SqlAuthentication": {
      "Enabled": true,
      "Username": "sqladmin",
      "Password": "YourPassword123!"
    }
  }
}
```

#### Single Method
```json
{
  "AzureSql": {
    "ServerName": "your-server.database.windows.net",
    "DatabaseName": "MyDatabase",
    "TableName": "TestTable",
    "ConnectionMethod": "SqlAuthentication",
    
    "SqlAuthentication": {
      "Enabled": true,
      "Username": "sqladmin",
      "Password": "YourPassword123!"
    }
  }
}
```

### Security Best Practices

1. **Never commit secrets**: Add `appsettings.Development.json` to `.gitignore` if it contains sensitive data
2. **Use Managed Identity in production**: This is the most secure method as it eliminates credential management
3. **Use Azure Key Vault**: Store connection strings and passwords in Azure Key Vault for production environments
4. **Enable firewall rules**: Configure Azure SQL Server firewall to allow access only from trusted IP addresses
5. **Use encryption**: Always use encrypted connections (Encrypt=True in connection string)

## Running the Application

```bash
cd AzureSQLConnector
dotnet run
```

The application will:
1. Load configuration from `appsettings.Development.json`
2. Connect to Azure SQL Database using the specified connection method
3. Create or verify the test table
4. Perform basic CRUD operations as a demonstration

## What It Does

The application demonstrates:
- **Multiple authentication methods**: Managed Identity, Connection String, and SQL Authentication
- **Connection validation**: Verifies the database connection and displays server information
- **Table management**: Creates a test table if it doesn't exist
- **Data operations**: Inserts a dummy record to verify write access
- **Best practices**: Uses parameterized queries, proper error handling, and connection retry logic
