using System;
using System.IO;
using System.Text;
using IniEdit;
using IniEdit.GUI.Services;

namespace IniEdit.GUI.Tests.Services
{
    [TestFixture]
    public class BackupManagerTests
    {
        private string _testDir = null!;
        private BackupManager _backupManager = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"BackupManagerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _backupManager = new BackupManager(maxBackupsToKeep: 3);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidMaxBackups_SetsProperty()
        {
            var manager = new BackupManager(maxBackupsToKeep: 5);
            Assert.That(manager.MaxBackupsToKeep, Is.EqualTo(5));
        }

        [Test]
        public void Constructor_WithDefaultMaxBackups_Uses10()
        {
            var manager = new BackupManager();
            Assert.That(manager.MaxBackupsToKeep, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_WithZeroMaxBackups_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackupManager(maxBackupsToKeep: 0));
        }

        [Test]
        public void Constructor_WithNegativeMaxBackups_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackupManager(maxBackupsToKeep: -1));
        }

        #endregion

        #region GetBackupDirectory Tests

        [Test]
        public void GetBackupDirectory_WithValidPath_ReturnsCorrectPath()
        {
            var sourceFile = Path.Combine(_testDir, "test.ini");
            var expected = Path.Combine(_testDir, ".ini_backup");

            var result = _backupManager.GetBackupDirectory(sourceFile);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetBackupDirectory_WithNullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _backupManager.GetBackupDirectory(null!));
        }

        [Test]
        public void GetBackupDirectory_WithEmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _backupManager.GetBackupDirectory(string.Empty));
        }

        #endregion

        #region GenerateBackupPath Tests

        [Test]
        public void GenerateBackupPath_WithValidInputs_ReturnsCorrectPath()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");
            var timestamp = new DateTime(2024, 3, 15, 10, 30, 45);

            var result = _backupManager.GenerateBackupPath(sourceFile, timestamp);

            var expected = Path.Combine(_testDir, ".ini_backup", "config_20240315_103045.ini.bak");
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateBackupPath_WithDifferentExtension_PreservesExtension()
        {
            var sourceFile = Path.Combine(_testDir, "settings.cfg");
            var timestamp = new DateTime(2024, 1, 1, 0, 0, 0);

            var result = _backupManager.GenerateBackupPath(sourceFile, timestamp);

            Assert.That(result, Does.Contain("settings_20240101_000000.cfg.bak"));
        }

        [Test]
        public void GenerateBackupPath_WithNullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _backupManager.GenerateBackupPath(null!, DateTime.Now));
        }

        #endregion

        #region GetBackupSearchPattern Tests

        [Test]
        public void GetBackupSearchPattern_WithValidPath_ReturnsCorrectPattern()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");

            var result = _backupManager.GetBackupSearchPattern(sourceFile);

            Assert.That(result, Is.EqualTo("config_*.ini.bak"));
        }

        [Test]
        public void GetBackupSearchPattern_WithDifferentExtension_ReturnsCorrectPattern()
        {
            var sourceFile = Path.Combine(_testDir, "app.cfg");

            var result = _backupManager.GetBackupSearchPattern(sourceFile);

            Assert.That(result, Is.EqualTo("app_*.cfg.bak"));
        }

        #endregion

        #region EnsureBackupDirectoryExists Tests

        [Test]
        public void EnsureBackupDirectoryExists_CreatesDirectory()
        {
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Assert.That(Directory.Exists(backupDir), Is.False);

            _backupManager.EnsureBackupDirectoryExists(backupDir);

            Assert.That(Directory.Exists(backupDir), Is.True);
        }

        [Test]
        public void EnsureBackupDirectoryExists_ExistingDirectory_DoesNotThrow()
        {
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Directory.CreateDirectory(backupDir);

            Assert.DoesNotThrow(() => _backupManager.EnsureBackupDirectoryExists(backupDir));
        }

        #endregion

        #region CleanupOldBackupsInDirectory Tests

        [Test]
        public void CleanupOldBackupsInDirectory_WithMoreThanMaxBackups_DeletesOldest()
        {
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Directory.CreateDirectory(backupDir);

            // Create 5 backup files (max is 3)
            var files = new[]
            {
                "config_20240101_000000.ini.bak",
                "config_20240102_000000.ini.bak",
                "config_20240103_000000.ini.bak",
                "config_20240104_000000.ini.bak",
                "config_20240105_000000.ini.bak"
            };

            foreach (var file in files)
            {
                File.WriteAllText(Path.Combine(backupDir, file), "test");
            }

            var deletedCount = _backupManager.CleanupOldBackupsInDirectory(backupDir, "config_*.ini.bak");

            Assert.That(deletedCount, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(backupDir).Length, Is.EqualTo(3));
        }

        [Test]
        public void CleanupOldBackupsInDirectory_WithFewerThanMaxBackups_DeletesNone()
        {
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Directory.CreateDirectory(backupDir);

            // Create 2 backup files (max is 3)
            File.WriteAllText(Path.Combine(backupDir, "config_20240101_000000.ini.bak"), "test");
            File.WriteAllText(Path.Combine(backupDir, "config_20240102_000000.ini.bak"), "test");

            var deletedCount = _backupManager.CleanupOldBackupsInDirectory(backupDir, "config_*.ini.bak");

            Assert.That(deletedCount, Is.EqualTo(0));
            Assert.That(Directory.GetFiles(backupDir).Length, Is.EqualTo(2));
        }

        [Test]
        public void CleanupOldBackupsInDirectory_NonExistentDirectory_ReturnsZero()
        {
            var deletedCount = _backupManager.CleanupOldBackupsInDirectory(
                Path.Combine(_testDir, "nonexistent"), "*.bak");

            Assert.That(deletedCount, Is.EqualTo(0));
        }

        #endregion

        #region GetBackupFiles Tests

        [Test]
        public void GetBackupFiles_WithExistingBackups_ReturnsOrderedList()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Directory.CreateDirectory(backupDir);

            // Create backup files
            File.WriteAllText(Path.Combine(backupDir, "config_20240101_000000.ini.bak"), "1");
            File.WriteAllText(Path.Combine(backupDir, "config_20240103_000000.ini.bak"), "3");
            File.WriteAllText(Path.Combine(backupDir, "config_20240102_000000.ini.bak"), "2");

            var result = _backupManager.GetBackupFiles(sourceFile);

            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(Path.GetFileName(result[0]), Does.Contain("20240103")); // Newest first
            Assert.That(Path.GetFileName(result[2]), Does.Contain("20240101")); // Oldest last
        }

        [Test]
        public void GetBackupFiles_NoBackupDirectory_ReturnsEmptyArray()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");

            var result = _backupManager.GetBackupFiles(sourceFile);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetBackupFiles_EmptyBackupDirectory_ReturnsEmptyArray()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");
            var backupDir = Path.Combine(_testDir, ".ini_backup");
            Directory.CreateDirectory(backupDir);

            var result = _backupManager.GetBackupFiles(sourceFile);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetBackupFiles_NullPath_ReturnsEmptyArray()
        {
            var result = _backupManager.GetBackupFiles(null!);
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region CreateBackup Tests

        [Test]
        public void CreateBackup_WithValidDocument_CreatesBackupFile()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");
            var doc = new Document();
            doc.AddSection(new Section("Test"));

            var backupPath = _backupManager.CreateBackup(sourceFile, doc, Encoding.UTF8);

            Assert.That(backupPath, Is.Not.Null);
            Assert.That(File.Exists(backupPath), Is.True);
        }

        [Test]
        public void CreateBackup_NullDocument_ReturnsNull()
        {
            var sourceFile = Path.Combine(_testDir, "config.ini");

            var result = _backupManager.CreateBackup(sourceFile, null!, Encoding.UTF8);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void CreateBackup_NullPath_ReturnsNull()
        {
            var doc = new Document();

            var result = _backupManager.CreateBackup(null!, doc, Encoding.UTF8);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void CreateBackup_NullEncoding_ReturnsNull()
        {
            var sourceFile = Path.Combine(_testDir, "test.ini");
            var doc = new Document();

            var result = _backupManager.CreateBackup(sourceFile, doc, null!);

            Assert.That(result, Is.Null);
        }

        #endregion
    }
}
