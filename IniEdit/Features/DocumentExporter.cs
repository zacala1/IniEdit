using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace IniEdit
{
    /// <summary>
    /// Provides methods to export INI documents to various formats.
    /// </summary>
    public static class DocumentExporter
    {
        #region JSON Export

        /// <summary>
        /// Exports a document to JSON format.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="options">Optional JSON export options.</param>
        /// <returns>A JSON string representation of the document.</returns>
        public static string ToJson(Document document, JsonExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            options ??= new JsonExportOptions();

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = options.Indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            WriteDocumentAsJson(writer, document, options);

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Exports a document to a JSON file.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="options">Optional JSON export options.</param>
        public static void ToJsonFile(Document document, string filePath, JsonExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var json = ToJson(document, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private static void WriteDocumentAsJson(Utf8JsonWriter writer, Document document, JsonExportOptions options)
        {
            writer.WriteStartObject();

            // Write default section properties at root level or under "_default"
            if (document.DefaultSection.PropertyCount > 0)
            {
                if (options.FlattenDefaultSection)
                {
                    foreach (var property in document.DefaultSection.GetProperties())
                    {
                        WritePropertyAsJson(writer, property, options);
                    }
                }
                else
                {
                    writer.WritePropertyName("_default");
                    WriteSectionAsJson(writer, document.DefaultSection, options);
                }
            }

            // Write named sections
            foreach (var section in document)
            {
                writer.WritePropertyName(section.Name);
                WriteSectionAsJson(writer, section, options);
            }

            writer.WriteEndObject();
        }

        private static void WriteSectionAsJson(Utf8JsonWriter writer, Section section, JsonExportOptions options)
        {
            writer.WriteStartObject();

            if (options.IncludeComments && section.PreComments.Count > 0)
            {
                writer.WritePropertyName("_preComments");
                writer.WriteStartArray();
                foreach (var comment in section.PreComments)
                {
                    writer.WriteStringValue(comment.Value);
                }
                writer.WriteEndArray();
            }

            if (options.IncludeComments && section.Comment != null)
            {
                writer.WriteString("_comment", section.Comment.Value);
            }

            foreach (var property in section.GetProperties())
            {
                WritePropertyAsJson(writer, property, options);
            }

            writer.WriteEndObject();
        }

        private static void WritePropertyAsJson(Utf8JsonWriter writer, Property property, JsonExportOptions options)
        {
            if (options.IncludeComments && (property.PreComments.Count > 0 || property.Comment != null))
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartObject();
                writer.WriteString("value", property.Value);

                if (property.PreComments.Count > 0)
                {
                    writer.WritePropertyName("preComments");
                    writer.WriteStartArray();
                    foreach (var comment in property.PreComments)
                    {
                        writer.WriteStringValue(comment.Value);
                    }
                    writer.WriteEndArray();
                }

                if (property.Comment != null)
                {
                    writer.WriteString("comment", property.Comment.Value);
                }

                writer.WriteEndObject();
            }
            else
            {
                // Auto-convert types if enabled
                if (options.AutoConvertTypes)
                {
                    WriteAutoTypedValue(writer, property.Name, property.Value);
                }
                else
                {
                    writer.WriteString(property.Name, property.Value);
                }
            }
        }

        private static void WriteAutoTypedValue(Utf8JsonWriter writer, string name, string value)
        {
            // Try boolean
            if (bool.TryParse(value, out var boolVal))
            {
                writer.WriteBoolean(name, boolVal);
                return;
            }
            if (value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteBoolean(name, true);
                return;
            }
            if (value == "0" || value.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteBoolean(name, false);
                return;
            }

            // Try integer
            if (long.TryParse(value, out var longVal))
            {
                writer.WriteNumber(name, longVal);
                return;
            }

            // Try decimal
            if (double.TryParse(value, out var doubleVal))
            {
                writer.WriteNumber(name, doubleVal);
                return;
            }

            // Default to string
            writer.WriteString(name, value);
        }

        #endregion

        #region XML Export

        /// <summary>
        /// Exports a document to XML format.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="options">Optional XML export options.</param>
        /// <returns>An XML string representation of the document.</returns>
        public static string ToXml(Document document, XmlExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            options ??= new XmlExportOptions();

            var settings = new XmlWriterSettings
            {
                Indent = options.Indented,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = !options.IncludeXmlDeclaration
            };

            using var stringWriter = new StringWriter();
            using (var writer = XmlWriter.Create(stringWriter, settings))
            {
                WriteDocumentAsXml(writer, document, options);
            }

            return stringWriter.ToString();
        }

        /// <summary>
        /// Exports a document to an XML file.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="options">Optional XML export options.</param>
        public static void ToXmlFile(Document document, string filePath, XmlExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var xml = ToXml(document, options);
            File.WriteAllText(filePath, xml, Encoding.UTF8);
        }

        private static void WriteDocumentAsXml(XmlWriter writer, Document document, XmlExportOptions options)
        {
            writer.WriteStartElement(options.RootElementName);

            // Write default section
            if (document.DefaultSection.PropertyCount > 0)
            {
                WriteSectionAsXml(writer, document.DefaultSection, "_default", options);
            }

            // Write named sections
            foreach (var section in document)
            {
                WriteSectionAsXml(writer, section, section.Name, options);
            }

            writer.WriteEndElement();
        }

        private static void WriteSectionAsXml(XmlWriter writer, Section section, string elementName, XmlExportOptions options)
        {
            var safeElementName = MakeValidXmlName(elementName);
            writer.WriteStartElement(options.SectionElementName);
            writer.WriteAttributeString("name", elementName);

            if (options.IncludeComments)
            {
                foreach (var comment in section.PreComments)
                {
                    writer.WriteComment(comment.Value);
                }

                if (section.Comment != null)
                {
                    writer.WriteComment($"Inline: {section.Comment.Value}");
                }
            }

            foreach (var property in section.GetProperties())
            {
                WritePropertyAsXml(writer, property, options);
            }

            writer.WriteEndElement();
        }

        private static void WritePropertyAsXml(XmlWriter writer, Property property, XmlExportOptions options)
        {
            if (options.IncludeComments)
            {
                foreach (var comment in property.PreComments)
                {
                    writer.WriteComment(comment.Value);
                }
            }

            writer.WriteStartElement(options.PropertyElementName);
            writer.WriteAttributeString("key", property.Name);

            if (options.UseAttributeForValue)
            {
                writer.WriteAttributeString("value", property.Value);
            }
            else
            {
                writer.WriteString(property.Value);
            }

            if (options.IncludeComments && property.Comment != null)
            {
                writer.WriteAttributeString("comment", property.Comment.Value);
            }

            writer.WriteEndElement();
        }

        private static string MakeValidXmlName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_";

            var sb = new StringBuilder();
            char first = name[0];

            // First character must be letter or underscore
            if (char.IsLetter(first) || first == '_')
                sb.Append(first);
            else
                sb.Append('_');

            // Subsequent characters can be letters, digits, hyphens, underscores, or periods
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            return sb.ToString();
        }

        #endregion

        #region CSV Export

        /// <summary>
        /// Exports a document to CSV format.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="options">Optional CSV export options.</param>
        /// <returns>A CSV string representation of the document.</returns>
        public static string ToCsv(Document document, CsvExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            options ??= new CsvExportOptions();

            var sb = new StringBuilder();

            // Write header
            if (options.IncludeHeader)
            {
                var headers = new List<string> { "Section", "Key", "Value" };
                if (options.IncludeComments)
                {
                    headers.Add("Comment");
                }
                sb.AppendLine(string.Join(options.Delimiter.ToString(), headers));
            }

            // Write default section properties
            foreach (var property in document.DefaultSection.GetProperties())
            {
                WritePropertyAsCsv(sb, "", property, options);
            }

            // Write named section properties
            foreach (var section in document)
            {
                foreach (var property in section.GetProperties())
                {
                    WritePropertyAsCsv(sb, section.Name, property, options);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Exports a document to a CSV file.
        /// </summary>
        /// <param name="document">The document to export.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="options">Optional CSV export options.</param>
        public static void ToCsvFile(Document document, string filePath, CsvExportOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var csv = ToCsv(document, options);
            File.WriteAllText(filePath, csv, options?.Encoding ?? Encoding.UTF8);
        }

        private static void WritePropertyAsCsv(StringBuilder sb, string sectionName, Property property, CsvExportOptions options)
        {
            var fields = new List<string>
            {
                EscapeCsvField(sectionName, options),
                EscapeCsvField(property.Name, options),
                EscapeCsvField(property.Value, options)
            };

            if (options.IncludeComments)
            {
                var commentText = property.Comment?.Value ?? "";
                fields.Add(EscapeCsvField(commentText, options));
            }

            sb.AppendLine(string.Join(options.Delimiter.ToString(), fields));
        }

        private static string EscapeCsvField(string value, CsvExportOptions options)
        {
            if (string.IsNullOrEmpty(value))
                return options.AlwaysQuote ? "\"\"" : "";

            bool needsQuoting = options.AlwaysQuote ||
                                value.Contains(options.Delimiter) ||
                                value.Contains('"') ||
                                value.Contains('\r') ||
                                value.Contains('\n');

            if (!needsQuoting)
                return value;

            // Escape double quotes by doubling them
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        #endregion
    }

    #region Export Options

    /// <summary>
    /// Options for JSON export.
    /// </summary>
    public sealed class JsonExportOptions
    {
        /// <summary>
        /// Gets or sets whether to indent the output.
        /// </summary>
        public bool Indented { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include comments in the output.
        /// </summary>
        public bool IncludeComments { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to flatten default section properties to root level.
        /// </summary>
        public bool FlattenDefaultSection { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to automatically convert string values to appropriate JSON types.
        /// </summary>
        public bool AutoConvertTypes { get; set; } = false;
    }

    /// <summary>
    /// Options for XML export.
    /// </summary>
    public sealed class XmlExportOptions
    {
        /// <summary>
        /// Gets or sets whether to indent the output.
        /// </summary>
        public bool Indented { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include the XML declaration.
        /// </summary>
        public bool IncludeXmlDeclaration { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include comments in the output.
        /// </summary>
        public bool IncludeComments { get; set; } = false;

        /// <summary>
        /// Gets or sets the root element name.
        /// </summary>
        public string RootElementName { get; set; } = "configuration";

        /// <summary>
        /// Gets or sets the section element name.
        /// </summary>
        public string SectionElementName { get; set; } = "section";

        /// <summary>
        /// Gets or sets the property element name.
        /// </summary>
        public string PropertyElementName { get; set; } = "property";

        /// <summary>
        /// Gets or sets whether to use an attribute for the value instead of element content.
        /// </summary>
        public bool UseAttributeForValue { get; set; } = false;
    }

    /// <summary>
    /// Options for CSV export.
    /// </summary>
    public sealed class CsvExportOptions
    {
        /// <summary>
        /// Gets or sets the field delimiter.
        /// </summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// Gets or sets whether to include a header row.
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include comments.
        /// </summary>
        public bool IncludeComments { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to always quote fields.
        /// </summary>
        public bool AlwaysQuote { get; set; } = false;

        /// <summary>
        /// Gets or sets the encoding for file output.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }

    #endregion
}
