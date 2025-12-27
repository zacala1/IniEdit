namespace IniEdit.Tests.Extensions
{
    [TestFixture]
    public class DocumentExtensionsTests
    {
        #region SortPropertiesByName (Section) Tests

        [Test]
        public void SortPropertiesByName_Section_SortsAlphabetically()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Zebra", "1");
            section.AddProperty("Alpha", "2");
            section.AddProperty("Middle", "3");

            // Act
            section.SortPropertiesByName();

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Name, Is.EqualTo("Alpha"));
                Assert.That(props[1].Name, Is.EqualTo("Middle"));
                Assert.That(props[2].Name, Is.EqualTo("Zebra"));
            });
        }

        [Test]
        public void SortPropertiesByName_Section_CaseInsensitive()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("zebra", "1");
            section.AddProperty("ALPHA", "2");
            section.AddProperty("Middle", "3");

            // Act
            section.SortPropertiesByName();

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Name, Is.EqualTo("ALPHA"));
                Assert.That(props[1].Name, Is.EqualTo("Middle"));
                Assert.That(props[2].Name, Is.EqualTo("zebra"));
            });
        }

        [Test]
        public void SortPropertiesByName_Section_NullSection_ThrowsArgumentNullException()
        {
            // Arrange
            Section? section = null;

            // Act & Assert
#pragma warning disable CS8604
            var ex = Assert.Throws<ArgumentNullException>(() => section!.SortPropertiesByName());
#pragma warning restore CS8604
            Assert.That(ex!.ParamName, Is.EqualTo("section"));
        }

        [Test]
        public void SortPropertiesByName_Section_EmptySection_NoException()
        {
            // Arrange
            var section = new Section("Empty");

            // Act & Assert
            Assert.DoesNotThrow(() => section.SortPropertiesByName());
        }

        #endregion

        #region SortPropertiesByName (Document) Tests

        [Test]
        public void SortPropertiesByName_Document_SortsAllSectionsProperties()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Zebra", "1");
            doc["Section1"].AddProperty("Alpha", "2");
            doc["Section2"].AddProperty("Charlie", "3");
            doc["Section2"].AddProperty("Bravo", "4");

            // Act
            doc.SortPropertiesByName();

            // Assert
            var section1Props = doc["Section1"].GetProperties().ToList();
            var section2Props = doc["Section2"].GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(section1Props[0].Name, Is.EqualTo("Alpha"));
                Assert.That(section1Props[1].Name, Is.EqualTo("Zebra"));
                Assert.That(section2Props[0].Name, Is.EqualTo("Bravo"));
                Assert.That(section2Props[1].Name, Is.EqualTo("Charlie"));
            });
        }

        [Test]
        public void SortPropertiesByName_Document_NullDocument_ThrowsArgumentNullException()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
#pragma warning disable CS8604
            var ex = Assert.Throws<ArgumentNullException>(() => doc!.SortPropertiesByName());
#pragma warning restore CS8604
            Assert.That(ex!.ParamName, Is.EqualTo("doc"));
        }

        #endregion

        #region SortSectionsByName Tests

        [Test]
        public void SortSectionsByName_SortsAlphabetically()
        {
            // Arrange
            var doc = new Document();
            doc["Zebra"].AddProperty("Key", "1");
            doc["Alpha"].AddProperty("Key", "2");
            doc["Middle"].AddProperty("Key", "3");

            // Act
            doc.SortSectionsByName();

            // Assert
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(sections[0].Name, Is.EqualTo("Alpha"));
                Assert.That(sections[1].Name, Is.EqualTo("Middle"));
                Assert.That(sections[2].Name, Is.EqualTo("Zebra"));
            });
        }

        [Test]
        public void SortSectionsByName_CaseInsensitive()
        {
            // Arrange
            var doc = new Document();
            doc["zebra"].AddProperty("Key", "1");
            doc["ALPHA"].AddProperty("Key", "2");
            doc["Middle"].AddProperty("Key", "3");

            // Act
            doc.SortSectionsByName();

            // Assert
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(sections[0].Name, Is.EqualTo("ALPHA"));
                Assert.That(sections[1].Name, Is.EqualTo("Middle"));
                Assert.That(sections[2].Name, Is.EqualTo("zebra"));
            });
        }

        [Test]
        public void SortSectionsByName_NullDocument_ThrowsArgumentNullException()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
