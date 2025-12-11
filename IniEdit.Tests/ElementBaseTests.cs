namespace IniEdit.Tests
{
    [TestFixture]
    public class ElementBaseTests
    {
        // Test subclass to access protected methods
        private class TestElement : ElementBase
        {
            public TestElement(string name) : base(name) { }

            public void TestSetComment(string? comment) => SetComment(comment);
            public void TestSetComment(Comment? comment) => SetComment(comment);
            public void TestAppendComment(string? comment) => AppendComment(comment);
            public void TestAppendComment(Comment? comment) => AppendComment(comment);
            public void TestAddPreComment(Comment comment) => AddPreComment(comment);
            public void TestAddPreComments(IEnumerable<Comment> collection) => AddPreComments(collection);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidName_SetsName()
        {
            // Act
            var element = new TestElement("ValidName");

            // Assert
            Assert.That(element.Name, Is.EqualTo("ValidName"));
        }

        [Test]
        public void Constructor_ValidName_InitializesPreComments()
        {
            // Act
            var element = new TestElement("Test");

            // Assert
            Assert.That(element.PreComments, Is.Not.Null);
            Assert.That(element.PreComments, Is.Empty);
        }

        [Test]
        public void Constructor_NullName_ThrowsArgumentException()
        {
            // Act & Assert
#pragma warning disable CS8625
            var ex = Assert.Throws<ArgumentException>(() => new TestElement(null));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_EmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new TestElement(string.Empty));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_WhitespaceName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new TestElement("   "));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_LeadingWhitespace_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new TestElement(" Name"));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_TrailingWhitespace_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new TestElement("Name "));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_WithNewline_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => new TestElement("Name\nTest"));
                Assert.Throws<ArgumentException>(() => new TestElement("Name\rTest"));
            });
        }

        #endregion

        #region SetComment Tests

        [Test]
        public void SetComment_String_SetsComment()
        {
            // Arrange
            var element = new TestElement("Test");

            // Act
            element.TestSetComment("Test comment");

            // Assert
            Assert.That(element.Comment, Is.Not.Null);
            Assert.That(element.Comment!.Value, Is.EqualTo("Test comment"));
        }

        [Test]
        public void SetComment_NullString_DoesNothing()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("Original");

            // Act
            element.TestSetComment((string?)null);

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("Original"));
        }

        [Test]
        public void SetComment_Comment_SetsComment()
        {
            // Arrange
            var element = new TestElement("Test");
            var comment = new Comment("#", "Test comment");

            // Act
            element.TestSetComment(comment);

            // Assert
            Assert.That(element.Comment, Is.Not.Null);
            Assert.That(element.Comment!.Value, Is.EqualTo("Test comment"));
            Assert.That(element.Comment.Prefix, Is.EqualTo("#"));
        }

        [Test]
        public void SetComment_NullComment_DoesNothing()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("Original");

            // Act
            element.TestSetComment((Comment?)null);

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("Original"));
        }

        [Test]
        public void SetComment_WhenCommentExists_ReplacesComment()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("Original");

            // Act
            element.TestSetComment(new Comment("New"));

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("New"));
        }

        #endregion

        #region AppendComment Tests

        [Test]
        public void AppendComment_String_WhenNoComment_SetsComment()
        {
            // Arrange
            var element = new TestElement("Test");

            // Act
            element.TestAppendComment("First comment");

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("First comment"));
        }

        [Test]
        public void AppendComment_String_WhenCommentExists_Appends()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("First");

            // Act
            element.TestAppendComment(" Second");

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("First Second"));
        }

        [Test]
        public void AppendComment_NullString_DoesNothing()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("Original");

            // Act
            element.TestAppendComment((string?)null);

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("Original"));
        }

        [Test]
        public void AppendComment_Comment_WhenNoComment_SetsComment()
        {
            // Arrange
            var element = new TestElement("Test");
            var comment = new Comment("#", "First");

            // Act
            element.TestAppendComment(comment);

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("First"));
        }

        [Test]
        public void AppendComment_Comment_WhenCommentExists_Appends()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment(";", "First");

            // Act
            element.TestAppendComment(new Comment("#", " Second"));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(element.Comment!.Value, Is.EqualTo("First Second"));
                Assert.That(element.Comment.Prefix, Is.EqualTo(";")); // Preserves original prefix
            });
        }

        [Test]
        public void AppendComment_NullComment_DoesNothing()
        {
            // Arrange
            var element = new TestElement("Test");
            element.Comment = new Comment("Original");

            // Act
            element.TestAppendComment((Comment?)null);

            // Assert
            Assert.That(element.Comment!.Value, Is.EqualTo("Original"));
        }

        #endregion

        #region AddPreComment Tests

        [Test]
        public void AddPreComment_AddsToCollection()
        {
            // Arrange
            var element = new TestElement("Test");
            var comment = new Comment("Pre-comment");

            // Act
            element.TestAddPreComment(comment);

            // Assert
            Assert.That(element.PreComments, Has.Count.EqualTo(1));
            Assert.That(element.PreComments[0].Value, Is.EqualTo("Pre-comment"));
        }

        [Test]
        public void AddPreComment_ClonesComment()
        {
            // Arrange
            var element = new TestElement("Test");
            var comment = new Comment("Original");

            // Act
            element.TestAddPreComment(comment);
            comment.Value = "Modified";

            // Assert - should be cloned, so modification doesn't affect
            Assert.That(element.PreComments[0].Value, Is.EqualTo("Original"));
        }

        [Test]
        public void AddPreComment_MultipleComments_AddsAll()
        {
            // Arrange
            var element = new TestElement("Test");

            // Act
            element.TestAddPreComment(new Comment("First"));
            element.TestAddPreComment(new Comment("Second"));
            element.TestAddPreComment(new Comment("Third"));

            // Assert
            Assert.That(element.PreComments, Has.Count.EqualTo(3));
        }

        #endregion

        #region AddPreComments Tests

        [Test]
        public void AddPreComments_AddsAllToCollection()
        {
            // Arrange
            var element = new TestElement("Test");
            var comments = new List<Comment>
            {
                new Comment("First"),
                new Comment("Second"),
                new Comment("Third")
            };

            // Act
            element.TestAddPreComments(comments);

            // Assert
            Assert.That(element.PreComments, Has.Count.EqualTo(3));
        }

        [Test]
        public void AddPreComments_FiltersNullComments()
        {
            // Arrange
            var element = new TestElement("Test");
            var comments = new List<Comment?>
            {
                new Comment("First"),
                null,
                new Comment("Third")
            };

            // Act
            element.TestAddPreComments(comments!);

            // Assert
            Assert.That(element.PreComments, Has.Count.EqualTo(2));
        }

        [Test]
        public void AddPreComments_ClonesComments()
        {
            // Arrange
            var element = new TestElement("Test");
            var original = new Comment("Original");
            var comments = new List<Comment> { original };

            // Act
            element.TestAddPreComments(comments);
            original.Value = "Modified";

            // Assert
            Assert.That(element.PreComments[0].Value, Is.EqualTo("Original"));
        }

        [Test]
        public void AddPreComments_EmptyCollection_DoesNothing()
        {
            // Arrange
            var element = new TestElement("Test");

            // Act
            element.TestAddPreComments(new List<Comment>());

            // Assert
            Assert.That(element.PreComments, Is.Empty);
        }

        #endregion
    }
}
