using System.Collections.Concurrent;
using System.Reflection;

namespace IniEdit.Serialization
{
    /// <summary>
    /// Provides serialization and deserialization between C# objects and INI documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Design Constraints:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>Maximum nesting depth is 1 level. Root class properties can be complex types (mapped to sections),
    /// but those complex types can only contain primitive properties.</description>
    /// </item>
    /// <item>
    /// <description>Supported property types: primitives (int, bool, etc.), string, enum, DateTime, Guid,
    /// and arrays of these types.</description>
    /// </item>
    /// <item>
    /// <description>Properties without setters are ignored during deserialization.</description>
    /// </item>
    /// <item>
    /// <description>For complex nested structures, consider using JSON or TOML formats instead.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AppConfig
    /// {
    ///     public string AppName { get; set; }  // Goes to DefaultSection
    ///
    ///     [IniSection("Database")]
    ///     public DatabaseConfig Database { get; set; }  // Goes to [Database] section
    /// }
    ///
    /// public class DatabaseConfig
    /// {
    ///     public string Host { get; set; }
    ///     public int Port { get; set; }
    /// }
    ///
    /// // Deserialize
    /// var config = IniSerializer.Deserialize&lt;AppConfig&gt;(document);
    ///
    /// // Serialize
    /// var document = IniSerializer.Serialize(config);
    /// </code>
    /// </example>
    public static class IniSerializer
    {
        private const int MaxNestingDepth = 1;

        /// <summary>
        /// Cache for generic MethodInfo to avoid repeated reflection overhead on hot deserialization paths.
        /// Key: (MethodName, TypeArgument) → MethodInfo
        /// </summary>
        private static readonly ConcurrentDictionary<(string, Type), MethodInfo> MethodCache = new();

        /// <summary>
        /// Deserializes an INI document to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to. Must have a parameterless constructor.</typeparam>
        /// <param name="document">The INI document to deserialize.</param>
        /// <returns>An instance of T populated with values from the document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
        /// <exception cref="IniSerializationException">Thrown when the type structure is not supported or deserialization fails.</exception>
        public static T Deserialize<T>(Document document) where T : new()
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            ValidateTypeStructure(typeof(T));

            var result = new T();
            PopulateObject(result, typeof(T), document, depth: 0);
            return result;
        }

        /// <summary>
        /// Deserializes an INI document to an object of the specified type.
        /// </summary>
        /// <param name="document">The INI document to deserialize.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>An instance of the type populated with values from the document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when document or type is null.</exception>
        /// <exception cref="IniSerializationException">Thrown when the type structure is not supported or deserialization fails.</exception>
        public static object Deserialize(Document document, Type type)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ValidateTypeStructure(type);

