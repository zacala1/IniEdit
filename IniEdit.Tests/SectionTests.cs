using static IniEdit.IniConfigOption;

namespace IniEdit.Tests
{
    [TestFixture]
    public class SectionTests
    {
#pragma warning disable CS8618
        private Section _section;
#pragma warning restore CS8618
        private const string TEST_SECTION_NAME = "TestSection";

        [SetUp]
        public void Setup()
        {
            _section = new Section(TEST_SECTION_NAME);
        }

        #region Constructor and Basic Properties Tests

        [Test]
        public void Constructor_CreatesEmptySection()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_section.Name, Is.EqualTo(TEST_SECTION_NAME));
                Assert.That(_section.PropertyCount, Is.Zero);
                Assert.That(_section.PreComments, Is.Empty);
                Assert.That(_section.Comment, Is.Null);
            });
        }

        [Test]
        public void PropertyCount_ReflectsActualCount()
        {
            // Arrange & Act
            _section.AddProperty("TestKey", "TestValue");
            _section.AddProperty("Another", "Value");

            // Assert
            Assert.That(_section.PropertyCount, Is.EqualTo(2));
        }

        #endregion

        #region Indexer Tests

        [Test]
        public void IndexerByInt_ValidIndex_ReturnsProperty()
        {
            // Arrange
            var property = new Property("TestKey", "TestValue");
            _section.AddProperty(property);

            // Act & Assert
            Assert.That(_section[0].Name, Is.EqualTo("TestKey"));
            Assert.That(_section[0].Value, Is.EqualTo("TestValue"));
        }

        [Test]
        public void IndexerByInt_InvalidIndex_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = _section[0]);
        }

        [Test]
        public void IndexerByString_ExistingKey_ReturnsProperty()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var property = _section["TestKey"];

            // Assert
            Assert.That(property.Value, Is.EqualTo("TestValue"));
        }

        [Test]
        public void IndexerByString_NonExistingKey_CreatesNewProperty()
        {
            // Act
            var property = _section["NewProperty"];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(property, Is.Not.Null);
                Assert.That(property.Name, Is.EqualTo("NewProperty"));
                Assert.That(property.Value, Is.Empty);
                Assert.That(_section.PropertyCount, Is.EqualTo(1));
            });
        }

        #endregion

        #region Property Management Tests

        [Test]
        public void GetPropertyByKey_CaseInsensitive_FindsProperty()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var prop1 = _section.GetProperty("TESTKEY");
            var prop2 = _section.GetProperty("testkey");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(prop1, Is.Not.Null);
                Assert.That(prop2, Is.Not.Null);
                Assert.That(prop1, Is.SameAs(prop2));
            });
        }

        [Test]
        public void AddProperty_DuplicateKeys_ThrowsException()
        {
            // Arrange
            _section.AddProperty("key", "value1");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _section.AddProperty("key", "value2"));
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(1));
                Assert.That(_section["key"].Value, Is.EqualTo("value1")); // Verify original value is preserved
            });
        }

        [Test]
        public void AddPropertyRange_WithNullElements_SkipsNullElements()
        {
            // Arrange
#pragma warning disable CS8625
            var properties = new Property[]
            {
                new Property("key1", "value1"),
                null,
                new Property("key2", "value2")
            };
#pragma warning restore CS8625

            // Act
            _section.AddPropertyRange(properties);

            // Assert
            Assert.That(_section.PropertyCount, Is.EqualTo(2));
        }

        [Test]
        public void AddProperty_ValidProperty_AddsToCollection()
        {
            // Act
            _section.AddProperty("TestKey", "TestValue");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(1));
                Assert.That(_section[0].Name, Is.EqualTo("TestKey"));
                Assert.That(_section[0].Value, Is.EqualTo("TestValue"));
            });
        }

        [Test]
        public void AddProperty_NullProperty_ThrowsArgumentNullException()
        {
            // Act & Assert
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() => _section.AddProperty(null));
#pragma warning restore CS8600, CS8625
            Assert.That(_section.PropertyCount, Is.Zero);
        }

        [Test]
        public void AddPropertyRange_ValidCollection_AddsAllProperties()
        {
            // Arrange
            var properties = new[]
            {
                new Property("Prop1", "Value1"),
                new Property("Prop2", "Value2")
            };

            // Act
            _section.AddPropertyRange(properties);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(2));
                Assert.That(_section[0].Name, Is.EqualTo("Prop1"));
                Assert.That(_section[1].Name, Is.EqualTo("Prop2"));
            });
        }

        [Test]
        public void AddPropertyRange_NullCollection_ThrowsException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() => _section.AddPropertyRange(null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void InsertPropertyAt_ValidIndex_InsertsProperty()
        {
            // Arrange
            _section.AddProperty("First", "1");
            _section.AddProperty("Third", "3");

            // Act
            _section.InsertProperty(1, "Second", "2");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(3));
                Assert.That(_section[0].Name, Is.EqualTo("First"));
                Assert.That(_section[1].Name, Is.EqualTo("Second"));
                Assert.That(_section[2].Name, Is.EqualTo("Third"));
            });
        }

        [Test]
        public void InsertProperty_InvalidIndex_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _section.InsertProperty(1, "TestKey", "TestValue"));
        }

        #endregion

        #region Property Removal Tests

        [Test]
        public void RemoveProperty_ExistingProperty_RemovesAndReturnsTrue()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var result = _section.RemoveProperty("TestKey");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_section.PropertyCount, Is.Zero);
            });
        }

        [Test]
        public void RemoveProperty_NonExistingProperty_ReturnsFalse()
        {
            Assert.That(_section.RemoveProperty("NonExisting"), Is.False);
        }

        [Test]
        public void RemovePropertyAt_ValidIndex_RemovesAndReturnsTrue()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var result = _section.RemoveProperty(0);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_section.PropertyCount, Is.Zero);
            });
        }

        [Test]
        public void RemovePropertyAt_InvalidIndex_ReturnsFalse()
        {
            Assert.That(_section.RemoveProperty(0), Is.False);
        }

        [Test]
        public void RemovePropertyAt_AfterClear_ReturnsFalse()
        {
            // Arrange
            _section.AddProperty("key", "value");
            _section.Clear();

            // Act & Assert
            Assert.That(_section.RemoveProperty(0), Is.False);
        }

        [Test]
        public void Clear_RemovesAllProperties()
        {
            // Arrange
            _section.AddProperty("Prop1", "Value1");
            _section.AddProperty("Prop2", "Value2");

            // Act
            _section.Clear();

            // Assert
            Assert.That(_section.PropertyCount, Is.Zero);
        }

        #endregion

        #region Property Check Tests

        [Test]
        public void HasProperty_ExistingPropertyInstance_ReturnsTrue()
        {
            // Arrange
            var property = new Property("TestKey", "TestValue");
            _section.AddProperty(property);

            // Act & Assert
            Assert.That(_section.HasProperty(property), Is.True);
        }

        [Test]
        public void HasProperty_NonExistingPropertyInstance_ReturnsFalse()
        {
            // Arrange
            var property = new Property("TestKey", "TestValue");

            // Act & Assert
            Assert.That(_section.HasProperty(property), Is.False);
        }

        [Test]
        public void HasProperty_NullProperty_ThrowsException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() => _section.HasProperty((Property)null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void HasProperty_ExistingPropertyName_ReturnsTrue()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act & Assert
            Assert.That(_section.HasProperty("TestKey"), Is.True);
        }

        [Test]
        public void HasProperty_EmptyPropertyName_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => _section.HasProperty(string.Empty));
        }

        #endregion

        #region Sorting and Enumeration Tests

        [Test]
        public void SortPropertiesByName_SortsCorrectly()
        {
            // Arrange
            _section.AddProperty("C", "3");
            _section.AddProperty("A", "1");
            _section.AddProperty("B", "2");

            // Act
            _section.SortPropertiesByName();

            // Assert
            var properties = _section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(properties[0].Name, Is.EqualTo("A"));
                Assert.That(properties[1].Name, Is.EqualTo("B"));
                Assert.That(properties[2].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void GetEnumerator_ReturnsAllProperties()
        {
            // Arrange
            _section.AddProperty("Prop1", "Value1");
            _section.AddProperty("Prop2", "Value2");

            // Act
            var properties = _section.ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(properties, Has.Count.EqualTo(2));
                Assert.That(properties[0].Name, Is.EqualTo("Prop1"));
                Assert.That(properties[1].Name, Is.EqualTo("Prop2"));
            });
        }

        #endregion

        #region Clone and Merge Tests

        [Test]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");
            _section.PreComments.Add(new Comment("PreComment"));
            _section.Comment = new Comment("PostComment");

            // Act
            var clone = _section.Clone();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(clone.Name, Is.EqualTo(_section.Name));
                Assert.That(clone.PropertyCount, Is.EqualTo(_section.PropertyCount));
                Assert.That(clone[0].Name, Is.EqualTo(_section[0].Name));
                Assert.That(clone[0].Value, Is.EqualTo(_section[0].Value));
                Assert.That(clone.PreComments[0].Value, Is.EqualTo(_section.PreComments[0].Value));
                Assert.That(clone.Comment?.Value, Is.EqualTo(_section.Comment?.Value));
            });
        }

        [Test]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");
            var clone = _section.Clone();

            // Act
            clone.AddProperty("NewProp", "NewValue");
            clone["TestKey"].Value = "ModifiedValue";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(1));
                Assert.That(_section["TestKey"].Value, Is.EqualTo("TestValue"));
                Assert.That(clone.PropertyCount, Is.EqualTo(2));
                Assert.That(clone["TestKey"].Value, Is.EqualTo("ModifiedValue"));
            });
        }

        [Test]
        public void MergeFrom_NullSection_ThrowsArgumentNullException()
        {
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentNullException>(() => _section.MergeFrom(null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        public void MergeFromOnFirstWin_KeepsExistingProperties()
        {
            // Arrange
            _section.AddProperty(new Property("prop1", "value1"));
            var section2 = new Section("Section2");
            section2.AddProperty(new Property("prop1", "value2"));

            // Act
            _section.MergeFrom(section2, DuplicateKeyPolicyType.FirstWin);

            // Assert
            Assert.That(_section.GetProperty("prop1")?.Value, Is.EqualTo("value1"));
        }

        [Test]
        public void MergeFromOnFirstWin_AddsNewProperties()
        {
            // Arrange
            _section.AddProperty(new Property("prop1", "value1"));
            var section2 = new Section("Section2");
            section2.AddProperty(new Property("prop2", "value2"));

            // Act
            _section.MergeFrom(section2, DuplicateKeyPolicyType.FirstWin);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.GetProperty("prop1")?.Value, Is.EqualTo("value1"));
                Assert.That(_section.GetProperty("prop2")?.Value, Is.EqualTo("value2"));
            });
        }

        [Test]
        public void MergeFromOnLastWin_OverwritesExistingProperties()
        {
            // Arrange
            _section.AddProperty(new Property("prop1", "value1"));
            _section.AddProperty(new Property("prop2", "value2"));
            var section2 = new Section("Section2");
            section2.AddProperty(new Property("prop1", "newValue1"));
            section2.PreComments.Add(new Comment("Test comment"));

            // Act
            _section.MergeFrom(section2, DuplicateKeyPolicyType.LastWin);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.GetProperty("prop1")?.Value, Is.EqualTo("newValue1"));
                Assert.That(_section.GetProperty("prop2")?.Value, Is.EqualTo("value2"));
                Assert.That(_section.PreComments, Has.Count.EqualTo(1));
                Assert.That(_section.PreComments[0].Value, Is.EqualTo("Test comment"));
            });
        }

        [Test]
        public void MergeFromOnThrowError_WithDuplicateProperties_ThrowsArgumentException()
        {
            // Arrange
            _section.AddProperty(new Property("prop1", "value1"));
            var section2 = new Section("Section2");
            section2.AddProperty(new Property("prop1", "value2"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _section.MergeFrom(section2, DuplicateKeyPolicyType.ThrowError));
        }

        [Test]
        public void MergeFromOnThrowError_WithoutDuplicates_MergesSuccessfully()
        {
            // Arrange
            _section.AddProperty(new Property("prop1", "value1"));
            _section.Comment = new Comment("Original comment");
            var section2 = new Section("Section2");
            section2.AddProperty(new Property("prop2", "value2"));
            section2.PreComments.Add(new Comment("Pre comment"));
            section2.Comment = new Comment("New comment");

            // Act
            _section.MergeFrom(section2, DuplicateKeyPolicyType.ThrowError);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.GetProperty("prop1")?.Value, Is.EqualTo("value1"));
                Assert.That(_section.GetProperty("prop2")?.Value, Is.EqualTo("value2"));
                Assert.That(_section.PreComments, Has.Count.EqualTo(1));
                Assert.That(_section.PreComments[0].Value, Is.EqualTo("Pre comment"));
                Assert.That(_section.Comment?.Value, Is.EqualTo("Original commentNew comment"));
            });
        }

        [Test]
        public void MergeFrom_ClonesSourceSection()
        {
            // Arrange
            var section2 = new Section("Section2");
            var property = new Property("prop1", "value1");
            section2.AddProperty(property);

            // Act
            _section.MergeFrom(section2);
            property.Value = "modified";  // Modify original property

            // Assert
            Assert.That(_section.GetProperty("prop1")?.Value, Is.EqualTo("value1"));
        }

        [Test]
        public void Clone_WithCircularReferences_HandlesCorrectly()
        {
            // Arrange
            var prop1 = new Property("key1", "value1");
            var prop2 = new Property("key2", "value2");
            _section.AddProperty(prop1);
            _section.AddProperty(prop2);

            // Act
            var clone = _section.Clone();
            clone.AddProperty("key3", "value3");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(2));
                Assert.That(clone.PropertyCount, Is.EqualTo(3));
            });
        }

        #endregion

        #region Dictionary Performance and Sync Tests (PERF-001)

        [Test]
        public void GetProperty_WithManyProperties_PerformsEfficiently()
        {
            // Arrange - Add 1000 properties
            for (int i = 0; i < 1000; i++)
            {
                _section.AddProperty($"key{i}", $"value{i}");
            }

            // Act - Lookup should be O(1), not O(n)
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var property = _section.GetProperty("key999");
            stopwatch.Stop();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(property, Is.Not.Null);
                Assert.That(property!.Value, Is.EqualTo("value999"));
                // Dictionary lookup should be very fast, even with 1000 properties
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10));
            });
        }

        [Test]
        public void GetProperty_CaseInsensitive_UsesDictionary()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var prop1 = _section.GetProperty("TESTKEY");
            var prop2 = _section.GetProperty("testkey");
            var prop3 = _section.GetProperty("TestKey");

            // Assert - All should return the same property instance
            Assert.Multiple(() =>
            {
                Assert.That(prop1, Is.Not.Null);
                Assert.That(prop2, Is.Not.Null);
                Assert.That(prop3, Is.Not.Null);
                Assert.That(prop1, Is.SameAs(prop2));
                Assert.That(prop2, Is.SameAs(prop3));
            });
        }

        [Test]
        public void AddProperty_UpdatesDictionaryAndList()
        {
            // Arrange & Act
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key2", "value2");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(2));
                Assert.That(_section.GetProperty("key1"), Is.Not.Null);
                Assert.That(_section.GetProperty("key2"), Is.Not.Null);
                Assert.That(_section[0].Name, Is.EqualTo("key1"));
                Assert.That(_section[1].Name, Is.EqualTo("key2"));
            });
        }

        [Test]
        public void RemoveProperty_UpdatesDictionaryAndList()
        {
            // Arrange
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key2", "value2");
            _section.AddProperty("key3", "value3");

            // Act
            var removed = _section.RemoveProperty("key2");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(_section.PropertyCount, Is.EqualTo(2));
                Assert.That(_section.GetProperty("key2"), Is.Null);
                Assert.That(_section.GetProperty("key1"), Is.Not.Null);
                Assert.That(_section.GetProperty("key3"), Is.Not.Null);
            });
        }

        [Test]
        public void RemovePropertyByIndex_UpdatesDictionaryAndList()
        {
            // Arrange
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key2", "value2");
            _section.AddProperty("key3", "value3");

            // Act
            var removed = _section.RemoveProperty(1); // Remove "key2"

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(_section.PropertyCount, Is.EqualTo(2));
                Assert.That(_section.GetProperty("key2"), Is.Null);
                Assert.That(_section.GetProperty("key1"), Is.Not.Null);
                Assert.That(_section.GetProperty("key3"), Is.Not.Null);
                Assert.That(_section[0].Name, Is.EqualTo("key1"));
                Assert.That(_section[1].Name, Is.EqualTo("key3"));
            });
        }

        [Test]
        public void InsertProperty_UpdatesDictionaryAndList()
        {
            // Arrange
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key3", "value3");

            // Act
            _section.InsertProperty(1, "key2", "value2");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(3));
                Assert.That(_section.GetProperty("key2"), Is.Not.Null);
                Assert.That(_section[0].Name, Is.EqualTo("key1"));
                Assert.That(_section[1].Name, Is.EqualTo("key2"));
                Assert.That(_section[2].Name, Is.EqualTo("key3"));
            });
        }

        [Test]
        public void Clear_UpdatesDictionaryAndList()
        {
            // Arrange
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key2", "value2");
            _section.AddProperty("key3", "value3");

            // Act
            _section.Clear();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.Zero);
                Assert.That(_section.GetProperty("key1"), Is.Null);
                Assert.That(_section.GetProperty("key2"), Is.Null);
                Assert.That(_section.GetProperty("key3"), Is.Null);
            });
        }

        [Test]
        public void HasProperty_UsesDictionaryLookup()
        {
            // Arrange
            for (int i = 0; i < 100; i++)
            {
                _section.AddProperty($"key{i}", $"value{i}");
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var exists = _section.HasProperty("key99");
            stopwatch.Stop();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(exists, Is.True);
                // Dictionary lookup should be very fast
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5));
            });
        }

        [Test]
        public void IndexerByString_AutoCreate_UpdatesDictionary()
        {
            // Act
            var property = _section["NewKey"];
            property.Value = "NewValue";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(1));
                Assert.That(_section.GetProperty("NewKey"), Is.Not.Null);
                Assert.That(_section.GetProperty("NewKey")!.Value, Is.EqualTo("NewValue"));
            });
        }

        [Test]
        public void MergeFromLastWin_UpdatesDictionary()
        {
            // Arrange
            _section.AddProperty("key1", "value1");
            _section.AddProperty("key2", "value2");

            var section2 = new Section("Section2");
            section2.AddProperty("key2", "newValue2");
            section2.AddProperty("key3", "value3");

            // Act
            _section.MergeFrom(section2, IniConfigOption.DuplicateKeyPolicyType.LastWin);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_section.PropertyCount, Is.EqualTo(3));
                Assert.That(_section.GetProperty("key1")!.Value, Is.EqualTo("value1"));
                Assert.That(_section.GetProperty("key2")!.Value, Is.EqualTo("newValue2"));
                Assert.That(_section.GetProperty("key3")!.Value, Is.EqualTo("value3"));
            });
        }

        [Test]
        public void TryGetProperty_ExistingProperty_ReturnsTrue()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var result = _section.TryGetProperty("TestKey", out var property);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(property, Is.Not.Null);
                Assert.That(property!.Value, Is.EqualTo("TestValue"));
            });
        }

        [Test]
        public void TryGetProperty_NonExistingProperty_ReturnsFalse()
        {
            // Act
            var result = _section.TryGetProperty("NonExistent", out var property);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(property, Is.Null);
            });
        }

        [Test]
        public void TryGetProperty_CaseInsensitive_ReturnsCorrectProperty()
        {
            // Arrange
            _section.AddProperty("TestKey", "TestValue");

            // Act
            var result1 = _section.TryGetProperty("TESTKEY", out var property1);
            var result2 = _section.TryGetProperty("testkey", out var property2);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result1, Is.True);
                Assert.That(result2, Is.True);
                Assert.That(property1, Is.SameAs(property2));
            });
        }

        #endregion

        #region MEDIUM Priority Tests - GetPropertyValue Helpers

        [Test]
        public void GetPropertyValue_ValidKey_ReturnsValue()
        {
            // Arrange
            _section.AddProperty("Port", "8080");

            // Act
            var value = _section.GetPropertyValue<int>("Port");

            // Assert
            Assert.That(value, Is.EqualTo(8080));
        }

        [Test]
        public void GetPropertyValue_NonExistentKey_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _section.GetPropertyValue<int>("NonExistent"));
            Assert.That(ex.Message, Does.Contain("NonExistent"));
        }

        [Test]
        public void GetPropertyValueOrDefault_ValidKey_ReturnsValue()
        {
            // Arrange
            _section.AddProperty("Timeout", "30");

            // Act
            var value = _section.GetPropertyValueOrDefault<int>("Timeout", 60);

            // Assert
            Assert.That(value, Is.EqualTo(30));
        }

        [Test]
        public void GetPropertyValueOrDefault_NonExistentKey_ReturnsDefault()
        {
            // Act
            var value = _section.GetPropertyValueOrDefault<int>("NonExistent", 60);

            // Assert
            Assert.That(value, Is.EqualTo(60));
        }

        [Test]
        public void GetPropertyValueOrDefault_InvalidFormat_ReturnsDefault()
        {
            // Arrange
            _section.AddProperty("Port", "invalid");

            // Act
            var value = _section.GetPropertyValueOrDefault<int>("Port", 80);

            // Assert
            Assert.That(value, Is.EqualTo(80));
        }

        [Test]
        public void TryGetPropertyValue_ValidKey_ReturnsTrue()
        {
            // Arrange
            _section.AddProperty("MaxConnections", "100");

            // Act
            var result = _section.TryGetPropertyValue<int>("MaxConnections", out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(100));
            });
        }

        [Test]
        public void TryGetPropertyValue_NonExistentKey_ReturnsFalse()
        {
            // Act
            var result = _section.TryGetPropertyValue<int>("NonExistent", out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(int)));
            });
        }

        [Test]
        public void TryGetPropertyValue_InvalidFormat_ReturnsFalse()
        {
            // Arrange
            _section.AddProperty("Port", "invalid");

            // Act
            var result = _section.TryGetPropertyValue<int>("Port", out var value);

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
        public void WithProperty_AddsProperty_ReturnsSection()
        {
            // Act
            var result = _section.WithProperty("Host", "localhost");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_section));
                Assert.That(_section.HasProperty("Host"), Is.True);
                Assert.That(_section["Host"].Value, Is.EqualTo("localhost"));
            });
        }

        [Test]
        public void WithPropertyTyped_AddsProperty_ReturnsSection()
        {
            // Act
            var result = _section.WithProperty<int>("Port", 8080);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_section));
                Assert.That(_section["Port"].Value, Is.EqualTo("8080"));
            });
        }

        [Test]
        public void WithComment_SetsComment_ReturnsSection()
        {
            // Act
            var result = _section.WithComment("Server configuration");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_section));
                Assert.That(_section.Comment, Is.Not.Null);
                Assert.That(_section.Comment!.Value, Is.EqualTo("Server configuration"));
            });
        }

        [Test]
        public void WithPreComment_AddsPreComment_ReturnsSection()
        {
            // Act
            var result = _section.WithPreComment("Important section");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_section));
                Assert.That(_section.PreComments, Has.Count.EqualTo(1));
                Assert.That(_section.PreComments[0].Value, Is.EqualTo("Important section"));
            });
        }

        [Test]
        public void FluentAPI_ChainMultipleOperations_WorksCorrectly()
        {
            // Act
            var result = new Section("Server")
                .WithProperty("Host", "localhost")
                .WithProperty<int>("Port", 8080)
                .WithProperty<bool>("SSL", true)
                .WithComment("Server settings")
                .WithPreComment("Production configuration");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.PropertyCount, Is.EqualTo(3));
                Assert.That(result["Host"].Value, Is.EqualTo("localhost"));
                Assert.That(result["Port"].Value, Is.EqualTo("8080"));
                Assert.That(result["SSL"].Value, Is.EqualTo("True"));
                Assert.That(result.Comment!.Value, Is.EqualTo("Server settings"));
                Assert.That(result.PreComments, Has.Count.EqualTo(1));
            });
        }

        #endregion
    }
}