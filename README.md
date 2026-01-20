# Azure Database Demo

A collection of demo applications showcasing various authentication methods for connecting to Azure database services.

## Projects

### [CosmosDBConnector](CosmosDBConnector)
Demonstrates connecting to **Azure Cosmos DB NoSQL** using multiple authentication methods:
- Managed Identity (recommended for production)
- Connection String
- Account Key
- Local Cosmos DB Emulator

**Features:**
- Multiple authentication options
- Database and container management
- Basic CRUD operations
- Partition key configuration

[View CosmosDBConnector README →](CosmosDBConnector/README.md)

### [AzureSQLConnector](AzureSQLConnector)
Demonstrates connecting to **Azure SQL Database** using multiple authentication methods:
- Managed Identity with Entra ID (recommended for production)
- Connection String
- SQL Authentication (username/password)

**Features:**
- Multiple authentication options
- Table creation and management
- Basic CRUD operations
- Connection retry logic

[View AzureSQLConnector README →](AzureSQLConnector/README.md)

## Prerequisites

- .NET 8.0 SDK
- Azure subscription with:
  - Azure Cosmos DB account (for CosmosDBConnector)
  - Azure SQL Database (for AzureSQLConnector)

## Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd azure-db-demo
   ```

2. **Choose a project** and navigate to its directory:
   ```bash
   cd CosmosDBConnector  # or AzureSQLConnector
   ```

3. **Configure settings** in `appsettings.Development.json`:
   - Copy the template values
   - Replace with your Azure resource details
   - See each project's README for detailed configuration instructions

4. **Run the application**:
   ```bash
   dotnet run
   ```

## Solution Structure

```
azure-db-demo/
├── azure-db-demo.sln              # Visual Studio solution file
├── CosmosDBConnector/             # Azure Cosmos DB demo project
│   ├── Program.cs
│   ├── CosmosDbConnector.cs
│   ├── appsettings.Development.json
│   └── README.md
└── AzureSQLConnector/             # Azure SQL Database demo project
    ├── Program.cs
    ├── AzureSqlConnector.cs
    ├── appsettings.Development.json
    └── README.md
```

## Common Features

Both projects demonstrate:
- ✅ **Managed Identity authentication** (recommended for production)
- ✅ **Multiple connection methods** for different scenarios
- ✅ **Proper error handling** and connection validation
- ✅ **Azure best practices** for authentication and security
- ✅ **Development-friendly** configuration with local fallbacks

## Security Best Practices

1. **Never commit secrets** - Add `appsettings.Development.json` to `.gitignore` if it contains credentials
2. **Use Managed Identity in production** - Eliminates credential management
3. **Store secrets in Azure Key Vault** - For production environments
4. **Rotate credentials regularly** - If using connection strings or keys
5. **Enable encryption** - Always use encrypted connections

## Building the Solution

To build all projects:
```bash
dotnet build azure-db-demo.sln
```

To run tests (if added):
```bash
dotnet test azure-db-demo.sln
```

## Contributing

When adding new database connector projects:
1. Follow the existing project structure
2. Include a detailed README with configuration instructions
3. Implement Managed Identity as the recommended authentication method
4. Add comprehensive error handling and logging
5. Update this top-level README

## License

[Your License Here]