            var result = Activator.CreateInstance(type)
                ?? throw new IniSerializationException($"Failed to create instance of type '{type.Name}'");
            PopulateObject(result, type, document, depth: 0);
            return result;
        }

        /// <summary>
        /// Serializes an object to an INI document.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="option">Optional INI configuration options.</param>
        /// <returns>An INI document representing the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="IniSerializationException">Thrown when the type structure is not supported or serialization fails.</exception>
        public static Document Serialize<T>(T obj, IniConfigOption? option = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ValidateTypeStructure(typeof(T));

            var document = new Document(option);
            WriteObject(obj, typeof(T), document, depth: 0);
            return document;
        }

        /// <summary>
        /// Validates that the type structure is supported for INI serialization.
        /// </summary>
        /// <param name="type">The type to validate.</param>
        /// <exception cref="IniSerializationException">Thrown when the type structure exceeds the maximum nesting depth.</exception>
        public static void ValidateTypeStructure(Type type)
        {
            ValidateTypeRecursive(type, depth: 0, new HashSet<Type>());
        }

        private static void ValidateTypeRecursive(Type type, int depth, HashSet<Type> visitedTypes)
        {
            if (!visitedTypes.Add(type))
            {
                throw new IniSerializationException(
                    $"Circular reference detected for type '{type.Name}'. INI serialization does not support circular references.");
            }

            foreach (var prop in GetSerializableProperties(type))
            {
                var propType = GetUnderlyingType(prop.PropertyType);

                if (IsComplexType(propType))
                {
                    int newDepth = depth + 1;
                    if (newDepth > MaxNestingDepth)
                    {
                        throw new IniSerializationException(
                            $"Nesting depth exceeded. Property '{prop.Name}' of type '{propType.Name}' is at depth {newDepth}, " +
                            $"but maximum allowed depth is {MaxNestingDepth}.\n\n" +
                            $"INI format only supports flat key-value structures. For complex nested objects, " +
                            $"consider using:\n" +
                            $"  - IniEdit.DocumentExporter.ToJson() for JSON export\n" +
                            $"  - A dedicated TOML or YAML library for complex configurations");
                    }

                    ValidateTypeRecursive(propType, newDepth, new HashSet<Type>(visitedTypes));
                }
            }
        }

        private static void PopulateObject(object obj, Type type, Document document, int depth)
        {
            // Get the section for this object (DefaultSection for root, named section for nested)
            var sectionAttr = type.GetCustomAttribute<IniSectionAttribute>();

            foreach (var prop in GetSerializableProperties(type))
            {
                if (!prop.CanWrite)
                    continue;

                var propType = prop.PropertyType;
                var underlyingType = GetUnderlyingType(propType);

                if (IsComplexType(underlyingType))
                {
                    // Nested object - get from a named section
                    var nestedSectionAttr = prop.GetCustomAttribute<IniSectionAttribute>();
                    string sectionName = nestedSectionAttr?.Name ?? prop.Name;

                    if (document.TryGetSection(sectionName, out var section) && section != null)
                    {
                        var nestedObj = Activator.CreateInstance(underlyingType)
                            ?? throw new IniSerializationException($"Failed to create instance of type '{underlyingType.Name}'");
                        PopulateFromSection(nestedObj, underlyingType, section);
                        prop.SetValue(obj, nestedObj);
                    }
                }
                else
                {
                    // Simple property - get from DefaultSection (for root) or current section context
                    Section targetSection;
                    if (depth == 0)
                    {
                        if (sectionAttr != null && document.TryGetSection(sectionAttr.Name, out var s) && s != null)
                            targetSection = s;
                        else
                            targetSection = document.DefaultSection;
                    }
                    else
                    {
                        targetSection = document.DefaultSection;
                    }

                    SetPropertyFromSection(obj, prop, targetSection);
                }
            }
        }

        private static void PopulateFromSection(object obj, Type type, Section section)
        {
            foreach (var prop in GetSerializableProperties(type))
            {
                if (!prop.CanWrite)
                    continue;

                if (IsComplexType(GetUnderlyingType(prop.PropertyType)))
                {
                    // This shouldn't happen if validation passed, but skip just in case
                    continue;
                }

                SetPropertyFromSection(obj, prop, section);
            }
        }

        private static void SetPropertyFromSection(object obj, PropertyInfo prop, Section section)
        {
            var propAttr = prop.GetCustomAttribute<IniPropertyAttribute>();
            string keyName = propAttr?.Name ?? prop.Name;

            if (!section.TryGetProperty(keyName, out var iniProperty) || iniProperty == null)
            {
                // Try to use default value from attribute
                if (propAttr?.DefaultValue != null)
                {
                    prop.SetValue(obj, Convert.ChangeType(propAttr.DefaultValue, prop.PropertyType));
                }
                return;
            }

            try
            {
                var propType = prop.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(propType);
                var targetType = underlyingType ?? propType;

                if (propType.IsArray)
                {
                    var elementType = propType.GetElementType()!;
                    var method = MethodCache.GetOrAdd((nameof(Property.GetValueArray), elementType),
                        key => typeof(Property).GetMethod(key.Item1)!.MakeGenericMethod(key.Item2));
                    var array = method.Invoke(iniProperty, new object[] { Property.DefaultMaxArrayElements });
                    prop.SetValue(obj, array);
                }
                else if (targetType.IsEnum)
                {
                    // Handle enum types
                    if (string.IsNullOrEmpty(iniProperty.Value))
                    {
                        if (underlyingType != null)
                            prop.SetValue(obj, null);
                        // else leave default
                    }
                    else
                    {
                        var enumValue = Enum.Parse(targetType, iniProperty.Value, ignoreCase: true);
                        prop.SetValue(obj, enumValue);
                    }
                }
                else if (targetType == typeof(TimeSpan))
                {
                    // Handle TimeSpan (not supported by Convert.ChangeType)
                    if (string.IsNullOrEmpty(iniProperty.Value))
                    {
                        if (underlyingType != null)
                            prop.SetValue(obj, null);
                    }
                    else
                    {
                        var timeSpanValue = TimeSpan.Parse(iniProperty.Value);
                        prop.SetValue(obj, timeSpanValue);
                    }
                }
                else if (targetType == typeof(Guid))
                {
                    // Handle Guid (not supported by Convert.ChangeType)
                    if (string.IsNullOrEmpty(iniProperty.Value))
                    {
                        if (underlyingType != null)
                            prop.SetValue(obj, null);
                    }
                    else
                    {
                        var guidValue = Guid.Parse(iniProperty.Value);
                        prop.SetValue(obj, guidValue);
                    }
                }
                else if (underlyingType != null)
                {
                    // Nullable type (non-enum)
                    if (string.IsNullOrEmpty(iniProperty.Value))
                    {
                        prop.SetValue(obj, null);
                    }
                    else
                    {
                        var method = MethodCache.GetOrAdd((nameof(Property.GetValue), underlyingType),
                            key => typeof(Property).GetMethod(key.Item1)!.MakeGenericMethod(key.Item2));
                        var value = method.Invoke(iniProperty, null);
                        prop.SetValue(obj, value);
                    }
                }
                else
                {
                    var method = MethodCache.GetOrAdd((nameof(Property.GetValue), propType),
                        key => typeof(Property).GetMethod(key.Item1)!.MakeGenericMethod(key.Item2));
                    var value = method.Invoke(iniProperty, null);
                    prop.SetValue(obj, value);
                }
            }
            catch (Exception ex) when (ex is not IniSerializationException)
            {
                throw new IniSerializationException(
                    $"Failed to deserialize property '{prop.Name}' from key '{keyName}': {ex.Message}", ex);
            }
        }

        private static void WriteObject(object obj, Type type, Document document, int depth)
        {
            var sectionAttr = type.GetCustomAttribute<IniSectionAttribute>();

            foreach (var prop in GetSerializableProperties(type))
            {
                if (!prop.CanRead)
                    continue;

                var value = prop.GetValue(obj);
                if (value == null)
                    continue;

                var propType = prop.PropertyType;
                var underlyingType = GetUnderlyingType(propType);

                if (IsComplexType(underlyingType))
                {
                    // Nested object - write to a named section
                    var nestedSectionAttr = prop.GetCustomAttribute<IniSectionAttribute>();
                    string sectionName = nestedSectionAttr?.Name ?? prop.Name;

                    var section = new Section(sectionName);
                    WritePropertiesToSection(value, underlyingType, section);
                    document.AddSection(section);
                }
                else
                {
                    // Simple property - write to DefaultSection or class-level section
                    Section targetSection;
                    if (sectionAttr != null)
                    {
                        if (!document.TryGetSection(sectionAttr.Name, out targetSection!) || targetSection == null)
                        {
                            targetSection = new Section(sectionAttr.Name);
                            document.AddSection(targetSection);
                        }
                    }
                    else
                    {
                        targetSection = document.DefaultSection;
                    }

                    WritePropertyToSection(prop, value, targetSection);
                }
            }
        }

        private static void WritePropertiesToSection(object obj, Type type, Section section)
        {
            foreach (var prop in GetSerializableProperties(type))
            {
                if (!prop.CanRead)
                    continue;

                var value = prop.GetValue(obj);
                if (value == null)
                    continue;

                WritePropertyToSection(prop, value, section);
            }
        }

        private static void WritePropertyToSection(PropertyInfo prop, object value, Section section)
        {
            var propAttr = prop.GetCustomAttribute<IniPropertyAttribute>();
            string keyName = propAttr?.Name ?? prop.Name;

            var propType = prop.PropertyType;

            if (propType.IsArray)
            {
                var elementType = propType.GetElementType()!;
                var iniProp = new Property(keyName);
                var method = MethodCache.GetOrAdd((nameof(Property.SetValueArray), elementType),
                    key => typeof(Property).GetMethod(key.Item1)!.MakeGenericMethod(key.Item2));
                method.Invoke(iniProp, new[] { value });
                section.AddProperty(iniProp);
            }
            else
            {
                section.AddProperty(keyName, value?.ToString() ?? string.Empty);
            }
        }

        private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<IniIgnoreAttribute>() == null);
        }

        private static Type GetUnderlyingType(Type type)
        {
            // Handle nullable types
            var nullableUnderlying = Nullable.GetUnderlyingType(type);
            if (nullableUnderlying != null)
                return nullableUnderlying;

            // Handle arrays
            if (type.IsArray)
                return type.GetElementType()!;

            return type;
        }

        private static bool IsComplexType(Type type)
        {
            if (type.IsPrimitive)
                return false;
            if (type == typeof(string))
                return false;
            if (type == typeof(decimal))
                return false;
            if (type == typeof(DateTime))
                return false;
            if (type == typeof(DateTimeOffset))
                return false;
            if (type == typeof(TimeSpan))
                return false;
            if (type == typeof(Guid))
                return false;
            if (type.IsEnum)
                return false;

            return true;
        }
    }
}
