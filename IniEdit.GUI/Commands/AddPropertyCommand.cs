using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for adding a new property to a section. Supports undo/redo operations.
    /// </summary>
    public sealed class AddPropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly Property _property;
        private readonly int _index;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Add Property '{_property.Name}' = '{_property.Value}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPropertyCommand"/> class.
        /// </summary>
        /// <param name="section">The section to add the property to.</param>
        /// <param name="property">The property to add.</param>
        /// <param name="index">The index at which to insert the property, or -1 to append.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public AddPropertyCommand(Section section, Property property, int index, Action refreshUI)
        {
            _section = section;
            _property = property;
            _index = index;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            if (_index >= 0 && _index < _section.PropertyCount)
            {
                _section.InsertProperty(_index, _property);
            }
            else
            {
                _section.AddProperty(_property);
            }
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            _section.RemoveProperty(_property.Name);
            _refreshUI();
        }
    }
}
