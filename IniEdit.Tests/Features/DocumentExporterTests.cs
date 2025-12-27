using NUnit.Framework;
using System.Text.Json;
using System.Xml;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Features
{
    [TestFixture]
    public class DocumentExporterTests
    {
        #region JSON Export Tests

        [Test]
        public void ToJson_SimpleDocument_ReturnsValidJson()
        {
            // Arrange
            var doc = new Document();
            doc["Database"].AddProperty("Host", "localhost");
            doc["Database"].AddProperty("Port", "3306");

            // Act
            var json = DocumentExporter.ToJson(doc);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Database"));
            Assert.IsTrue(json.Contains("localhost"));
            Assert.IsTrue(json.Contains("3306"));

            // Verify it's valid JSON
            var parsed = JsonDocument.Parse(json);
            Assert.IsNotNull(parsed);
        }

        [Test]
        public void ToJson_WithDefaultSection_IncludesDefaultProperties()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("GlobalKey", "GlobalValue");
            doc["Section1"].AddProperty("Key1", "Value1");

            // Act
            var json = DocumentExporter.ToJson(doc);

            // Assert
            Assert.IsTrue(json.Contains("GlobalKey"));
            Assert.IsTrue(json.Contains("GlobalValue"));
        }

        [Test]
        public void ToJson_FlattenDefaultSection_PutsPropertiesAtRoot()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("GlobalKey", "GlobalValue");

            var options = new JsonExportOptions { FlattenDefaultSection = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsFalse(json.Contains("_default"));
            Assert.IsTrue(json.Contains("GlobalKey"));
        }

        [Test]
        public void ToJson_AutoConvertTypes_ConvertsNumbers()
        {
            // Arrange
            var doc = new Document();
            doc["Settings"].AddProperty("Port", "8080");
            doc["Settings"].AddProperty("Enabled", "true");
            doc["Settings"].AddProperty("Ratio", "3.14");

            var options = new JsonExportOptions { AutoConvertTypes = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            var parsed = JsonDocument.Parse(json);
            var settings = parsed.RootElement.GetProperty("Settings");

            Assert.AreEqual(JsonValueKind.Number, settings.GetProperty("Port").ValueKind);
            Assert.AreEqual(JsonValueKind.True, settings.GetProperty("Enabled").ValueKind);
            Assert.AreEqual(JsonValueKind.Number, settings.GetProperty("Ratio").ValueKind);
        }

        [Test]
        public void ToJson_IncludeComments_AddsCommentProperties()
        {
            // Arrange
            var doc = new Document();
            var prop = new Property("Key", "Value");
            prop.PreComments.Add(new Comment("This is a comment"));
            doc["Section"].AddProperty(prop);

            var options = new JsonExportOptions { IncludeComments = true };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsTrue(json.Contains("preComments"));
            Assert.IsTrue(json.Contains("This is a comment"));
        }

        [Test]
        public void ToJson_NotIndented_ProducesCompactOutput()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new JsonExportOptions { Indented = false };

            // Act
            var json = DocumentExporter.ToJson(doc, options);

            // Assert
            Assert.IsFalse(json.Contains("\n"));
        }

        [Test]
        public void ToJson_NullDocument_ThrowsArgumentNullException()
        {
            // Act & Assert
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => DocumentExporter.ToJson(null));
#pragma warning restore CS8625
        }

        #endregion

        #region XML Export Tests

        [Test]
        public void ToXml_SimpleDocument_ReturnsValidXml()
        {
            // Arrange
            var doc = new Document();
            doc["Database"].AddProperty("Host", "localhost");
            doc["Database"].AddProperty("Port", "3306");

            // Act
            var xml = DocumentExporter.ToXml(doc);

            // Assert
            Assert.IsNotNull(xml);
            Assert.IsTrue(xml.Contains("<configuration>"));
            Assert.IsTrue(xml.Contains("Database"));
            Assert.IsTrue(xml.Contains("localhost"));

            // Verify it's valid XML
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            Assert.IsNotNull(xmlDoc.DocumentElement);
        }

        [Test]
        public void ToXml_CustomRootElement_UsesSpecifiedName()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions { RootElementName = "settings" };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("<settings>"));
            Assert.IsTrue(xml.Contains("</settings>"));
        }

        [Test]
        public void ToXml_WithXmlDeclaration_IncludesDeclaration()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions { IncludeXmlDeclaration = true };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("<?xml"));
        }

        [Test]
        public void ToXml_WithoutXmlDeclaration_OmitsDeclaration()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions { IncludeXmlDeclaration = false };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsFalse(xml.Contains("<?xml"));
        }

        [Test]
        public void ToXml_UseAttributeForValue_PutsValueInAttribute()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new XmlExportOptions { UseAttributeForValue = true };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("value=\"Value\""));
        }

        [Test]
        public void ToXml_IncludeComments_AddsXmlComments()
        {
            // Arrange
            var doc = new Document();
            var prop = new Property("Key", "Value");
            prop.PreComments.Add(new Comment("Property comment"));
            doc["Section"].AddProperty(prop);

            var options = new XmlExportOptions { IncludeComments = true };

            // Act
            var xml = DocumentExporter.ToXml(doc, options);

            // Assert
            Assert.IsTrue(xml.Contains("<!--Property comment-->"));
        }

        [Test]
        public void ToXml_NullDocument_ThrowsArgumentNullException()
        {
            // Act & Assert
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => DocumentExporter.ToXml(null));
#pragma warning restore CS8625
        }

        #endregion

        #region CSV Export Tests

        [Test]
        public void ToCsv_SimpleDocument_ReturnsValidCsv()
        {
            // Arrange
            var doc = new Document();
            doc["Database"].AddProperty("Host", "localhost");
            doc["Database"].AddProperty("Port", "3306");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsNotNull(csv);
            Assert.IsTrue(csv.Contains("Section,Key,Value"));
            Assert.IsTrue(csv.Contains("Database,Host,localhost"));
            Assert.IsTrue(csv.Contains("Database,Port,3306"));
        }

        [Test]
        public void ToCsv_WithHeader_IncludesHeaderRow()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new CsvExportOptions { IncludeHeader = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.AreEqual("Section,Key,Value", lines[0].Trim());
        }

        [Test]
        public void ToCsv_WithoutHeader_OmitsHeaderRow()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new CsvExportOptions { IncludeHeader = false };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Assert - Only data row, no header (with header it would be 2 lines)
            Assert.AreEqual(1, lines.Length);
            Assert.AreEqual("Section,Key,Value", lines[0].Trim());
        }

        [Test]
        public void ToCsv_CustomDelimiter_UsesSemicolon()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");

            var options = new CsvExportOptions { Delimiter = ';' };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);

            // Assert
            Assert.IsTrue(csv.Contains("Section;Key;Value"));
        }

        [Test]
        public void ToCsv_ValueWithComma_EscapesWithQuotes()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value,With,Commas");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains("\"Value,With,Commas\""));
        }

        [Test]
        public void ToCsv_ValueWithQuotes_EscapesQuotes()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value \"quoted\"");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains("\"Value \"\"quoted\"\"\""));
        }

        [Test]
        public void ToCsv_AlwaysQuote_QuotesAllFields()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "SimpleValue");

            var options = new CsvExportOptions { AlwaysQuote = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);

            // Assert
            Assert.IsTrue(csv.Contains("\"Section\""));
            Assert.IsTrue(csv.Contains("\"Key\""));
            Assert.IsTrue(csv.Contains("\"SimpleValue\""));
        }

        [Test]
        public void ToCsv_IncludeComments_AddsCommentColumn()
        {
            // Arrange
            var doc = new Document();
            var prop = new Property("Key", "Value");
            prop.Comment = new Comment("inline comment");
            doc["Section"].AddProperty(prop);

            var options = new CsvExportOptions { IncludeComments = true, IncludeHeader = true };

            // Act
            var csv = DocumentExporter.ToCsv(doc, options);

            // Assert
            Assert.IsTrue(csv.Contains("Section,Key,Value,Comment"));
            Assert.IsTrue(csv.Contains("inline comment"));
        }

        [Test]
        public void ToCsv_DefaultSection_UsesEmptySectionName()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("GlobalKey", "GlobalValue");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains(",GlobalKey,GlobalValue"));
        }

        [Test]
        public void ToCsv_NullDocument_ThrowsArgumentNullException()
        {
            // Act & Assert
#pragma warning disable CS8625
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => DocumentExporter.ToCsv(null));
#pragma warning restore CS8625
        }

        #endregion

        #region File Export Tests

        [Test]
        public void ToJsonFile_CreatesFile()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var tempFile = Path.GetTempFileName() + ".json";

            try
            {
                // Act
                DocumentExporter.ToJsonFile(doc, tempFile);

                // Assert
                Assert.IsTrue(File.Exists(tempFile));
                var content = File.ReadAllText(tempFile);
                Assert.IsTrue(content.Contains("Section"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void ToXmlFile_CreatesFile()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var tempFile = Path.GetTempFileName() + ".xml";

            try
            {
                // Act
                DocumentExporter.ToXmlFile(doc, tempFile);

                // Assert
                Assert.IsTrue(File.Exists(tempFile));
                var content = File.ReadAllText(tempFile);
                Assert.IsTrue(content.Contains("<configuration>"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void ToCsvFile_CreatesFile()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value");
            var tempFile = Path.GetTempFileName() + ".csv";

            try
            {
                // Act
                DocumentExporter.ToCsvFile(doc, tempFile);

                // Assert
                Assert.IsTrue(File.Exists(tempFile));
                var content = File.ReadAllText(tempFile);
                Assert.IsTrue(content.Contains("Section,Key,Value"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void ToJsonFile_EmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => DocumentExporter.ToJsonFile(doc, ""));
        }

        [Test]
        public void ToXmlFile_EmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => DocumentExporter.ToXmlFile(doc, ""));
        }

        [Test]
        public void ToCsvFile_EmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentException>(() => DocumentExporter.ToCsvFile(doc, ""));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ToJson_EmptyDocument_ReturnsEmptyObject()
        {
            // Arrange
            var doc = new Document();

            // Act
            var json = DocumentExporter.ToJson(doc);

            // Assert
            var parsed = JsonDocument.Parse(json);
            Assert.AreEqual(JsonValueKind.Object, parsed.RootElement.ValueKind);
        }

        [Test]
        public void ToXml_EmptyDocument_ReturnsEmptyRoot()
        {
            // Arrange
            var doc = new Document();

            // Act
            var xml = DocumentExporter.ToXml(doc);

            // Assert
            Assert.IsTrue(xml.Contains("<configuration"));
            Assert.IsTrue(xml.Contains("</configuration>") || xml.Contains("<configuration />"));
        }

        [Test]
        public void ToCsv_EmptyDocument_ReturnsOnlyHeader()
        {
            // Arrange
            var doc = new Document();

            // Act
            var csv = DocumentExporter.ToCsv(doc);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.AreEqual(1, lines.Length);
            Assert.AreEqual("Section,Key,Value", lines[0].Trim());
        }

        [Test]
        public void ToJson_SpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value with \"quotes\" and \\ backslash");

            // Act
            var json = DocumentExporter.ToJson(doc);

            // Assert
            var parsed = JsonDocument.Parse(json);
            var value = parsed.RootElement.GetProperty("Section").GetProperty("Key").GetString();
            Assert.AreEqual("Value with \"quotes\" and \\ backslash", value);
        }

        [Test]
        public void ToXml_SpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Value with <xml> & special chars");

            // Act
            var xml = DocumentExporter.ToXml(doc);

            // Assert
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var propertyNode = xmlDoc.SelectSingleNode("//property[@key='Key']");
            Assert.IsNotNull(propertyNode);
            Assert.AreEqual("Value with <xml> & special chars", propertyNode!.InnerText);
        }

        [Test]
        public void ToCsv_NewlineInValue_EscapesCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Key", "Line1\nLine2");

            // Act
            var csv = DocumentExporter.ToCsv(doc);

            // Assert
            Assert.IsTrue(csv.Contains("\"Line1\nLine2\""));
        }

        #endregion
    }
}
