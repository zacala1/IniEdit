using IniEdit;
using IniEdit.GUI.Commands;
using NUnit.Framework;

namespace IniEdit.GUI.Tests.Commands
{
    [TestFixture]
    public class GenericCommandTests
    {
        [Test]
        public void Constructor_NullDescription_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GenericCommand(null!, () => { }, () => { }));
        }

        [Test]
        public void Constructor_NullExecuteAction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GenericCommand("Test", null!, () => { }));
        }

        [Test]
        public void Constructor_NullUndoAction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GenericCommand("Test", () => { }, null!));
        }

        [Test]
        public void Description_ReturnsProvidedValue()
        {
            var cmd = new GenericCommand("My Description", () => { }, () => { });
            Assert.That(cmd.Description, Is.EqualTo("My Description"));
        }

        [Test]
        public void Execute_InvokesExecuteAction()
        {
            bool executed = false;
            var cmd = new GenericCommand("Test", () => executed = true, () => { });

            cmd.Execute();

            Assert.That(executed, Is.True);
        }

        [Test]
        public void Undo_InvokesUndoAction()
        {
            bool undone = false;
            var cmd = new GenericCommand("Test", () => { }, () => undone = true);

            cmd.Undo();

            Assert.That(undone, Is.True);
        }
    }

    [TestFixture]
    public class AddSectionCommandTests
    {
        private Document _document = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _document = new Document();
            _refreshCalled = false;
        }

        [Test]
        public void Execute_AddsSection()
        {
            var section = new Section("NewSection");
            var cmd = new AddSectionCommand(_document, section, -1, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_document.HasSection("NewSection"), Is.True);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Execute_InsertsAtIndex()
        {
            _document.AddSection(new Section("First"));
            _document.AddSection(new Section("Third"));
            var section = new Section("Second");
            var cmd = new AddSectionCommand(_document, section, 1, () => { });

            cmd.Execute();

            Assert.That(_document.GetSectionByIndex(1)?.Name, Is.EqualTo("Second"));
        }

        [Test]
        public void Undo_RemovesSection()
        {
            var section = new Section("NewSection");
            var cmd = new AddSectionCommand(_document, section, -1, () => _refreshCalled = true);
            cmd.Execute();
            _refreshCalled = false;

            cmd.Undo();

            Assert.That(_document.HasSection("NewSection"), Is.False);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Description_ContainsSectionName()
        {
            var section = new Section("TestSection");
            var cmd = new AddSectionCommand(_document, section, -1, () => { });

            Assert.That(cmd.Description, Does.Contain("TestSection"));
        }
    }

    [TestFixture]
    public class DeleteSectionCommandTests
    {
        private Document _document = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _document = new Document();
            _document.AddSection(new Section("Section1"));
            _document.AddSection(new Section("Section2"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_RemovesSection()
        {
            var section = _document.GetSection("Section1")!;
            var cmd = new DeleteSectionCommand(_document, section, 0, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_document.HasSection("Section1"), Is.False);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresSection()
        {
            var section = _document.GetSection("Section1")!;
            section.AddProperty(new Property("Key", "Value"));
            var cmd = new DeleteSectionCommand(_document, section, 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_document.HasSection("Section1"), Is.True);
        }

        [Test]
        public void Undo_RestoresAtOriginalIndex()
        {
            var section = _document.GetSection("Section1")!;
            var cmd = new DeleteSectionCommand(_document, section, 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_document.GetSectionByIndex(0)?.Name, Is.EqualTo("Section1"));
        }

        [Test]
        public void Description_ContainsSectionName()
        {
            var section = _document.GetSection("Section1")!;
            var cmd = new DeleteSectionCommand(_document, section, 0, () => { });

            Assert.That(cmd.Description, Does.Contain("Section1"));
        }
    }

    [TestFixture]
    public class EditSectionCommandTests
    {
        private Document _document = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _document = new Document();
            var section = new Section("OldName");
            section.AddProperty(new Property("Key", "Value"));
            _document.AddSection(section);
            _refreshCalled = false;
        }

        [Test]
        public void Execute_RenamesSection()
        {
            var cmd = new EditSectionCommand(_document, "OldName", "NewName", () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_document.HasSection("OldName"), Is.False);
            Assert.That(_document.HasSection("NewName"), Is.True);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Execute_PreservesProperties()
        {
            var cmd = new EditSectionCommand(_document, "OldName", "NewName", () => { });

            cmd.Execute();

            var section = _document.GetSection("NewName");
            Assert.That(section?.GetProperty("Key")?.Value, Is.EqualTo("Value"));
        }

        [Test]
        public void Undo_RestoresOriginalName()
        {
            var cmd = new EditSectionCommand(_document, "OldName", "NewName", () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_document.HasSection("OldName"), Is.True);
            Assert.That(_document.HasSection("NewName"), Is.False);
        }

        [Test]
        public void Description_ContainsBothNames()
        {
            var cmd = new EditSectionCommand(_document, "OldName", "NewName", () => { });

            Assert.That(cmd.Description, Does.Contain("OldName"));
            Assert.That(cmd.Description, Does.Contain("NewName"));
        }
    }

    [TestFixture]
    public class AddPropertyCommandTests
    {
        private Section _section = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _section = new Section("TestSection");
            _refreshCalled = false;
        }

        [Test]
        public void Execute_AddsProperty()
        {
            var property = new Property("Key", "Value");
            var cmd = new AddPropertyCommand(_section, property, -1, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_section.HasProperty("Key"), Is.True);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Execute_InsertsAtIndex()
        {
            _section.AddProperty(new Property("First", "1"));
            _section.AddProperty(new Property("Third", "3"));
            var property = new Property("Second", "2");
            var cmd = new AddPropertyCommand(_section, property, 1, () => { });

            cmd.Execute();

            Assert.That(_section[1]?.Name, Is.EqualTo("Second"));
        }

        [Test]
        public void Undo_RemovesProperty()
        {
            var property = new Property("Key", "Value");
            var cmd = new AddPropertyCommand(_section, property, -1, () => _refreshCalled = true);
            cmd.Execute();
            _refreshCalled = false;

            cmd.Undo();

            Assert.That(_section.HasProperty("Key"), Is.False);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Description_ContainsPropertyInfo()
        {
            var property = new Property("TestKey", "TestValue");
            var cmd = new AddPropertyCommand(_section, property, -1, () => { });

            Assert.That(cmd.Description, Does.Contain("TestKey"));
            Assert.That(cmd.Description, Does.Contain("TestValue"));
        }
    }

    [TestFixture]
    public class DeletePropertyCommandTests
    {
        private Section _section = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _section = new Section("TestSection");
            _section.AddProperty(new Property("Key1", "Value1"));
            _section.AddProperty(new Property("Key2", "Value2"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_RemovesProperty()
        {
            var property = _section.GetProperty("Key1")!;
            var cmd = new DeletePropertyCommand(_section, property, 0, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_section.HasProperty("Key1"), Is.False);
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresProperty()
        {
            var property = _section.GetProperty("Key1")!;
            var cmd = new DeletePropertyCommand(_section, property, 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section.HasProperty("Key1"), Is.True);
            Assert.That(_section.GetProperty("Key1")?.Value, Is.EqualTo("Value1"));
        }

        [Test]
        public void Undo_RestoresAtOriginalIndex()
        {
            var property = _section.GetProperty("Key1")!;
            var cmd = new DeletePropertyCommand(_section, property, 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section[0]?.Name, Is.EqualTo("Key1"));
        }

        [Test]
        public void Undo_PreservesPropertyAttributes()
        {
            var property = _section.GetProperty("Key1")!;
            property.IsQuoted = true;
            property.Comment = new Comment("inline comment");
            var cmd = new DeletePropertyCommand(_section, property, 0, () => { });
            cmd.Execute();

            cmd.Undo();

            var restored = _section.GetProperty("Key1");
            Assert.That(restored?.IsQuoted, Is.True);
            Assert.That(restored?.Comment?.Value, Is.EqualTo("inline comment"));
        }

        [Test]
        public void Description_ContainsPropertyName()
        {
            var property = _section.GetProperty("Key1")!;
            var cmd = new DeletePropertyCommand(_section, property, 0, () => { });

            Assert.That(cmd.Description, Does.Contain("Key1"));
        }
    }

    [TestFixture]
    public class EditPropertyCommandTests
    {
        private Section _section = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _section = new Section("TestSection");
            _section.AddProperty(new Property("OldKey", "OldValue"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_ChangesKeyAndValue()
        {
            var cmd = new EditPropertyCommand(
                _section, "OldKey", "OldValue", "NewKey", "NewValue",
                null, null, false, false, new CommentCollection(), 0, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_section.HasProperty("OldKey"), Is.False);
            Assert.That(_section.HasProperty("NewKey"), Is.True);
            Assert.That(_section.GetProperty("NewKey")?.Value, Is.EqualTo("NewValue"));
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Execute_PreservesIndex()
        {
            _section.AddProperty(new Property("Another", "Value"));
            var cmd = new EditPropertyCommand(
                _section, "OldKey", "OldValue", "NewKey", "NewValue",
                null, null, false, false, new CommentCollection(), 0, () => { });

            cmd.Execute();

            Assert.That(_section[0]?.Name, Is.EqualTo("NewKey"));
        }

        [Test]
        public void Undo_RestoresOriginalProperty()
        {
            var cmd = new EditPropertyCommand(
                _section, "OldKey", "OldValue", "NewKey", "NewValue",
                null, null, false, false, new CommentCollection(), 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section.HasProperty("OldKey"), Is.True);
            Assert.That(_section.HasProperty("NewKey"), Is.False);
            Assert.That(_section.GetProperty("OldKey")?.Value, Is.EqualTo("OldValue"));
        }

        [Test]
        public void Undo_RestoresQuotedState()
        {
            var cmd = new EditPropertyCommand(
                _section, "OldKey", "OldValue", "NewKey", "NewValue",
                null, null, true, false, new CommentCollection(), 0, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section.GetProperty("OldKey")?.IsQuoted, Is.True);
        }

        [Test]
        public void Description_ContainsBothKeys()
        {
            var cmd = new EditPropertyCommand(
                _section, "OldKey", "OldValue", "NewKey", "NewValue",
                null, null, false, false, new CommentCollection(), 0, () => { });

            Assert.That(cmd.Description, Does.Contain("OldKey"));
            Assert.That(cmd.Description, Does.Contain("NewKey"));
        }
    }

    [TestFixture]
    public class MoveSectionCommandTests
    {
        private Document _document = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _document = new Document();
            _document.AddSection(new Section("A"));
            _document.AddSection(new Section("B"));
            _document.AddSection(new Section("C"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_MovesSectionToNewIndex()
        {
            var cmd = new MoveSectionCommand(_document, "A", 0, 2, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_document.GetSectionByIndex(2)?.Name, Is.EqualTo("A"));
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresOriginalPosition()
        {
            var cmd = new MoveSectionCommand(_document, "A", 0, 2, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_document.GetSectionByIndex(0)?.Name, Is.EqualTo("A"));
        }

        [Test]
        public void Description_ContainsSectionName()
        {
            var cmd = new MoveSectionCommand(_document, "TestSection", 0, 1, () => { });

            Assert.That(cmd.Description, Does.Contain("TestSection"));
        }
    }

    [TestFixture]
    public class MovePropertyCommandTests
    {
        private Section _section = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _section = new Section("TestSection");
            _section.AddProperty(new Property("A", "1"));
            _section.AddProperty(new Property("B", "2"));
            _section.AddProperty(new Property("C", "3"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_MovesPropertyToNewIndex()
        {
            var cmd = new MovePropertyCommand(_section, "A", 0, 2, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_section[2]?.Name, Is.EqualTo("A"));
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresOriginalPosition()
        {
            var cmd = new MovePropertyCommand(_section, "A", 0, 2, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section[0]?.Name, Is.EqualTo("A"));
        }

        [Test]
        public void Description_ContainsPropertyName()
        {
            var cmd = new MovePropertyCommand(_section, "TestProperty", 0, 1, () => { });

            Assert.That(cmd.Description, Does.Contain("TestProperty"));
        }
    }

    [TestFixture]
    public class SortSectionsCommandTests
    {
        private Document _document = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _document = new Document();
            _document.AddSection(new Section("C"));
            _document.AddSection(new Section("A"));
            _document.AddSection(new Section("B"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_SortsSectionsAlphabetically()
        {
            var cmd = new SortSectionsCommand(_document, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_document.GetSectionByIndex(0)?.Name, Is.EqualTo("A"));
            Assert.That(_document.GetSectionByIndex(1)?.Name, Is.EqualTo("B"));
            Assert.That(_document.GetSectionByIndex(2)?.Name, Is.EqualTo("C"));
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresOriginalOrder()
        {
            var cmd = new SortSectionsCommand(_document, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_document.GetSectionByIndex(0)?.Name, Is.EqualTo("C"));
            Assert.That(_document.GetSectionByIndex(1)?.Name, Is.EqualTo("A"));
            Assert.That(_document.GetSectionByIndex(2)?.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Description_ReturnsSortSections()
        {
            var cmd = new SortSectionsCommand(_document, () => { });

            Assert.That(cmd.Description, Is.EqualTo("Sort Sections"));
        }
    }

    [TestFixture]
    public class SortPropertiesCommandTests
    {
        private Section _section = null!;
        private bool _refreshCalled;

        [SetUp]
        public void SetUp()
        {
            _section = new Section("TestSection");
            _section.AddProperty(new Property("C", "3"));
            _section.AddProperty(new Property("A", "1"));
            _section.AddProperty(new Property("B", "2"));
            _refreshCalled = false;
        }

        [Test]
        public void Execute_SortsPropertiesAlphabetically()
        {
            var cmd = new SortPropertiesCommand(_section, () => _refreshCalled = true);

            cmd.Execute();

            Assert.That(_section[0]?.Name, Is.EqualTo("A"));
            Assert.That(_section[1]?.Name, Is.EqualTo("B"));
            Assert.That(_section[2]?.Name, Is.EqualTo("C"));
            Assert.That(_refreshCalled, Is.True);
        }

        [Test]
        public void Undo_RestoresOriginalOrder()
        {
            var cmd = new SortPropertiesCommand(_section, () => { });
            cmd.Execute();

            cmd.Undo();

            Assert.That(_section[0]?.Name, Is.EqualTo("C"));
            Assert.That(_section[1]?.Name, Is.EqualTo("A"));
            Assert.That(_section[2]?.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Description_ContainsSectionName()
        {
            var cmd = new SortPropertiesCommand(_section, () => { });

            Assert.That(cmd.Description, Does.Contain("TestSection"));
        }
    }
}
