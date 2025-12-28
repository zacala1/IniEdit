using IniEdit.GUI.Commands;
using NUnit.Framework;

namespace IniEdit.GUI.Tests.Commands
{
    [TestFixture]
    public class CommandManagerTests
    {
        private CommandManager _manager = null!;

        [SetUp]
        public void SetUp()
        {
            _manager = new CommandManager();
        }

        #region Initial State Tests

        [Test]
        public void NewManager_CanUndo_IsFalse()
        {
            Assert.That(_manager.CanUndo, Is.False);
        }

        [Test]
        public void NewManager_CanRedo_IsFalse()
        {
            Assert.That(_manager.CanRedo, Is.False);
        }

        [Test]
        public void NewManager_IsDirtyFromSavePoint_IsFalse()
        {
            Assert.That(_manager.IsDirtyFromSavePoint, Is.False);
        }

        [Test]
        public void NewManager_UndoDescription_IsNull()
        {
            Assert.That(_manager.UndoDescription, Is.Null);
        }

        [Test]
        public void NewManager_RedoDescription_IsNull()
        {
            Assert.That(_manager.RedoDescription, Is.Null);
        }

        #endregion

        #region ExecuteCommand Tests

        [Test]
        public void ExecuteCommand_CommandIsExecuted()
        {
            bool executed = false;
            var command = new GenericCommand("Test", () => executed = true, () => { });

            _manager.ExecuteCommand(command);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void ExecuteCommand_CanUndo_IsTrue()
        {
            var command = new GenericCommand("Test", () => { }, () => { });

            _manager.ExecuteCommand(command);

            Assert.That(_manager.CanUndo, Is.True);
        }

        [Test]
        public void ExecuteCommand_UndoDescription_ReturnsCommandDescription()
        {
            var command = new GenericCommand("Add Item", () => { }, () => { });

            _manager.ExecuteCommand(command);

            Assert.That(_manager.UndoDescription, Is.EqualTo("Add Item"));
        }

        [Test]
        public void ExecuteCommand_IsDirtyFromSavePoint_IsTrue()
        {
            var command = new GenericCommand("Test", () => { }, () => { });

            _manager.ExecuteCommand(command);

            Assert.That(_manager.IsDirtyFromSavePoint, Is.True);
        }

        [Test]
        public void ExecuteCommand_ClearsRedoStack()
        {
            var command1 = new GenericCommand("First", () => { }, () => { });
            var command2 = new GenericCommand("Second", () => { }, () => { });

            _manager.ExecuteCommand(command1);
            _manager.Undo();
            Assert.That(_manager.CanRedo, Is.True);

            _manager.ExecuteCommand(command2);
            Assert.That(_manager.CanRedo, Is.False);
        }

        [Test]
        public void ExecuteCommand_RaisesStateChanged()
        {
            bool eventRaised = false;
            _manager.StateChanged += (s, e) => eventRaised = true;
            var command = new GenericCommand("Test", () => { }, () => { });

            _manager.ExecuteCommand(command);

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_CommandIsUndone()
        {
            bool undone = false;
            var command = new GenericCommand("Test", () => { }, () => undone = true);
            _manager.ExecuteCommand(command);

            _manager.Undo();

            Assert.That(undone, Is.True);
        }

        [Test]
        public void Undo_CanRedo_IsTrue()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);

            _manager.Undo();

            Assert.That(_manager.CanRedo, Is.True);
        }

        [Test]
        public void Undo_CanUndo_IsFalse_WhenStackEmpty()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);

            _manager.Undo();

