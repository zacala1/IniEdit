using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit.Tests.Core
{
    [TestFixture]
    public class CommentCollectionTests
    {
        private CommentCollection _comments;

        [SetUp]
        public void Setup()
        {
            _comments = new CommentCollection();
        }

        [Test]
        public void ToMultiLineText_EmptyCollection_ReturnsEmptyString()
        {
            // Act
            string result = _comments.ToMultiLineText();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ToMultiLineText_SingleComment_ReturnsSingleLine()
        {
            // Arrange
            _comments.Add(new Comment("test"));

            // Act
            string result = _comments.ToMultiLineText();

            // Assert
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void ToMultiLineText_MultipleComments_ReturnsMultiLineText()
        {
            // Arrange
            _comments.Add(new Comment("line1"));
            _comments.Add(new Comment("line2"));
            _comments.Add(new Comment("line3"));

            // Act
            string result = _comments.ToMultiLineText();

            // Assert
            var expected = $"line1{Environment.NewLine}line2{Environment.NewLine}line3";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TrySetFromMultiLineText_NullInput_ClearsCollectionAndReturnsTrue()
        {
            // Arrange
            _comments.Add(new Comment("existing"));

            // Act
#pragma warning disable CS8625
            bool result = _comments.TrySetMultiLineText(null);
#pragma warning restore CS8625

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_comments, Is.Empty);
            });
        }

        [Test]
        public void TrySetFromMultiLineText_EmptyString_ClearsCollectionAndReturnsTrue()
        {
            // Arrange
            _comments.Add(new Comment("existing"));

            // Act
            bool result = _comments.TrySetMultiLineText(string.Empty);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_comments, Is.Empty);
            });
        }

        [Test]
        public void TrySetFromMultiLineText_SingleLine_AddsOneComment()
        {
            // Act
            bool result = _comments.TrySetMultiLineText("single line");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_comments, Has.Count.EqualTo(1));
                Assert.That(_comments[0].Value, Is.EqualTo("single line"));
            });
        }

        [Test]
        public void TrySetFromMultiLineText_MultipleLines_AddsAllComments()
        {
            // Act
            bool result = _comments.TrySetMultiLineText("line1\nline2\r\nline3");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_comments, Has.Count.EqualTo(3));
                Assert.That(_comments[0].Value, Is.EqualTo("line1"));
                Assert.That(_comments[1].Value, Is.EqualTo("line2"));
                Assert.That(_comments[2].Value, Is.EqualTo("line3"));
            });
        }

        [Test]
        public void TrySetFromMultiLineText_DifferentLineEndings_HandlesCorrectly()
        {
            // Act
            bool result = _comments.TrySetMultiLineText("line1\nline2\rline3\r\nline4");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(_comments, Has.Count.EqualTo(4));
                Assert.That(_comments.Select(c => c.Value), Is.EqualTo(new[] { "line1", "line2", "line3", "line4" }));
            });
        }
    }
}
