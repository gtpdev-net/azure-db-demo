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
This method uses Azure Entra ID (formerly Azure Active Directory) authentication, eliminating the need to manage keys or connection strings.

#### Understanding Control Plane vs Data Plane Access

**This is the most common source of confusion with Managed Identity authentication.**

- **Control Plane**: Manages the Cosmos DB resource itself (create/delete account, change settings, manage firewall rules)
  - Subscription-level roles: Owner, Contributor, Reader
  - Resource-level roles: Cosmos DB Account Contributor, Cosmos DB Operator
  - ⚠️ **These roles do NOT grant access to read/write data**

- **Data Plane**: Accesses the actual data (read/write documents, query containers)
  - Cosmos DB-specific roles: Cosmos DB Built-in Data Contributor, Cosmos DB Built-in Data Reader
  - ✅ **These roles ARE required to access data**

**Key Point**: Even if you have Owner or Contributor at the subscription level, you still need a separate data plane role assignment to access Cosmos DB data.

#### Prerequisites

1. Azure CLI installed and configured
2. Appropriate permissions to assign roles on the Cosmos DB account
3. Your Cosmos DB account, resource group, and database/container already created

#### Option A: Grant Access Using Azure CLI (Recommended)

This is the fastest method and works in all environments (local dev, CI/CD, Cloud Shell).

**Step 1: Login to Azure**
```bash
az login
```

**Step 2: Get your user's Object ID (Principal ID)**
```bash
USER_ID=$(az ad signed-in-user show --query id -o tsv)
echo "Your User Object ID: $USER_ID"
```

**Step 3: Assign the Cosmos DB Built-in Data Contributor role**
```bash
# Replace with your values
COSMOS_ACCOUNT="your-cosmos-account-name"
RESOURCE_GROUP="your-resource-group-name"

# Assign the data plane role
az cosmosdb sql role assignment create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $USER_ID \
  --scope "/"
```

**Note on Scope:**
- `/` = Full account access (all databases and containers)
- `/dbs/MyDatabase` = Access to a specific database and its containers
- `/dbs/MyDatabase/colls/MyContainer` = Access to a specific container only

**Step 4: Verify the role assignment**
```bash
az cosmosdb sql role assignment list \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query "[?principalId=='$USER_ID']" \
  --output table
```

You should see output showing your role assignment with the role definition for "Cosmos DB Built-in Data Contributor".

**Step 5: Update appsettings.Development.json**
```json
"ManagedIdentity": {
  "Enabled": true
}
```

#### Option B: Grant Access Using Azure Portal

If you prefer a graphical interface:

**Step 1: Get your User Object ID**
1. Go to **Azure Portal** → **Microsoft Entra ID** → **Users**
2. Search for your user account
3. Click on your user → Copy the **Object ID** from the overview page

**Step 2: Navigate to Your Cosmos DB Account**
1. Go to **Azure Portal** → **Cosmos DB Accounts** → Select your account
2. In the left menu, find **Settings** → **Data Explorer**
3. Click on the **Access Control (IAM)** button in the Data Explorer toolbar

**Alternative Path:**
1. From your Cosmos DB account page
2. Left menu → **Settings** → **Data Explorer**
3. Top menu → **Access Control** (or use the search bar to find "Role assignments")

**Step 3: Create a Role Assignment**
1. Click **+ Add** → **Add role assignment**
2. In the **Role** tab:
   - Search for "Cosmos DB Built-in Data Contributor"
   - Select it and click **Next**
3. In the **Members** tab:
   - Select **User, group, or service principal**
   - Click **+ Select members**
   - Search for and select your user account
   - Click **Select**, then **Next**
4. In the **Review + assign** tab:
   - Review the settings
   - Click **Review + assign** to complete

**Step 4: Verify the Assignment**
1. In **Data Explorer**, click **Access Control**
2. Go to **Role assignments** tab
3. Search for your user name
4. Verify "Cosmos DB Built-in Data Contributor" is listed

**Step 5: Login via Azure CLI (for local development)**
```bash
az login
```

**Step 6: Update appsettings.Development.json**
```json
"ManagedIdentity": {
  "Enabled": true
}
```

#### Available Cosmos DB Data Plane Roles

| Role | Description | Permissions |
|------|-------------|-------------|
| **Cosmos DB Built-in Data Contributor** | Full read/write access to data | Create, read, update, delete documents and containers |
| **Cosmos DB Built-in Data Reader** | Read-only access to data | Read documents and metadata only |

Choose the role based on your needs. For this demo (which creates documents), you need Data Contributor.

#### For Production: Using System-Assigned or User-Assigned Managed Identity

When deploying to Azure (App Service, Container Apps, Functions, AKS, VMs), follow these additional steps:

