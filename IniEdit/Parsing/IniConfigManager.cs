using System.Buffers;
using System.Text;
using static IniEdit.IniConfigOption;

namespace IniEdit
{
    /// <summary>
    /// Provides static methods for loading and saving INI configuration files.
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe. Multiple threads loading files concurrently
    /// should use separate instances or external synchronization.
    /// Use <see cref="IniConfigOption.CollectParsingErrors"/> to collect parsing errors
    /// instead of relying on events in multi-threaded scenarios.
    /// </remarks>
    public static partial class IniConfigManager
    {
        private const int BufferSize = 4096;

        /// <summary>
        /// Occurs when a parsing error is encountered during file loading.
        /// </summary>
        public static event EventHandler<ParsingErrorEventArgs>? ParsingError;

        /// <summary>
        /// Special characters that require quoting in property values.
        /// </summary>
        private static readonly char[] SpecialCharsRequiringQuotes = new[] { ';', '#', '\r', '\n', '\t', '\0', '\a', '\b', '\\', '"' };

        /// <summary>
        /// Reports a parsing error by invoking the event and optionally collecting the error.
        /// </summary>
        private static void ReportError(Document doc, IniConfigOption option, int lineNumber, string line, string reason)
        {
            var error = new ParsingErrorEventArgs(lineNumber, line, reason);
            ParsingError?.Invoke(null, error);
            if (option.CollectParsingErrors)
                doc.AddParsingError(error);
        }

        /// <summary>
        /// Truncates a line for error reporting (max 100 chars).
        /// </summary>
        private static string TruncateLine(string line, int maxLength = 100)
            => line.Length > maxLength ? line.Substring(0, maxLength) + "..." : line;

        /// <summary>
        /// Checks if a property value needs to be quoted to preserve special characters.
        /// </summary>
        private static bool NeedsQuoting(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Check for special characters that require quoting
            if (value.IndexOfAny(SpecialCharsRequiringQuotes) >= 0)
                return true;

            // Check for leading or trailing whitespace
            if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
                return true;

            return false;
        }

