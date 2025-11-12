using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests
{
    [TestFixture]
    public class FilteringExtensionsTests
    {
        [Test]
        public void GetSectionsWhere_FiltersCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key", "Value");
            doc["Section2"].AddProperty("Key", "Value");
            doc["TestSection"].AddProperty("Key", "Value");

            // Act
            var result = doc.GetSectionsWhere(s => s.Name.StartsWith("Section"));

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void GetSectionsByPattern_MatchesRegex()
        {
            // Arrange
            var doc = new Document();
            doc["App.Settings"].AddProperty("Key", "Value");
            doc["App.Database"].AddProperty("Key", "Value");
            doc["System.Log"].AddProperty("Key", "Value");

            // Act
            var result = doc.GetSectionsByPattern("^App\\.");

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(s => s.Name.StartsWith("App.")));
        }

        [Test]
        public void GetPropertiesWhere_FiltersCorrectly()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Value1");
            section.AddProperty("Key2", "");
            section.AddProperty("Key3", "Value3");

            // Act
            var result = section.GetPropertiesWhere(p => p.IsEmpty);

            // Assert
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Key2", result.First().Name);
        }

        [Test]
        public void GetPropertiesByPattern_MatchesRegex()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("HostName", "localhost");
            section.AddProperty("PortNumber", "5432");
            section.AddProperty("DatabaseName", "mydb");

            // Act
            var result = section.GetPropertiesByPattern(".*Name$");

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(p => p.Name.EndsWith("Name")));
        }

        [Test]
        public void GetPropertiesWithValue_FindsMatches()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "localhost");
            section.AddProperty("Key2", "localhost");
            section.AddProperty("Key3", "remotehost");

            // Act
            var result = section.GetPropertiesWithValue("localhost");

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void GetPropertiesContaining_FindsSubstring()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "localhost");
            section.AddProperty("Key2", "localserver");
            section.AddProperty("Key3", "remotehost");

            // Act
            var result = section.GetPropertiesContaining("local");

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void FindPropertiesByName_SearchesAllSections()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Host", "host1");
            doc["Section2"].AddProperty("Host", "host2");
            doc["Section3"].AddProperty("Port", "5432");

            // Act
            var result = doc.FindPropertiesByName("Host");

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void FindPropertiesByName_IncludesDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Host", "default-host");
            doc["Section1"].AddProperty("Host", "host1");

            // Act
            var result = doc.FindPropertiesByName("Host");

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(r => r.Section.Name == "$DEFAULT"));
        }

        [Test]
        public void FindPropertiesByValue_SearchesAllSections()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "target");
            doc["Section2"].AddProperty("Key2", "target");
            doc["Section3"].AddProperty("Key3", "other");

            // Act
            var result = doc.FindPropertiesByValue("target");

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void FindPropertiesByValue_IncludesDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Key", "target");
            doc["Section1"].AddProperty("Key", "target");

            // Act
            var result = doc.FindPropertiesByValue("target");

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(r => r.Section.Name == "$DEFAULT"));
        }

        [Test]
        public void CopyWithSections_FiltersCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Database"].AddProperty("Host", "localhost");
            doc["Logging"].AddProperty("Level", "Info");
            doc["Cache"].AddProperty("Size", "100");

            // Act
            var filtered = doc.CopyWithSections(s => s.Name == "Database" || s.Name == "Cache");

            // Assert
            Assert.AreEqual(2, filtered.SectionCount);
            Assert.IsTrue(filtered.HasSection("Database"));
            Assert.IsTrue(filtered.HasSection("Cache"));
            Assert.IsFalse(filtered.HasSection("Logging"));
        }

        [Test]
        public void CopyWithSections_CopiesDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Version", "1.0");
            doc["Section1"].AddProperty("Key", "Value");

            // Act
            var filtered = doc.CopyWithSections(s => s.Name == "Section1");

            // Assert
            Assert.AreEqual(1, filtered.DefaultSection.PropertyCount);
            Assert.AreEqual("1.0", filtered.DefaultSection["Version"].Value);
        }

        [Test]
        public void CopyWithProperties_FiltersCorrectly()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Value1");
            section.AddProperty("Key2", "");
            section.AddProperty("Key3", "Value3");

            // Act
            var filtered = section.CopyWithProperties(p => !p.IsEmpty);

            // Assert
            Assert.AreEqual(2, filtered.PropertyCount);
            Assert.IsTrue(filtered.HasProperty("Key1"));
            Assert.IsTrue(filtered.HasProperty("Key3"));
            Assert.IsFalse(filtered.HasProperty("Key2"));
        }

        [Test]
        public void GetSectionsWhere_ThrowsOnNullPredicate()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc.GetSectionsWhere(null!));
        }

        [Test]
        public void GetPropertiesWhere_ThrowsOnNullPredicate()
        {
            // Arrange
            var section = new Section("Test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => section.GetPropertiesWhere(null!));
        }

        [Test]
        public void GetSectionsByPattern_ThrowsOnEmptyPattern()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => doc.GetSectionsByPattern(""));
        }

        [Test]
        public void FindPropertiesByName_ThrowsOnEmptyName()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => doc.FindPropertiesByName("").ToList());
        }
    }
}
