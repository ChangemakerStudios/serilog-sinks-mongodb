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
using System.Globalization;

using Serilog.Sinks.MongoDB;

namespace Serilog.Helpers;

internal static class RollingIntervalHelper
{
    /// <summary>
    ///     Returns collection name based on rolling interval.
    /// </summary>
    /// <example>log202210</example>
    /// <param name="interval">The <see cref="RollingInterval" />.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <exception cref="ArgumentException">If passed invalid rolling interval.</exception>
    internal static string GetCollectionName(this RollingInterval interval, string collectionName)
    {
        if (interval == RollingInterval.Infinite) return collectionName;

        return
            $"{collectionName}_{DateTime.Now.ToString(GetDateTimeFormatForInterval(interval), CultureInfo.InvariantCulture)}";
    }

    internal static string GetDateTimeFormatForInterval(this RollingInterval interval)
    {
        switch (interval)
        {
            case RollingInterval.Year:
                return "yyyy";
            case RollingInterval.Month:
                return "yyyyMM";
            case RollingInterval.Day:
                return "yyyyMMdd";
            case RollingInterval.Hour:
                return "yyyyMMddhh";
            case RollingInterval.Minute:
                return "yyyyMMddhhmm";
            default:
                throw new ArgumentException("Invalid rolling interval");
        }
    }
}