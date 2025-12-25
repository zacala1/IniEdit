using System;
using System.Collections.Generic;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Manages undo/redo operations using the Command Pattern.
    /// </summary>
    public sealed class CommandManager
    {
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();
        private const int MaxStackSize = 100;

        /// <summary>
        /// Tracks the undo stack count at the time of last save.
        /// -1 means no save point (new document or cleared).
        /// </summary>
        private int _savePointUndoCount = 0;

        /// <summary>
        /// Occurs when the undo/redo state changes.
        /// </summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// Gets a value indicating whether there are commands to undo.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets a value indicating whether there are commands to redo.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the current state differs from the last save point.
        /// This is determined by comparing the current undo stack count with the save point.
        /// </summary>
        public bool IsDirtyFromSavePoint => _undoStack.Count != _savePointUndoCount;

        /// <summary>
        /// Gets the description of the command that would be undone.
        /// </summary>
        public string? UndoDescription => CanUndo ? _undoStack.Peek().Description : null;

        /// <summary>
        /// Gets the description of the command that would be redone.
        /// </summary>
        public string? RedoDescription => CanRedo ? _redoStack.Peek().Description : null;

        /// <summary>
        /// Executes a command and adds it to the undo stack.
        /// </summary>
        /// <param name="command">The command to execute.</param>
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
        /// Undoes the last executed command.
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
        /// Redoes the last undone command.
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
        /// Clears all undo/redo history.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _savePointUndoCount = 0;
            OnStateChanged();
        }

        /// <summary>
        /// Marks the current state as the save point.
        /// Call this after successfully saving the document.
        /// </summary>
        public void MarkSavePoint()
        {
            _savePointUndoCount = _undoStack.Count;
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
