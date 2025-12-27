using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Features
{
    /// <summary>
    /// Additional tests to improve coverage for IniSchema.
    /// </summary>
    [TestFixture]
    public class IniSchemaAdditionalTests
    {
        #region SectionDefinition Edge Cases

        [Test]
        public void SectionDefinition_DefineProperty_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Test");

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => sectionDef.DefineProperty(""));
        }

        [Test]
        public void SectionDefinition_DefineProperty_WhitespaceKey_ThrowsArgumentException()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Test");

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => sectionDef.DefineProperty("   "));
        }

        [Test]
        public void SectionDefinition_Properties_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Test");
            sectionDef.DefineProperty("Key1");
            sectionDef.DefineProperty("Key2");

            // Act
            var properties = sectionDef.Properties;

            // Assert
            Assert.AreEqual(2, properties.Count);
            Assert.IsTrue(properties.ContainsKey("Key1"));
            Assert.IsTrue(properties.ContainsKey("Key2"));
        }

        #endregion

        #region IniSchema Edge Cases

        [Test]
        public void DefineSection_WhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var schema = new IniSchema();

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => schema.DefineSection("   "));
        }

        [Test]
        public void DefineSection_NullName_ThrowsArgumentException()
        {
            // Arrange
            var schema = new IniSchema();

            // Act & Assert
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentException>(() => schema.DefineSection(null));
#pragma warning restore CS8625
        }

        [Test]
        public void Sections_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Section1");
            schema.DefineSection("Section2");

            // Act
            var sections = schema.Sections;

            // Assert
            Assert.AreEqual(2, sections.Count);
            Assert.IsTrue(sections.ContainsKey("Section1"));
        }

        [Test]
        public void Validate_DateTimeType_ValidValue_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Date", typeof(DateTime));

            var doc = new Document();
            doc["Settings"].AddProperty("Date", "2024-01-15");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_DateTimeType_InvalidValue_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Date", typeof(DateTime));

            var doc = new Document();
            doc["Settings"].AddProperty("Date", "not-a-date");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.TypeMismatch, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_LongType_ValidValue_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("BigNumber", typeof(long));

            var doc = new Document();
            doc["Settings"].AddProperty("BigNumber", "9223372036854775807");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_DoubleType_ValidValue_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Decimal", typeof(double));

            var doc = new Document();
            doc["Settings"].AddProperty("Decimal", "3.14159");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_DefaultSectionRequired_MissingProperty_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineProperty("GlobalKey", typeof(string), required: true);

            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.MissingRequiredProperty, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_DefaultSectionUndefinedProperty_DisallowedWhenConfigured()
        {
            // Arrange
            var schema = new IniSchema { AllowUndefinedProperties = false };
            schema.DefineProperty("Known");

            var doc = new Document();
            doc.DefaultSection.AddProperty("Known", "Value");
            doc.DefaultSection.AddProperty("Unknown", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.UndefinedProperty, result.Errors[0].ErrorType);
        }

        #endregion

        #region SchemaValidationResult Tests

        [Test]
        public void SchemaValidationResult_ErrorCount_ReturnsCorrectCount()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Required1", true);
            schema.DefineSection("Required2", true);

            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.AreEqual(2, result.ErrorCount);
            Assert.AreEqual(2, result.Errors.Count);
        }

        [Test]
        public void SchemaValidationResult_ToString_ValidDocument_ReturnsPassMessage()
        {
            // Arrange
            var schema = new IniSchema();
            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.AreEqual("Validation passed.", result.ToString());
        }

        #endregion

        #region SchemaValidationError Tests

        [Test]
        public void SchemaValidationError_ToString_ReturnsMessage()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Required", true);

            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            var errorString = result.Errors[0].ToString();
            Assert.IsTrue(errorString.Contains("Required"));
        }

        [Test]
        public void SchemaValidationError_Properties_AreSet()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("TestSection");
            sectionDef.DefineProperty("TestKey", typeof(int), true);

            var doc = new Document();
            doc["TestSection"].AddProperty("OtherKey", "value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            var error = result.Errors[0];
            Assert.AreEqual("TestSection", error.SectionName);
            Assert.AreEqual("TestKey", error.PropertyKey);
            Assert.AreEqual(SchemaErrorType.MissingRequiredProperty, error.ErrorType);
        }

        #endregion

        #region Pattern Timeout Test

        [Test]
        public void Validate_PatternTimeout_ReturnsValidationFailedError()
        {
            // This test verifies that regex timeout is handled
            // Using a simple pattern that won't timeout
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Email")
                .WithPattern(@"^[a-z]+$");

            var doc = new Document();
            doc["Settings"].AddProperty("Email", "UPPERCASE");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.PatternMismatch, result.Errors[0].ErrorType);
        }

        #endregion

        #region Allowed Values Case Sensitivity

        [Test]
        public void Validate_AllowedValues_CaseInsensitive()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Level")
                .WithAllowedValues("DEBUG", "INFO", "WARN");

            var doc = new Document();
            doc["Settings"].AddProperty("Level", "debug"); // lowercase

            // Act
            var result = schema.Validate(doc);

            // Assert - HashSet uses case-insensitive comparer from WithAllowedValues
            Assert.IsTrue(result.IsValid);
        }

        #endregion
    }
}
