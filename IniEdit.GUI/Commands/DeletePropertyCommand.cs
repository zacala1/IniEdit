using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class DeletePropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly Property _property;
        private readonly int _originalIndex;
        private readonly Action _refreshUI;

        public string Description => $"Delete Property '{_property.Name}'";

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

        public void Execute()
        {
            _section.RemoveProperty(_property.Name);
            _refreshUI();
        }

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
