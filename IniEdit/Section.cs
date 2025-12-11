using System.Collections;
using System.Runtime.CompilerServices;
using static IniEdit.IniConfigOption;

namespace IniEdit
{
    /// <summary>
    /// Represents a section in an INI configuration file containing properties.
    /// </summary>
    /// <remarks>
    /// This class is NOT thread-safe. External synchronization is required for concurrent access.
    /// </remarks>
    public class Section : ElementBase, IEnumerable<Property>
    {
        private readonly List<Property> _properties;
        private readonly Dictionary<string, Property> _propertyLookup;

        /// <summary>
        /// Gets the number of properties in this section.
        /// </summary>
        public int PropertyCount => _properties.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        public Section(string name) : base(name)
        {
            _properties = new List<Property>();
            _propertyLookup = new Dictionary<string, Property>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the property.</param>
        /// <returns>The property at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public Property this[int index]
        {
            get
            {
                var property = GetProperty(index);
                if (property == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return property;
            }
        }

        /// <summary>
        /// Gets the property with the specified key. Creates a new property if it doesn't exist.
        /// </summary>
        /// <param name="key">The key of the property.</param>
        /// <returns>The property with the specified key.</returns>
        /// <remarks>
        /// WARNING: This indexer auto-creates properties if they don't exist. This means typos
        /// in property keys will silently create empty properties. Use <see cref="TryGetProperty"/>
        /// or <see cref="HasProperty"/> to check for existence without auto-creating.
        /// </remarks>
        public Property this[string key]
        {
            get
            {
                var property = GetProperty(key);
                if (property == null)
                {
                    property = new Property(key, string.Empty);
                    _properties.Add(property);
                    _propertyLookup[key] = property;
                }

                return property;
            }
        }

        /// <summary>
        /// Gets the property with the specified key.
        /// </summary>
        /// <param name="key">The key of the property (case-insensitive).</param>
        /// <returns>The property if found; otherwise, null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Property? GetProperty(string key)
        {
            _propertyLookup.TryGetValue(key, out var property);
            return property;
        }

        /// <summary>
        /// Tries to get the property with the specified key.
        /// </summary>
        /// <param name="key">The key of the property (case-insensitive).</param>
        /// <param name="property">The property if found; otherwise, null.</param>
        /// <returns>True if the property was found; otherwise, false.</returns>
        public bool TryGetProperty(string key, out Property? property)
        {
            property = GetProperty(key);
            return property != null;
        }

        /// <summary>
        /// Gets the property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the property.</param>
        /// <returns>The property if found; otherwise, null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Property? GetProperty(int index)
        {
            if ((uint)index >= (uint)_properties.Count) return null;

            return _properties[index];
        }

        /// <summary>
        /// Adds a new property with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        public void AddProperty(string name, string value)
        {
            AddProperty(new Property(name, value));
        }

        /// <summary>
        /// Adds the specified property to the section.
        /// </summary>
        /// <param name="property">The property to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a property with the same key already exists.</exception>
        public void AddProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (HasProperty(property.Name))
                throw new ArgumentException("The specified key already exists in the section.");

            _properties.Add(property);
            _propertyLookup[property.Name] = property;
        }

        /// <summary>
        /// Adds a collection of properties to the section.
        /// </summary>
        /// <param name="collection">The properties to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when collection is null.</exception>
        public void AddPropertyRange(IEnumerable<Property> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var property in collection)
            {
                if (property == null) continue;
                AddProperty(property);
            }
        }

        /// <summary>
        /// Inserts a new property before the property with the specified target key.
        /// </summary>
        /// <param name="targetKey">The key of the property before which to insert.</param>
        /// <param name="name">The name of the property to insert.</param>
        /// <param name="value">The value of the property to insert.</param>
        public void InsertProperty(string targetKey, string name, string value)
        {
            InsertProperty(targetKey, new Property(name, value));
        }

        /// <summary>
        /// Inserts the specified property before the property with the specified target key.
        /// </summary>
        /// <param name="targetKey">The key of the property before which to insert (case-insensitive).</param>
        /// <param name="property">The property to insert.</param>
        /// <exception cref="ArgumentException">Thrown when target key is empty.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when target key is not found.</exception>
        public void InsertProperty(string targetKey, Property property)
        {
            if (string.IsNullOrEmpty(targetKey))
                throw new ArgumentException("Target key cannot be empty", nameof(targetKey));

            // Use O(1) dictionary lookup instead of O(n) FindIndex
            if (!_propertyLookup.TryGetValue(targetKey, out var targetProperty))
                throw new KeyNotFoundException($"Target key '{targetKey}' not found");

            var index = _properties.IndexOf(targetProperty);
            InsertProperty(index, property);
        }

