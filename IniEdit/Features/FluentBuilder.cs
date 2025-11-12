namespace IniEdit
{
    public class DocumentBuilder
    {
        private readonly Document _document;

        public DocumentBuilder()
        {
            _document = new Document();
        }

        public DocumentBuilder(IniConfigOption option)
        {
            _document = new Document(option);
        }

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

        public DocumentBuilder WithDefaultProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _document.DefaultSection.AddProperty(key, value);
            return this;
        }

        public DocumentBuilder WithDefaultProperty<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key);
            property.SetValue(value);
            _document.DefaultSection.AddProperty(property);
            return this;
        }

        public Document Build()
        {
            return _document;
        }

        public static implicit operator Document(DocumentBuilder builder)
        {
            return builder.Build();
        }
    }

    public class SectionBuilder
    {
        private readonly Section _section;

        internal SectionBuilder(Section section)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));
        }

        public SectionBuilder WithProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _section.AddProperty(key, value);
            return this;
        }

        public SectionBuilder WithProperty<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key);
            property.SetValue(value);
            _section.AddProperty(property);
            return this;
        }

        public SectionBuilder WithQuotedProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var property = new Property(key, value);
            property.IsQuoted = true;
            _section.AddProperty(property);
            return this;
        }

        public SectionBuilder WithComment(string comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            _section.Comment = new Comment(comment);
            return this;
        }

        public SectionBuilder WithPreComment(string comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            _section.PreComments.Add(new Comment(comment));
            return this;
        }
    }

    public static class FluentExtensions
    {
        public static DocumentBuilder ToBuilder(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var builder = new DocumentBuilder();

            // Add default section properties
            foreach (var property in document.DefaultSection.GetProperties())
            {
                builder.WithDefaultProperty(property.Name, property.Value);
            }

            // Add sections
            foreach (var section in document)
            {
                builder.WithSection(section.Name, sectionBuilder =>
                {
                    foreach (var property in section.GetProperties())
                    {
                        sectionBuilder.WithProperty(property.Name, property.Value);
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
