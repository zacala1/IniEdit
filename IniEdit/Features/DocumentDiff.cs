namespace IniEdit
{
    /// <summary>
    /// Represents the differences between two INI documents.
    /// </summary>
    public sealed class DocumentDiff
    {
        /// <summary>
        /// Gets the list of sections that were added in the modified document.
        /// </summary>
        public List<Section> AddedSections { get; }

        /// <summary>
        /// Gets the list of sections that were removed from the original document.
        /// </summary>
        public List<Section> RemovedSections { get; }

        /// <summary>
        /// Gets the list of sections that were modified between documents.
        /// </summary>
        public List<SectionDiff> ModifiedSections { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDiff"/> class.
        /// </summary>
        public DocumentDiff()
        {
            AddedSections = new List<Section>();
            RemovedSections = new List<Section>();
            ModifiedSections = new List<SectionDiff>();
        }

        /// <summary>
        /// Gets a value indicating whether there are any changes between the documents.
        /// </summary>
        public bool HasChanges => AddedSections.Count > 0 || RemovedSections.Count > 0 || ModifiedSections.Count > 0;
    }

    /// <summary>
    /// Represents the differences within a single section between two documents.
    /// </summary>
    public sealed class SectionDiff
    {
        /// <summary>
        /// Gets the name of the section being compared.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Gets the list of properties that were added to the section.
        /// </summary>
        public List<Property> AddedProperties { get; }

        /// <summary>
        /// Gets the list of properties that were removed from the section.
        /// </summary>
        public List<Property> RemovedProperties { get; }

        /// <summary>
        /// Gets the list of properties that were modified within the section.
        /// </summary>
        public List<PropertyDiff> ModifiedProperties { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionDiff"/> class.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        public SectionDiff(string sectionName)
        {
            SectionName = sectionName;
            AddedProperties = new List<Property>();
            RemovedProperties = new List<Property>();
            ModifiedProperties = new List<PropertyDiff>();
        }

        /// <summary>
        /// Gets a value indicating whether there are any changes within the section.
        /// </summary>
        public bool HasChanges => AddedProperties.Count > 0 || RemovedProperties.Count > 0 || ModifiedProperties.Count > 0;
    }

    /// <summary>
    /// Represents the difference in a single property's value.
    /// </summary>
    public sealed class PropertyDiff
    {
        /// <summary>
        /// Gets the name of the property that was modified.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the original value of the property.
        /// </summary>
        public string OldValue { get; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public string NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDiff"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="oldValue">The original value.</param>
        /// <param name="newValue">The new value.</param>
        public PropertyDiff(string propertyName, string oldValue, string newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Provides extension methods for comparing INI documents.
    /// </summary>
    public static class DocumentDiffExtensions
    {
        /// <summary>
        /// Compares two documents and returns a diff containing the changes.
        /// </summary>
        /// <param name="original">The original document.</param>
        /// <param name="modified">The modified document.</param>
        /// <returns>A <see cref="DocumentDiff"/> containing added, removed, and modified sections.</returns>
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