        /// <summary>
        /// Inserts a new property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the property.</param>
        /// <param name="name">The name of the property to insert.</param>
        /// <param name="value">The value of the property to insert.</param>
        public void InsertProperty(int index, string name, string value)
        {
            InsertProperty(index, new Property(name, value));
        }

        /// <summary>
        /// Inserts the specified property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the property.</param>
        /// <param name="property">The property to insert.</param>
        /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
        /// <exception cref="ArgumentException">Thrown when a property with the same key already exists.</exception>
        public void InsertProperty(int index, Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if ((uint)index > (uint)_properties.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (HasProperty(property.Name))
                throw new ArgumentException("The specified key already exists in the section.");

            _properties.Insert(index, property);
            _propertyLookup[property.Name] = property;
        }

        /// <summary>
        /// Removes the property with the specified name.
        /// </summary>
        /// <param name="name">The name of the property to remove (case-insensitive).</param>
        /// <returns>True if the property was removed; otherwise, false.</returns>
        public bool RemoveProperty(string name)
        {
            if (_propertyLookup.TryGetValue(name, out var property))
            {
                _properties.Remove(property);
                _propertyLookup.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the property to remove.</param>
        /// <returns>True if the property was removed; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveProperty(int index)
        {
            if ((uint)index >= (uint)_properties.Count) return false;
            var property = _properties[index];
            _properties.RemoveAt(index);
            _propertyLookup.Remove(property.Name);
            return true;
        }

        /// <summary>
        /// Determines whether the section contains the specified property.
        /// </summary>
        /// <param name="value">The property to locate.</param>
        /// <returns>True if the property exists; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        public bool HasProperty(Property value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return HasProperty(value.Name);
        }

        /// <summary>
        /// Determines whether the section contains a property with the specified key.
        /// </summary>
        /// <param name="key">The key of the property to locate (case-insensitive).</param>
        /// <returns>True if the property exists; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public bool HasProperty(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));

            return _propertyLookup.ContainsKey(key);
        }

        /// <summary>
        /// Sets the value of the property with the specified key. Creates a new property if it doesn't exist.
        /// </summary>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The value to set.</param>
        public void SetProperty(string key, string value)
        {
            var property = GetProperty(key);
            if (property != null)
            {
                property.Value = value;
            }
            else
            {
                AddProperty(key, value);
            }
        }

        /// <summary>
        /// Sets the typed value of the property with the specified key. Creates a new property if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The value to set.</param>
        public void SetProperty<T>(string key, T value)
        {
            var property = GetProperty(key);
            if (property != null)
            {
                property.SetValue(value);
            }
            else
            {
                var newProperty = new Property(key);
                newProperty.SetValue(value);
                AddProperty(newProperty);
            }
        }

        /// <summary>
        /// Gets the value of the property with the specified key, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="key">The key of the property (case-insensitive).</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property doesn't exist.</exception>
        public T GetPropertyValue<T>(string key)
        {
            var property = GetProperty(key);
            if (property == null)
                throw new InvalidOperationException($"Property '{key}' not found in section '{Name}'");
            return property.GetValue<T>();
        }

        /// <summary>
        /// Gets the value of the property with the specified key, or a default value if the property doesn't exist or conversion fails.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="key">The key of the property (case-insensitive).</param>
        /// <param name="defaultValue">The default value to return if the property doesn't exist or conversion fails.</param>
        /// <returns>The converted value, or the default value.</returns>
        public T GetPropertyValueOrDefault<T>(string key, T defaultValue = default!)
        {
            var property = GetProperty(key);
            if (property == null)
                return defaultValue;
            return property.GetValueOrDefault(defaultValue);
        }

        /// <summary>
        /// Tries to get the value of the property with the specified key, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="key">The key of the property (case-insensitive).</param>
        /// <param name="value">The converted value if successful; otherwise, the default value.</param>
        /// <returns>True if the property exists and conversion succeeded; otherwise, false.</returns>
        public bool TryGetPropertyValue<T>(string key, out T value)
        {
            var property = GetProperty(key);
            if (property == null)
            {
                value = default!;
                return false;
            }
            return property.TryGetValue(out value);
        }

        /// <summary>
        /// Removes all properties and comments from the section.
        /// </summary>
        public void Clear()
        {
            PreComments.Clear();
            Comment = null;
            _properties.Clear();
            _propertyLookup.Clear();
        }

        /// <summary>
        /// Merges properties from another section using the specified duplicate key policy.
        /// </summary>
        /// <param name="section">The section to merge from.</param>
        /// <param name="policy">The policy for handling duplicate keys.</param>
        /// <exception cref="ArgumentNullException">Thrown when section is null.</exception>
        public void MergeFrom(Section section, DuplicateKeyPolicyType policy = DuplicateKeyPolicyType.FirstWin)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            var clone = section.Clone();
            if (policy == DuplicateKeyPolicyType.ThrowError)
            {
                MergeFromOnThrowError(clone);
            }
            else if (policy == DuplicateKeyPolicyType.LastWin)
            {
                MergeFromOnLastWin(clone);
            }
            else
            {
                MergeFromOnFirstWin(clone);
            }
        }

