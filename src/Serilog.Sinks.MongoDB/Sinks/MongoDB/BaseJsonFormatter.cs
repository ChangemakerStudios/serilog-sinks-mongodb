// Copyright 2013-2015 Serilog Contributors
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;

namespace Serilog.Formatting.Json
{
    /// <summary>
    /// Formats log events in a simple JSON structure. Instances of this class
    /// are safe for concurrent access by multiple threads.
    /// </summary>
    public class BaseJsonFormatter : ITextFormatter
    {

        readonly bool _omitEnclosingObject;
        readonly string _closingDelimiter;
        readonly bool _renderMessage;
        readonly IFormatProvider _formatProvider;
        readonly IDictionary<Type, Action<object, bool, TextWriter>> _literalWriters;

        /// <summary>
        /// Construct a <see cref="BaseJsonFormatter"/>.
        /// </summary>
        /// <param name="closingDelimiter">A string that will be written after each log event is formatted.
        /// If null, <see cref="Environment.NewLine"/> will be used.</param>
        /// <param name="renderMessage">If true, the message will be rendered and written to the output as a
        /// property named RenderedMessage.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        public BaseJsonFormatter(
            string closingDelimiter = null,
            bool renderMessage = false,
            IFormatProvider formatProvider = null)
            : this(false, closingDelimiter, renderMessage, formatProvider)
        {
        }

        /// <summary>
        /// Construct a <see cref="BaseJsonFormatter"/>.
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
        public BaseJsonFormatter(
            bool omitEnclosingObject,
            string closingDelimiter = null,
            bool renderMessage = false,
            IFormatProvider formatProvider = null)
        {
            _omitEnclosingObject = omitEnclosingObject;
            _closingDelimiter = closingDelimiter ?? Environment.NewLine;
            _renderMessage = renderMessage;
            _formatProvider = formatProvider;

            _literalWriters = new Dictionary<Type, Action<object, bool, TextWriter>>
            {
                { typeof(bool), (v, q, w) => WriteBoolean((bool)v, w) },
                { typeof(char), (v, q, w) => WriteString(((char)v).ToString(), w) },
                { typeof(byte), WriteToString },
                { typeof(sbyte), WriteToString },
                { typeof(short), WriteToString },
                { typeof(ushort), WriteToString },
                { typeof(int), WriteToString },
                { typeof(uint), WriteToString },
                { typeof(long), WriteToString },
                { typeof(ulong), WriteToString },
                { typeof(float), (v, q, w) => WriteSingle((float)v, w) },
                { typeof(double), (v, q, w) => WriteDouble((double)v, w) },
                { typeof(decimal), WriteToString },
                { typeof(string), (v, q, w) => WriteString((string)v, w) },
                { typeof(DateTime), (v, q, w) => WriteDateTime((DateTime)v, w) },
                { typeof(DateTimeOffset), (v, q, w) => WriteOffset((DateTimeOffset)v, w) },
                { typeof(ScalarValue), (v, q, w) => WriteLiteral(((ScalarValue)v).Value, w, q) },
                { typeof(SequenceValue), (v, q, w) => WriteSequence(((SequenceValue)v).Elements, w) },
                { typeof(DictionaryValue), (v, q, w) => WriteDictionary(((DictionaryValue)v).Elements, w) },
                { typeof(StructureValue), (v, q, w) => WriteStructure(((StructureValue)v).TypeTag, ((StructureValue)v).Properties, w) },
            };
        }

        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            if (!_omitEnclosingObject)
                output.Write("{");

            var delim = "";
            WriteTimestamp(logEvent.Timestamp, ref delim, output);
            WriteLevel(logEvent.Level, ref delim, output);
            WriteMessageTemplate(logEvent.MessageTemplate.Text, ref delim, output);
            if (_renderMessage)
            {
                var message = logEvent.RenderMessage(_formatProvider);
                WriteRenderedMessage(message, ref delim, output);
            }

            if (logEvent.Exception != null)
                WriteException(logEvent.Exception, ref delim, output);

