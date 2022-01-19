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
using System.Diagnostics;

using MongoDB.Bson;
using MongoDB.Driver;

using Serilog.Sinks.MongoDB;

namespace Serilog.Helpers
{
    internal static class MongoDbHelper
    {
        /// <summary>
        ///     Returns true if a collection exists on the mongodb server.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        internal static bool CollectionExists(this IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collectionCursor =
                database.ListCollections(new ListCollectionsOptions { Filter = filter });
            return collectionCursor.Any();
        }

        /// <summary>
        ///     Verifies the collection exists. If it doesn't, create it using the Collection Creation Options provided.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="collectionCreationOptions">The collection creation options.</param>
        internal static void VerifyCollectionExists(
            this IMongoDatabase database,
            string collectionName,
            CreateCollectionOptions collectionCreationOptions = null)
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
    }
}