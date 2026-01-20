# Quick Configuration Guide

This guide will help you configure the Cosmos DB Connector to test all connection methods.

## Step 1: Get Your Cosmos DB Account Details

From the Azure Portal, navigate to your Cosmos DB account and note:
- **Account Endpoint**: Found under "Overview" → "URI" (e.g., `https://your-account.documents.azure.com:443/`)
- **Database name**: Your database name (must exist)
- **Container name**: Your container name (must exist in the database)
- **Partition Key Path**: Found under your container → "Scale & Settings" → "Partition Key" (e.g., `/id`)

## Step 2: Choose Your Authentication Method(s)

### Option A: Test All Methods (Recommended)
Set `ConnectionMethod` to `TestAll` and configure all methods below.

### Option B: Test Individual Method
Set `ConnectionMethod` to one of: `ManagedIdentity`, `ConnectionString`, `AccountKey`, or `Emulator`

## Step 3: Configure Each Method

### Method 1: Account Key (Easiest to start)
Uses the primary or secondary key from your Cosmos DB account:

1. In Azure Portal: Your Cosmos DB account → **Settings** → **Keys**
2. Copy the **Primary Key** or **Secondary Key**
3. Update appsettings.Development.json:
```json
"AccountKey": {
  "Enabled": true,
  "Value": "your-primary-or-secondary-key"
}
```

### Method 2: Connection String
Uses the full connection string from your Cosmos DB account:

1. In Azure Portal: Your Cosmos DB account → **Settings** → **Keys**
2. Copy the **Primary Connection String** or **Secondary Connection String**
3. Update appsettings.Development.json:
```json
"ConnectionString": {
  "Enabled": true,
  "Value": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key-here;"
}
```

### Method 3: Managed Identity / Entra ID (Most Secure)
This requires additional setup:

1. **Enable Entra ID authentication** by assigning RBAC roles
2. **Assign yourself the Cosmos DB Built-in Data Contributor role**:
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
3. **Login via Azure CLI**:
   ```bash
   az login
   ```
4. Update appsettings.Development.json:
```json
"ManagedIdentity": {
  "Enabled": true
}
```

**Important:** Subscription-level roles (Owner, Contributor) only grant *control plane* access (manage the Cosmos DB resource itself). To access data, you need a *data plane* role assignment like "Cosmos DB Built-in Data Contributor".

### Method 4: Emulator (Local Development)
Uses the local Cosmos DB Emulator for testing:

1. **Install and start the Cosmos DB Emulator** (Windows or Linux with Docker)
   - Download from: https://aka.ms/cosmosdb-emulator
   - Or use Docker: `docker run -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator`
2. Update appsettings.Development.json:
```json
"Emulator": {
  "Enabled": true
}
```

**Note:** The emulator uses a well-known endpoint and key, so no additional configuration is needed.

## Step 4: Update appsettings.Development.json

Full example for testing all methods:

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
      "Value": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=abc123...xyz789=="
    },
    
    "AccountKey": {
      "Enabled": true,
      "Value": "abc123...xyz789=="
    },
    
    "Emulator": {
      "Enabled": false
    }
  }
}
```

## Step 5: Run the Application

```bash
dotnet run
```

## Expected Output

When using `TestAll` mode:
```
=== CosmosDB Connector Demo ===

Account Endpoint: https://your-account.documents.azure.com:443/
Database: MyDatabase
Container: MyContainer

Mode: Testing all connection methods

========================================================================

--- Testing: ManagedIdentity ---

⊘ ManagedIdentity is disabled in configuration. Skipping...

--- Testing: ConnectionString ---

Connecting to Cosmos DB using Connection String...
  Account: your-account
  Regions: East US
✓ Successfully connected using Connection String
Getting or creating database: MyDatabase
✓ Database 'MyDatabase' already exists
Getting or creating container: MyContainer
✓ Container 'MyContainer' already exists

--- Creating Dummy Record ---
Creating dummy record with id: 12345678-1234-1234-1234-123456789012
✓ Dummy record created successfully!
  Record ID: 12345678-1234-1234-1234-123456789012
  RU consumed: 10.29
  Status Code: Created

✓ ConnectionString - All operations completed successfully!

--- Testing: AccountKey ---

Connecting to Cosmos DB using Account Key...
...

========================================================================
SUMMARY
========================================================================

✓ Successful connections (2):
  • ConnectionString
  • AccountKey

✗ Failed connections (2):
  • ManagedIdentity
    Reason: ManagedIdentity is disabled in configuration
  • Emulator
    Reason: Failed to connect to Emulator: The operation was canceled because the token timed out...

Total: 2/4 connection methods succeeded
```

## Troubleshooting

### AccountKey or ConnectionString fails
- Verify the key is correct (copy from Azure Portal → Keys)
- Check that the account endpoint matches your Cosmos DB account
- Ensure firewall rules allow your IP address
- Verify the database and container names are correct

### ManagedIdentity fails with "Forbidden" or "Unauthorized"
- Run `az login` first
- Verify you have the "Cosmos DB Built-in Data Contributor" role assigned
- Note: Subscription-level roles (Owner/Contributor) don't grant data access
- Check role assignment:
  ```bash
  az cosmosdb sql role assignment list \
    --account-name <your-account> \
    --resource-group <your-rg>
  ```

### Emulator fails
- Make sure the Cosmos DB Emulator is running
- Check it's accessible at `https://localhost:8081`
- For SSL certificate errors, the emulator auto-handles this
- If using Docker, ensure port 8081 is exposed

### Request Rate Too Large (429 errors)
- Cosmos DB has RU (Request Unit) limits
- Increase throughput in Azure Portal if needed
- Add retry logic (already included in the connector)

## Disable Methods You Don't Want to Test

Set `"Enabled": false` for any method you want to skip:

```json
"Emulator": {
  "Enabled": false
}
```

Or change `ConnectionMethod` from `TestAll` to a specific method:
```json
"ConnectionMethod": "AccountKey"
```

## Security Best Practices

1. **Never commit secrets** - Add `appsettings.Development.json` to `.gitignore` if it contains keys
2. **Use Managed Identity in production** - Most secure method, no credential management
3. **Store keys in Azure Key Vault** - For production environments
4. **Rotate keys regularly** - If using AccountKey or ConnectionString methods
5. **Use Read-only keys** - If you only need read access (found in Keys → Read-only Keys)
6. **Enable firewall rules** - Restrict access to trusted IP addresses in Cosmos DB settings
