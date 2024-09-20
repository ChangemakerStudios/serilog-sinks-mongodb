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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using Serilog.Events;

namespace Serilog.Sinks.MongoDB;

public class MongoDBSink : MongoDBSinkBase
{
    static MongoDBSink()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Exception)))
            BsonClassMap.RegisterClassMap<Exception>(
                cm =>
                {
                    cm.AutoMap();
                    cm.MapProperty(s => s.Message);
                    cm.MapProperty(s => s.Source);
                    cm.MapProperty(s => s.StackTrace);
                    cm.MapProperty(s => s.Data);
                });

        // fixes https://github.com/serilog/serilog/issues/2101
        BsonTypeMapper.RegisterCustomTypeMapper(typeof(IntPtr), new CustomIntPtrMapper());
        BsonTypeMapper.RegisterCustomTypeMapper(typeof(UIntPtr), new CustomIntPtrMapper());
    }

    public MongoDBSink(MongoDBSinkConfiguration configuration)
        : base(configuration)
    {
    }

    public override Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        return this.InsertMany(
            events.Select(@event => LogEntry.MapFrom(@event, this.IncludeMessageTemplate)));
    }

    private class CustomIntPtrMapper : ICustomBsonTypeMapper
    {
        public bool TryMapToBsonValue(object value, [UnscopedRef] out BsonValue? bsonValue)
        {
            switch (value)
            {
                case IntPtr intPtr:
                    bsonValue = intPtr.ToInt32();
                    return true;
                case UIntPtr uIntPtr:
                    bsonValue = uIntPtr.ToUInt32();
                    return true;
                default:
                    bsonValue = null;
                    return false;
            }
        }
    }
}