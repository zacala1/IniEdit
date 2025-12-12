using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class AddSectionCommand : ICommand
    {
        private readonly Document _document;
        private readonly Section _section;
        private readonly int _index;
        private readonly Action _refreshUI;

        public string Description => $"Add Section '{_section.Name}'";

        public AddSectionCommand(Document document, Section section, int index, Action refreshUI)
        {
            _document = document;
            _section = section;
            _index = index;
            _refreshUI = refreshUI;
        }

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

        public void Undo()
        {
            _document.RemoveSection(_section.Name);
            _refreshUI();
        }
    }
}
