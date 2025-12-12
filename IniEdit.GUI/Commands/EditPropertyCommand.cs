using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class EditPropertyCommand : ICommand
    {
        private readonly Section _section;
        private readonly string _oldKey;
        private readonly string _oldValue;
        private readonly string _newKey;
        private readonly string _newValue;
        private readonly Comment? _oldComment;
        private readonly Comment? _newComment;
        private readonly bool _oldIsQuoted;
        private readonly bool _newIsQuoted;
        private readonly CommentCollection _oldPreComments;
        private readonly int _propertyIndex;
        private readonly Action _refreshUI;

        public string Description => $"Edit Property '{_oldKey}' to '{_newKey}'";

        public EditPropertyCommand(
            Section section,
            string oldKey,
            string oldValue,
            string newKey,
            string newValue,
            Comment? oldComment,
            Comment? newComment,
            bool oldIsQuoted,
            bool newIsQuoted,
            CommentCollection oldPreComments,
            int propertyIndex,
            Action refreshUI)
        {
            _section = section;
            _oldKey = oldKey;
            _oldValue = oldValue;
            _newKey = newKey;
            _newValue = newValue;
            _oldComment = oldComment;
            _newComment = newComment;
            _oldIsQuoted = oldIsQuoted;
            _newIsQuoted = newIsQuoted;
            _oldPreComments = new CommentCollection();
            foreach (var comment in oldPreComments)
            {
                _oldPreComments.Add(comment);
            }
            _propertyIndex = propertyIndex;
            _refreshUI = refreshUI;
        }

        public void Execute()
        {
            _section.RemoveProperty(_oldKey);
            var newProperty = new Property(_newKey, _newValue)
            {
                Comment = _newComment,
                IsQuoted = _newIsQuoted
            };
            foreach (var comment in _oldPreComments)
            {
                newProperty.PreComments.Add(comment);
            }
            _section.InsertProperty(_propertyIndex, newProperty);
            _refreshUI();
        }

        public void Undo()
        {
            _section.RemoveProperty(_newKey);
            var oldProperty = new Property(_oldKey, _oldValue)
            {
                Comment = _oldComment,
                IsQuoted = _oldIsQuoted
            };
            foreach (var comment in _oldPreComments)
            {
                oldProperty.PreComments.Add(comment);
            }
            _section.InsertProperty(_propertyIndex, oldProperty);
            _refreshUI();
        }
    }
}
