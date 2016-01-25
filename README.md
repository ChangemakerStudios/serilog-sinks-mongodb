# Serilog.Sinks.MongoDB

[![Build status](https://ci.appveyor.com/api/projects/status/50a20wxfl1klrsra/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-mongodb/branch/master)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.5

You'll need to create a collection on your MongoDB server. In the example shown, the database in use is called `logs`. The collection name is `log` and is created implicitely.

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongodb://mymongodb/logs")
    .CreateLogger();
```

Additionally you can utilize a [Capped Collection](https://docs.mongodb.org/manual/core/capped-collections/). This type allows explicit collection naming.

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDBCapped("mongodb://mymongodb/logs", collectionName: "customCollectionName")
    .CreateLogger();
```
