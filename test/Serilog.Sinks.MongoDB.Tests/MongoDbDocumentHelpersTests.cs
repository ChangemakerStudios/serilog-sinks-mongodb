namespace Serilog.Sinks.MongoDB.Tests;

[TestFixture]
public class MongoDbDocumentHelpersTests
{
    [Test]
    public void ToBsonValue_WithDateTimeOffset_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var dateTimeOffset = DateTimeOffset.Now;
        var scalarValue = new ScalarValue(dateTimeOffset);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(dateTimeOffset.ToString());
    }

    [Test]
    public void ToBsonValue_WithEmptyGuid_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var guid = Guid.Empty;
        var scalarValue = new ScalarValue(guid);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(guid.ToString());
    }

    [Test]
    public void ToBsonValue_WithGuid_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var scalarValue = new ScalarValue(guid);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(guid.ToString());
    }

    [Test]
    public void ToBsonValue_WithInteger_ShouldConvertToInt32BsonValue()
    {
        // Arrange
        var intValue = 42;
        var scalarValue = new ScalarValue(intValue);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonInt32>();
        result.AsInt32.Should().Be(intValue);
    }

    [Test]
    public void ToBsonValue_WithNullableGuid_ShouldConvertToStringBsonValue()
    {
        // Arrange
        Guid? guid = Guid.NewGuid();
        var scalarValue = new ScalarValue(guid);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(guid.ToString());
    }

    [Test]
    public void ToBsonValue_WithNullGuid_ShouldReturnNull()
    {
        // Arrange
        Guid? guid = null;
        var scalarValue = new ScalarValue(guid);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ToBsonValue_WithString_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var stringValue = "test string";
        var scalarValue = new ScalarValue(stringValue);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(stringValue);
    }

    [Test]
    public void ToBsonValue_WithTimeSpan_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var timeSpan = TimeSpan.FromMinutes(30);
        var scalarValue = new ScalarValue(timeSpan);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(timeSpan.ToString());
    }

    [Test]
    public void ToBsonValue_WithUri_ShouldConvertToStringBsonValue()
    {
        // Arrange
        var uri = new Uri("https://example.com");
        var scalarValue = new ScalarValue(uri);

        // Act
        var result = scalarValue.ToBsonValue();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BsonString>();
        result.AsString.Should().Be(uri.ToString());
    }
}