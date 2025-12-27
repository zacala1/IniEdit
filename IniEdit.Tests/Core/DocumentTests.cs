namespace IniEdit.Tests.Core
{
    [TestFixture]
    public class DocumentTests
    {
#pragma warning disable CS8618
        private Document _document;
#pragma warning restore CS8618
        private const string TEST_SECTION_NAME = "TestSection";

        [SetUp]
        public void Setup()
        {
            _document = new Document();
        }

        #region Constructor and Basic Properties Tests

        [Test]
        public void Constructor_WithDefaultOption_InitializesCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_document.SectionCount, Is.Zero);
                Assert.That(_document.DefaultSection, Is.Not.Null);
                Assert.That(_document.DefaultSection.Name, Is.EqualTo("$DEFAULT"));
                Assert.That(_document.CommentPrefixChars, Is.Not.Empty);
            });
        }

        [Test]
        public void Constructor_WithCustomOption_InitializesCorrectly()
        {
            // Arrange
            var option = new IniConfigOption
            {
                CommentPrefixChars = new[] { '#', '/' },
                DefaultCommentPrefixChar = '#'
            };

            // Act
            var document = new Document(option);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(document.CommentPrefixChars, Is.EqualTo(new[] { '#', '/' }));
                Assert.That(document.DefaultCommentPrefixChar, Is.EqualTo('#'));
            });
        }

        [Test]
        public void DefaultCommentPrefixChar_SetInvalidChar_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _document.DefaultCommentPrefixChar = 'x');
        }

        #endregion

        #region Indexer and Section Access Tests

        [Test]
        public void IndexerByInt_InvalidIndex_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = _document[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = _document[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = _document[999]);
        }

        [Test]
        public void IndexerByString_NonExistingSection_CreatesNewSection()
        {
            // Act
            var section = _document["NewSection"];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(section, Is.Not.Null);
                Assert.That(section.Name, Is.EqualTo("NewSection"));
                Assert.That(_document.SectionCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void IndexerByString_ExistingSection_ReturnsSameSection()
        {
            // Arrange
            var section1 = _document[TEST_SECTION_NAME];

            // Act
            var section2 = _document[TEST_SECTION_NAME];

            // Assert
            Assert.That(section2, Is.SameAs(section1));
        }

        [Test]
        public void GetSection_CaseInsensitive_FindsSection()
        {
            // Arrange
            _document.AddSection("TestSection");

            // Act
            var section1 = _document.GetSection("TESTSECTION");
            var section2 = _document.GetSection("testsection");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(section1, Is.Not.Null);
                Assert.That(section2, Is.Not.Null);
                Assert.That(section1, Is.SameAs(section2));
            });
        }

        #endregion

        #region Section Management Tests

        [Test]
        [Ignore("Policy changed: AddSection_NullSection now throws exception")]
        public void AddSection_NullSection_DoesNothing()
        {
            // Act
#pragma warning disable CS8600, CS8625
            _document.AddSection((Section)null);
#pragma warning restore CS8600, CS8625

            // Assert
            Assert.That(_document.SectionCount, Is.Zero);
        }

        [Test]
        public void AddSection_NullSection_ThrowsException()
        {
            // Arrange
            Section? section = null;

            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _document.AddSection(section!));
        }

        [Test]
        public void InsertSection_InvalidIndex_ThrowsException()
        {
            // Arrange
            var section = new Section(TEST_SECTION_NAME);

            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _document.InsertSection(1, section));
        }

        [Test]
        public void InsertSection_DuplicateSection_ThrowsException()
        {
            // Arrange
            _document.AddSection(TEST_SECTION_NAME);
            var duplicateSection = new Section(TEST_SECTION_NAME);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _document.InsertSection(0, duplicateSection));
        }

        [Test]
        public void HasSection_NullOrEmptyName_ThrowsException()
        {
            Assert.Multiple(() =>
            {
#pragma warning disable CS8600, CS8625
                Assert.Throws<ArgumentException>(() => _document.HasSection((string)null));
#pragma warning restore CS8600, CS8625
                Assert.Throws<ArgumentException>(() => _document.HasSection(string.Empty));
            });
        }

        #endregion

        #region Sorting Tests

        [Test]
        public void SortSectionsByName_SortsCorrectly()
        {
            // Arrange
            _document.AddSection("C");
            _document.AddSection("A");
            _document.AddSection("B");

            // Act
            _document.SortSectionsByName();

            // Assert
            var sections = _document.GetSections();
            Assert.Multiple(() =>
            {
                Assert.That(sections[0].Name, Is.EqualTo("A"));
                Assert.That(sections[1].Name, Is.EqualTo("B"));
                Assert.That(sections[2].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void SortPropertiesByName_SortsAllSectionsProperties()
        {
            // Arrange
            var section = _document[TEST_SECTION_NAME];
            section.AddProperty("C", "3");
            section.AddProperty("A", "1");
            section.AddProperty("B", "2");

            // Act
            _document.SortPropertiesByName();

            // Assert
            var properties = section.GetProperties();
            Assert.Multiple(() =>
            {
                Assert.That(properties[0].Name, Is.EqualTo("A"));
                Assert.That(properties[1].Name, Is.EqualTo("B"));
                Assert.That(properties[2].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void SortAllByName_SortsBothSectionsAndProperties()
        {
            // Arrange
            _document.AddSection("SectionC");
            _document.AddSection("SectionA");
            _document.AddSection("SectionB");

            _document["SectionA"].AddProperty("PropC", "3");
            _document["SectionA"].AddProperty("PropA", "1");
            _document["SectionA"].AddProperty("PropB", "2");

            // Act
            _document.SortAllByName();

            // Assert
            var sections = _document.GetSections();
            var properties = sections[0].GetProperties();

            Assert.Multiple(() =>
            {
                // Check sections are sorted
                Assert.That(sections[0].Name, Is.EqualTo("SectionA"));
                Assert.That(sections[1].Name, Is.EqualTo("SectionB"));
                Assert.That(sections[2].Name, Is.EqualTo("SectionC"));

                // Check properties are sorted
                Assert.That(properties[0].Name, Is.EqualTo("PropA"));
                Assert.That(properties[1].Name, Is.EqualTo("PropB"));
                Assert.That(properties[2].Name, Is.EqualTo("PropC"));
            });
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void RemoveSection_NonExistentSection_ReturnsFalse()
        {
            Assert.That(_document.RemoveSection("NonExistent"), Is.False);
        }

        [Test]
        public void RemoveSectionAt_InvalidIndex_ReturnsFalse()
        {
            Assert.That(_document.RemoveSection(999), Is.False);
        }

        [Test]
        public void GetSectionByIndex_InvalidIndex_ReturnsNull()
        {
            Assert.That(_document.GetSectionByIndex(-1), Is.Null);
            Assert.That(_document.GetSectionByIndex(999), Is.Null);
        }

        [Test]
        [Ignore("Policy changed: duplicate sections are no longer allowed")]
        public void AddSection_DuplicateName_HandlesCorrectly()
        {
            // Arrange
            _document.AddSection(TEST_SECTION_NAME);
            var section2 = new Section(TEST_SECTION_NAME);
            section2.AddProperty("Key", "Value");

            // Act
            _document.AddSection(section2);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_document.SectionCount, Is.EqualTo(2));
                Assert.That(_document.GetSections().Count(s => s.Name == TEST_SECTION_NAME), Is.EqualTo(2));
            });
        }

        [Test]
        public void AddSection_DuplicateName_ThrowException()
        {
            // Arrange
            _document.AddSection(TEST_SECTION_NAME);
            var section2 = new Section(TEST_SECTION_NAME);
            section2.AddProperty("Key", "Value");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _document.AddSection(section2));
            Assert.Multiple(() =>
            {
                Assert.That(_document.SectionCount, Is.EqualTo(1));
                Assert.That(_document.GetSections().Count(s => s.Name == TEST_SECTION_NAME), Is.EqualTo(1));
            });
        }

        [Test]
        public void Clear_RemovesAllSectionsButKeepsDefaultSection()
        {
            // Arrange
            _document.AddSection("Section1");
            _document.AddSection("Section2");
            _document.DefaultSection.AddProperty("Key", "Value");

            // Act
            _document.Clear();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_document.SectionCount, Is.Zero);
                Assert.That(_document.DefaultSection, Is.Not.Null);
                Assert.That(_document.DefaultSection.PropertyCount, Is.EqualTo(1));
            });
        }

        #endregion

        #region MEDIUM Priority Tests - Dictionary Lookup & Nested Value Access

        [Test]
        public void GetSection_WithManySection_PerformsEfficiently()
        {
            // Arrange - Add 1000 sections
            for (int i = 0; i < 1000; i++)
            {
                _document.AddSection($"Section{i}");
            }

            // Act - Lookup should be O(1), not O(n)
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var section = _document.GetSection("Section999");
            stopwatch.Stop();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(section, Is.Not.Null);
                Assert.That(section!.Name, Is.EqualTo("Section999"));
                // Dictionary lookup should be very fast
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10));
            });
        }

        [Test]
        public void GetValue_ValidSectionAndProperty_ReturnsValue()
        {
            // Arrange
            _document["Server"]["Port"].Value = "8080";

            // Act
            var value = _document.GetValue<int>("Server", "Port");

            // Assert
            Assert.That(value, Is.EqualTo(8080));
        }

        [Test]
        public void GetValue_NonExistentSection_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _document.GetValue<int>("NonExistent", "Port"));
        }

        [Test]
        public void GetValueOrDefault_NonExistentSection_ReturnsDefault()
        {
            // Act
            var value = _document.GetValueOrDefault<int>("NonExistent", "Port", 3000);

            // Assert
            Assert.That(value, Is.EqualTo(3000));
        }

        [Test]
        public void GetValueOrDefault_NonExistentProperty_ReturnsDefault()
        {
            // Arrange
            _document.AddSection("Server");

            // Act
            var value = _document.GetValueOrDefault<int>("Server", "Port", 3000);

            // Assert
            Assert.That(value, Is.EqualTo(3000));
        }

        [Test]
        public void TryGetValue_ValidSectionAndProperty_ReturnsTrue()
        {
            // Arrange
            _document["Database"]["MaxConnections"].Value = "100";

            // Act
            var result = _document.TryGetValue<int>("Database", "MaxConnections", out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(100));
            });
        }

        [Test]
        public void TryGetValue_NonExistentSection_ReturnsFalse()
        {
            // Act
            var result = _document.TryGetValue<int>("NonExistent", "Key", out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(int)));
            });
        }

        #endregion

        #region LOW Priority Tests - Fluent API

        [Test]
        public void WithSection_AddsSection_ReturnsDocument()
        {
            // Act
            var result = _document.WithSection("Server");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_document));
                Assert.That(_document.HasSection("Server"), Is.True);
            });
        }

        [Test]
        public void WithDefaultProperty_AddsProperty_ReturnsDocument()
        {
            // Act
            var result = _document.WithDefaultProperty("Version", "1.0");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_document));
                Assert.That(_document.DefaultSection["Version"].Value, Is.EqualTo("1.0"));
            });
        }

        [Test]
        public void FluentAPI_ChainMultipleOperations_WorksCorrectly()
        {
            // Act
            _document
                .WithDefaultProperty("AppName", "MyApp")
                .WithDefaultProperty<int>("Version", 2)
                .WithSection("Server")
                .WithSection("Database");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_document.DefaultSection["AppName"].Value, Is.EqualTo("MyApp"));
                Assert.That(_document.DefaultSection["Version"].Value, Is.EqualTo("2"));
                Assert.That(_document.HasSection("Server"), Is.True);
                Assert.That(_document.HasSection("Database"), Is.True);
            });
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void GetSection_CaseInsensitive_ReturnsCorrectSection()
        {
            // Arrange
            _document.AddSection("Server");

            // Act
            var section1 = _document.GetSection("SERVER");
            var section2 = _document.GetSection("server");
            var section3 = _document.GetSection("Server");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(section1, Is.Not.Null);
                Assert.That(section2, Is.Not.Null);
                Assert.That(section3, Is.Not.Null);
                Assert.That(section1, Is.SameAs(section2));
                Assert.That(section2, Is.SameAs(section3));
            });
        }

        [Test]
        public void RemoveSection_UpdatesDictionary_ReturnsTrue()
        {
            // Arrange
            _document.AddSection("ToRemove");

            // Act
            var removed = _document.RemoveSection("ToRemove");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(_document.HasSection("ToRemove"), Is.False);
                Assert.That(_document.GetSection("ToRemove"), Is.Null);
            });
        }

        [Test]
        public void Clear_UpdatesDictionary_RemovesAllSections()
        {
            // Arrange
            _document.AddSection("Section1");
            _document.AddSection("Section2");

            // Act
            _document.Clear();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_document.SectionCount, Is.Zero);
                Assert.That(_document.HasSection("Section1"), Is.False);
                Assert.That(_document.HasSection("Section2"), Is.False);
            });
        }

        #endregion
    }
}
