using System.Text;

namespace IniEdit.Tests.Parsing
{
    [TestFixture]
    public class SaveOptionsTests
    {
        #region KeyValueSeparator Tests

        [Test]
        public void Save_WithDefaultSeparator_UsesSpacedEquals()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            // Act
            var result = SaveToString(doc, SaveOptions.Default);

            // Assert
            Assert.That(result, Does.Contain("Key = Value"));
        }

        [Test]
        public void Save_WithCompactSeparator_UsesNoSpaces()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var options = new SaveOptions { KeyValueSeparator = "=" };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("Key=Value"));
        }

        [Test]
        public void Save_WithColonSeparator_UsesColon()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var options = new SaveOptions { KeyValueSeparator = ": " };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("Key: Value"));
        }

        #endregion

        #region BlankLinesBetweenSections Tests

        [Test]
        public void Save_WithZeroBlankLines_NoBlankLinesBetweenSections()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section2"].AddProperty("Key2", "Value2");
            var options = new SaveOptions { BlankLinesBetweenSections = 0 };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("[Section1]\r\nKey1 = Value1\r\n[Section2]").Or.Contain("[Section1]\nKey1 = Value1\n[Section2]"));
        }

        [Test]
        public void Save_WithTwoBlankLines_TwoBlankLinesBetweenSections()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section2"].AddProperty("Key2", "Value2");
            var options = new SaveOptions { BlankLinesBetweenSections = 2 };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            // Should have 3 newlines between sections (end of section1 + 2 blank lines)
            Assert.That(result, Does.Contain("Value1\r\n\r\n\r\n[Section2]").Or.Contain("Value1\n\n\n[Section2]"));
        }

        #endregion

        #region NormalizeCommentPrefix Tests

        [Test]
        public void Save_WithNormalizeCommentPrefix_NormalizesToDefault()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Test");
            section.PreComments.Add(new Comment("#", " Hash comment")); // Note: space is part of value
            doc.AddSection(section);
            section.AddProperty("Key", "Value");

            var options = new SaveOptions { NormalizeCommentPrefix = true };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("; Hash comment")); // Normalized to semicolon
        }

        [Test]
        public void Save_WithoutNormalizeCommentPrefix_PreservesOriginal()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Test");
            section.PreComments.Add(new Comment("#", " Hash comment")); // Note: space is part of value
            doc.AddSection(section);
            section.AddProperty("Key", "Value");

            var options = new SaveOptions { NormalizeCommentPrefix = false };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("# Hash comment")); // Preserved original
        }

        #endregion

        #region SpaceBeforeInlineComment Tests

        [Test]
        public void Save_WithSpaceBeforeInlineComment_AddsSpace()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Test");
            section.Comment = new Comment("inline");
            doc.AddSection(section);
            section.AddProperty("Key", "Value");

            var options = new SaveOptions { SpaceBeforeInlineComment = true };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("[Test] ;inline"));
        }

        [Test]
        public void Save_WithoutSpaceBeforeInlineComment_NoSpace()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Test");
            section.Comment = new Comment("inline");
            doc.AddSection(section);
            section.AddProperty("Key", "Value");

            var options = new SaveOptions { SpaceBeforeInlineComment = false };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("[Test];inline"));
        }

        #endregion

        #region BlankLineAfterDefaultSection Tests

        [Test]
        public void Save_WithBlankLineAfterDefaultSection_AddsBlankLine()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("GlobalKey", "GlobalValue");
            doc["Section"].AddProperty("Key", "Value");

            var options = new SaveOptions { BlankLineAfterDefaultSection = true };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("GlobalValue\r\n\r\n[Section]").Or.Contain("GlobalValue\n\n[Section]"));
        }

        [Test]
        public void Save_WithoutBlankLineAfterDefaultSection_NoBlankLine()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("GlobalKey", "GlobalValue");
            doc["Section"].AddProperty("Key", "Value");

            var options = new SaveOptions { BlankLineAfterDefaultSection = false };

            // Act
            var result = SaveToString(doc, options);

            // Assert
            Assert.That(result, Does.Contain("GlobalValue\r\n[Section]").Or.Contain("GlobalValue\n[Section]"));
        }

        #endregion

        #region Clone Tests

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new SaveOptions
            {
                KeyValueSeparator = "=",
                BlankLinesBetweenSections = 2,
                NormalizeCommentPrefix = true
            };

            // Act
            var clone = original.Clone();
            clone.KeyValueSeparator = ": ";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(original.KeyValueSeparator, Is.EqualTo("="));
                Assert.That(clone.KeyValueSeparator, Is.EqualTo(": "));
            });
        }

        #endregion

        #region Validation Tests

        [Test]
        public void KeyValueSeparator_Null_ThrowsArgumentNullException()
        {
            // Arrange
            var options = new SaveOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.KeyValueSeparator = null!);
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void BlankLinesBetweenSections_Negative_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new SaveOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.BlankLinesBetweenSections = -1);
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void BlankLinesBetweenSections_ExceedsMax_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new SaveOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.BlankLinesBetweenSections = SaveOptions.MaxBlankLinesBetweenSections + 1);
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void BlankLinesBetweenSections_MaxValue_Succeeds()
        {
            // Arrange
            var options = new SaveOptions();

            // Act
            options.BlankLinesBetweenSections = SaveOptions.MaxBlankLinesBetweenSections;

            // Assert
            Assert.That(options.BlankLinesBetweenSections, Is.EqualTo(SaveOptions.MaxBlankLinesBetweenSections));
        }

        #endregion

        #region FrozenSaveOptions (Default) Tests

        [Test]
        public void Default_ModifyKeyValueSeparator_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SaveOptions.Default.KeyValueSeparator = "=");
        }

        [Test]
        public void Default_ModifyBlankLinesBetweenSections_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SaveOptions.Default.BlankLinesBetweenSections = 0);
        }

        [Test]
        public void Default_ModifyBlankLineAfterDefaultSection_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SaveOptions.Default.BlankLineAfterDefaultSection = false);
        }

        [Test]
        public void Default_ModifyNormalizeCommentPrefix_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SaveOptions.Default.NormalizeCommentPrefix = true);
        }

        [Test]
        public void Default_ModifySpaceBeforeInlineComment_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SaveOptions.Default.SpaceBeforeInlineComment = false);
        }

        [Test]
        public void Default_ReadProperties_ReturnsDefaultValues()
        {
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(SaveOptions.Default.KeyValueSeparator, Is.EqualTo(" = "));
                Assert.That(SaveOptions.Default.BlankLinesBetweenSections, Is.EqualTo(1));
                Assert.That(SaveOptions.Default.BlankLineAfterDefaultSection, Is.True);
                Assert.That(SaveOptions.Default.NormalizeCommentPrefix, Is.False);
                Assert.That(SaveOptions.Default.SpaceBeforeInlineComment, Is.True);
            });
        }

        #endregion

        #region Helper Methods

        private static string SaveToString(Document doc, SaveOptions options)
        {
            using var stream = new MemoryStream();
            IniConfigManager.Save(stream, Encoding.UTF8, doc, options);
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        #endregion
    }
}
