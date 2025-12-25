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
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while opening the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
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
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while opening the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
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
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the file or directory is denied.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while writing the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
        /// <remarks>
        /// This method uses atomic write (write to temp file, then replace) to prevent data loss.
        /// Uses true asynchronous I/O with WriteAsync/WriteLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static Task SaveAsync(string filePath, Document document, CancellationToken cancellationToken = default)
        {
            return SaveAsync(filePath, Encoding.UTF8, document, cancellationToken);
        }

        /// <summary>
        /// Asynchronously saves an INI configuration document to the specified path using the specified encoding.
        /// </summary>
        /// <param name="filePath">The path where the file will be saved.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> or <paramref name="document"/> is null.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the file or directory is denied.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while writing the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
        /// <remarks>
        /// This method uses atomic write (write to temp file, then replace) to prevent data loss.
        /// Uses true asynchronous I/O with WriteAsync/WriteLineAsync, avoiding thread pool exhaustion.
        /// Suitable for high-concurrency scenarios.
        /// </remarks>
        public static async Task SaveAsync(string filePath, Encoding encoding, Document document, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            // Use atomic write: write to temp file first, then replace original
            var directory = Path.GetDirectoryName(filePath) ?? ".";
            var tempFilePath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                // Write to temporary file
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    await SaveAsync(fileStream, encoding, document, cancellationToken).ConfigureAwait(false);
                }

                // Replace original file with temp file (atomic on most file systems)
                File.Move(tempFilePath, filePath, overwrite: true);
            }
            catch
            {
                // Clean up temp file if something went wrong
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                throw;
            }
        }
    }
}
