namespace IniEdit
{
    /// <summary>
    /// Configuration options for INI file parsing and writing.
    /// </summary>
    public class IniConfigOption
    {
        /// <summary>
        /// Specifies how to handle duplicate keys within a section.
        /// </summary>
        public enum DuplicateKeyPolicyType
        {
            /// <summary>Keep the first occurrence and ignore subsequent duplicates.</summary>
            FirstWin,
            /// <summary>Keep the last occurrence and overwrite previous duplicates.</summary>
            LastWin,
            /// <summary>Throw an exception when a duplicate is found.</summary>
            ThrowError
        }

        /// <summary>
        /// Specifies how to handle duplicate section names.
        /// </summary>
        public enum DuplicateSectionPolicyType
        {
            /// <summary>Keep the first occurrence and ignore subsequent duplicates.</summary>
            FirstWin,
            /// <summary>Keep the last occurrence and overwrite previous duplicates.</summary>
            LastWin,
            /// <summary>Merge properties from all occurrences into one section.</summary>
            Merge,
            /// <summary>Throw an exception when a duplicate is found.</summary>
            ThrowError
        }

        /// <summary>
        /// Gets or sets the allowed comment prefix characters (e.g., ';' and '#').
        /// </summary>
        public char[] CommentPrefixChars { get; set; }

        /// <summary>
        /// Gets or sets the default comment prefix character used when writing comments.
        /// </summary>
        public char DefaultCommentPrefixChar { get; set; }

        /// <summary>
        /// Gets or sets the policy for handling duplicate keys within a section.
        /// </summary>
        public DuplicateKeyPolicyType DuplicateKeyPolicy { get; set; }

        /// <summary>
        /// Gets or sets the policy for handling duplicate section names.
        /// </summary>
        public DuplicateSectionPolicyType DuplicateSectionPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to collect parsing errors instead of throwing exceptions.
        /// </summary>
        public bool CollectParsingErrors { get; set; }

        /// <summary>
        /// Maximum number of sections allowed. 0 means unlimited (default).
        /// Set this to prevent memory exhaustion from malicious INI files.
        /// </summary>
        public int MaxSections { get; set; }

        /// <summary>
        /// Maximum number of properties per section. 0 means unlimited (default).
        /// Set this to prevent memory exhaustion from malicious INI files.
        /// </summary>
        public int MaxPropertiesPerSection { get; set; }

        /// <summary>
        /// Maximum length of a property value in characters. 0 means unlimited (default).
        /// Set this to prevent memory exhaustion from malicious INI files.
        /// </summary>
        public int MaxValueLength { get; set; }

        /// <summary>
        /// Maximum length of a single line in characters. 0 means unlimited (default).
        /// Set this to prevent memory exhaustion from malicious INI files with very long lines.
        /// </summary>
        public int MaxLineLength { get; set; }

        /// <summary>
        /// Maximum number of parsing errors to collect. 0 means unlimited (default).
        /// Set this to prevent unbounded memory growth when parsing malformed files.
        /// Only applies when <see cref="CollectParsingErrors"/> is true.
        /// </summary>
        public int MaxParsingErrors { get; set; }

        /// <summary>
        /// Maximum number of pending comments to buffer during parsing. 0 means unlimited (default).
        /// Set this to prevent memory exhaustion from malicious INI files with excessive consecutive comment lines.
        /// When the limit is reached, older comments are discarded (FIFO).
        /// </summary>
        public int MaxPendingComments { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniConfigOption"/> class with default values.
        /// </summary>
        public IniConfigOption()
        {
            CommentPrefixChars = new char[] { ';', '#' };
            DefaultCommentPrefixChar = ';';
            DuplicateKeyPolicy = DuplicateKeyPolicyType.FirstWin;
            DuplicateSectionPolicy = DuplicateSectionPolicyType.FirstWin;
            CollectParsingErrors = false;
            MaxSections = 0;
            MaxPropertiesPerSection = 0;
            MaxValueLength = 0;
            MaxLineLength = 0;
            MaxParsingErrors = 0;
            MaxPendingComments = 0;
        }
    }
}
