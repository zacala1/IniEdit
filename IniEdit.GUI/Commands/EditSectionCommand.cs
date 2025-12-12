using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class EditSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly string _oldName;
        private readonly string _newName;
        private readonly Action _refreshUI;

        public string Description => $"Rename Section '{_oldName}' to '{_newName}'";

        public EditSectionCommand(Document document, string oldName, string newName, Action refreshUI)
        {
            _document = document;
            _oldName = oldName;
            _newName = newName;
            _refreshUI = refreshUI;
        }

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
