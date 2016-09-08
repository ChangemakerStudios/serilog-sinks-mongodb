// Serilog.Sinks.MongoDB - Copyright (c) 2016 CaptiveAire

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;

using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Helpers
{
    internal static class MongoDbHelper
    {
        /// <summary>
        /// Returns true if a collection exists on the mongodb server.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        internal static bool CollectionExists(this IMongoDatabase database, string collectionName)
        {
            return
                database.ListCollections(new ListCollectionsOptions { Filter = new BsonDocument { { "name", collectionName } } })
                    .ToList()
                    .Count > 0;
        }

        /// <summary>
        /// Verifies the collection exists. If it doesn't, create it using the Collection Creation Options provided.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="collectionCreationOptions">The collection creation options.</param>
        internal static void VerifyCollectionExists(this IMongoDatabase database, string collectionName, CreateCollectionOptions collectionCreationOptions = null)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (collectionName == null)
                throw new ArgumentNullException(nameof(collectionName));

            if (!database.CollectionExists(collectionName))
            {
                database.CreateCollection(collectionName, collectionCreationOptions);
            }
        }

        /// <summary>
        /// Generate BSON documents from LogEvents.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="formatter">The formatter.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        internal static IReadOnlyCollection<BsonDocument> GenerateBsonDocuments(this IEnumerable<LogEvent> events, JsonFormatter formatter)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var payload = new StringWriter();

            payload.Write("{\"logEvents\":[");

            var delimStart = "{";

            foreach (var logEvent in events)
            {
                payload.Write(delimStart);
                formatter.Format(logEvent, payload);
                payload.Write(",\"UtcTimestamp\":\"{0:u}\"}}", logEvent.Timestamp.ToUniversalTime().DateTime);
                delimStart = ",{";
            }

            payload.Write("]}");

            var bson = BsonDocument.Parse(payload.ToString());

            return bson["logEvents"].AsBsonArray.Select(x => x.AsBsonDocument).ToList();
        }
    }
}