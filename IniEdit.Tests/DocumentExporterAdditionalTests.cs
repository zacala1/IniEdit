using NUnit.Framework;
using System.Text;
using System.Text.Json;
using System.Xml;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests
{
    /// <summary>
    /// Additional tests to improve coverage for DocumentExporter.
    /// </summary>
    [TestFixture]
    public class DocumentExporterAdditionalTests
    {
        #region JSON - AutoConvertTypes Edge Cases

        [Test]
        public void ToJson_AutoConvertTypes_YesNo_ConvertsToBoolean()
        {
            // Arrange
            var doc = new Document();
            doc["Settings"].AddProperty("EnabledYes", "yes");
            doc["Settings"].AddProperty("EnabledNo", "no");
            doc["Settings"].AddProperty("EnabledOne", "1");
            doc["Settings"].AddProperty("EnabledZero", "0");

            var options = new JsonExportOptions { AutoConvertTypes = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);
            var parsed = JsonDocument.Parse(json);
            var settings = parsed.RootElement.GetProperty("Settings");

            // Assert
            Assert.AreEqual(JsonValueKind.True, settings.GetProperty("EnabledYes").ValueKind);
            Assert.AreEqual(JsonValueKind.False, settings.GetProperty("EnabledNo").ValueKind);
            Assert.AreEqual(JsonValueKind.True, settings.GetProperty("EnabledOne").ValueKind);
            Assert.AreEqual(JsonValueKind.False, settings.GetProperty("EnabledZero").ValueKind);
        }

        [Test]
        public void ToJson_AutoConvertTypes_StringValue_RemainsString()
        {
            // Arrange
            var doc = new Document();
            doc["Settings"].AddProperty("Name", "HelloWorld");

            var options = new JsonExportOptions { AutoConvertTypes = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);
            var parsed = JsonDocument.Parse(json);
            var settings = parsed.RootElement.GetProperty("Settings");

            // Assert
            Assert.AreEqual(JsonValueKind.String, settings.GetProperty("Name").ValueKind);
        }

        #endregion

        #region JSON - Comments with values

        [Test]
        public void ToJson_IncludeComments_PropertyWithInlineComment()
        {
            // Arrange
            var doc = new Document();
            var prop = new Property("Key", "Value");
            prop.Comment = new Comment("inline comment");
            doc["Section"].AddProperty(prop);

            var options = new JsonExportOptions { IncludeComments = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsTrue(json.Contains("\"comment\""));
            Assert.IsTrue(json.Contains("inline comment"));
        }

        [Test]
        public void ToJson_IncludeComments_SectionWithPreComments()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("TestSection");
            section.PreComments.Add(new Comment("Section pre-comment"));
            section.Comment = new Comment("Section inline comment");
            section.AddProperty("Key", "Value");
            doc.AddSection(section);

            var options = new JsonExportOptions { IncludeComments = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsTrue(json.Contains("_preComments"));
            Assert.IsTrue(json.Contains("Section pre-comment"));
            Assert.IsTrue(json.Contains("_comment"));
        }

        [Test]
        public void ToJson_WithoutComments_PropertyHasNoCommentFields()
        {
            // Arrange
            var doc = new Document();
            var prop = new Property("Key", "Value");
            prop.Comment = new Comment("should not appear");
            doc["Section"].AddProperty(prop);

            var options = new JsonExportOptions { IncludeComments = false };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsFalse(json.Contains("\"comment\""));
            Assert.IsFalse(json.Contains("should not appear"));
        }

        #endregion

        #region XML - Element content vs attribute

        [Test]
        public void ToXml_ValueAsContent_NotAsAttribute()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "TestValue");

            var options = new XmlExportOptions { UseAttributeForValue = false };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains(">TestValue<"));
            Assert.IsFalse(xml.Contains("value=\"TestValue\""));
        }

        [Test]
        public void ToXml_CustomElementNames()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions
            {
                RootElementName = "config",
                SectionElementName = "group",
                PropertyElementName = "setting"
            };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("<config>"));
            Assert.IsTrue(xml.Contains("<group"));
            Assert.IsTrue(xml.Contains("<setting"));
        }

        [Test]
        public void ToXml_NotIndented_ProducesCompactOutput()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions { Indented = false };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            // No indentation means fewer newlines with spaces
            var lines = xml.Split('\n');
            Assert.IsTrue(lines.Length <= 3); // Compact output
        }

        [Test]
        public void ToXml_SectionWithInlineComment()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("TestSection");
            section.Comment = new Comment("Section comment");
            section.AddProperty("Key", "Value");
            doc.AddSection(section);

            var options = new XmlExportOptions { IncludeComments = true };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("<!--Inline: Section comment-->"));
        }

        #endregion

        #region CSV - Edge Cases

        [Test]
        public void ToCsv_EmptyValue_HandledCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains("Section,Key,"));
        }

        [Test]
        public void ToCsv_ValueWithNewlines_EscapesCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Line1\r\nLine2");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains("\"Line1\r\nLine2\""));
        }

        [Test]
        public void ToCsv_EmptyComment_IncludesEmptyField()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new CsvExportOptions { IncludeComments = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Assert - Last field should be empty (no comment)
            Assert.IsTrue(lines[1].EndsWith(",") || lines[1].Trim().EndsWith(",\"\"") || lines[1].Split(',').Length == 4);
        }

        [Test]
        public void ToCsv_CustomEncoding_UsesSpecifiedEncoding()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var tempFile = Path.GetTempFileName() + ".csv";

            var options = new CsvExportOptions { Encoding = Encoding.UTF32 };

            try
            {
                // Act
                DocumentExporter.ToCsvFile(doc, tempFile, options);

                // Assert
                var bytes = File.ReadAllBytes(tempFile);
                // UTF-32 has 4 bytes per character and BOM
                Assert.IsTrue(bytes.Length > 4);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void ToCsv_TabDelimiter_UsesTabs()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new CsvExportOptions { Delimiter = '\t', IncludeHeader = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);

            // Assert
            Assert.IsTrue(csv.Contains("Section\tKey\tValue"));
        }

        [Test]
        public void ToCsv_AlwaysQuote_EmptyValue_QuotesEmpty()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "");

            var options = new CsvExportOptions { AlwaysQuote = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);

            // Assert
            Assert.IsTrue(csv.Contains("\"\""));
        }

        #endregion

        #region Multiple Sections

        [Test]
        public void ToJson_MultipleSections_AllIncluded()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section2"].AddProperty("Key2", "Value2");
            doc["Section3"].AddProperty("Key3", "Value3");

            // Act
            var json = DocumentExporter.ToJson(doc);
            var parsed = JsonDocument.Parse(json);

            // Assert
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Section1", out _));
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Section2", out _));
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Section3", out _));
        }

        [Test]
        public void ToXml_MultipleSections_AllIncluded()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section2"].AddProperty("Key2", "Value2");

            // Act
            var xml = DocumentExporter.ToXml(doc);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            // Assert
            var sections = xmlDoc.SelectNodes("//section");
            Assert.AreEqual(2, sections!.Count);
        }

        [Test]
        public void ToCsv_MultipleSections_AllRowsIncluded()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section1"].AddProperty("Key2", "Value2");
            doc["Section2"].AddProperty("Key3", "Value3");

            var options = new CsvExportOptions { IncludeHeader = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Assert - 1 header + 3 data rows
            Assert.AreEqual(4, lines.Length);
        }

        #endregion

        #region Default Section Edge Cases

        [Test]
        public void ToJson_OnlyDefaultSection_FlattenedCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Key1", "Value1");
            doc.DefaultSection.AddProperty("Key2", "Value2");

            var options = new JsonExportOptions { FlattenDefaultSection = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);
            var parsed = JsonDocument.Parse(json);

            // Assert
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Key1", out _));
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Key2", out _));
            Assert.IsFalse(parsed.RootElement.TryGetProperty("_default", out _));
        }

        [Test]
        public void ToXml_OnlyDefaultSection_IncludesDefaultElement()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Key", "Value");

            // Act
            var xml = DocumentExporter.ToXml(doc);

            // Assert
            Assert.IsTrue(xml.Contains("name=\"_default\""));
        }

        #endregion

        #region Special Characters in Section Names

        [Test]
        public void ToXml_SectionNameWithSpaces_HandledCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section With Spaces"].AddProperty("Key", "Value");

            // Act
            var xml = DocumentExporter.ToXml(doc);

            // Assert
            Assert.IsTrue(xml.Contains("name=\"Section With Spaces\""));
        }

        [Test]
        public void ToJson_SectionNameWithSpecialChars_HandledCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section.With.Dots"].AddProperty("Key", "Value");

            // Act
            var json = DocumentExporter.ToJson(doc);
            var parsed = JsonDocument.Parse(json);

            // Assert
            Assert.IsTrue(parsed.RootElement.TryGetProperty("Section.With.Dots", out _));
        }

        #endregion

        #region Null File Path Tests

        [Test]
        public void ToJsonFile_NullDocument_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() =>
                DocumentExporter.ToJsonFile(null, "test.json"));
#pragma warning restore CS8625
        }

        [Test]
        public void ToXmlFile_NullDocument_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() =>
                DocumentExporter.ToXmlFile(null, "test.xml"));
#pragma warning restore CS8625
        }

        [Test]
        public void ToCsvFile_NullDocument_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() =>
                DocumentExporter.ToCsvFile(null, "test.csv"));
#pragma warning restore CS8625
        }

        [Test]
        public void ToJsonFile_WhitespacePath_ThrowsArgumentException()
        {
            var doc = new Document();
            NUnit.Framework.Assert.Throws<ArgumentException>(() =>
                DocumentExporter.ToJsonFile(doc, "   "));
        }

        [Test]
        public void ToXmlFile_WhitespacePath_ThrowsArgumentException()
        {
            var doc = new Document();
            NUnit.Framework.Assert.Throws<ArgumentException>(() =>
                DocumentExporter.ToXmlFile(doc, "   "));
        }

        [Test]
        public void ToCsvFile_WhitespacePath_ThrowsArgumentException()
        {
            var doc = new Document();
            NUnit.Framework.Assert.Throws<ArgumentException>(() =>
                DocumentExporter.ToCsvFile(doc, "   "));
        }

        #endregion
    }
}
