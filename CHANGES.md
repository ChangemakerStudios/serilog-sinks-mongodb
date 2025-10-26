7.2.0
 * Fixed: MongoDB error handling for v7.x compatibility using error codes instead of string matching (#98, #99) - Thanks to [ntark](https://github.com/ntark)!
 * Added: Comprehensive unit tests for MongoDB error handling (#100)
 * Improved: CI workflow optimization to skip package generation during test runs

7.1.0
 * Fixed: Guid to BsonValue mapping error (#91)
 * Fixed: RollingInterval collection naming with proper formatting (#95)
 * Fixed: Renamed SetRollingInternal to SetRollingInterval (#97)

7.0.0
 * Upgraded to MongoDB.Driver 3.0 fixing incompatibilities
 * Dropped .NET Standard 2.0 support

2.0.0
 * Moved the MongoDB sink from its [original location](https://github.com/serilog/serilog)
