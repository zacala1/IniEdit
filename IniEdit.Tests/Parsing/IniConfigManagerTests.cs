using System.Text;

namespace IniEdit.Tests.Parsing
{
    [TestFixture]
    public class IniConfigManagerTests
    {
        private const string ValidIniContent = @"
; Default section comment
key1=value1
key2=value2 ; inline comment

; Section comment
[Section1]
key3=value3
key4=""quoted value"" ; inline comment

[Section2] ; section inline comment
key5=value5";
#pragma warning disable CS8618
        private string _tempFilePath;
#pragma warning restore CS8618

        [SetUp]
        public void Setup()
        {
            _tempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }

        #region Load Tests - File Path Overloads

        [Test]
        public void Load_NullFilePath_ThrowsArgumentException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentException>(() => IniConfigManager.Load((string)null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void Load_EmptyFilePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => IniConfigManager.Load(string.Empty));
        }

        [Test]
        public void Load_NonExistentFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() =>
                IniConfigManager.Load("nonexistent.ini"));
        }

        [Test]
        public void Load_WithEncoding_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, ValidIniContent, Encoding.UTF8);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, Encoding.UTF8);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                Assert.That(doc.SectionCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void Load_WithEncoding_NullEncoding_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() =>
                IniConfigManager.Load(_tempFilePath, (Encoding)null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void Load_WithQuotedValues_ParsesCorrectly()
        {
            // Arrange
            var ini = @"
[Section1]
key1=""simple value""
key2=""value with spaces""
key3=""value with \"" quote""
key4=""value with \\ backslash""
key5=""value with \t tab""
key6=""value with \n newline""
";
            File.WriteAllText(_tempFilePath, ini);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var section = doc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                Assert.That(section.GetProperty("key1")?.Value, Is.EqualTo("simple value"));
                Assert.That(section.GetProperty("key1")?.IsQuoted, Is.True);
                Assert.That(section.GetProperty("key2")?.Value, Is.EqualTo("value with spaces"));
                Assert.That(section.GetProperty("key3")?.Value, Is.EqualTo("value with \" quote"));
                Assert.That(section.GetProperty("key4")?.Value, Is.EqualTo("value with \\ backslash"));
                Assert.That(section.GetProperty("key5")?.Value, Is.EqualTo("value with \t tab"));
                Assert.That(section.GetProperty("key6")?.Value, Is.EqualTo("value with \n newline"));
            });
        }

        [Test]
        public void Load_WithInvalidQuotes_IgnoresInvalidLines()
        {
            // Arrange
            var ini = @"
[Section1]
key1=""unterminated quote
key2=""valid quote"" extra content
key3=""valid quote"" # with comment
key4=unquoted value
";
            File.WriteAllText(_tempFilePath, ini);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var section = doc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                Assert.That(section.GetProperty("key1"), Is.Null);
                Assert.That(section.GetProperty("key2"), Is.Null);
                Assert.That(section.GetProperty("key3")?.Value, Is.EqualTo("valid quote"));
                Assert.That(section.GetProperty("key4")?.Value, Is.EqualTo("unquoted value"));
                Assert.That(section.GetProperty("key4")?.IsQuoted, Is.False);
            });
        }

        [Test]
        public void Load_WithEscapeSequences_ParsesCorrectly()
        {
            // Arrange
            var ini = @"
[Section1]
key1=""value with \0 null""
key2=""value with \a bell""
key3=""value with \b backspace""
key4=""value with \t tab""
key5=""value with \r\n newline""
key6=""value with \; semicolon""
key7=""value with \# hash""
";
            File.WriteAllText(_tempFilePath, ini);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var section = doc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                Assert.That(section.GetProperty("key1")?.Value, Is.EqualTo("value with \0 null"));
                Assert.That(section.GetProperty("key2")?.Value, Is.EqualTo("value with \a bell"));
                Assert.That(section.GetProperty("key3")?.Value, Is.EqualTo("value with \b backspace"));
                Assert.That(section.GetProperty("key4")?.Value, Is.EqualTo("value with \t tab"));
                Assert.That(section.GetProperty("key5")?.Value, Is.EqualTo("value with \r\n newline"));
                Assert.That(section.GetProperty("key6")?.Value, Is.EqualTo("value with ; semicolon"));
                Assert.That(section.GetProperty("key7")?.Value, Is.EqualTo("value with # hash"));
            });
        }

        [Test]
        public void Load_WithMixedQuotedAndUnquotedValues_ParsesCorrectly()
        {
            // Arrange
            var ini = @"
[Section1]
unquoted1=simple value
unquoted2=value with spaces
quoted1=""quoted value""
unquoted3=123
unquoted4=true
quoted2=""value with spaces""
unquoted5=value#not a comment
unquoted6=value ;not a comment
quoted3=""value # in quotes""
";
            File.WriteAllText(_tempFilePath, ini);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var section = doc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                // Basic unquoted value
                Assert.That(section.GetProperty("unquoted1")?.Value, Is.EqualTo("simple value"));
                Assert.That(section.GetProperty("unquoted1")?.IsQuoted, Is.False);

                // Unquoted value with spaces
                Assert.That(section.GetProperty("unquoted2")?.Value, Is.EqualTo("value with spaces"));
                Assert.That(section.GetProperty("unquoted2")?.IsQuoted, Is.False);

                // Compare with quoted value
                Assert.That(section.GetProperty("quoted1")?.Value, Is.EqualTo("quoted value"));
                Assert.That(section.GetProperty("quoted1")?.IsQuoted, Is.True);

                // Numeric and boolean values
                Assert.That(section.GetProperty("unquoted3")?.Value, Is.EqualTo("123"));
                Assert.That(section.GetProperty("unquoted4")?.Value, Is.EqualTo("true"));

                // Unquoted value containing comment characters
                Assert.That(section.GetProperty("unquoted5")?.Value, Is.EqualTo("value"));
                Assert.That(section.GetProperty("unquoted6")?.Value, Is.EqualTo("value"));
            });
        }

        #endregion

        #region Load Tests - Stream Overload

        [Test]
        public void Load_NullStream_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() =>
                IniConfigManager.Load((Stream)null, Encoding.UTF8));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void Load_NonReadableStream_ThrowsArgumentException()
        {
            using var stream = new MemoryStream();
            stream.Close(); // Make stream unreadable

            Assert.Throws<ArgumentException>(() =>
                IniConfigManager.Load(stream, Encoding.UTF8));
        }

        [Test]
        public void Load_ValidStream_LoadsSuccessfully()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidIniContent));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                Assert.That(doc.SectionCount, Is.EqualTo(2));
            });
        }

        #endregion

        #region DuplicateSection Policy Tests

        [Test]
        public void Load_DuplicateSection_ThrowErrorPolicy_ThrowsInvalidOperationException()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.ThrowError
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                IniConfigManager.Load(_tempFilePath, options));
        }

        [Test]
        public void Load_DuplicateSection_FirstWinPolicy_KeepsFirstSection()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.FirstWin
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section, Is.Not.Null);
                Assert.That(section["key1"].Value, Is.EqualTo("value1"));
                Assert.That(section.PropertyCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void Load_DuplicateSection_LastWinPolicy_KeepsLastSection()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.LastWin
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section, Is.Not.Null);
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section.PropertyCount, Is.EqualTo(1));
            });
        }

        #endregion

        #region Save Tests

        [Test]
        public void Save_NullFilePath_ThrowsArgumentException()
        {
            var doc = new Document();
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentException>(() =>
                IniConfigManager.Save(null, doc));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void Save_EmptyFilePath_ThrowsArgumentException()
        {
            var doc = new Document();
            Assert.Throws<ArgumentException>(() =>
                IniConfigManager.Save(string.Empty, doc));
        }

        [Test]
        public void Save_ValidDocument_SavesSuccessfully()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("key1", "value1");
            var section = new Section("Section1");
            section.AddProperty("key2", "value2");
            doc.AddSection(section);

            // Act
            IniConfigManager.Save(_tempFilePath, doc);

            // Assert
            Assert.That(File.Exists(_tempFilePath), Is.True);
            var loadedDoc = IniConfigManager.Load(_tempFilePath);
            Assert.Multiple(() =>
            {
                Assert.That(loadedDoc.DefaultSection["key1"].Value, Is.EqualTo("value1"));
                Assert.That(loadedDoc["Section1"]["key2"].Value, Is.EqualTo("value2"));
            });
        }

        [Test]
        public void Save_WithEncoding_NullEncoding_ThrowsArgumentNullException()
        {
            var doc = new Document();
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() =>
                IniConfigManager.Save(_tempFilePath, null, doc));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void Save_NullDocument_ThrowsArgumentNullException()
        {
            using var stream = new MemoryStream();
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() =>
                IniConfigManager.Save(stream, Encoding.UTF8, null));
