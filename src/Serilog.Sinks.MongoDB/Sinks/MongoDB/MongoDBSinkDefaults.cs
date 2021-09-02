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

namespace Serilog.Sinks.MongoDB
{
    public static class MongoDBSinkDefaults
    {
        /// <summary>
        ///     A reasonable default for the number of events posted in
        ///     each batch.
        /// </summary>
        public const int BatchPostingLimit = 50;

        /// <summary>
        ///     The default name for the log collection.
        /// </summary>
        public const string CollectionName = "log";

        /// <summary>
        ///     Default capped collection max size in megabytes
        /// </summary>
        public const int CappedCollectionMaxSizeMb = 50;

        /// <summary>
        ///     A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan BatchPeriod = TimeSpan.FromSeconds(2);
    }
}