#pragma warning disable CS8604
            var ex = Assert.Throws<ArgumentNullException>(() => doc!.SortSectionsByName());
#pragma warning restore CS8604
            Assert.That(ex!.ParamName, Is.EqualTo("doc"));
        }

        [Test]
        public void SortSectionsByName_EmptyDocument_NoException()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.DoesNotThrow(() => doc.SortSectionsByName());
        }

        #endregion

        #region SortAllByName Tests

        [Test]
        public void SortAllByName_SortsBothSectionsAndProperties()
        {
            // Arrange
            var doc = new Document();
            doc["Zebra"].AddProperty("Key2", "1");
            doc["Zebra"].AddProperty("Key1", "2");
            doc["Alpha"].AddProperty("Beta", "3");
            doc["Alpha"].AddProperty("Alpha", "4");

            // Act
            doc.SortAllByName();

            // Assert
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                // Sections sorted
                Assert.That(sections[0].Name, Is.EqualTo("Alpha"));
                Assert.That(sections[1].Name, Is.EqualTo("Zebra"));

                // Properties sorted
                var alphaProps = sections[0].GetProperties().ToList();
                Assert.That(alphaProps[0].Name, Is.EqualTo("Alpha"));
                Assert.That(alphaProps[1].Name, Is.EqualTo("Beta"));

                var zebraProps = sections[1].GetProperties().ToList();
                Assert.That(zebraProps[0].Name, Is.EqualTo("Key1"));
                Assert.That(zebraProps[1].Name, Is.EqualTo("Key2"));
            });
        }

        [Test]
        public void SortAllByName_NullDocument_ThrowsArgumentNullException()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
#pragma warning disable CS8604
            var ex = Assert.Throws<ArgumentNullException>(() => doc!.SortAllByName());
