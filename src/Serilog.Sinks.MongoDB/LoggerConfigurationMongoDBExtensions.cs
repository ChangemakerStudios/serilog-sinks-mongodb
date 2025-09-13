// Copyright 2014-2024 Serilog Contributors
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

using MongoDB.Driver;

using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.MongoDB;
using Serilog.Sinks.PeriodicBatching;

// ReSharper disable once CheckNamespace
namespace Serilog;

/// <summary>
///     Adds the WriteTo.MongoDB() extension method to <see cref="LoggerConfiguration" />.
/// </summary>
public static class LoggerConfigurationMongoDBExtensions
{
    /// <summary>
    ///     Adds a sink that writes log events as bson documents to a MongoDb database.
    ///     For AppSettings Configuration.
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="databaseUrl">Mongo Url Connection String</param>
    /// <param name="collectionName"></param>
    /// <param name="restrictedToMinimumLevel"></param>
    /// <param name="batchPostingLimit"></param>
    /// <param name="period"></param>
    /// <param name="cappedMaxSizeMb"></param>
    /// <param name="cappedMaxDocuments"></param>
    /// <param name="rollingInterval"></param>
    /// <returns></returns>
    public static LoggerConfiguration MongoDBBson(
        this LoggerSinkConfiguration loggerConfiguration,
        string databaseUrl,
        string collectionName = MongoDBSinkDefaults.CollectionName,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
        TimeSpan? period = null,
        long? cappedMaxSizeMb = null,
        long? cappedMaxDocuments = null,
        RollingInterval? rollingInterval = null)
    {
        var cfg = new MongoDBSinkConfiguration();

        cfg.SetMongoUrl(databaseUrl);
        cfg.SetCollectionName(collectionName);
        cfg.SetBatchPostingLimit(batchPostingLimit);

        if (period.HasValue) cfg.SetBatchPeriod(period.Value);

        if (cappedMaxSizeMb.HasValue || cappedMaxDocuments.HasValue)
            cfg.SetCreateCappedCollection(
                cappedMaxSizeMb ?? MongoDBSinkDefaults.CappedCollectionMaxSizeMb,
                cappedMaxDocuments);

        if (rollingInterval.HasValue) cfg.SetRollingInterval(rollingInterval.Value);

        cfg.Validate();

        return loggerConfiguration.Sink(
            new PeriodicBatchingSink(
                new MongoDBSink(cfg),
                cfg.ToPeriodicBatchingSinkOptions()),
            restrictedToMinimumLevel);
    }

