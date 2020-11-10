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
        private readonly MongoDBSinkConfiguration _configuration;

        protected string CollectionName => this._configuration.CollectionName;

        private readonly IMongoDatabase _mongoDatabase;

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
        protected MongoDBSinkBase(MongoDBSinkConfiguration configuration)
            : base(configuration.BatchPostingLimit, configuration.BatchPeriod)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            this._configuration = configuration;
            this._mongoDatabase = TryGetMongoDatabaseFromConfiguration(configuration);
        }

        protected static IMongoDatabase TryGetMongoDatabaseFromConfiguration(
            MongoDBSinkConfiguration configuration)
        {
            configuration.Validate();

            if (configuration.MongoDatabase != null)
            {
                return configuration.MongoDatabase;
            }

            var mongoDatabase = new MongoClient(configuration.MongoUrl).GetDatabase(
                configuration.MongoUrl.DatabaseName);

            mongoDatabase.VerifyCollectionExists(
                configuration.CollectionName,
                configuration.CollectionCreationOptions);

            return mongoDatabase;
        }

        /// <summary>
        ///     Gets the log collection.
        /// </summary>
        /// <returns></returns>
        protected IMongoCollection<T> GetCollection<T>()
        {
            return this._mongoDatabase.GetCollection<T>(this.CollectionName);
        }

        protected Task InsertMany<T>(IEnumerable<T> objects)
        {
            return Task.WhenAll(this.GetCollection<T>().InsertManyAsync(objects));
        }
    }
}