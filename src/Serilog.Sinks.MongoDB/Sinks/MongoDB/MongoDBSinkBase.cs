// Copyright 2014-2020 Serilog Contributors
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
using System.Configuration;
using System.Threading.Tasks;

using MongoDB.Driver;

using Serilog.Helpers;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MongoDB
{
    /// <summary>
    ///     Writes log events as documents to a MongoDb database.
    /// </summary>
    public abstract class MongoDBSinkBase : PeriodicBatchingSink
    {
        private readonly string _collectionName;

        private readonly IMongoDatabase _mongoDatabase;

        /// <summary>
        ///     Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="databaseUrlOrConnStrName">The URL of a MongoDB database, or connection string name containing the URL.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
        protected MongoDBSinkBase(
            string databaseUrlOrConnStrName,
            int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
            TimeSpan? period = null,
            string collectionName = MongoDBSinkDefaults.CollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : this(
                DatabaseFromMongoUrl(databaseUrlOrConnStrName),
                batchPostingLimit,
                period,
                collectionName,
                collectionCreationOptions)
        {
        }

        /// <summary>
        ///     Construct a sink posting to a specified database.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="collectionName">Name of the MongoDb collection to use for the log. Default is "log".</param>
        /// <param name="collectionCreationOptions">Collection Creation Options for the log collection creation.</param>
        /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
        protected MongoDBSinkBase(
            IMongoDatabase database,
            int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
            TimeSpan? period = null,
            string collectionName = MongoDBSinkDefaults.CollectionName,
            CreateCollectionOptions collectionCreationOptions = null)
            : base(batchPostingLimit, period ?? MongoDBSinkDefaults.BatchPeriod)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            this._mongoDatabase = database;
            this._collectionName = collectionName;
            this._mongoDatabase.VerifyCollectionExists(
                this._collectionName,
                collectionCreationOptions);
        }

        /// <summary>
        ///     Get the MongoDatabase for a specified database url
        /// </summary>
        /// <param name="databaseUrlOrConnStrName">The URL of a MongoDB database, or connection string name containing the URL.</param>
        /// <returns>The Mongodatabase</returns>
        private static IMongoDatabase DatabaseFromMongoUrl(string databaseUrlOrConnStrName)
        {
            if (string.IsNullOrWhiteSpace(databaseUrlOrConnStrName))
                throw new ArgumentNullException(nameof(databaseUrlOrConnStrName));

            MongoUrl mongoUrl;

#if NET452
            try
            {
                mongoUrl = MongoUrl.Create(databaseUrlOrConnStrName);
            }
            catch (MongoConfigurationException)
            {
                var connectionString =
                    ConfigurationManager.ConnectionStrings[databaseUrlOrConnStrName];
                if (connectionString == null)
                    throw new KeyNotFoundException(
                        $"Invalid database url or connection string key: {databaseUrlOrConnStrName}");

                mongoUrl = MongoUrl.Create(connectionString.ConnectionString);
            }
#else
            mongoUrl = MongoUrl.Create(databaseUrlOrConnStrName);
#endif

            var mongoClient = new MongoClient(mongoUrl);

            return mongoClient.GetDatabase(mongoUrl.DatabaseName);
        }

        /// <summary>
        ///     Gets the log collection.
        /// </summary>
        /// <returns></returns>
        protected IMongoCollection<T> GetCollection<T>()
        {
            return this._mongoDatabase.GetCollection<T>(this._collectionName);
        }

        protected Task InsertMany<T>(IEnumerable<T> objects)
        {
            return Task.WhenAll(this.GetCollection<T>().InsertManyAsync(objects));
        }
    }
}