using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class EditPropertyCommand : ICommand
    {
        private readonly Section section;
        private readonly string oldKey;
        private readonly string oldValue;
        private readonly string newKey;
        private readonly string newValue;
        private readonly Comment? oldComment;
        private readonly Comment? newComment;
        private readonly bool oldIsQuoted;
        private readonly bool newIsQuoted;
        private readonly CommentCollection oldPreComments;
        private readonly int propertyIndex;
        private readonly Action refreshUI;

        public string Description => $"Edit Property '{oldKey}' to '{newKey}'";

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
            this.section = section;
            this.oldKey = oldKey;
            this.oldValue = oldValue;
            this.newKey = newKey;
            this.newValue = newValue;
            this.oldComment = oldComment;
            this.newComment = newComment;
            this.oldIsQuoted = oldIsQuoted;
            this.newIsQuoted = newIsQuoted;
            this.oldPreComments = new CommentCollection();
            foreach (var comment in oldPreComments)
            {
                this.oldPreComments.Add(comment);
            }
            this.propertyIndex = propertyIndex;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            section.RemoveProperty(oldKey);
            var newProperty = new Property(newKey, newValue)
            {
                Comment = newComment,
                IsQuoted = newIsQuoted
            };
            foreach (var comment in oldPreComments)
            {
                newProperty.PreComments.Add(comment);
            }
            section.InsertProperty(propertyIndex, newProperty);
            refreshUI();
        }

        public void Undo()
        {
            section.RemoveProperty(newKey);
            var oldProperty = new Property(oldKey, oldValue)
            {
                Comment = oldComment,
                IsQuoted = oldIsQuoted
            };
            foreach (var comment in oldPreComments)
            {
                oldProperty.PreComments.Add(comment);
            }
            section.InsertProperty(propertyIndex, oldProperty);
            refreshUI();
        }
    }
}