#pragma warning restore CS8604
            Assert.That(ex!.ParamName, Is.EqualTo("doc"));
        }

        [Test]
        public void SortAllByName_PreservesPropertyValues()
        {
            // Arrange
            var doc = new Document();
            doc["Section"].AddProperty("Zebra", "ZebraValue");
            doc["Section"].AddProperty("Alpha", "AlphaValue");

            // Act
            doc.SortAllByName();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(doc["Section"]["Alpha"].Value, Is.EqualTo("AlphaValue"));
                Assert.That(doc["Section"]["Zebra"].Value, Is.EqualTo("ZebraValue"));
            });
        }

        #endregion

        #region Descending Sort Tests

        [Test]
        public void SortPropertiesByName_Descending_SortsInReverseOrder()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Alpha", "1");
            section.AddProperty("Middle", "2");
            section.AddProperty("Zebra", "3");

            // Act
            section.SortPropertiesByName(descending: true);

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Name, Is.EqualTo("Zebra"));
                Assert.That(props[1].Name, Is.EqualTo("Middle"));
                Assert.That(props[2].Name, Is.EqualTo("Alpha"));
            });
        }

        [Test]
        public void SortSectionsByName_Descending_SortsInReverseOrder()
        {
            // Arrange
            var doc = new Document();
            doc["Alpha"].AddProperty("Key", "1");
            doc["Middle"].AddProperty("Key", "2");
            doc["Zebra"].AddProperty("Key", "3");

            // Act
            doc.SortSectionsByName(descending: true);

            // Assert
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(sections[0].Name, Is.EqualTo("Zebra"));
                Assert.That(sections[1].Name, Is.EqualTo("Middle"));
                Assert.That(sections[2].Name, Is.EqualTo("Alpha"));
            });
        }

        #endregion

        #region SortPropertiesByValue Tests

        [Test]
        public void SortPropertiesByValue_Section_SortsAlphabetically()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Zebra");
            section.AddProperty("Key2", "Alpha");
            section.AddProperty("Key3", "Middle");

            // Act
            section.SortPropertiesByValue();

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Value, Is.EqualTo("Alpha"));
                Assert.That(props[1].Value, Is.EqualTo("Middle"));
                Assert.That(props[2].Value, Is.EqualTo("Zebra"));
            });
        }

        [Test]
        public void SortPropertiesByValue_Descending_SortsInReverseOrder()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Alpha");
            section.AddProperty("Key2", "Zebra");

            // Act
            section.SortPropertiesByValue(descending: true);

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Value, Is.EqualTo("Zebra"));
                Assert.That(props[1].Value, Is.EqualTo("Alpha"));
            });
        }

        #endregion

        #region Custom Comparison Tests

        [Test]
        public void SortProperties_CustomComparison_SortsByValueLength()
        {
            // Arrange
            var section = new Section("Test");
            section.AddProperty("Key1", "Short");
            section.AddProperty("Key2", "A");
            section.AddProperty("Key3", "VeryLongValue");

            // Act
            section.SortProperties((a, b) => a.Value.Length.CompareTo(b.Value.Length));

            // Assert
            var props = section.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(props[0].Value, Is.EqualTo("A"));
                Assert.That(props[1].Value, Is.EqualTo("Short"));
                Assert.That(props[2].Value, Is.EqualTo("VeryLongValue"));
            });
        }

        [Test]
        public void SortSections_CustomComparison_SortsByPropertyCount()
        {
            // Arrange
            var doc = new Document();
            doc["Few"].AddProperty("Key1", "1");
            doc["Many"].AddProperty("Key1", "1");
            doc["Many"].AddProperty("Key2", "2");
            doc["Many"].AddProperty("Key3", "3");
            doc["None"].AddProperty("temp", ""); // Add and remove to create empty section
            doc["None"].RemoveProperty("temp");

            // Act
            doc.SortSections((a, b) => a.PropertyCount.CompareTo(b.PropertyCount));

            // Assert
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(sections[0].Name, Is.EqualTo("None"));
                Assert.That(sections[1].Name, Is.EqualTo("Few"));
                Assert.That(sections[2].Name, Is.EqualTo("Many"));
            });
        }

        [Test]
        public void SortProperties_NullComparison_ThrowsArgumentNullException()
        {
            // Arrange
            var section = new Section("Test");

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => section.SortProperties(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("comparison"));
        }

        #endregion

        #region IncludeDefaultSection Tests

        [Test]
        public void SortPropertiesByName_IncludeDefaultSection_SortsDefaultSectionToo()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Zebra", "1");
            doc.DefaultSection.AddProperty("Alpha", "2");
            doc["Section1"].AddProperty("Beta", "3");
            doc["Section1"].AddProperty("Gamma", "4");

            // Act
            doc.SortPropertiesByName(includeDefaultSection: true);

            // Assert
            var defaultProps = doc.DefaultSection.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(defaultProps[0].Name, Is.EqualTo("Alpha"));
                Assert.That(defaultProps[1].Name, Is.EqualTo("Zebra"));
            });
        }

        [Test]
        public void SortPropertiesByName_ExcludeDefaultSection_LeavesDefaultSectionUnsorted()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Zebra", "1");
            doc.DefaultSection.AddProperty("Alpha", "2");

            // Act
            doc.SortPropertiesByName(includeDefaultSection: false);

            // Assert
            var defaultProps = doc.DefaultSection.GetProperties().ToList();
            Assert.Multiple(() =>
            {
                Assert.That(defaultProps[0].Name, Is.EqualTo("Zebra")); // Not sorted
                Assert.That(defaultProps[1].Name, Is.EqualTo("Alpha"));
            });
        }

        [Test]
        public void SortAllByName_IncludeDefaultSection_SortsEverything()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Zebra", "1");
            doc.DefaultSection.AddProperty("Alpha", "2");
            doc["SectionZ"].AddProperty("Key", "3");
            doc["SectionA"].AddProperty("Key", "4");

            // Act
            doc.SortAllByName(includeDefaultSection: true);

            // Assert
            var defaultProps = doc.DefaultSection.GetProperties().ToList();
            var sections = doc.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(defaultProps[0].Name, Is.EqualTo("Alpha"));
                Assert.That(sections[0].Name, Is.EqualTo("SectionA"));
            });
        }

        #endregion
    }
}
