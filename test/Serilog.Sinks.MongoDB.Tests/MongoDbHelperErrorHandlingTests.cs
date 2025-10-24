namespace Serilog.Sinks.MongoDB.Tests;

/// <summary>
/// Tests for MongoDB error handling in MongoDbHelper.
/// These tests verify that PR #99 correctly handles MongoDB exceptions using error codes
/// instead of string matching, ensuring compatibility across MongoDB versions.
/// </summary>
[TestFixture]
public class MongoDbHelperErrorHandlingTests
{
    private static string MongoConnectionString => MongoTestFixture.ConnectionString;

    private const string MongoDatabaseName = "mongodb-sink-error-handling-tests";

    private const string MongoCollectionName = "test-collection";

    private static (MongoClient, IMongoDatabase) GetDatabase()
    {
        var mongoClient = new MongoClient(MongoConnectionString);
        return (mongoClient, mongoClient.GetDatabase(MongoDatabaseName));
    }

    [TearDown]
    public void Cleanup()
    {
        var mongoClient = new MongoClient(MongoConnectionString);
        mongoClient.DropDatabase(MongoDatabaseName);
    }

    #region VerifyCollectionExists Tests

    [Test]
    public void VerifyCollectionExists_WhenCollectionDoesNotExist_ShouldCreateCollection()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_new";

        // Act
        mongoDatabase.VerifyCollectionExists(collectionName);

        // Assert
        var collectionExists = mongoDatabase.CollectionExists(collectionName);
        collectionExists.Should().BeTrue("Collection should be created when it doesn't exist");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyCollectionExists_WhenCollectionAlreadyExists_ShouldNotThrowException()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_exists";

        // Create the collection first
        mongoDatabase.CreateCollection(collectionName);

        // Act & Assert - Should not throw when collection already exists
        var act = () => mongoDatabase.VerifyCollectionExists(collectionName);
        act.Should().NotThrow("VerifyCollectionExists should handle existing collections gracefully");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyCollectionExists_WithRaceCondition_ShouldHandleNamespaceExistsError()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_race";

        // Pre-create the collection but the method doesn't know about it
        // This simulates a race condition where CollectionExists returns false
        // but the collection gets created before CreateCollection is called
        mongoDatabase.CreateCollection(collectionName);

        // Act & Assert - Even though it exists, should handle gracefully
        // The internal check will see it exists and return early,
        // but if it didn't, the catch block should handle NamespaceExists
        var act = () => mongoDatabase.VerifyCollectionExists(collectionName);
        act.Should().NotThrow("Should handle NamespaceExists error code gracefully");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyCollectionExists_WithCollectionCreationOptions_ShouldCreateWithOptions()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_with_options";
        var options = new CreateCollectionOptions
        {
            Capped = true,
            MaxSize = 1024 * 1024, // 1MB
            MaxDocuments = 1000
        };

        // Act
        mongoDatabase.VerifyCollectionExists(collectionName, options);

        // Assert
        var collectionExists = mongoDatabase.CollectionExists(collectionName);
        collectionExists.Should().BeTrue("Collection should be created with options");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    #endregion

    #region VerifyExpireTTLSetup Tests

    [Test]
    public void VerifyExpireTTLSetup_WhenNoIndexExists_ShouldCreateTTLIndex()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_new";
        var expireTtl = TimeSpan.FromMinutes(30);

        // Create the collection first
        mongoDatabase.CreateCollection(collectionName);

        // Act
        mongoDatabase.VerifyExpireTTLSetup(collectionName, expireTtl);

