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
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

using Serilog.Events;
using Serilog.Helpers;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MongoDB
{
    /// <summary>
    /// Writes log events as documents to a MongoDB database.
    /// </summary>
    public class MongoDBSink : PeriodicBatchingSink
    {
        readonly string _collectionName;
        readonly IMongoDatabase _mongoDatabase;
        readonly MongoDBJsonFormatter _mongoDbJsonFormatter;

        /// <summary>
        /// Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="databaseUrlOrConnStrName">The URL of a MongoDB database, or connection string name containing the URL.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        public MongoDBSink(
            string databaseUrlOrConnStrName,
            int batchPostingLimit = DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            string collectionName = DefaultCollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : this(DatabaseFromMongoUrl(databaseUrlOrConnStrName), batchPostingLimit, period, formatProvider, collectionName, collectionCreationOptions)
        {
        }

        /// <summary>
        /// Construct a sink posting to a specified database.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        public MongoDBSink(
            IMongoDatabase database,
            int batchPostingLimit = DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            string collectionName = DefaultCollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : base(batchPostingLimit, period ?? DefaultPeriod)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            this._mongoDatabase = database;
            this._collectionName = collectionName;
            this._mongoDbJsonFormatter = new MongoDBJsonFormatter(true, renderMessage: true, formatProvider: formatProvider);

            this._mongoDatabase.VerifyCollectionExists(this._collectionName, collectionCreationOptions);
        }

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
        /// The default name for the log collection.
        /// </summary>
        public const string DefaultCollectionName = "log";

        /// <summary>
        /// Get the MongoDatabase for a specified database url
        /// </summary>
        /// <param name="databaseUrlOrConnStrName">The URL of a MongoDB database, or connection string name containing the URL.</param>
        /// <returns>The Mongodatabase</returns>
        static IMongoDatabase DatabaseFromMongoUrl(string databaseUrlOrConnStrName)
        {
            if (string.IsNullOrWhiteSpace(databaseUrlOrConnStrName))
                throw new ArgumentNullException(nameof(databaseUrlOrConnStrName));

            MongoUrl mongoUrl;

#if NET_45
            try
            {
                mongoUrl = MongoUrl.Create(databaseUrlOrConnStrName);
            }
            catch (MongoConfigurationException)
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[databaseUrlOrConnStrName];
                if (connectionString == null)
                    throw new KeyNotFoundException($"Invalid database url or connection string key: {databaseUrlOrConnStrName}");

                mongoUrl = MongoUrl.Create(connectionString.ConnectionString);
            }
#else
            mongoUrl = MongoUrl.Create(databaseUrlOrConnStrName);
#endif

            var mongoClient = new MongoClient(mongoUrl);
            return mongoClient.GetDatabase(mongoUrl.DatabaseName);
        }

        /// <summary>
        /// Gets the log collection.
        /// </summary>
        /// <returns></returns>
        IMongoCollection<BsonDocument> GetLogCollection()
        {
            return this._mongoDatabase.GetCollection<BsonDocument>(this._collectionName);
        }

        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <returns></returns>
        /// <remarks>
        /// Override either <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" /> or <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />,
        /// not both. Overriding EmitBatch() is preferred.
        /// </remarks>
        protected override Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var docs = events.GenerateBsonDocuments(this._mongoDbJsonFormatter);
            return Task.WhenAll(this.GetLogCollection().InsertManyAsync(docs));
        }
    }
}