            Assert.That(_manager.CanUndo, Is.False);
        }

        [Test]
        public void Undo_WhenCannotUndo_DoesNothing()
        {
            // Should not throw
            _manager.Undo();
            Assert.That(_manager.CanUndo, Is.False);
        }

        [Test]
        public void Undo_RaisesStateChanged()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);

            bool eventRaised = false;
            _manager.StateChanged += (s, e) => eventRaised = true;

            _manager.Undo();

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Redo Tests

        [Test]
        public void Redo_CommandIsReExecuted()
        {
            int executeCount = 0;
            var command = new GenericCommand("Test", () => executeCount++, () => { });
            _manager.ExecuteCommand(command);
            _manager.Undo();

            _manager.Redo();

            Assert.That(executeCount, Is.EqualTo(2));
        }

        [Test]
        public void Redo_CanUndo_IsTrue()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.Undo();

            _manager.Redo();

            Assert.That(_manager.CanUndo, Is.True);
        }

        [Test]
        public void Redo_CanRedo_IsFalse_WhenStackEmpty()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.Undo();

            _manager.Redo();

            Assert.That(_manager.CanRedo, Is.False);
        }

        [Test]
        public void Redo_WhenCannotRedo_DoesNothing()
        {
            // Should not throw
            _manager.Redo();
            Assert.That(_manager.CanRedo, Is.False);
        }

        [Test]
        public void Redo_RaisesStateChanged()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.Undo();

            bool eventRaised = false;
            _manager.StateChanged += (s, e) => eventRaised = true;

            _manager.Redo();

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_CanUndo_IsFalse()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);

            _manager.Clear();

            Assert.That(_manager.CanUndo, Is.False);
        }

        [Test]
        public void Clear_CanRedo_IsFalse()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.Undo();

            _manager.Clear();

            Assert.That(_manager.CanRedo, Is.False);
        }

        [Test]
        public void Clear_ResetsSavePoint()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.MarkSavePoint();

            _manager.Clear();

            Assert.That(_manager.IsDirtyFromSavePoint, Is.False);
        }

        [Test]
        public void Clear_RaisesStateChanged()
        {
            bool eventRaised = false;
            _manager.StateChanged += (s, e) => eventRaised = true;

            _manager.Clear();

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region SavePoint Tests

        [Test]
        public void MarkSavePoint_IsDirtyFromSavePoint_IsFalse()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);

            _manager.MarkSavePoint();

            Assert.That(_manager.IsDirtyFromSavePoint, Is.False);
        }

        [Test]
        public void MarkSavePoint_AfterUndo_IsDirty()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.MarkSavePoint();

            _manager.Undo();

            Assert.That(_manager.IsDirtyFromSavePoint, Is.True);
        }

        [Test]
        public void MarkSavePoint_AfterUndoAndRedo_IsNotDirty()
        {
            var command = new GenericCommand("Test", () => { }, () => { });
            _manager.ExecuteCommand(command);
            _manager.MarkSavePoint();
            _manager.Undo();

            _manager.Redo();

            Assert.That(_manager.IsDirtyFromSavePoint, Is.False);
        }

        [Test]
        public void MarkSavePoint_RaisesStateChanged()
        {
            bool eventRaised = false;
            _manager.StateChanged += (s, e) => eventRaised = true;

            _manager.MarkSavePoint();

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Multiple Commands Tests

        [Test]
        public void MultipleCommands_UndoInReverseOrder()
        {
            var order = new List<string>();
            var cmd1 = new GenericCommand("First", () => order.Add("exec1"), () => order.Add("undo1"));
            var cmd2 = new GenericCommand("Second", () => order.Add("exec2"), () => order.Add("undo2"));
            var cmd3 = new GenericCommand("Third", () => order.Add("exec3"), () => order.Add("undo3"));

            _manager.ExecuteCommand(cmd1);
            _manager.ExecuteCommand(cmd2);
            _manager.ExecuteCommand(cmd3);
            order.Clear();

            _manager.Undo();
            _manager.Undo();
            _manager.Undo();

            Assert.That(order, Is.EqualTo(new[] { "undo3", "undo2", "undo1" }));
        }

        [Test]
        public void MultipleCommands_RedoInOriginalOrder()
        {
            var order = new List<string>();
            var cmd1 = new GenericCommand("First", () => order.Add("exec1"), () => order.Add("undo1"));
            var cmd2 = new GenericCommand("Second", () => order.Add("exec2"), () => order.Add("undo2"));

            _manager.ExecuteCommand(cmd1);
            _manager.ExecuteCommand(cmd2);
            _manager.Undo();
            _manager.Undo();
            order.Clear();

            _manager.Redo();
            _manager.Redo();

            Assert.That(order, Is.EqualTo(new[] { "exec1", "exec2" }));
        }

        [Test]
        public void MultipleCommands_UndoDescription_ReturnsLastCommand()
        {
            var cmd1 = new GenericCommand("First", () => { }, () => { });
            var cmd2 = new GenericCommand("Second", () => { }, () => { });

            _manager.ExecuteCommand(cmd1);
            _manager.ExecuteCommand(cmd2);

            Assert.That(_manager.UndoDescription, Is.EqualTo("Second"));
        }

        [Test]
        public void MultipleCommands_RedoDescription_ReturnsLastUndone()
        {
            var cmd1 = new GenericCommand("First", () => { }, () => { });
            var cmd2 = new GenericCommand("Second", () => { }, () => { });

            _manager.ExecuteCommand(cmd1);
            _manager.ExecuteCommand(cmd2);
            _manager.Undo();

            Assert.That(_manager.RedoDescription, Is.EqualTo("Second"));
        }

        #endregion

        #region Stack Limit Tests

        [Test]
        public void ExecuteCommand_ExceedsMaxSize_RemovesOldestCommands()
        {
            // Execute 101 commands (max is 100)
            for (int i = 0; i < 101; i++)
            {
                var cmd = new GenericCommand($"Command {i}", () => { }, () => { });
                _manager.ExecuteCommand(cmd);
            }

            // Count how many undos we can do
            int undoCount = 0;
            while (_manager.CanUndo)
            {
                _manager.Undo();
                undoCount++;
            }

            // Should be limited to MaxStackSize - 1 = 99
            Assert.That(undoCount, Is.LessThanOrEqualTo(100));
        }

        #endregion
    }
}
