using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for moving a property to a new position. Supports undo/redo operations.
    /// </summary>
    public sealed class MovePropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly string _propertyName;
        private readonly int _oldIndex;
        private readonly int _newIndex;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Move Property '{_propertyName}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="MovePropertyCommand"/> class.
        /// </summary>
        /// <param name="section">The section containing the property.</param>
        /// <param name="propertyName">The name of the property to move.</param>
        /// <param name="oldIndex">The current index of the property.</param>
        /// <param name="newIndex">The target index for the property.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public MovePropertyCommand(Section section, string propertyName, int oldIndex, int newIndex, Action refreshUI)
        {
            _section = section;
            _propertyName = propertyName;
            _oldIndex = oldIndex;
            _newIndex = newIndex;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            MoveProperty(_oldIndex, _newIndex);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            MoveProperty(_newIndex, _oldIndex);
        }

        private void MoveProperty(int fromIndex, int toIndex)
        {
            var property = _section[fromIndex];
            if (property != null)
            {
                _section.RemoveProperty(property.Name);
                _section.InsertProperty(toIndex, property);
            }
            _refreshUI();
        }
    }
}