#pragma warning restore CS8625
        }

        [Test]
        public void Save_ToStream_NonWritableStream_ThrowsArgumentException()
        {
            // Arrange
            var doc = new Document();
            using var stream = new MemoryStream();
            stream.Close(); // Make stream unwritable

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                IniConfigManager.Save(stream, Encoding.UTF8, doc));
        }

        [Test]
        public void Save_WithQuotedValues_SavesCorrectly()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Section1");

            var prop1 = new Property("key1", "simple value") { IsQuoted = true };
            var prop2 = new Property("key2", "value with \" quote") { IsQuoted = true };
            var prop3 = new Property("key3", "value with \n newline") { IsQuoted = true };
            var prop4 = new Property("key4", "normal value") { IsQuoted = false };

            section.AddProperty(prop1);
            section.AddProperty(prop2);
            section.AddProperty(prop3);
            section.AddProperty(prop4);
            doc.AddSection(section);

            // Act
            IniConfigManager.Save(_tempFilePath, doc);
            var loadedDoc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var loadedSection = loadedDoc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                Assert.That(loadedSection.GetProperty("key1")?.Value, Is.EqualTo("simple value"));
                Assert.That(loadedSection.GetProperty("key1")?.IsQuoted, Is.True);
                Assert.That(loadedSection.GetProperty("key2")?.Value, Is.EqualTo("value with \" quote"));
                Assert.That(loadedSection.GetProperty("key3")?.Value, Is.EqualTo("value with \n newline"));
                Assert.That(loadedSection.GetProperty("key4")?.Value, Is.EqualTo("normal value"));
                Assert.That(loadedSection.GetProperty("key4")?.IsQuoted, Is.False);
            });
        }

        [Test]
        public void SaveAndLoad_PreservesExactFormat()
        {
            // Arrange
            var original = @"[Section1]
key1=""simple value"" # comment1
key2=""value with \""quote\"""" ; comment2
key3=unquoted value # comment3

[Section2]
# Pre-comment
key1=""value with \n newline""
key2=""value with \t tab""";

            File.WriteAllText(_tempFilePath, original);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);
            var newFile = _tempFilePath + ".new";
            IniConfigManager.Save(newFile, doc);
            var loadedContent = File.ReadAllText(newFile);

            // Assert - normalize line endings for cross-platform compatibility
            var expected = @"[Section1]
