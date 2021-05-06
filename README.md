# Serilog.Sinks.MongoDB

[![Build status](https://ci.appveyor.com/api/projects/status/50a20wxfl1klrsra/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-mongodb/branch/master)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.6.2, .NETStandard 1.5

In the examples below, the sink is writing to the database `logs`. The default collection name is `log`, but a custom collection can be supplied with the optional `CollectionName` parameter. The database and collection will be created if they do not exist.

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
        cfg.SetCreateCappedCollection(100mb);
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
    })
    .CreateLogger();    
```
