using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace IniEdit
{
    /// <summary>
    /// Provides extension methods for sorting sections and properties in INI documents.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Sorts all properties in the section alphabetically by name (case-insensitive).
        /// </summary>
        /// <param name="section">The section to sort.</param>
        /// <exception cref="ArgumentNullException">Thrown when section is null.</exception>
        public static void SortPropertiesByName(this Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            section.GetInternalProperties().Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sorts properties in all sections of the document alphabetically by name (case-insensitive).
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <exception cref="ArgumentNullException">Thrown when doc is null.</exception>
        public static void SortPropertiesByName(this Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            foreach (var section in doc.GetInternalSections())
            {
                section.SortPropertiesByName();
            }
        }

        /// <summary>
        /// Sorts all sections in the document alphabetically by name (case-insensitive).
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <exception cref="ArgumentNullException">Thrown when doc is null.</exception>
        public static void SortSectionsByName(this Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            doc.GetInternalSections().Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sorts both sections and properties in the document alphabetically by name (case-insensitive).
        /// </summary>
        /// <param name="doc">The document to sort.</param>
        /// <exception cref="ArgumentNullException">Thrown when doc is null.</exception>
        public static void SortAllByName(this Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            SortSectionsByName(doc);
            SortPropertiesByName(doc);
        }
    }
}
