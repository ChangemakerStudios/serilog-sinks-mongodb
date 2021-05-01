using System.Linq;

using MongoDB.Bson;

using Serilog.Events;

namespace Serilog.Helpers
{
    internal static class MongoDbDocumentHelpers
    {
        /// <summary>
        ///     Credit to @IvaskevychYuriy for this excellent solution:
        ///     https://github.com/serilog/serilog-sinks-mongodb/pull/59#issuecomment-830079904
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