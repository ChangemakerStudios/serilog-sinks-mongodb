using System;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog.Helpers;
using Xunit;

namespace Serilog.Sinks.MongoDB.Tests
{
    public class LoggerConfigurationMongoDbExtensionsTests
    {
        private const string MongoConnectionString = "mongodb://localhost:27017";
        private const string MongoDatabaseName = "mongodb-sink";
        private const string MongoCollectionName = "test";
        
        [Fact]
        public void Create_Collection_Based_Without_Rolling_Interval()
        {
            TestCollectionAndDocumentExists();
        }
        
        [Fact]
        public void Create_Collection_Based_On_Rolling_Interval_Infinite()
        {
            TestCollectionAndDocumentExists(RollingInterval.Infinite);
        }
        
        [Fact]
        public void Create_Collection_Based_On_Rolling_Interval_Minute()
        {
            TestCollectionAndDocumentExists(RollingInterval.Minute);
        }

        [Fact]
        public void Create_Collection_With_Rolling_Interval_From_Configuration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("serilog.json")
                .Build();

            var now = DateTime.Now;
            var collectionName = $"test{now.Year.ToString()}{now.Month.ToString()}";
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            
            const string message = "some message logged into mongodb";
            Log.Logger.Information(message);

            Log.CloseAndFlush();
            
            var (mongoClient,mongoDatabase) = GetDatabase();
            var collectionExists = mongoDatabase.CollectionExists(collectionName);
            Assert.True(collectionExists);

            var mongoCollection = mongoDatabase.GetCollection<LogEntryModel>(collectionName);
            var existsDocument = mongoCollection.Find(x => x.RenderedMessage == message).Any();
            
            Assert.True(existsDocument);

            mongoClient.DropDatabase(MongoDatabaseName);
            
        }

        private static void TestCollectionAndDocumentExists(RollingInterval? rollingInterval = null)
        {
            var (mongoClient,mongoDatabase) = GetDatabase();
            var expectedCollectionName = rollingInterval is null ? MongoCollectionName : rollingInterval.Value.GetCollectionName(MongoCollectionName);
            Log.Logger = new LoggerConfiguration()
                .WriteTo.MongoDBBson(configuration =>
                {
                    configuration.SetMongoDatabase(mongoDatabase);
                    if (rollingInterval is not null)
                        configuration.SetRollingInternal(rollingInterval.Value);
                    configuration.SetCollectionName(MongoCollectionName);
                }).CreateLogger();

            const string message = "some message logged into mongodb";
            Log.Logger.Information(message);

            Log.CloseAndFlush();

            var collectionExists = mongoDatabase.CollectionExists(expectedCollectionName);
            Assert.True(collectionExists);

            var mongoCollection = mongoDatabase.GetCollection<LogEntryModel>(expectedCollectionName);
            var existsDocument = mongoCollection.Find(x => x.RenderedMessage == message).Any();
            
            Assert.True(existsDocument);

            mongoClient.DropDatabase(MongoDatabaseName);
        }

        private static (MongoClient,IMongoDatabase) GetDatabase()
        {
            var mongoClient = new MongoClient(MongoConnectionString);
            return (mongoClient,mongoClient.GetDatabase(MongoDatabaseName));
        }
    }
}