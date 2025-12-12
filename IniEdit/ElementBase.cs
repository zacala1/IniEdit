namespace IniEdit
{
    /// <summary>
    /// Base class for INI elements (sections and properties) that support comments.
    /// </summary>
    public abstract class ElementBase
    {
        /// <summary>
        /// Gets the name of this element (section name or property key).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the collection of comments that appear before this element.
        /// </summary>
        public CommentCollection PreComments { get; }

        /// <summary>
        /// Gets or sets the inline comment that appears on the same line as this element.
        /// </summary>
        public Comment? Comment { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementBase"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
        public ElementBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Element name cannot be empty or whitespace", nameof(name));
            if (name.StartsWith(' ') || name.EndsWith(' '))
                throw new ArgumentException("Element name cannot have leading or trailing whitespace", nameof(name));
            if (name.AsSpan().IndexOfAny('\r', '\n') >= 0)
                throw new ArgumentException("Element name cannot contain newline characters", nameof(name));

            Name = name;
            PreComments = new CommentCollection();
        }

        /// <summary>
        /// Sets the inline comment, replacing any existing comment.
        /// </summary>
        /// <param name="comment">The comment text.</param>
        protected void SetComment(string? comment)
        {
            if (comment == null)
                return;
            SetComment(new Comment(comment));
        }

        /// <summary>
        /// Sets the inline comment, replacing any existing comment.
        /// </summary>
        /// <param name="comment">The comment object.</param>
        protected void SetComment(Comment? comment)
        {
            if (comment == null)
                return;

            Comment = Comment == null ? comment : new Comment(comment.Prefix, comment.Value);
        }

        /// <summary>
        /// Appends text to the inline comment.
        /// </summary>
        /// <param name="comment">The comment text to append.</param>
        protected void AppendComment(string? comment)
        {
            if (comment == null)
                return;
            AppendComment(new Comment(comment));
        }

        /// <summary>
        /// Appends text to the inline comment.
        /// </summary>
        /// <param name="comment">The comment object to append.</param>
        protected void AppendComment(Comment? comment)
        {
            if (comment == null)
                return;

            Comment = Comment == null ? comment : new Comment(Comment.Prefix, Comment.Value + comment.Value);
        }

        /// <summary>
        /// Adds a comment to the pre-comments collection.
        /// </summary>
        /// <param name="comment">The comment to add.</param>
        protected void AddPreComment(Comment comment)
        {
            PreComments.Add(comment.Clone());
        }

        /// <summary>
        /// Adds a collection of comments to the pre-comments collection.
        /// </summary>
        /// <param name="collection">The comments to add.</param>
        protected void AddPreComments(IEnumerable<Comment> collection)
        {
            var preComments = collection.Where(item => item != null)
                .Select(item => item.Clone()).ToList();
            PreComments.AddRange(preComments);
        }
    }
}
