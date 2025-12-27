using Newtonsoft.Json.Linq;

namespace IniEdit.Tests.Core
{
    [TestFixture]
    public class PropertyTests
    {
#pragma warning disable CS8618
        private Property _property;
#pragma warning restore CS8618
        private const string TEST_NAME = "TestKey";
        private const string TEST_VALUE = "TestValue";

        [SetUp]
        public void Setup()
        {
            _property = new Property(TEST_NAME, TEST_VALUE);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNameOnly_CreatesPropertyWithEmptyValue()
        {
            // Arrange & Act
            var property = new Property("TestKey");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(property.Name, Is.EqualTo("TestKey"));
                Assert.That(property.Value, Is.Empty);
                Assert.That(property.PreComments, Is.Empty);
                Assert.That(property.Comment, Is.Null);
            });
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new Property(string.Empty));
        }

        [Test]
        public void Constructor_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new Property("   "));
        }

        [Test]
        public void Constructor_WithLeadingWhitespace_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new Property(" key"));
        }

        [Test]
        public void Constructor_WithTrailingWhitespace_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new Property("key "));
        }

        [Test]
        public void Constructor_WithInnerWhitespace_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new Property("key name"));
        }

        [Test]
        public void Constructor_WithNameAndValue_CreatesPropertyWithValue()
        {
            // Arrange & Act
            var property = new Property("TestKey", "TestValue");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(property.Name, Is.EqualTo("TestKey"));
                Assert.That(property.Value, Is.EqualTo("TestValue"));
                Assert.That(property.PreComments, Is.Empty);
                Assert.That(property.Comment, Is.Null);
            });
        }

        [Test]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
#pragma warning disable CS8600, CS8625
            Assert.Throws<ArgumentException>(() => new Property(null));
