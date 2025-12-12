using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for deleting a property from a section. Supports undo/redo operations.
    /// </summary>
    public sealed class DeletePropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly Property _property;
        private readonly int _originalIndex;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Delete Property '{_property.Name}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeletePropertyCommand"/> class.
        /// </summary>
        /// <param name="section">The section containing the property.</param>
        /// <param name="property">The property to delete.</param>
        /// <param name="originalIndex">The original index of the property for undo.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public DeletePropertyCommand(Section section, Property property, int originalIndex, Action refreshUI)
        {
            _section = section;
            _property = new Property(property.Name, property.Value)
            {
                Comment = property.Comment,
                IsQuoted = property.IsQuoted
            };
            // Clone pre-comments
            foreach (var comment in property.PreComments)
            {
                _property.PreComments.Add(comment);
            }
            _originalIndex = originalIndex;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            _section.RemoveProperty(_property.Name);
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            var newProperty = new Property(_property.Name, _property.Value)
            {
                Comment = _property.Comment,
                IsQuoted = _property.IsQuoted
            };
            foreach (var comment in _property.PreComments)
            {
                newProperty.PreComments.Add(comment);
            }
            _section.InsertProperty(_originalIndex, newProperty);
            _refreshUI();
        }
    }
}
