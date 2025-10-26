# ![mongo icon](https://raw.githubusercontent.com/ChangemakerStudios/serilog-sinks-mongodb/dev/assets/mongo-icon.png) Serilog.Sinks.MongoDB

[![NuGet version](https://badge.fury.io/nu/Serilog.Sinks.MongoDB.svg)](https://badge.fury.io/nu/Serilog.Sinks.MongoDB)
[![Downloads](https://img.shields.io/nuget/dt/Serilog.Sinks.MongoDB.svg?logo=nuget&color=purple)](https://www.nuget.org/packages/Serilog.Sinks.MongoDB) 
[![Build status](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/actions/workflows/deploy.yml/badge.svg)](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/actions) 

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
**Platforms** - .NET 4.7.2, .NET 6.0+, .NET Standard 2.1

## Whats New

See [CHANGES.md](CHANGES.md) for complete version history.

#### New in v7.2
* Fixed MongoDB v7.x compatibility - Error handling now uses error codes instead of string matching (#98, #99)
* Added comprehensive unit tests for MongoDB error handling
* Improved CI performance

## Installation
Install the sink via NuGet Package Manager Console:

```powershell
Install-Package Serilog.Sinks.MongoDB
```

or via the .NET CLI:

```bash
dotnet add package Serilog.Sinks.MongoDB
```

## Usage Examples
In the examples below, the sink is writing to the database `logs` with structured Bson. The default collection name is `log`, but a custom collection can be supplied with the optional `CollectionName` parameter. The database and collection will be created if they do not exist.

### Basic:
```csharp
using Serilog;

// use BSON structured logs
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson("mongodb://mymongodb/logs")
    .CreateLogger();

log.Information("This is a test log message");
```

### Capped Collection:

```csharp
// capped collection using BSON structured logs
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson("mongodb://mymongodb/logs", cfg =>
    {
        // optional configuration options:
        cfg.SetCollectionName("log");
        cfg.SetBatchPeriod(TimeSpan.FromSeconds(1));

        // create capped collection that is max 100mb
        cfg.SetCreateCappedCollection(100);
    })
    .CreateLogger();
```

### Custom Mongodb Settings:
```csharp
// create sink instance with custom mongodb settings.
var log = new LoggerConfiguration()
	.WriteTo.MongoDBBson(cfg =>
    {
		// custom MongoDb configuration
		var mongoDbSettings = new MongoClientSettings
		{
			UseTls = true,			
			AllowInsecureTls = true,
			Credential = MongoCredential.CreateCredential("databaseName", "username", "password"),
			Server = new MongoServerAddress("127.0.0.1")
		};

		var mongoDbInstance = new MongoClient(mongoDbSettings).GetDatabase("serilog");
		
		// sink will use the IMongoDatabase instance provided
		cfg.SetMongoDatabase(mongoDbInstance);
		cfg.SetRollingInterval(RollingInterval.Month);
    })
	.CreateLogger();
```
### JSON (_Microsoft.Extensions.Configuration_)

Keys and values are not case-sensitive. This is an example of configuring the MongoDB sink arguments from _Appsettings.json_:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Error",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
      	"Name": "MongoDBBson",
        "Args": {
            "databaseUrl": "mongodb://username:password@ip:port/dbName?authSource=admin",
            "collectionName": "logs",
            "cappedMaxSizeMb": "1024",
            "cappedMaxDocuments": "50000",
            "rollingInterval": "Month"
        }
      }
    ]
  }
}
```

## Advanced Configuration

### Authentication & Secure Connections

For password-protected MongoDB instances, Azure Cosmos DB, or SSL/TLS connections:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(cfg =>
    {
        var mongoDbSettings = new MongoClientSettings
        {
            UseTls = true,
            AllowInsecureTls = false, // set true only for dev/testing
            Credential = MongoCredential.CreateCredential("databaseName", "username", "password"),
            Server = new MongoServerAddress("your-server.com", 27017)
        };

        var mongoDbInstance = new MongoClient(mongoDbSettings).GetDatabase("logs");
        cfg.SetMongoDatabase(mongoDbInstance);
    })
    .CreateLogger();
```

**Azure Cosmos DB** (MongoDB API):
```csharp
var connectionString = "mongodb://cosmosdb-account:key@cosmosdb-account.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false";
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(connectionString)
    .CreateLogger();
```

### TTL / Auto-Expiration

Automatically delete old logs using MongoDB's TTL feature:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(cfg =>
    {
        cfg.SetMongoUrl("mongodb://localhost/logs");
        cfg.SetExpireTTL(TimeSpan.FromDays(30)); // logs expire after 30 days
    })
    .CreateLogger();
