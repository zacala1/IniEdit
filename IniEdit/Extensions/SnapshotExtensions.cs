namespace IniEdit.Extensions
{
    public static class SnapshotExtensions
    {
        // Deep clone for Document
        public static Document CreateSnapshot(this Document source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var snapshot = new Document();

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

    // Snapshot management class
    public class DocumentSnapshot
    {
        private readonly Stack<Document> _snapshots;
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
            _snapshots = new Stack<Document>();
            _maxSnapshots = maxSnapshots;
        }

        public void TakeSnapshot()
        {
            var snapshot = Current.CreateSnapshot();
            _snapshots.Push(snapshot);

            // Limit snapshot history
            while (_snapshots.Count > _maxSnapshots)
            {
                var items = _snapshots.ToArray();
                _snapshots.Clear();
                for (int i = 0; i < _maxSnapshots; i++)
                {
                    _snapshots.Push(items[i]);
                }
            }
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            var snapshot = _snapshots.Pop();
            Current.RestoreFromSnapshot(snapshot);
            return true;
        }

        public void ClearSnapshots()
        {
            _snapshots.Clear();
        }
    }
}
