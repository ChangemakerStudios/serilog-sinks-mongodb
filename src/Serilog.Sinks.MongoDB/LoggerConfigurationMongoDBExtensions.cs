﻿// Copyright 2014-2016 Serilog Contributors
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
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.MongoDB;
using MongoDB.Driver;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.MongoDB() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationMongoDBExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events as documents to a MongoDb database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="databaseUrl">The URL of a created MongoDB collection that log events will be written to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="collectionName">Name of the collection. Default is "log".</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration MongoDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string databaseUrl,
            string collectionName = MongoDBSink.DefaultCollectionName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = MongoDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrWhiteSpace(databaseUrl)) throw new ArgumentNullException(nameof(databaseUrl));

            return
                loggerConfiguration.Sink(
                    new MongoDBSink(databaseUrl, batchPostingLimit, period, formatProvider, collectionName),
                    restrictedToMinimumLevel);
        }

        /// <summary>
        /// Adds a sink that writes log events as documents to a MongoDb database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="database">The MongoDb database where the log collection will live.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="collectionName">Name of the collection. Default is "log".</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration MongoDB(
            this LoggerSinkConfiguration loggerConfiguration,
            IMongoDatabase database,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string collectionName = MongoDBSink.DefaultCollectionName,
            int batchPostingLimit = MongoDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (database == null) throw new ArgumentNullException(nameof(database));

            return
                loggerConfiguration.Sink(
                    new MongoDBSink(database, batchPostingLimit, period, formatProvider, collectionName),
                    restrictedToMinimumLevel);
        }

        /// <summary>
        /// Adds a sink that writes log events as documents to a capped collection in a MongoDb database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="databaseUrl">The URL of a MongoDb database where the log collection will live (used for backwards compatibility).</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="cappedMaxSizeMb">Max total size in megabytes of the created capped collection. (Default: 50mb)</param>
        /// <param name="cappedMaxDocuments">Max number of documents of the created capped collection.</param>
        /// <param name="collectionName">Name of the collection. Default is "log".</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration MongoDBCapped(
            this LoggerSinkConfiguration loggerConfiguration,
            string databaseUrl,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            long cappedMaxSizeMb = 50,
            long? cappedMaxDocuments = null,
            string collectionName = MongoDBSink.DefaultCollectionName,
            int batchPostingLimit = MongoDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {

            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrWhiteSpace(databaseUrl)) throw new ArgumentNullException(nameof(databaseUrl));

            var options = new CreateCollectionOptions
            {
                Capped = true,
                MaxSize = cappedMaxSizeMb * 1024 * 1024
            };

            if (cappedMaxDocuments.HasValue) options.MaxDocuments = cappedMaxDocuments.Value;

            return loggerConfiguration.Sink(
                new MongoDBSink(
                    databaseUrl,
                    batchPostingLimit,
                    period,
                    formatProvider,
                    collectionName,
                    options),
                restrictedToMinimumLevel);
        }

        /// <summary>
        /// Adds a sink that writes log events as documents to a capped collection in a MongoDb database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="database">The MongoDb database where the log collection will live.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="cappedMaxSizeMb">Max total size in megabytes of the created capped collection. (Default: 50mb)</param>
        /// <param name="cappedMaxDocuments">Max number of documents of the created capped collection.</param>
        /// <param name="collectionName">Name of the collection. Default is "log".</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration MongoDBCapped(
            this LoggerSinkConfiguration loggerConfiguration,
            IMongoDatabase database,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            long cappedMaxSizeMb = 50,
            long? cappedMaxDocuments = null,
            string collectionName = MongoDBSink.DefaultCollectionName,
            int batchPostingLimit = MongoDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (database == null) throw new ArgumentNullException(nameof(database));

            var options = new CreateCollectionOptions()
            {
                Capped = true,
                MaxSize = cappedMaxSizeMb * 1024 * 1024
            };

            if (cappedMaxDocuments.HasValue) options.MaxDocuments = cappedMaxDocuments.Value;

            return loggerConfiguration.Sink(
                new MongoDBSink(
                    database,
                    batchPostingLimit,
                    period,
                    formatProvider,
                    collectionName,
                    options),
                restrictedToMinimumLevel);
        }
    }
}
