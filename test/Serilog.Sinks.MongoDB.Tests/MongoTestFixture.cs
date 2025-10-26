namespace Serilog.Sinks.MongoDB.Tests;

[SetUpFixture]
public class MongoTestFixture
{
    private static IMongoRunner? _mongoRunner;

    public static string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Start ephemeral MongoDB instance
        var options = new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = false
        };

        _mongoRunner = MongoRunner.Run(options);
        ConnectionString = _mongoRunner.ConnectionString;

        Console.WriteLine($"Ephemeral MongoDB started at: {ConnectionString}");

        // Register BSON serializers
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        _mongoRunner?.Dispose();
        Console.WriteLine("Ephemeral MongoDB instance stopped");
    }
}
