using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Core
{
    /// <summary>
    /// Tests for new features added in v2.0
    /// </summary>
    [TestFixture]
    public class NewFeatureTests
    {
        #region TryGet Pattern Tests

        [Test]
        public void Document_TryGetSection_ExistingSection_ReturnsTrue()
        {
            // Arrange
            var doc = new Document();
            doc["TestSection"].AddProperty("Key", "Value");

            // Act
            var result = doc.TryGetSection("TestSection", out var section);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(section);
            Assert.AreEqual("TestSection", section!.Name);
        }

        [Test]
        public void Document_TryGetSection_NonExistingSection_ReturnsFalse()
        {
            // Arrange
            var doc = new Document();

            // Act
            var result = doc.TryGetSection("NonExistent", out var section);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(section);
        }

        [Test]
        public void Document_TryGetSection_CaseInsensitive_Works()
        {
            // Arrange
            var doc = new Document();
            doc["TestSection"].AddProperty("Key", "Value");

            // Act
            var result = doc.TryGetSection("testsection", out var section);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(section);
        }

        [Test]
        public void Section_TryGetProperty_ExistingProperty_ReturnsTrue()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key", "Value");

            // Act
            var result = section.TryGetProperty("Key", out var property);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(property);
            Assert.AreEqual("Key", property!.Name);
            Assert.AreEqual("Value", property.Value);
        }

        [Test]
        public void Section_TryGetProperty_NonExistingProperty_ReturnsFalse()
        {
            // Arrange
            var section = new Section("Test");

            // Act
            var result = section.TryGetProperty("NonExistent", out var property);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(property);
        }

        [Test]
        public void Section_TryGetProperty_CaseInsensitive_Works()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key", "Value");

            // Act
            var result = section.TryGetProperty("key", out var property);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(property);
        }

        #endregion

        #region SetProperty Tests

        [Test]
        public void Section_SetProperty_NewProperty_AddsProperty()
        {
            // Arrange
            var section = new Section("Test");

            // Act
            section.SetProperty("NewKey", "NewValue");

            // Assert
            Assert.IsTrue(section.HasProperty("NewKey"));
            Assert.AreEqual("NewValue", section["NewKey"].Value);
        }

        [Test]
        public void Section_SetProperty_ExistingProperty_UpdatesValue()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key", "OldValue");

            // Act
            section.SetProperty("Key", "NewValue");

            // Assert
            Assert.AreEqual(1, section.PropertyCount);
            Assert.AreEqual("NewValue", section["Key"].Value);
        }

        [Test]
        public void Section_SetPropertyGeneric_NewProperty_AddsProperty()
        {
            // Arrange
            var section = new Section("Test");

            // Act
            section.SetProperty("Port", 8080);

            // Assert
            Assert.IsTrue(section.HasProperty("Port"));
            Assert.AreEqual("8080", section["Port"].Value);
        }

        [Test]
        public void Section_SetPropertyGeneric_ExistingProperty_UpdatesValue()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Port", "5432");

            // Act
            section.SetProperty("Port", 8080);

            // Assert
            Assert.AreEqual(1, section.PropertyCount);
            Assert.AreEqual("8080", section["Port"].Value);
        }

        [Test]
        public void Section_SetProperty_CaseInsensitive_UpdatesExisting()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key", "OldValue");

            // Act
            section.SetProperty("key", "NewValue");

            // Assert
            Assert.AreEqual(1, section.PropertyCount);
            Assert.AreEqual("NewValue", section["Key"].Value);
        }

        #endregion

        #region GetValueOrDefault Tests

        [Test]
        public void Property_GetValueOrDefault_ValidValue_ReturnsValue()
        {
            // Arrange
            var property = new Property("Port", "5432");

            // Act
            var result = property.GetValueOrDefault(8080);

            // Assert
            Assert.AreEqual(5432, result);
        }

        [Test]
        public void Property_GetValueOrDefault_InvalidValue_ReturnsDefault()
        {
            // Arrange
            var property = new Property("Port", "invalid");

            // Act
            var result = property.GetValueOrDefault(8080);

            // Assert
            Assert.AreEqual(8080, result);
        }

        [Test]
        public void Property_GetValueOrDefault_EmptyValue_ReturnsDefault()
        {
            // Arrange
            var property = new Property("Port", "");

            // Act
            var result = property.GetValueOrDefault(8080);

            // Assert
            Assert.AreEqual(8080, result);
        }

        [Test]
        public void Property_GetValueOrDefault_String_ReturnsValue()
        {
            // Arrange
            var property = new Property("Host", "localhost");

            // Act
            var result = property.GetValueOrDefault("default-host");

            // Assert
            Assert.AreEqual("localhost", result);
        }

        [Test]
        public void Property_GetValueOrDefault_Boolean_Works()
        {
            // Arrange
            var property = new Property("Enabled", "true");

            // Act
            var result = property.GetValueOrDefault(false);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Property_GetValueOrDefault_Double_Works()
        {
            // Arrange
            var property = new Property("Ratio", "1.5");

            // Act
            var result = property.GetValueOrDefault(0.0);

            // Assert
            Assert.AreEqual(1.5, result);
        }

        #endregion

        #region Property.Clone IsQuoted Tests

        [Test]
        public void Property_Clone_PreservesIsQuoted_True()
        {
            // Arrange
            var original = new Property("Key", "Value");
            original.IsQuoted = true;

            // Act
            var clone = original.Clone();

            // Assert
            Assert.IsTrue(clone.IsQuoted);
        }

        [Test]
        public void Property_Clone_PreservesIsQuoted_False()
        {
            // Arrange
            var original = new Property("Key", "Value");
            original.IsQuoted = false;

            // Act
            var clone = original.Clone();

            // Assert
            Assert.IsFalse(clone.IsQuoted);
        }

        [Test]
        public void Property_Clone_PreservesAllProperties()
        {
            // Arrange
            var original = new Property("Key", "Value");
            original.IsQuoted = true;
            original.PreComments.Add(new Comment("Comment"));
            original.Comment = new Comment("Inline");

            // Act
            var clone = original.Clone();

            // Assert
            Assert.AreEqual("Key", clone.Name);
            Assert.AreEqual("Value", clone.Value);
            Assert.IsTrue(clone.IsQuoted);
            Assert.AreEqual(1, clone.PreComments.Count);
            Assert.AreEqual("Comment", clone.PreComments[0].Value);
            Assert.AreEqual("Inline", clone.Comment!.Value);
        }

        #endregion

        #region Parsing Error Collection Tests

        [Test]
        public void Document_ParsingErrors_InitiallyEmpty()
        {
            // Arrange & Act
            var doc = new Document();

            // Assert
            Assert.AreEqual(0, doc.ParsingErrors.Count);
        }

        [Test]
        public void IniConfigOption_CollectParsingErrors_DefaultFalse()
        {
            // Arrange & Act
            var option = new IniConfigOption();

            // Assert
            Assert.IsFalse(option.CollectParsingErrors);
        }

        [Test]
        public void IniConfigOption_CollectParsingErrors_CanSet()
        {
            // Arrange
            var option = new IniConfigOption();

            // Act
            option.CollectParsingErrors = true;

            // Assert
            Assert.IsTrue(option.CollectParsingErrors);
        }

        #endregion

        #region Case-Sensitivity Fix Tests

        [Test]
        public void Section_HasProperty_CaseInsensitive()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("TestKey", "Value");

            // Act & Assert
            Assert.IsTrue(section.HasProperty("testkey"));
            Assert.IsTrue(section.HasProperty("TESTKEY"));
            Assert.IsTrue(section.HasProperty("TestKey"));
        }

        [Test]
        public void Section_InsertProperty_CaseInsensitive()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Value1");
            section.AddProperty("Key2", "Value2");

            // Act
            section.InsertProperty("key1", "InsertedKey", "InsertedValue");

            // Assert
            Assert.AreEqual("InsertedKey", section[0].Name);
            Assert.AreEqual("Key1", section[1].Name);
        }

        [Test]
        public void Document_HasSection_CaseInsensitive()
        {
            // Arrange
            var doc = new Document();
            doc["TestSection"].AddProperty("Key", "Value");

            // Act & Assert
            Assert.IsTrue(doc.HasSection("testsection"));
            Assert.IsTrue(doc.HasSection("TESTSECTION"));
            Assert.IsTrue(doc.HasSection("TestSection"));
        }

        #endregion

        #region LoadOptions Tests

        [Test]
        public void LoadOptions_DefaultFileShare_IsRead()
        {
            // Arrange & Act
            var options = new LoadOptions();

            // Assert
            Assert.AreEqual(FileShare.Read, options.FileShare);
        }

        [Test]
        public void LoadOptions_CanSetFileShare()
        {
            // Arrange
            var options = new LoadOptions();

            // Act
            options.FileShare = FileShare.ReadWrite;

            // Assert
            Assert.AreEqual(FileShare.ReadWrite, options.FileShare);
        }

        [Test]
        public void LoadOptions_CanSetConfigOption()
        {
            // Arrange
            var options = new LoadOptions();
            var configOption = new IniConfigOption();

            // Act
            options.ConfigOption = configOption;

            // Assert
            Assert.AreEqual(configOption, options.ConfigOption);
        }

        [Test]
        public void LoadOptions_CanSetSectionFilter()
        {
            // Arrange
            var options = new LoadOptions();
            Func<string, bool> filter = name => name.StartsWith("App");

            // Act
            options.SectionFilter = filter;

            // Assert
            Assert.AreEqual(filter, options.SectionFilter);
        }

        #endregion
    }
}
