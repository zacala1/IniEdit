using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class DeleteSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly Section _section;
        private readonly int _originalIndex;
        private readonly Action _refreshUI;

        public string Description => $"Delete Section '{_section.Name}'";

        public DeleteSectionCommand(Document document, Section section, int originalIndex, Action refreshUI)
        {
            _document = document;
            _section = section.Clone(); // Clone to preserve state
            _originalIndex = originalIndex;
            _refreshUI = refreshUI;
        }

        public void Execute()
        {
            _document.RemoveSection(_section.Name);
            _refreshUI();
        }

        public void Undo()
        {
            _document.InsertSection(_originalIndex, _section.Clone());
            _refreshUI();
        }
    }
}
