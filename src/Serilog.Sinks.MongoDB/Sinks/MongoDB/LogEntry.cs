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
using System.Linq;

using MongoDB.Bson;

using Serilog.Events;

namespace Serilog.Sinks.MongoDB
{
    public class LogEntry
    {
        public DateTime UtcTimeStamp { get; set; }

        public MessageTemplate MessageTemplate { get; set; }

        public string RenderedMessage { get; set; }

        public Exception Exception { get; set; }

        public BsonDocument Properties { get; set; }

        public static LogEntry MapFrom(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            return new LogEntry
                   {
                       MessageTemplate = logEvent.MessageTemplate,
                       RenderedMessage = logEvent.RenderMessage(),
                       UtcTimeStamp = logEvent.Timestamp.ToUniversalTime().UtcDateTime,
                       Exception = logEvent.Exception,
                       Properties = BsonDocument.Create(
                           logEvent.Properties.ToDictionary(s => s.Key, s => ToBsonValue(s.Value)))
                   };
        }

        internal static BsonValue ToBsonValue(LogEventPropertyValue value)
        {
            if (value == null) return null;

            if (value is ScalarValue scalar) return BsonValue.Create(scalar.Value);

            if (value is StructureValue sv)
                return BsonDocument.Create(
                    sv.Properties.ToDictionary(s => s.Name, s => ToBsonValue(s.Value)));

            if (value is DictionaryValue dv)
                return BsonDocument.Create(
                    dv.Elements.ToDictionary(s => s.Key.Value, s => ToBsonValue(s.Value)));

            if (value is SequenceValue sq)
                return BsonValue.Create(sq.Elements.Select(ToBsonValue).ToArray());

            return null;
        }
    }
}