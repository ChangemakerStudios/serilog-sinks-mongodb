// Copyright 2014 Serilog Contributors
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

using MongoDB.Bson;
using MongoDB.Driver;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MongoDB
{
    /// <summary>
    /// Writes log events as documents to a MongoDB database.
    /// </summary>
    public class MongoDBSink : PeriodicBatchingSink
    {
        readonly string _collectionName;
        readonly CreateCollectionOptions _collectionCreationOptions;
        readonly IFormatProvider _formatProvider;
        readonly IMongoDatabase _mongoDatabase;

        /// <summary>
        /// The default name for the log collection.
        /// </summary>
        public static readonly string DefaultCollectionName = "log";

        /// <summary>
        /// A reasonable default for the number of events posted in
        /// each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 50;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="databaseUrl">The URL of a MongoDB database.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        public MongoDBSink(string databaseUrl, int batchPostingLimit, TimeSpan period, IFormatProvider formatProvider, string collectionName, CreateCollectionOptions collectionCreationOptions)
           : this(DatabaseFromMongoUrl(databaseUrl), batchPostingLimit, period, formatProvider, collectionName, collectionCreationOptions)
        { }

        /// <summary>
        /// Construct a sink posting to a specified database.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        public MongoDBSink(IMongoDatabase database, int batchPostingLimit, TimeSpan period, IFormatProvider formatProvider, string collectionName, CreateCollectionOptions collectionCreationOptions)
            : base(batchPostingLimit, period)
        {
            if (database == null) throw new ArgumentNullException("database");

            _mongoDatabase = database;
            _collectionName = collectionName;
            _collectionCreationOptions = collectionCreationOptions;
            _formatProvider = formatProvider;

            //  capped collections have to be created because GetCollection doesn't offer the option to create one implicitly
            //  only create one if it doesn't already exist
            if (_collectionCreationOptions.Capped.GetValueOrDefault(false)) 
            {
                if (_mongoDatabase.ListCollections(new ListCollectionsOptions 
                {
                    Filter = new BsonDocument {{"name", _collectionName}}
                }).ToList().Count == 0) 
                {
                    _mongoDatabase.CreateCollection(_collectionName, _collectionCreationOptions);
                }
            }
        }

        /// <summary>
        /// Get the MongoDatabase for a specified database url
        /// </summary>
        /// <param name="databaseUrl">The URL of a MongoDB database.</param>
        /// <returns>The Mongodatabase</returns>
        private static IMongoDatabase DatabaseFromMongoUrl (string databaseUrl)
        {
            if (databaseUrl == null) throw new ArgumentNullException("databaseUrl");

            var mongoUrl = new MongoUrl(databaseUrl);
            var mongoClient = new MongoClient(mongoUrl);
            return mongoClient.GetDatabase(mongoUrl.DatabaseName);
        }


        IMongoCollection<BsonDocument> GetLogCollection()
        {
            return _mongoDatabase.GetCollection<BsonDocument>(_collectionName);
        }

        /// <summary>
        /// Verifies the the MongoDatabase collection exists or creates it if it doesn't.
        /// </summary>
        [Obsolete("MongoDB no longer needs to be checked, it'll create on the fly")]
        protected void VerifyCollection()
        {
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>Override either <see cref="PeriodicBatchingSink.EmitBatch"/> or <see cref="PeriodicBatchingSink.EmitBatchAsync"/>,
        /// not both.</remarks>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            var payload = new StringWriter();
            payload.Write("{\"d\":[");

            var formatter = new MongoDBJsonFormatter(true,
                renderMessage: true,
                formatProvider: _formatProvider);

            var delimStart = "{";
            foreach (var e in events)
            {
                payload.Write(delimStart);
                formatter.Format(e, payload);
                payload.Write(",\"UtcTimestamp\":\"{0:u}\"}}",
                              e.Timestamp.ToUniversalTime().DateTime);
                delimStart = ",{";
            }

            payload.Write("]}");
            
            var bson = BsonDocument.Parse(payload.ToString());
            var docs = bson["d"].AsBsonArray.Select(x => x.AsBsonDocument);
            Task.WaitAll(GetLogCollection().InsertManyAsync(docs));
        }
    }
}
