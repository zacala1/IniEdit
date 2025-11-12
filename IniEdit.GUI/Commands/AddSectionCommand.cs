using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class AddSectionCommand : ICommand
    {
        private readonly Document document;
        private readonly Section section;
        private readonly int index;
        private readonly Action refreshUI;

        public string Description => $"Add Section '{section.Name}'";

        public AddSectionCommand(Document document, Section section, int index, Action refreshUI)
        {
            this.document = document;
            this.section = section;
            this.index = index;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            if (index >= 0 && index < document.SectionCount)
            {
                document.InsertSection(index, section);
            }
            else
            {
                document.AddSection(section);
            }
            refreshUI();
        }

        public void Undo()
        {
            document.RemoveSection(section.Name);
            refreshUI();
        }
    }
}
