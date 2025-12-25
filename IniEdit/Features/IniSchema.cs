using System;
using System.Collections.Generic;

namespace IniEdit
{
    /// <summary>
    /// Defines a schema for validating INI documents.
    /// </summary>
    public sealed class IniSchema
    {
        private readonly Dictionary<string, SectionDefinition> _sections = new(StringComparer.OrdinalIgnoreCase);
        private bool _allowUndefinedSections = true;
        private bool _allowUndefinedProperties = true;

        /// <summary>
        /// Gets the section definitions in this schema.
        /// </summary>
        public IReadOnlyDictionary<string, SectionDefinition> Sections => _sections;

        /// <summary>
        /// Gets or sets whether undefined sections are allowed.
        /// </summary>
        public bool AllowUndefinedSections
        {
            get => _allowUndefinedSections;
            set => _allowUndefinedSections = value;
        }

        /// <summary>
        /// Gets or sets whether undefined properties are allowed.
        /// </summary>
        public bool AllowUndefinedProperties
        {
            get => _allowUndefinedProperties;
            set => _allowUndefinedProperties = value;
        }

        /// <summary>
        /// Defines a section in the schema.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="required">Whether the section is required.</param>
        /// <returns>The section definition for further configuration.</returns>
        public SectionDefinition DefineSection(string name, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Section name cannot be null or empty.", nameof(name));

            var definition = new SectionDefinition(name, required);
            _sections[name] = definition;
            return definition;
        }

        /// <summary>
        /// Defines a property directly on the default section.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="expectedType">The expected type of the property value.</param>
        /// <param name="required">Whether the property is required.</param>
        /// <returns>The property definition for further configuration.</returns>
        public PropertyDefinition DefineProperty(string key, Type? expectedType = null, bool required = false)
        {
            const string defaultSectionName = "";
            if (!_sections.TryGetValue(defaultSectionName, out var section))
            {
                section = new SectionDefinition(defaultSectionName, false);
                _sections[defaultSectionName] = section;
            }
            return section.DefineProperty(key, expectedType, required);
        }

