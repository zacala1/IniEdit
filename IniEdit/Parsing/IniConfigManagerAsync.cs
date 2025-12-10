using System.Buffers;
using System.Text;
using static IniEdit.IniConfigOption;

namespace IniEdit
{
    public static partial class IniConfigManager
    {
        /// <summary>
        /// Asynchronously loads an INI configuration file from the specified path using UTF-8 encoding.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the loaded document.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with ReadLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task<Document> LoadAsync(string filePath, IniConfigOption? option = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
            return await LoadAsync(fileStream, Encoding.UTF8, option, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously loads an INI configuration file from the specified path using the specified encoding.
        /// </summary>
        /// <param name="filePath">The path to the INI file.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="option">Optional configuration options.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the loaded document.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with ReadLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task<Document> LoadAsync(string filePath, Encoding encoding, IniConfigOption? option = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
            return await LoadAsync(fileStream, encoding, option, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to the specified path using UTF-8 encoding.
        /// </summary>
        /// <param name="filePath">The path where the file will be saved.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with WriteAsync/WriteLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task SaveAsync(string filePath, Document document, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
            await SaveAsync(fileStream, Encoding.UTF8, document, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to the specified path using the specified encoding.
        /// </summary>
        /// <param name="filePath">The path where the file will be saved.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method uses true asynchronous I/O with WriteAsync/WriteLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task SaveAsync(string filePath, Encoding encoding, Document document, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
            await SaveAsync(fileStream, encoding, document, cancellationToken).ConfigureAwait(false);
        }
    }
}
