using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class DeleteSectionCommand : ICommand
    {
        private readonly Document document;
        private readonly Section section;
        private readonly int originalIndex;
        private readonly Action refreshUI;

        public string Description => $"Delete Section '{section.Name}'";

        public DeleteSectionCommand(Document document, Section section, int originalIndex, Action refreshUI)
        {
            this.document = document;
            this.section = section.Clone(); // Clone to preserve state
            this.originalIndex = originalIndex;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            document.RemoveSection(section.Name);
            refreshUI();
        }

        public void Undo()
        {
            document.InsertSection(originalIndex, section.Clone());
            refreshUI();
        }
    }
}
