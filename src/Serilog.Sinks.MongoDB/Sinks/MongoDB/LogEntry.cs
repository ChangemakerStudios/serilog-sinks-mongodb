﻿// Copyright 2014-2020 Serilog Contributors
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

        public BsonDocument Properties { get; set; }

        public BsonDocument Exception { get; set; }

        public static LogEntry MapFrom(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            return new LogEntry
                   {
                       MessageTemplate = logEvent.MessageTemplate,
                       RenderedMessage = logEvent.RenderMessage(),
                       UtcTimeStamp = logEvent.Timestamp.ToUniversalTime().UtcDateTime,
                       Exception = logEvent.Exception == null ? null : SanitizeDocumentRecursive(logEvent.Exception.ToBsonDocument()),
                       Properties = BsonDocument.Create(
                           logEvent.Properties.ToDictionary(
                               s => SanitizedElementName(s.Key),
                               s => ToBsonValue(s.Value)))
                   };
        }

        /// <summary>
        /// Credit to @IvaskevychYuriy for this excellent solution:
        /// https://github.com/serilog/serilog-sinks-mongodb/pull/59#issuecomment-830079904
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static BsonDocument SanitizeDocumentRecursive(BsonDocument document)
        {
            var sanitizedElements = document.Select(
                e => new BsonElement(
                    SanitizedElementName(e.Name),
                    e.Value.IsBsonDocument
                        ? SanitizeDocumentRecursive(e.Value.AsBsonDocument)
                        : e.Value));

            return new BsonDocument(sanitizedElements);
        }

        private static string SanitizedElementName(string name)
        {
            if (name == null) return "[NULL]";

            return name.Replace('.', '-').Replace('$', '_');
        }

        internal static BsonValue ToBsonValue(LogEventPropertyValue value)
        {
            if (value == null) return null;

            if (value is ScalarValue scalar) return BsonValue.Create(scalar.Value);

            if (value is StructureValue sv)
                return BsonDocument.Create(
                    sv.Properties.ToDictionary(
                        s => SanitizedElementName(s.Name),
                        s => ToBsonValue(s.Value)));

            if (value is DictionaryValue dv)
                return BsonDocument.Create(
                    dv.Elements.ToDictionary(
                        s => SanitizedElementName(s.Key.Value?.ToString()),
                        s => ToBsonValue(s.Value)));

            if (value is SequenceValue sq)
                return BsonValue.Create(sq.Elements.Select(ToBsonValue).ToArray());

            return null;
        }
    }
}