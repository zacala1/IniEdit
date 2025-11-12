using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IniEdit.GUI
{
    /// <summary>
    /// Manages recent files list
    /// </summary>
    public class RecentFilesManager
    {
        private const int MaxRecentFiles = 10;
        private readonly List<string> recentFiles = new();
        private readonly string settingsFilePath;

        public event EventHandler? RecentFilesChanged;

        public IReadOnlyList<string> RecentFiles => recentFiles.AsReadOnly();

        public RecentFilesManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IniEditor"
            );
            Directory.CreateDirectory(appDataPath);
            settingsFilePath = Path.Combine(appDataPath, "recent_files.txt");
            LoadRecentFiles();
        }

        /// <summary>
        /// Add a file to recent files list
        /// </summary>
        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            // Normalize path
            filePath = Path.GetFullPath(filePath);

            // Remove if already exists
            recentFiles.Remove(filePath);

            // Add to top
            recentFiles.Insert(0, filePath);

            // Limit size
            if (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles.RemoveAt(recentFiles.Count - 1);
            }

            SaveRecentFiles();
            OnRecentFilesChanged();
        }

        /// <summary>
        /// Remove a file from recent files list
        /// </summary>
        public void RemoveRecentFile(string filePath)
        {
            if (recentFiles.Remove(filePath))
            {
                SaveRecentFiles();
                OnRecentFilesChanged();
            }
        }

        /// <summary>
        /// Clear all recent files
        /// </summary>
        public void ClearRecentFiles()
        {
            recentFiles.Clear();
            SaveRecentFiles();
            OnRecentFilesChanged();
        }

        private void LoadRecentFiles()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    var lines = File.ReadAllLines(settingsFilePath);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && File.Exists(line))
                        {
                            recentFiles.Add(line);
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors loading recent files
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                File.WriteAllLines(settingsFilePath, recentFiles);
            }
            catch
            {
                // Ignore errors saving recent files
            }
        }

        private void OnRecentFilesChanged()
        {
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
