using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests
{
    [TestFixture]
    public class DocumentDiffTests
    {
        [Test]
        public void Compare_IdenticalDocuments_NoChanges()
        {
            // Arrange
            var doc1 = new Document();
            doc1["Section1"].AddProperty("Key1", "Value1");
            doc1["Section1"].AddProperty("Key2", "Value2");

            var doc2 = new Document();
            doc2["Section1"].AddProperty("Key1", "Value1");
            doc2["Section1"].AddProperty("Key2", "Value2");

            // Act
            var diff = doc1.Compare(doc2);

            // Assert
            Assert.IsFalse(diff.HasChanges);
            Assert.AreEqual(0, diff.AddedSections.Count);
            Assert.AreEqual(0, diff.RemovedSections.Count);
            Assert.AreEqual(0, diff.ModifiedSections.Count);
        }

        [Test]
        public void Compare_AddedSection_DetectsAddition()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");
            modified["Section2"].AddProperty("Key2", "Value2");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.AddedSections.Count);
            Assert.AreEqual("Section2", diff.AddedSections[0].Name);
            Assert.AreEqual(0, diff.RemovedSections.Count);
        }

        [Test]
        public void Compare_RemovedSection_DetectsRemoval()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section2"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(0, diff.AddedSections.Count);
            Assert.AreEqual(1, diff.RemovedSections.Count);
            Assert.AreEqual("Section2", diff.RemovedSections[0].Name);
        }

        [Test]
        public void Compare_ModifiedProperty_DetectsChange()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "OldValue");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "NewValue");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.ModifiedSections.Count);
            var sectionDiff = diff.ModifiedSections[0];
            Assert.AreEqual("Section1", sectionDiff.SectionName);
            Assert.AreEqual(1, sectionDiff.ModifiedProperties.Count);
            Assert.AreEqual("Key1", sectionDiff.ModifiedProperties[0].PropertyName);
            Assert.AreEqual("OldValue", sectionDiff.ModifiedProperties[0].OldValue);
            Assert.AreEqual("NewValue", sectionDiff.ModifiedProperties[0].NewValue);
        }

        [Test]
        public void Compare_AddedProperty_DetectsAddition()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");
            modified["Section1"].AddProperty("Key2", "Value2");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.ModifiedSections.Count);
            var sectionDiff = diff.ModifiedSections[0];
            Assert.AreEqual(1, sectionDiff.AddedProperties.Count);
            Assert.AreEqual("Key2", sectionDiff.AddedProperties[0].Name);
            Assert.AreEqual("Value2", sectionDiff.AddedProperties[0].Value);
        }

        [Test]
        public void Compare_RemovedProperty_DetectsRemoval()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section1"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.ModifiedSections.Count);
            var sectionDiff = diff.ModifiedSections[0];
            Assert.AreEqual(1, sectionDiff.RemovedProperties.Count);
            Assert.AreEqual("Key2", sectionDiff.RemovedProperties[0].Name);
        }

        [Test]
        public void Compare_DefaultSection_DetectsChanges()
        {
            // Arrange
            var original = new Document();
            original.DefaultSection.AddProperty("Key1", "Value1");

            var modified = new Document();
            modified.DefaultSection.AddProperty("Key1", "Value2");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.ModifiedSections.Count);
            var sectionDiff = diff.ModifiedSections[0];
            Assert.AreEqual("$DEFAULT", sectionDiff.SectionName);
            Assert.AreEqual(1, sectionDiff.ModifiedProperties.Count);
        }

        [Test]
        public void Compare_ComplexChanges_DetectsAll()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section2"].AddProperty("Key2", "Value2");
            original["Section3"].AddProperty("Key3", "Value3");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "ModifiedValue");
            modified["Section2"].AddProperty("Key2", "Value2");
            modified["Section4"].AddProperty("Key4", "Value4");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.AddedSections.Count); // Section4
            Assert.AreEqual(1, diff.RemovedSections.Count); // Section3
            Assert.AreEqual(1, diff.ModifiedSections.Count); // Section1
        }

        [Test]
        public void SectionDiff_HasChanges_ReturnsTrueWhenModified()
        {
            // Arrange
            var sectionDiff = new SectionDiff("Test");
            sectionDiff.ModifiedProperties.Add(new PropertyDiff("Key", "Old", "New"));

            // Act & Assert
            Assert.IsTrue(sectionDiff.HasChanges);
        }

        [Test]
        public void SectionDiff_HasChanges_ReturnsFalseWhenEmpty()
        {
            // Arrange
            var sectionDiff = new SectionDiff("Test");

            // Act & Assert
            Assert.IsFalse(sectionDiff.HasChanges);
        }

        [Test]
        public void PropertyDiff_StoresValues_Correctly()
        {
            // Arrange & Act
            var propertyDiff = new PropertyDiff("TestKey", "OldValue", "NewValue");

            // Assert
            Assert.AreEqual("TestKey", propertyDiff.PropertyName);
            Assert.AreEqual("OldValue", propertyDiff.OldValue);
            Assert.AreEqual("NewValue", propertyDiff.NewValue);
        }

        #region Null Parameter Tests

        [Test]
        public void Compare_NullOriginal_ThrowsArgumentNullException()
        {
            // Arrange
            Document? original = null;
            var modified = new Document();

            // Act & Assert
#pragma warning disable CS8604
            var ex = NUnit.Framework.Assert.Throws<ArgumentNullException>(() => original!.Compare(modified));
#pragma warning restore CS8604
            Assert.AreEqual("original", ex!.ParamName);
        }

        [Test]
        public void Compare_NullModified_ThrowsArgumentNullException()
        {
            // Arrange
            var original = new Document();
            Document? modified = null;

            // Act & Assert
#pragma warning disable CS8604
            var ex = NUnit.Framework.Assert.Throws<ArgumentNullException>(() => original.Compare(modified!));
#pragma warning restore CS8604
            Assert.AreEqual("modified", ex!.ParamName);
        }

        [Test]
        public void Compare_BothNull_ThrowsArgumentNullException()
        {
            // Arrange
            Document? original = null;
            Document? modified = null;

            // Act & Assert
#pragma warning disable CS8604
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => original!.Compare(modified!));
#pragma warning restore CS8604
        }

        #endregion

        #region Empty Document Tests

        [Test]
        public void Compare_BothEmptyDocuments_NoChanges()
        {
            // Arrange
            var doc1 = new Document();
            var doc2 = new Document();

            // Act
            var diff = doc1.Compare(doc2);

            // Assert
            Assert.IsFalse(diff.HasChanges);
        }

        [Test]
        public void Compare_EmptyVsPopulated_DetectsAdditions()
        {
            // Arrange
            var original = new Document();
            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.AddedSections.Count);
        }

        [Test]
        public void Compare_PopulatedVsEmpty_DetectsRemovals()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            var modified = new Document();

            // Act
            var diff = original.Compare(modified);

            // Assert
            Assert.IsTrue(diff.HasChanges);
            Assert.AreEqual(1, diff.RemovedSections.Count);
        }

        #endregion

        #region Merge Tests

        [Test]
        public void Merge_AddedSections_AppliesChanges()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");
            modified["Section2"].AddProperty("Key2", "Value2");

            var diff = original.Compare(modified);

            // Act
            var result = original.Merge(diff);

            // Assert
            Assert.AreEqual(1, result.SectionsAdded);
            Assert.IsTrue(original.HasSection("Section2"));
            Assert.AreEqual("Value2", original["Section2"].GetProperty("Key2")!.Value);
        }

        [Test]
        public void Merge_RemovedSections_AppliesWhenEnabled()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section2"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            var diff = original.Compare(modified);
            var options = new MergeOptions { ApplyRemovedSections = true };

            // Act
            var result = original.Merge(diff, options);

            // Assert
            Assert.AreEqual(1, result.SectionsRemoved);
            Assert.IsFalse(original.HasSection("Section2"));
        }

        [Test]
        public void Merge_RemovedSections_IgnoredByDefault()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section2"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            var diff = original.Compare(modified);

            // Act
            var result = original.Merge(diff);

            // Assert
            Assert.AreEqual(0, result.SectionsRemoved);
            Assert.IsTrue(original.HasSection("Section2"));
        }

        [Test]
        public void Merge_AddedProperties_AppliesChanges()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");
            modified["Section1"].AddProperty("Key2", "Value2");

            var diff = original.Compare(modified);

            // Act
            var result = original.Merge(diff);

            // Assert
            Assert.AreEqual(1, result.PropertiesAdded);
            Assert.AreEqual("Value2", original["Section1"].GetProperty("Key2")!.Value);
        }

        [Test]
        public void Merge_ModifiedProperties_AppliesChanges()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "OldValue");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "NewValue");

            var diff = original.Compare(modified);

            // Act
            var result = original.Merge(diff);

            // Assert
            Assert.AreEqual(1, result.PropertiesModified);
            Assert.AreEqual("NewValue", original["Section1"].GetProperty("Key1")!.Value);
        }

        [Test]
        public void Merge_RemovedProperties_AppliesWhenEnabled()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section1"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "Value1");

            var diff = original.Compare(modified);
            var options = new MergeOptions { ApplyRemovedProperties = true };

            // Act
            var result = original.Merge(diff, options);

            // Assert
            Assert.AreEqual(1, result.PropertiesRemoved);
            Assert.IsFalse(original["Section1"].HasProperty("Key2"));
        }

        [Test]
        public void Merge_NullTarget_ThrowsArgumentNullException()
        {
            // Arrange
            Document? target = null;
            var diff = new DocumentDiff();

            // Act & Assert
#pragma warning disable CS8604
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => target!.Merge(diff));
#pragma warning restore CS8604
        }

        [Test]
        public void Merge_NullDiff_ThrowsArgumentNullException()
        {
            // Arrange
            var target = new Document();
            DocumentDiff? diff = null;

            // Act & Assert
#pragma warning disable CS8604
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => target.Merge(diff!));
#pragma warning restore CS8604
        }

        [Test]
        public void Merge_ComplexChanges_AppliesAll()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original["Section2"].AddProperty("Key2", "Value2");

            var modified = new Document();
            modified["Section1"].AddProperty("Key1", "ModifiedValue");
            modified["Section1"].AddProperty("NewKey", "NewValue");
            modified["Section3"].AddProperty("Key3", "Value3");

            var diff = original.Compare(modified);
            var options = new MergeOptions
            {
                ApplyAddedSections = true,
                ApplyAddedProperties = true,
                ApplyModifiedProperties = true
            };

            // Act
            var result = original.Merge(diff, options);

            // Assert
            Assert.AreEqual(1, result.SectionsAdded); // Section3
            Assert.AreEqual(1, result.PropertiesAdded); // NewKey
            Assert.AreEqual(1, result.PropertiesModified); // Key1
            Assert.AreEqual("ModifiedValue", original["Section1"].GetProperty("Key1")!.Value);
            Assert.AreEqual("NewValue", original["Section1"].GetProperty("NewKey")!.Value);
            Assert.IsTrue(original.HasSection("Section3"));
        }

        [Test]
        public void MergeResult_TotalChanges_SumsCorrectly()
        {
            // Arrange
            var result = new MergeResult
            {
                SectionsAdded = 1,
                SectionsRemoved = 2,
                PropertiesAdded = 3,
                PropertiesRemoved = 4,
                PropertiesModified = 5
            };

            // Assert
            Assert.AreEqual(15, result.TotalChanges);
        }

        [Test]
        public void MergeOptions_Defaults_AreCorrect()
        {
            // Arrange & Act
            var options = new MergeOptions();

            // Assert
            Assert.IsTrue(options.ApplyAddedSections);
            Assert.IsFalse(options.ApplyRemovedSections);
            Assert.IsTrue(options.ApplyAddedProperties);
            Assert.IsFalse(options.ApplyRemovedProperties);
            Assert.IsTrue(options.ApplyModifiedProperties);
        }

        #endregion
    }
}
