using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class AddPropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly Property _property;
        private readonly int _index;
        private readonly Action _refreshUI;

        public string Description => $"Add Property '{_property.Name}' = '{_property.Value}'";

        public AddPropertyCommand(Section section, Property property, int index, Action refreshUI)
        {
            _section = section;
            _property = property;
            _index = index;
            _refreshUI = refreshUI;
        }

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

        public void Undo()
        {
            _section.RemoveProperty(_property.Name);
            _refreshUI();
        }
    }
}
