namespace IniEdit
{
    /// <summary>
    /// Provides validation methods for INI configuration elements.
    /// </summary>
    public class IniConfigValidator
    {
        private readonly IniConfigOption _option;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniConfigValidator"/> class.
        /// </summary>
        /// <param name="option">The configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when option is null.</exception>
        public IniConfigValidator(IniConfigOption option)
        {
            _option = option ?? throw new ArgumentNullException(nameof(option));
        }

        /// <summary>
        /// Checks if a string contains newline characters.
        /// </summary>
        private static bool ContainsNewline(string value)
            => value.AsSpan().IndexOfAny('\r', '\n') >= 0;

        /// <summary>
        /// Validates a single-line text field.
        /// </summary>
        private static IniValidationResult ValidateSingleLineText(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return IniValidationResult.Error($"{fieldName} cannot be empty");

            if (ContainsNewline(value))
                return IniValidationResult.Error($"{fieldName} cannot contain newline characters");

            return IniValidationResult.Success();
        }

        /// <summary>
        /// Validates a pre-comment that can span multiple lines.
        /// </summary>
        /// <param name="value">The comment text to validate.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidatePreCommentAsMultiLine(string value)
        {
            if (string.IsNullOrEmpty(value))
                return IniValidationResult.Error("Pre-comment cannot be empty");

            return IniValidationResult.Success();
        }

        /// <summary>
        /// Validates a single-line pre-comment.
        /// </summary>
        /// <param name="value">The comment text to validate.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidatePreComment(string value)
            => ValidateSingleLineText(value, "Pre-comment");

        /// <summary>
        /// Validates an inline comment.
        /// </summary>
        /// <param name="value">The comment text to validate.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidateInlineComment(string value)
            => ValidateSingleLineText(value, "Inline comment");

        /// <summary>
        /// Validates a section name.
        /// </summary>
        /// <param name="value">The section name to validate.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidateSectionName(string value)
        {
            var result = ValidateSingleLineText(value, "Section name");
            if (!result.IsValid)
                return result;

            if (value.AsSpan().IndexOfAny('[', ']') >= 0)
                return IniValidationResult.Error("Section name cannot contain brackets");

            return IniValidationResult.Success();
        }

        /// <summary>
        /// Validates a property key.
        /// </summary>
        /// <param name="value">The property key to validate.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidateKey(string value)
        {
            var result = ValidateSingleLineText(value, "Key");
            if (!result.IsValid)
                return result;

            if (value.Contains('='))
                return IniValidationResult.Error("Key cannot contain equals sign");

            return IniValidationResult.Success();
        }

        /// <summary>
        /// Validates a property value.
        /// </summary>
        /// <param name="value">The property value to validate.</param>
        /// <param name="isQuoted">Whether the value will be quoted when written.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public IniValidationResult ValidateValue(string value, bool isQuoted)
        {
            if (value == null)
                return IniValidationResult.Error("Value cannot be null");

            if (!isQuoted && ContainsNewline(value))
                return IniValidationResult.Error("Unquoted value cannot contain newline characters");

            return IniValidationResult.Success();
        }
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class IniValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation succeeded.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the error message if validation failed; otherwise, null.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful validation result.</returns>
        public static IniValidationResult Success() =>
            new IniValidationResult(true, null);

        /// <summary>
        /// Creates a failed validation result with the specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>A failed validation result.</returns>
        public static IniValidationResult Error(string message) =>
            new IniValidationResult(false, message);

        private IniValidationResult(bool isValid, string? message)
        {
            IsValid = isValid;
            ErrorMessage = message;
        }
    }
}
