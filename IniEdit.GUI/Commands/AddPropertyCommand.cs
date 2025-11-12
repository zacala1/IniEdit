using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class AddPropertyCommand : ICommand
    {
        private readonly Section section;
        private readonly Property property;
        private readonly int index;
        private readonly Action refreshUI;

        public string Description => $"Add Property '{property.Name}' = '{property.Value}'";

        public AddPropertyCommand(Section section, Property property, int index, Action refreshUI)
        {
            this.section = section;
            this.property = property;
            this.index = index;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            if (index >= 0 && index < section.PropertyCount)
            {
                section.InsertProperty(index, property);
            }
            else
            {
                section.AddProperty(property);
            }
            refreshUI();
        }

        public void Undo()
        {
            section.RemoveProperty(property.Name);
            refreshUI();
        }
    }
}
