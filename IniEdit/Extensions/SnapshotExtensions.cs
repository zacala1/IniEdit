namespace IniEdit.Extensions
{
    /// <summary>
    /// Provides extension methods for creating and restoring document snapshots.
    /// </summary>
    public static class SnapshotExtensions
    {
        /// <summary>
        /// Creates a deep clone of the document for snapshot purposes.
        /// </summary>
        /// <param name="source">The document to clone.</param>
        /// <returns>A new document instance with all sections and properties cloned.</returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        /// <remarks>
        /// Preserves <see cref="IniConfigOption"/> settings including CommentPrefixChars and DefaultCommentPrefixChar.
        /// </remarks>
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

        /// <summary>
        /// Restores the document state from a snapshot.
        /// </summary>
        /// <param name="target">The document to restore into.</param>
        /// <param name="snapshot">The snapshot to restore from.</param>
        /// <exception cref="ArgumentNullException">Thrown when target or snapshot is null.</exception>
        /// <remarks>
        /// Clears all existing sections and properties before restoring.
        /// Does not restore IniConfigOption settings from the snapshot.
        /// </remarks>
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

        /// <summary>
        /// Gets the current document being managed.
        /// </summary>
        public Document Current { get; private set; }

        /// <summary>
        /// Gets the number of stored snapshots.
        /// </summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>
        /// Gets a value indicating whether an undo operation is available.
        /// </summary>
        public bool CanUndo => _snapshots.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSnapshot"/> class.
        /// </summary>
        /// <param name="document">The document to manage.</param>
        /// <param name="maxSnapshots">Maximum number of snapshots to retain. Default is 10.</param>
        /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
        /// <exception cref="ArgumentException">Thrown when maxSnapshots is less than 1.</exception>
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
