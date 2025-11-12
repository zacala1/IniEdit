using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class DeletePropertyCommand : ICommand
    {
        private readonly Section section;
        private readonly Property property;
        private readonly int originalIndex;
        private readonly Action refreshUI;

        public string Description => $"Delete Property '{property.Name}'";

        public DeletePropertyCommand(Section section, Property property, int originalIndex, Action refreshUI)
        {
            this.section = section;
            this.property = new Property(property.Name, property.Value)
            {
                Comment = property.Comment,
                IsQuoted = property.IsQuoted
            };
            // Clone pre-comments
            foreach (var comment in property.PreComments)
            {
                this.property.PreComments.Add(comment);
            }
            this.originalIndex = originalIndex;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            section.RemoveProperty(property.Name);
            refreshUI();
        }

        public void Undo()
        {
            var newProperty = new Property(property.Name, property.Value)
            {
                Comment = property.Comment,
                IsQuoted = property.IsQuoted
            };
            foreach (var comment in property.PreComments)
            {
                newProperty.PreComments.Add(comment);
            }
            section.InsertProperty(originalIndex, newProperty);
            refreshUI();
        }
    }
}