key1 = ""simple value"" ; comment1
key2 = ""value with \""quote\"""" ; comment2
key3 = unquoted value ; comment3

[Section2]
; Pre-comment
key1 = ""value with \n newline""
key2 = ""value with \t tab""";
            Assert.That(loadedContent.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")));
        }

        [Test]
        public void Load_WithCommentInQuotedValue_ParsesCorrectly()
        {
            // Arrange
            var ini = @"
[Section]
key1=""value with # not a comment""
key2=""quoted value"" # actual comment
key3=""value with ; not a comment"" ; actual comment
";
            File.WriteAllText(_tempFilePath, ini);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var section = doc.GetSection("Section")!;
            Assert.Multiple(() =>
            {
                Assert.That(section.GetProperty("key1")?.Value, Is.EqualTo("value with # not a comment"));
                Assert.That(section.GetProperty("key2")?.Value, Is.EqualTo("quoted value"));
                Assert.That(section.GetProperty("key2")?.Comment?.Value, Is.EqualTo(" actual comment"));
                Assert.That(section.GetProperty("key3")?.Value, Is.EqualTo("value with ; not a comment"));
                Assert.That(section.GetProperty("key3")?.Comment?.Value, Is.EqualTo(" actual comment"));
            });
        }

        [Test]
        public void Save_WithUnquotedValues_SavesCorrectly()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Section1");

            section.AddProperty(new Property("key1", "simple value"));
            section.AddProperty(new Property("key2", "value with spaces"));
            section.AddProperty(new Property("key3", "123"));
            section.AddProperty(new Property("key4", "true"));

            doc.AddSection(section);

            // Act
            IniConfigManager.Save(_tempFilePath, doc);
            var loadedDoc = IniConfigManager.Load(_tempFilePath);

            // Assert
            var loadedSection = loadedDoc.GetSection("Section1")!;
            Assert.Multiple(() =>
            {
                Assert.That(loadedSection.GetProperty("key1")?.Value, Is.EqualTo("simple value"));
                Assert.That(loadedSection.GetProperty("key1")?.IsQuoted, Is.False);
                Assert.That(loadedSection.GetProperty("key2")?.Value, Is.EqualTo("value with spaces"));
                Assert.That(loadedSection.GetProperty("key2")?.IsQuoted, Is.False);
                Assert.That(loadedSection.GetProperty("key3")?.Value, Is.EqualTo("123"));
                Assert.That(loadedSection.GetProperty("key4")?.Value, Is.EqualTo("true"));
            });
        }

        [Test]
        public void Save_DoesNotMutateIsQuoted_WhenValueNeedsQuoting()
        {
            // Arrange - 특수 문자가 있어도 IsQuoted=false로 설정
            var doc = new Document();
            var section = new Section("Section1");
            var prop = new Property("key1", "value with ; semicolon") { IsQuoted = false };
            section.AddProperty(prop);
            doc.AddSection(section);

            // Act - Save 호출
            IniConfigManager.Save(_tempFilePath, doc);

            // Assert - IsQuoted가 변경되지 않아야 함
            Assert.That(prop.IsQuoted, Is.False, "Save should not mutate IsQuoted property");

            // 파일에는 자동으로 따옴표로 감싸져야 함
            var content = File.ReadAllText(_tempFilePath);
            Assert.That(content, Does.Contain("\"value with \\; semicolon\""));
        }

        #endregion

        #region Async I/O Tests (True Async)

        [Test]
        public async Task LoadAsync_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, ValidIniContent, Encoding.UTF8);

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc.DefaultSection["key1"].Value, Is.EqualTo("value1"));
                Assert.That(doc["Section1"]["key3"].Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public async Task LoadAsync_WithEncoding_LoadsSuccessfully()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, ValidIniContent, Encoding.UTF8);

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, Encoding.UTF8);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                Assert.That(doc.SectionCount, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task LoadAsync_WithOptions_LoadsSuccessfully()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.LastWin
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section.PropertyCount, Is.EqualTo(1));
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
            });
        }

        [Test]
        public void LoadAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, ValidIniContent);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await IniConfigManager.LoadAsync(_tempFilePath, null, cts.Token));
        }

        [Test]
        public async Task LoadAsync_FromStream_LoadsSuccessfully()
        {
            // Arrange
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidIniContent));

            // Act
            var doc = await IniConfigManager.LoadAsync(stream, Encoding.UTF8);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                Assert.That(doc.SectionCount, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task SaveAsync_ValidDocument_SavesSuccessfully()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("key1", "value1");
            var section = new Section("Section1");
            section.AddProperty("key2", "value2");
            doc.AddSection(section);

            // Act
            await IniConfigManager.SaveAsync(_tempFilePath, doc);

            // Assert
            Assert.That(File.Exists(_tempFilePath), Is.True);
            var loadedDoc = await IniConfigManager.LoadAsync(_tempFilePath);
            Assert.Multiple(() =>
            {
                Assert.That(loadedDoc.DefaultSection["key1"].Value, Is.EqualTo("value1"));
                Assert.That(loadedDoc["Section1"]["key2"].Value, Is.EqualTo("value2"));
            });
        }

        [Test]
        public async Task SaveAsync_WithEncoding_SavesSuccessfully()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("key1", "value1");

            // Act
            await IniConfigManager.SaveAsync(_tempFilePath, Encoding.UTF8, doc);

            // Assert
            Assert.That(File.Exists(_tempFilePath), Is.True);
            var content = await File.ReadAllTextAsync(_tempFilePath, Encoding.UTF8);
            Assert.That(content, Does.Contain("key1 = value1"));
        }

        [Test]
        public void SaveAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("key1", "value1");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            NUnit.Framework.Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await IniConfigManager.SaveAsync(_tempFilePath, doc, cts.Token));
        }

        [Test]
        public async Task SaveAsync_ToStream_SavesSuccessfully()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("key1", "value1");
            using var stream = new MemoryStream();

            // Act
            await IniConfigManager.SaveAsync(stream, Encoding.UTF8, doc);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            Assert.That(content, Does.Contain("key1 = value1"));
        }

        [Test]
        public async Task LoadAsync_MultipleConcurrentOperations_HandlesCorrectly()
        {
            // Arrange
            var tempFiles = new string[10];
            for (int i = 0; i < 10; i++)
            {
                tempFiles[i] = Path.GetTempFileName();
                File.WriteAllText(tempFiles[i], ValidIniContent);
            }

            try
            {
                // Act - Load multiple files concurrently
                var tasks = tempFiles.Select(file => IniConfigManager.LoadAsync(file)).ToArray();
                var documents = await Task.WhenAll(tasks);

                // Assert
                Assert.Multiple(() =>
                {
                    Assert.That(documents, Has.Length.EqualTo(10));
                    foreach (var doc in documents)
                    {
                        Assert.That(doc.DefaultSection.PropertyCount, Is.EqualTo(2));
                        Assert.That(doc.SectionCount, Is.EqualTo(2));
                    }
                });
            }
            finally
            {
                // Cleanup
                foreach (var file in tempFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        [Test]
        public async Task SaveAsync_MultipleConcurrentOperations_HandlesCorrectly()
        {
            // Arrange
            var tempFiles = new string[10];
            var documents = new Document[10];
            for (int i = 0; i < 10; i++)
            {
                tempFiles[i] = Path.GetTempFileName();
                documents[i] = new Document();
                documents[i].DefaultSection.AddProperty($"key{i}", $"value{i}");
            }

            try
            {
                // Act - Save multiple files concurrently
                var tasks = new Task[10];
                for (int i = 0; i < 10; i++)
                {
                    var index = i;
                    tasks[i] = IniConfigManager.SaveAsync(tempFiles[index], documents[index]);
                }
                await Task.WhenAll(tasks);

                // Assert - Verify all files were saved correctly
                for (int i = 0; i < 10; i++)
                {
                    Assert.That(File.Exists(tempFiles[i]), Is.True);
                    var doc = await IniConfigManager.LoadAsync(tempFiles[i]);
                    Assert.That(doc.DefaultSection[$"key{i}"].Value, Is.EqualTo($"value{i}"));
                }
            }
            finally
            {
                // Cleanup
                foreach (var file in tempFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        [Test]
        public async Task LoadAsync_LargeFile_LoadsEfficiently()
        {
            // Arrange - Create a large INI file with 1000 sections and 10 properties each
            var sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.AppendLine($"[Section{i}]");
                for (int j = 0; j < 10; j++)
                {
                    sb.AppendLine($"key{j}=value{j}");
                }
            }
            File.WriteAllText(_tempFilePath, sb.ToString());

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var doc = await IniConfigManager.LoadAsync(_tempFilePath);
            stopwatch.Stop();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(1000));
                Assert.That(doc["Section999"]["key9"].Value, Is.EqualTo("value9"));
                // Should load reasonably fast even with large files
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000));
            });
        }

        [Test]
        public async Task SaveAsync_LargeFile_SavesEfficiently()
        {
            // Arrange - Create a large document with 1000 sections
            var doc = new Document();
            for (int i = 0; i < 1000; i++)
            {
                var section = new Section($"Section{i}");
                for (int j = 0; j < 10; j++)
                {
                    section.AddProperty($"key{j}", $"value{j}");
                }
                doc.AddSection(section);
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await IniConfigManager.SaveAsync(_tempFilePath, doc);
            stopwatch.Stop();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(_tempFilePath), Is.True);
                // Should save reasonably fast even with large documents
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000));
            });
        }

        [Test]
        public void LoadAsync_NullFilePath_ThrowsArgumentException()
        {
#pragma warning disable CS8600, CS8625
            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await IniConfigManager.LoadAsync((string)null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void LoadAsync_EmptyFilePath_ThrowsArgumentException()
        {
            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await IniConfigManager.LoadAsync(string.Empty));
        }

        [Test]
        public void SaveAsync_NullFilePath_ThrowsArgumentException()
        {
            var doc = new Document();
#pragma warning disable CS8600, CS8625
            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await IniConfigManager.SaveAsync(null, doc));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void SaveAsync_EmptyFilePath_ThrowsArgumentException()
        {
            var doc = new Document();
            NUnit.Framework.Assert.ThrowsAsync<ArgumentException>(async () =>
                await IniConfigManager.SaveAsync(string.Empty, doc));
        }

        #endregion

        #region Regression Tests - Dictionary Sync Bug Fix

        [Test]
        public void Load_AfterLoading_GetSectionWorks()
        {
            // Arrange - This tests the critical bug fix where Dictionary wasn't synced
            var content = @"
[Server]
Host=localhost
Port=8080

[Database]
ConnectionString=Data Source=db.sqlite
";
            File.WriteAllText(_tempFilePath, content);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert - GetSection should work (was returning null before fix)
            Assert.Multiple(() =>
            {
                var serverSection = doc.GetSection("Server");
                Assert.That(serverSection, Is.Not.Null, "GetSection should return the Server section");
                Assert.That(serverSection!.Name, Is.EqualTo("Server"));

                var dbSection = doc.GetSection("Database");
                Assert.That(dbSection, Is.Not.Null, "GetSection should return the Database section");
                Assert.That(dbSection!.Name, Is.EqualTo("Database"));

                // HasSection should also work
                Assert.That(doc.HasSection("Server"), Is.True);
                Assert.That(doc.HasSection("Database"), Is.True);
            });
        }

        [Test]
        public void Load_AfterLoading_CaseInsensitiveGetSectionWorks()
        {
            // Arrange
            var content = @"
[Server]
Host=localhost
";
            File.WriteAllText(_tempFilePath, content);

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert - Case-insensitive lookup should work
            Assert.Multiple(() =>
            {
                Assert.That(doc.GetSection("SERVER"), Is.Not.Null);
                Assert.That(doc.GetSection("server"), Is.Not.Null);
                Assert.That(doc.GetSection("Server"), Is.Not.Null);

                var s1 = doc.GetSection("SERVER");
                var s2 = doc.GetSection("server");
                Assert.That(s1, Is.SameAs(s2), "All lookups should return same instance");
            });
        }

        [Test]
        public void Load_WithManySections_AllSectionsAccessible()
        {
            // Arrange - Test with multiple sections
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"[Section{i}]");
                sb.AppendLine($"Key{i}=Value{i}");
            }
            File.WriteAllText(_tempFilePath, sb.ToString());

            // Act
            var doc = IniConfigManager.Load(_tempFilePath);

            // Assert - All sections should be accessible via GetSection
            for (int i = 0; i < 100; i++)
            {
                var section = doc.GetSection($"Section{i}");
                Assert.That(section, Is.Not.Null, $"Section{i} should be accessible");
                Assert.That(section!.HasProperty($"Key{i}"), Is.True);
            }
        }

        [Test]
        public async Task LoadAsync_AfterLoading_GetSectionWorks()
        {
            // Arrange - Test async version has same fix
            var content = @"
[AsyncSection]
Key=Value
";
            File.WriteAllText(_tempFilePath, content);

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath);

            // Assert
            var section = doc.GetSection("AsyncSection");
            Assert.That(section, Is.Not.Null);
            Assert.That(section!.Name, Is.EqualTo("AsyncSection"));
        }

        [Test]
        public void Load_WithDuplicateSections_DictionaryStaysConsistent()
        {
            // Arrange
            var content = @"
[Server]
Port=8080
[Server]
Port=9090
";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.LastWin
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert - Dictionary should be consistent after duplicate handling
            var section = doc.GetSection("Server");
            Assert.That(section, Is.Not.Null);
            Assert.That(section!["Port"].Value, Is.EqualTo("9090"));
            Assert.That(doc.SectionCount, Is.EqualTo(1));
        }

        #endregion

        #region DuplicateSection Merge Policy Tests (CRITICAL - Previously Missing)

        [Test]
        public void Load_DuplicateSection_MergePolicy_MergesSections()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2
