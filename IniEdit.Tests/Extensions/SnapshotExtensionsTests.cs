using IniEdit.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Extensions
{
    [TestFixture]
    public class SnapshotExtensionsTests
    {
        [Test]
        public void CreateSnapshot_CreatesDeepCopy()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");
            original.DefaultSection.AddProperty("DefaultKey", "DefaultValue");

            // Act
            var snapshot = original.CreateSnapshot();

            // Assert
            Assert.AreNotSame(original, snapshot);
            Assert.AreEqual(1, snapshot.SectionCount);
            Assert.AreEqual("Value1", snapshot["Section1"]["Key1"].Value);
            Assert.AreEqual("DefaultValue", snapshot.DefaultSection["DefaultKey"].Value);
        }

        [Test]
        public void CreateSnapshot_ModifyingSnapshotDoesNotAffectOriginal()
        {
            // Arrange
            var original = new Document();
            original["Section1"].AddProperty("Key1", "Value1");

            // Act
            var snapshot = original.CreateSnapshot();
            snapshot["Section1"]["Key1"].Value = "Modified";

            // Assert
            Assert.AreEqual("Value1", original["Section1"]["Key1"].Value);
            Assert.AreEqual("Modified", snapshot["Section1"]["Key1"].Value);
        }

        [Test]
        public void CreateSnapshot_PreservesComments()
        {
            // Arrange
            var original = new Document();
            var section = new Section("Test");
            section.PreComments.Add(new Comment("Section comment"));
            section.Comment = new Comment("Inline comment");
            original.AddSection(section);

            var property = new Property("Key", "Value");
            property.PreComments.Add(new Comment("Property comment"));
            section.AddProperty(property);

            // Act
            var snapshot = original.CreateSnapshot();

            // Assert
            Assert.AreEqual(1, snapshot["Test"].PreComments.Count);
            Assert.AreEqual("Section comment", snapshot["Test"].PreComments[0].Value);
            Assert.AreEqual("Inline comment", snapshot["Test"].Comment!.Value);
            Assert.AreEqual(1, snapshot["Test"]["Key"].PreComments.Count);
        }

        [Test]
        public void RestoreFromSnapshot_RestoresState()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Original");

            var snapshot = doc.CreateSnapshot();
            doc["Section1"]["Key1"].Value = "Modified";
            doc["Section2"].AddProperty("Key2", "New");

            // Act
            doc.RestoreFromSnapshot(snapshot);

            // Assert
            Assert.AreEqual("Original", doc["Section1"]["Key1"].Value);
            Assert.IsFalse(doc.HasSection("Section2"));
        }

        [Test]
        public void RestoreFromSnapshot_ClearsExistingContent()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");
            doc["Section2"].AddProperty("Key2", "Value2");

            var snapshot = new Document();
            snapshot["Section1"].AddProperty("Key1", "Value1");

            // Act
            doc.RestoreFromSnapshot(snapshot);

            // Assert
            Assert.AreEqual(1, doc.SectionCount);
            Assert.IsFalse(doc.HasSection("Section2"));
        }

        [Test]
        public void RestoreFromSnapshot_RestoresDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Key", "Original");

            var snapshot = doc.CreateSnapshot();
            doc.DefaultSection["Key"].Value = "Modified";

            // Act
            doc.RestoreFromSnapshot(snapshot);

            // Assert
            Assert.AreEqual("Original", doc.DefaultSection["Key"].Value);
        }

        [Test]
        public void DocumentSnapshot_InitializesCorrectly()
        {
            // Arrange
            var doc = new Document();

            // Act
            var manager = new DocumentSnapshot(doc, maxSnapshots: 5);

            // Assert
            Assert.AreEqual(doc, manager.Current);
            Assert.AreEqual(0, manager.SnapshotCount);
            Assert.IsFalse(manager.CanUndo);
        }

        [Test]
        public void DocumentSnapshot_TakeSnapshot_IncreasesCount()
        {
            // Arrange
            var doc = new Document();
            var manager = new DocumentSnapshot(doc);

            // Act
            manager.TakeSnapshot();

            // Assert
            Assert.AreEqual(1, manager.SnapshotCount);
            Assert.IsTrue(manager.CanUndo);
        }

        [Test]
        public void DocumentSnapshot_Undo_RestoresPreviousState()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Original");

            var manager = new DocumentSnapshot(doc);
            manager.TakeSnapshot();

            doc["Section1"]["Key1"].Value = "Modified";

            // Act
            var result = manager.Undo();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Original", doc["Section1"]["Key1"].Value);
            Assert.AreEqual(0, manager.SnapshotCount);
        }

        [Test]
        public void DocumentSnapshot_Undo_ReturnsFalseWhenNoSnapshots()
        {
            // Arrange
            var doc = new Document();
            var manager = new DocumentSnapshot(doc);

            // Act
            var result = manager.Undo();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void DocumentSnapshot_LimitsSnapshotHistory()
        {
            // Arrange
            var doc = new Document();
            var manager = new DocumentSnapshot(doc, maxSnapshots: 3);

            // Act
            for (int i = 0; i < 5; i++)
            {
                manager.TakeSnapshot();
                doc["Section"]["Key"].Value = $"Value{i}";
            }

            // Assert
            Assert.AreEqual(3, manager.SnapshotCount);
        }

        [Test]
        public void DocumentSnapshot_MultipleUndos_WorksCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc["Section"]["Key"].Value = "State0";

            var manager = new DocumentSnapshot(doc);

            manager.TakeSnapshot();
            doc["Section"]["Key"].Value = "State1";

            manager.TakeSnapshot();
            doc["Section"]["Key"].Value = "State2";

            manager.TakeSnapshot();
            doc["Section"]["Key"].Value = "State3";

            // Act & Assert
            manager.Undo();
            Assert.AreEqual("State2", doc["Section"]["Key"].Value);

            manager.Undo();
            Assert.AreEqual("State1", doc["Section"]["Key"].Value);

            manager.Undo();
            Assert.AreEqual("State0", doc["Section"]["Key"].Value);

            Assert.IsFalse(manager.CanUndo);
        }

        [Test]
        public void DocumentSnapshot_ClearSnapshots_RemovesAll()
        {
            // Arrange
            var doc = new Document();
            var manager = new DocumentSnapshot(doc);
            manager.TakeSnapshot();
            manager.TakeSnapshot();

            // Act
            manager.ClearSnapshots();

            // Assert
            Assert.AreEqual(0, manager.SnapshotCount);
            Assert.IsFalse(manager.CanUndo);
        }

        [Test]
        public void CreateSnapshot_ThrowsOnNull()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc!.CreateSnapshot());
        }

        [Test]
        public void RestoreFromSnapshot_ThrowsOnNullTarget()
        {
            // Arrange
            Document? doc = null;
            var snapshot = new Document();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc!.RestoreFromSnapshot(snapshot));
        }

        [Test]
        public void RestoreFromSnapshot_ThrowsOnNullSnapshot()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc.RestoreFromSnapshot(null!));
        }

        [Test]
        public void DocumentSnapshot_ThrowsOnNullDocument()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DocumentSnapshot(null!, 10));
        }

        [Test]
        public void DocumentSnapshot_ThrowsOnInvalidMaxSnapshots()
        {
            // Arrange
            var doc = new Document();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DocumentSnapshot(doc, maxSnapshots: 0));
        }
    }
}
