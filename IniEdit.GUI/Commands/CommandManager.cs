using System;
using System.Collections.Generic;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Manages undo/redo operations using Command Pattern
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> undoStack = new();
        private readonly Stack<ICommand> redoStack = new();
        private const int MaxStackSize = 100;

        public event EventHandler? StateChanged;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public string? UndoDescription => CanUndo ? undoStack.Peek().Description : null;
        public string? RedoDescription => CanRedo ? redoStack.Peek().Description : null;

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);

            // Limit stack size
            if (undoStack.Count > MaxStackSize)
            {
                var tempStack = new Stack<ICommand>();
                for (int i = 0; i < MaxStackSize - 1; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }

            // Clear redo stack when new command is executed
            redoStack.Clear();

            OnStateChanged();
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);

            OnStateChanged();
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);

            OnStateChanged();
        }

        /// <summary>
        /// Clear all undo/redo history
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
