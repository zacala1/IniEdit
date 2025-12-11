namespace IniEdit
{
    public class DocumentDiff
    {
        public List<Section> AddedSections { get; }
        public List<Section> RemovedSections { get; }
        public List<SectionDiff> ModifiedSections { get; }

        public DocumentDiff()
        {
            AddedSections = new List<Section>();
            RemovedSections = new List<Section>();
            ModifiedSections = new List<SectionDiff>();
        }

        public bool HasChanges => AddedSections.Count > 0 || RemovedSections.Count > 0 || ModifiedSections.Count > 0;
    }

    public class SectionDiff
    {
        public string SectionName { get; }
        public List<Property> AddedProperties { get; }
        public List<Property> RemovedProperties { get; }
        public List<PropertyDiff> ModifiedProperties { get; }

        public SectionDiff(string sectionName)
        {
            SectionName = sectionName;
            AddedProperties = new List<Property>();
            RemovedProperties = new List<Property>();
            ModifiedProperties = new List<PropertyDiff>();
        }

        public bool HasChanges => AddedProperties.Count > 0 || RemovedProperties.Count > 0 || ModifiedProperties.Count > 0;
    }

    public class PropertyDiff
    {
        public string PropertyName { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public PropertyDiff(string propertyName, string oldValue, string newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public static class DocumentDiffExtensions
    {
        /// <summary>
        /// Compares two documents and returns a diff containing the changes.
        /// </summary>
        /// <param name="original">The original document.</param>
        /// <param name="modified">The modified document.</param>
        /// <returns>A DocumentDiff containing added, removed, and modified sections.</returns>
        /// <exception cref="ArgumentNullException">Thrown when original or modified is null.</exception>
        public static DocumentDiff Compare(this Document original, Document modified)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            if (modified == null)
                throw new ArgumentNullException(nameof(modified));

            var diff = new DocumentDiff();

            // Compare DefaultSection
            var defaultDiff = CompareSections(original.DefaultSection, modified.DefaultSection);
            if (defaultDiff.HasChanges)
            {
                diff.ModifiedSections.Add(defaultDiff);
            }

            // Find added and modified sections
            foreach (var modifiedSection in modified)
            {
                if (!original.TryGetSection(modifiedSection.Name, out var originalSection))
                {
                    // Section was added
                    diff.AddedSections.Add(modifiedSection.Clone());
                }
                else
                {
                    // Section exists in both, check properties
                    var sectionDiff = CompareSections(originalSection!, modifiedSection);
                    if (sectionDiff.HasChanges)
                    {
                        diff.ModifiedSections.Add(sectionDiff);
                    }
                }
            }

            // Find removed sections
            foreach (var originalSection in original)
            {
                if (!modified.TryGetSection(originalSection.Name, out _))
                {
                    diff.RemovedSections.Add(originalSection.Clone());
                }
            }

            return diff;
        }

        private static SectionDiff CompareSections(Section original, Section modified)
        {
            var sectionDiff = new SectionDiff(original.Name);

            // Find added and modified properties
            foreach (var modifiedProperty in modified)
            {
                if (!original.TryGetProperty(modifiedProperty.Name, out var originalProperty))
                {
                    // Property was added
                    sectionDiff.AddedProperties.Add(modifiedProperty.Clone());
                }
                else if (originalProperty!.Value != modifiedProperty.Value)
                {
                    // Property was modified
                    sectionDiff.ModifiedProperties.Add(new PropertyDiff(
                        modifiedProperty.Name,
                        originalProperty.Value,
                        modifiedProperty.Value));
                }
            }

            // Find removed properties
            foreach (var originalProperty in original)
            {
                if (!modified.TryGetProperty(originalProperty.Name, out _))
                {
                    sectionDiff.RemovedProperties.Add(originalProperty.Clone());
                }
            }

            return sectionDiff;
        }
    }
}
