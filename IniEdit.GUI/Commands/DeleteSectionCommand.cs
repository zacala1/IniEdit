using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for deleting a section from a document. Supports undo/redo operations.
    /// </summary>
    public sealed class DeleteSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly Section _section;
        private readonly int _originalIndex;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Delete Section '{_section.Name}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteSectionCommand"/> class.
        /// </summary>
        /// <param name="document">The document containing the section.</param>
        /// <param name="section">The section to delete.</param>
        /// <param name="originalIndex">The original index of the section for undo.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public DeleteSectionCommand(Document document, Section section, int originalIndex, Action refreshUI)
        {
            _document = document;
            _section = section.Clone(); // Clone to preserve state
            _originalIndex = originalIndex;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            _document.RemoveSection(_section.Name);
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            _document.InsertSection(_originalIndex, _section.Clone());
            _refreshUI();
        }
    }
}
