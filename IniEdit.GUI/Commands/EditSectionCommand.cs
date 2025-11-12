using IniEdit;

namespace IniEdit.GUI.Commands
{
    public class EditSectionCommand : ICommand
    {
        private readonly Document document;
        private readonly string oldName;
        private readonly string newName;
        private readonly Action refreshUI;

        public string Description => $"Rename Section '{oldName}' to '{newName}'";

        public EditSectionCommand(Document document, string oldName, string newName, Action refreshUI)
        {
            this.document = document;
            this.oldName = oldName;
            this.newName = newName;
            this.refreshUI = refreshUI;
        }

        public void Execute()
        {
            var section = document.GetSection(oldName);
            if (section != null)
            {
                // Find section index
                int index = -1;
                for (int i = 0; i < document.SectionCount; i++)
                {
                    if (document.GetSectionByIndex(i)?.Name == oldName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    // Create new section with new name
                    var newSection = new Section(newName);
                    newSection.AddPropertyRange(section.GetProperties());
                    newSection.PreComments.AddRange(section.PreComments);
                    newSection.Comment = section.Comment;

                    document.RemoveSection(oldName);
                    document.InsertSection(index, newSection);
                }
            }
            refreshUI();
        }

        public void Undo()
        {
            var section = document.GetSection(newName);
            if (section != null)
            {
                // Find section index
                int index = -1;
                for (int i = 0; i < document.SectionCount; i++)
                {
                    if (document.GetSectionByIndex(i)?.Name == newName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    // Create new section with old name
                    var oldSection = new Section(oldName);
                    oldSection.AddPropertyRange(section.GetProperties());
                    oldSection.PreComments.AddRange(section.PreComments);
                    oldSection.Comment = section.Comment;

                    document.RemoveSection(newName);
                    document.InsertSection(index, oldSection);
                }
            }
            refreshUI();
        }
    }
}
