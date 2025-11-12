namespace IniEdit
{
    /// <summary>
    /// Represents a single-line comment in an INI configuration file.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Gets or sets the comment prefix character (e.g., ';' or '#').
        /// </summary>
        public string Prefix { get; set; }

        private string _value = string.Empty;

        /// <summary>
        /// Gets or sets the comment text without the prefix.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when value contains newline characters.</exception>
        public string Value
        {
            get => _value;
            set
            {
                if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
                    throw new ArgumentException("Comment value cannot contain newline characters");
                _value = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Comment"/> class with a default prefix.
        /// </summary>
        /// <param name="value">The comment text.</param>
        public Comment(string value) : this(";", value)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Comment"/> class with a specified prefix.
        /// </summary>
        /// <param name="prefix">The comment prefix character.</param>
        /// <param name="value">The comment text.</param>
        public Comment(string prefix, string value)
        {
            Prefix = prefix;
            Value = value;
        }

        /// <summary>
        /// Tries to set the comment value. Returns false if the value contains newlines.
        /// </summary>
        /// <param name="value">The comment text to set.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        public bool TrySetComment(string value)
        {
            if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
                return false;

            _value = value;
            return true;
        }

        /// <summary>
        /// Creates a copy of this comment.
        /// </summary>
        /// <returns>A new comment with the same prefix and value.</returns>
        public Comment Clone()
        {
            return new Comment(Prefix, Value);
        }

        /// <summary>
        /// Implicitly converts a comment to its string value.
        /// </summary>
        /// <param name="value">The comment to convert.</param>
        public static implicit operator string(Comment value)
        {
            return value?.Value ?? string.Empty;
        }

        /// <summary>
        /// Implicitly converts a string to a comment.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        public static implicit operator Comment(string value)
        {
            return new Comment(value);
        }
    }
}
