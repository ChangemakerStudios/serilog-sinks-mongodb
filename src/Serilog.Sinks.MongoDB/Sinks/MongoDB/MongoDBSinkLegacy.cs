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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Helpers;

namespace Serilog.Sinks.MongoDB
{
    public class MongoDBSinkLegacy : MongoDBSinkBase
    {
        private readonly ITextFormatter _formatter;

        public MongoDBSinkLegacy(
            MongoDBSinkConfiguration configuration,
            IFormatProvider? formatProvider = null,
            ITextFormatter? textFormatter = null)
            : base(configuration)
        {
            this._formatter = textFormatter ?? new MongoDBJsonFormatter(
                renderMessage: true,
                formatProvider: formatProvider);
        }

        /// <summary>
        /// Creates Json from the log events enumerable
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        private string GenerateJson(IEnumerable<LogEvent> events)
        {
            var logEventLines = new List<string>();

            foreach (var logEvent in events)
            {
                string logEventLine;

                using (var writer = new StringWriter())
                {
                    this._formatter.Format(logEvent, writer);
                    logEventLine = writer.ToString().Trim();
                }

                if (logEventLine.EndsWith("}"))
                {
                    // remove so we can add Utc to end
                    logEventLine = logEventLine.Substring(0, logEventLine.Length - 1);
                }

                logEventLine += $@", ""UtcTimestamp"": ""{logEvent.Timestamp.ToUniversalTime().DateTime:u}"" }}";

                logEventLines.Add(logEventLine);
            }

            return $@"{{ ""logEvents"": [{string.Join(",", logEventLines)}] }}";
        }

        /// <summary>
        ///     Generate BSON documents from LogEvents.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        protected IReadOnlyCollection<BsonDocument> GenerateBsonDocuments(
            IEnumerable<LogEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var json = this.GenerateJson(events);
            var bson = BsonDocument.Parse(json);

            return bson["logEvents"].AsBsonArray
                .Select(x => x.AsBsonDocument.SanitizeDocumentRecursive()).ToList();
        }

        /// <summary>
        ///     Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <returns></returns>
        /// <remarks>
        ///     Override either
        ///     <see
        ///         cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />
        ///     or
        ///     <see
        ///         cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />
        ///     ,
        ///     not both. Overriding EmitBatch() is preferred.
        /// </remarks>
        protected override Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            return this.InsertMany(this.GenerateBsonDocuments(events));
        }
    }
}