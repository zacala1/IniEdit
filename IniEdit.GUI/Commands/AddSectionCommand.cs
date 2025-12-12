using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for adding a new section to a document. Supports undo/redo operations.
    /// </summary>
    public sealed class AddSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly Section _section;
        private readonly int _index;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Add Section '{_section.Name}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="AddSectionCommand"/> class.
        /// </summary>
        /// <param name="document">The document to add the section to.</param>
        /// <param name="section">The section to add.</param>
        /// <param name="index">The index at which to insert the section, or -1 to append.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public AddSectionCommand(Document document, Section section, int index, Action refreshUI)
        {
            _document = document;
            _section = section;
            _index = index;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            if (_index >= 0 && _index < _document.SectionCount)
            {
                _document.InsertSection(_index, _section);
            }
            else
            {
                _document.AddSection(_section);
            }
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            _document.RemoveSection(_section.Name);
            _refreshUI();
        }
    }
}
