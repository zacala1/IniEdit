using System.Text;

namespace IniEdit
{
    /// <summary>
    /// Represents a collection of comments that appear before a section or property.
    /// </summary>
    /// <remarks>
    /// This class extends List&lt;Comment&gt; with additional methods for handling multi-line comment text.
    /// Pre-comments are displayed above their associated element in the INI file.
    /// </remarks>
    public class CommentCollection : List<Comment>
    {
        /// <summary>
        /// Line break separators used for splitting multi-line text.
        /// </summary>
        private static readonly string[] LineBreakSeparators = { "\r\n", "\r", "\n" };

        /// <summary>
        /// Estimated average characters per comment for StringBuilder capacity.
        /// </summary>
        private const int EstimatedCharsPerComment = 50;

        /// <summary>
        /// Converts the comment collection to a multi-line text string.
        /// </summary>
        /// <returns>A string with each comment on a separate line, or empty string if no comments.</returns>
        public string ToMultiLineText()
        {
            if (Count == 0) return string.Empty;
            if (Count == 1) return this[0].Value;

            var builder = new StringBuilder(Count * EstimatedCharsPerComment);
            builder.Append(this[0].Value);
            for (int i = 1; i < Count; i++)
            {
                builder.Append(Environment.NewLine);
                builder.Append(this[i].Value);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Tries to set the comments from a multi-line text string.
        /// </summary>
        /// <param name="value">The multi-line text to parse into comments. Null or empty clears the collection.</param>
        /// <returns>True if all lines are valid comment text; false if any line contains invalid characters.</returns>
        /// <remarks>
        /// If parsing fails, the original collection is preserved (no partial updates).
        /// </remarks>
        public bool TrySetMultiLineText(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Clear();
                return true;
            }

            var lines = value.Split(LineBreakSeparators, StringSplitOptions.None);
            var newComments = new List<Comment>(lines.Length);

            foreach (var line in lines)
            {
                try
                {
                    newComments.Add(new Comment(line));
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            Clear();
            AddRange(newComments);
            return true;
        }
    }
}
