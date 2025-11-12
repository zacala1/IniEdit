using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit
{
    public class IniConfigValidator
    {
        private readonly IniConfigOption _option;

        public IniConfigValidator(IniConfigOption option)
        {
            _option = option ?? throw new ArgumentNullException(nameof(option));
        }

        public ValidationResult ValidatePreCommentAsMultiLine(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Error("Pre-comment cannot be empty");

            return ValidationResult.Success();
        }

        public ValidationResult ValidatePreComment(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Error("Pre-comment cannot be empty");

            if (value.Contains('\n') || value.Contains('\r'))
                return ValidationResult.Error("Pre-comment cannot contain newline characters");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateInlineComment(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Error("Inline comment cannot be empty");

            if (value.Contains('\n') || value.Contains('\r'))
                return ValidationResult.Error("Inline comment cannot contain newline characters");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateSectionName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Error("Section name cannot be empty");

            if (value.Contains('\n') || value.Contains('\r'))
                return ValidationResult.Error("Section name cannot contain newline characters");

            if (value.Contains('[') || value.Contains(']'))
                return ValidationResult.Error("Section name cannot contain brackets");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateKey(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Error("Key cannot be empty");

            if (value.Contains('\n') || value.Contains('\r'))
                return ValidationResult.Error("Key cannot contain newline characters");

            if (value.Contains('='))
                return ValidationResult.Error("Key cannot contain equals sign");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateValue(string value, bool isQuoted)
        {
            if (value is null)
                return ValidationResult.Error("Value cannot be null");

            if (!isQuoted && (value.Contains('\n') || value.Contains('\r')))
                return ValidationResult.Error("Unquoted value cannot contain newline characters");

            return ValidationResult.Success();
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        public static ValidationResult Success() =>
            new ValidationResult(true, null);

        public static ValidationResult Error(string message) =>
            new ValidationResult(false, message);

        private ValidationResult(bool isValid, string? message)
        {
            IsValid = isValid;
            ErrorMessage = message;
        }
    }
}
