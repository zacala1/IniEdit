using System.Text;

namespace IniEdit
{
    /// <summary>
    /// Specifies options for loading INI files.
    /// </summary>
    public sealed class LoadOptions
    {
        /// <summary>
        /// Gets or sets the configuration options for parsing the INI file.
        /// </summary>
        public IniConfigOption? ConfigOption { get; set; }

        /// <summary>
        /// Gets or sets the file sharing mode when opening the file.
        /// </summary>
        public FileShare FileShare { get; set; }

        /// <summary>
        /// Gets or sets a filter function to include or exclude sections by name.
        /// Return true to include the section, false to exclude it.
        /// </summary>
        public Func<string, bool>? SectionFilter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadOptions"/> class with default values.
        /// </summary>
        public LoadOptions()
        {
            FileShare = FileShare.Read;
        }
    }

    public static partial class IniConfigManager
    {
        /// <summary>
        /// Loads an INI document from a file with specified options.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="options">The load options.</param>
        /// <returns>The loaded document.</returns>
        /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public static Document LoadWithOptions(string filePath, LoadOptions options)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, options.FileShare);
            var document = Load(fileStream, Encoding.UTF8, options.ConfigOption);

            // Apply section filter if provided
            if (options.SectionFilter != null)
            {
                var sectionsToRemove = document.GetInternalSections()
                    .Where(s => !options.SectionFilter(s.Name))
                    .ToList();

                foreach (var section in sectionsToRemove)
                {
                    document.RemoveSection(section.Name);
                }
            }

            return document;
        }

        /// <summary>
        /// Asynchronously loads an INI document from a file with specified options.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="options">The load options.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous load operation.</returns>
        /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public static async Task<Document> LoadWithOptionsAsync(string filePath, LoadOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, options.FileShare, BufferSize, useAsync: true);
            var document = await LoadAsync(fileStream, Encoding.UTF8, options.ConfigOption, cancellationToken).ConfigureAwait(false);

            // Apply section filter if provided
            if (options.SectionFilter != null)
            {
                var sectionsToRemove = document.GetInternalSections()
                    .Where(s => !options.SectionFilter(s.Name))
                    .ToList();

                foreach (var section in sectionsToRemove)
                {
                    document.RemoveSection(section.Name);
                }
            }

            return document;
        }
    }
}
