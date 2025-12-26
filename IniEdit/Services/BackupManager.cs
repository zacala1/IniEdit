using System;
using System.IO;
using System.Linq;
using System.Text;

namespace IniEdit.Services
{
    /// <summary>
    /// Manages automatic backup of INI files.
    /// </summary>
    public sealed class BackupManager
    {
        private const string BackupDirectoryName = ".ini_backup";
        private const string BackupExtension = ".bak";

        private readonly int _maxBackupsToKeep;

        /// <summary>
        /// Gets the maximum number of backups to keep.
        /// </summary>
        public int MaxBackupsToKeep => _maxBackupsToKeep;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupManager"/> class.
        /// </summary>
        /// <param name="maxBackupsToKeep">Maximum number of backup files to keep per source file.</param>
        public BackupManager(int maxBackupsToKeep = 10)
        {
            if (maxBackupsToKeep < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBackupsToKeep), "Must keep at least 1 backup.");

            _maxBackupsToKeep = maxBackupsToKeep;
        }

        /// <summary>
        /// Gets the backup directory path for a given source file.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <returns>The backup directory path.</returns>
        public string GetBackupDirectory(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

            var directory = Path.GetDirectoryName(sourceFilePath) ?? ".";
            return Path.Combine(directory, BackupDirectoryName);
        }

        /// <summary>
        /// Generates a backup file path with timestamp.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="timestamp">The timestamp for the backup.</param>
        /// <returns>The backup file path.</returns>
        public string GenerateBackupPath(string sourceFilePath, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

            var backupDir = GetBackupDirectory(sourceFilePath);
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var ext = Path.GetExtension(sourceFilePath);
            var timestampStr = timestamp.ToString("yyyyMMdd_HHmmss");

            return Path.Combine(backupDir, $"{fileName}_{timestampStr}{ext}{BackupExtension}");
        }

        /// <summary>
        /// Gets the search pattern for backup files of a source file.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <returns>The search pattern.</returns>
        public string GetBackupSearchPattern(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var ext = Path.GetExtension(sourceFilePath);
            return $"{fileName}_*{ext}{BackupExtension}";
        }

        /// <summary>
        /// Creates a backup of the document.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="document">The document to backup.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The backup file path, or null if backup failed.</returns>
        public string? CreateBackup(string sourceFilePath, Document document, Encoding encoding)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                return null;
            if (document == null)
                return null;
            if (encoding == null)
                return null;

            try
            {
                var backupDir = GetBackupDirectory(sourceFilePath);
                EnsureBackupDirectoryExists(backupDir);

                var backupPath = GenerateBackupPath(sourceFilePath, DateTime.Now);
                IniConfigManager.Save(backupPath, encoding, document);

                CleanupOldBackups(sourceFilePath);

                return backupPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Ensures the backup directory exists and is hidden.
        /// </summary>
        /// <param name="backupDir">The backup directory path.</param>
        public void EnsureBackupDirectoryExists(string backupDir)
        {
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
                try
                {
                    File.SetAttributes(backupDir, File.GetAttributes(backupDir) | FileAttributes.Hidden);
                }
                catch
                {
                    // Ignore attribute setting errors (e.g., on non-Windows systems)
                }
            }
        }

        /// <summary>
        /// Cleans up old backup files, keeping only the most recent ones.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <returns>The number of files deleted.</returns>
        public int CleanupOldBackups(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                return 0;

            var backupDir = GetBackupDirectory(sourceFilePath);
            if (!Directory.Exists(backupDir))
                return 0;

            return CleanupOldBackupsInDirectory(backupDir, GetBackupSearchPattern(sourceFilePath));
        }

        /// <summary>
        /// Cleans up old backup files in a directory.
        /// </summary>
        /// <param name="backupDir">The backup directory.</param>
        /// <param name="searchPattern">The search pattern for backup files.</param>
        /// <returns>The number of files deleted.</returns>
        public int CleanupOldBackupsInDirectory(string backupDir, string searchPattern)
        {
            try
            {
                var filesToDelete = Directory.GetFiles(backupDir, searchPattern)
                    .OrderByDescending(f => f)
                    .Skip(_maxBackupsToKeep);

                int deletedCount = 0;
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }

                return deletedCount;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets all backup files for a source file, ordered by newest first.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <returns>Array of backup file paths.</returns>
        public string[] GetBackupFiles(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
                return Array.Empty<string>();

            var backupDir = GetBackupDirectory(sourceFilePath);
            if (!Directory.Exists(backupDir))
                return Array.Empty<string>();

            try
            {
                return Directory.GetFiles(backupDir, GetBackupSearchPattern(sourceFilePath))
                    .OrderByDescending(f => f)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
