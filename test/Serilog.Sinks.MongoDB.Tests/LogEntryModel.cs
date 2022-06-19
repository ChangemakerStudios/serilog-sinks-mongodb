using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Serilog.Events;

namespace Serilog.Sinks.MongoDB.Tests
{
    public class LogEntryModel
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.String)]
        public LogEventLevel Level { get; set; }

        public DateTime UtcTimeStamp { get; set; }

        public MessageTemplate MessageTemplate { get; set; }

        public string RenderedMessage { get; set; }

        public BsonDocument Properties { get; set; }

        public BsonDocument Exception { get; set; }
    }
}