namespace IniEdit.Tests
{
    [TestFixture]
    public class IniConfigValidatorTests
    {
        private IniConfigValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new IniConfigValidator(new IniConfigOption());
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullOption_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625
            var ex = Assert.Throws<ArgumentNullException>(() => new IniConfigValidator(null));
#pragma warning restore CS8625
            Assert.That(ex!.ParamName, Is.EqualTo("option"));
        }

        [Test]
        public void Constructor_WithValidOption_Succeeds()
        {
            // Act
            var validator = new IniConfigValidator(new IniConfigOption());

            // Assert
            Assert.That(validator, Is.Not.Null);
        }

        #endregion

        #region ValidateSectionName Tests

        [Test]
        public void ValidateSectionName_ValidName_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateSectionName("ValidSection");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateSectionName_EmptyName_ReturnsError()
        {
            // Act
            var result = _validator.ValidateSectionName(string.Empty);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("empty"));
            });
        }

        [Test]
        public void ValidateSectionName_WithNewline_ReturnsError()
        {
            // Act
            var result = _validator.ValidateSectionName("Section\nName");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("newline"));
            });
        }

        [Test]
        public void ValidateSectionName_WithOpenBracket_ReturnsError()
        {
            // Act
            var result = _validator.ValidateSectionName("Section[Name");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("brackets"));
            });
        }

        [Test]
        public void ValidateSectionName_WithCloseBracket_ReturnsError()
        {
            // Act
            var result = _validator.ValidateSectionName("Section]Name");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("brackets"));
            });
        }

        #endregion

        #region ValidateKey Tests

        [Test]
        public void ValidateKey_ValidKey_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateKey("ValidKey");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateKey_EmptyKey_ReturnsError()
        {
            // Act
            var result = _validator.ValidateKey(string.Empty);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("empty"));
            });
        }

        [Test]
        public void ValidateKey_WithNewline_ReturnsError()
        {
            // Act
            var result = _validator.ValidateKey("Key\nName");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("newline"));
            });
        }

        [Test]
        public void ValidateKey_WithEqualsSign_ReturnsError()
        {
            // Act
            var result = _validator.ValidateKey("Key=Name");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("equals"));
            });
        }

        #endregion

        #region ValidateValue Tests

        [Test]
        public void ValidateValue_ValidUnquotedValue_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateValue("ValidValue", isQuoted: false);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateValue_ValidQuotedValue_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateValue("Valid Value With Spaces", isQuoted: true);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateValue_NullValue_ReturnsError()
        {
            // Act
#pragma warning disable CS8625
            var result = _validator.ValidateValue(null, isQuoted: false);
#pragma warning restore CS8625

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("null"));
            });
        }

        [Test]
        public void ValidateValue_UnquotedWithNewline_ReturnsError()
        {
            // Act
            var result = _validator.ValidateValue("Value\nWithNewline", isQuoted: false);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("newline"));
            });
        }

        [Test]
        public void ValidateValue_QuotedWithNewline_ReturnsSuccess()
        {
            // Quoted values can contain newlines
            var result = _validator.ValidateValue("Value\nWithNewline", isQuoted: true);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateValue_EmptyString_ReturnsSuccess()
        {
            // Empty string is valid
            var result = _validator.ValidateValue(string.Empty, isQuoted: false);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region ValidatePreComment Tests

        [Test]
        public void ValidatePreComment_ValidComment_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidatePreComment("This is a comment");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidatePreComment_EmptyComment_ReturnsError()
        {
            // Act
            var result = _validator.ValidatePreComment(string.Empty);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("empty"));
            });
        }

        [Test]
        public void ValidatePreComment_WithNewline_ReturnsError()
        {
            // Act
            var result = _validator.ValidatePreComment("Comment\nWith\nNewlines");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("newline"));
            });
        }

        #endregion

        #region ValidatePreCommentAsMultiLine Tests

        [Test]
        public void ValidatePreCommentAsMultiLine_ValidComment_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidatePreCommentAsMultiLine("This is a comment");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidatePreCommentAsMultiLine_WithNewline_ReturnsSuccess()
        {
            // Multi-line comments can contain newlines
            var result = _validator.ValidatePreCommentAsMultiLine("Comment\nWith\nNewlines");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidatePreCommentAsMultiLine_EmptyComment_ReturnsError()
        {
            // Act
            var result = _validator.ValidatePreCommentAsMultiLine(string.Empty);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("empty"));
            });
        }

        #endregion

        #region ValidateInlineComment Tests

        [Test]
        public void ValidateInlineComment_ValidComment_ReturnsSuccess()
        {
            // Act
            var result = _validator.ValidateInlineComment("inline comment");

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateInlineComment_EmptyComment_ReturnsError()
        {
            // Act
            var result = _validator.ValidateInlineComment(string.Empty);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void ValidateInlineComment_WithNewline_ReturnsError()
        {
            // Act
            var result = _validator.ValidateInlineComment("comment\nwith newline");

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region IniValidationResult Tests

        [Test]
        public void IniValidationResult_Success_HasNoErrorMessage()
        {
            // Act
            var result = IniValidationResult.Success();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.ErrorMessage, Is.Null);
            });
        }

        [Test]
        public void IniValidationResult_Error_HasErrorMessage()
        {
            // Act
            var result = IniValidationResult.Error("Test error message");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Is.EqualTo("Test error message"));
            });
        }

        #endregion
    }
}
