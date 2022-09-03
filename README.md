# Serilog.Sinks.MongoDB

![Build status](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/actions/workflows/deploy.yml/badge.svg)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.7.2, .NET Standard 2.0,, .NET Standard 2.1

### New in v5.x
* Output structured MongoDB Bson logs by switching to the .MongoDBBson() extensions. Existing the .MongoDB() extensions will continue to work converting logs to Json and then to Bson.
* Rolling Log Collection Naming (Thanks to [Revazashvili](https://github.com/Revazashvili) for the PR!). MongoDBBson sink only.
* Expire TTL support. MongoDBBson sink only.

### Configuration
In the examples below, the sink is writing to the database `logs` with structured Bson. The default collection name is `log`, but a custom collection can be supplied with the optional `CollectionName` parameter. The database and collection will be created if they do not exist.

```csharp
// use Bson structured logs
var log = new LoggerConfiguration()
    .WriteTo.MongoDBBson("mongodb://mymongodb/logs")
    .CreateLogger();

// capped collection using Bson structured logs
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
		cfg.SetRollingInternal(RollingInterval.Month);
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
