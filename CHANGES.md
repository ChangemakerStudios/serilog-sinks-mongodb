# Serilog.Sinks.MongoDB - Change Log

## 7.2.0 (2025-10-25)
 * Fixed: MongoDB error handling for v7.x compatibility using error codes instead of string matching ([#98](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/98), [#99](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/99)) - Thanks to [@ntark](https://github.com/ntark)!
 * Added: Comprehensive unit tests for MongoDB error handling ([#100](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/100))
 * Improved: CI workflow optimization to skip package generation during test runs

## 7.1.0 (2025-10-04)
 * Fixed: Guid to BsonValue mapping error ([#91](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/91))
 * Fixed: RollingInterval collection naming with proper formatting ([#95](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/95)) - Thanks to [@ntark](https://github.com/ntark)!
 * Fixed: Renamed SetRollingInternal to SetRollingInterval ([#97](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/97)) - Thanks to [@ntark](https://github.com/ntark)!

## 7.0.0 (2024-11-05)
 * **BREAKING**: Upgraded MongoDB.Driver to v3.0 fixing incompatibilities
 * **BREAKING**: Dropped .NET Standard 2.0 support

## 6.0.0 (2024-09-19)
 * **BREAKING**: Upgraded Serilog from 2.12.0 to 3.1.1
 * **BREAKING**: Upgraded MongoDB.Driver to v2.28.0 ([#85](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/85)) - Thanks to [@Memoyu](https://github.com/Memoyu)!
 * Added: Trace context (TraceId and SpanId) to LogEntry ([#87](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/87)) - Thanks to [@fernandovmp](https://github.com/fernandovmp)!
 * Added: Feature for issue [#82](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/82)
 * Added: License expression and repository badges
 * Updated: GitHub Actions to latest versions

## 5.4.0 (2024-02-17)
 * Updated: MongoDB.Driver dependency to v2.19.0
 * Updated: Converted tests to NUnit framework
 * Updated: Deployment workflow

## 5.3.1 (2022-09-27)
 * Updated: Documentation images and formatting

## 5.3.0 (2022-09-27)
 * Updated: Upgraded to latest PeriodicBatchSink and Serilog packages
 * Fixed: Compatibility with Serilog.Sinks.PeriodicBatching 3.0.0+ ([#80](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/80))

## 5.2.2 (2022-09-25)
 * Added: `InsertOptions` configuration to the sink to improve performance ([#79](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/79))
 * Updated: Documentation and added icon

## 5.2.1 (2022-09-02)
 * Fixed: Backwards compatibility issue with `Contains()` using `StringComparison` ([#76](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/76))
 * Added: Missing copyright headers

## 5.2.0 (2022-09-02)
 * Added: Rolling interval feature for collection names ([#77](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/77)) - Thanks to [@Revazashvili](https://github.com/Revazashvili)!
 * Added: Nullability annotations to the codebase
 * Updated: Latest Serilog version
 * Updated: Migrated CI/CD from AppVeyor to GitHub Actions
 * Fixed: DateTimeOffset type mapping to BSON ([#78](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/78))

## 5.1.5 (2022-04-23)
 * Version bump release

## 5.1.4 (2022-04-23)
 * Fixed: Uri and TimeSpan type mapping to BSON ([#74](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/74))

## 5.1.3 (2022-03-16)
 * Added: TTL (Time To Live) expire support for MongoDBBson sink ([#67](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/67))
 * Updated: Switched to GitVersion for versioning
 * Updated: Repository URLs and documentation

## 5.1.2 (2022-01-19)
 * Fixed: Collection validation/creation not being called when database was specified in configuration ([#68](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/68))
 * Fixed: Capped collection options not being applied ([#68](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/68))
 * Fixed: `RepositoryUrl` metadata ([#70](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/70)) - Thanks to [@behroozbc](https://github.com/behroozbc)!
 * Updated: Copyright year to 2022

## 5.1.1 (2021-09-25)
 * Fixed: Version bump to correct previous release versioning issue

## 5.1.0 (2021-09-25)
 * Fixed: V5 regression with malformed JSON output ([#66](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/66))
 * Fixed: Application startup when MongoDB is unavailable ([#37](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/37))
 * Updated: README documentation

## 5.0.0 (2021-09-01)
 * **BREAKING**: Major architectural change - logs now use native BSON serialization instead of JSON ([#60](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/60))
 * Added: JSON configuration support for sink ([#62](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/62)) - Thanks to [@behroozbc](https://github.com/behroozbc)!
 * Added: Custom MongoDB examples
 * Updated: MongoDB driver ([#65](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/65)) - Thanks to [@laitnee](https://github.com/laitnee)!
 * Fixed: Missing log level in output - now includes log level as expected
 * Removed: Legacy examples

## 4.1.0 (2021-05-05)
 * Added: Support for reserved character replacement in BSON document key fields ([#40](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/40)) - Thanks to [@IcanBENCHurCAT](https://github.com/IcanBENCHurCAT)!
 * Updated: Upgraded project to .NET Core 3.1
 * Updated: Build tooling to latest versions
 * Fixed: Capped collection not getting created - `CollectionExists` was always returning true ([#54](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/54)) - Thanks to [@karthik-v-raman](https://github.com/karthik-v-raman)!

## 4.0.0 (2018-11-09)
 * **BREAKING**: Changed method signature to accept `ITextFormatter`
 * Added: Accept `ITextFormatter` as optional constructor parameter in MongoDBSink ([#32](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/32)) - Thanks to [@giacomociti](https://github.com/giacomociti)!
 * Updated: Replaced `JsonFormatter` with `ITextFormatter` in MongoDbHelpers
 * Fixed: Error when checking if collection exists on MongoDB 3.6 ([#35](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/35)) - Thanks to [@pberggreen](https://github.com/pberggreen)!

## 3.1.0 (2016-11-02)
 * Added: Support for .NET Standard 1.5 ([#22](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/22), [#23](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/23)) - Thanks to [@kalahari](https://github.com/kalahari)!
 * Added: `CollectionName` parameter to MongoDB sink extension ([#19](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/19), [#21](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/21))
 * Added: Capability to use connection strings from config file ([#17](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/17)) - Thanks to [@ammachado](https://github.com/ammachado)!
 * Updated: Switched to `EmitAsync` ([#14](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/14), [#15](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/15))
 * Updated: NuGet packages to latest versions
 * Fixed: `CollectionCreationOptions` not being used even when provided - changed back to explicit collection creation ([#14](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/14), [#8](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/8))
 * Fixed: Race condition when collection is created externally between existence check and creation ([#25](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/25)) - Thanks to [@kalahari](https://github.com/kalahari)!
 * Code cleanup and refactoring ([#27](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/pull/27)) - Thanks to [@dsoronda](https://github.com/dsoronda)!

## 3.0.0 (2016-08-09)
 * Initial versioned release after moving from original Serilog repository

## 2.0.0
 * Moved the MongoDB sink from its [original location](https://github.com/serilog/serilog)