        /// <summary>
        /// Writes an escaped value to the TextWriter for quoted properties.
        /// Optimized to avoid string allocations for each character.
        /// </summary>
        private static void WriteEscapedValue(TextWriter writer, string value)
        {
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\0':
                        writer.Write("\\0");
                        break;
                    case '\a':
                        writer.Write("\\a");
                        break;
                    case '\b':
                        writer.Write("\\b");
                        break;
                    case '\t':
                        writer.Write("\\t");
                        break;
                    case '\r':
                        writer.Write("\\r");
                        break;
                    case '\n':
                        writer.Write("\\n");
                        break;
                    case ';':
                        writer.Write("\\;");
                        break;
                    case '#':
                        writer.Write("\\#");
                        break;
                    case '"':
                        writer.Write("\\\"");
                        break;
                    case '\\':
                        writer.Write("\\\\");
                        break;
                    default:
                        writer.Write(c);
                        break; // No string allocation
                }
            }
        }

        public static Document Load(string filePath, IniConfigOption? option = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Load(fileStream, Encoding.UTF8, option);
        }

        public static Document Load(string filePath, Encoding encoding, IniConfigOption? option = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Load(fileStream, encoding, option);
        }

        public static Document Load(Stream stream, Encoding encoding, IniConfigOption? option = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));
            if (option == null)
                option = new IniConfigOption();

            var doc = new Document(option);
            using var reader = new StreamReader(stream, encoding, true, BufferSize, leaveOpen: true);
            {

                Section currentSection = doc.DefaultSection;
                var pendingComments = new List<Comment>();
                string? line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    // Check line length limit
                    if (option.MaxLineLength > 0 && line.Length > option.MaxLineLength)
                    {
                        ReportError(doc, option, lineNumber, TruncateLine(line), $"Line exceeds maximum length ({option.MaxLineLength} characters)");
                        continue;
                    }

                    var span = line.AsSpan().Trim();
                    if (span.IsEmpty)
                        continue;

                    var commentSign = span.IndexOfAny(doc.CommentPrefixChars);
                    if (commentSign == 0)
                    {
                        var commentString = span.Slice(1).ToString();
                        // Enforce MaxPendingComments limit (FIFO - remove oldest when full)
                        if (option.MaxPendingComments > 0 && pendingComments.Count >= option.MaxPendingComments)
                        {
                            pendingComments.RemoveAt(0);
                        }
                        pendingComments.Add(new Comment(commentString));
                        continue;
                    }

                    if (span[0] == '[')
                    {
                        var closeBracket = span.IndexOf(']');
                        if (closeBracket == -1)
                        {
                            ReportError(doc, option, lineNumber, line, "Missing closing bracket in section declaration");
                            continue;
                        }

                        var sectionName = span.Slice(1, closeBracket - 1).Trim().ToString();

                        if (string.IsNullOrWhiteSpace(sectionName))
                        {
                            ReportError(doc, option, lineNumber, line, "Section name cannot be empty");
                            continue;
                        }

                        try
                        {
                            currentSection = new Section(sectionName);
                        }
                        catch (ArgumentException ex)
                        {
                            var error = new ParsingErrorEventArgs(lineNumber, line, $"Invalid section name: {ex.Message}");
                            if (option.CollectParsingErrors)
                                doc.AddParsingError(error);
                            continue;
                        }

                        if (pendingComments.Count > 0)
                        {
                            currentSection.PreComments.AddRange(pendingComments);
                            pendingComments.Clear();
                        }

                        var afterSection = span.Slice(closeBracket + 1).TrimStart();
                        commentSign = afterSection.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign == 0)
                        {
                            var commentString = afterSection.Slice(1).ToString();
                            currentSection.Comment = new Comment(commentString);
                        }

                        // Check section limit
                        if (option.MaxSections > 0 && doc.SectionCount >= option.MaxSections)
                        {
                            ReportError(doc, option, lineNumber, line, $"Maximum section limit ({option.MaxSections}) exceeded");
                            continue;
                        }

                        doc.AddSectionInternal(currentSection);
                        continue;
                    }

                    var equalSign = span.IndexOf('=');
                    if (equalSign == -1)
                    {
                        ReportError(doc, option, lineNumber, line, "Missing equals sign in key-value pair");
                        continue;
                    }

                    var keyName = span.Slice(0, equalSign).Trim().ToString();
                    if (string.IsNullOrEmpty(keyName))
                    {
                        ReportError(doc, option, lineNumber, line, "Key is empty");
                        continue;
                    }

                    var valueStart = span.Slice(equalSign + 1).TrimStart();
                    bool isQuoted = false;
                    string value, comment = string.Empty;

                    if (valueStart.IsEmpty)
                    {
                        value = string.Empty;
                        comment = string.Empty;
                    }
                    else if (valueStart[0] == '"')
                    {
                        isQuoted = true;
                        bool isEscaped = false;
                        bool isTerminated = false;
                        StringBuilder sb = new StringBuilder(valueStart.Length);
                        var remains = valueStart.Slice(1);
                        while (remains.Length > 0)
                        {
                            if (isEscaped)
                            {
                                isEscaped = false;
                                var escapeChar = remains[0] switch
                                {
                                    '0' => '\0',  // null
                                    'a' => '\a',  // bell
                                    'b' => '\b',  // backspace
                                    't' => '\t',  // tab
                                    'r' => '\r',  // carriage return
                                    'n' => '\n',  // newline
                                    ';' => ';',   // semicolon
                                    '#' => '#',   // hash
                                    '"' => '"',   // quote
                                    '\\' => '\\', // backslash
                                    _ => remains[0]
                                };
                                sb.Append(escapeChar);
                            }
                            else
                            {
                                if (remains[0] == '"')
                                {
                                    remains = remains.Slice(1);
                                    isTerminated = true;
                                    break;
                                }
                                else if (remains[0] == '\\')
                                {
                                    isEscaped = true;
                                    remains = remains.Slice(1);
                                    continue;
                                }
                                sb.Append(remains[0]);
                            }
                            remains = remains.Slice(1);
                        }
                        if (isEscaped)
                        {
                            ReportError(doc, option, lineNumber, line, "Invalid escape sequence: incomplete escape marker");
                            continue;
                        }
                        if (!isTerminated)
                        {
                            ReportError(doc, option, lineNumber, line, "Unterminated quote: missing closing quotation mark");
                            continue;
                        }

                        value = sb.ToString();

                        // Check for inline comment
                        remains = remains.TrimStart();
                        commentSign = remains.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign == 0)
                        {
                            remains = remains.Slice(1);
                            comment = remains.ToString();
                            remains = [];
                        }
                        else if (commentSign > 0)
                        {
                            ReportError(doc, option, lineNumber, line, "Invalid content after closing quote");
                            continue;
                        }

                        remains = remains.Trim();
                        if (remains.Length != 0)
                        {
                            ReportError(doc, option, lineNumber, line, "Invalid quote format");
                            continue;
                        }
                    }
                    else
                    {
                        // Check for inline comment
                        commentSign = valueStart.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign >= 0)
                        {
                            value = valueStart.Slice(0, commentSign).TrimEnd().ToString();
                            comment = valueStart.Slice(commentSign + 1).ToString();
                        }
                        else
                        {
                            value = valueStart.TrimEnd().ToString();
                            comment = string.Empty;
                        }
                    }

                    // Check value length limit
                    if (option.MaxValueLength > 0 && value.Length > option.MaxValueLength)
                    {
                        ReportError(doc, option, lineNumber, line, $"Value length ({value.Length}) exceeds maximum ({option.MaxValueLength})");
                        continue;
                    }

                    // Check property count limit
                    if (option.MaxPropertiesPerSection > 0 && currentSection.PropertyCount >= option.MaxPropertiesPerSection)
                    {
                        ReportError(doc, option, lineNumber, line, $"Maximum properties per section ({option.MaxPropertiesPerSection}) exceeded");
                        continue;
                    }

                    var property = new Property(keyName, value);
                    property.IsQuoted = isQuoted;

                    if (pendingComments.Count > 0)
                    {
                        property.PreComments.AddRange(pendingComments);
                        pendingComments.Clear();
                    }

                    if (!string.IsNullOrEmpty(comment))
                    {
                        property.Comment = new Comment(comment);
                    }

                    currentSection.AddProperty(property);
                }
            }

            // Remove null values (defensive check)
            doc.GetInternalSections().RemoveAll(x => x == null);
            foreach (Section section in doc)
            {
                section.GetInternalProperties().RemoveAll(x => x == null);
            }

            // Apply policies - RebuildSectionLookup is called once at the end
            bool needsRebuild = false;
            if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.ThrowError)
            {
                ThrowDuplicateSectionExist(doc.GetInternalSections());
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.FirstWin)
            {
                DeduplicateSectionOnFirstWin(doc.GetInternalSections());
                needsRebuild = true;
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.LastWin)
            {
                DeduplicateSectionOnLastWin(doc.GetInternalSections());
                needsRebuild = true;
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.Merge)
            {
                DeduplicateSectionOnMerging(doc.GetInternalSections(), option.DuplicateKeyPolicy);
                needsRebuild = true;
            }

            if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.ThrowError)
            {
                ThrowDuplicatePropertyExist(doc);
            }
            else if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.FirstWin)
            {
                DeduplicatePropertyOnFirstWin(doc);
            }
            else if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.LastWin)
            {
                DeduplicatePropertyOnLastWin(doc);
            }

            // Single rebuild at the end for all section modifications
            if (needsRebuild)
            {
                doc.RebuildSectionLookup();
            }

            return doc;
        }

        private static void ThrowDuplicateSectionExist(List<Section> sections)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (var section in sections)
            {
                if (section == null)
                    continue;
                if (!seen.Add(section.Name))
                {
                    throw new InvalidOperationException($"Duplicate section name '{section.Name}' found");
                }
            }
        }

        private static void DeduplicateSectionOnFirstWin(List<Section> sections)
        {
            HashSet<string> seen = new HashSet<string>();
            sections.RemoveAll(section => !seen.Add(section.Name));
        }

        private static void DeduplicateSectionOnLastWin(List<Section> sections)
        {
            // Optimized O(n) algorithm: build new list instead of RemoveAt (which is O(n) per call)
            var lastOccurrence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // First pass: find last occurrence index for each section name
            for (int i = 0; i < sections.Count; i++)
            {
                lastOccurrence[sections[i].Name] = i;
            }

            // Second pass: keep only items at their last occurrence index
            int writeIndex = 0;
            for (int i = 0; i < sections.Count; i++)
            {
                if (lastOccurrence[sections[i].Name] == i)
                {
                    sections[writeIndex++] = sections[i];
                }
            }

            // Remove trailing items
            sections.RemoveRange(writeIndex, sections.Count - writeIndex);
        }

        private static void DeduplicateSectionOnMerging(List<Section> sections, DuplicateKeyPolicyType policy = DuplicateKeyPolicyType.FirstWin)
        {
            // Optimized O(n) algorithm using dictionary for lookups
            var seen = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
            int writeIndex = 0;

            for (int i = 0; i < sections.Count; i++)
            {
                if (seen.TryGetValue(sections[i].Name, out var existing))
                {
                    // Merge into existing section
                    existing.MergeFrom(sections[i], policy);
                }
                else
                {
                    // First occurrence, keep it
                    seen[sections[i].Name] = sections[i];
                    sections[writeIndex++] = sections[i];
                }
            }

            // Remove the trailing items that were merged
            sections.RemoveRange(writeIndex, sections.Count - writeIndex);
        }

        private static void ThrowDuplicatePropertyExist(IEnumerable<Section> sections)
        {
            foreach (var section in sections)
            {
                if (section == null)
                    continue;
                HashSet<string> seen = new HashSet<string>();
                foreach (var property in section)
                {
                    if (property == null)
                        continue;
                    if (!seen.Add(property.Name))
                    {
                        throw new InvalidOperationException($"Duplicate property name '{property.Name}' found in section '{section.Name}'. Each property name must be unique within a section.");
                    }
                }
            }
        }

        private static void DeduplicatePropertyOnFirstWin(IEnumerable<Section> sections)
        {
            foreach (var section in sections)
            {
                HashSet<string> seen = new HashSet<string>();
                var properties = section.GetInternalProperties();
                properties.RemoveAll(section => !seen.Add(section.Name));
            }
        }

        private static void DeduplicatePropertyOnLastWin(IEnumerable<Section> sections)
        {
            foreach (var section in sections)
            {
                var properties = section.GetInternalProperties();

                // Optimized O(n) algorithm: same pattern as DeduplicateSectionOnLastWin
                var lastOccurrence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                // First pass: find last occurrence index for each property name
                for (int i = 0; i < properties.Count; i++)
                {
                    lastOccurrence[properties[i].Name] = i;
                }

                // Second pass: keep only items at their last occurrence index
                int writeIndex = 0;
                for (int i = 0; i < properties.Count; i++)
                {
                    if (lastOccurrence[properties[i].Name] == i)
                    {
                        properties[writeIndex++] = properties[i];
                    }
                }

                // Remove trailing items
                properties.RemoveRange(writeIndex, properties.Count - writeIndex);
            }
        }

        public static void Save(string filePath, Document document)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            Save(fileStream, Encoding.UTF8, document);
        }

        public static void Save(string filePath, Encoding encoding, Document document)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            Save(fileStream, encoding, document);
        }

        public static void Save(Stream stream, Encoding encoding, Document document)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            using var writer = new StreamWriter(stream, encoding, BufferSize, leaveOpen: true);

            // Write default section
            foreach (var property in document.DefaultSection.GetProperties())
            {
                foreach (var comment in property.PreComments)
                {
                    writer.Write(document.DefaultCommentPrefixChar);
                    writer.WriteLine(comment.Value);
                }

                var shouldQuote = property.IsQuoted || NeedsQuoting(property.Value);

                writer.Write($"{property.Name} = ");
                if (shouldQuote)
                {
                    writer.Write('"');
                    WriteEscapedValue(writer, property.Value);
                    writer.Write('"');
                }
                else
                {
                    writer.Write(property.Value);
                }
                if (!string.IsNullOrEmpty(property.Comment?.Value))
                {
                    writer.Write($" {document.DefaultCommentPrefixChar}{property.Comment.Value}");
                }
                writer.WriteLine();
            }
            if (document.DefaultSection.PropertyCount > 0 &&
                document.SectionCount > 0)
            {
                writer.WriteLine();
            }

            // Write sections
            for (var indexSection = 0; indexSection < document.SectionCount; indexSection++)
            {
                var section = document[indexSection];
                // Write section comments
                foreach (var comment in section.PreComments)
                {
                    writer.Write(document.DefaultCommentPrefixChar);
                    writer.WriteLine(comment.Value);
                }

                // Write section with inline comment
                writer.Write($"[{section.Name}]");
                if (!string.IsNullOrEmpty(section.Comment?.Value))
                {
                    writer.Write($" {document.DefaultCommentPrefixChar}{section.Comment.Value}");
                }

                // Write properties
                foreach (var property in section.GetProperties())
                {
                    writer.WriteLine();
                    foreach (var comment in property.PreComments)
                    {
                        writer.Write(document.DefaultCommentPrefixChar);
                        writer.WriteLine(comment.Value);
                    }

                    var shouldQuote = property.IsQuoted || NeedsQuoting(property.Value);

                    writer.Write($"{property.Name} = ");
                    if (shouldQuote)
                    {
                        writer.Write('"');
                        WriteEscapedValue(writer, property.Value);
                        writer.Write('"');
                    }
                    else
                    {
                        writer.Write(property.Value);
                    }
                    if (!string.IsNullOrEmpty(property.Comment?.Value))
                    {
                        writer.Write($" {document.DefaultCommentPrefixChar}{property.Comment.Value}");
                    }
                }

                if (indexSection < document.SectionCount - 1)
                {
                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Asynchronously loads an INI configuration from a stream using true async I/O.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the loaded document.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with ReadLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task<Document> LoadAsync(Stream stream, Encoding encoding, IniConfigOption? option = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));
            if (option == null)
                option = new IniConfigOption();

            cancellationToken.ThrowIfCancellationRequested();

            var doc = new Document(option);
            using var reader = new StreamReader(stream, encoding, true, BufferSize, leaveOpen: true);
            {

                Section currentSection = doc.DefaultSection;
                var pendingComments = new List<Comment>();
                string? line;
                int lineNumber = 0;
                while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
                {
                    lineNumber++;

                    // Check line length limit
                    if (option.MaxLineLength > 0 && line.Length > option.MaxLineLength)
                    {
                        ReportError(doc, option, lineNumber, TruncateLine(line), $"Line exceeds maximum length ({option.MaxLineLength} characters)");
                        continue;
                    }

                    var span = line.AsSpan().Trim();
                    if (span.IsEmpty)
                        continue;

                    var commentSign = span.IndexOfAny(doc.CommentPrefixChars);
                    if (commentSign == 0)
                    {
                        var commentString = span.Slice(1).ToString();
                        // Enforce MaxPendingComments limit (FIFO - remove oldest when full)
                        if (option.MaxPendingComments > 0 && pendingComments.Count >= option.MaxPendingComments)
                        {
                            pendingComments.RemoveAt(0);
                        }
                        pendingComments.Add(new Comment(commentString));
                        continue;
                    }

                    if (span[0] == '[')
                    {
                        var closeBracket = span.IndexOf(']');
                        if (closeBracket == -1)
                        {
                            ReportError(doc, option, lineNumber, line, "Missing closing bracket in section declaration");
                            continue;
                        }

                        var sectionNameSpan = span.Slice(1, closeBracket - 1).Trim();

                        if (sectionNameSpan.IsEmpty || sectionNameSpan.IsWhiteSpace())
                        {
                            ReportError(doc, option, lineNumber, line, "Section name cannot be empty");
                            continue;
                        }

                        var sectionName = sectionNameSpan.ToString();
                        try
                        {
                            currentSection = new Section(sectionName);
                        }
                        catch (ArgumentException ex)
                        {
                            var error = new ParsingErrorEventArgs(lineNumber, line, $"Invalid section name: {ex.Message}");
                            if (option.CollectParsingErrors)
                                doc.AddParsingError(error);
                            continue;
                        }

                        var commentRemains = span.Slice(closeBracket + 1).TrimStart();
                        commentSign = commentRemains.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign == 0)
                        {
                            var commentString = commentRemains.Slice(1).ToString();
                            currentSection.Comment = new Comment(commentString);
                        }

                        if (pendingComments.Count > 0)
                        {
                            currentSection.PreComments.AddRange(pendingComments);
                            pendingComments.Clear();
                        }

                        // Check section limit
                        if (option.MaxSections > 0 && doc.SectionCount >= option.MaxSections)
                        {
                            ReportError(doc, option, lineNumber, line, $"Maximum section limit ({option.MaxSections}) exceeded");
                            continue;
                        }

                        doc.AddSectionInternal(currentSection);
                        continue;
                    }

                    // Parse property
                    var equalSign = span.IndexOf('=');
                    if (equalSign == -1)
                    {
                        ReportError(doc, option, lineNumber, line, "Missing equals sign in key-value pair");
                        continue;
                    }

                    var keyNameSpan = span.Slice(0, equalSign).Trim();
                    if (keyNameSpan.IsEmpty)
                    {
                        ReportError(doc, option, lineNumber, line, "Property key cannot be empty");
                        continue;
                    }

                    var keyName = keyNameSpan.ToString(); // Allocate only for key name
                    var valueStartSpan = span.Slice(equalSign + 1).TrimStart();
                    string value;
                    string comment = string.Empty;
                    bool isQuoted = false;

                    if (valueStartSpan.Length > 0 && valueStartSpan[0] == '"')
                    {
                        isQuoted = true;
                        var sb = new StringBuilder(valueStartSpan.Length);
                        var remains = valueStartSpan.Slice(1);
                        bool isEscaped = false;
                        bool isTerminated = false;
                        int remainsIndex = 0;

                        while (remainsIndex < remains.Length)
                        {
                            if (isEscaped)
                            {
                                var escapeResult = remains[remainsIndex] switch
                                {
                                    '0' => '\0',
                                    'a' => '\a',
                                    'b' => '\b',
                                    't' => '\t',
                                    'r' => '\r',
                                    'n' => '\n',
                                    ';' => ';',
                                    '#' => '#',
                                    '"' => '"',
                                    '\\' => '\\',
                                    _ => remains[remainsIndex]  // Use original character for unknown escape sequences
                                };
                                sb.Append(escapeResult);
                                isEscaped = false;
                            }
                            else
                            {
                                if (remains[remainsIndex] == '\\')
                                {
                                    isEscaped = true;
                                }
                                else if (remains[remainsIndex] == '"')
                                {
                                    isTerminated = true;
                                    remainsIndex++;
                                    break;
                                }
                                else
                                {
                                    sb.Append(remains[remainsIndex]);
                                }
                            }
                            remainsIndex++;
                        }
                        if (isEscaped)
                        {
                            ReportError(doc, option, lineNumber, line, "Invalid escape sequence: incomplete escape marker");
                            continue;
                        }
                        if (!isTerminated)
                        {
                            ReportError(doc, option, lineNumber, line, "Unterminated quote: missing closing quotation mark");
                            continue;
                        }

                        value = sb.ToString();

                        // Check for inline comment
                        var afterQuoteSpan = remains.Slice(remainsIndex).TrimStart();
                        commentSign = afterQuoteSpan.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign == 0)
                        {
                            comment = afterQuoteSpan.Slice(1).ToString();
                        }
                        else if (commentSign > 0)
                        {
                            ReportError(doc, option, lineNumber, line, "Invalid content after closing quote");
                            continue;
                        }
                        else
                        {
                            afterQuoteSpan = afterQuoteSpan.Trim();
                            if (afterQuoteSpan.Length != 0)
                            {
                                ReportError(doc, option, lineNumber, line, "Invalid quote format");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // Check for inline comment
                        commentSign = valueStartSpan.IndexOfAny(doc.CommentPrefixChars);
                        if (commentSign >= 0)
                        {
                            value = valueStartSpan.Slice(0, commentSign).TrimEnd().ToString();
                            comment = valueStartSpan.Slice(commentSign + 1).ToString();
                        }
                        else
                        {
                            value = valueStartSpan.TrimEnd().ToString();
                            comment = string.Empty;
                        }
                    }

                    // Check value length limit
                    if (option.MaxValueLength > 0 && value.Length > option.MaxValueLength)
                    {
                        ReportError(doc, option, lineNumber, line, $"Value length ({value.Length}) exceeds maximum ({option.MaxValueLength})");
                        continue;
                    }

                    // Check property count limit
                    if (option.MaxPropertiesPerSection > 0 && currentSection.PropertyCount >= option.MaxPropertiesPerSection)
                    {
                        ReportError(doc, option, lineNumber, line, $"Maximum properties per section ({option.MaxPropertiesPerSection}) exceeded");
                        continue;
                    }

                    var property = new Property(keyName, value);
                    property.IsQuoted = isQuoted;

                    if (pendingComments.Count > 0)
                    {
                        property.PreComments.AddRange(pendingComments);
                        pendingComments.Clear();
                    }

                    if (!string.IsNullOrEmpty(comment))
                    {
                        property.Comment = new Comment(comment);
                    }

                    currentSection.AddProperty(property);
                }
            }

            // Remove null values (defensive check)
            doc.GetInternalSections().RemoveAll(x => x == null);
            foreach (Section section in doc)
            {
                section.GetInternalProperties().RemoveAll(x => x == null);
            }

            // Apply policies - RebuildSectionLookup is called once at the end
            bool needsRebuild = false;
            if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.ThrowError)
            {
                ThrowDuplicateSectionExist(doc.GetInternalSections());
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.FirstWin)
            {
                DeduplicateSectionOnFirstWin(doc.GetInternalSections());
                needsRebuild = true;
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.LastWin)
            {
                DeduplicateSectionOnLastWin(doc.GetInternalSections());
                needsRebuild = true;
            }
            else if (option.DuplicateSectionPolicy == DuplicateSectionPolicyType.Merge)
            {
                DeduplicateSectionOnMerging(doc.GetInternalSections(), option.DuplicateKeyPolicy);
                needsRebuild = true;
            }

            if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.ThrowError)
            {
                ThrowDuplicatePropertyExist(doc);
            }
            else if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.FirstWin)
            {
                DeduplicatePropertyOnFirstWin(doc);
            }
            else if (option.DuplicateKeyPolicy == DuplicateKeyPolicyType.LastWin)
            {
                DeduplicatePropertyOnLastWin(doc);
            }

            // Single rebuild at the end for all section modifications
            if (needsRebuild)
            {
                doc.RebuildSectionLookup();
            }

            return doc;
        }

        /// <summary>
        /// Asynchronously saves an INI configuration to a stream using true async I/O.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with WriteAsync/WriteLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task SaveAsync(Stream stream, Encoding encoding, Document document, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            cancellationToken.ThrowIfCancellationRequested();

            using var writer = new StreamWriter(stream, encoding, BufferSize, leaveOpen: true);

            // Write default section
            foreach (var property in document.DefaultSection.GetProperties())
            {
                foreach (var comment in property.PreComments)
                {
                    await writer.WriteAsync(document.DefaultCommentPrefixChar).ConfigureAwait(false);
                    await writer.WriteLineAsync(comment.Value).ConfigureAwait(false);
                }

                var shouldQuote = property.IsQuoted || NeedsQuoting(property.Value);

                await writer.WriteAsync($"{property.Name} = ").ConfigureAwait(false);
                if (shouldQuote)
                {
                    await writer.WriteAsync('"').ConfigureAwait(false);
                    WriteEscapedValue(writer, property.Value);
                    await writer.WriteAsync('"').ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAsync(property.Value).ConfigureAwait(false);
                }
                if (!string.IsNullOrEmpty(property.Comment?.Value))
                {
                    await writer.WriteAsync($" {document.DefaultCommentPrefixChar}{property.Comment.Value}").ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
            if (document.DefaultSection.PropertyCount > 0 &&
                document.SectionCount > 0)
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            // Write sections
            for (var indexSection = 0; indexSection < document.SectionCount; indexSection++)
            {
                var section = document[indexSection];
                // Write section comments
                foreach (var comment in section.PreComments)
                {
                    await writer.WriteAsync(document.DefaultCommentPrefixChar).ConfigureAwait(false);
                    await writer.WriteLineAsync(comment.Value).ConfigureAwait(false);
                }

                // Write section with inline comment
                await writer.WriteAsync($"[{section.Name}]").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(section.Comment?.Value))
                {
                    await writer.WriteAsync($" {document.DefaultCommentPrefixChar}{section.Comment.Value}").ConfigureAwait(false);
                }

                // Write properties
                foreach (var property in section.GetProperties())
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    foreach (var comment in property.PreComments)
                    {
                        await writer.WriteAsync(document.DefaultCommentPrefixChar).ConfigureAwait(false);
                        await writer.WriteLineAsync(comment.Value).ConfigureAwait(false);
                    }

                    var shouldQuote = property.IsQuoted || NeedsQuoting(property.Value);

                    await writer.WriteAsync($"{property.Name} = ").ConfigureAwait(false);
                    if (shouldQuote)
                    {
                        await writer.WriteAsync('"').ConfigureAwait(false);
                        WriteEscapedValue(writer, property.Value);
                        await writer.WriteAsync('"').ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteAsync(property.Value).ConfigureAwait(false);
                    }
                    if (!string.IsNullOrEmpty(property.Comment?.Value))
                    {
                        await writer.WriteAsync($" {document.DefaultCommentPrefixChar}{property.Comment.Value}").ConfigureAwait(false);
                    }
                }

                if (indexSection < document.SectionCount - 1)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
