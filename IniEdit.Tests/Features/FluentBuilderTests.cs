using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace IniEdit.Tests.Features
{
    [TestFixture]
    public class FluentBuilderTests
    {
        [Test]
        public void DocumentBuilder_WithSection_CreatesSection()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Database", db => db
                    .WithProperty("Host", "localhost"))
                .Build();

            // Assert
            Assert.IsTrue(doc.HasSection("Database"));
            Assert.AreEqual("localhost", doc["Database"]["Host"].Value);
        }

        [Test]
        public void DocumentBuilder_WithDefaultProperty_AddsToDefaultSection()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithDefaultProperty("Version", "1.0")
                .Build();

            // Assert
            Assert.AreEqual(1, doc.DefaultSection.PropertyCount);
            Assert.AreEqual("1.0", doc.DefaultSection["Version"].Value);
        }

        [Test]
        public void DocumentBuilder_WithDefaultPropertyGeneric_ConvertsType()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithDefaultProperty("Port", 8080)
                .Build();

            // Assert
            Assert.AreEqual("8080", doc.DefaultSection["Port"].Value);
        }

        [Test]
        public void DocumentBuilder_MultipleSections_CreatesAll()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Section1", s => s.WithProperty("Key1", "Value1"))
                .WithSection("Section2", s => s.WithProperty("Key2", "Value2"))
                .WithSection("Section3", s => s.WithProperty("Key3", "Value3"))
                .Build();

            // Assert
            Assert.AreEqual(3, doc.SectionCount);
            Assert.IsTrue(doc.HasSection("Section1"));
            Assert.IsTrue(doc.HasSection("Section2"));
            Assert.IsTrue(doc.HasSection("Section3"));
        }

        [Test]
        public void SectionBuilder_WithProperty_AddsProperty()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Test", s => s
                    .WithProperty("Key1", "Value1")
                    .WithProperty("Key2", "Value2"))
                .Build();

            // Assert
            Assert.AreEqual(2, doc["Test"].PropertyCount);
        }

        [Test]
        public void SectionBuilder_WithPropertyGeneric_ConvertsType()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Test", s => s
                    .WithProperty("Port", 5432)
                    .WithProperty("Timeout", 30))
                .Build();

            // Assert
            Assert.AreEqual("5432", doc["Test"]["Port"].Value);
            Assert.AreEqual("30", doc["Test"]["Timeout"].Value);
        }

        [Test]
        public void SectionBuilder_WithQuotedProperty_SetsIsQuoted()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Test", s => s
                    .WithQuotedProperty("ConnectionString", "Server=localhost"))
                .Build();

            // Assert
            Assert.IsTrue(doc["Test"]["ConnectionString"].IsQuoted);
        }

        [Test]
        public void SectionBuilder_WithComment_SetsComment()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Test", s => s
                    .WithProperty("Key", "Value")
                    .WithComment("Section comment"))
                .Build();

            // Assert
            Assert.IsNotNull(doc["Test"].Comment);
            Assert.AreEqual("Section comment", doc["Test"].Comment!.Value);
        }

        [Test]
        public void SectionBuilder_WithPreComment_AddsPreComment()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithSection("Test", s => s
                    .WithPreComment("Comment line 1")
                    .WithPreComment("Comment line 2")
                    .WithProperty("Key", "Value"))
                .Build();

            // Assert
            Assert.AreEqual(2, doc["Test"].PreComments.Count);
            Assert.AreEqual("Comment line 1", doc["Test"].PreComments[0].Value);
            Assert.AreEqual("Comment line 2", doc["Test"].PreComments[1].Value);
        }

        [Test]
        public void DocumentBuilder_ImplicitConversion_Works()
        {
            // Act
            Document doc = new DocumentBuilder()
                .WithSection("Test", s => s.WithProperty("Key", "Value"));

            // Assert
            Assert.IsNotNull(doc);
            Assert.IsTrue(doc.HasSection("Test"));
        }

        [Test]
        public void DocumentBuilder_WithIniConfigOption_UsesOption()
        {
            // Arrange
            var option = new IniConfigOption
            {
                DefaultCommentPrefixChar = '#'
            };

            // Act
            var doc = new DocumentBuilder(option).Build();

            // Assert
            Assert.AreEqual('#', doc.DefaultCommentPrefixChar);
        }

        [Test]
        public void ToBuilder_ConvertsDocument_WithDefaultSection()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("DefaultKey", "DefaultValue");

            // Act
            var builder = doc.ToBuilder();
            var rebuilt = builder.Build();

            // Assert
            Assert.AreEqual(1, rebuilt.DefaultSection.PropertyCount);
            Assert.AreEqual("DefaultValue", rebuilt.DefaultSection["DefaultKey"].Value);
        }

        [Test]
        public void ToBuilder_ConvertsDocument_WithSections()
        {
            // Arrange
            var doc = new Document();
            var section = new Section("Test");
            section.AddProperty("Key", "Value");
            section.Comment = new Comment("Test comment");
            section.PreComments.Add(new Comment("Pre comment"));
            doc.AddSection(section);

            // Act
            var builder = doc.ToBuilder();
            var rebuilt = builder.Build();

            // Assert
            Assert.AreEqual(1, rebuilt.SectionCount);
            Assert.AreEqual("Value", rebuilt["Test"]["Key"].Value);
            Assert.AreEqual("Test comment", rebuilt["Test"].Comment!.Value);
            Assert.AreEqual(1, rebuilt["Test"].PreComments.Count);
        }

        [Test]
        public void ToBuilder_AllowsModification()
        {
            // Arrange
            var doc = new Document();
            doc["Section1"].AddProperty("Key1", "Value1");

            // Act
            var builder = doc.ToBuilder();
            var modified = builder
                .WithSection("Section2", s => s.WithProperty("Key2", "Value2"))
                .Build();

            // Assert
            Assert.AreEqual(2, modified.SectionCount);
            Assert.IsTrue(modified.HasSection("Section1"));
            Assert.IsTrue(modified.HasSection("Section2"));
        }

        [Test]
        public void DocumentBuilder_ComplexDocument_BuildsCorrectly()
        {
            // Act
            var doc = new DocumentBuilder()
                .WithDefaultProperty("Version", "2.0")
                .WithDefaultProperty("AppName", "TestApp")
                .WithSection("Database", db => db
                    .WithPreComment("Database configuration")
                    .WithProperty("Host", "localhost")
                    .WithProperty("Port", 5432)
                    .WithQuotedProperty("ConnectionString", "Server=localhost;Database=test")
                    .WithComment("Primary database"))
                .WithSection("Logging", log => log
                    .WithProperty("Level", "Info")
                    .WithProperty("File", "/var/log/app.log")
                    .WithComment("Logging settings"))
                .Build();

            // Assert
            Assert.AreEqual(2, doc.DefaultSection.PropertyCount);
            Assert.AreEqual(2, doc.SectionCount);
            Assert.AreEqual("2.0", doc.DefaultSection["Version"].Value);
            Assert.AreEqual("localhost", doc["Database"]["Host"].Value);
            Assert.AreEqual("5432", doc["Database"]["Port"].Value);
            Assert.IsTrue(doc["Database"]["ConnectionString"].IsQuoted);
            Assert.AreEqual("Primary database", doc["Database"].Comment!.Value);
            Assert.AreEqual(1, doc["Database"].PreComments.Count);
        }

        [Test]
        public void DocumentBuilder_WithSection_ThrowsOnEmptyName()
        {
            // Arrange
            var builder = new DocumentBuilder();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.WithSection("", s => s.WithProperty("Key", "Value")));
        }

        [Test]
        public void DocumentBuilder_WithSection_ThrowsOnNullConfigure()
        {
            // Arrange
            var builder = new DocumentBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.WithSection("Test", null!));
        }

        [Test]
        public void DocumentBuilder_WithDefaultProperty_ThrowsOnEmptyKey()
        {
            // Arrange
            var builder = new DocumentBuilder();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.WithDefaultProperty("", "Value"));
        }

        [Test]
        public void SectionBuilder_WithProperty_ThrowsOnEmptyKey()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new DocumentBuilder()
                    .WithSection("Test", s => s.WithProperty("", "Value"))
                    .Build());
        }

        [Test]
        public void ToBuilder_ThrowsOnNull()
        {
            // Arrange
            Document? doc = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => doc!.ToBuilder());
        }
    }
}
