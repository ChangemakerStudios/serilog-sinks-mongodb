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
using System.Linq;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Serilog.Events;
using Serilog.Helpers;

namespace Serilog.Sinks.MongoDB
{
    public class LogEntry
    {
        [BsonRepresentation(BsonType.String)]
        public LogEventLevel Level { get; set; }

        public DateTime UtcTimeStamp { get; set; }

        public MessageTemplate MessageTemplate { get; set; }

        public string RenderedMessage { get; set; }

        public BsonDocument Properties { get; set; }

        public BsonDocument Exception { get; set; }

        public static LogEntry MapFrom(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            return new LogEntry
                   {
                       MessageTemplate = logEvent.MessageTemplate,
                       RenderedMessage = logEvent.RenderMessage(),
                       Level = logEvent.Level,
                       UtcTimeStamp = logEvent.Timestamp.ToUniversalTime().UtcDateTime,
                       Exception = logEvent.Exception?.ToBsonDocument().SanitizeDocumentRecursive(),
                       Properties = BsonDocument.Create(
                           logEvent.Properties.ToDictionary(
                               s => s.Key.SanitizedElementName(),
                               s => s.Value.ToBsonValue()))
                   };
        }
    }
}