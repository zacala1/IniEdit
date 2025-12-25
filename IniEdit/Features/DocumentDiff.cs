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
    /// Options for merging document differences.
    /// </summary>
    public sealed class MergeOptions
    {
        /// <summary>
        /// Gets or sets whether to apply added sections.
        /// </summary>
        public bool ApplyAddedSections { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to apply removed sections.
        /// </summary>
        public bool ApplyRemovedSections { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to apply added properties.
        /// </summary>
        public bool ApplyAddedProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to apply removed properties.
        /// </summary>
        public bool ApplyRemovedProperties { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to apply modified properties.
        /// </summary>
        public bool ApplyModifiedProperties { get; set; } = true;
    }

    /// <summary>
    /// Represents the result of a merge operation.
    /// </summary>
    public sealed class MergeResult
    {
        /// <summary>
        /// Gets or sets the number of sections added.
        /// </summary>
        public int SectionsAdded { get; set; }

        /// <summary>
        /// Gets or sets the number of sections removed.
        /// </summary>
        public int SectionsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the number of properties added.
        /// </summary>
        public int PropertiesAdded { get; set; }

        /// <summary>
        /// Gets or sets the number of properties removed.
        /// </summary>
        public int PropertiesRemoved { get; set; }

        /// <summary>
        /// Gets or sets the number of properties modified.
        /// </summary>
        public int PropertiesModified { get; set; }

        /// <summary>
        /// Gets the total number of changes applied.
        /// </summary>
        public int TotalChanges => SectionsAdded + SectionsRemoved + PropertiesAdded + PropertiesRemoved + PropertiesModified;
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

        /// <summary>
        /// Applies the differences from a DocumentDiff to a target document.
        /// </summary>
        /// <param name="target">The document to apply changes to.</param>
        /// <param name="diff">The differences to apply.</param>
        /// <param name="options">Optional merge options.</param>
        /// <returns>A result containing the number of changes applied.</returns>
        public static MergeResult Merge(this Document target, DocumentDiff diff, MergeOptions? options = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (diff == null)
                throw new ArgumentNullException(nameof(diff));

            options ??= new MergeOptions();
            var result = new MergeResult();

            // Apply added sections
            if (options.ApplyAddedSections)
            {
                foreach (var section in diff.AddedSections)
                {
                    if (!target.HasSection(section.Name))
                    {
                        target.AddSection(section.Clone());
                        result.SectionsAdded++;
                    }
                }
            }

            // Apply removed sections
            if (options.ApplyRemovedSections)
            {
                foreach (var section in diff.RemovedSections)
                {
                    if (target.HasSection(section.Name))
                    {
                        target.RemoveSection(section.Name);
                        result.SectionsRemoved++;
                    }
                }
            }

            // Apply modified sections
            foreach (var sectionDiff in diff.ModifiedSections)
            {
                var section = string.IsNullOrEmpty(sectionDiff.SectionName)
                    ? target.DefaultSection
                    : target.GetSection(sectionDiff.SectionName);

                if (section == null)
                {
                    section = new Section(sectionDiff.SectionName);
                    target.AddSection(section);
                }

                // Apply added properties
                if (options.ApplyAddedProperties)
                {
                    foreach (var prop in sectionDiff.AddedProperties)
                    {
                        if (!section.HasProperty(prop.Name))
                        {
                            section.AddProperty(prop.Clone());
                            result.PropertiesAdded++;
                        }
                    }
                }

                // Apply removed properties
                if (options.ApplyRemovedProperties)
                {
                    foreach (var prop in sectionDiff.RemovedProperties)
                    {
                        if (section.HasProperty(prop.Name))
                        {
                            section.RemoveProperty(prop.Name);
                            result.PropertiesRemoved++;
                        }
                    }
                }

                // Apply modified properties
                if (options.ApplyModifiedProperties)
                {
                    foreach (var propDiff in sectionDiff.ModifiedProperties)
                    {
                        var existingProp = section.GetProperty(propDiff.PropertyName);
                        if (existingProp != null)
                        {
                            existingProp.Value = propDiff.NewValue;
                            result.PropertiesModified++;
                        }
                    }
                }
            }

            return result;
        }
    }
}
