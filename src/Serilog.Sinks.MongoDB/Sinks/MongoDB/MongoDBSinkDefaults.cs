using System;

namespace Serilog.Sinks.MongoDB
{
    public static class MongoDBSinkDefaults
    {
        /// <summary>
        ///     A reasonable default for the number of events posted in
        ///     each batch.
        /// </summary>
        public const int BatchPostingLimit = 50;

        /// <summary>
        ///     The default name for the log collection.
        /// </summary>
        public const string CollectionName = "log";

        /// <summary>
        ///     A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan BatchPeriod = TimeSpan.FromSeconds(2);
    }
}