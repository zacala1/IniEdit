namespace IniEdit.Tests
{
    [TestFixture]
    public class ExceptionTests
    {
        #region ParsingException Tests

        [Test]
        public void ParsingException_WithAllParameters_SetsAllProperties()
        {
            // Arrange
            var errors = new List<ParsingErrorEventArgs>
            {
                new ParsingErrorEventArgs(1, "line1", "error1"),
                new ParsingErrorEventArgs(2, "line2", "error2")
            };

            // Act
            var ex = new ParsingException("Test message", 1, "test line", errors);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("Test message"));
                Assert.That(ex.LineNumber, Is.EqualTo(1));
                Assert.That(ex.Line, Is.EqualTo("test line"));
                Assert.That(ex.AllErrors, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void ParsingException_WithSingleError_SetsProperties()
        {
            // Arrange
            var error = new ParsingErrorEventArgs(5, "error line", "error reason");

            // Act
            var ex = new ParsingException("Test message", error);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("Test message"));
                Assert.That(ex.LineNumber, Is.EqualTo(5));
                Assert.That(ex.Line, Is.EqualTo("error line"));
                Assert.That(ex.AllErrors, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void ParsingException_WithErrorList_SetsFirstErrorAsLineNumber()
        {
            // Arrange
            var errors = new List<ParsingErrorEventArgs>
            {
                new ParsingErrorEventArgs(10, "first line", "first error"),
                new ParsingErrorEventArgs(20, "second line", "second error")
            };

            // Act
            var ex = new ParsingException("Test message", errors);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex.LineNumber, Is.EqualTo(10));
                Assert.That(ex.Line, Is.EqualTo("first line"));
            });
        }

        [Test]
        public void ParsingException_WithNullErrors_SetsEmptyList()
        {
            // Act
#pragma warning disable CS8625
            var ex = new ParsingException("Test message", 1, "line", null);
#pragma warning restore CS8625

            // Assert
            Assert.That(ex.AllErrors, Is.Empty);
        }

        [Test]
        public void ParsingException_ToString_ContainsErrorDetails()
        {
            // Arrange
            var errors = new List<ParsingErrorEventArgs>
            {
                new ParsingErrorEventArgs(1, "line1", "Missing bracket"),
                new ParsingErrorEventArgs(2, "line2", "Invalid key")
            };
            var ex = new ParsingException("Parsing failed", 1, "line1", errors);

            // Act
            var result = ex.ToString();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Does.Contain("Parsing failed"));
                Assert.That(result, Does.Contain("Total errors: 2"));
                Assert.That(result, Does.Contain("Line 1"));
                Assert.That(result, Does.Contain("Missing bracket"));
            });
        }

        [Test]
        public void ParsingException_ToString_TruncatesOver10Errors()
        {
            // Arrange
            var errors = new List<ParsingErrorEventArgs>();
            for (int i = 1; i <= 15; i++)
            {
                errors.Add(new ParsingErrorEventArgs(i, $"line{i}", $"error{i}"));
            }
            var ex = new ParsingException("Many errors", errors);

            // Act
            var result = ex.ToString();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Does.Contain("Total errors: 15"));
                Assert.That(result, Does.Contain("and 5 more errors"));
            });
        }

        #endregion

        #region DuplicateElementException Tests

        [Test]
        public void DuplicateElementException_ForSection_SetsAllProperties()
        {
            // Act
            var ex = new DuplicateElementException(
                "Duplicate section 'Settings'",
                "Settings",
                "Section");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("Duplicate section 'Settings'"));
                Assert.That(ex.ElementName, Is.EqualTo("Settings"));
                Assert.That(ex.ElementType, Is.EqualTo("Section"));
                Assert.That(ex.SectionName, Is.Null);
            });
        }

        [Test]
        public void DuplicateElementException_ForProperty_SetsAllProperties()
        {
            // Act
            var ex = new DuplicateElementException(
                "Duplicate property 'Key1' in section 'Settings'",
                "Key1",
                "Property",
                "Settings");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Is.EqualTo("Duplicate property 'Key1' in section 'Settings'"));
                Assert.That(ex.ElementName, Is.EqualTo("Key1"));
                Assert.That(ex.ElementType, Is.EqualTo("Property"));
                Assert.That(ex.SectionName, Is.EqualTo("Settings"));
            });
        }

        [Test]
        public void DuplicateElementException_InheritsFromInvalidOperationException()
        {
            // Act
            var ex = new DuplicateElementException("Test", "element", "type");

            // Assert
            Assert.That(ex, Is.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region ParsingErrorEventArgs Tests

        [Test]
        public void ParsingErrorEventArgs_SetsAllProperties()
        {
            // Act
            var args = new ParsingErrorEventArgs(10, "[Invalid", "Missing closing bracket");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(args.LineNumber, Is.EqualTo(10));
                Assert.That(args.Line, Is.EqualTo("[Invalid"));
                Assert.That(args.Reason, Is.EqualTo("Missing closing bracket"));
            });
        }

        [Test]
        public void ParsingErrorEventArgs_InheritsFromEventArgs()
        {
            // Act
            var args = new ParsingErrorEventArgs(1, "line", "reason");

            // Assert
            Assert.That(args, Is.InstanceOf<EventArgs>());
        }

        #endregion
    }
}
