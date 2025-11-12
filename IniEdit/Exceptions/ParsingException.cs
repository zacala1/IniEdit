namespace IniEdit
{
    /// <summary>
    /// Exception thrown when INI file parsing fails with collected errors.
    /// </summary>
    public class ParsingException : Exception
    {
        public IReadOnlyList<ParsingErrorEventArgs> AllErrors { get; }
        public int LineNumber { get; }
        public string? Line { get; }

        public ParsingException(string message, int lineNumber, string line, IReadOnlyList<ParsingErrorEventArgs> allErrors)
            : base(message)
        {
            LineNumber = lineNumber;
            Line = line;
            AllErrors = allErrors ?? new List<ParsingErrorEventArgs>();
        }

        public ParsingException(string message, ParsingErrorEventArgs error)
            : base(message)
        {
            LineNumber = error.LineNumber;
            Line = error.Line;
            AllErrors = new List<ParsingErrorEventArgs> { error };
        }

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
        public string ElementName { get; }
        public string ElementType { get; } // "Section" or "Property"
        public string? SectionName { get; }

        public DuplicateElementException(string message, string elementName, string elementType, string? sectionName = null)
            : base(message)
        {
            ElementName = elementName;
            ElementType = elementType;
            SectionName = sectionName;
        }
    }
}
