# Serilog.Sinks.MongoDB

[![Build status](https://ci.appveyor.com/api/projects/status/50a20wxfl1klrsra/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-mongodb/branch/master)

A Serilog sink that writes events as documents to [MongoDB](http://mongodb.org).

**Package** - [Serilog.Sinks.MongoDB](http://nuget.org/packages/serilog.sinks.mongodb)
| **Platforms** - .NET 4.5

You'll need to create a collection on your MongoDB server. In the example shown, it is called `log`.

```csharp
var log = new LoggerConfiguration()
    .WriteTo.MongoDB("mongo://mymongodb/log")
    .CreateLogger();
```
