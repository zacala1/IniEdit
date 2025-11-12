using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit.Tests
{
    [TestFixture]
    public class CommentTests
    {
        [Test]
        public void Constructor_WithValueOnly_SetsSemicolonPrefix()
        {
            // Act
            var comment = new Comment("test");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(comment.Prefix, Is.EqualTo(";"));
                Assert.That(comment.Value, Is.EqualTo("test"));
            });
        }

        [Test]
        public void Constructor_WithPrefixAndValue_SetsBoth()
        {
            // Act
            var comment = new Comment("#", "test");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(comment.Prefix, Is.EqualTo("#"));
                Assert.That(comment.Value, Is.EqualTo("test"));
            });
        }

        [Test]
        public void Constructor_WithNewlineInValue_ThrowsArgumentException()
        {
            // Assert
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => new Comment("test\ntest"));
                Assert.Throws<ArgumentException>(() => new Comment("test\rtest"));
                Assert.Throws<ArgumentException>(() => new Comment("test\r\ntest"));
            });
        }

        [Test]
        public void Value_SetWithNewline_ThrowsArgumentException()
        {
            // Arrange
            var comment = new Comment("test");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => comment.Value = "test\ntest");
                Assert.Throws<ArgumentException>(() => comment.Value = "test\rtest");
                Assert.Throws<ArgumentException>(() => comment.Value = "test\r\ntest");
            });
        }

        [Test]
        public void TrySetComment_WithValidValue_ReturnsTrue()
        {
            // Arrange
            var comment = new Comment("original");

            // Act
            bool result = comment.TrySetComment("updated");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(comment.Value, Is.EqualTo("updated"));
            });
        }

        [Test]
        public void TrySetComment_WithNewline_ReturnsFalseAndPreservesOriginal()
        {
            // Arrange
            var comment = new Comment("original");

            // Act
            bool result = comment.TrySetComment("test\ntest");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(comment.Value, Is.EqualTo("original"));
            });
        }

        [Test]
        public void Clone_CreatesNewInstanceWithSameValues()
        {
            // Arrange
            var original = new Comment("#", "test");

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.SameAs(original));
                Assert.That(clone.Prefix, Is.EqualTo(original.Prefix));
                Assert.That(clone.Value, Is.EqualTo(original.Value));
            });
        }

        [Test]
        public void ImplicitConversion_FromComment_ReturnsValue()
        {
            // Arrange
            var comment = new Comment("test");

            // Act
            string value = comment;

            // Assert
            Assert.That(value, Is.EqualTo("test"));
        }

        [Test]
        public void ImplicitConversion_FromNullComment_ReturnsEmptyString()
        {
            // Arrange
#pragma warning disable CS8600
            Comment comment = null;
#pragma warning restore CS8600

            // Act
#pragma warning disable CS8604
            string value = comment;
#pragma warning restore CS8604

            // Assert
            Assert.That(value, Is.Empty);
        }

        [Test]
        public void ImplicitConversion_FromString_CreatesComment()
        {
            // Act
            Comment comment = "test";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(comment.Prefix, Is.EqualTo(";"));
                Assert.That(comment.Value, Is.EqualTo("test"));
            });
        }

        [Test]
        public void ImplicitConversion_FromStringWithNewline_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() =>
            {
                Comment comment = "test\ntest";
            });
        }

        [Test]
        public void Value_SetEmpty_AcceptsEmptyString()
        {
            // Arrange
            var comment = new Comment("test");

            // Act
            comment.Value = string.Empty;

            // Assert
            Assert.That(comment.Value, Is.Empty);
        }

        [Test]
        public void Prefix_CanBeModified()
        {
            // Arrange
            var comment = new Comment(";", "test");

            // Act
            comment.Prefix = "#";

            // Assert
            Assert.That(comment.Prefix, Is.EqualTo("#"));
        }
    }
}
