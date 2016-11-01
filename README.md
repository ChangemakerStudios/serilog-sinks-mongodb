# Serilog.Sinks.MongoDB

[![Build status](https://ci.appveyor.com/api/projects/status/50a20wxfl1klrsra/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-mongodb/branch/master)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.6, .NETStandard 1.5


In the example shown, the sink will write to the database `logs`. The default collection name is `log`, but a custom collection can be supplied with the optional `CollectionName` parameter.
The database and collection will be created if they do not exist.

```csharp
// basic usage defaults to writing to `log` collection
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://mymongodb/logs")
    .CreateLogger();

// creates custom collection `applog`
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://mymongodb/logs", collectionName: "applog")
    .CreateLogger();
```

Additionally, you can utilize a [Capped Collection](https://docs.mongodb.org/manual/core/capped-collections/).

```csharp
// basic with custom collection name
var log = new LoggerConfiguration()
    .WriteTo.MongoDBCapped("mongodb://mymongodb/logs", collectionName: "rollingapplog")
    .CreateLogger();

// optional parameters cappedMaxSizeMb and cappedMaxDocuments specified
var log = new LoggerConfiguration()
    .WriteTo.MongoDBCapped("mongodb://mymongodb/logs", cappedMaxSizeMb: 50, cappedMaxDocuments: 1000)
    .CreateLogger();
```
