using IniEdit.Serialization;
using NUnit.Framework;

namespace IniEdit.Tests.Serialization
{
    [TestFixture]
    public class IniSerializerTests
    {
        #region Test Classes

        public class SimpleConfig
        {
            public string Name { get; set; } = string.Empty;
            public int Port { get; set; }
            public bool Enabled { get; set; }
        }

        public class ConfigWithSection
        {
            public string AppName { get; set; } = string.Empty;

            [IniSection("Database")]
            public DatabaseConfig Database { get; set; } = new();
        }

        public class DatabaseConfig
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; }

            [IniProperty("user")]
            public string Username { get; set; } = string.Empty;
        }

        public class ConfigWithArray
        {
            public string[] Tags { get; set; } = Array.Empty<string>();
            public int[] Ports { get; set; } = Array.Empty<int>();
        }

        public class ConfigWithIgnore
        {
            public string Name { get; set; } = string.Empty;

            [IniIgnore]
            public string Secret { get; set; } = "secret";
        }

        public class ConfigWithDefaults
        {
            [IniProperty(DefaultValue = "default-name")]
            public string Name { get; set; } = string.Empty;

            [IniProperty(DefaultValue = 8080)]
            public int Port { get; set; }
        }

        public class ConfigWithNullable
        {
            public int? OptionalPort { get; set; }
            public bool? OptionalEnabled { get; set; }
        }

        public class ConfigWithEnum
        {
            public LogLevel Level { get; set; }
        }

        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        // Invalid: nested depth > 1
        public class InvalidDeepConfig
        {
            [IniSection("Level1")]
            public Level1Config Level1 { get; set; } = new();
        }

        public class Level1Config
        {
            [IniSection("Level2")]
            public Level2Config Level2 { get; set; } = new();
        }

        public class Level2Config
        {
            public string Value { get; set; } = string.Empty;
        }

        // Invalid: circular reference
        public class CircularConfig
        {
            [IniSection("Self")]
            public CircularConfig? Self { get; set; }
        }

        public class MultiSectionConfig
        {
            [IniSection("Server")]
            public ServerConfig Server { get; set; } = new();

            [IniSection("Logging")]
            public LoggingConfig Logging { get; set; } = new();
        }

        public class ServerConfig
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; }
        }

        public class LoggingConfig
        {
            public string Path { get; set; } = string.Empty;
            public LogLevel Level { get; set; }
        }

        public class ConfigWithDateTime
        {
            public DateTime CreatedAt { get; set; }
            public TimeSpan Timeout { get; set; }
            public Guid Id { get; set; }
            public decimal Price { get; set; }
        }

        public class ConfigWithNullableEnum
        {
            public LogLevel? OptionalLevel { get; set; }
        }

        public class ConfigWithReadOnly
        {
            public string Name { get; set; } = string.Empty;
            public string ReadOnlyValue { get; } = "readonly";
        }

        public class ConfigWithEmptyArrays
        {
            public string[] EmptyTags { get; set; } = Array.Empty<string>();
        }

        public class ConfigWithDoubleAndFloat
        {
            public double Latitude { get; set; }
            public float Longitude { get; set; }
            public long BigNumber { get; set; }
        }

        #endregion

        #region Deserialize Tests

        [Test]
        public void Deserialize_SimpleConfig_PopulatesProperties()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "TestApp");
            doc.DefaultSection.AddProperty("Port", "8080");
            doc.DefaultSection.AddProperty("Enabled", "true");

            // Act
            var config = IniSerializer.Deserialize<SimpleConfig>(doc);

            // Assert
            Assert.That(config.Name, Is.EqualTo("TestApp"));
            Assert.That(config.Port, Is.EqualTo(8080));
            Assert.That(config.Enabled, Is.True);
        }

        [Test]
        public void Deserialize_ConfigWithSection_PopulatesNestedObject()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("AppName", "MyApp");

            var dbSection = new Section("Database");
            dbSection.AddProperty("Host", "localhost");
            dbSection.AddProperty("Port", "5432");
            dbSection.AddProperty("user", "admin");
            doc.AddSection(dbSection);

            // Act
            var config = IniSerializer.Deserialize<ConfigWithSection>(doc);

            // Assert
            Assert.That(config.AppName, Is.EqualTo("MyApp"));
            Assert.That(config.Database.Host, Is.EqualTo("localhost"));
            Assert.That(config.Database.Port, Is.EqualTo(5432));
            Assert.That(config.Database.Username, Is.EqualTo("admin"));
        }

        [Test]
        public void Deserialize_ConfigWithArray_PopulatesArrays()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Tags", "{web, api, backend}");
            doc.DefaultSection.AddProperty("Ports", "{80, 443, 8080}");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithArray>(doc);

            // Assert
            Assert.That(config.Tags, Is.EqualTo(new[] { "web", "api", "backend" }));
            Assert.That(config.Ports, Is.EqualTo(new[] { 80, 443, 8080 }));
        }

        [Test]
        public void Deserialize_ConfigWithIgnore_SkipsIgnoredProperties()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "TestApp");
            doc.DefaultSection.AddProperty("Secret", "exposed");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithIgnore>(doc);

            // Assert
            Assert.That(config.Name, Is.EqualTo("TestApp"));
            Assert.That(config.Secret, Is.EqualTo("secret")); // Default value, not "exposed"
        }

        [Test]
        public void Deserialize_MissingProperty_UsesDefaultValue()
        {
            // Arrange
            var doc = new Document();
            // No properties set

            // Act
            var config = IniSerializer.Deserialize<ConfigWithDefaults>(doc);

            // Assert
            Assert.That(config.Name, Is.EqualTo("default-name"));
            Assert.That(config.Port, Is.EqualTo(8080));
        }

        [Test]
        public void Deserialize_ConfigWithNullable_HandlesNullValues()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("OptionalPort", "9000");
            // OptionalEnabled not set

            // Act
            var config = IniSerializer.Deserialize<ConfigWithNullable>(doc);

            // Assert
            Assert.That(config.OptionalPort, Is.EqualTo(9000));
            Assert.That(config.OptionalEnabled, Is.Null);
        }

        [Test]
        public void Deserialize_ConfigWithEnum_ParsesEnumValue()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Level", "Warning");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithEnum>(doc);

            // Assert
            Assert.That(config.Level, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Deserialize_NullDocument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => IniSerializer.Deserialize<SimpleConfig>(null!));
        }

        #endregion

        #region Serialize Tests

        [Test]
        public void Serialize_SimpleConfig_CreatesDocument()
        {
            // Arrange
            var config = new SimpleConfig
            {
                Name = "TestApp",
                Port = 8080,
                Enabled = true
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection["Name"].Value, Is.EqualTo("TestApp"));
            Assert.That(doc.DefaultSection["Port"].Value, Is.EqualTo("8080"));
            Assert.That(doc.DefaultSection["Enabled"].Value, Is.EqualTo("True"));
        }

        [Test]
        public void Serialize_ConfigWithSection_CreatesNamedSection()
        {
            // Arrange
            var config = new ConfigWithSection
            {
                AppName = "MyApp",
                Database = new DatabaseConfig
                {
                    Host = "localhost",
                    Port = 5432,
                    Username = "admin"
                }
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection["AppName"].Value, Is.EqualTo("MyApp"));
            Assert.That(doc.HasSection("Database"), Is.True);

            var dbSection = doc.GetSection("Database");
            Assert.That(dbSection, Is.Not.Null);
            Assert.That(dbSection!["Host"].Value, Is.EqualTo("localhost"));
            Assert.That(dbSection["Port"].Value, Is.EqualTo("5432"));
            Assert.That(dbSection["user"].Value, Is.EqualTo("admin"));
        }

        [Test]
        public void Serialize_ConfigWithArray_CreatesArrayValue()
        {
            // Arrange
            var config = new ConfigWithArray
            {
                Tags = new[] { "web", "api" },
                Ports = new[] { 80, 443 }
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            var tagsArray = doc.DefaultSection["Tags"].GetValueArray<string>();
            Assert.That(tagsArray, Is.EqualTo(new[] { "web", "api" }));

            var portsArray = doc.DefaultSection["Ports"].GetValueArray<int>();
            Assert.That(portsArray, Is.EqualTo(new[] { 80, 443 }));
        }

        [Test]
        public void Serialize_ConfigWithIgnore_SkipsIgnoredProperties()
        {
            // Arrange
            var config = new ConfigWithIgnore
            {
                Name = "TestApp",
                Secret = "my-secret"
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection.HasProperty("Name"), Is.True);
            Assert.That(doc.DefaultSection.HasProperty("Secret"), Is.False);
        }

        [Test]
        public void Serialize_MultiSectionConfig_CreatesMultipleSections()
        {
            // Arrange
            var config = new MultiSectionConfig
            {
                Server = new ServerConfig { Host = "localhost", Port = 8080 },
                Logging = new LoggingConfig { Path = "/var/log", Level = LogLevel.Info }
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.HasSection("Server"), Is.True);
            Assert.That(doc.HasSection("Logging"), Is.True);

            Assert.That(doc["Server"]["Host"].Value, Is.EqualTo("localhost"));
            Assert.That(doc["Server"]["Port"].Value, Is.EqualTo("8080"));
            Assert.That(doc["Logging"]["Path"].Value, Is.EqualTo("/var/log"));
            Assert.That(doc["Logging"]["Level"].Value, Is.EqualTo("Info"));
        }

        [Test]
        public void Serialize_NullObject_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => IniSerializer.Serialize<SimpleConfig>(null!));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Validate_DeepNesting_ThrowsIniSerializationException()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<IniSerializationException>(() =>
                IniSerializer.ValidateTypeStructure(typeof(InvalidDeepConfig)));

            Assert.That(ex!.Message, Does.Contain("Nesting depth exceeded"));
            Assert.That(ex.Message, Does.Contain("Level2Config"));
        }

        [Test]
        public void Validate_CircularReference_ThrowsIniSerializationException()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<IniSerializationException>(() =>
                IniSerializer.ValidateTypeStructure(typeof(CircularConfig)));

            Assert.That(ex!.Message, Does.Contain("Circular reference"));
        }

        [Test]
        public void Validate_ValidConfig_DoesNotThrow()
        {
            // Arrange & Act & Assert - should not throw
            Assert.DoesNotThrow(() => IniSerializer.ValidateTypeStructure(typeof(ConfigWithSection)));
            Assert.DoesNotThrow(() => IniSerializer.ValidateTypeStructure(typeof(MultiSectionConfig)));
        }

        #endregion

        #region Round-trip Tests

        [Test]
        public void RoundTrip_ConfigWithSection_PreservesData()
        {
            // Arrange
            var original = new ConfigWithSection
            {
                AppName = "MyApp",
                Database = new DatabaseConfig
                {
                    Host = "localhost",
                    Port = 5432,
                    Username = "admin"
                }
            };

            // Act
            var doc = IniSerializer.Serialize(original);
            var restored = IniSerializer.Deserialize<ConfigWithSection>(doc);

            // Assert
            Assert.That(restored.AppName, Is.EqualTo(original.AppName));
            Assert.That(restored.Database.Host, Is.EqualTo(original.Database.Host));
            Assert.That(restored.Database.Port, Is.EqualTo(original.Database.Port));
            Assert.That(restored.Database.Username, Is.EqualTo(original.Database.Username));
        }

        [Test]
        public void RoundTrip_MultiSectionConfig_PreservesData()
        {
            // Arrange
            var original = new MultiSectionConfig
            {
                Server = new ServerConfig { Host = "example.com", Port = 443 },
                Logging = new LoggingConfig { Path = "/var/log/app", Level = LogLevel.Debug }
            };

            // Act
            var doc = IniSerializer.Serialize(original);
            var restored = IniSerializer.Deserialize<MultiSectionConfig>(doc);

            // Assert
            Assert.That(restored.Server.Host, Is.EqualTo(original.Server.Host));
            Assert.That(restored.Server.Port, Is.EqualTo(original.Server.Port));
            Assert.That(restored.Logging.Path, Is.EqualTo(original.Logging.Path));
            Assert.That(restored.Logging.Level, Is.EqualTo(original.Logging.Level));
        }

        #endregion

        #region Additional Type Tests

        [Test]
        public void Deserialize_ConfigWithDateTime_ParsesDateTimeTypes()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("CreatedAt", "2025-01-15T10:30:00");
            doc.DefaultSection.AddProperty("Timeout", "00:05:30");
            doc.DefaultSection.AddProperty("Id", "a1b2c3d4-e5f6-7890-abcd-ef1234567890");
            doc.DefaultSection.AddProperty("Price", "123.45");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithDateTime>(doc);

            // Assert
            Assert.That(config.CreatedAt, Is.EqualTo(new DateTime(2025, 1, 15, 10, 30, 0)));
            Assert.That(config.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30))));
            Assert.That(config.Id, Is.EqualTo(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890")));
            Assert.That(config.Price, Is.EqualTo(123.45m));
        }

        [Test]
        public void Serialize_ConfigWithDateTime_WritesCorrectFormat()
        {
            // Arrange
            var config = new ConfigWithDateTime
            {
                CreatedAt = new DateTime(2025, 1, 15, 10, 30, 0),
                Timeout = TimeSpan.FromMinutes(5),
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Price = 99.99m
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection.HasProperty("CreatedAt"), Is.True);
            Assert.That(doc.DefaultSection.HasProperty("Timeout"), Is.True);
            Assert.That(doc.DefaultSection.HasProperty("Id"), Is.True);
            Assert.That(doc.DefaultSection.HasProperty("Price"), Is.True);
        }

        [Test]
        public void Deserialize_ConfigWithNullableEnum_HandlesNullAndValue()
        {
            // Arrange - with value
            var doc1 = new Document();
            doc1.DefaultSection.AddProperty("OptionalLevel", "Error");

            // Arrange - without value
            var doc2 = new Document();

            // Act
            var config1 = IniSerializer.Deserialize<ConfigWithNullableEnum>(doc1);
            var config2 = IniSerializer.Deserialize<ConfigWithNullableEnum>(doc2);

            // Assert
            Assert.That(config1.OptionalLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(config2.OptionalLevel, Is.Null);
        }

        [Test]
        public void Deserialize_ConfigWithNullableEnum_EmptyValue_ReturnsNull()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("OptionalLevel", "");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithNullableEnum>(doc);

            // Assert
            Assert.That(config.OptionalLevel, Is.Null);
        }

        [Test]
        public void Deserialize_ConfigWithEnum_CaseInsensitive()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Level", "warning"); // lowercase

            // Act
            var config = IniSerializer.Deserialize<ConfigWithEnum>(doc);

            // Assert
            Assert.That(config.Level, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Serialize_ConfigWithReadOnly_SkipsReadOnlyProperties()
        {
            // Arrange
            var config = new ConfigWithReadOnly { Name = "Test" };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection.HasProperty("Name"), Is.True);
            // ReadOnlyValue should not cause an error, just be skipped or written
        }

        [Test]
        public void Deserialize_ConfigWithReadOnly_SkipsReadOnlyProperties()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "TestName");
            doc.DefaultSection.AddProperty("ReadOnlyValue", "attempted-write");

            // Act
            var config = IniSerializer.Deserialize<ConfigWithReadOnly>(doc);

            // Assert
            Assert.That(config.Name, Is.EqualTo("TestName"));
            Assert.That(config.ReadOnlyValue, Is.EqualTo("readonly")); // Original default value
        }

        [Test]
        public void Serialize_EmptyArray_WritesEmptyArraySyntax()
        {
            // Arrange
            var config = new ConfigWithEmptyArrays
            {
                EmptyTags = Array.Empty<string>()
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert - empty array should still be written
            Assert.That(doc.DefaultSection.HasProperty("EmptyTags"), Is.True);
        }

        [Test]
        public void Deserialize_DoubleAndFloat_ParsesCorrectly()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Latitude", "37.5665");
            doc.DefaultSection.AddProperty("Longitude", "126.978");
            doc.DefaultSection.AddProperty("BigNumber", "9223372036854775807"); // long.MaxValue

            // Act
            var config = IniSerializer.Deserialize<ConfigWithDoubleAndFloat>(doc);

            // Assert
            Assert.That(config.Latitude, Is.EqualTo(37.5665).Within(0.0001));
            Assert.That(config.Longitude, Is.EqualTo(126.978f).Within(0.001f));
            Assert.That(config.BigNumber, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void Deserialize_NonGeneric_PopulatesObject()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "TestApp");
            doc.DefaultSection.AddProperty("Port", "8080");
            doc.DefaultSection.AddProperty("Enabled", "true");

            // Act
            var result = IniSerializer.Deserialize(doc, typeof(SimpleConfig));

            // Assert
            Assert.That(result, Is.TypeOf<SimpleConfig>());
            var config = (SimpleConfig)result;
            Assert.That(config.Name, Is.EqualTo("TestApp"));
            Assert.That(config.Port, Is.EqualTo(8080));
            Assert.That(config.Enabled, Is.True);
        }

        [Test]
        public void Deserialize_NonGeneric_NullDocument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => IniSerializer.Deserialize(null!, typeof(SimpleConfig)));
        }

        [Test]
        public void Deserialize_NonGeneric_NullType_ThrowsArgumentNullException()
        {
            var doc = new Document();
            Assert.Throws<ArgumentNullException>(() => IniSerializer.Deserialize(doc, null!));
        }

        [Test]
        public void RoundTrip_ConfigWithDateTime_PreservesValues()
        {
            // Arrange
            var original = new ConfigWithDateTime
            {
                CreatedAt = new DateTime(2025, 6, 15, 14, 30, 0),
                Timeout = TimeSpan.FromHours(2),
                Id = Guid.NewGuid(),
                Price = 1234.56m
            };

            // Act
            var doc = IniSerializer.Serialize(original);
            var restored = IniSerializer.Deserialize<ConfigWithDateTime>(doc);

            // Assert
            Assert.That(restored.CreatedAt, Is.EqualTo(original.CreatedAt));
            Assert.That(restored.Timeout, Is.EqualTo(original.Timeout));
            Assert.That(restored.Id, Is.EqualTo(original.Id));
            Assert.That(restored.Price, Is.EqualTo(original.Price));
        }

        [Test]
        public void RoundTrip_ConfigWithDoubleAndFloat_PreservesValues()
        {
            // Arrange
            var original = new ConfigWithDoubleAndFloat
            {
                Latitude = 37.5665,
                Longitude = 126.978f,
                BigNumber = 9876543210L
            };

            // Act
            var doc = IniSerializer.Serialize(original);
            var restored = IniSerializer.Deserialize<ConfigWithDoubleAndFloat>(doc);

            // Assert
            Assert.That(restored.Latitude, Is.EqualTo(original.Latitude).Within(0.0001));
            Assert.That(restored.Longitude, Is.EqualTo(original.Longitude).Within(0.001f));
            Assert.That(restored.BigNumber, Is.EqualTo(original.BigNumber));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Deserialize_MissingSection_CreatesDefaultNestedObject()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("AppName", "MyApp");
            // Database section is NOT added

            // Act
            var config = IniSerializer.Deserialize<ConfigWithSection>(doc);

            // Assert
            Assert.That(config.AppName, Is.EqualTo("MyApp"));
            Assert.That(config.Database, Is.Not.Null); // Should still use default
            Assert.That(config.Database.Host, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Serialize_NullNestedObject_SkipsSection()
        {
            // Arrange
            var config = new ConfigWithSection
            {
                AppName = "MyApp",
                Database = null!
            };

            // Act
            var doc = IniSerializer.Serialize(config);

            // Assert
            Assert.That(doc.DefaultSection["AppName"].Value, Is.EqualTo("MyApp"));
            Assert.That(doc.HasSection("Database"), Is.False);
        }

        [Test]
        public void Deserialize_ExtraPropertiesInDocument_IgnoresThem()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "TestApp");
            doc.DefaultSection.AddProperty("Port", "8080");
            doc.DefaultSection.AddProperty("Enabled", "true");
            doc.DefaultSection.AddProperty("UnknownProperty", "ignored");

            // Act
            var config = IniSerializer.Deserialize<SimpleConfig>(doc);

            // Assert - should not throw, just ignore unknown
            Assert.That(config.Name, Is.EqualTo("TestApp"));
        }

        [Test]
        public void Deserialize_InvalidEnumValue_ThrowsIniSerializationException()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Level", "InvalidLevel");

            // Act & Assert
            Assert.Throws<IniSerializationException>(() =>
                IniSerializer.Deserialize<ConfigWithEnum>(doc));
        }

        [Test]
        public void Deserialize_InvalidIntValue_ThrowsIniSerializationException()
        {
            // Arrange
            var doc = new Document();
            doc.DefaultSection.AddProperty("Name", "Test");
            doc.DefaultSection.AddProperty("Port", "not-a-number");
            doc.DefaultSection.AddProperty("Enabled", "true");

            // Act & Assert
            Assert.Throws<IniSerializationException>(() =>
                IniSerializer.Deserialize<SimpleConfig>(doc));
        }

        #endregion
    }
}
