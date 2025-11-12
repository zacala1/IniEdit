using IniEdit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IniEdit.Tests
{
    [TestFixture]
    public class IniConfigManagerParsingErrorTests
    {
        private List<ParsingErrorEventArgs> _errors;

        [SetUp]
        public void Setup()
        {
            _errors = new List<ParsingErrorEventArgs>();
            IniConfigManager.ParsingError += OnParsingError;
        }

        [TearDown]
        public void Cleanup()
        {
            IniConfigManager.ParsingError -= OnParsingError;
            _errors.Clear();
        }

        private void OnParsingError(object? sender, ParsingErrorEventArgs e)
        {
            _errors.Add(e);
        }

        private void AssertError(ParsingErrorEventArgs error, int lineNumber, string line, string reason)
        {
            Assert.That(error.LineNumber, Is.EqualTo(lineNumber), "LineNumber mismatch");
            Assert.That(error.Line, Is.EqualTo(line), "Line content mismatch");
            Assert.That(error.Reason, Is.EqualTo(reason), "Error reason mismatch");
        }

        [Test]
        public void Parse_MissingClosingBracket_RaisesParsingError()
        {
            // Arrange
            var line = "[Section";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Missing closing bracket in section declaration"
            );
        }

        [Test]
        public void Parse_MissingEqualsSign_RaisesParsingError()
        {
            // Arrange
            var line = "keyvalue";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Missing equals sign in key-value pair"
            );
        }

        [Test]
        public void Parse_EmptyKey_RaisesParsingError()
        {
            // Arrange
            var line = "=value";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Key is empty"
            );
        }

        [Test]
        public void Parse_UnterminatedQuote_RaisesParsingError()
        {
            // Arrange
            var line = "key=\"value";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Unterminated quote: missing closing quotation mark"
            );
        }

        [Test]
        public void Parse_IncompleteEscapeSequence_RaisesParsingError()
        {
            // Arrange
            var line = "key=\"value\\";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Invalid escape sequence: incomplete escape marker"
            );
        }

        [Test]
        public void Parse_ContentAfterQuote_RaisesParsingError([Values("invalid", " invalid", "\tinvalid")] string extraContent)
        {
            // Arrange
            var line = $"key=\"value\"{extraContent}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Invalid quote format"
            );
        }

        [Test]
        public void Parse_InvalidContentBeforeComment_RaisesParsingError()
        {
            // Arrange
            var line = "key=\"value\" invalid ;comment";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(line));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(1));
            AssertError(
                _errors[0],
                lineNumber: 1,
                line: line,
                reason: "Invalid content after closing quote"
            );
        }

        [Test]
        public void Parse_MultipleErrors_RaisesAllErrorsInOrder()
        {
            // Arrange
            var content = @"[Section
key1
=value2
key3=""unterminated
key4=""value4""invalid";

            var expectedErrors = new[]
            {
            (1, "[Section", "Missing closing bracket in section declaration"),
            (2, "key1", "Missing equals sign in key-value pair"),
            (3, "=value2", "Key is empty"),
            (4, "key3=\"unterminated", "Unterminated quote: missing closing quotation mark"),
            (5, "key4=\"value4\"invalid", "Invalid quote format")
        };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(expectedErrors.Length));

            for (int i = 0; i < _errors.Count; i++)
            {
                AssertError(
                    _errors[i],
                    expectedErrors[i].Item1,
                    expectedErrors[i].Item2,
                    expectedErrors[i].Item3
                );
            }
        }

        [Test]
        public void Parse_ValidContent_NoErrors([Values] bool includeComments)
        {
            // Arrange
            var content = includeComments ?
                @"[Section] ; section comment
key1=value1 ; simple value
key2=""quoted value"" ; quoted value
key3=""escaped\nvalue""" :
                @"[Section]
key1=value1
key2=""quoted value""
key3=""escaped\nvalue""";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Is.Empty);
        }

        [Test]
        public void Parse_CorrectLineNumbersAcrossEmptyLines()
        {
            // Arrange
            var content = @"key1=value1

[Section

key2
=value3";

            var expectedErrors = new[]
            {
            (3, "[Section", "Missing closing bracket in section declaration"),
            (5, "key2", "Missing equals sign in key-value pair"),
            (6, "=value3", "Key is empty")
        };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Has.Count.EqualTo(expectedErrors.Length));

            for (int i = 0; i < _errors.Count; i++)
            {
                AssertError(
                    _errors[i],
                    expectedErrors[i].Item1,
                    expectedErrors[i].Item2,
                    expectedErrors[i].Item3
                );
            }
        }

        [Test]
        public void Parse_AllEscapeSequences_NoErrors()
        {
            // Arrange
            var content = @"key=""\0\a\b\t\r\n\;\#\""\\""";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var doc = IniConfigManager.Load(stream, Encoding.UTF8);

            // Assert
            Assert.That(_errors, Is.Empty);
        }
    }
}