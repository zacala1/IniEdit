namespace IniEdit
{
    /// <summary>
    /// Extension methods for sorting sections and properties in INI documents.
    /// </summary>
    /// <remarks>
    /// All operations are in-place with O(n log n) complexity and use case-insensitive ordinal comparison.
    /// These methods are NOT thread-safe; use external synchronization for concurrent access.
    /// </remarks>
    public static class DocumentExtensions
    {
        #region Section Property Sorting

        /// <summary>
        /// Sorts all properties in the section alphabetically by name.
        /// </summary>
        /// <param name="section">The section to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        public static void SortPropertiesByName(this Section section, bool descending = false)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            var multiplier = descending ? -1 : 1;
            section.GetInternalProperties().Sort((a, b) =>
                multiplier * string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sorts all properties in the section by their values.
        /// </summary>
        /// <param name="section">The section to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        public static void SortPropertiesByValue(this Section section, bool descending = false)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            var multiplier = descending ? -1 : 1;
            section.GetInternalProperties().Sort((a, b) =>
                multiplier * string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sorts all properties in the section using a custom comparison.
        /// </summary>
        /// <param name="section">The section to sort.</param>
        /// <param name="comparison">The comparison delegate.</param>
        public static void SortProperties(this Section section, Comparison<Property> comparison)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));

            section.GetInternalProperties().Sort(comparison);
        }

        #endregion

        #region Document Property Sorting

        /// <summary>
        /// Sorts properties in all sections alphabetically by name.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        /// <param name="includeDefaultSection">If true, also sorts the default section.</param>
        public static void SortPropertiesByName(this Document doc, bool descending = false, bool includeDefaultSection = false)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (includeDefaultSection)
            {
                doc.DefaultSection.SortPropertiesByName(descending);
            }

            foreach (var section in doc.GetInternalSections())
            {
                section.SortPropertiesByName(descending);
            }
        }

        /// <summary>
        /// Sorts properties in all sections by their values.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        /// <param name="includeDefaultSection">If true, also sorts the default section.</param>
        public static void SortPropertiesByValue(this Document doc, bool descending = false, bool includeDefaultSection = false)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (includeDefaultSection)
            {
                doc.DefaultSection.SortPropertiesByValue(descending);
            }

            foreach (var section in doc.GetInternalSections())
            {
                section.SortPropertiesByValue(descending);
            }
        }

        /// <summary>
        /// Sorts properties in all sections using a custom comparison.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="comparison">The comparison delegate.</param>
        /// <param name="includeDefaultSection">If true, also sorts the default section.</param>
        public static void SortProperties(this Document doc, Comparison<Property> comparison, bool includeDefaultSection = false)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));

            if (includeDefaultSection)
            {
                doc.DefaultSection.SortProperties(comparison);
            }

            foreach (var section in doc.GetInternalSections())
            {
                section.SortProperties(comparison);
            }
        }

        #endregion

        #region Section Sorting

        /// <summary>
        /// Sorts all sections in the document alphabetically by name.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        public static void SortSectionsByName(this Document doc, bool descending = false)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var multiplier = descending ? -1 : 1;
            doc.GetInternalSections().Sort((a, b) =>
                multiplier * string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sorts all sections in the document using a custom comparison.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="comparison">The comparison delegate.</param>
        public static void SortSections(this Document doc, Comparison<Section> comparison)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));

            doc.GetInternalSections().Sort(comparison);
        }

        #endregion

        #region Combined Sorting

        /// <summary>
        /// Sorts both sections and properties alphabetically by name.
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <param name="descending">If true, sorts in descending order.</param>
        /// <param name="includeDefaultSection">If true, also sorts the default section.</param>
        public static void SortAllByName(this Document doc, bool descending = false, bool includeDefaultSection = false)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            SortSectionsByName(doc, descending);
            SortPropertiesByName(doc, descending, includeDefaultSection);
        }

        #endregion
    }
}
