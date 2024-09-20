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
using System.Diagnostics;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Serilog.Events;
using Serilog.Helpers;

namespace Serilog.Sinks.MongoDB;

public class LogEntry
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public LogEventLevel Level { get; set; }

    public DateTime UtcTimeStamp { get; set; }

    [BsonIgnoreIfNull]
    public MessageTemplate? MessageTemplate { get; set; }

    public string? RenderedMessage { get; set; }

    public BsonDocument? Properties { get; set; }

    public BsonDocument? Exception { get; set; }
    [BsonIgnoreIfNull]
    public string? TraceId { get; set; }
    [BsonIgnoreIfNull]
    public string? SpanId { get; set; }

    public static LogEntry MapFrom(LogEvent logEvent, bool includeMessageTemplate)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        var logEntry = new LogEntry
                       {
                           RenderedMessage = logEvent.RenderMessage(),
                           Level = logEvent.Level,
                           UtcTimeStamp = logEvent.Timestamp.ToUniversalTime().UtcDateTime,
                           TraceId = logEvent.TraceId?.ToString(),
                           SpanId = logEvent.SpanId?.ToString(),
                           Exception = logEvent.Exception?.ToBsonDocument().SanitizeDocumentRecursive(),
                           Properties = BsonDocument.Create(
                               logEvent.Properties.ToDictionary(
                                   s => s.Key.SanitizedElementName(),
                                   s => s.Value.ToBsonValue()))
                       };

        if (includeMessageTemplate)
        {
            logEntry.MessageTemplate = logEvent.MessageTemplate;
        }

        return logEntry;
    }
}