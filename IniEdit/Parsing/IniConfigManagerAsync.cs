using System.Text;

namespace IniEdit
{
    /// <summary>
    /// Provides asynchronous methods for loading and saving INI configuration files.
    /// </summary>
    public static partial class IniConfigManager
    {
        /// <summary>
        /// Asynchronously loads an INI configuration file from the specified path using UTF-8 encoding.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The loaded document.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public static async Task<Document> LoadAsync(string filePath, IniConfigOption? option = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            return await LoadAsync(fileStream, Encoding.UTF8, option, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously loads an INI configuration file from the specified path using the specified encoding.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The loaded document.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public static async Task<Document> LoadAsync(string filePath, Encoding encoding, IniConfigOption? option = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            return await LoadAsync(fileStream, encoding, option, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously loads an INI configuration from a stream using the specified encoding.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The loaded document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="encoding"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
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

            var doc = new Document(option);
            using var reader = new StreamReader(stream, encoding, true, BufferSize, leaveOpen: true);

            Section currentSection = doc.DefaultSection;
            var pendingComments = new Queue<Comment>();
            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;

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
                    if (option.MaxPendingComments > 0 && pendingComments.Count >= option.MaxPendingComments)
                        pendingComments.Dequeue();
                    pendingComments.Enqueue(new Comment(commentString));
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

                    try { currentSection = new Section(sectionName); }
                    catch (ArgumentException ex)
                    {
                        if (option.CollectParsingErrors)
                            doc.AddParsingError(new ParsingErrorEventArgs(lineNumber, line, $"Invalid section name: {ex.Message}"));
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
                        currentSection.Comment = new Comment(afterSection.Slice(1).ToString());

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
                }
                else if (valueStart[0] == '"')
                {
                    isQuoted = true;
                    bool isEscaped = false;
                    bool isTerminated = false;
                    var sb = new System.Text.StringBuilder(valueStart.Length);
                    var remains = valueStart.Slice(1);
                    while (remains.Length > 0)
                    {
                        if (isEscaped)
                        {
                            isEscaped = false;
                            sb.Append(remains[0] switch
                            {
                                '0' => '\0', 'a' => '\a', 'b' => '\b', 't' => '\t',
                                'r' => '\r', 'n' => '\n', ';' => ';', '#' => '#',
                                '"' => '"', '\\' => '\\', _ => remains[0]
                            });
                        }
                        else if (remains[0] == '"')
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
                        else
                        {
                            sb.Append(remains[0]);
                        }
                        remains = remains.Slice(1);
                    }

                    if (isEscaped) { ReportError(doc, option, lineNumber, line, "Invalid escape sequence: incomplete escape marker"); continue; }
                    if (!isTerminated) { ReportError(doc, option, lineNumber, line, "Unterminated quote: missing closing quotation mark"); continue; }

                    value = sb.ToString();
                    remains = remains.TrimStart();
                    commentSign = remains.IndexOfAny(doc.CommentPrefixChars);
                    if (commentSign == 0) { comment = remains.Slice(1).ToString(); remains = []; }
                    else if (commentSign > 0) { ReportError(doc, option, lineNumber, line, "Invalid content after closing quote"); continue; }

                    remains = remains.Trim();
                    if (remains.Length != 0) { ReportError(doc, option, lineNumber, line, "Invalid quote format"); continue; }
                }
                else
                {
                    commentSign = valueStart.IndexOfAny(doc.CommentPrefixChars);
                    if (commentSign >= 0)
                    {
                        value = valueStart.Slice(0, commentSign).TrimEnd().ToString();
                        comment = valueStart.Slice(commentSign + 1).ToString();
                    }
                    else
                    {
                        value = valueStart.TrimEnd().ToString();
                    }
                }

                if (option.MaxValueLength > 0 && value.Length > option.MaxValueLength)
                {
                    ReportError(doc, option, lineNumber, line, $"Value length ({value.Length}) exceeds maximum ({option.MaxValueLength})");
                    continue;
                }

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
                    property.Comment = new Comment(comment);

                currentSection.AddPropertyInternal(property);
            }

            // Apply same post-processing as synchronous Load
            doc.GetInternalSections().RemoveAll(x => x == null);
            foreach (Section section in doc)
                section.GetInternalProperties().RemoveAll(x => x == null);

            bool needsRebuild = false;
            if (option.DuplicateSectionPolicy == IniConfigOption.DuplicateSectionPolicyType.ThrowError)
                ThrowDuplicateSectionExist(doc.GetInternalSections());
            else if (option.DuplicateSectionPolicy == IniConfigOption.DuplicateSectionPolicyType.FirstWin)
            { DeduplicateSectionOnFirstWin(doc.GetInternalSections()); needsRebuild = true; }
            else if (option.DuplicateSectionPolicy == IniConfigOption.DuplicateSectionPolicyType.LastWin)
            { DeduplicateSectionOnLastWin(doc.GetInternalSections()); needsRebuild = true; }
            else if (option.DuplicateSectionPolicy == IniConfigOption.DuplicateSectionPolicyType.Merge)
            { DeduplicateSectionOnMerging(doc.GetInternalSections(), option.DuplicateKeyPolicy); needsRebuild = true; }

            if (option.DuplicateKeyPolicy == IniConfigOption.DuplicateKeyPolicyType.ThrowError)
                ThrowDuplicatePropertyExist(doc);
            else if (option.DuplicateKeyPolicy == IniConfigOption.DuplicateKeyPolicyType.FirstWin)
                DeduplicatePropertyOnFirstWin(doc);
            else if (option.DuplicateKeyPolicy == IniConfigOption.DuplicateKeyPolicyType.LastWin)
                DeduplicatePropertyOnLastWin(doc);

            if (needsRebuild)
                doc.RebuildSectionLookup();

            return doc;
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to the specified path using UTF-8 encoding.
        /// </summary>
        /// <param name="filePath">The path where the file will be saved.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="options">Optional save options controlling formatting.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <remarks>
        /// Uses atomic write (write to temp file, then replace) to prevent data loss.
        /// </remarks>
        public static Task SaveAsync(string filePath, Document document, SaveOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SaveAsync(filePath, Encoding.UTF8, document, options, cancellationToken);
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to the specified path using the specified encoding.
        /// </summary>
        /// <param name="filePath">The path where the file will be saved.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="options">Optional save options controlling formatting.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> or <paramref name="document"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <remarks>
        /// Uses atomic write (write to temp file, then replace) to prevent data loss.
        /// </remarks>
        public static async Task SaveAsync(string filePath, Encoding encoding, Document document, SaveOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(filePath) ?? ".";
            var tempFilePath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
                {
                    await SaveAsync(fileStream, encoding, document, options ?? SaveOptions.Default, cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempFilePath, filePath, overwrite: true);
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch (IOException) { }
                throw;
            }
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to a stream using the specified encoding.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The save options controlling formatting.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the stream is not writable.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public static async Task SaveAsync(Stream stream, Encoding encoding, Document document, SaveOptions? options = null, CancellationToken cancellationToken = default)
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

            // Use synchronous Save into a MemoryStream, then copy asynchronously
            // This avoids duplicating the entire write logic while still providing true async I/O
            using var buffer = new MemoryStream();
            Save(buffer, encoding, document, options ?? SaveOptions.Default);
            buffer.Position = 0;
            await buffer.CopyToAsync(stream, BufferSize, cancellationToken).ConfigureAwait(false);
        }
    }
}
