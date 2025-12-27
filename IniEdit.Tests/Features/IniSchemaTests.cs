using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Features
{
    [TestFixture]
    public class IniSchemaTests
    {
        #region Basic Schema Definition Tests

        [Test]
        public void DefineSection_ValidName_CreatesDefinition()
        {
            // Arrange
            var schema = new IniSchema();

            // Act
            var sectionDef = schema.DefineSection("Database", true);

            // Assert
            Assert.IsNotNull(sectionDef);
            Assert.AreEqual("Database", sectionDef.Name);
            Assert.IsTrue(sectionDef.IsRequired);
            Assert.IsTrue(schema.Sections.ContainsKey("Database"));
        }

        [Test]
        public void DefineSection_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var schema = new IniSchema();

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => schema.DefineSection(""));
        }

        [Test]
        public void DefineProperty_InSection_CreatesDefinition()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Database");

            // Act
            var propDef = sectionDef.DefineProperty("Host", typeof(string), true);

            // Assert
            Assert.IsNotNull(propDef);
            Assert.AreEqual("Host", propDef.Key);
            Assert.AreEqual(typeof(string), propDef.ExpectedType);
            Assert.IsTrue(propDef.IsRequired);
        }

        [Test]
        public void DefineProperty_OnDefaultSection_CreatesDefinition()
        {
            // Arrange
            var schema = new IniSchema();

            // Act
            var propDef = schema.DefineProperty("GlobalSetting", typeof(int));

            // Assert
            Assert.IsNotNull(propDef);
            Assert.AreEqual("GlobalSetting", propDef.Key);
        }

        #endregion

        #region Validation Tests - Required Elements

        [Test]
        public void Validate_MissingRequiredSection_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Required", true);

            var doc = new Document();
            doc["Other"].AddProperty("Key", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.ErrorCount);
            Assert.AreEqual(SchemaErrorType.MissingRequiredSection, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_MissingRequiredProperty_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Database");
            sectionDef.DefineProperty("Host", typeof(string), true);

            var doc = new Document();
            doc["Database"].AddProperty("Port", "3306");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.MissingRequiredProperty, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_AllRequiredPresent_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Database", true);
            sectionDef.DefineProperty("Host", typeof(string), true);

            var doc = new Document();
            doc["Database"].AddProperty("Host", "localhost");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        #endregion

        #region Validation Tests - Type Checking

        [Test]
        public void Validate_InvalidIntType_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Port", typeof(int));

            var doc = new Document();
            doc["Settings"].AddProperty("Port", "not-a-number");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.TypeMismatch, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_ValidIntType_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Port", typeof(int));

            var doc = new Document();
            doc["Settings"].AddProperty("Port", "8080");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_BoolType_AcceptsVariousFormats()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Enabled", typeof(bool));

            var testCases = new[] { "true", "false", "1", "0", "yes", "no", "True", "False" };

            foreach (var value in testCases)
            {
                var doc = new Document();
                doc["Settings"].AddProperty("Enabled", value);

                // Act
                var result = schema.Validate(doc);

                // Assert
                Assert.IsTrue(result.IsValid, $"Failed for value: {value}");
            }
        }

        #endregion

        #region Validation Tests - Allowed Values

        [Test]
        public void Validate_ValueNotInAllowedList_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("LogLevel")
                .WithAllowedValues("DEBUG", "INFO", "WARN", "ERROR");

            var doc = new Document();
            doc["Settings"].AddProperty("LogLevel", "INVALID");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.ValueNotAllowed, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_ValueInAllowedList_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("LogLevel")
                .WithAllowedValues("DEBUG", "INFO", "WARN", "ERROR");

            var doc = new Document();
            doc["Settings"].AddProperty("LogLevel", "INFO");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        #endregion

        #region Validation Tests - Pattern Matching

        [Test]
        public void Validate_PatternMismatch_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Email")
                .WithPattern(@"^[\w\.-]+@[\w\.-]+\.\w+$");

            var doc = new Document();
            doc["Settings"].AddProperty("Email", "invalid-email");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.PatternMismatch, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_PatternMatch_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Email")
                .WithPattern(@"^[\w\.-]+@[\w\.-]+\.\w+$");

            var doc = new Document();
            doc["Settings"].AddProperty("Email", "test@example.com");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        #endregion

        #region Validation Tests - Range Checking

        [Test]
        public void Validate_ValueBelowMinimum_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Port", typeof(int))
                .WithRange(min: 1, max: 65535);

            var doc = new Document();
            doc["Settings"].AddProperty("Port", "0");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.ValueOutOfRange, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_ValueAboveMaximum_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Port", typeof(int))
                .WithRange(min: 1, max: 65535);

            var doc = new Document();
            doc["Settings"].AddProperty("Port", "99999");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.ValueOutOfRange, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_ValueInRange_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Port", typeof(int))
                .WithRange(min: 1, max: 65535);

            var doc = new Document();
            doc["Settings"].AddProperty("Port", "8080");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        #endregion

        #region Validation Tests - Custom Validators

        [Test]
        public void Validate_CustomValidatorFails_ReturnsError()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Password")
                .WithValidator(value =>
                    value.Length >= 8
                        ? IniValidationResult.Success()
                        : IniValidationResult.Error("Password must be at least 8 characters"));

            var doc = new Document();
            doc["Settings"].AddProperty("Password", "short");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.ValidationFailed, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_CustomValidatorPasses_ReturnsValid()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Password")
                .WithValidator(value =>
                    value.Length >= 8
                        ? IniValidationResult.Success()
                        : IniValidationResult.Error("Password must be at least 8 characters"));

            var doc = new Document();
            doc["Settings"].AddProperty("Password", "longenoughpassword");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        #endregion

        #region Validation Tests - Undefined Elements

        [Test]
        public void Validate_UndefinedSection_AllowedByDefault()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Known");

            var doc = new Document();
            doc["Known"].AddProperty("Key", "Value");
            doc["Unknown"].AddProperty("Key", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_UndefinedSection_DisallowedWhenConfigured()
        {
            // Arrange
            var schema = new IniSchema { AllowUndefinedSections = false };
            schema.DefineSection("Known");

            var doc = new Document();
            doc["Known"].AddProperty("Key", "Value");
            doc["Unknown"].AddProperty("Key", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.UndefinedSection, result.Errors[0].ErrorType);
        }

        [Test]
        public void Validate_UndefinedProperty_AllowedByDefault()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Known");

            var doc = new Document();
            doc["Settings"].AddProperty("Known", "Value");
            doc["Settings"].AddProperty("Unknown", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_UndefinedProperty_DisallowedWhenConfigured()
        {
            // Arrange
            var schema = new IniSchema { AllowUndefinedProperties = false };
            var sectionDef = schema.DefineSection("Settings");
            sectionDef.DefineProperty("Known");

            var doc = new Document();
            doc["Settings"].AddProperty("Known", "Value");
            doc["Settings"].AddProperty("Unknown", "Value");

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SchemaErrorType.UndefinedProperty, result.Errors[0].ErrorType);
        }

        #endregion

        #region Property Definition Fluent API Tests

        [Test]
        public void PropertyDefinition_FluentChaining_Works()
        {
            // Arrange
            var schema = new IniSchema();
            var sectionDef = schema.DefineSection("Settings");

            // Act
            var propDef = sectionDef.DefineProperty("Port", typeof(int), true)
                .WithDefault("8080")
                .WithDescription("Server port number")
                .WithRange(1, 65535)
                .WithAllowedValues("80", "443", "8080", "8443");

            // Assert
            Assert.AreEqual("8080", propDef.DefaultValue);
            Assert.AreEqual("Server port number", propDef.Description);
            Assert.AreEqual(1, propDef.MinValue);
            Assert.AreEqual(65535, propDef.MaxValue);
            Assert.IsNotNull(propDef.AllowedValues);
            Assert.IsTrue(propDef.AllowedValues.Contains("8080"));
        }

        #endregion

        #region Null and Edge Cases

        [Test]
        public void Validate_NullDocument_ThrowsArgumentNullException()
        {
            // Arrange
            var schema = new IniSchema();

            // Act & Assert
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => schema.Validate(null));
#pragma warning restore CS8625
        }

        [Test]
        public void Validate_EmptyDocument_ValidWhenNoRequirements()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Optional");

            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void SchemaValidationResult_ToString_ReturnsReadableOutput()
        {
            // Arrange
            var schema = new IniSchema();
            schema.DefineSection("Required", true);

            var doc = new Document();

            // Act
            var result = schema.Validate(doc);

            // Assert
            var output = result.ToString();
            Assert.IsTrue(output.Contains("error"));
            Assert.IsTrue(output.Contains("Required"));
        }

        #endregion
    }
}