        /// <summary>
        /// Validates a document against this schema.
        /// </summary>
        /// <param name="document">The document to validate.</param>
        /// <returns>The validation result.</returns>
        public SchemaValidationResult Validate(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var result = new SchemaValidationResult();

            // Check required sections
            foreach (var sectionDef in _sections.Values)
            {
                if (sectionDef.IsRequired)
                {
                    var section = sectionDef.Name == ""
                        ? document.DefaultSection
                        : document.GetSection(sectionDef.Name);

                    if (section == null || (sectionDef.Name != "" && !document.HasSection(sectionDef.Name)))
                    {
                        result.AddError(new SchemaValidationError(
                            SchemaErrorType.MissingRequiredSection,
                            $"Required section '{sectionDef.Name}' is missing.",
                            sectionDef.Name,
                            null));
                    }
                }
            }

            // Validate default section properties
            ValidateSectionProperties(document.DefaultSection, "", result);

            // Validate each section
            foreach (var section in document)
            {
                if (_sections.TryGetValue(section.Name, out var sectionDef))
                {
                    ValidateSectionProperties(section, section.Name, result);
                }
                else if (!_allowUndefinedSections)
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.UndefinedSection,
                        $"Section '{section.Name}' is not defined in the schema.",
                        section.Name,
                        null));
                }
            }

            return result;
        }

        private void ValidateSectionProperties(Section section, string sectionName, SchemaValidationResult result)
        {
            if (!_sections.TryGetValue(sectionName, out var sectionDef))
                return;

            // Check required properties
            foreach (var propDef in sectionDef.Properties.Values)
            {
                if (propDef.IsRequired && !section.HasProperty(propDef.Key))
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.MissingRequiredProperty,
                        $"Required property '{propDef.Key}' is missing in section '{sectionName}'.",
                        sectionName,
                        propDef.Key));
                }
            }

            // Validate each property
            foreach (var property in section.GetProperties())
            {
                if (sectionDef.Properties.TryGetValue(property.Name, out var propDef))
                {
                    ValidatePropertyValue(property, propDef, sectionName, result);
                }
                else if (!_allowUndefinedProperties)
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.UndefinedProperty,
                        $"Property '{property.Name}' in section '{sectionName}' is not defined in the schema.",
                        sectionName,
                        property.Name));
                }
            }
        }

        private static void ValidatePropertyValue(Property property, PropertyDefinition propDef, string sectionName, SchemaValidationResult result)
        {
            // Type validation
            if (propDef.ExpectedType != null)
            {
                if (!TryConvertValue(property.Value, propDef.ExpectedType, out _))
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.TypeMismatch,
                        $"Property '{property.Name}' in section '{sectionName}' cannot be converted to {propDef.ExpectedType.Name}.",
                        sectionName,
                        property.Name));
                }
            }

            // Custom validator
            if (propDef.Validator != null)
            {
                var validatorResult = propDef.Validator(property.Value);
                if (!validatorResult.IsValid)
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.ValidationFailed,
                        validatorResult.ErrorMessage ?? $"Property '{property.Name}' failed custom validation.",
                        sectionName,
                        property.Name));
                }
            }

            // Allowed values
            if (propDef.AllowedValues != null && propDef.AllowedValues.Count > 0)
            {
                if (!propDef.AllowedValues.Contains(property.Value))
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.ValueNotAllowed,
                        $"Property '{property.Name}' has value '{property.Value}' which is not in the allowed values.",
                        sectionName,
                        property.Name));
                }
            }

            // Pattern validation
            if (!string.IsNullOrEmpty(propDef.Pattern))
            {
                try
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(property.Value, propDef.Pattern))
                    {
                        result.AddError(new SchemaValidationError(
                            SchemaErrorType.PatternMismatch,
                            $"Property '{property.Name}' value does not match the required pattern.",
                            sectionName,
                            property.Name));
                    }
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                {
                    result.AddError(new SchemaValidationError(
                        SchemaErrorType.ValidationFailed,
                        $"Property '{property.Name}' pattern validation timed out.",
                        sectionName,
                        property.Name));
                }
            }

            // Range validation for numeric types
            if (propDef.MinValue != null || propDef.MaxValue != null)
            {
                if (double.TryParse(property.Value, out var numericValue))
                {
                    if (propDef.MinValue != null && numericValue < propDef.MinValue)
                    {
                        result.AddError(new SchemaValidationError(
                            SchemaErrorType.ValueOutOfRange,
                            $"Property '{property.Name}' value {numericValue} is less than minimum {propDef.MinValue}.",
                            sectionName,
                            property.Name));
                    }
                    if (propDef.MaxValue != null && numericValue > propDef.MaxValue)
                    {
                        result.AddError(new SchemaValidationError(
                            SchemaErrorType.ValueOutOfRange,
                            $"Property '{property.Name}' value {numericValue} is greater than maximum {propDef.MaxValue}.",
                            sectionName,
                            property.Name));
                    }
                }
            }
        }

        private static bool TryConvertValue(string value, Type targetType, out object? result)
        {
            result = null;
            try
            {
                if (targetType == typeof(string))
                {
                    result = value;
                    return true;
                }
                if (targetType == typeof(int))
                {
                    if (int.TryParse(value, out var intVal))
                    {
                        result = intVal;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(value, out var boolVal))
                    {
                        result = boolVal;
                        return true;
                    }
                    if (value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                        return true;
                    }
                    if (value == "0" || value.Equals("no", StringComparison.OrdinalIgnoreCase))
                    {
                        result = false;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(double))
                {
                    if (double.TryParse(value, out var doubleVal))
                    {
                        result = doubleVal;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(long))
                {
                    if (long.TryParse(value, out var longVal))
                    {
                        result = longVal;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParse(value, out var dateVal))
                    {
                        result = dateVal;
                        return true;
                    }
                    return false;
                }

                result = Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Defines a section in an INI schema.
    /// </summary>
    public sealed class SectionDefinition
    {
        private readonly Dictionary<string, PropertyDefinition> _properties = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the section name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether this section is required.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets the property definitions for this section.
        /// </summary>
        public IReadOnlyDictionary<string, PropertyDefinition> Properties => _properties;

        internal SectionDefinition(string name, bool required)
        {
            Name = name;
            IsRequired = required;
        }

        /// <summary>
        /// Defines a property in this section.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="expectedType">The expected type of the property value.</param>
        /// <param name="required">Whether the property is required.</param>
        /// <returns>The property definition for further configuration.</returns>
        public PropertyDefinition DefineProperty(string key, Type? expectedType = null, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            var definition = new PropertyDefinition(key, expectedType, required);
            _properties[key] = definition;
            return definition;
        }
    }

    /// <summary>
    /// Defines a property in an INI schema.
    /// </summary>
    public sealed class PropertyDefinition
    {
        /// <summary>
        /// Gets the property key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the expected type of the property value.
        /// </summary>
        public Type? ExpectedType { get; }

        /// <summary>
        /// Gets whether this property is required.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets or sets the default value for this property.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a description for this property.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a custom validator function.
        /// </summary>
        public Func<string, IniValidationResult>? Validator { get; set; }

        /// <summary>
        /// Gets or sets the allowed values for this property.
        /// </summary>
        public HashSet<string>? AllowedValues { get; set; }

        /// <summary>
        /// Gets or sets a regex pattern the value must match.
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets the minimum numeric value allowed.
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum numeric value allowed.
        /// </summary>
        public double? MaxValue { get; set; }

        internal PropertyDefinition(string key, Type? expectedType, bool required)
        {
            Key = key;
            ExpectedType = expectedType;
            IsRequired = required;
        }

        /// <summary>
        /// Sets the default value and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithDefault(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Sets the description and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithDescription(string description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        /// Sets a custom validator and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithValidator(Func<string, IniValidationResult> validator)
        {
            Validator = validator;
            return this;
        }

        /// <summary>
        /// Sets the allowed values and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithAllowedValues(params string[] values)
        {
            AllowedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
            return this;
        }

        /// <summary>
        /// Sets a regex pattern and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithPattern(string pattern)
        {
            Pattern = pattern;
            return this;
        }

        /// <summary>
        /// Sets the value range and returns this definition for fluent chaining.
        /// </summary>
        public PropertyDefinition WithRange(double? min = null, double? max = null)
        {
            MinValue = min;
            MaxValue = max;
            return this;
        }
    }

    /// <summary>
    /// Represents the result of schema validation.
    /// </summary>
    public sealed class SchemaValidationResult
    {
        private readonly List<SchemaValidationError> _errors = new();

        /// <summary>
        /// Gets whether the validation passed with no errors.
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public IReadOnlyList<SchemaValidationError> Errors => _errors;

        /// <summary>
        /// Gets the number of errors.
        /// </summary>
        public int ErrorCount => _errors.Count;

        internal void AddError(SchemaValidationError error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// Returns a summary of all validation errors.
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
                return "Validation passed.";

            return $"Validation failed with {_errors.Count} error(s):\n" +
                   string.Join("\n", _errors.ConvertAll(e => $"  - {e.Message}"));
        }
    }

    /// <summary>
    /// Represents a single validation error.
    /// </summary>
    public sealed class SchemaValidationError
    {
        /// <summary>
        /// Gets the error type.
        /// </summary>
        public SchemaErrorType ErrorType { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the section name where the error occurred.
        /// </summary>
        public string? SectionName { get; }

        /// <summary>
        /// Gets the property key where the error occurred.
        /// </summary>
        public string? PropertyKey { get; }

        internal SchemaValidationError(SchemaErrorType errorType, string message, string? sectionName, string? propertyKey)
        {
            ErrorType = errorType;
            Message = message;
            SectionName = sectionName;
            PropertyKey = propertyKey;
        }

        /// <inheritdoc />
        public override string ToString() => Message;
    }

    /// <summary>
    /// Types of schema validation errors.
    /// </summary>
    public enum SchemaErrorType
    {
        /// <summary>A required section is missing.</summary>
        MissingRequiredSection,

        /// <summary>A required property is missing.</summary>
        MissingRequiredProperty,

        /// <summary>A section is not defined in the schema.</summary>
        UndefinedSection,

        /// <summary>A property is not defined in the schema.</summary>
        UndefinedProperty,

        /// <summary>A property value cannot be converted to the expected type.</summary>
        TypeMismatch,

        /// <summary>A property value is not in the allowed values list.</summary>
        ValueNotAllowed,

        /// <summary>A property value does not match the required pattern.</summary>
        PatternMismatch,

        /// <summary>A property value is outside the allowed range.</summary>
        ValueOutOfRange,

        /// <summary>Custom validation failed.</summary>
        ValidationFailed
    }
}
