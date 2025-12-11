using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace IniEdit
{
    /// <summary>
    /// Provides extension methods for filtering sections and properties in INI documents.
    /// </summary>
    public static class FilteringExtensions
    {
        /// <summary>
        /// Default timeout for regex operations to prevent ReDoS attacks.
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Thread-safe cache for compiled regex patterns.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

        /// <summary>
        /// Gets or creates a cached regex for the specified pattern.
        /// </summary>
        private static Regex GetOrCreateRegex(string pattern)
        {
            return RegexCache.GetOrAdd(pattern, p =>
                new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout));
        }

        /// <summary>
        /// Filters sections using a predicate function.
        /// </summary>
        /// <param name="document">The document to filter.</param>
        /// <param name="predicate">The function to test each section.</param>
        /// <returns>Sections that match the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static IEnumerable<Section> GetSectionsWhere(this Document document, Func<Section, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return document.Where(predicate);
        }

        /// <summary>
        /// Filters sections by name using a regex pattern.
        /// </summary>
        /// <param name="document">The document to filter.</param>
        /// <param name="namePattern">The regex pattern to match section names (case-insensitive).</param>
        /// <returns>Sections whose names match the pattern.</returns>
        /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
        /// <exception cref="RegexMatchTimeoutException">Thrown when pattern matching exceeds 100ms.</exception>
        /// <remarks>
        /// Regex patterns are compiled and cached for performance.
        /// A 100ms timeout is enforced to prevent ReDoS attacks.
        /// </remarks>
        public static IEnumerable<Section> GetSectionsByPattern(this Document document, string namePattern)
        {
            if (string.IsNullOrEmpty(namePattern))
                throw new ArgumentException("Name pattern cannot be null or empty", nameof(namePattern));

            var regex = GetOrCreateRegex(namePattern);
            return document.Where(s => regex.IsMatch(s.Name));
        }

        /// <summary>
        /// Filters properties using a predicate function.
        /// </summary>
        /// <param name="section">The section to filter.</param>
        /// <param name="predicate">The function to test each property.</param>
        /// <returns>Properties that match the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static IEnumerable<Property> GetPropertiesWhere(this Section section, Func<Property, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return section.Where(predicate);
        }

        /// <summary>
        /// Filters properties by name using a regex pattern.
        /// </summary>
        /// <param name="section">The section to filter.</param>
        /// <param name="namePattern">The regex pattern to match property names (case-insensitive).</param>
        /// <returns>Properties whose names match the pattern.</returns>
        /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
        /// <exception cref="RegexMatchTimeoutException">Thrown when pattern matching exceeds 100ms.</exception>
        /// <remarks>
        /// Regex patterns are compiled and cached for performance.
        /// A 100ms timeout is enforced to prevent ReDoS attacks.
        /// </remarks>
        public static IEnumerable<Property> GetPropertiesByPattern(this Section section, string namePattern)
        {
            if (string.IsNullOrEmpty(namePattern))
                throw new ArgumentException("Name pattern cannot be null or empty", nameof(namePattern));

            var regex = GetOrCreateRegex(namePattern);
            return section.Where(p => regex.IsMatch(p.Name));
        }

        /// <summary>
        /// Filters properties by exact value match.
        /// </summary>
        /// <param name="section">The section to filter.</param>
        /// <param name="value">The exact value to match.</param>
        /// <returns>Properties whose values equal the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        public static IEnumerable<Property> GetPropertiesWithValue(this Section section, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return section.Where(p => p.Value == value);
        }

        /// <summary>
        /// Filters properties whose values contain a substring.
        /// </summary>
        /// <param name="section">The section to filter.</param>
        /// <param name="substring">The substring to search for.</param>
        /// <returns>Properties whose values contain the substring.</returns>
        /// <exception cref="ArgumentNullException">Thrown when substring is null.</exception>
        public static IEnumerable<Property> GetPropertiesContaining(this Section section, string substring)
        {
            if (substring == null)
                throw new ArgumentNullException(nameof(substring));

            return section.Where(p => p.Value.Contains(substring));
        }

        /// <summary>
        /// Searches the entire document for properties with the specified name.
        /// </summary>
        /// <param name="document">The document to search.</param>
        /// <param name="propertyName">The property name to find (case-insensitive).</param>
        /// <returns>Tuples of (Section, Property) for each matching property.</returns>
        /// <exception cref="ArgumentException">Thrown when property name is null or empty.</exception>
        public static IEnumerable<(Section Section, Property Property)> FindPropertiesByName(this Document document, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            // Search in DefaultSection
            if (document.DefaultSection.TryGetProperty(propertyName, out var defaultProperty))
            {
                yield return (document.DefaultSection, defaultProperty!);
            }

            // Search in all sections
            foreach (var section in document)
            {
                if (section.TryGetProperty(propertyName, out var property))
                {
                    yield return (section, property!);
                }
            }
        }

        /// <summary>
        /// Searches the entire document for properties with the specified value.
        /// </summary>
        /// <param name="document">The document to search.</param>
        /// <param name="value">The exact value to find.</param>
        /// <returns>Tuples of (Section, Property) for each matching property.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        public static IEnumerable<(Section Section, Property Property)> FindPropertiesByValue(this Document document, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Search in DefaultSection
            foreach (var property in document.DefaultSection)
            {
                if (property.Value == value)
                {
                    yield return (document.DefaultSection, property);
                }
            }

            // Search in all sections
            foreach (var section in document)
            {
                foreach (var property in section)
                {
                    if (property.Value == value)
                    {
                        yield return (section, property);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new document containing only sections that match the filter.
        /// </summary>
        /// <param name="source">The source document.</param>
        /// <param name="sectionFilter">The function to test each section.</param>
        /// <returns>A new document with filtered sections (DefaultSection properties are always copied).</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null.</exception>
        public static Document CopyWithSections(this Document source, Func<Section, bool> sectionFilter)
        {
            if (sectionFilter == null)
                throw new ArgumentNullException(nameof(sectionFilter));

            var newDoc = new Document();

            // Copy DefaultSection properties
            foreach (var property in source.DefaultSection.GetProperties())
            {
                newDoc.DefaultSection.AddProperty(property.Clone());
            }

            // Copy filtered sections
            foreach (var section in source.Where(sectionFilter))
            {
                newDoc.AddSection(section.Clone());
            }

            return newDoc;
        }

        /// <summary>
        /// Creates a new section containing only properties that match the filter.
        /// </summary>
        /// <param name="source">The source section.</param>
        /// <param name="propertyFilter">The function to test each property.</param>
        /// <returns>A new section with filtered properties.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null.</exception>
        public static Section CopyWithProperties(this Section source, Func<Property, bool> propertyFilter)
        {
            if (propertyFilter == null)
                throw new ArgumentNullException(nameof(propertyFilter));

            var newSection = new Section(source.Name);
            foreach (var property in source.Where(propertyFilter))
            {
                newSection.AddProperty(property.Clone());
            }

            return newSection;
        }
    }
}