        private void MergeFromOnFirstWin(Section section)
        {
            foreach (var property in section)
            {
                if (HasProperty(property.Name)) continue;
                AddProperty(property);
            }
        }

        private void MergeFromOnLastWin(Section section)
        {
            PreComments.Clear();
            PreComments.AddRange(section.PreComments);
            Comment = section.Comment;

            // Build index map for O(1) position lookup during merge (O(n+m) instead of O(n*m))
            var indexMap = new Dictionary<string, int>(_properties.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _properties.Count; i++)
            {
                indexMap[_properties[i].Name] = i;
            }

            foreach (var property in section)
            {
                if (indexMap.TryGetValue(property.Name, out var index))
                {
                    _properties[index] = property;
                    _propertyLookup[property.Name] = property;
                }
                else
                {
                    AddProperty(property);
                }
            }
        }

        private void MergeFromOnThrowError(Section section)
        {
            if (section.Any(property => HasProperty(property.Name)))
            {
                throw new ArgumentException("Section contains one or more properties that already exist");
            }

            PreComments.AddRange(section.PreComments);
            AppendComment(section.Comment);
            foreach (var property in section)
            {
                _properties.Add(property);
                _propertyLookup[property.Name] = property;
            }
        }

        /// <summary>
        /// Creates a deep copy of this section.
        /// </summary>
        /// <returns>A new section with the same properties and comments.</returns>
        public Section Clone()
        {
            var clone = new Section(Name);

            var properties = new List<Property>(_properties.Count);
            foreach (var item in _properties)
            {
                if (item != null)
                    properties.Add(item.Clone());
            }
            clone.AddPropertyRange(properties);

            var preComments = new List<Comment>(PreComments.Count);
            foreach (var item in PreComments)
            {
                if (item != null)
                    preComments.Add(item.Clone());
            }
            clone.PreComments.AddRange(preComments);

            clone.Comment = Comment?.Clone();

            return clone;
        }

        /// <summary>
        /// Gets a read-only collection of all properties in the section.
        /// </summary>
        /// <returns>A read-only list of properties.</returns>
        public IReadOnlyList<Property> GetProperties() => _properties;

        internal List<Property> GetInternalProperties() => _properties;

        /// <summary>
        /// Returns an enumerator that iterates through the properties.
        /// </summary>
        /// <returns>An enumerator for the properties.</returns>
        public IEnumerator<Property> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a property with the specified key and value, returning this section for fluent chaining.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This section instance.</returns>
        public Section WithProperty(string key, string value)
        {
            AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds a property with the specified key and typed value, returning this section for fluent chaining.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This section instance.</returns>
        public Section WithProperty<T>(string key, T value)
        {
            SetProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds an inline comment to this section, returning this section for fluent chaining.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This section instance.</returns>
        public Section WithComment(string comment)
        {
            Comment = new Comment(comment);
            return this;
        }

        /// <summary>
        /// Adds a pre-comment to this section, returning this section for fluent chaining.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This section instance.</returns>
        public Section WithPreComment(string comment)
        {
            PreComments.Add(new Comment(comment));
            return this;
        }
    }
}
