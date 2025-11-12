using System.Text;

namespace IniEdit
{
    public class LoadOptions
    {
        public IniConfigOption? ConfigOption { get; set; }
        public FileShare FileShare { get; set; }
        public Func<string, bool>? SectionFilter { get; set; }

        public LoadOptions()
        {
            FileShare = FileShare.Read;
        }
    }

    public static partial class IniConfigManager
    {
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

        public static async Task<Document> LoadWithOptionsAsync(string filePath, LoadOptions options)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, options.FileShare, BufferSize, true);
            var document = await Task.Run(() => Load(fileStream, Encoding.UTF8, options.ConfigOption));

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
