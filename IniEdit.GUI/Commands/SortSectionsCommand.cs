using IniEdit;

namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Command for sorting sections alphabetically. Supports undo/redo operations.
    /// </summary>
    public sealed class SortSectionsCommand : ICommand
    {
        private readonly Document _document;
        private readonly List<Section> _originalSections;
        private readonly Action _refreshUI;

        /// <inheritdoc/>
        public string Description => "Sort Sections";

        /// <summary>
        /// Initializes a new instance of the <see cref="SortSectionsCommand"/> class.
        /// </summary>
        /// <param name="document">The document to sort.</param>
        /// <param name="refreshUI">Action to refresh the UI after execution.</param>
        public SortSectionsCommand(Document document, Action refreshUI)
        {
            _document = document;
            _originalSections = new List<Section>();

            // Save original order by cloning sections
            for (int i = 0; i < document.SectionCount; i++)
            {
                var section = document.GetSectionByIndex(i);
                if (section != null)
                {
                    _originalSections.Add(section.Clone());
                }
            }

            _refreshUI = refreshUI;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            _document.SortSectionsByName();
            _refreshUI();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Remove all sections
            while (_document.SectionCount > 0)
            {
                var section = _document.GetSectionByIndex(0);
                if (section != null)
                {
                    _document.RemoveSection(section.Name);
                }
            }

            // Re-add in original order
            foreach (var section in _originalSections)
            {
                _document.AddSection(section.Clone());
            }

            _refreshUI();
        }
    }
}
