// Copyright 2014-2016 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
                    .Any();
        }

        /// <summary>
        /// Verifies the collection exists. If it doesn't, create it using the Collection Creation Options provided.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="collectionCreationOptions">The collection creation options.</param>
        internal static void VerifyCollectionExists(this IMongoDatabase database, string collectionName, CreateCollectionOptions collectionCreationOptions = null)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            if (collectionName == null) throw new ArgumentNullException(nameof(collectionName));

            if (database.CollectionExists(collectionName)) return;

            try
            {
                database.CreateCollection(collectionName, collectionCreationOptions);
            }
            catch (MongoCommandException e)
            {
                if (!e.ErrorMessage.Equals("collection already exists")) throw;
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
            if (events == null) throw new ArgumentNullException(nameof(events));

            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            var payload = new StringWriter();

            payload.Write(@"{""logEvents"":[");

            var delimStart = "{";

            foreach (var logEvent in events)
            {
                payload.Write(delimStart);
                formatter.Format(logEvent, payload);
                payload.Write(@",""UtcTimestamp"":""{0:u}""}}", logEvent.Timestamp.ToUniversalTime().DateTime);
                delimStart = ",{";
            }

            payload.Write("]}");

            var bson = BsonDocument.Parse(payload.ToString());

            return bson["logEvents"].AsBsonArray.Select(x => x.AsBsonDocument).ToList();
        }
    }
}