[Section1]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(1), "Should have exactly one section after merge");
                Assert.That(section.PropertyCount, Is.EqualTo(3), "Should have all properties merged");
                Assert.That(section["key1"].Value, Is.EqualTo("value1"));
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section["key3"].Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public void Load_DuplicateSection_MergeWithFirstWinKeys_KeepsFirstValue()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
key2=value2
[Section1]
key1=value1_new
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge,
                DuplicateKeyPolicy = IniConfigOption.DuplicateKeyPolicyType.FirstWin
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section.PropertyCount, Is.EqualTo(3));
                Assert.That(section["key1"].Value, Is.EqualTo("value1"), "FirstWin should keep original value");
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section["key3"].Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public void Load_DuplicateSection_MergeWithLastWinKeys_KeepsLastValue()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
key2=value2
[Section1]
key1=value1_new
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge,
                DuplicateKeyPolicy = IniConfigOption.DuplicateKeyPolicyType.LastWin
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section.PropertyCount, Is.EqualTo(3));
                Assert.That(section["key1"].Value, Is.EqualTo("value1_new"), "LastWin should keep new value");
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section["key3"].Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public void Load_DuplicateSection_MergeWithThrowErrorKeys_ThrowsException()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key1=value1_new";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge,
                DuplicateKeyPolicy = IniConfigOption.DuplicateKeyPolicyType.ThrowError
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                IniConfigManager.Load(_tempFilePath, options),
                "Should throw when duplicate keys found during merge with ThrowError policy");
        }

        [Test]
        public void Load_MultipleDuplicateSections_MergesAll()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section2]
