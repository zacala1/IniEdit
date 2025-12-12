using System.Collections.Generic;
using System.Linq;
using IniEdit;

namespace IniEdit.GUI
{
    /// <summary>
    /// Helper class for validating INI document structure
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Types of validation errors that can occur.
        /// </summary>
        public enum ValidationErrorType
        {
            /// <summary>Duplicate key found in a section.</summary>
            DuplicateKey,
            /// <summary>Property key is empty or whitespace.</summary>
            EmptyKey,
            /// <summary>Property value is empty.</summary>
            EmptyValue,
            /// <summary>Section name is empty or whitespace.</summary>
            EmptySectionName,
            /// <summary>Key or value contains invalid characters.</summary>
            InvalidCharacters,
            /// <summary>Quoted value is not properly terminated.</summary>
            UnterminatedQuote,
            /// <summary>Property line is missing equals sign.</summary>
            MissingEquals
        }

        /// <summary>
        /// Represents a validation error in an INI document.
        /// </summary>
        public sealed class ValidationError
        {
            /// <summary>Gets or sets the type of validation error.</summary>
            public ValidationErrorType Type { get; set; }
            /// <summary>Gets or sets the name of the section where the error occurred.</summary>
            public string SectionName { get; set; } = string.Empty;
            /// <summary>Gets or sets the name of the property where the error occurred, if applicable.</summary>
            public string? PropertyName { get; set; }
            /// <summary>Gets or sets the error message.</summary>
            public string Message { get; set; } = string.Empty;
            /// <summary>Gets or sets the line number where the error occurred.</summary>
            public int LineNumber { get; set; }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(PropertyName))
                    return $"[{SectionName}]: {Message}";
                return $"[{SectionName}] {PropertyName}: {Message}";
            }
        }

        /// <summary>
        /// Validate entire document
        /// </summary>
        public static List<ValidationError> ValidateDocument(Document document)
        {
            var errors = new List<ValidationError>();

            // Check global section
            errors.AddRange(ValidateSection(document.DefaultSection, "Global"));

            // Check all sections
            foreach (var section in document)
            {
                // Check section name
                if (string.IsNullOrWhiteSpace(section.Name))
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.EmptySectionName,
                        SectionName = section.Name,
                        Message = "Section name is empty"
                    });
                }

                // Check properties in section
                errors.AddRange(ValidateSection(section, section.Name));
            }

            // Check for duplicate section names
            var sectionGroups = document.GroupBy(s => s.Name, System.StringComparer.OrdinalIgnoreCase);
            foreach (var group in sectionGroups)
            {
                if (group.Count() > 1)
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.DuplicateKey,
                        SectionName = group.Key,
                        Message = $"Duplicate section name '{group.Key}' found {group.Count()} times"
                    });
                }
            }

            return errors;
        }

        /// <summary>
        /// Validate a single section
        /// </summary>
        public static List<ValidationError> ValidateSection(Section section, string sectionName)
        {
            var errors = new List<ValidationError>();
            var keyNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var property in section)
            {
                // Check for empty key
                if (string.IsNullOrWhiteSpace(property.Name))
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.EmptyKey,
                        SectionName = sectionName,
                        PropertyName = property.Name,
                        Message = "Property key is empty"
                    });
                }

                // Check for duplicate keys
                if (!keyNames.Add(property.Name))
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.DuplicateKey,
                        SectionName = sectionName,
                        PropertyName = property.Name,
                        Message = $"Duplicate key '{property.Name}'"
                    });
                }

                // Check for invalid characters in key name
                if (property.Name.Contains('='))
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidCharacters,
                        SectionName = sectionName,
                        PropertyName = property.Name,
                        Message = "Key name contains '=' character"
                    });
                }

                if (property.Name.Contains('[') || property.Name.Contains(']'))
                {
                    errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidCharacters,
                        SectionName = sectionName,
                        PropertyName = property.Name,
                        Message = "Key name contains '[' or ']' characters"
                    });
                }
            }

            return errors;
        }

        /// <summary>
        /// Get duplicate keys in a section
        /// </summary>
        public static List<string> GetDuplicateKeys(Section section)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();

            foreach (var property in section)
            {
                if (!seen.Add(property.Name))
                {
                    if (!duplicates.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                        duplicates.Add(property.Name);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// Check if section has duplicate keys
        /// </summary>
        public static bool HasDuplicateKeys(Section section)
        {
            var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in section)
            {
                if (!keySet.Add(property.Name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get statistics for document
        /// </summary>
        public static DocumentStatistics GetStatistics(Document document)
        {
            var stats = new DocumentStatistics
            {
                TotalSections = document.SectionCount,
                TotalProperties = document.DefaultSection.PropertyCount
            };

            var seenSectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var defaultSectionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.DefaultSection)
            {
                stats.TotalComments += property.PreComments.Count;
                if (property.Comment != null)
                    stats.TotalComments++;

                if (property.IsQuoted)
                    stats.QuotedValues++;
                else
                    stats.UnquotedValues++;

                if (string.IsNullOrWhiteSpace(property.Value))
                    stats.EmptyValues++;

                if (!defaultSectionKeys.Add(property.Name))
                {
                    stats.DuplicateKeys++;
                    stats.ValidationErrors++;
                }
            }

            foreach (var section in document)
            {
                stats.TotalProperties += section.PropertyCount;

                if (!seenSectionNames.Add(section.Name))
                    stats.ValidationErrors++;

                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in section)
                {
                    stats.TotalComments += property.PreComments.Count;
                    if (property.Comment != null)
                        stats.TotalComments++;

                    if (property.IsQuoted)
                        stats.QuotedValues++;
                    else
                        stats.UnquotedValues++;

                    if (string.IsNullOrWhiteSpace(property.Value))
                        stats.EmptyValues++;

                    if (!seenKeys.Add(property.Name))
                    {
                        stats.DuplicateKeys++;
                        stats.ValidationErrors++;
                    }
                }

                stats.TotalComments += section.PreComments.Count;
                if (section.Comment != null)
                    stats.TotalComments++;
            }

            stats.TotalComments += document.DefaultSection.PreComments.Count;
            if (document.DefaultSection.Comment != null)
                stats.TotalComments++;

            stats.ParsingErrors = document.ParsingErrors.Count;

            return stats;
        }
    }

    /// <summary>
    /// Statistics for an INI document.
    /// </summary>
    public sealed class DocumentStatistics
    {
        /// <summary>Gets or sets the total number of sections.</summary>
        public int TotalSections { get; set; }
        /// <summary>Gets or sets the total number of properties.</summary>
        public int TotalProperties { get; set; }
        /// <summary>Gets or sets the total number of comments.</summary>
        public int TotalComments { get; set; }
        /// <summary>Gets or sets the number of quoted values.</summary>
        public int QuotedValues { get; set; }
        /// <summary>Gets or sets the number of unquoted values.</summary>
        public int UnquotedValues { get; set; }
        /// <summary>Gets or sets the number of empty values.</summary>
        public int EmptyValues { get; set; }
        /// <summary>Gets or sets the number of parsing errors.</summary>
        public int ParsingErrors { get; set; }
        /// <summary>Gets or sets the number of duplicate keys.</summary>
        public int DuplicateKeys { get; set; }
        /// <summary>Gets or sets the number of validation errors.</summary>
        public int ValidationErrors { get; set; }

        public override string ToString()
        {
            return $@"Document Statistics
==================
Sections:          {TotalSections}
Properties:        {TotalProperties}
Comments:          {TotalComments}
Quoted Values:     {QuotedValues}
Unquoted Values:   {UnquotedValues}
Empty Values:      {EmptyValues}
Parsing Errors:    {ParsingErrors}
Duplicate Keys:    {DuplicateKeys}
Validation Errors: {ValidationErrors}";
        }
    }
}
