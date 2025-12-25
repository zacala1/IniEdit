using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for moving a section to a new position. Supports undo/redo operations.
    /// </summary>
    public sealed class MoveSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly string _sectionName;
        private readonly int _oldIndex;
        private readonly int _newIndex;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Move Section '{_sectionName}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveSectionCommand"/> class.
        /// </summary>
        /// <param name="document">The document containing the section.</param>
        /// <param name="sectionName">The name of the section to move.</param>
        /// <param name="oldIndex">The current index of the section.</param>
        /// <param name="newIndex">The target index for the section.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public MoveSectionCommand(Document document, string sectionName, int oldIndex, int newIndex, Action refreshUI)
        {
            _document = document;
            _sectionName = sectionName;
            _oldIndex = oldIndex;
            _newIndex = newIndex;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            MoveSection(_oldIndex, _newIndex);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            MoveSection(_newIndex, _oldIndex);
        }

        private void MoveSection(int fromIndex, int toIndex)
        {
            var section = _document.GetSectionByIndex(fromIndex);
            if (section != null)
            {
                _document.RemoveSection(section.Name);
                _document.InsertSection(toIndex, section);
            }
            _refreshUI();
        }
    }
}
