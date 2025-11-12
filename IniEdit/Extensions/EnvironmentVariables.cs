using System.Text.RegularExpressions;

namespace IniEdit
{
    public static class EnvironmentVariableExtensions
    {
        private static readonly Regex EnvVarPattern = new Regex(@"\$\{([^}]+)\}|%([^%]+)%", RegexOptions.Compiled);

        /// <summary>
        /// Substitutes environment variables in property values.
        /// Supports ${VAR} and %VAR% syntax.
        /// </summary>
        public static void SubstituteEnvironmentVariables(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            // Process default section
            foreach (var property in document.DefaultSection.GetProperties())
            {
                property.Value = SubstituteValue(property.Value);
            }

            // Process all sections
            foreach (var section in document)
            {
                foreach (var property in section.GetProperties())
                {
                    property.Value = SubstituteValue(property.Value);
                }
            }
        }

        /// <summary>
        /// Substitutes environment variables in a section's property values.
        /// </summary>
        public static void SubstituteEnvironmentVariables(this Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            foreach (var property in section.GetProperties())
            {
                property.Value = SubstituteValue(property.Value);
            }
        }

        /// <summary>
        /// Substitutes environment variables in a property value.
        /// </summary>
        public static void SubstituteEnvironmentVariables(this Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            property.Value = SubstituteValue(property.Value);
        }

        /// <summary>
        /// Returns a new string with environment variables substituted.
        /// </summary>
        public static string SubstituteEnvironmentVariablesInValue(string value)
        {
            return SubstituteValue(value);
        }

        private static string SubstituteValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return EnvVarPattern.Replace(value, match =>
            {
                // Check ${VAR} format (group 1) or %VAR% format (group 2)
                string varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

                string? envValue = Environment.GetEnvironmentVariable(varName);

                // Return the environment variable value if found, otherwise keep the original text
                return envValue ?? match.Value;
            });
        }

        /// <summary>
        /// Tries to substitute environment variables and returns whether any substitutions were made.
        /// </summary>
        public static bool TrySubstituteEnvironmentVariables(this Property property, out string result)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            result = SubstituteValue(property.Value);
            return result != property.Value;
        }
    }
}
