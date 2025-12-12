using System;
using System.Collections.Generic;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Manages undo/redo operations using Command Pattern
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();
        private const int MaxStackSize = 100;

        public event EventHandler? StateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string? UndoDescription => CanUndo ? _undoStack.Peek().Description : null;
        public string? RedoDescription => CanRedo ? _redoStack.Peek().Description : null;

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);

            // Limit stack size
            if (_undoStack.Count > MaxStackSize)
            {
                var tempStack = new Stack<ICommand>();
                for (int i = 0; i < MaxStackSize - 1; i++)
                {
                    tempStack.Push(_undoStack.Pop());
                }
                _undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    _undoStack.Push(tempStack.Pop());
                }
            }

            // Clear redo stack when new command is executed
            _redoStack.Clear();

            OnStateChanged();
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            OnStateChanged();
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            OnStateChanged();
        }

        /// <summary>
        /// Clear all undo/redo history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
