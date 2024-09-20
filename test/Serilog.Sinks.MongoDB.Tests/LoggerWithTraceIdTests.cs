using FluentAssertions;

using Microsoft.Extensions.Configuration;

using MongoDB.Driver;

using Serilog.Helpers;

namespace Serilog.Sinks.MongoDB.Tests;

using System.Diagnostics;
using NUnit.Framework;

[TestFixture]
public class LoggerWithTraceIdTests
{
    private const string MongoConnectionString = "mongodb://localhost:27017";

    private const string MongoDatabaseName = "mongodb-sink";

    private static (MongoClient, IMongoDatabase) GetDatabase()
    {
        var mongoClient = new MongoClient(MongoConnectionString);
        return (mongoClient, mongoClient.GetDatabase(MongoDatabaseName));
    }

    [Test]
    public void Log_Without_Activity_Should_Have_TraceId_And_SpanId_Null()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .Build();

        var collectionName = RollingInterval.Month.GetCollectionName("test");

        const string Message = "some message logged into mongodb without activity";

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
        var document = mongoCollection.Find(x => x.RenderedMessage == Message).FirstOrDefault();

        document.Should().NotBeNull();
        document.TraceId.Should().BeNull();
        document.SpanId.Should().BeNull();

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void Log_Within_Activity_Should_Have_TraceId_And_SpanId_Not_Null()
    {
        ActivityTraceId traceId;
        ActivitySpanId spanId;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .Build();

        var collectionName = RollingInterval.Month.GetCollectionName("test");

        const string Message = "some message logged into mongodb within an activity";

        using (var logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(configuration)
                   .CreateLogger())
        {
            var activitySource = new ActivitySource("Serilog.Sinks.MongoDB.Tests");
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(activityListener);
            using (var activity = activitySource.StartActivity("LogTest"))
            {
                traceId = activity.TraceId;
                spanId = activity.SpanId;
                logger.Information(Message);
            }
        }

        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionExists = mongoDatabase.CollectionExists(collectionName);

        collectionExists.Should().BeTrue();

        var mongoCollection = mongoDatabase.GetCollection<LogEntry>(collectionName);
        var document = mongoCollection.Find(x => x.RenderedMessage == Message).FirstOrDefault();

        document.Should().NotBeNull();
        document.TraceId.Should().Be(traceId.ToString());
        document.SpanId.Should().Be(spanId.ToString());

        mongoClient.DropDatabase(MongoDatabaseName);
    }
}