#pragma warning restore CS8600, CS8625
        }

        [Test]
        [Ignore("Passing null to non-nullable string in nullable enabled context is not intended behavior")]
        public void Constructor_WithNameAndNullValue_CreatesPropertyWithEmptyValue()
        {
            // Arrange & Act
#pragma warning disable CS8625
            var property = new Property("TestKey", null);
#pragma warning restore CS8625

            // Assert
            Assert.That(property.Value, Is.Empty);
        }

        #endregion

        #region Basic Value Tests

        [Test]
        [Ignore("Passing null to non-nullable string in nullable enabled context is not intended behavior")]
        public void Value_SetNull_ConvertedToEmptyString()
        {
            // Arrange & Act
#pragma warning disable CS8600, CS8625
            _property.Value = null;
#pragma warning restore CS8600, CS8625

            // Assert
            Assert.That(_property.Value, Is.Empty);
        }

        [Test]
        public void IsEmpty_EmptyStringValue()
        {
            // Arrange
            _property.Value = "";

            // Act & Assert
            Assert.That(_property.Value, Is.Empty);
            Assert.That(_property.IsEmpty, Is.EqualTo(true));
        }

        [Test]
        public void IsEmpty_EmptyArrayStringValue()
        {
            // Arrange
            _property.Value = "{}";

            // Act & Assert
            Assert.That(_property.Value, Is.Not.Empty);
            Assert.That(_property.IsEmpty, Is.EqualTo(false));
        }

        [Test]
        public void IsEmpty_SetStringValue()
        {
            // Arrange
            _property.Value = "test";

            // Act & Assert
            Assert.That(_property.Value, Is.Not.Empty);
            Assert.That(_property.IsEmpty, Is.EqualTo(false));
        }

        [Test]
        public void GetStringValue_ReturnsOriginalValue()
        {
            // Arrange & Act & Assert
            Assert.That(_property.Value, Is.EqualTo(TEST_VALUE));
        }

        [Test]
        public void GetValue_EmptyString_HandlesCorrectly()
        {
            // Arrange
            _property.Value = "";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_property.GetValue<string>(), Is.Empty);
                Assert.Throws<FormatException>(() => _property.GetValue<int>());
                Assert.Throws<FormatException>(() => _property.GetValue<double>());
                Assert.Throws<FormatException>(() => _property.GetValue<bool>());
            });
        }

        [Test]
        public void GetValue_WhitespaceString_HandlesCorrectly()
        {
            // Arrange
            _property.Value = "   ";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_property.GetValue<string>(), Is.EqualTo("   "));
                Assert.Throws<FormatException>(() => _property.GetValue<int>());
                Assert.Throws<FormatException>(() => _property.GetValue<bool>());
                Assert.Throws<FormatException>(() => _property.GetValue<double>());
            });
        }

        [TestCase("true", true)]
        [TestCase("True", true)]
        [TestCase("TRUE", true)]
        [TestCase("false", false)]
        [TestCase("False", false)]
        [TestCase("FALSE", false)]
        [TestCase("  true  ", true)]
        [TestCase("  false  ", false)]
        public void GetValue_BooleanCaseInsensitive_ParsesCorrectly(string input, bool expected)
        {
            // Arrange
            _property.Value = input;

            // Act & Assert
            Assert.That(_property.GetValue<bool>(), Is.EqualTo(expected));
        }

        [TestCase("1.23E+5", 123000.0)]
        [TestCase("-1.23E-2", -0.0123)]
        [TestCase("nan", double.NaN)]
        //[TestCase("INF", double.PositiveInfinity)]
        //[TestCase("-INF", double.NegativeInfinity)]
        [TestCase("  3.14  ", 3.14)]
        public void GetValue_SpecialDoubleValues_ParsesCorrectly(string input, double expected)
        {
            // Arrange
            _property.Value = input;

            // Act & Assert
            if (double.IsNaN(expected))
                Assert.That(_property.GetValue<double>(), Is.NaN);
            else
                Assert.That(_property.GetValue<double>(), Is.EqualTo(expected));
        }

        [TestCase("42", typeof(int), 42)]
        [TestCase("3.14", typeof(double), 3.14)]
        [TestCase("true", typeof(bool), true)]
        [TestCase("test", typeof(string), "test")]
        [TestCase(" 42 ", typeof(int), 42)]
        [TestCase(" 3.14 ", typeof(double), 3.14)]
        public void GetValue_ValidInput_ReturnsCorrectlyTypedValue(string input, Type type, object expected)
        {
            // Arrange
            _property.Value = input;

            // Act
            var result = typeof(Property)
                .GetMethod("GetValue")
                ?.MakeGenericMethod(type)
                .Invoke(_property, Array.Empty<object>());

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetValue_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "notAnInteger";

            // Assert
            Assert.Throws<FormatException>(() => _property.GetValue<int>());
        }

        #endregion

        #region Array Value Tests

        [Test]
        public void GetValueArray_ValidInput_ReturnsCorrectArray()
        {
            // Arrange
            _property.Value = "{1, 2, 3, 4}";

            // Act
            var result = _property.GetValueArray<int>();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void GetValueArray_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            _property.Value = "{}";

            // Act
            var result = _property.GetValueArray<int>();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [TestCase("{1, , 2,  ,3, }")]
        [TestCase("{1,2,3}")]
        [TestCase("{ 1 , 2 , 3 }")]
        [TestCase("{1,2,3,}")]
        public void GetValueArray_DifferentFormats_ParsesCorrectly(string input)
        {
            // Arrange
            _property.Value = input;

            // Act
            var result = _property.GetValueArray<int>();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void GetValue_WithInvalidInput_ThrowsException()
        {
            _property.Value = "invalid";
            Assert.Throws<FormatException>(() => _property.GetValue<int>());
        }

        [Test]
        public void GetValueArray_WithSpecialCharactersInString_ParsesCorrectly()
        {
            // Arrange
            _property.Value = "{\"test,1\", \"test{2}\", \"test}3\"}";

            // Act
            var result = _property.GetValueArray<string>();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { "test,1", "test{2}", "test}3" }));
        }

        [Test]
        public void GetValueArray_WithInvalidFormat_ThrowsException()
        {
            // Arrange
            _property.Value = "1, 2, 3";

            // Assert
            Assert.Throws<FormatException>(() => _property.GetValueArray<int>());
        }

        [Test]
        public void GetValueArray_WithInvalidType_ThrowsException()
        {
            // Arrange
            _property.Value = "{a, b, c}";

            // Assert
            Assert.Throws<FormatException>(() => _property.GetValueArray<int>());
        }

        [Test]
        public void SetValueArray_ValidInput_FormatsArrayCorrectly()
        {
            // Arrange
            var array = new[] { 1, 2, 3 };

            // Act
            _property.SetValueArray(array);

            // Assert
            Assert.That(_property.Value, Is.EqualTo("{1, 2, 3}"));
        }

        [Test]
        public void GetValueArray_NestedBraces_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "{{1, 2}, {3, 4}}";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValueArray<int>());
        }

        [Test]
        public void GetValueArray_MixedTypes_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "{1, true, 2.5}";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValueArray<int>());
        }

        [Test]
        public void GetValueArray_ExtraSpaces_HandlesCorrectly()
        {
            // Arrange
            _property.Value = "{   1   ,   2   ,   3   }";

            // Act
            var result = _property.GetValueArray<int>();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void SetValueArray_EmptyArray_SetsEmptyArray()
        {
            // Act
            _property.SetValueArray<string>(Array.Empty<string>());
            // Assert
            Assert.That(_property.Value, Is.EqualTo("{}"));
        }

        #endregion

        #region Comment Tests

        [Test]
        public void Comments_AreManaged()
        {
            var preComment = new Comment("Pre Comment");
            var inlineComment = new Comment("Inline Comment");
            _property.PreComments.Add(preComment);
            _property.Comment = inlineComment;

            Assert.Multiple(() =>
            {
                Assert.That(_property.PreComments, Has.Count.EqualTo(1));
                Assert.That(_property.PreComments[0].Value, Does.Contain("Pre Comment"));
                Assert.That(_property.Comment, Is.Not.Null);
                Assert.That(_property.Comment.Value, Does.Contain("Inline Comment"));
            });
        }

        [Test]
        public void Comments_MultiplePreComments_AddedCorrectly()
        {
            // Arrange
            var comments = new[]
            {
                new Comment("First Comment"),
                new Comment("Second Comment")
            };

            // Act
            foreach (var comment in comments)
            {
                _property.PreComments.Add(comment);
            }

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_property.PreComments, Has.Count.EqualTo(2));
                Assert.That(_property.PreComments[0].Value, Is.EqualTo("First Comment"));
                Assert.That(_property.PreComments[1].Value, Is.EqualTo("Second Comment"));
            });
        }

        [Test]
        public void Comments_NullComment_HandledCorrectly()
        {
            // Arrange & Act
            _property.Comment = null;

            // Assert
            Assert.That(_property.Comment, Is.Null);
        }

        #endregion

        #region Clone Tests

        [Test]
        public void Clone_CreatesExactCopy()
        {
            _property.PreComments.Add(new Comment("Pre Comment"));
            _property.Comment = new Comment("Inline Comment");
            var clone = _property.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(clone.Name, Is.EqualTo(_property.Name));
                Assert.That(clone.Value, Is.EqualTo(_property.Value));
                Assert.That(clone.PreComments.Count, Is.EqualTo(_property.PreComments.Count));
                Assert.That(clone.PreComments[0].Value, Is.EqualTo(_property.PreComments[0].Value));
                Assert.That(clone.Comment?.Value, Is.EqualTo(_property.Comment?.Value));
            });
        }

        [Test]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            var clone = _property.Clone();

            clone.Value = "newValue";
            clone.PreComments.Add(new Comment("newComment"));

            Assert.Multiple(() =>
            {
                Assert.That(_property.Value, Is.EqualTo(TEST_VALUE));
                Assert.That(_property.PreComments, Is.Empty);
                Assert.That(clone.Value, Is.EqualTo("newValue"));
                Assert.That(clone.PreComments, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void Clone_WithNullComment_ClonesCorrectly()
        {
            // Arrange
            _property.PreComments.Add(new Comment("preComment"));
            _property.Comment = null;

            // Act
            var clone = _property.Clone();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(clone.PreComments, Has.Count.EqualTo(1));
                Assert.That(clone.Comment, Is.Null);
            });
        }

        [Test]
        public void Clone_WithEmptyPreComments_ClonesCorrectly()
        {
            // Act
            var clone = _property.Clone();

            // Assert
            Assert.That(clone.PreComments, Is.Empty);
        }

        #endregion

        #region TryGetValue Tests (FEATURE-004)

        [Test]
        public void TryGetValue_ValidIntValue_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "42";

            // Act
            var result = _property.TryGetValue<int>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(42));
            });
        }

        [Test]
        public void TryGetValue_InvalidIntValue_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "not an integer";

            // Act
            var result = _property.TryGetValue<int>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(int)));
            });
        }

        [Test]
        public void TryGetValue_ValidDoubleValue_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "3.14";

            // Act
            var result = _property.TryGetValue<double>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(3.14).Within(0.001));
            });
        }

        [Test]
        public void TryGetValue_InvalidDoubleValue_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "not a double";

            // Act
            var result = _property.TryGetValue<double>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(double)));
            });
        }

        [Test]
        public void TryGetValue_ValidBoolValue_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "true";

            // Act
            var result = _property.TryGetValue<bool>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.True);
            });
        }

        [Test]
        public void TryGetValue_InvalidBoolValue_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "not a bool";

            // Act
            var result = _property.TryGetValue<bool>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(bool)));
            });
        }

        [Test]
        public void TryGetValue_StringValue_AlwaysReturnsTrue()
        {
            // Arrange
            _property.Value = "any string value";

            // Act
            var result = _property.TryGetValue<string>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo("any string value"));
            });
        }

        [Test]
        public void TryGetValue_OverflowValue_ReturnsFalse()
        {
            // Arrange
            _property.Value = "999999999999999999999";

            // Act
            var result = _property.TryGetValue<int>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(value, Is.EqualTo(default(int)));
            });
        }

        [Test]
        public void TryGetValue_EmptyString_ReturnsFalseForNumericTypes()
        {
            // Arrange
            _property.Value = "";

            // Act
            var intResult = _property.TryGetValue<int>(out var intValue);
            var doubleResult = _property.TryGetValue<double>(out var doubleValue);
            var boolResult = _property.TryGetValue<bool>(out var boolValue);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(intResult, Is.False);
                Assert.That(intValue, Is.EqualTo(default(int)));
                Assert.That(doubleResult, Is.False);
                Assert.That(doubleValue, Is.EqualTo(default(double)));
                Assert.That(boolResult, Is.False);
                Assert.That(boolValue, Is.EqualTo(default(bool)));
            });
        }

        [Test]
        public void TryGetValue_WithWhitespace_HandlesCorrectly()
        {
            // Arrange
            _property.Value = "  42  ";

            // Act
            var result = _property.TryGetValue<int>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(42));
            });
        }

        [TestCase("42", typeof(int), 42)]
        [TestCase("3.14", typeof(double), 3.14)]
        [TestCase("true", typeof(bool), true)]
        [TestCase("test", typeof(string), "test")]
        public void TryGetValue_VariousTypes_ParsesCorrectly(string input, Type type, object expected)
        {
            // Arrange
            _property.Value = input;

            // Act
            var tryGetValueMethod = typeof(Property).GetMethod("TryGetValue");
            var genericMethod = tryGetValueMethod?.MakeGenericMethod(type);
            var parameters = new object?[] { null };
            var result = (bool)genericMethod!.Invoke(_property, parameters)!;
            var value = parameters[0];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(value, Is.EqualTo(expected));
            });
        }

        [Test]
        public void GetValueOrDefault_WithValidValue_ReturnsValue()
        {
            // Arrange
            _property.Value = "42";

            // Act
            var value = _property.GetValueOrDefault<int>(100);

            // Assert
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void GetValueOrDefault_WithInvalidValue_ReturnsDefault()
        {
            // Arrange
            _property.Value = "invalid";

            // Act
            var value = _property.GetValueOrDefault<int>(100);

            // Assert
            Assert.That(value, Is.EqualTo(100));
        }

        [Test]
        public void GetValueOrDefault_WithEmptyValue_ReturnsDefault()
        {
            // Arrange
            _property.Value = "";

            // Act
            var value = _property.GetValueOrDefault<int>(999);

            // Assert
            Assert.That(value, Is.EqualTo(999));
        }

        #endregion

        #region LOW Priority Tests - Fluent API

        [Test]
        public void WithValue_SetsValue_ReturnsProperty()
        {
            // Act
            var result = _property.WithValue("NewValue");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_property));
                Assert.That(_property.Value, Is.EqualTo("NewValue"));
            });
        }

        [Test]
        public void WithValueTyped_SetsValue_ReturnsProperty()
        {
            // Act
            var result = _property.WithValue<int>(42);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_property));
                Assert.That(_property.Value, Is.EqualTo("42"));
            });
        }

        [Test]
        public void WithQuoted_SetsQuoted_ReturnsProperty()
        {
            // Act
            var result = _property.WithQuoted();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_property));
                Assert.That(_property.IsQuoted, Is.True);
            });
        }

        [Test]
        public void WithComment_SetsComment_ReturnsProperty()
        {
            // Act
            var result = _property.WithComment("Test comment");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_property));
                Assert.That(_property.Comment, Is.Not.Null);
                Assert.That(_property.Comment!.Value, Is.EqualTo("Test comment"));
            });
        }

        [Test]
        public void WithPreComment_AddsPreComment_ReturnsProperty()
        {
            // Act
            var result = _property.WithPreComment("Pre comment");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.SameAs(_property));
                Assert.That(_property.PreComments, Has.Count.EqualTo(1));
                Assert.That(_property.PreComments[0].Value, Is.EqualTo("Pre comment"));
            });
        }

        [Test]
        public void FluentAPI_ChainMultipleOperations_WorksCorrectly()
        {
            // Act
            var result = new Property("Config")
                .WithValue("Production")
                .WithQuoted()
                .WithComment("Environment setting")
                .WithPreComment("Important configuration");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.EqualTo("Production"));
                Assert.That(result.IsQuoted, Is.True);
                Assert.That(result.Comment!.Value, Is.EqualTo("Environment setting"));
                Assert.That(result.PreComments, Has.Count.EqualTo(1));
            });
        }

        #endregion

        #region GetValue<T> Fast Path Tests (long, float, decimal)

        [Test]
        public void GetValue_Long_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "9223372036854775807"; // long.MaxValue

            // Act
            var result = _property.GetValue<long>();

            // Assert
            Assert.That(result, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void GetValue_Long_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a long";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<long>());
        }

        [Test]
        public void GetValue_Long_WithWhitespace_ParsesCorrectly()
        {
            // Arrange
            _property.Value = "  12345678901234  ";

            // Act
            var result = _property.GetValue<long>();

            // Assert
            Assert.That(result, Is.EqualTo(12345678901234L));
        }

        [Test]
        public void TryGetValue_Long_ValidInput_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "9876543210";

            // Act
            var success = _property.TryGetValue<long>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(value, Is.EqualTo(9876543210L));
            });
        }

        [Test]
        public void TryGetValue_Long_InvalidInput_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "invalid";

            // Act
            var success = _property.TryGetValue<long>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(value, Is.EqualTo(default(long)));
            });
        }

        [Test]
        public void GetValue_Float_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "3.14159";

            // Act
            var result = _property.GetValue<float>();

            // Assert
            Assert.That(result, Is.EqualTo(3.14159f).Within(0.00001f));
        }

        [Test]
        public void GetValue_Float_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a float";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<float>());
        }

        [Test]
        public void GetValue_Float_ScientificNotation_ParsesCorrectly()
        {
            // Arrange
            _property.Value = "1.5E+10";

            // Act
            var result = _property.GetValue<float>();

            // Assert
            Assert.That(result, Is.EqualTo(1.5E+10f).Within(1E+5f));
        }

        [Test]
        public void TryGetValue_Float_ValidInput_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "2.71828";

            // Act
            var success = _property.TryGetValue<float>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(value, Is.EqualTo(2.71828f).Within(0.00001f));
            });
        }

        [Test]
        public void TryGetValue_Float_InvalidInput_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "invalid";

            // Act
            var success = _property.TryGetValue<float>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(value, Is.EqualTo(default(float)));
            });
        }

        [Test]
        public void GetValue_Decimal_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "12345678901234567890.12345678";

            // Act
            var result = _property.GetValue<decimal>();

            // Assert
            Assert.That(result, Is.EqualTo(12345678901234567890.12345678m));
        }

        [Test]
        public void GetValue_Decimal_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a decimal";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<decimal>());
        }

        [Test]
        public void GetValue_Decimal_HighPrecision_PreservesPrecision()
        {
            // Arrange
            _property.Value = "0.123456789012345678901234567";

            // Act
            var result = _property.GetValue<decimal>();

            // Assert - decimal has 28-29 significant digits precision
            Assert.That(result.ToString().Length, Is.GreaterThan(15));
        }

        [Test]
        public void TryGetValue_Decimal_ValidInput_ReturnsTrueAndValue()
        {
            // Arrange
            _property.Value = "999.99";

            // Act
            var success = _property.TryGetValue<decimal>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(value, Is.EqualTo(999.99m));
            });
        }

        [Test]
        public void TryGetValue_Decimal_InvalidInput_ReturnsFalseAndDefault()
        {
            // Arrange
            _property.Value = "invalid";

            // Act
            var success = _property.TryGetValue<decimal>(out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(value, Is.EqualTo(default(decimal)));
            });
        }

        [Test]
        public void GetValue_BooleanExtendedFormats_ParsesCorrectly()
        {
            // Test "yes" and "no" formats
            _property.Value = "yes";
            Assert.That(_property.GetValue<bool>(), Is.True);

            _property.Value = "YES";
            Assert.That(_property.GetValue<bool>(), Is.True);

            _property.Value = "no";
            Assert.That(_property.GetValue<bool>(), Is.False);

            _property.Value = "NO";
            Assert.That(_property.GetValue<bool>(), Is.False);

            _property.Value = "1";
            Assert.That(_property.GetValue<bool>(), Is.True);

            _property.Value = "0";
            Assert.That(_property.GetValue<bool>(), Is.False);
        }

        #endregion

        #region Edge Case Tests - SetValueArray

        [Test]
        public void SetValueArray_NullArray_SetsEmptyArray()
        {
            // Act
#pragma warning disable CS8625
            _property.SetValueArray<string>(null);
#pragma warning restore CS8625

            // Assert
            Assert.That(_property.Value, Is.EqualTo("{}"));
        }

        [Test]
        public void SetValueArray_LargeArray_HandlesCorrectly()
        {
            // Arrange
            var largeArray = Enumerable.Range(1, 1000).ToArray();

            // Act
            _property.SetValueArray(largeArray);
            var result = _property.GetValueArray<int>();

            // Assert
            Assert.That(result, Is.EqualTo(largeArray));
        }

        [Test]
        public void SetValueArray_WithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var array = new[] { "value,1", "value{2}", "value}3", "value\"4\"", "value 5" };

            // Act
            _property.SetValueArray(array);
            var result = _property.GetValueArray<string>();

            // Assert
            Assert.That(result, Is.EqualTo(array));
        }

        #endregion

        #region GetValue<T> Additional Fast Path Tests (short, ushort, byte, sbyte, uint, ulong, char, DateTime, Guid)

        [Test]
        public void GetValue_Short_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "12345";

            // Act
            var result = _property.GetValue<short>();

            // Assert
            Assert.That(result, Is.EqualTo((short)12345));
        }

        [Test]
        public void GetValue_Short_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a number";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<short>());
        }

        [Test]
        public void GetValue_UShort_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "65535";

            // Act
            var result = _property.GetValue<ushort>();

            // Assert
            Assert.That(result, Is.EqualTo((ushort)65535));
        }

        [Test]
        public void GetValue_UShort_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "-1";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<ushort>());
        }

        [Test]
        public void GetValue_Byte_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "255";

            // Act
            var result = _property.GetValue<byte>();

            // Assert
            Assert.That(result, Is.EqualTo((byte)255));
        }

        [Test]
        public void GetValue_Byte_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "256";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<byte>());
        }

        [Test]
        public void GetValue_SByte_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "-128";

            // Act
            var result = _property.GetValue<sbyte>();

            // Assert
            Assert.That(result, Is.EqualTo((sbyte)-128));
        }

        [Test]
        public void GetValue_SByte_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "128";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<sbyte>());
        }

        [Test]
        public void GetValue_UInt_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "4294967295";

            // Act
            var result = _property.GetValue<uint>();

            // Assert
            Assert.That(result, Is.EqualTo(4294967295U));
        }

        [Test]
        public void GetValue_UInt_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "-1";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<uint>());
        }

        [Test]
        public void GetValue_ULong_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "18446744073709551615";

            // Act
            var result = _property.GetValue<ulong>();

            // Assert
            Assert.That(result, Is.EqualTo(18446744073709551615UL));
        }

        [Test]
        public void GetValue_ULong_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "-1";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<ulong>());
        }

        [Test]
        public void GetValue_Char_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "A";

            // Act
            var result = _property.GetValue<char>();

            // Assert
            Assert.That(result, Is.EqualTo('A'));
        }

        [Test]
        public void GetValue_Char_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "AB";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<char>());
        }

        [Test]
        public void GetValue_DateTime_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            _property.Value = "2024-12-25";

            // Act
            var result = _property.GetValue<DateTime>();

            // Assert
            Assert.That(result.Year, Is.EqualTo(2024));
            Assert.That(result.Month, Is.EqualTo(12));
            Assert.That(result.Day, Is.EqualTo(25));
        }

        [Test]
        public void GetValue_DateTime_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a date";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<DateTime>());
        }

        [Test]
        public void GetValue_Guid_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            _property.Value = guid.ToString();

            // Act
            var result = _property.GetValue<Guid>();

            // Assert
            Assert.That(result, Is.EqualTo(guid));
        }

        [Test]
        public void GetValue_Guid_InvalidInput_ThrowsFormatException()
        {
            // Arrange
            _property.Value = "not a guid";

            // Act & Assert
            Assert.Throws<FormatException>(() => _property.GetValue<Guid>());
        }

        [Test]
        public void TryGetValue_AdditionalTypes_ValidInput_ReturnsTrueAndValue()
        {
            // short
            _property.Value = "100";
            Assert.That(_property.TryGetValue<short>(out var shortVal), Is.True);
            Assert.That(shortVal, Is.EqualTo((short)100));

            // ushort
            _property.Value = "200";
            Assert.That(_property.TryGetValue<ushort>(out var ushortVal), Is.True);
            Assert.That(ushortVal, Is.EqualTo((ushort)200));

            // byte
            _property.Value = "50";
            Assert.That(_property.TryGetValue<byte>(out var byteVal), Is.True);
            Assert.That(byteVal, Is.EqualTo((byte)50));

            // sbyte
            _property.Value = "-50";
            Assert.That(_property.TryGetValue<sbyte>(out var sbyteVal), Is.True);
            Assert.That(sbyteVal, Is.EqualTo((sbyte)-50));

            // uint
            _property.Value = "1000000";
            Assert.That(_property.TryGetValue<uint>(out var uintVal), Is.True);
            Assert.That(uintVal, Is.EqualTo(1000000U));

            // ulong
            _property.Value = "9999999999";
            Assert.That(_property.TryGetValue<ulong>(out var ulongVal), Is.True);
            Assert.That(ulongVal, Is.EqualTo(9999999999UL));

            // char
            _property.Value = "X";
            Assert.That(_property.TryGetValue<char>(out var charVal), Is.True);
            Assert.That(charVal, Is.EqualTo('X'));

            // DateTime
            _property.Value = "2024-01-01";
            Assert.That(_property.TryGetValue<DateTime>(out var dateVal), Is.True);
            Assert.That(dateVal.Year, Is.EqualTo(2024));

            // Guid
            var guid = Guid.NewGuid();
            _property.Value = guid.ToString();
            Assert.That(_property.TryGetValue<Guid>(out var guidVal), Is.True);
            Assert.That(guidVal, Is.EqualTo(guid));
        }

        [Test]
        public void TryGetValue_AdditionalTypes_InvalidInput_ReturnsFalse()
        {
            _property.Value = "invalid";

            Assert.That(_property.TryGetValue<short>(out _), Is.False);
            Assert.That(_property.TryGetValue<ushort>(out _), Is.False);
            Assert.That(_property.TryGetValue<byte>(out _), Is.False);
            Assert.That(_property.TryGetValue<sbyte>(out _), Is.False);
            Assert.That(_property.TryGetValue<uint>(out _), Is.False);
            Assert.That(_property.TryGetValue<ulong>(out _), Is.False);
            Assert.That(_property.TryGetValue<DateTime>(out _), Is.False);
            Assert.That(_property.TryGetValue<Guid>(out _), Is.False);

            _property.Value = "AB"; // Too long for char
            Assert.That(_property.TryGetValue<char>(out _), Is.False);
        }

        #endregion
    }
}
