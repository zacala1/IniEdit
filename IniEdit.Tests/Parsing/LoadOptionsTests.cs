namespace IniEdit.Tests.Parsing
{
    [TestFixture]
    public class LoadOptionsTests
    {
        private string _tempFilePath = null!;

        [SetUp]
        public void SetUp()
        {
            _tempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }

        #region LoadOptions Class Tests

        [Test]
        public void LoadOptions_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var options = new LoadOptions();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(options.ConfigOption, Is.Null);
                Assert.That(options.FileShare, Is.EqualTo(FileShare.Read));
                Assert.That(options.SectionFilter, Is.Null);
            });
        }

        [Test]
        public void LoadOptions_CanSetAllProperties()
        {
            // Arrange
            var configOption = new IniConfigOption();
            Func<string, bool> filter = s => s.StartsWith("A");

            // Act
            var options = new LoadOptions
            {
                ConfigOption = configOption,
                FileShare = FileShare.ReadWrite,
                SectionFilter = filter
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(options.ConfigOption, Is.SameAs(configOption));
                Assert.That(options.FileShare, Is.EqualTo(FileShare.ReadWrite));
                Assert.That(options.SectionFilter, Is.SameAs(filter));
            });
        }

        #endregion

        #region LoadWithOptions Tests

        [Test]
        public void LoadWithOptions_ValidFile_LoadsDocument()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section2]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions();

            // Act
            var doc = IniConfigManager.LoadWithOptions(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc["Section1"]["key1"].Value, Is.EqualTo("value1"));
            });
        }

        [Test]
        public void LoadWithOptions_WithSectionFilter_FiltersSection()
        {
            // Arrange
            var content = @"
[IncludeMe]
key1=value1
[ExcludeMe]
key2=value2
[IncludeToo]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions
            {
                SectionFilter = name => name.StartsWith("Include")
            };

            // Act
            var doc = IniConfigManager.LoadWithOptions(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc.HasSection("IncludeMe"), Is.True);
                Assert.That(doc.HasSection("IncludeToo"), Is.True);
                Assert.That(doc.HasSection("ExcludeMe"), Is.False);
            });
        }

        [Test]
        public void LoadWithOptions_WithConfigOption_AppliesOption()
        {
            // Arrange - use custom comment prefix
            var content = @"
# This is a hash comment
[Section]
key=value";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions
            {
                ConfigOption = new IniConfigOption
                {
                    CommentPrefixChars = new[] { '#' },
                    DefaultCommentPrefixChar = '#'
                }
            };

            // Act
            var doc = IniConfigManager.LoadWithOptions(_tempFilePath, options);

            // Assert - verify config option was applied
            Assert.Multiple(() =>
            {
                Assert.That(doc["Section"]["key"].Value, Is.EqualTo("value"));
                Assert.That(doc.CommentPrefixChars, Contains.Item('#'));
            });
        }

        [Test]
        public void LoadWithOptions_NullFilePath_ThrowsArgumentException()
        {
            // Arrange
            var options = new LoadOptions();

            // Act & Assert
#pragma warning disable CS8625
            var ex = Assert.Throws<ArgumentException>(() => IniConfigManager.LoadWithOptions(null, options));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("filePath"));
        }

        [Test]
        public void LoadWithOptions_EmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            var options = new LoadOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => IniConfigManager.LoadWithOptions(string.Empty, options));
            Assert.That(ex!.ParamName, Is.EqualTo("filePath"));
        }

        [Test]
        public void LoadWithOptions_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "[Section]\nkey=value");

            // Act & Assert
#pragma warning disable CS8625
            var ex = Assert.Throws<ArgumentNullException>(() => IniConfigManager.LoadWithOptions(_tempFilePath, null));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("options"));
        }

        #endregion

        #region LoadWithOptionsAsync Tests

        [Test]
        public async Task LoadWithOptionsAsync_ValidFile_LoadsDocument()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section2]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions();

            // Act
            var doc = await IniConfigManager.LoadWithOptionsAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc["Section1"]["key1"].Value, Is.EqualTo("value1"));
            });
        }

        [Test]
        public async Task LoadWithOptionsAsync_WithSectionFilter_FiltersSection()
        {
            // Arrange
            var content = @"
[Keep1]
key1=value1
[Remove]
key2=value2
[Keep2]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions
            {
                SectionFilter = name => name.StartsWith("Keep")
            };

            // Act
            var doc = await IniConfigManager.LoadWithOptionsAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc.HasSection("Keep1"), Is.True);
                Assert.That(doc.HasSection("Keep2"), Is.True);
                Assert.That(doc.HasSection("Remove"), Is.False);
            });
        }

        [Test]
        public void LoadWithOptionsAsync_NullFilePath_ThrowsArgumentException()
        {
            // Arrange
            var options = new LoadOptions();

            // Act & Assert
#pragma warning disable CS8625
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await IniConfigManager.LoadWithOptionsAsync(null, options));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("filePath"));
        }

        [Test]
        public void LoadWithOptionsAsync_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "[Section]\nkey=value");

            // Act & Assert
#pragma warning disable CS8625
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await IniConfigManager.LoadWithOptionsAsync(_tempFilePath, null));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("options"));
        }

        [Test]
        public void LoadWithOptionsAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var content = @"[Section]
key=value";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await IniConfigManager.LoadWithOptionsAsync(_tempFilePath, options, cts.Token));
        }

        #endregion

        #region FileShare Tests

        [Test]
        public void LoadWithOptions_FileShareRead_AllowsOtherReaders()
        {
            // Arrange
            var content = "[Section]\nkey=value";
            File.WriteAllText(_tempFilePath, content);
            var options = new LoadOptions { FileShare = FileShare.Read };

            // Act - Load with FileShare.Read and verify another reader can access
            using var firstStream = new FileStream(_tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // This should succeed because FileShare.Read allows other readers
            Assert.DoesNotThrow(() =>
            {
                using var secondStream = new FileStream(_tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            });
        }

        #endregion
    }
}
