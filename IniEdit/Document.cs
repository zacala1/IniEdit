using System.Collections;
using System.Runtime.CompilerServices;

namespace IniEdit
{
    /// <summary>
    /// Represents an INI configuration document containing sections and properties.
    /// </summary>
    /// <remarks>
    /// This class is NOT thread-safe. Do not access the same Document instance from multiple
    /// threads simultaneously without external synchronization. Each thread should use its
    /// own Document instance or use locks for concurrent access.
    /// </remarks>
    public class Document : IEnumerable<Section>
    {
        /// <summary>
        /// The name used for the default section (properties without explicit section header).
        /// </summary>
        public const string DefaultSectionName = "$DEFAULT";

        private readonly List<Section> _sections;
        private readonly Dictionary<string, Section> _sectionLookup;
        private readonly List<ParsingErrorEventArgs> _parsingErrors;

        /// <summary>
        /// Gets the allowed comment prefix characters for this document.
        /// </summary>
        public char[] CommentPrefixChars { get; }

        private char _defaultCommentPrefixChar;

        /// <summary>
        /// Gets or sets the default comment prefix character used when writing comments.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the value is not in <see cref="CommentPrefixChars"/>.</exception>
        public char DefaultCommentPrefixChar
        {
            get { return _defaultCommentPrefixChar; }
            set
            {
                if (!Array.Exists(CommentPrefixChars, (char c) => c == value))
                {
                    throw new ArgumentException("Invalid character prefix");
                }
                _defaultCommentPrefixChar = value;
            }
        }

        /// <summary>
        /// Gets the default section that contains properties without an explicit section header.
        /// </summary>
        public Section DefaultSection { get; }

        /// <summary>
        /// Gets the number of sections in this document (excluding the default section).
        /// </summary>
        public int SectionCount => _sections.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="option">Configuration options for the document.</param>
        public Document(IniConfigOption? option = null)
        {
            if (option == null)
            {
                option = new IniConfigOption();
            }

            _sections = new List<Section>();
            _sectionLookup = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
            _parsingErrors = new List<ParsingErrorEventArgs>();
            DefaultSection = new Section(DefaultSectionName);
            CommentPrefixChars = option.CommentPrefixChars.ToArray();
            DefaultCommentPrefixChar = option.DefaultCommentPrefixChar;
        }

        /// <summary>
        /// Gets the collection of parsing errors encountered during file loading.
        /// </summary>
        public IReadOnlyList<ParsingErrorEventArgs> ParsingErrors => _parsingErrors;

        internal void AddParsingError(ParsingErrorEventArgs error)
        {
            _parsingErrors.Add(error);
        }

