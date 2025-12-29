namespace IniEdit.Serialization
{
    /// <summary>
    /// Specifies the INI section name for a class or property.
    /// </summary>
    /// <remarks>
    /// When applied to a class, it maps the class to a specific INI section.
    /// When applied to a property of a complex type, it specifies the section name for that nested object.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IniSectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the section name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSectionAttribute"/> class.
        /// </summary>
        /// <param name="name">The INI section name.</param>
        public IniSectionAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    /// <summary>
    /// Specifies the INI property key name for a class property.
    /// </summary>
    /// <remarks>
    /// If not specified, the property name is used as the INI key.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IniPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets the property key name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets or sets the default value when the property is not found in the INI file.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniPropertyAttribute"/> class.
        /// </summary>
        public IniPropertyAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniPropertyAttribute"/> class with a custom key name.
        /// </summary>
        /// <param name="name">The INI property key name.</param>
        public IniPropertyAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Indicates that a property should be ignored during serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IniIgnoreAttribute : Attribute
    {
    }
}