keyA=valueA
[Section1]
key2=value2
[Section2]
keyB=valueB
[Section1]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2), "Should have 2 sections after merging");

                var section1 = doc["Section1"];
                Assert.That(section1.PropertyCount, Is.EqualTo(3));
                Assert.That(section1["key1"].Value, Is.EqualTo("value1"));
                Assert.That(section1["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section1["key3"].Value, Is.EqualTo("value3"));

                var section2 = doc["Section2"];
                Assert.That(section2.PropertyCount, Is.EqualTo(2));
                Assert.That(section2["keyA"].Value, Is.EqualTo("valueA"));
                Assert.That(section2["keyB"].Value, Is.EqualTo("valueB"));
            });
        }

        [Test]
        public async Task LoadAsync_DuplicateSection_MergePolicy_WorksCorrectly()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section1]
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(1));
                Assert.That(section.PropertyCount, Is.EqualTo(2));
                Assert.That(section["key1"].Value, Is.EqualTo("value1"));
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
            });
        }

        [Test]
        public void Load_DuplicateSection_MergePolicy_CaseInsensitive_MergesSections()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[SECTION1]
key2=value2
[section1]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(1), "Should merge case-insensitive duplicates into one section");
                Assert.That(section.PropertyCount, Is.EqualTo(3), "Should have all properties merged");
                Assert.That(section["key1"].Value, Is.EqualTo("value1"));
                Assert.That(section["key2"].Value, Is.EqualTo("value2"));
                Assert.That(section["key3"].Value, Is.EqualTo("value3"));
            });
        }

        #endregion

        #region CollectParsingErrors Integration Tests (HIGH - Previously Missing)

        [Test]
        public void Load_WithCollectParsingErrors_CollectsAllErrors()
        {
            // Arrange
            var content = @"
[Section
key1
key2=""unterminated
=emptykey";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(4), "Should collect all 4 parsing errors");
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("closing bracket"));
                Assert.That(doc.ParsingErrors[1].Reason, Does.Contain("equals sign"));
                Assert.That(doc.ParsingErrors[2].Reason, Does.Contain("Unterminated"));
                Assert.That(doc.ParsingErrors[3].Reason, Does.Contain("empty"));
            });
        }

        [Test]
        public void Load_WithCollectParsingErrors_ContinuesParsing()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section
