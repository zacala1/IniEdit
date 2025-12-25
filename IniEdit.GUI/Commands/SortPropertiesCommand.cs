using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for sorting properties in a section alphabetically. Supports undo/redo operations.
    /// </summary>
    public sealed class SortPropertiesCommand : ICommand
    {
        private readonly Section _section;
        private readonly List<Property> _originalProperties;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Sort Properties in '{_section.Name}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="SortPropertiesCommand"/> class.
        /// </summary>
        /// <param name="section">The section to sort.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public SortPropertiesCommand(Section section, Action refreshUI)
        {
            _section = section;
            _originalProperties = new List<Property>();

            // Save original order by cloning properties
            foreach (var property in section.GetProperties())
            {
                _originalProperties.Add(property.Clone());
            }

            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            _section.SortPropertiesByName();
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Remove all properties
            var propertyNames = _section.GetProperties().Select(p => p.Name).ToList();
            foreach (var name in propertyNames)
            {
                _section.RemoveProperty(name);
            }

            // Re-add in original order
            foreach (var property in _originalProperties)
            {
                _section.AddProperty(property.Clone());
            }

            _refreshUI();
        }
    }
}
