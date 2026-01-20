# Quick Configuration Guide

This guide will help you configure the Azure SQL Connector to test all connection methods.

## Step 1: Get Your SQL Server Details

From the Azure Portal, navigate to your SQL Database and note:
- **Server name**: Found under "Overview" → "Server name" (e.g., `spx-server.database.windows.net`)
- **Database name**: Your database name (e.g., `free-sql-db-9039009`)

## Step 2: Choose Your Authentication Method(s)

### Option A: Test All Methods (Recommended)
Set `ConnectionMethod` to `TestAll` and configure all three methods below.

### Option B: Test Individual Method
Set `ConnectionMethod` to one of: `ManagedIdentity`, `ConnectionString`, or `SqlAuthentication`

## Step 3: Configure Each Method

### Method 1: SQL Authentication (Easiest to start)
If your SQL Server has SQL authentication enabled:

1. Get your SQL admin username and password
2. Update appsettings.Development.json:
```json
"SqlAuthentication": {
  "Enabled": true,
  "Username": "your-sql-admin-username",
  "Password": "your-sql-admin-password"
}
```

### Method 2: Connection String
This is the same as SQL Authentication but uses a connection string format:

1. In Azure Portal: Your SQL Database → **Settings** → **Connection strings** → **ADO.NET (SQL authentication)**
2. Copy the connection string
3. Replace `{your_username}` and `{your_password}` with your actual credentials
4. Update appsettings.Development.json:
```json
"ConnectionString": {
  "Enabled": true,
  "Value": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;User Id=admin;Password=YourPass123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}
```

### Method 3: Managed Identity / Entra ID (Most Secure)
This requires additional setup:

1. **Enable Entra ID authentication** on your SQL Server (in Azure Portal)
2. **Set yourself as admin**:
   ```bash
   az sql server ad-admin create \
     --resource-group <your-rg> \
     --server-name <your-server> \
     --display-name <your-email@domain.com> \
     --object-id <your-user-object-id>
   ```
3. **Login via Azure CLI**:
   ```bash
   az login
   ```
4. **Grant database permissions** (connect using Azure Data Studio or SSMS with Entra ID auth):
   ```sql
   CREATE USER [your-email@domain.com] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [your-email@domain.com];
   ALTER ROLE db_datawriter ADD MEMBER [your-email@domain.com];
   ALTER ROLE db_ddladmin ADD MEMBER [your-email@domain.com];
   ```
5. Update appsettings.Development.json:
```json
"ManagedIdentity": {
  "Enabled": true
}
```

## Step 4: Update appsettings.Development.json

Full example for testing all methods:

```json
{
  "AzureSql": {
    "ServerName": "spx-server.database.windows.net",
    "DatabaseName": "free-sql-db-9039009",
    "TableName": "TestTable",
    
    "ConnectionMethod": "TestAll",
    
    "ManagedIdentity": {
      "Enabled": false
    },
    
    "ConnectionString": {
      "Enabled": true,
      "Value": "Server=tcp:spx-server.database.windows.net,1433;Initial Catalog=free-sql-db-9039009;User Id=sqladmin;Password=YourPassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    },
    
    "SqlAuthentication": {
      "Enabled": true,
      "Username": "sqladmin",
      "Password": "YourPassword123!"
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
=== Azure SQL Database Connector Demo ===

Server: spx-server.database.windows.net
Database: free-sql-db-9039009
Table: TestTable

Mode: Testing all connection methods

========================================================================

--- Testing: ManagedIdentity ---

⊘ ManagedIdentity is disabled in configuration. Skipping...

--- Testing: SqlAuthentication ---

Connecting to Azure SQL Database using SQL Authentication...
  Database: free-sql-db-9039009
  Version: Microsoft SQL Azure...
✓ Successfully connected using SQL Authentication

Checking if table 'TestTable' exists...
✓ Table 'TestTable' already exists

--- Creating Dummy Record ---
Creating dummy record with id: 12345678-1234-1234-1234-123456789012
✓ Dummy record created successfully!
  Record ID: 12345678-1234-1234-1234-123456789012
  Rows affected: 1

✓ SqlAuthentication - All operations completed successfully!

--- Testing: ConnectionString ---

Connecting to Azure SQL Database using Connection String...
...

========================================================================
SUMMARY
========================================================================

✓ Successful connections (2):
  • SqlAuthentication
  • ConnectionString

✗ Failed connections (1):
  • ManagedIdentity
    Reason: ManagedIdentity is disabled in configuration

Total: 2/3 connection methods succeeded
```

## Troubleshooting

### SQL Authentication fails
- Verify SQL authentication is enabled on your SQL Server
- Check username and password are correct
- Ensure firewall rules allow your IP address

### ManagedIdentity fails with "token" errors
- Run `az login` first
- Ensure you have Entra ID admin rights on the SQL Server
- Verify database user permissions were granted

### Connection timeout
- Check firewall rules in Azure Portal
- Verify you're not behind a restrictive corporate firewall
- Try adding your current IP to the SQL Server firewall rules

## Disable Methods You Don't Want to Test

Set `"Enabled": false` for any method you want to skip:

```json
"ManagedIdentity": {
  "Enabled": false
}
```

Or change `ConnectionMethod` from `TestAll` to a specific method:
```json
"ConnectionMethod": "SqlAuthentication"
```
