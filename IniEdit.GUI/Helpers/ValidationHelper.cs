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
        public enum ValidationErrorType
        {
            DuplicateKey,
            EmptyKey,
            EmptyValue,
            EmptySectionName,
            InvalidCharacters,
            UnterminatedQuote,
            MissingEquals
        }

        public class ValidationError
        {
            public ValidationErrorType Type { get; set; }
            public string SectionName { get; set; } = string.Empty;
            public string? PropertyName { get; set; }
            public string Message { get; set; } = string.Empty;
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
    /// Statistics for an INI document
    /// </summary>
    public class DocumentStatistics
    {
        public int TotalSections { get; set; }
        public int TotalProperties { get; set; }
        public int TotalComments { get; set; }
        public int QuotedValues { get; set; }
        public int UnquotedValues { get; set; }
        public int EmptyValues { get; set; }
        public int ParsingErrors { get; set; }
        public int DuplicateKeys { get; set; }
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
