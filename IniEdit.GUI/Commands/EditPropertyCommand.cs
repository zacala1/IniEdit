using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for editing an existing property. Supports undo/redo operations.
    /// </summary>
    public sealed class EditPropertyCommand : ICommand
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

        /// <inheritdoc/>
        public string Description => $"Edit Property '{_oldKey}' to '{_newKey}'";

        /// <summary>
        /// Initializes a new instance of the <see cref="EditPropertyCommand"/> class.
        /// </summary>
        /// <param name="section">The section containing the property.</param>
        /// <param name="oldKey">The original property key.</param>
        /// <param name="oldValue">The original property value.</param>
        /// <param name="newKey">The new property key.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="oldComment">The original inline comment.</param>
        /// <param name="newComment">The new inline comment.</param>
        /// <param name="oldIsQuoted">Whether the original value was quoted.</param>
        /// <param name="newIsQuoted">Whether the new value should be quoted.</param>
        /// <param name="oldPreComments">The original pre-comments.</param>
        /// <param name="propertyIndex">The index of the property in the section.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
