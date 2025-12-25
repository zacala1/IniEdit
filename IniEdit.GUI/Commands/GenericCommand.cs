using System;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// A generic command that uses delegates for execute and undo actions.
    /// </summary>
    public sealed class GenericCommand : ICommand
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;

        /// <summary>
        /// Gets the description of this command.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericCommand"/> class.
        /// </summary>
        /// <param name="description">The description of the command.</param>
        /// <param name="executeAction">The action to execute.</param>
        /// <param name="undoAction">The action to undo.</param>
        public GenericCommand(string description, Action executeAction, Action undoAction)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public void Execute()
        {
            _executeAction();
        }

        /// <summary>
        /// Undoes the command.
        /// </summary>
        public void Undo()
        {
            _undoAction();
        }
    }
}
