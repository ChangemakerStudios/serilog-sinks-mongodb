namespace Serilog.Sinks.MongoDB.Tests;

[TestFixture]
public class LoggerConfigurationMongoDbExtensionsTests
{
    private static string MongoConnectionString => MongoTestFixture.ConnectionString;

    private const string MongoDatabaseName = "mongodb-sink";

    private const string MongoCollectionName = "test";

    private static IReadOnlyDictionary<string, string?> GetSerilogMongoConfiguration()
    {
        var databaseUrl = $"{MongoConnectionString}/{MongoDatabaseName}";

        return new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Args:databaseUrl"] = databaseUrl
        };
    }

    private static void TestCollectionAndDocumentExists(RollingInterval? rollingInterval = null)
    {
        var (mongoClient, mongoDatabase) = GetDatabase();
        var expectedCollectionName = rollingInterval is null
            ? MongoCollectionName
            : rollingInterval.Value.GetCollectionName(MongoCollectionName);

        const string Message = "some message logged into mongodb";

        using (var logger = new LoggerConfiguration()
                   .WriteTo.MongoDBBson(configuration =>
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
            .AddInMemoryCollection(GetSerilogMongoConfiguration())
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

    [Test]
    public void Create_Ttl_Index_From_Configuration_ExpireTtl_Flag()
    {
        var expireTtl = TimeSpan.FromMinutes(5);
        const string collectionName = "test-ttl-from-config";
        const string message = "ttl-index-from-config";

        var configurationValues = new Dictionary<string, string?>(GetSerilogMongoConfiguration())
        {
            ["Serilog:WriteTo:0:Args:collectionName"] = collectionName,
            ["Serilog:WriteTo:0:Args:rollingInterval"] = "Infinite",
            ["Serilog:WriteTo:0:Args:expireTtl"] = expireTtl.ToString("c")
        };

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .AddInMemoryCollection(configurationValues)
            .Build();

        using (var logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(configuration)
                   .CreateLogger())
        {
            logger.Information(message);
        }

        var (mongoClient, mongoDatabase) = GetDatabase();

        var mongoCollection = mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var indexes = mongoCollection.Indexes.List().ToList();

        var ttlIndex = indexes.SingleOrDefault(i =>
            i.Contains("name") && i["name"].AsString == "serilog_sink_expired_ttl");

        ttlIndex.Should().NotBeNull();
        ttlIndex!["expireAfterSeconds"].ToDouble().Should().Be(expireTtl.TotalSeconds);

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void Exclude_MessageTemplate_From_Configuration_Flag()
    {
        const string collectionName = "test-exclude-template-from-config";
        const string template = "Order {OrderId} processed";
        const int orderId = 42;

        var configurationValues = new Dictionary<string, string?>(GetSerilogMongoConfiguration())
        {
            ["Serilog:WriteTo:0:Args:collectionName"] = collectionName,
            ["Serilog:WriteTo:0:Args:rollingInterval"] = "Infinite",
            ["Serilog:WriteTo:0:Args:excludeMessageTemplate"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .AddInMemoryCollection(configurationValues)
            .Build();

        using (var logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(configuration)
                   .CreateLogger())
        {
            logger.Information(template, orderId);
        }

        var (mongoClient, mongoDatabase) = GetDatabase();
        var mongoCollection = mongoDatabase.GetCollection<BsonDocument>(collectionName);

        var document = mongoCollection.Find(new BsonDocument()).FirstOrDefault();

        document.Should().NotBeNull();
        document!.Contains("MessageTemplate").Should().BeFalse();
        document.Contains("RenderedMessage").Should().BeTrue();
        document["RenderedMessage"].AsString.Should().Be("Order 42 processed");

        mongoClient.DropDatabase(MongoDatabaseName);
    }
}