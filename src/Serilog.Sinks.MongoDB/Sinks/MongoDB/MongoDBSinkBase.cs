// Copyright 2014-2022 Serilog Contributors
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
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver;

using Serilog.Events;
using Serilog.Helpers;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.MongoDB;

/// <summary>
///     Writes log events as documents to a MongoDb database.
/// </summary>
public abstract class MongoDBSinkBase : IBatchedLogEventSink
{
    private readonly MongoDBSinkConfiguration _configuration;

    private readonly Lazy<IMongoDatabase> _mongoDatabase;

    /// <summary>
    ///     Construct a sink posting to a specified database.
    /// </summary>
    protected MongoDBSinkBase(MongoDBSinkConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        this._configuration = configuration;

        // validate the settings
        configuration.Validate();

        this._mongoDatabase = new Lazy<IMongoDatabase>(
            () => GetVerifiedMongoDatabaseFromConfiguration(this._configuration),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected string CollectionName => this._configuration.CollectionName;

    protected RollingInterval RollingInterval => this._configuration.RollingInterval;

    public abstract Task EmitBatchAsync(IEnumerable<LogEvent> batch);

    public virtual Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    protected static IMongoDatabase GetVerifiedMongoDatabaseFromConfiguration(
        MongoDBSinkConfiguration configuration)
    {
        var mongoDatabase = configuration.MongoDatabase
                            ?? new MongoClient(configuration.MongoUrl).GetDatabase(
                                configuration.MongoUrl!.DatabaseName);

        // connection attempt
        mongoDatabase.VerifyCollectionExists(
            configuration.CollectionName,
            configuration.CollectionCreationOptions);

        // setup TTL if desired
        mongoDatabase.VerifyExpireTTLSetup(
            configuration.CollectionName,
            configuration.ExpireTTL);

        return mongoDatabase;
    }

    /// <summary>
    ///     Gets the log collection.
    /// </summary>
    /// <returns></returns>
    public IMongoCollection<T> GetCollection<T>()
    {
        var collectionName = this.RollingInterval.GetCollectionName(this.CollectionName);
        return this._mongoDatabase.Value.GetCollection<T>(collectionName);
    }

    protected Task InsertMany<T>(IEnumerable<T> objects)
    {
        return Task.WhenAll(
            this.GetCollection<T>().InsertManyAsync(
                objects,
                this._configuration.InsertManyOptions));
    }
}