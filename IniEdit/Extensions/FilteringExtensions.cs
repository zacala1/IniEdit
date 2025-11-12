namespace IniEdit
{
    public static class FilteringExtensions
    {
        // Section filtering
        public static IEnumerable<Section> GetSectionsWhere(this Document document, Func<Section, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return document.Where(predicate);
        }

        public static IEnumerable<Section> GetSectionsByPattern(this Document document, string namePattern)
        {
            if (string.IsNullOrEmpty(namePattern))
                throw new ArgumentException("Name pattern cannot be null or empty", nameof(namePattern));

            var regex = new System.Text.RegularExpressions.Regex(namePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return document.Where(s => regex.IsMatch(s.Name));
        }

        // Property filtering
        public static IEnumerable<Property> GetPropertiesWhere(this Section section, Func<Property, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return section.Where(predicate);
        }

        public static IEnumerable<Property> GetPropertiesByPattern(this Section section, string namePattern)
        {
            if (string.IsNullOrEmpty(namePattern))
                throw new ArgumentException("Name pattern cannot be null or empty", nameof(namePattern));

            var regex = new System.Text.RegularExpressions.Regex(namePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return section.Where(p => regex.IsMatch(p.Name));
        }

        public static IEnumerable<Property> GetPropertiesWithValue(this Section section, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return section.Where(p => p.Value == value);
        }

        public static IEnumerable<Property> GetPropertiesContaining(this Section section, string substring)
        {
            if (substring == null)
                throw new ArgumentNullException(nameof(substring));

            return section.Where(p => p.Value.Contains(substring));
        }

        // Document-wide property search
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

        // Filtered copy
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
