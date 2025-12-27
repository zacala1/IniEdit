using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Extensions
{
    [TestFixture]
    public class EnvironmentVariableTests
    {
        private const string TestVarName = "MARRONCONFIG_TEST_VAR";
        private const string TestVarValue = "TestValue123";

        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable(TestVarName, TestVarValue);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(TestVarName, null);
        }

        [Test]
        public void SubstituteEnvironmentVariables_Document_SubstitutesAllSections()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", $"${{{TestVarName}}}");
            doc["Section2"].AddProperty("Key2", $"%{TestVarName}%");

            // Act
            doc.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, doc["Section1"]["Key1"].Value);
            Assert.AreEqual(TestVarValue, doc["Section2"]["Key2"].Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_Document_SubstitutesDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Key", $"${{{TestVarName}}}");

            // Act
            doc.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, doc.DefaultSection["Key"].Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_Section_SubstitutesAllProperties()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", $"${{{TestVarName}}}");
            section.AddProperty("Key2", $"prefix-${{{TestVarName}}}-suffix");

            // Act
            section.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, section["Key1"].Value);
            Assert.AreEqual($"prefix-{TestVarValue}-suffix", section["Key2"].Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_Property_Substitutes()
        {
            // Arrange
            var property = new Property("Key", $"${{{TestVarName}}}");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariablesInValue_ReturnsSubstitutedString()
        {
            // Arrange
            var value = $"Path: ${{{TestVarName}}}/subdir";

            // Act
            var result = EnvironmentVariableExtensions.SubstituteEnvironmentVariablesInValue(value);

            // Assert
            Assert.AreEqual($"Path: {TestVarValue}/subdir", result);
        }

        [Test]
        public void SubstituteEnvironmentVariables_WindowsStyle_Works()
        {
            // Arrange
            var property = new Property("Key", $"%{TestVarName}%");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_UnixStyle_Works()
        {
            // Arrange
            var property = new Property("Key", $"${{{TestVarName}}}");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual(TestVarValue, property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_MixedStyles_WorksTogether()
        {
            // Arrange
            var property = new Property("Key", $"${{{TestVarName}}} and %{TestVarName}%");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual($"{TestVarValue} and {TestVarValue}", property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_NonExistentVariable_KeepsOriginal()
        {
            // Arrange
            var property = new Property("Key", "${NONEXISTENT_VAR_12345}");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual("${NONEXISTENT_VAR_12345}", property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_EmptyValue_ReturnsEmpty()
        {
            // Arrange
            var property = new Property("Key", "");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual("", property.Value);
        }

        [Test]
        public void SubstituteEnvironmentVariables_NoVariables_RemainsUnchanged()
        {
            // Arrange
            var property = new Property("Key", "Just a regular value");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual("Just a regular value", property.Value);
        }

        [Test]
        public void TrySubstituteEnvironmentVariables_WithSubstitution_ReturnsTrue()
        {
            // Arrange
            var property = new Property("Key", $"${{{TestVarName}}}");

            // Act
            var result = property.TrySubstituteEnvironmentVariables(out string substituted);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(TestVarValue, substituted);
        }

        [Test]
        public void TrySubstituteEnvironmentVariables_WithoutSubstitution_ReturnsFalse()
        {
            // Arrange
            var property = new Property("Key", "Regular value");

            // Act
            var result = property.TrySubstituteEnvironmentVariables(out string substituted);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual("Regular value", substituted);
        }

        [Test]
        public void TrySubstituteEnvironmentVariables_NonExistentVariable_ReturnsFalse()
        {
            // Arrange
            var property = new Property("Key", "${NONEXISTENT_VAR}");

            // Act
            var result = property.TrySubstituteEnvironmentVariables(out string substituted);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual("${NONEXISTENT_VAR}", substituted);
        }

        [Test]
        public void SubstituteEnvironmentVariables_MultipleVariables_SubstitutesAll()
        {
            // Arrange
            Environment.SetEnvironmentVariable("VAR1", "Value1");
            Environment.SetEnvironmentVariable("VAR2", "Value2");

            var property = new Property("Key", "${VAR1}/path/${VAR2}");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.AreEqual("Value1/path/Value2", property.Value);

            // Cleanup
            Environment.SetEnvironmentVariable("VAR1", null);
            Environment.SetEnvironmentVariable("VAR2", null);
        }

        [Test]
        public void SubstituteEnvironmentVariables_RealWorldExample_TEMP()
        {
            // Arrange
            var tempPath = Environment.GetEnvironmentVariable("TEMP")
                          ?? Environment.GetEnvironmentVariable("TMP");

            if (tempPath == null)
            {
                Assert.Ignore("TEMP/TMP environment variable not available");
                return;
            }

            var property = new Property("LogPath", "${TEMP}/app.log");

            // Act
            property.SubstituteEnvironmentVariables();

            // Assert
            Assert.IsTrue(property.Value.EndsWith("/app.log"));
            Assert.IsFalse(property.Value.Contains("${TEMP}"));
        }

        [Test]
        public void SubstituteEnvironmentVariables_Document_ThrowsOnNull()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc!.SubstituteEnvironmentVariables());
        }

        [Test]
        public void SubstituteEnvironmentVariables_Section_ThrowsOnNull()
        {
            // Arrange
            Section? section = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => section!.SubstituteEnvironmentVariables());
        }

        [Test]
        public void SubstituteEnvironmentVariables_Property_ThrowsOnNull()
        {
            // Arrange
            Property? property = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => property!.SubstituteEnvironmentVariables());
        }

        [Test]
        public void TrySubstituteEnvironmentVariables_ThrowsOnNull()
        {
            // Arrange
            Property? property = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                property!.TrySubstituteEnvironmentVariables(out _));
        }
    }
}