            if (logEvent.Properties.Count != 0)
                WriteProperties(logEvent.Properties, output);

            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null)
                .GroupBy(pt => pt.PropertyName)
                .ToArray();

            if (tokensWithFormat.Length != 0)
            {
                WriteRenderings(tokensWithFormat, logEvent.Properties, output);
            }

            if (!_omitEnclosingObject)
            {
                output.Write("}");
                output.Write(_closingDelimiter);
            }
        }

        /// <summary>
        /// Adds a writer function for a given type.
        /// </summary>
        /// <param name="type">The type of values, which <paramref name="writer" /> handles.</param>
        /// <param name="writer">The function, which writes the values.</param>
        protected void AddLiteralWriter(Type type, Action<object, TextWriter> writer)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            _literalWriters[type] = (v, _, w) => writer(v, w);
        }

        /// <summary>
        /// Writes out individual renderings of attached properties
        /// </summary>
        protected virtual void WriteRenderings(IGrouping<string, PropertyToken>[] tokensWithFormat, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            output.Write(",\"{0}\":{{", "Renderings");
            WriteRenderingsValues(tokensWithFormat, properties, output);
            output.Write("}");
        }

        /// <summary>
        /// Writes out the values of individual renderings of attached properties
        /// </summary>
        protected virtual void WriteRenderingsValues(IGrouping<string, PropertyToken>[] tokensWithFormat, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            var rdelim = "";
            foreach (var ptoken in tokensWithFormat)
            {
                output.Write(rdelim);
                rdelim = ",";
                output.Write("\"");
                output.Write(ptoken.Key);
                output.Write("\":[");

                var fdelim = "";
                foreach (var format in ptoken)
                {
                    output.Write(fdelim);
                    fdelim = ",";

                    output.Write("{");
                    var eldelim = "";

                    WriteJsonProperty("Format", format.Format, ref eldelim, output);

                    var sw = new StringWriter();
                    MessageTemplateRenderer.RenderPropertyToken(format, properties, sw, _formatProvider, true, false);
                    WriteJsonProperty("Rendering", sw.ToString(), ref eldelim, output);

                    output.Write("}");
                }

                output.Write("]");
            }
        }

        /// <summary>
        /// Writes out the attached properties
        /// </summary>
        protected virtual void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            output.Write(",\"{0}\":{{", "Properties");
            WritePropertiesValues(properties, output);
            output.Write("}");
        }

        /// <summary>
        /// Writes out the attached properties values
        /// </summary>
        protected virtual void WritePropertiesValues(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            var precedingDelimiter = "";
            foreach (var property in properties)
            {
                WriteJsonProperty(property.Key, property.Value, ref precedingDelimiter, output);
            }
        }

        /// <summary>
        /// Writes out the attached exception
        /// </summary>
        protected virtual void WriteException(Exception exception, ref string delim, TextWriter output)
        {
            WriteJsonProperty("Exception", exception, ref delim, output);
        }

        /// <summary>
        /// (Optionally) writes out the rendered message
        /// </summary>
        protected virtual void WriteRenderedMessage(string message, ref string delim, TextWriter output)
        {
            WriteJsonProperty("RenderedMessage", message, ref delim, output);
        }

        /// <summary>
        /// Writes out the message template for the logevent.
        /// </summary>
        protected virtual void WriteMessageTemplate(string template, ref string delim, TextWriter output)
        {
            WriteJsonProperty("MessageTemplate", template, ref delim, output);
        }

        /// <summary>
        /// Writes out the log level
        /// </summary>
        protected virtual void WriteLevel(LogEventLevel level, ref string delim, TextWriter output)
        {
            WriteJsonProperty("Level", level, ref delim, output);
        }

        /// <summary>
        /// Writes out the log timestamp
        /// </summary>
        protected virtual void WriteTimestamp(DateTimeOffset timestamp, ref string delim, TextWriter output)
        {
            WriteJsonProperty("Timestamp", timestamp, ref delim, output);
        }

        /// <summary>
        /// Writes out a structure property
        /// </summary>
        protected virtual void WriteStructure(string typeTag, IEnumerable<LogEventProperty> properties, TextWriter output)
        {
            output.Write("{");

            var delim = "";
            if (typeTag != null)
                WriteJsonProperty("_typeTag", typeTag, ref delim, output);

            foreach (var property in properties)
                WriteJsonProperty(property.Name, property.Value, ref delim, output);

            output.Write("}");
        }

        /// <summary>
        /// Writes out a sequence property
        /// </summary>
        protected virtual void WriteSequence(IEnumerable elements, TextWriter output)
        {
            output.Write("[");
            var delim = "";
            foreach (var value in elements)
            {
                output.Write(delim);
                delim = ",";
                WriteLiteral(value, output);
            }
            output.Write("]");
        }

        /// <summary>
        /// Writes out a dictionary
        /// </summary>
        protected virtual void WriteDictionary(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements, TextWriter output)
        {
            output.Write("{");
            var delim = "";
            foreach (var e in elements)
            {
                output.Write(delim);
                delim = ",";
                WriteLiteral(e.Key, output, true);
                output.Write(":");
                WriteLiteral(e.Value, output);
            }
            output.Write("}");
        }

        /// <summary>
        /// Writes out a json property with the specified value on output writer
        /// </summary>
        protected virtual void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
        {
            output.Write(precedingDelimiter);
            output.Write("\"");
            output.Write(name);
            output.Write("\":");
            WriteLiteral(value, output);
            precedingDelimiter = ",";
        }

        /// <summary>
        /// Allows a subclass to write out objects that have no configured literal writer.
        /// </summary>
        /// <param name="value">The value to be written as a json construct</param>
        /// <param name="output">The writer to write on</param>
        protected virtual void WriteLiteralValue(object value, TextWriter output)
        {
            WriteString(value.ToString(), output);
        }

        protected void WriteLiteral(object value, TextWriter output, bool forceQuotation = false)
        {
            if (value == null)
            {
                output.Write("null");
                return;
            }

            Action<object, bool, TextWriter> writer;
            if (_literalWriters.TryGetValue(value.GetType(), out writer))
            {
                writer(value, forceQuotation, output);
                return;
            }

            WriteLiteralValue(value, output);
        }

        static void WriteToString(object number, bool quote, TextWriter output)
        {
            if (quote) output.Write('"');

            var fmt = number as IFormattable;
            if (fmt != null)
                output.Write(fmt.ToString(null, CultureInfo.InvariantCulture));
            else
                output.Write(number.ToString());

            if (quote) output.Write('"');
        }

        static void WriteBoolean(bool value, TextWriter output)
        {
            output.Write(value ? "true" : "false");
        }

        static void WriteSingle(float value, TextWriter output)
        {
            output.Write(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteDouble(double value, TextWriter output)
        {
            output.Write(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteOffset(DateTimeOffset value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }

        static void WriteDateTime(DateTime value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }

        static void WriteString(string value, TextWriter output)
        {
            value = value.Replace("$", "_").Replace(".", "-");
            JsonValueFormatter.WriteQuotedJsonString(value, output);
        }

        /// <summary>
        /// Perform simple JSON string escaping on <paramref name="s"/>.
        /// </summary>
        /// <param name="s">A raw string.</param>
        /// <returns>A JSON-escaped version of <paramref name="s"/>.</returns>
        public static string Escape(string s)
        {
            if (s == null) return null;

            var escapedResult = new StringWriter();
            JsonValueFormatter.WriteQuotedJsonString(s, escapedResult);
            var quoted = escapedResult.ToString();
            return quoted.Substring(1, quoted.Length - 2);
        }
    }

    /// <summary>
    /// A message template token representing a log event property.
    /// </summary>
    public sealed class PropertyToken : MessageTemplateToken
    {
        readonly string _rawText;
        readonly int? _position;

        /// <summary>
        /// Construct a <see cref="PropertyToken"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="rawText">The token as it appears in the message template.</param>
        /// <param name="formatObsolete">The format applied to the property, if any.</param>
        /// <param name="destructuringObsolete">The destructuring strategy applied to the property, if any.</param>
        /// <exception cref="ArgumentNullException"></exception>
        [Obsolete("Use named arguments with this method to guarantee forwards-compatibility."), EditorBrowsable(EditorBrowsableState.Never)]
        public PropertyToken(string propertyName, string rawText, string formatObsolete, Destructuring destructuringObsolete)
            : this(propertyName, rawText, formatObsolete, null, destructuringObsolete)
        {
        }

        /// <summary>
        /// Construct a <see cref="PropertyToken"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="rawText">The token as it appears in the message template.</param>
        /// <param name="format">The format applied to the property, if any.</param>
        /// <param name="alignment">The alignment applied to the property, if any.</param>
        /// <param name="destructuring">The destructuring strategy applied to the property, if any.</param>
        /// <param name="startIndex">The token's start index in the template.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PropertyToken(string propertyName, string rawText, string format = null, Alignment? alignment = null, Destructuring destructuring = Destructuring.Default, int startIndex = -1)
            : base(startIndex)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Format = format;
            Destructuring = destructuring;
            _rawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
            Alignment = alignment;

            int position;
            if (int.TryParse(PropertyName, NumberStyles.None, CultureInfo.InvariantCulture, out position) &&
                position >= 0)
            {
                _position = position;
            }
        }

        /// <summary>
        /// The token's length.
        /// </summary>
        public override int Length => _rawText.Length;

        /// <summary>
        /// Render the token to the output.
        /// </summary>
        /// <param name="properties">Properties that may be represented by the token.</param>
        /// <param name="output">Output for the rendered string.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        public override void Render(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output, IFormatProvider formatProvider = null)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (output == null) throw new ArgumentNullException(nameof(output));

            MessageTemplateRenderer.RenderPropertyToken(this, properties, output, formatProvider, false, false);
        }

        /// <summary>
        /// The property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Destructuring strategy applied to the property.
        /// </summary>
        public Destructuring Destructuring { get; }

        /// <summary>
        /// Format applied to the property.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// Alignment applied to the property.
        /// </summary>
        public Alignment? Alignment { get; }

        /// <summary>
        /// True if the property name is a positional index; otherwise, false.
        /// </summary>
        public bool IsPositional => _position.HasValue;

        internal string RawText => _rawText;

        /// <summary>
        /// Try to get the integer value represented by the property name.
        /// </summary>
        /// <param name="position">The integer value, if present.</param>
        /// <returns>True if the property is positional, otherwise false.</returns>
        public bool TryGetPositionalValue(out int position)
        {
            if (_position == null)
            {
                position = 0;
                return false;
            }

            position = _position.Value;
            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var pt = obj as PropertyToken;
            return pt != null &&
                pt.Destructuring == Destructuring &&
                pt.Format == Format &&
                pt.PropertyName == PropertyName &&
                pt._rawText == _rawText;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() => PropertyName.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => _rawText;
    }

    static class Padding
    {
        static readonly char[] PaddingChars = Enumerable.Repeat(' ', 80).ToArray();

        /// <summary>
        /// Writes the provided value to the output, applying direction-based padding when <paramref name="alignment"/> is provided.
        /// </summary>
        public static void Apply(TextWriter output, string value, Alignment? alignment)
        {
            if (!alignment.HasValue || value.Length >= alignment.Value.Width)
            {
                output.Write(value);
                return;
            }

            var pad = alignment.Value.Width - value.Length;

            if (alignment.Value.Direction == AlignmentDirection.Left)
                output.Write(value);

            if (pad <= PaddingChars.Length)
            {
                output.Write(PaddingChars, 0, pad);
            }
            else
            {
                output.Write(new string(' ', pad));
            }

            if (alignment.Value.Direction == AlignmentDirection.Right)
                output.Write(value);
        }
    }

    static class MessageTemplateRenderer
    {
        static readonly JsonValueFormatter JsonValueFormatter = new JsonValueFormatter("$type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Render(MessageTemplate messageTemplate, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output, string format = null, IFormatProvider formatProvider = null)
        {
            bool isLiteral = false, isJson = false;

            if (format != null)
            {
                for (var i = 0; i < format.Length; ++i)
                {
                    if (format[i] == 'l')
                        isLiteral = true;
                    else if (format[i] == 'j')
                        isJson = true;
                }
            }

            for (var ti = 0; ti < messageTemplate.TokenArray.Length; ++ti)
            {
                var token = messageTemplate.TokenArray[ti];
                if (token is TextToken tt)
                {
                    RenderTextToken(tt, output);
                }
                else
                {
                    var pt = (PropertyToken)token;
                    RenderPropertyToken(pt, properties, output, formatProvider, isLiteral, isJson);
                }
            }
        }

        public static void RenderTextToken(TextToken tt, TextWriter output)
        {
            output.Write(tt.Text);
        }

        public static void RenderPropertyToken(PropertyToken pt, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output, IFormatProvider formatProvider, bool isLiteral, bool isJson)
        {
            LogEventPropertyValue propertyValue;
            if (!properties.TryGetValue(pt.PropertyName, out propertyValue))
            {
                output.Write(pt.RawText);
                return;
            }

            if (!pt.Alignment.HasValue)
            {
                RenderValue(propertyValue, isLiteral, isJson, output, pt.Format, formatProvider);
                return;
            }

            var valueOutput = new StringWriter();
            RenderValue(propertyValue, isLiteral, isJson, valueOutput, pt.Format, formatProvider);
            var value = valueOutput.ToString();

            if (value.Length >= pt.Alignment.Value.Width)
            {
                output.Write(value);
                return;
            }

            Padding.Apply(output, value, pt.Alignment.Value);
        }

        static void RenderValue(LogEventPropertyValue propertyValue, bool literal, bool json, TextWriter output, string format, IFormatProvider formatProvider)
        {
            if (literal && propertyValue is ScalarValue sv && sv.Value is string str)
            {
                output.Write(str);
            }
            else if (json)
            {
                JsonValueFormatter.Format(propertyValue, output);
            }
            else
            {
                propertyValue.Render(output, format, formatProvider);
            }
        }
    }
}