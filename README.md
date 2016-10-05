# Serilog.Sinks.MongoDB

[![Build status](https://ci.appveyor.com/api/projects/status/50a20wxfl1klrsra/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-mongodb/branch/master)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.6


In the example shown, the database in use is called `logs`. The default collection name is `log`, but a custom name can be supplied with the optional `CollectionName` parameter.

```csharp
// basic
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://mymongodb/logs")
    .CreateLogger();

// custom collection name
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://mymongodb/logs", collectionName: "myapplogs")
    .CreateLogger();
```

Additionally, you can utilize a [Capped Collection](https://docs.mongodb.org/manual/core/capped-collections/) which creates a special "rolling" collection in MongoDB.

```csharp
// basic
var log = new LoggerConfiguration()
    .WriteTo.MongoDBCapped("mongodb://mymongodb/logs")
    .CreateLogger();

// optional parameters cappedMaxSizeMb and cappedMaxDocuments specified
var log = new LoggerConfiguration()
    .WriteTo.MongoDBCapped("mongodb://mymongodb/logs", cappedMaxSizeMb: 100, cappedMaxDocuments: 1000)
    .CreateLogger();
```
