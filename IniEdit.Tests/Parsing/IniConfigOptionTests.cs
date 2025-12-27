namespace IniEdit.Tests.Parsing
{
    [TestFixture]
    public class IniConfigOptionTests
    {
        private IniConfigOption _options;

        [SetUp]
        public void Setup()
        {
            _options = new IniConfigOption();
        }

        [Test]
        public void Constructor_SetsDefaultValues()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_options.CommentPrefixChars, Is.EquivalentTo(new[] { ';', '#' }));
                Assert.That(_options.DefaultCommentPrefixChar, Is.EqualTo(';'));
                Assert.That(_options.DuplicateKeyPolicy, Is.EqualTo(IniConfigOption.DuplicateKeyPolicyType.FirstWin));
                Assert.That(_options.DuplicateSectionPolicy, Is.EqualTo(IniConfigOption.DuplicateSectionPolicyType.FirstWin));
            });
        }

        [Test]
        public void DefaultCommentPrefixChar_SetValidValue_UpdatesValue()
        {
            // Arrange
            _options.CommentPrefixChars = new[] { '@', '$' };

            // Act
            _options.DefaultCommentPrefixChar = '@';

            // Assert
            Assert.That(_options.DefaultCommentPrefixChar, Is.EqualTo('@'));
        }

        [Test]
        public void CommentPrefixChars_UpdateValue_UpdatesSuccessfully()
        {
            // Act
            var newChars = new[] { '@', '$', '%' };
            _options.CommentPrefixChars = newChars;

            // Assert
            Assert.That(_options.CommentPrefixChars, Is.EquivalentTo(newChars));
        }

        [Test]
        public void DuplicateKeyPolicy_UpdateValue_UpdatesSuccessfully()
        {
            // Act
            _options.DuplicateKeyPolicy = IniConfigOption.DuplicateKeyPolicyType.LastWin;

            // Assert
            Assert.That(_options.DuplicateKeyPolicy,
                Is.EqualTo(IniConfigOption.DuplicateKeyPolicyType.LastWin));
        }

        [Test]
        public void DuplicateSectionPolicy_UpdateValue_UpdatesSuccessfully()
        {
            // Act
            _options.DuplicateSectionPolicy = IniConfigOption.DuplicateSectionPolicyType.Merge;

            // Assert
            Assert.That(_options.DuplicateSectionPolicy,
                Is.EqualTo(IniConfigOption.DuplicateSectionPolicyType.Merge));
        }
    }
}