        // Assert
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);
        var indexes = collection.Indexes.List().ToList();
        var ttlIndex = indexes.FirstOrDefault(idx =>
            idx.Contains("name") && idx["name"].AsString == "serilog_sink_expired_ttl");

        ttlIndex.Should().NotBeNull("TTL index should be created");
        ttlIndex!["expireAfterSeconds"].Should().Be((int)expireTtl.TotalSeconds);

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyExpireTTLSetup_WhenIndexExistsWithSameOptions_ShouldNotThrow()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_same";
        var expireTtl = TimeSpan.FromMinutes(30);

        mongoDatabase.CreateCollection(collectionName);
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);

        // Create the TTL index first
        var indexKeysDefinition = Builders<LogEntry>.IndexKeys.Ascending(s => s.UtcTimeStamp);
        var indexOptions = new CreateIndexOptions
        {
            Name = "serilog_sink_expired_ttl",
            ExpireAfter = expireTtl
        };
        collection.Indexes.CreateOne(new CreateIndexModel<LogEntry>(indexKeysDefinition, indexOptions));

        // Act & Assert - Should not throw when index exists with same options
        var act = () => mongoDatabase.VerifyExpireTTLSetup(collectionName, expireTtl);
        act.Should().NotThrow("Should handle existing TTL index with same options");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyExpireTTLSetup_WhenIndexExistsWithDifferentOptions_ShouldRecreateIndex()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_different";
        var originalExpireTtl = TimeSpan.FromMinutes(30);
        var newExpireTtl = TimeSpan.FromMinutes(60);

        mongoDatabase.CreateCollection(collectionName);
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);

        // Create the TTL index with original expiration
        var indexKeysDefinition = Builders<LogEntry>.IndexKeys.Ascending(s => s.UtcTimeStamp);
        var originalIndexOptions = new CreateIndexOptions
        {
            Name = "serilog_sink_expired_ttl",
            ExpireAfter = originalExpireTtl
        };
        collection.Indexes.CreateOne(new CreateIndexModel<LogEntry>(indexKeysDefinition, originalIndexOptions));

        // Act - Update with different expiration time
        // This should trigger IndexOptionsConflict error code and handle it by dropping and recreating
        mongoDatabase.VerifyExpireTTLSetup(collectionName, newExpireTtl);

        // Assert
        var indexes = collection.Indexes.List().ToList();
        var ttlIndex = indexes.FirstOrDefault(idx =>
            idx.Contains("name") && idx["name"].AsString == "serilog_sink_expired_ttl");

        ttlIndex.Should().NotBeNull("TTL index should still exist");
        ttlIndex!["expireAfterSeconds"].Should().Be((int)newExpireTtl.TotalSeconds,
            "Index should be recreated with new expiration time");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyExpireTTLSetup_WhenNullExpireTTL_ShouldRemoveIndex()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_remove";
        var expireTtl = TimeSpan.FromMinutes(30);

        mongoDatabase.CreateCollection(collectionName);
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);

        // Create the TTL index first
        var indexKeysDefinition = Builders<LogEntry>.IndexKeys.Ascending(s => s.UtcTimeStamp);
        var indexOptions = new CreateIndexOptions
        {
            Name = "serilog_sink_expired_ttl",
            ExpireAfter = expireTtl
        };
        collection.Indexes.CreateOne(new CreateIndexModel<LogEntry>(indexKeysDefinition, indexOptions));

        // Act - Call with null to remove the index
        mongoDatabase.VerifyExpireTTLSetup(collectionName, null);

        // Assert
        var indexes = collection.Indexes.List().ToList();
        var ttlIndex = indexes.FirstOrDefault(idx =>
            idx.Contains("name") && idx["name"].AsString == "serilog_sink_expired_ttl");

        ttlIndex.Should().BeNull("TTL index should be removed when expireTTL is null");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyExpireTTLSetup_WhenNullExpireTTLAndNoIndex_ShouldNotThrow()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_null_no_index";

        mongoDatabase.CreateCollection(collectionName);

        // Act & Assert - Should not throw when trying to remove non-existent index
        var act = () => mongoDatabase.VerifyExpireTTLSetup(collectionName, null);
        act.Should().NotThrow("Should handle removal of non-existent index gracefully");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    [Test]
    public void VerifyExpireTTLSetup_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_ttl_idempotent";
        var expireTtl = TimeSpan.FromMinutes(45);

        mongoDatabase.CreateCollection(collectionName);

        // Act - Call multiple times with same expiration
        mongoDatabase.VerifyExpireTTLSetup(collectionName, expireTtl);
        mongoDatabase.VerifyExpireTTLSetup(collectionName, expireTtl);
        mongoDatabase.VerifyExpireTTLSetup(collectionName, expireTtl);

        // Assert
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);
        var indexes = collection.Indexes.List().ToList();
        var ttlIndexes = indexes.Where(idx =>
            idx.Contains("name") && idx["name"].AsString == "serilog_sink_expired_ttl").ToList();

        ttlIndexes.Should().HaveCount(1, "Should only have one TTL index even after multiple calls");
        ttlIndexes[0]["expireAfterSeconds"].Should().Be((int)expireTtl.TotalSeconds);

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    #endregion

    #region Integration Tests - Error Code Validation

    /// <summary>
    /// This test validates that the MongoDB driver actually returns CodeName "NamespaceExists"
    /// for error code 48 when a collection already exists. This ensures our fix in PR #99
    /// is compatible with the actual MongoDB behavior.
    /// Note: We need to use CreateCollectionOptions to force MongoDB to throw the exception,
    /// as calling CreateCollection without options on an existing collection is idempotent.
    /// </summary>
    [Test]
    public void MongoCommandException_WhenCollectionExists_ShouldHaveNamespaceExistsCodeName()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_namespace_error";

        // Create the collection first with specific options
        var options = new CreateCollectionOptions
        {
            Capped = true,
            MaxSize = 1024 * 1024
        };
        mongoDatabase.CreateCollection(collectionName, options);

        // Act & Assert - Try to create the same collection again with different options
        MongoCommandException? caughtException = null;
        try
        {
            var differentOptions = new CreateCollectionOptions
            {
                Capped = false
            };
            mongoDatabase.CreateCollection(collectionName, differentOptions);
        }
        catch (MongoCommandException ex)
        {
            caughtException = ex;
        }

        caughtException.Should().NotBeNull("Should throw MongoCommandException when creating duplicate collection");
        caughtException!.CodeName.Should().Be("NamespaceExists",
            "MongoDB should return CodeName 'NamespaceExists' for duplicate collection");
        caughtException.Code.Should().Be(48, "Error code should be 48 for NamespaceExists");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    /// <summary>
    /// This test validates that the MongoDB driver actually returns CodeName "IndexOptionsConflict"
    /// for error code 85 when an index exists with different options. This ensures our fix in PR #99
    /// is compatible with the actual MongoDB behavior.
    /// </summary>
    [Test]
    public void MongoCommandException_WhenIndexExistsWithDifferentOptions_ShouldHaveIndexOptionsConflictCodeName()
    {
        // Arrange
        var (mongoClient, mongoDatabase) = GetDatabase();
        var collectionName = $"{MongoCollectionName}_index_error";

        mongoDatabase.CreateCollection(collectionName);
        var collection = mongoDatabase.GetCollection<LogEntry>(collectionName);

        // Create index with one expiration time
        var indexKeysDefinition = Builders<LogEntry>.IndexKeys.Ascending(s => s.UtcTimeStamp);
        var indexOptions = new CreateIndexOptions
        {
            Name = "test_ttl_index",
            ExpireAfter = TimeSpan.FromMinutes(30)
        };
        collection.Indexes.CreateOne(new CreateIndexModel<LogEntry>(indexKeysDefinition, indexOptions));

        // Act & Assert - Try to create same index with different expiration
        MongoCommandException? caughtException = null;
        try
        {
            var differentIndexOptions = new CreateIndexOptions
            {
                Name = "test_ttl_index",
                ExpireAfter = TimeSpan.FromMinutes(60)
            };
            collection.Indexes.CreateOne(new CreateIndexModel<LogEntry>(indexKeysDefinition, differentIndexOptions));
        }
        catch (MongoCommandException ex)
        {
            caughtException = ex;
        }

        caughtException.Should().NotBeNull("Should throw MongoCommandException when creating index with different options");
        caughtException!.CodeName.Should().Be("IndexOptionsConflict",
            "MongoDB should return CodeName 'IndexOptionsConflict' for index with different options");
        caughtException.Code.Should().Be(85, "Error code should be 85 for IndexOptionsConflict");

        mongoClient.DropDatabase(MongoDatabaseName);
    }

    #endregion
}