key2=value2
[Section2]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1), "Should have 1 parsing error");
                Assert.That(doc.SectionCount, Is.EqualTo(2), "Should still parse valid sections");
                Assert.That(doc["Section1"]["key1"].Value, Is.EqualTo("value1"));
                Assert.That(doc["Section2"]["key3"].Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public void Load_WithCollectParsingErrors_False_ThrowsOnError()
        {
            // Arrange
            var content = @"
[Section
key1=value1";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = false
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert - Should not throw, but errors should not be collected
            Assert.That(doc.ParsingErrors.Count, Is.EqualTo(0), "Should not collect errors when CollectParsingErrors is false");
        }

        [Test]
        public async Task LoadAsync_WithCollectParsingErrors_CollectsAllErrors()
        {
            // Arrange
            var content = @"
[Section
key1
key2=""unterminated";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(3), "Should collect all parsing errors in async mode");
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("closing bracket"));
                Assert.That(doc.ParsingErrors[1].Reason, Does.Contain("equals sign"));
                Assert.That(doc.ParsingErrors[2].Reason, Does.Contain("Unterminated"));
            });
        }

        [Test]
        public void Load_WithCollectParsingErrors_PreservesLineNumbers()
        {
            // Arrange
            var content = @"key1=value1
key2=value2
[Section
validkey=validvalue
invalid_line_without_equals
anotherkey=anothervalue
key3=""unterminated";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(3), "Should have 3 parsing errors");
                Assert.That(doc.ParsingErrors[0].LineNumber, Is.EqualTo(3), "Error on line 3: [Section (missing closing bracket)");
                Assert.That(doc.ParsingErrors[1].LineNumber, Is.EqualTo(5), "Error on line 5: invalid_line_without_equals");
                Assert.That(doc.ParsingErrors[2].LineNumber, Is.EqualTo(7), "Error on line 7: unterminated quote");
            });
        }

        [Test]
        public void Load_EmptySectionName_CollectsError()
        {
            // Arrange
            var content = @"
[]
key=value";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("empty").IgnoreCase);
            });
        }

        #endregion

        #region MaxParsingErrors Tests (CRITICAL - Security Feature)

        [Test]
        public void Load_WithMaxParsingErrors_LimitsErrorCollection()
        {
            // Arrange - 10개의 오류가 있는 파일
            var content = @"
[Section
key1
=value
key2
[Section2
key3
=empty
key4
key5
key6";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxParsingErrors = 3
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(3), "Should collect only 3 errors when MaxParsingErrors=3");
            });
        }

        [Test]
        public void Load_WithMaxParsingErrors_Zero_CollectsUnlimitedErrors()
        {
            // Arrange - 5개의 오류가 있는 파일
            var content = @"
[Section
key1
=value
key2
key3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxParsingErrors = 0 // unlimited
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.That(doc.ParsingErrors.Count, Is.EqualTo(5), "Should collect all errors when MaxParsingErrors=0");
        }

        [Test]
        public void Load_WithMaxParsingErrors_ContinuesParsing()
        {
            // Arrange
            var content = @"
[Section
key1
=value
[ValidSection]
validKey=validValue";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxParsingErrors = 2
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(2), "Should stop collecting after 2 errors");
                Assert.That(doc.SectionCount, Is.EqualTo(1), "Should still parse valid sections");
                Assert.That(doc["ValidSection"]["validKey"].Value, Is.EqualTo("validValue"));
            });
        }

        [Test]
        public async Task LoadAsync_WithMaxParsingErrors_LimitsErrorCollection()
        {
            // Arrange
            var content = @"
[Section
key1
=value
key2
key3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxParsingErrors = 2
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.That(doc.ParsingErrors.Count, Is.EqualTo(2), "Async load should also respect MaxParsingErrors");
        }

        #endregion

        #region Security Limit Tests (MaxSections, MaxPropertiesPerSection, MaxValueLength, MaxLineLength)

        [Test]
        public void Load_WithMaxSections_LimitsSectionCount()
        {
            // Arrange - 5개 섹션이 있지만 3개만 허용
            var content = @"
[Section1]
key1=value1
[Section2]
key2=value2
[Section3]
key3=value3
[Section4]
key4=value4
[Section5]
key5=value5";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxSections = 3
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(3), "Should have only 3 sections");
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(2), "Should report 2 errors for exceeded sections");
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("Maximum section limit"));
            });
        }

        [Test]
        public void Load_WithMaxPropertiesPerSection_LimitsPropertyCount()
        {
            // Arrange - 5개 속성이 있지만 3개만 허용
            var content = @"
[Section1]
key1=value1
key2=value2
key3=value3
key4=value4
key5=value5";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxPropertiesPerSection = 3
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section.PropertyCount, Is.EqualTo(3), "Should have only 3 properties");
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(2), "Should report 2 errors for exceeded properties");
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("Maximum properties per section"));
            });
        }

        [Test]
        public void Load_WithMaxValueLength_RejectsLongValues()
        {
            // Arrange
            var longValue = new string('x', 100);
            var content = $@"
[Section1]
key1=short
key2={longValue}
key3=ok";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxValueLength = 50
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            var section = doc["Section1"];
            Assert.Multiple(() =>
            {
                Assert.That(section.PropertyCount, Is.EqualTo(2), "Should have only 2 valid properties");
                Assert.That(section.HasProperty("key1"), Is.True);
                Assert.That(section.HasProperty("key2"), Is.False, "key2 should be rejected due to long value");
                Assert.That(section.HasProperty("key3"), Is.True);
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("Value length"));
            });
        }

        [Test]
        public void Load_WithMaxLineLength_RejectsLongLines()
        {
            // Arrange
            var longLine = "[" + new string('x', 200) + "]";
            var content = $@"
[Section1]
key1=value1
{longLine}
key2=value2";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxLineLength = 100
            };

            // Act
            var doc = IniConfigManager.Load(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("Line exceeds maximum length"));
            });
        }

        [Test]
        public async Task LoadAsync_WithMaxSections_LimitsSectionCount()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