**Step 1: Enable Managed Identity on your Azure resource**
```bash
# For App Service
az webapp identity assign \
  --name <your-app-name> \
  --resource-group <your-rg>

# For Function App
az functionapp identity assign \
  --name <your-function-name> \
  --resource-group <your-rg>

# For Container App
az containerapp identity assign \
  --name <your-app-name> \
  --resource-group <your-rg>
```

**Step 2: Get the Managed Identity's Principal ID**
```bash
# For App Service
MANAGED_IDENTITY_ID=$(az webapp identity show \
  --name <your-app-name> \
  --resource-group <your-rg> \
  --query principalId -o tsv)

echo "Managed Identity Principal ID: $MANAGED_IDENTITY_ID"
```

**Step 3: Assign Cosmos DB role to the Managed Identity**
```bash
az cosmosdb sql role assignment create \
  --account-name <your-cosmos-account> \
  --resource-group <your-cosmos-rg> \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $MANAGED_IDENTITY_ID \
  --scope "/"
```

**Step 4: Verify**
```bash
az cosmosdb sql role assignment list \
  --account-name <your-cosmos-account> \
  --resource-group <your-cosmos-rg> \
  --query "[?principalId=='$MANAGED_IDENTITY_ID']" \
  --output table
```

#### Propagation Time

⏱️ **Important**: Role assignments can take **5-10 minutes** to propagate. If you get "Forbidden" errors immediately after assigning the role, wait a few minutes and try again.

#### Testing Your Configuration

After completing the setup, enable Managed Identity in your config:

```json
"ManagedIdentity": {
  "Enabled": true
}
```

Then run:
```bash
dotnet run
```

If configured correctly, you should see:
```
--- Testing: ManagedIdentity ---
Connecting to Cosmos DB using Managed Identity...
✓ Successfully connected using Managed Identity
```

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

This is the most common issue and usually indicates missing data plane role assignments.

**Quick Diagnostic Checklist:**

1. **Are you logged in to Azure CLI?**
   ```bash
   az account show
   ```
   If not logged in, run: `az login`

2. **Do you have the correct data plane role?**
   ```bash
   # Check your role assignments
   USER_ID=$(az ad signed-in-user show --query id -o tsv)
   az cosmosdb sql role assignment list \
     --account-name <your-cosmos-account> \
     --resource-group <your-rg> \
     --query "[?principalId=='$USER_ID']" \
     --output table
   ```
   
   You should see a role assignment with "Cosmos DB Built-in Data Contributor" or "Cosmos DB Built-in Data Reader".
   
   **If empty or missing**: You need to assign the data plane role (see Method 3 above)

3. **Common Mistakes:**
   - ❌ Having only subscription-level roles (Owner, Contributor) - These don't grant data access
   - ❌ Having "Cosmos DB Account Contributor" role - This is control plane only
   - ❌ Assigning roles but not waiting for propagation (5-10 minutes)
   - ❌ Using the wrong Cosmos DB account name or resource group
   - ✅ You need "Cosmos DB Built-in Data Contributor" assigned at the data plane level

4. **Verify your identity is correct:**
   ```bash
   # Check which account you're logged in as
   az account show --query user
   
   # If using managed identity in Azure, verify the principal ID matches
   az webapp identity show \
     --name <your-app> \
     --resource-group <your-rg> \
     --query principalId
   ```

5. **Check Cosmos DB firewall settings:**
   - Go to **Azure Portal** → Your Cosmos DB → **Settings** → **Networking**
   - Ensure "Selected networks" includes your IP or select "All networks" for testing
   - Or enable "Allow access from Azure Portal" to test from Azure resources

6. **Wait for role propagation:**
   After assigning a role, wait 5-10 minutes before testing again.

7. **Use verbose error output:**
   Enable detailed logging to see the exact error:
   ```bash
   export AZURE_LOG_LEVEL=verbose
   dotnet run
   ```

**Still not working?**

Try the nuclear option - assign the role again:
```bash
# Remove existing assignments (if any)
USER_ID=$(az ad signed-in-user show --query id -o tsv)
ASSIGNMENTS=$(az cosmosdb sql role assignment list \
  --account-name <your-cosmos-account> \
  --resource-group <your-rg> \
  --query "[?principalId=='$USER_ID'].id" -o tsv)

for assignment in $ASSIGNMENTS; do
  az cosmosdb sql role assignment delete \
    --account-name <your-cosmos-account> \
    --resource-group <your-rg> \
    --role-assignment-id $assignment
done

# Re-assign the role
az cosmosdb sql role assignment create \
  --account-name <your-cosmos-account> \
  --resource-group <your-rg> \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $USER_ID \
  --scope "/"

# Wait 10 minutes, then test again
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
