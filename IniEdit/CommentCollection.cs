using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit
{
    /// <summary>
    /// Represents a collection of comments that appear before a section or property.
    /// </summary>
    public class CommentCollection : List<Comment>
    {
        /// <summary>
        /// Converts the comment collection to a multi-line text string.
        /// </summary>
        /// <returns>A string with each comment on a separate line.</returns>
        public string ToMultiLineText()
        {
            if (Count == 0) return string.Empty;
            if (Count == 1) return this[0].Value;

            var builder = new StringBuilder(Count * 50); // Estimate 50 chars per comment
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
        /// <param name="value">The multi-line text to parse into comments.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        public bool TrySetMultiLineText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Clear();
                return true;
            }

            var newComments = new List<Comment>();
            var lines = value.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

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