[Section2]
key2=value2
[Section3]
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxSections = 2
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.SectionCount, Is.EqualTo(2));
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task LoadAsync_WithMaxPropertiesPerSection_LimitsPropertyCount()
        {
            // Arrange
            var content = @"
[Section1]
key1=value1
key2=value2
key3=value3";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxPropertiesPerSection = 2
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc["Section1"].PropertyCount, Is.EqualTo(2));
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task LoadAsync_WithMaxValueLength_RejectsLongValues()
        {
            // Arrange
            var longValue = new string('y', 60);
            var content = $@"
[Section1]
key1={longValue}
key2=short";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxValueLength = 50
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc["Section1"].PropertyCount, Is.EqualTo(1));
                Assert.That(doc["Section1"]["key2"].Value, Is.EqualTo("short"));
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task LoadAsync_WithMaxLineLength_RejectsLongLines()
        {
            // Arrange
            var longKey = "key" + new string('z', 150);
            var content = $@"
[Section1]
{longKey}=value
normalKey=normalValue";
            File.WriteAllText(_tempFilePath, content);
            var options = new IniConfigOption
            {
                CollectParsingErrors = true,
                MaxLineLength = 100
            };

            // Act
            var doc = await IniConfigManager.LoadAsync(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc.ParsingErrors.Count, Is.EqualTo(1));
                Assert.That(doc.ParsingErrors[0].Reason, Does.Contain("Line exceeds maximum length"));
                Assert.That(doc["Section1"]["normalKey"].Value, Is.EqualTo("normalValue"));
            });
        }

        #endregion
    }
}
