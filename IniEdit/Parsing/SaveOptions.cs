namespace IniEdit
{
    /// <summary>
    /// Configuration options for saving INI documents.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Default"/> for standard formatting, or create a custom instance.
    /// The Default instance is immutable; use <see cref="Clone"/> to create a modifiable copy.
    /// </remarks>
    public class SaveOptions
    {
        /// <summary>
        /// Maximum allowed value for <see cref="BlankLinesBetweenSections"/>.
        /// </summary>
        public const int MaxBlankLinesBetweenSections = 10;

        private string _keyValueSeparator = " = ";
        private int _blankLinesBetweenSections = 1;
        private bool _blankLineAfterDefaultSection = true;
        private bool _normalizeCommentPrefix = false;
        private bool _spaceBeforeInlineComment = true;

        /// <summary>
        /// Gets the default save options. This instance is immutable.
        /// </summary>
        public static SaveOptions Default { get; } = new FrozenSaveOptions();

        /// <summary>
        /// Gets or sets the separator between key and value. Default is " = ".
        /// </summary>
        /// <remarks>Common values: " = " (default), "=", ": "</remarks>
        public virtual string KeyValueSeparator
        {
            get => _keyValueSeparator;
            set => _keyValueSeparator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the number of blank lines between sections. Default is 1.
        /// </summary>
        /// <remarks>Valid range: 0 to <see cref="MaxBlankLinesBetweenSections"/>.</remarks>
        public virtual int BlankLinesBetweenSections
        {
            get => _blankLinesBetweenSections;
            set
            {
                if (value < 0 || value > MaxBlankLinesBetweenSections)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        $"Value must be between 0 and {MaxBlankLinesBetweenSections}.");
                _blankLinesBetweenSections = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to add a blank line after the default section. Default is true.
        /// </summary>
        public virtual bool BlankLineAfterDefaultSection
        {
            get => _blankLineAfterDefaultSection;
            set => _blankLineAfterDefaultSection = value;
        }

        /// <summary>
        /// Gets or sets whether to normalize all comment prefixes to the document's default. Default is false.
        /// </summary>
        /// <remarks>When true, converts all # and ; prefixes to <see cref="Document.DefaultCommentPrefixChar"/>.</remarks>
        public virtual bool NormalizeCommentPrefix
        {
            get => _normalizeCommentPrefix;
            set => _normalizeCommentPrefix = value;
        }

        /// <summary>
        /// Gets or sets whether to add a space before inline comments. Default is true.
        /// </summary>
        /// <remarks>true: "key = value ; comment", false: "key = value; comment"</remarks>
        public virtual bool SpaceBeforeInlineComment
        {
            get => _spaceBeforeInlineComment;
            set => _spaceBeforeInlineComment = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveOptions"/> class with default values.
        /// </summary>
        public SaveOptions()
        {
        }

        /// <summary>
        /// Creates a deep copy of this save options instance.
        /// </summary>
        /// <returns>A new <see cref="SaveOptions"/> with the same values.</returns>
        public SaveOptions Clone()
        {
            return new SaveOptions
            {
                KeyValueSeparator = KeyValueSeparator,
                BlankLinesBetweenSections = BlankLinesBetweenSections,
                BlankLineAfterDefaultSection = BlankLineAfterDefaultSection,
                NormalizeCommentPrefix = NormalizeCommentPrefix,
                SpaceBeforeInlineComment = SpaceBeforeInlineComment
            };
        }
    }

    /// <summary>
    /// An immutable version of <see cref="SaveOptions"/> that throws on any modification attempt.
    /// Used for the <see cref="SaveOptions.Default"/> singleton to ensure thread safety.
    /// </summary>
    internal sealed class FrozenSaveOptions : SaveOptions
    {
        private const string FrozenMessage = "Cannot modify SaveOptions.Default. Use Clone() or create a new instance.";

        public override string KeyValueSeparator
        {
            get => base.KeyValueSeparator;
            set => throw new InvalidOperationException(FrozenMessage);
        }

        public override int BlankLinesBetweenSections
        {
            get => base.BlankLinesBetweenSections;
            set => throw new InvalidOperationException(FrozenMessage);
        }

        public override bool BlankLineAfterDefaultSection
        {
            get => base.BlankLineAfterDefaultSection;
            set => throw new InvalidOperationException(FrozenMessage);
        }

        public override bool NormalizeCommentPrefix
        {
            get => base.NormalizeCommentPrefix;
            set => throw new InvalidOperationException(FrozenMessage);
        }

        public override bool SpaceBeforeInlineComment
        {
            get => base.SpaceBeforeInlineComment;
            set => throw new InvalidOperationException(FrozenMessage);
        }
    }
}
