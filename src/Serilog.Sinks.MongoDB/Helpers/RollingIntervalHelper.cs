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
using Serilog.Sinks.MongoDB;

namespace Serilog.Helpers
{
    internal static class RollingIntervalHelper
    {
        private static Func<DateTime> _dateTimeNow = () => DateTime.Now;
        private static DateTime DateTimeNow => _dateTimeNow();

        /// <summary>
        /// Returns collection name based on rolling interval.
        /// </summary>
        /// <example>log202210</example>
        /// <param name="interval">The <see cref="RollingInterval"/>.</param>
        /// <param name="collectionName">Collection name.</param>
        /// <exception cref="ArgumentException">If passed invalid rolling interval.</exception>
        internal static string GetCollectionName(this RollingInterval interval, string collectionName)
        {
            switch (interval)
            {
                case RollingInterval.Infinite:
                    return collectionName;
                case RollingInterval.Year:
                    return $"{collectionName}{DateTimeNow.Year.ToString()}";
                case RollingInterval.Month:
                    return $"{collectionName}{DateTimeNow.Year.ToString()}{DateTimeNow.Month.ToString()}";
                case RollingInterval.Day:
                    return
                        $"{collectionName}{DateTimeNow.Year.ToString()}{DateTimeNow.Month.ToString()}{DateTimeNow.Day.ToString()}";
                case RollingInterval.Hour:
                    return
                        $"{collectionName}{DateTimeNow.Year.ToString()}{DateTimeNow.Month.ToString()}{DateTimeNow.Day.ToString()}{DateTimeNow.Hour.ToString()}";
                case RollingInterval.Minute:
                    return
                        $"{collectionName}{DateTimeNow.Year.ToString()}{DateTimeNow.Month.ToString()}{DateTimeNow.Day.ToString()}{DateTimeNow.Hour.ToString()}{DateTimeNow.Minute.ToString()}";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }
    }
}