```

### Exclude Redundant Fields

Reduce storage costs by excluding the `MessageTemplate` field (the rendered message is still stored):

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(cfg =>
    {
        cfg.SetMongoUrl("mongodb://localhost/logs");
        cfg.SetExcludeMessageTemplate(true); // saves storage space
    })
    .CreateLogger();
```

### Rolling Collections

Create time-based collections (e.g., one per day/month):

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson(cfg =>
    {
        cfg.SetMongoUrl("mongodb://localhost/logs");
        cfg.SetCollectionName("log");
        cfg.SetRollingInterval(RollingInterval.Day); // creates: log-20251004, log-20251005, etc.
    })
    .CreateLogger();
```

**Collection naming patterns:**
- `RollingInterval.Day` → `log-yyyyMMdd` (e.g., `log-20251004`)
- `RollingInterval.Month` → `log-yyyyMM` (e.g., `log-202510`)
- `RollingInterval.Year` → `log-yyyy` (e.g., `log-2025`)

**Querying rolling collections:**
```csharp
// Query specific date range - you need to target the correct collection(s)
var collectionName = $"log-{DateTime.UtcNow:yyyyMMdd}";
var collection = database.GetCollection<BsonDocument>(collectionName);
var todayLogs = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
```

## Migration Guide

### From Legacy `.MongoDB()` to `.MongoDBBson()`

The legacy `.MongoDB()` sink converts logs to JSON then to BSON. The newer `.MongoDBBson()` writes structured BSON directly for better performance and features.

**Legacy (still supported):**
```csharp
.WriteTo.MongoDB("mongodb://localhost/logs") // converts to JSON first
```

**New Bson sink (recommended):**
```csharp
.WriteTo.MongoDBBson("mongodb://localhost/logs") // native BSON
```

**MongoDBBson exclusive features:**
- TTL/Expiration (`SetExpireTTL`)
- Exclude message template (`SetExcludeMessageTemplate`)
- Rolling collections (`SetRollingInterval`)
- Better type mapping and performance

## Troubleshooting

### MongoDB Connection Issues
**Problem:** Application hangs or fails when MongoDB is unavailable
**Solution:** Ensure your MongoDB connection string includes appropriate timeouts:
```csharp
"mongodb://localhost/logs?connectTimeoutMS=3000&serverSelectionTimeoutMS=3000"
```

### Type Mapping Errors
**Problem:** `System.Guid cannot be mapped to BsonValue`
**Solution:** Ensure you're using the latest version (v7.1+) which includes Guid mapping fixes.

### Capped Collections Not Created
**Problem:** Capped collection configuration not working
**Solution:** Ensure the collection doesn't already exist. Drop existing collection first:
```csharp
database.DropCollection("logs");
// Then configure with capped settings
```

### Rolling Collections Not Named Correctly
**Problem:** Collection created without time format suffix
**Solution:** Ensure you're using `MongoDBBson` sink (not legacy `MongoDB`) and v7.1+.

## Icon

[MongoDB](https://icons8.com/icon/74402/mongodb) icon by [Icons8](https://icons8.com)
