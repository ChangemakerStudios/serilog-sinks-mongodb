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

using Serilog.Events;

namespace Serilog.Helpers
{
    internal static class MongoDbDocumentHelpers
    {
        /// <summary>
        ///     Credit to @IvaskevychYuriy for this excellent solution:
        ///     https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/59#issuecomment-830079904
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static BsonDocument SanitizeDocumentRecursive(this BsonDocument document)
        {
            var sanitizedElements = document.Select(
                e => new BsonElement(
                    SanitizedElementName(e.Name),
                    e.Value.IsBsonDocument
                        ? SanitizeDocumentRecursive(e.Value.AsBsonDocument)
                        : e.Value));

            return new BsonDocument(sanitizedElements);
        }

        internal static string SanitizedElementName(this string name)
        {
            if (name == null) return "[NULL]";

            return name.Replace('.', '-').Replace('$', '_');
        }

        internal static BsonValue ToBsonValue(this LogEventPropertyValue value)
        {
            if (value == null) return null;

            if (value is ScalarValue scalar)
            {
                if (scalar.Value is Uri uri)
                {
                    return BsonValue.Create(uri.ToString());
                }

                if (scalar.Value is TimeSpan ts)
                {
                    return BsonValue.Create(ts.ToString());
                }

                if (scalar.Value is DateTimeOffset dto)
                {
                    return BsonValue.Create(dto.ToString());
                }

                return BsonValue.Create(scalar.Value);
            }

            if (value is StructureValue sv)
            {
                return BsonDocument.Create(
                    sv.Properties.ToDictionary(
                        s => SanitizedElementName(s.Name),
                        s => ToBsonValue(s.Value)));
            }

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