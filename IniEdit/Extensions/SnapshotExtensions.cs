namespace IniEdit.Extensions
{
    public static class SnapshotExtensions
    {
        // Deep clone for Document
        public static Document CreateSnapshot(this Document source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Preserve IniConfigOption from source document
            var option = new IniConfigOption
            {
                CommentPrefixChars = source.CommentPrefixChars.ToArray(),
                DefaultCommentPrefixChar = source.DefaultCommentPrefixChar
            };
            var snapshot = new Document(option);

            // Copy default section properties
            foreach (var property in source.DefaultSection.GetProperties())
            {
                snapshot.DefaultSection.AddProperty(property.Clone());
            }

            // Copy all sections
            foreach (var section in source)
            {
                snapshot.AddSection(section.Clone());
            }

            return snapshot;
        }

        // Restore from snapshot
        public static void RestoreFromSnapshot(this Document target, Document snapshot)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            target.Clear();
            target.DefaultSection.Clear();

            // Restore default section
            foreach (var property in snapshot.DefaultSection.GetProperties())
            {
                target.DefaultSection.AddProperty(property.Clone());
            }

            // Restore sections
            foreach (var section in snapshot)
            {
                target.AddSection(section.Clone());
            }
        }
    }

    /// <summary>
    /// Manages document snapshots with undo capability.
    /// </summary>
    public class DocumentSnapshot
    {
        private readonly LinkedList<Document> _snapshots;
        private readonly int _maxSnapshots;

        public Document Current { get; private set; }
        public int SnapshotCount => _snapshots.Count;
        public bool CanUndo => _snapshots.Count > 0;

        public DocumentSnapshot(Document document, int maxSnapshots = 10)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (maxSnapshots < 1)
                throw new ArgumentException("Max snapshots must be at least 1", nameof(maxSnapshots));

            Current = document;
            _snapshots = new LinkedList<Document>();
            _maxSnapshots = maxSnapshots;
        }

        /// <summary>
        /// Takes a snapshot of the current document state.
        /// </summary>
        public void TakeSnapshot()
        {
            var snapshot = Current.CreateSnapshot();
            _snapshots.AddFirst(snapshot);

            // Remove oldest snapshots if over capacity (O(1) operation)
            while (_snapshots.Count > _maxSnapshots)
            {
                _snapshots.RemoveLast();
            }
        }

        /// <summary>
        /// Restores the document to the most recent snapshot.
        /// </summary>
        /// <returns>True if restored; false if no snapshots available.</returns>
        public bool Undo()
        {
            if (!CanUndo)
                return false;

            var snapshot = _snapshots.First!.Value;
            _snapshots.RemoveFirst();
            Current.RestoreFromSnapshot(snapshot);
            return true;
        }

        /// <summary>
        /// Clears all snapshots.
        /// </summary>
        public void ClearSnapshots()
        {
            _snapshots.Clear();
        }
    }
}
