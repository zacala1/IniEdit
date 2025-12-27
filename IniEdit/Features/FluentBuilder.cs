namespace IniEdit
{
    /// <summary>
    /// Provides a fluent interface for building INI documents.
    /// </summary>
    public sealed class DocumentBuilder
    {
        private readonly Document _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentBuilder"/> class with default options.
        /// </summary>
        public DocumentBuilder()
        {
            _document = new Document();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentBuilder"/> class with specified options.
        /// </summary>
        /// <param name="option">The configuration options for the document.</param>
        public DocumentBuilder(IniConfigOption option)
        {
            _document = new Document(option);
        }

        /// <summary>
        /// Adds a section to the document with the specified name and configuration.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        /// <param name="configure">An action to configure the section using a <see cref="SectionBuilder"/>.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when configure is null.</exception>
        public DocumentBuilder WithSection(string name, Action<SectionBuilder> configure)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Section name cannot be null or empty", nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var section = new Section(name);
            var builder = new SectionBuilder(section);
            configure(builder);
            _document.AddSection(section);

            return this;
        }

        /// <summary>
        /// Adds a property to the default section.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public DocumentBuilder WithDefaultProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _document.DefaultSection.AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds a typed property to the default section.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public DocumentBuilder WithDefaultProperty<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key);
            property.SetValue(value);
            _document.DefaultSection.AddProperty(property);
            return this;
        }

        /// <summary>
        /// Adds a property (with full metadata including comments) to the default section.
        /// </summary>
        /// <param name="property">The property to add. A clone is created to avoid reference issues.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
        public DocumentBuilder WithDefaultProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _document.DefaultSection.AddProperty(property.Clone());
            return this;
        }

        /// <summary>
        /// Builds and returns the configured document.
        /// </summary>
        /// <returns>The built <see cref="Document"/>.</returns>
        public Document Build()
        {
            return _document;
        }

        /// <summary>
        /// Implicitly converts a <see cref="DocumentBuilder"/> to a <see cref="Document"/>.
        /// </summary>
        /// <param name="builder">The builder to convert.</param>
        public static implicit operator Document(DocumentBuilder builder)
        {
            return builder.Build();
        }
    }

    /// <summary>
    /// Provides a fluent interface for building INI sections.
    /// </summary>
    public sealed class SectionBuilder
    {
        private readonly Section _section;

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionBuilder"/> class.
        /// </summary>
        /// <param name="section">The section to build.</param>
        /// <exception cref="ArgumentNullException">Thrown when section is null.</exception>
        internal SectionBuilder(Section section)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));
        }

        /// <summary>
        /// Adds a property to the section.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public SectionBuilder WithProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _section.AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds a typed property to the section.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public SectionBuilder WithProperty<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key);
            property.SetValue(value);
            _section.AddProperty(property);
            return this;
        }

        /// <summary>
        /// Adds a property (with full metadata including comments) to the section.
        /// </summary>
        /// <param name="property">The property to add. A clone is created to avoid reference issues.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
        public SectionBuilder WithProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _section.AddProperty(property.Clone());
            return this;
        }

        /// <summary>
        /// Adds a quoted property to the section.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public SectionBuilder WithQuotedProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key, value);
            property.IsQuoted = true;
            _section.AddProperty(property);
            return this;
        }

        /// <summary>
        /// Sets the inline comment for the section.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comment is null.</exception>
        public SectionBuilder WithComment(string comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            _section.Comment = new Comment(comment);
            return this;
        }

        /// <summary>
        /// Adds a pre-comment to the section.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        /// <returns>This builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comment is null.</exception>
        public SectionBuilder WithPreComment(string comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            _section.PreComments.Add(new Comment(comment));
            return this;
        }
    }

    /// <summary>
    /// Provides extension methods for fluent document building.
    /// </summary>
    public static class FluentExtensions
    {
        /// <summary>
        /// Converts an existing document to a builder for further modification.
        /// </summary>
        /// <param name="document">The document to convert.</param>
        /// <returns>A new <see cref="DocumentBuilder"/> containing the document's content.</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
        public static DocumentBuilder ToBuilder(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var builder = new DocumentBuilder();

            // Add default section properties (with full metadata)
            foreach (var property in document.DefaultSection.GetProperties())
            {
                builder.WithDefaultProperty(property);
            }

            // Add sections
            foreach (var section in document)
            {
                builder.WithSection(section.Name, sectionBuilder =>
                {
                    // Add properties with full metadata (including comments)
                    foreach (var property in section.GetProperties())
                    {
                        sectionBuilder.WithProperty(property);
                    }

                    if (section.Comment != null)
                    {
                        sectionBuilder.WithComment(section.Comment.Value);
                    }

                    foreach (var preComment in section.PreComments)
                    {
                        sectionBuilder.WithPreComment(preComment.Value);
                    }
                });
            }

            return builder;
        }
    }
}
