using System.Runtime.CompilerServices;
using System.Text;

namespace IniEdit
{
    /// <summary>
    /// Represents a key-value property in an INI configuration file.
    /// </summary>
    /// <remarks>
    /// This class is NOT thread-safe. External synchronization is required for concurrent access.
    /// </remarks>
    public class Property : ElementBase
    {
        private string _value;

        /// <summary>
        /// Gets or sets the value of this property.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                SetStringValue(value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this property's value is null or empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        /// Gets or sets a value indicating whether this property's value should be quoted when written.
        /// </summary>
        public bool IsQuoted { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Property"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name (key) of the property.</param>
        public Property(string name) : this(name, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Property"/> class with the specified name and value.
        /// </summary>
        /// <param name="name">The name (key) of the property.</param>
        /// <param name="value">The value of the property.</param>
        public Property(string name, string value) : base(name)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value of this property converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The converted value.</returns>
        public T GetValue<T>()
        {
            if (typeof(T) == typeof(string)) return (T)(object)_value;

            return (T)Convert.ChangeType(_value, typeof(T));
        }

        /// <summary>
        /// Gets the value of this property converted to the specified type, or a default value if conversion fails.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="defaultValue">The default value to return if conversion fails.</param>
        /// <returns>The converted value, or the default value if conversion fails.</returns>
        /// <remarks>
        /// Only catches format-related exceptions. Critical exceptions like OutOfMemoryException are not caught.
        /// </remarks>
        public T GetValueOrDefault<T>(T defaultValue)
        {
            return TryGetValue<T>(out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the value of this property converted to the specified type, or the default value of T if conversion fails.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The converted value, or default(T) if conversion fails.</returns>
        /// <remarks>
        /// Only catches format-related exceptions. Critical exceptions like OutOfMemoryException are not caught.
        /// </remarks>
        public T GetValueOrDefault<T>()
        {
            return TryGetValue<T>(out var value) ? value : default!;
        }

        /// <summary>
        /// Tries to get the value of this property converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The converted value if successful; otherwise, the default value.</param>
        /// <returns>True if conversion succeeded; otherwise, false.</returns>
        public bool TryGetValue<T>(out T value)
        {
            try
            {
                value = GetValue<T>();
                return true;
            }
            catch (FormatException)
            {
                value = default!;
                return false;
            }
            catch (InvalidCastException)
            {
                value = default!;
                return false;
            }
            catch (OverflowException)
            {
                value = default!;
                return false;
            }
        }

        /// <summary>
        /// Gets the value of this property as an array of the specified type. Expected format: {value1, value2, ...}
        /// </summary>
        /// <typeparam name="T">The type of array elements.</typeparam>
        /// <returns>An array of values.</returns>
        /// <exception cref="FormatException">Thrown when the value is not in the correct array format.</exception>
        public T[] GetValueArray<T>()
        {
            ReadOnlySpan<char> span = _value.AsSpan().Trim();
            if (span.Length < 2 || span[0] != '{' || span[^1] != '}')
                throw new FormatException("Invalid array format");

            span = span.Slice(1, span.Length - 2);
            var values = new List<T>();

            int start = 0;
            bool inQuotes = false;

            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length)
                {
                    if (inQuotes)
                        throw new FormatException("Unterminated quote in array");

                    AddValueIfValid(span.Slice(start, i - start));
                    break;
                }

                if (span[i] == '"')
                {
                    if (i > 0 && span[i - 1] == '\\')
                        continue;
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && span[i] == ',')
                {
                    AddValueIfValid(span.Slice(start, i - start));
                    start = i + 1;
                }
            }

            return values.ToArray();

            void AddValueIfValid(ReadOnlySpan<char> item)
            {
                item = item.Trim();
                if (item.IsEmpty)
                    return;

                string valueStr = item.ToString();

                // Handle quoted strings
                if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
                {
                    valueStr = valueStr.Substring(1, valueStr.Length - 2)
                                      .Replace("\\\"", "\"");
                }

                values.Add((T)Convert.ChangeType(valueStr, typeof(T)));
            }
        }

        /// <summary>
        /// Sets the value of this property from a string.
        /// </summary>
        /// <param name="value">The string value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStringValue(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Sets the value of this property from a typed value.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(T value) => _value = Convert.ToString(value) ?? string.Empty;

        /// <summary>
        /// Sets the value of this property from an array. Format: {value1, value2, ...}
        /// </summary>
        /// <typeparam name="T">The type of array elements.</typeparam>
        /// <param name="values">The array of values to set.</param>
        public void SetValueArray<T>(T[] values)
        {
            if (values == null || values.Length == 0)
            {
                _value = "{}";
                return;
            }

            // Estimate capacity: {} + values + separators
            int estimatedCapacity = 2 + (values.Length * 10) + (values.Length - 1) * 2;
            var builder = new StringBuilder(estimatedCapacity);
            builder.Append('{');

            Span<char> specialChars = stackalloc char[] { ',', '{', '}', '"', ' ' };

            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) builder.Append(", ");

                string valueStr = Convert.ToString(values[i]) ?? string.Empty;

                // Check if the value needs to be quoted
                bool needsQuotes = valueStr.AsSpan().IndexOfAny(specialChars) >= 0;

                if (needsQuotes)
                {
                    // Escape any existing quotes and wrap in quotes
                    builder.Append('"');
                    foreach (char c in valueStr)
                    {
                        if (c == '"') builder.Append('\\');
                        builder.Append(c);
                    }
                    builder.Append('"');
                }
                else
                {
                    builder.Append(valueStr);
                }
            }

            builder.Append('}');
            _value = builder.ToString();
        }

        /// <summary>
        /// Creates a deep copy of this property.
        /// </summary>
        /// <returns>A new property with the same name, value, and comments.</returns>
        public Property Clone()
        {
            var clone = new Property(Name, _value);
            clone.IsQuoted = IsQuoted;

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
        /// Sets the value and returns this property for fluent chaining.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>This property instance.</returns>
        public Property WithValue(string value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Sets the typed value and returns this property for fluent chaining.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to set.</param>
        /// <returns>This property instance.</returns>
        public Property WithValue<T>(T value)
        {
            SetValue(value);
            return this;
        }

        /// <summary>
        /// Sets whether the value should be quoted and returns this property for fluent chaining.
        /// </summary>
        /// <param name="quoted">True to quote the value; otherwise, false.</param>
        /// <returns>This property instance.</returns>
        public Property WithQuoted(bool quoted = true)
        {
            IsQuoted = quoted;
            return this;
        }

        /// <summary>
        /// Adds an inline comment and returns this property for fluent chaining.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This property instance.</returns>
        public Property WithComment(string comment)
        {
            Comment = new Comment(comment);
            return this;
        }

        /// <summary>
        /// Adds a pre-comment and returns this property for fluent chaining.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This property instance.</returns>
        public Property WithPreComment(string comment)
        {
            PreComments.Add(new Comment(comment));
            return this;
        }
    }
}
