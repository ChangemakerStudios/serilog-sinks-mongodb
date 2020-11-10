using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using Serilog.Events;

namespace Serilog.Sinks.MongoDB
{
    public class MongoDBSink : MongoDBSinkBase
    {
        static MongoDBSink()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Exception)))
                BsonClassMap.RegisterClassMap<Exception>(
                    cm =>
                    {
                        cm.AutoMap();
                        cm.MapProperty(s => s.Message);
                        cm.MapProperty(s => s.Source);
                        cm.MapProperty(s => s.StackTrace);
                        cm.MapProperty(s => s.TargetSite);
                        cm.MapProperty(s => s.Data);
                    });
        }

        public MongoDBSink(
            string databaseUrlOrConnStrName,
            int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            string collectionName = MongoDBSinkDefaults.CollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : base(
                databaseUrlOrConnStrName,
                batchPostingLimit,
                period,
                formatProvider,
                collectionName,
                collectionCreationOptions)
        {
        }

        public MongoDBSink(
            IMongoDatabase database,
            int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            string collectionName = MongoDBSinkDefaults.CollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : base(
                database,
                batchPostingLimit,
                period,
                formatProvider,
                collectionName,
                collectionCreationOptions)
        {
        }

        protected override Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            return this.InsertMany(events.Select(LogEntry.MapFrom));
        }
    }
}