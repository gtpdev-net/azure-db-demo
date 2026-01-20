# Cosmos DB Connector Demo
A simple demo application that connects to Azure Cosmos DB NoSQL using various authentication methods.

## Prerequisites
- .NET 8.0 SDK
- Azure Cosmos DB account with NoSQL API
- (Optional) Azure Cosmos DB Emulator for local development

## Quick Start

See [CONFIGURATION.md](CONFIGURATION.md) for detailed step-by-step setup instructions.

## Configuration

### Setting up appsettings.Development.json

The application reads configuration from [appsettings.Development.json](appsettings.Development.json). You'll need to populate this file with your Cosmos DB connection details.

#### Required Settings

1. **AccountEndpoint** (Required)
   - Your Cosmos DB account URI
   - Find in Azure Portal: Navigate to your Cosmos DB account → **Overview** → **URI**
   - Format: `https://<your-account-name>.documents.azure.com:443/`

2. **DatabaseName** (Required)
   - Name of your Cosmos DB database
   - Must be an existing database in your Cosmos DB account

3. **ContainerName** (Required)
   - Name of your container within the database
   - Must be an existing container in the specified database

4. **PartitionKeyPath** (Optional, default: `/id`)
   - The partition key path for your container
   - Find in Azure Portal: Select your container → **Scale & Settings** → **Settings** → **Partition Key**
   - Example: `/category`, `/userId`, `/id`

5. **ConnectionMethod** (Required)
   - Specifies how to authenticate with Cosmos DB
   - Options:
     - `TestAll` - **Test all available connection methods** (recommended for initial setup)
     - `ManagedIdentity` - For Azure-hosted apps (requires RBAC setup)
     - `ConnectionString` - For local development
     - `AccountKey` - Alternative for development
     - `Emulator` - For local Cosmos DB Emulator

#### Connection Method-Specific Settings

Each connection method can be individually enabled/disabled and configured:

##### For ManagedIdentity method:
```json
"ManagedIdentity": {
  "Enabled": true
}
```
- No additional credentials required
- **Requires proper RBAC role assignment** - even if you have Owner/Contributor at subscription level
- Uses `DefaultAzureCredential` which supports:
  - Managed Identity (when running in Azure)
  - Azure CLI credentials (for local development with `az login`)
  - Visual Studio/VS Code credentials

##### For ConnectionString method:
```json
"ConnectionString": {
  "Enabled": true,
  "Value": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;"
}
```
- **Value**
  - Find in Azure Portal: Your Cosmos DB account → **Settings** → **Keys** → **Primary Connection String**
  - ⚠️ Keep this secure - do not commit to source control

##### For AccountKey method:
```json
"AccountKey": {
  "Enabled": true,
  "Value": "your-primary-or-secondary-key"
}
```
- **Value**
  - Find in Azure Portal: Your Cosmos DB account → **Settings** → **Keys** → **Primary Key**
  - ⚠️ Keep this secure - do not commit to source control

##### For Emulator method:
```json
"Emulator": {
  "Enabled": true
}
```
- No additional settings required
- Uses local Cosmos DB Emulator endpoint
- Emulator must be running locally

#### Setting up RBAC for ManagedIdentity

**Important:** Subscription-level roles (Owner, Contributor) only grant *control plane* access (manage the Cosmos DB resource itself). To access data, you need a *data plane* role assignment:

```bash
# Get your user's Object ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Assign Cosmos DB Built-in Data Contributor role
az cosmosdb sql role assignment create \
  --account-name <your-cosmos-account> \
  --resource-group <your-rg> \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $USER_ID \
  --scope "/"
```

**For local development**, if you don't want to set up RBAC, use `ConnectionString` or `AccountKey` methods instead, which bypass RBAC.

### Example Configuration

#### Test All Methods (Recommended for initial setup)
```json
{
  "CosmosDb": {
    "AccountEndpoint": "https://your-account.documents.azure.com:443/",
    "DatabaseName": "MyDatabase",
    "ContainerName": "MyContainer",
    "PartitionKeyPath": "/id",
    "ConnectionMethod": "TestAll",
    
    "ManagedIdentity": {
      "Enabled": false
    },
    
    "ConnectionString": {
      "Enabled": true,
      "Value": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key=="
    },
    
    "AccountKey": {
      "Enabled": true,
      "Value": "your-primary-key=="
    },
    
    "Emulator": {
      "Enabled": false
    }
  }
}
```

#### Single Method
```json
{
  "CosmosDb": {
    "AccountEndpoint": "https://your-account.documents.azure.com:443/",
    "DatabaseName": "MyDatabase",
    "ContainerName": "MyContainer",
    "PartitionKeyPath": "/id",
    "ConnectionMethod": "AccountKey",
    
    "AccountKey": {
      "Enabled": true,
      "Value": "your-primary-key=="
    }
  }
}
```

### Security Best Practices

1. **Never commit secrets**: Add `appsettings.Development.json` to `.gitignore` if it contains sensitive data
2. **Use Managed Identity in production**: This is the most secure method as it eliminates credential management
3. **Use Azure Key Vault**: Store connection strings and keys in Azure Key Vault for production environments
4. **Rotate keys regularly**: If using AccountKey or ConnectionString methods

## Running the Application

```bash
cd CosmosDBConnector
dotnet run
```

The application will:
1. Load configuration from `appsettings.Development.json`
2. Connect to Cosmos DB using the specified connection method
3. Create or verify the database and container
4. Perform basic CRUD operations as a demonstration
