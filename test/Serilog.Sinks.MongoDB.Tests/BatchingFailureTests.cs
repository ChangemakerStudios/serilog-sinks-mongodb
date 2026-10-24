using System.Collections.Concurrent;

using Serilog.Core;
using Serilog.Sinks.MongoDB;

namespace Serilog.Sinks.MongoDB.Tests;

[TestFixture]
public class BatchingFailureTests
{
    [Test]
    public void FallibleListener_ReceivesBatchFailures()
    {
        var listener = new RecordingFailureListener();

        using (var logger = new LoggerConfiguration()
                   .WriteTo.Fallible(
                       w => w.MongoDBBson(
                           "mongodb://127.0.0.1:1/serilog?serverSelectionTimeoutMS=100&connectTimeoutMS=100"),
                       listener)
                   .CreateLogger())
        {
            logger.Information("never written");
        }

        listener.Failures.Should().NotBeEmpty(
            "the sink must accept a failure listener instead of always reporting through SelfLog");
    }

    [Test]
    public void FailedDatabaseVerification_IsRetriedOnNextBatch()
    {
        var calls = 0;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        cursor.Current.Returns([new BsonDocument("name", MongoDBSinkDefaults.CollectionName)]);

        var database = Substitute.For<IMongoDatabase>();
        database.ListCollections(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1 ? throw new TimeoutException("selecting a server") : cursor);

        var configuration = new MongoDBSinkConfiguration();
        configuration.SetMongoDatabase(database);

        var sink = new MongoDBSink(configuration);

        var first = () => sink.GetCollection<LogEntry>();
        first.Should().Throw<TimeoutException>();

        var second = () => sink.GetCollection<LogEntry>();
        second.Should().NotThrow("a transient failure must not be cached for the life of the sink");
    }

    private sealed class RecordingFailureListener : ILoggingFailureListener
    {
        public ConcurrentQueue<LoggingFailureKind> Failures { get; } = new();

        public void OnLoggingFailed(
            object sender,
            LoggingFailureKind kind,
            string message,
            IReadOnlyCollection<LogEvent>? events,
            Exception? exception)
        {
            this.Failures.Enqueue(kind);
        }
    }
}
