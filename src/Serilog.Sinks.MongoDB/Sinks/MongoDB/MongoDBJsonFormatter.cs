// Copyright 2014-2016 Serilog Contributors
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

using System.Collections.Generic;
using MongoDB.Bson;
using Serilog.Formatting.Json;
using System;
using System.IO;

namespace Serilog.Sinks.MongoDB
{
    /// <summary>
    /// JsonFormatter for MongoDB
    /// </summary>
    public class MongoDBJsonFormatter : JsonFormatter
    {
        private readonly IDictionary<Type, Action<object, TextWriter>> _dateTimeWriters;

        /// <summary>
        /// See <see cref="T:Serilog.Formatting.Json.JsonFormatter"/>.
        /// Default JsonFormatter writes DateTimeOffset as string with round trip date/time pattern to MongoDB,
        /// which contains the local time zone component. String search on these field, with different timezones,
        /// will not return the correct values. This JsonFormatter writes them as MongoDB Date objects.
        /// </summary>
        /// <param name="omitEnclosingObject">If true, the properties of the event will be written to
        /// the output without enclosing braces. Otherwise, if false, each event will be written as a well-formed
        /// JSON object.</param>
        /// <param name="closingDelimiter">A string that will be written after each log event is formatted.
        /// If null, <see cref="Environment.NewLine"/> will be used. Ignored if <paramref name="omitEnclosingObject"/>
        /// is true.</param>
        /// <param name="renderMessage">If true, the message will be rendered and written to the output as a
        /// property named RenderedMessage.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        public MongoDBJsonFormatter(
            bool omitEnclosingObject = false,
            string closingDelimiter = null,
            bool renderMessage = false,
            IFormatProvider formatProvider = null)
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider)
        {
            _dateTimeWriters = new Dictionary<Type, Action<object, TextWriter>>
            {
                {typeof (DateTime), (v, w) => WriteDateTime((DateTime) v, w)},
                {typeof (DateTimeOffset), (v, w) => WriteOffset((DateTimeOffset) v, w)}
            };
        }

        /// <summary>
        /// Writes out a json property with the specified value on output writer
        /// </summary>
        protected override void WriteJsonProperty(
            string name,
            object value,
            ref string precedingDelimiter,
            TextWriter output)
        {
            Action<object, TextWriter> action;
            if (value != null && _dateTimeWriters.TryGetValue(value.GetType(), out action))
            {
                output.Write(precedingDelimiter);
                output.Write("\"");
                output.Write(name);
                output.Write("\":");
                action(value, output);
                precedingDelimiter = ",";
            }
            else
                base.WriteJsonProperty(name, value, ref precedingDelimiter, output);
        }

        private static void WriteOffset(DateTimeOffset value, TextWriter output)
        {
            output.Write($"{{ \"$date\" : {BsonUtils.ToMillisecondsSinceEpoch(value.UtcDateTime)} }}");
        }

        private static void WriteDateTime(DateTime value, TextWriter output)
        {
            output.Write($"{{ \"$date\" : {BsonUtils.ToMillisecondsSinceEpoch(value)} }}");
        }
    }
}