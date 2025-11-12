namespace IniEdit
{
    /// <summary>
    /// Provides data for parsing error events.
    /// </summary>
    public class ParsingErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the line number where the error occurred.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the line content that caused the error.
        /// </summary>
        public string Line { get; }

        /// <summary>
        /// Gets the reason for the parsing error.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingErrorEventArgs"/> class.
        /// </summary>
        /// <param name="lineNumber">The line number where the error occurred.</param>
        /// <param name="line">The line content that caused the error.</param>
        /// <param name="reason">The reason for the error.</param>
        public ParsingErrorEventArgs(int lineNumber, string line, string reason)
        {
            LineNumber = lineNumber;
            Line = line;
            Reason = reason;
        }
    }
}
