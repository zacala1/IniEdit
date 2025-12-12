using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for renaming a section. Supports undo/redo operations.
    /// </summary>
    public sealed class EditSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly string _oldName;
        private readonly string _newName;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => $"Rename Section '{_oldName}' to '{_newName}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="EditSectionCommand"/> class.
        /// </summary>
        /// <param name="document">The document containing the section.</param>
        /// <param name="oldName">The current name of the section.</param>
        /// <param name="newName">The new name for the section.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public EditSectionCommand(Document document, string oldName, string newName, Action refreshUI)
        {
            _document = document;
            _oldName = oldName;
            _newName = newName;
            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            var section = _document.GetSection(_oldName);
            if (section != null)
            {
                // Find section index
                int index = -1;
                for (int i = 0; i < _document.SectionCount; i++)
                {
                    if (_document.GetSectionByIndex(i)?.Name == _oldName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    // Create new section with new name
                    var newSection = new Section(_newName);
                    newSection.AddPropertyRange(section.GetProperties());
                    newSection.PreComments.AddRange(section.PreComments);
                    newSection.Comment = section.Comment;

                    _document.RemoveSection(_oldName);
                    _document.InsertSection(index, newSection);
                }
            }
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            var section = _document.GetSection(_newName);
            if (section != null)
            {
                // Find section index
                int index = -1;
                for (int i = 0; i < _document.SectionCount; i++)
                {
                    if (_document.GetSectionByIndex(i)?.Name == _newName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    // Create new section with old name
                    var oldSection = new Section(_oldName);
                    oldSection.AddPropertyRange(section.GetProperties());
                    oldSection.PreComments.AddRange(section.PreComments);
                    oldSection.Comment = section.Comment;

                    _document.RemoveSection(_newName);
                    _document.InsertSection(index, oldSection);
                }
            }
            _refreshUI();
        }
    }
}