        /// <summary>
        /// Gets the section at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the section.</param>
        /// <returns>The section at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public Section this[int index]
        {
            get
            {
                var section = GetSectionByIndex(index);
                if (section == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return section;
            }
        }

        /// <summary>
        /// Gets the section with the specified name. Creates a new section if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        /// <returns>The section with the specified name.</returns>
        /// <remarks>
        /// WARNING: This indexer auto-creates sections if they don't exist. This means typos
        /// in section names will silently create empty sections. Use <see cref="TryGetSection"/>
        /// or <see cref="HasSection"/> to check for existence without auto-creating.
        /// </remarks>
        public Section this[string name]
        {
            get
            {
                var section = GetSection(name);
                if (section == null)
                {
                    section = new Section(name);
                    _sections.Add(section);
                    _sectionLookup[name] = section;
                }

                return section;
            }
        }

        /// <summary>
        /// Gets the section with the specified name.
        /// </summary>
        /// <param name="name">The name of the section (case-insensitive).</param>
        /// <returns>The section if found; otherwise, null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Section? GetSection(string name)
        {
            _sectionLookup.TryGetValue(name, out var section);
            return section;
        }

        /// <summary>
        /// Tries to get the section with the specified name.
        /// </summary>
        /// <param name="name">The name of the section (case-insensitive).</param>
        /// <param name="section">The section if found; otherwise, null.</param>
        /// <returns>True if the section was found; otherwise, false.</returns>
        public bool TryGetSection(string name, out Section? section)
        {
            section = GetSection(name);
            return section != null;
        }

        /// <summary>
        /// Gets the section at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the section.</param>
        /// <returns>The section if found; otherwise, null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Section? GetSectionByIndex(int index)
        {
            if ((uint)index >= (uint)_sections.Count) return null;

            return _sections[index];
        }

        /// <summary>
        /// Adds a new section with the specified name.
        /// </summary>
        /// <param name="name">The name of the section to add.</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid or a section with the same name already exists.</exception>
        public void AddSection(string name)
        {
            var section = new Section(name);
            if (HasSection(section))
                throw new ArgumentException("The specified section already exists.");

            _sections.Add(section);
            _sectionLookup[name] = section;
        }

        /// <summary>
        /// Adds the specified section to the document.
        /// </summary>
        /// <param name="section">The section to add.</param>
        /// <exception cref="ArgumentException">Thrown when a section with the same name already exists.</exception>
        public void AddSection(Section section)
        {
            if (HasSection(section))
                throw new ArgumentException("The specified section already exists.");

            _sections.Add(section);
            _sectionLookup[section.Name] = section;
        }

        /// <summary>
        /// Inserts a new section with the specified name at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the section.</param>
        /// <param name="name">The name of the section to insert.</param>
        public void InsertSection(int index, string name)
        {
            InsertSection(index, new Section(name));
        }

        /// <summary>
        /// Inserts the specified section at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the section.</param>
        /// <param name="section">The section to insert.</param>
        /// <exception cref="ArgumentException">Thrown when a section with the same name already exists.</exception>
        public void InsertSection(int index, Section section)
        {
            if (HasSection(section))
                throw new ArgumentException("The specified section already exists.");

            _sections.Insert(index, section);
            _sectionLookup[section.Name] = section;
        }

        /// <summary>
        /// Removes the section with the specified name.
        /// </summary>
        /// <param name="name">The name of the section to remove (case-insensitive).</param>
        /// <returns>True if the section was removed; otherwise, false.</returns>
        public bool RemoveSection(string name)
        {
            if (_sectionLookup.TryGetValue(name, out var section))
            {
                _sections.Remove(section);
                _sectionLookup.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the section at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the section to remove.</param>
        /// <returns>True if the section was removed; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveSection(int index)
        {
            if ((uint)index >= (uint)_sections.Count) return false;
            var section = _sections[index];
            _sections.RemoveAt(index);
            _sectionLookup.Remove(section.Name);
            return true;
        }

        /// <summary>
        /// Determines whether the document contains the specified section.
        /// </summary>
        /// <param name="section">The section to locate.</param>
        /// <returns>True if the section exists; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when section is null.</exception>
        public bool HasSection(Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            return HasSection(section.Name);
        }

        /// <summary>
        /// Determines whether the document contains a section with the specified name.
        /// </summary>
        /// <param name="name">The name of the section to locate (case-insensitive).</param>
        /// <returns>True if the section exists; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
        public bool HasSection(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Section name cannot be null or empty", nameof(name));

            return _sectionLookup.ContainsKey(name);
        }

        /// <summary>
        /// Gets the value of a property from the specified section, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="sectionName">The name of the section (case-insensitive).</param>
        /// <param name="propertyKey">The key of the property (case-insensitive).</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the section or property doesn't exist.</exception>
        public T GetValue<T>(string sectionName, string propertyKey)
        {
            var section = GetSection(sectionName);
            if (section == null)
                throw new InvalidOperationException($"Section '{sectionName}' not found");
            return section.GetPropertyValue<T>(propertyKey);
        }

        /// <summary>
        /// Gets the value of a property from the specified section, or a default value if the section/property doesn't exist or conversion fails.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="sectionName">The name of the section (case-insensitive).</param>
        /// <param name="propertyKey">The key of the property (case-insensitive).</param>
        /// <param name="defaultValue">The default value to return if the section/property doesn't exist or conversion fails.</param>
        /// <returns>The converted value, or the default value.</returns>
        public T GetValueOrDefault<T>(string sectionName, string propertyKey, T defaultValue = default!)
        {
            var section = GetSection(sectionName);
            if (section == null)
                return defaultValue;
            return section.GetPropertyValueOrDefault(propertyKey, defaultValue);
        }

        /// <summary>
        /// Tries to get the value of a property from the specified section, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="sectionName">The name of the section (case-insensitive).</param>
        /// <param name="propertyKey">The key of the property (case-insensitive).</param>
        /// <param name="value">The converted value if successful; otherwise, the default value.</param>
        /// <returns>True if the section and property exist and conversion succeeded; otherwise, false.</returns>
        public bool TryGetValue<T>(string sectionName, string propertyKey, out T value)
        {
            var section = GetSection(sectionName);
            if (section == null)
            {
                value = default!;
                return false;
            }
            return section.TryGetPropertyValue(propertyKey, out value);
        }

        /// <summary>
        /// Removes all sections from the document.
        /// </summary>
        public void Clear()
        {
            _sections.Clear();
            _sectionLookup.Clear();
        }

        /// <summary>
        /// Gets a read-only collection of all sections in the document.
        /// </summary>
        /// <returns>A read-only list of sections.</returns>
        public IReadOnlyList<Section> GetSections() => _sections;

        internal List<Section> GetInternalSections() => _sections;

        /// <summary>
        /// Adds a section to the internal list without duplicate checking. Used by IniConfigManager during parsing.
        /// </summary>
        /// <param name="section">The section to add.</param>
        internal void AddSectionInternal(Section section)
        {
            _sections.Add(section);
            _sectionLookup[section.Name] = section;
        }

        /// <summary>
        /// Rebuilds the section lookup dictionary from the current section list. Used after bulk operations.
        /// </summary>
        internal void RebuildSectionLookup()
        {
            _sectionLookup.Clear();
            foreach (var section in _sections)
            {
                if (section != null)
                {
                    _sectionLookup[section.Name] = section;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the sections.
        /// </summary>
        /// <returns>An enumerator for the sections.</returns>
        public IEnumerator<Section> GetEnumerator()
        {
            return _sections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a section with the specified name, returning this document for fluent chaining.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <returns>This document instance.</returns>
        public Document WithSection(string name)
        {
            AddSection(name);
            return this;
        }

        /// <summary>
        /// Adds a section, returning this document for fluent chaining.
        /// </summary>
        /// <param name="section">The section to add.</param>
        /// <returns>This document instance.</returns>
        public Document WithSection(Section section)
        {
            AddSection(section);
            return this;
        }

        /// <summary>
        /// Adds a property to the default section, returning this document for fluent chaining.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This document instance.</returns>
        public Document WithDefaultProperty(string key, string value)
        {
            DefaultSection.AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds a typed property to the default section, returning this document for fluent chaining.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This document instance.</returns>
        public Document WithDefaultProperty<T>(string key, T value)
        {
            DefaultSection.SetProperty(key, value);
            return this;
        }
    }
}