    /// <summary>
    ///     Adds a sink that writes log events as bson documents to a MongoDb database.
    ///     Fluent configuration.
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="configureAction"></param>
    /// <param name="restrictedToMinimumLevel"></param>
    /// <returns></returns>
    public static LoggerConfiguration MongoDBBson(
        this LoggerSinkConfiguration loggerConfiguration,
        Action<MongoDBSinkConfiguration> configureAction,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));
        if (configureAction == null) throw new ArgumentNullException(nameof(configureAction));

        var cfg = new MongoDBSinkConfiguration();

        configureAction(cfg);

        cfg.Validate();

        return loggerConfiguration.Sink(
            new PeriodicBatchingSink(
                new MongoDBSink(cfg),
                cfg.ToPeriodicBatchingSinkOptions()),
            restrictedToMinimumLevel);
    }

    /// <summary>
    ///     Adds a sink that writes log events as documents to a MongoDb database.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="databaseUrl">The URL of a created MongoDB collection that log events will be written to.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
    /// <param name="collectionName">Name of the collection. Default is "log".</param>
    /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
    /// <param name="period">The time to wait between checking for event batches.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
    public static LoggerConfiguration MongoDB(
        this LoggerSinkConfiguration loggerConfiguration,
        string databaseUrl,
        string collectionName = MongoDBSinkDefaults.CollectionName,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? mongoDBJsonFormatter = null)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new ArgumentNullException(nameof(databaseUrl));

        var cfg = new MongoDBSinkConfiguration { Legacy = true };

        cfg.SetConnectionString(databaseUrl);
        cfg.SetBatchPostingLimit(batchPostingLimit);
        if (period.HasValue) cfg.SetBatchPeriod(period.Value);
        cfg.SetCollectionName(collectionName);

        return
            loggerConfiguration.Sink(
                new PeriodicBatchingSink(
                    new MongoDBSinkLegacy(
                        cfg,
                        formatProvider,
                        mongoDBJsonFormatter),
                    cfg.ToPeriodicBatchingSinkOptions()),
                restrictedToMinimumLevel);
    }

    /// <summary>
    ///     Adds a sink that writes log events as documents to a MongoDb database.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="database">The MongoDb database where the log collection will live.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
    /// <param name="collectionName">Name of the collection. Default is "log".</param>
    /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
    /// <param name="period">The time to wait between checking for event batches.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
    public static LoggerConfiguration MongoDB(
        this LoggerSinkConfiguration loggerConfiguration,
        IMongoDatabase database,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string collectionName = MongoDBSinkDefaults.CollectionName,
        int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? mongoDBJsonFormatter = null)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));
        if (database == null) throw new ArgumentNullException(nameof(database));

        var cfg = new MongoDBSinkConfiguration { Legacy = true };

        cfg.SetMongoDatabase(database);
        cfg.SetBatchPostingLimit(batchPostingLimit);
        if (period.HasValue) cfg.SetBatchPeriod(period.Value);
        cfg.SetCollectionName(collectionName);

        return
            loggerConfiguration.Sink(
                new PeriodicBatchingSink(
                    new MongoDBSinkLegacy(
                        cfg,
                        formatProvider,
                        mongoDBJsonFormatter),
                    cfg.ToPeriodicBatchingSinkOptions()),
                restrictedToMinimumLevel);
    }

    /// <summary>
    ///     Adds a sink that writes log events as documents to a capped collection in a MongoDb database.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="databaseUrl">
    ///     The URL of a MongoDb database where the log collection will live (used for backwards
    ///     compatibility).
    /// </param>
    /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
    /// <param name="cappedMaxSizeMb">Max total size in megabytes of the created capped collection. (Default: 50mb)</param>
    /// <param name="cappedMaxDocuments">Max number of documents of the created capped collection.</param>
    /// <param name="collectionName">Name of the collection. Default is "log".</param>
    /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
    /// <param name="period">The time to wait between checking for event batches.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
    public static LoggerConfiguration MongoDBCapped(
        this LoggerSinkConfiguration loggerConfiguration,
        string databaseUrl,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        long cappedMaxSizeMb = 50,
        long? cappedMaxDocuments = null,
        string collectionName = MongoDBSinkDefaults.CollectionName,
        int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? mongoDBJsonFormatter = null)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new ArgumentNullException(nameof(databaseUrl));

        var cfg = new MongoDBSinkConfiguration { Legacy = true };

        cfg.SetConnectionString(databaseUrl);
        cfg.SetBatchPostingLimit(batchPostingLimit);
        if (period.HasValue) cfg.SetBatchPeriod(period.Value);
        cfg.SetCollectionName(collectionName);
        cfg.SetCreateCappedCollection(cappedMaxSizeMb, cappedMaxDocuments);

        return
            loggerConfiguration.Sink(
                new PeriodicBatchingSink(
                    new MongoDBSinkLegacy(
                        cfg,
                        formatProvider,
                        mongoDBJsonFormatter),
                    cfg.ToPeriodicBatchingSinkOptions()),
                restrictedToMinimumLevel);
    }

    /// <summary>
    ///     Adds a sink that writes log events as documents to a capped collection in a MongoDb database.
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
    /// <param name="mongoDBJsonFormatter">Formatter to produce json for MongoDB.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
    public static LoggerConfiguration MongoDBCapped(
        this LoggerSinkConfiguration loggerConfiguration,
        IMongoDatabase database,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        long cappedMaxSizeMb = 50,
        long? cappedMaxDocuments = null,
        string collectionName = MongoDBSinkDefaults.CollectionName,
        int batchPostingLimit = MongoDBSinkDefaults.BatchPostingLimit,
        TimeSpan? period = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? mongoDBJsonFormatter = null)
    {
        if (loggerConfiguration == null)
            throw new ArgumentNullException(nameof(loggerConfiguration));
        if (database == null) throw new ArgumentNullException(nameof(database));

        var cfg = new MongoDBSinkConfiguration { Legacy = true };

        cfg.SetMongoDatabase(database);
        cfg.SetBatchPostingLimit(batchPostingLimit);
        if (period.HasValue) cfg.SetBatchPeriod(period.Value);
        cfg.SetCollectionName(collectionName);
        cfg.SetCreateCappedCollection(cappedMaxSizeMb, cappedMaxDocuments);

        return
            loggerConfiguration.Sink(
                new PeriodicBatchingSink(
                    new MongoDBSinkLegacy(
                        cfg,
                        formatProvider,
                        mongoDBJsonFormatter),
                    cfg.ToPeriodicBatchingSinkOptions()),
                restrictedToMinimumLevel);
    }
}