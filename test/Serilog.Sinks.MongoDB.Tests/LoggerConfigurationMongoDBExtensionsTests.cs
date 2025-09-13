using FluentAssertions;

using Microsoft.Extensions.Configuration;

using MongoDB.Driver;

using Serilog.Helpers;

namespace Serilog.Sinks.MongoDB.Tests;

using NUnit.Framework;

[TestFixture]
public class LoggerConfigurationMongoDbExtensionsTests
{
    private const string MongoConnectionString = "mongodb://localhost:27017";

    private const string MongoDatabaseName = "mongodb-sink";

    private const string MongoCollectionName = "test";

    private static void TestCollectionAndDocumentExists(RollingInterval? rollingInterval = null)
    {
        var (mongoClient, mongoDatabase) = GetDatabase();
        var expectedCollectionName = rollingInterval is null
            ? MongoCollectionName
            : rollingInterval.Value.GetCollectionName(MongoCollectionName);

        const string Message = "some message logged into mongodb";

        using (var logger = new LoggerConfiguration()
                   .WriteTo.MongoDBBson(
                       configuration =>
                       {
                           configuration.SetMongoDatabase(mongoDatabase);
                           if (rollingInterval is not null)
                               configuration.SetRollingInterval(rollingInterval.Value);
                           configuration.SetCollectionName(MongoCollectionName);
                       }).CreateLogger())
        {
            logger.Information(Message);
        }

        var collectionExists = mongoDatabase.CollectionExists(expectedCollectionName);

        collectionExists.Should().BeTrue();

        var mongoCollection = mongoDatabase.GetCollection<LogEntry>(expectedCollectionName);
        var existsDocument = mongoCollection.Find(x => x.RenderedMessage == Message).Any();

        existsDocument.Should().BeTrue("Rendered Message Should Exist");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    private static (MongoClient, IMongoDatabase) GetDatabase()
    {
        var mongoClient = new MongoClient(MongoConnectionString);
        return (mongoClient, mongoClient.GetDatabase(MongoDatabaseName));
    }

    [Test]
    public void Create_Collection_Based_On_Rolling_Interval_Infinite()
    {
        TestCollectionAndDocumentExists(RollingInterval.Infinite);
    }

    [Test]
    public void Create_Collection_Based_On_Rolling_Interval_Minute()
    {
        TestCollectionAndDocumentExists(RollingInterval.Minute);
    }

    [Test]
    public void Create_Collection_Based_Without_Rolling_Interval()
    {
        TestCollectionAndDocumentExists();
    }

    [Test]
    public void Create_Collection_With_Rolling_Interval_From_Configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .Build();

        var collectionName = RollingInterval.Month.GetCollectionName("test");

        const string Message = "some message logged into mongodb";

        using (var logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(configuration)
                   .CreateLogger())
        {
            logger.Information(Message);
        }

        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionExists = mongoDatabase.CollectionExists(collectionName);

        collectionExists.Should().BeTrue();

        var mongoCollection = mongoDatabase.GetCollection<LogEntry>(collectionName);
        var existsDocument = mongoCollection.Find(x => x.RenderedMessage == Message).Any();

        existsDocument.Should().BeTrue();

        mongoClient.DropDatabase(MongoDatabaseName);
    }
}