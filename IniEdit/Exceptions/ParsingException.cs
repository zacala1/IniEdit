namespace IniEdit
{
    /// <summary>
    /// Exception thrown when INI file parsing fails with collected errors.
    /// </summary>
    public class ParsingException : Exception
    {
        /// <summary>
        /// Gets all parsing errors that occurred during parsing.
        /// </summary>
        public IReadOnlyList<ParsingErrorEventArgs> AllErrors { get; }

        /// <summary>
        /// Gets the line number where the first error occurred.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the content of the line where the first error occurred.
        /// </summary>
        public string? Line { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        /// <param name="line">The content of the line.</param>
        /// <param name="allErrors">All collected parsing errors.</param>
        public ParsingException(string message, int lineNumber, string line, IReadOnlyList<ParsingErrorEventArgs> allErrors)
            : base(message)
        {
            LineNumber = lineNumber;
            Line = line;
            AllErrors = allErrors ?? new List<ParsingErrorEventArgs>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class from a single error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="error">The parsing error.</param>
        public ParsingException(string message, ParsingErrorEventArgs error)
            : base(message)
        {
            LineNumber = error.LineNumber;
            Line = error.Line;
            AllErrors = new List<ParsingErrorEventArgs> { error };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class from a collection of errors.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="allErrors">All collected parsing errors.</param>
        public ParsingException(string message, IReadOnlyList<ParsingErrorEventArgs> allErrors)
            : base(message)
        {
            AllErrors = allErrors ?? new List<ParsingErrorEventArgs>();
            if (allErrors != null && allErrors.Count > 0)
            {
                LineNumber = allErrors[0].LineNumber;
                Line = allErrors[0].Line;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(base.ToString());
            sb.AppendLine($"Total errors: {AllErrors.Count}");

            if (AllErrors.Count > 0)
            {
                sb.AppendLine("\nParsing errors:");
                foreach (var error in AllErrors.Take(10)) // Show first 10 errors
                {
                    sb.AppendLine($"  Line {error.LineNumber}: {error.Reason}");
                    sb.AppendLine($"    Content: {error.Line}");
                }

                if (AllErrors.Count > 10)
                {
                    sb.AppendLine($"  ... and {AllErrors.Count - 10} more errors");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Exception thrown when duplicate sections or properties are found with ThrowError policy.
    /// </summary>
    public class DuplicateElementException : InvalidOperationException
    {
        /// <summary>
        /// Gets the name of the duplicate element.
        /// </summary>
        public string ElementName { get; }

        /// <summary>
        /// Gets the type of the element ("Section" or "Property").
        /// </summary>
        public string ElementType { get; }

        /// <summary>
        /// Gets the section name where the duplicate property was found, if applicable.
        /// </summary>
        public string? SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateElementException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="elementName">The name of the duplicate element.</param>
        /// <param name="elementType">The type of the element ("Section" or "Property").</param>
        /// <param name="sectionName">The section name for duplicate properties.</param>
        public DuplicateElementException(string message, string elementName, string elementType, string? sectionName = null)
            : base(message)
        {
            ElementName = elementName;
            ElementType = elementType;
            SectionName = sectionName;
        }
    }
}
