# IniEdit

INI configuration file parser and editor for .NET 8.0+.

Supports reading, writing, and manipulating INI files with full comment preservation, type conversion, duplicate handling policies, schema validation, object serialization, and export to JSON/XML/CSV.

## Installation

```bash
dotnet add package IniEdit
```

## Quick Start

```csharp
using IniEdit;

// Load
var doc = IniConfigManager.Load("config.ini");

// Read
string host = doc["Database"]["Host"].Value;
int port = doc["Database"]["Port"].GetValue<int>();

// Write
doc["Database"]["Host"].Value = "newhost";
doc["Database"].SetProperty("Port", 5432);

// Save
IniConfigManager.Save("config.ini", doc);
```

---

## Loading

```csharp
// Basic load (UTF-8)
var doc = IniConfigManager.Load("config.ini");

// With encoding
var doc = IniConfigManager.Load("config.ini", Encoding.Latin1);

// From stream
using var stream = File.OpenRead("config.ini");
var doc = IniConfigManager.Load(stream, Encoding.UTF8);

// Async
var doc = await IniConfigManager.LoadAsync("config.ini");
var doc = await IniConfigManager.LoadAsync("config.ini", cancellationToken: cts.Token);

// With full options
var doc = IniConfigManager.LoadWithOptions("config.ini", new LoadOptions
{
    Encoding = Encoding.UTF8,
    FileShare = FileShare.Read,
    ConfigOption = new IniConfigOption { DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin },
    SectionFilter = name => name != "Internal"   // exclude sections by name
});
```

## Saving

```csharp
// Basic save
IniConfigManager.Save("config.ini", doc);

// With options
IniConfigManager.Save("config.ini", doc, new SaveOptions
{
    KeyValueSeparator = " = ",           // default: " = "
    BlankLinesBetweenSections = 1,       // default: 1
    BlankLineAfterDefaultSection = true, // default: true
    NormalizeCommentPrefix = false,      // default: false — keep original prefix
    SpaceBeforeInlineComment = true      // default: true — "key = value ; comment"
});

// Async
await IniConfigManager.SaveAsync("config.ini", doc);

// To stream or TextWriter
IniConfigManager.Save(stream, doc);
IniConfigManager.Save(textWriter, doc);
```

---

## Sections and Properties

```csharp
// Add section
doc.AddSection("Database");
doc.AddSection(new Section("Database"));

// Check and get
bool exists = doc.HasSection("Database");
Section? s = doc.GetSection("Database");         // null if not found
bool found = doc.TryGetSection("Database", out var section);

// Section indexer — auto-creates if not found
doc["Database"]["Host"].Value = "localhost";

// Safe access pattern
if (doc.TryGetSection("Database", out var db))
{
    if (db.TryGetProperty("Port", out var prop))
        Console.WriteLine(prop.Value);
}

// Remove
doc.RemoveSection("Database");
doc.Clear();

// Add properties
section.AddProperty("Host", "localhost");
section.AddProperty(new Property("Port", "5432"));

// Set (create or update)
section.SetProperty("Host", "localhost");
section.SetProperty("Port", 5432);

// Remove
section.RemoveProperty("Host");
```

### Shorthand on Document

```csharp
// Set value — creates section/property if missing
doc.SetValue("Database", "Host", "localhost");
doc.SetValue<int>("Database", "Port", 5432);

// Get value
string host = doc.GetValue<string>("Database", "Host");
int port = doc.GetValueOrDefault<int>("Database", "Port", defaultValue: 5432);
bool ok = doc.TryGetValue<int>("Database", "Port", out int portVal);
```

---

## Type Conversion

`GetValue<T>()` supports: `string`, `bool`, `int`, `long`, `short`, `byte`, `float`, `double`, `decimal`, `char`, `uint`, `ulong`, `ushort`, `sbyte`, `DateTime`, `Guid`, enums.

```csharp
int port    = prop.GetValue<int>();
bool flag   = prop.GetValue<bool>();        // true/false/1/0/yes/no
DateTime dt = prop.GetValue<DateTime>();
Guid id     = prop.GetValue<Guid>();
MyEnum val  = prop.GetValue<MyEnum>();      // case-insensitive

// Fallbacks
int port = prop.GetValueOrDefault<int>(8080);
int port = prop.GetValueOrDefault<int>();   // default(T)
bool ok  = prop.TryGetValue<int>(out int v);

// Section-level helpers
int port = section.GetPropertyValue<int>("Port");
int port = section.GetPropertyValueOrDefault<int>("Port", 8080);
```

## Arrays

Array format: `{value1, value2, "quoted value"}`

```csharp
// Read
int[] ports = prop.GetValueArray<int>();
string[] tags = prop.GetValueArray<string>(maxElements: 100);

// Write
prop.SetValueArray(new[] { 80, 443, 8080 });
// Output: {80, 443, 8080}
```

---

## Comments

```csharp
// Pre-comment (lines before the element)
section.PreComments.Add(new Comment("Database settings"));
section.PreComments.Add(new Comment("#", "also supports # prefix"));

// Inline comment
section.Comment = new Comment("main db");
prop.Comment = new Comment("port number");

// Output:
// ; Database settings
// # also supports # prefix
// [Database]  ; main db
// Port = 5432 ; port number

// Multi-line pre-comment
section.PreComments.TrySetMultiLineText("Line one\nLine two");
```

---

## Parsing Options

```csharp
var option = new IniConfigOption
{
    // Duplicate handling
    DuplicateKeyPolicy     = DuplicateKeyPolicyType.FirstWin,   // FirstWin | LastWin | ThrowError
    DuplicateSectionPolicy = DuplicateSectionPolicyType.Merge,  // FirstWin | LastWin | Merge | ThrowError

    // Error collection (instead of throwing on first error)
    CollectParsingErrors = true,
    MaxParsingErrors     = 50,    // 0 = unlimited

    // Security limits (0 = unlimited)
    MaxSections              = 500,
    MaxPropertiesPerSection  = 1000,
    MaxValueLength           = 4096,
    MaxLineLength            = 8192,
    MaxPendingComments       = 100,

    // Comment characters
    CommentPrefixChars      = new[] { ';', '#' },
    DefaultCommentPrefixChar = ';'
};

var doc = IniConfigManager.Load("config.ini", option);

// Inspect collected errors
foreach (var error in doc.ParsingErrors)
    Console.WriteLine($"Line {error.LineNumber}: {error.Reason}");
```

### Parse error event

```csharp
IniConfigManager.ParsingError += (s, e) =>
    Console.WriteLine($"Line {e.LineNumber}: {e.Reason}");
```

---

## Serialization

Maps C# objects to INI documents using attributes. Maximum nesting depth is 1 (root properties → default section; complex-type properties → named sections).

**Supported types:** primitives, `string`, `enum`, `DateTime`, `Guid`, and arrays of these.

```csharp
public class AppConfig
{
    public string AppName { get; set; } = "";    // → default section

    [IniSection("Database")]
    public DbConfig Database { get; set; } = new();  // → [Database] section
}

public class DbConfig
{
    [IniProperty("Host")]
    public string Host { get; set; } = "localhost";

    [IniProperty(DefaultValue = 5432)]
    public int Port { get; set; }

    [IniIgnore]
    public string InternalNote { get; set; } = "";  // not serialized
}
```

```csharp
// Serialize object → document
var config = new AppConfig { AppName = "MyApp", Database = new DbConfig { Host = "db1" } };
Document doc = IniSerializer.Serialize(config);
IniConfigManager.Save("config.ini", doc);

// Deserialize document → object
var doc = IniConfigManager.Load("config.ini");
var config = IniSerializer.Deserialize<AppConfig>(doc);
Console.WriteLine(config.Database.Host); // db1
```

---

## Schema Validation

```csharp
var schema = new IniSchema();

schema.DefineSection("Database", required: true)
    .DefineProperty("Host",    typeof(string), required: true)
    .DefineProperty("Port",    typeof(int),    required: true)
        .WithRange(1, 65535)
    .DefineProperty("Driver")
        .WithAllowedValues("sqlite", "postgres", "mysql");

schema.DefineSection("Logging")
    .DefineProperty("Level")
        .WithPattern(@"^(debug|info|warn|error)$")
    .DefineProperty("MaxFileSize", typeof(long))
        .WithRange(min: 1024);

schema.AllowUndefinedSections   = false;
schema.AllowUndefinedProperties = false;

var result = schema.Validate(doc);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"[{error.ErrorType}] {error.SectionName}.{error.PropertyKey}: {error.Message}");
}
```

**Error types:** `MissingRequiredSection`, `MissingRequiredProperty`, `UndefinedSection`, `UndefinedProperty`, `TypeMismatch`, `ValueNotAllowed`, `PatternMismatch`, `ValueOutOfRange`, `ValidationFailed`

---

## Export

```csharp
// JSON
string json = DocumentExporter.ToJson(doc);
string json = DocumentExporter.ToJson(doc, new JsonExportOptions
{
    Indented             = true,   // default: true
    FlattenDefaultSection = false, // default: false — wraps in "_default" key
    AutoConvertTypes     = true,   // default: false — auto-detect bool/int/double
    IncludeComments      = false   // default: false
});
DocumentExporter.ToJsonFile(doc, "output.json");

// XML
string xml = DocumentExporter.ToXml(doc);
string xml = DocumentExporter.ToXml(doc, new XmlExportOptions
{
    Indented              = true,
    IncludeXmlDeclaration = true,
    RootElementName       = "configuration",
    SectionElementName    = "section",
    PropertyElementName   = "property",
    UseAttributeForValue  = false, // false = value as element content
    IncludeComments       = false
});
DocumentExporter.ToXmlFile(doc, "output.xml");

// CSV  — columns: Section, Key, Value [, Comment]
string csv = DocumentExporter.ToCsv(doc);
string csv = DocumentExporter.ToCsv(doc, new CsvExportOptions
{
    Delimiter      = ',',
    IncludeHeader  = true,
    AlwaysQuote    = false,
    IncludeComments = false,
    Encoding       = Encoding.UTF8
});
DocumentExporter.ToCsvFile(doc, "output.csv");
```

---

## Fluent Builder

```csharp
var doc = new DocumentBuilder()
    .WithDefaultProperty("AppVersion", "1.0.0")
    .WithSection("Database", s => s
        .WithPreComment("Connection settings")
        .WithProperty("Host", "localhost")
        .WithProperty("Port", 5432)
        .WithComment("primary"))
    .WithSection("Logging", s => s
        .WithProperty("Level", "info")
        .WithProperty("File", "app.log"))
    .Build();
```

---

## Filtering

```csharp
// Sections
var dbSections = doc.GetSectionsByPattern(@"^DB_");     // regex
var enabled    = doc.GetSectionsWhere(s => s.HasProperty("Enabled"));

// Properties within a section
var longValues = section.GetPropertiesWhere(p => p.Value.Length > 100);
var hostProps  = section.GetPropertiesByPattern(@"host", RegexOptions.IgnoreCase);

// Cross-section search
var matches = doc.FindPropertiesByName("Timeout");
foreach (var (sec, prop) in matches)
    Console.WriteLine($"[{sec.Name}] {prop.Name} = {prop.Value}");

// Filtered copy
var reduced = doc.CopyWithSections(s => s.Name != "Debug");
```

---

## Sorting

```csharp
// Properties within a section
section.SortPropertiesByName();
section.SortPropertiesByValue(descending: true);
section.SortProperties((a, b) => string.Compare(a.Value, b.Value));

// Entire document
doc.SortSectionsByName();
doc.SortAllByName();                               // sections + properties
doc.SortPropertiesByName(includeDefaultSection: true);
```

---

## Diff and Merge

```csharp
var diff = original.Compare(modified);

Console.WriteLine($"Added sections: {diff.AddedSections.Count}");
Console.WriteLine($"Removed sections: {diff.RemovedSections.Count}");

foreach (var sd in diff.ModifiedSections)
foreach (var pd in sd.ModifiedProperties)
    Console.WriteLine($"[{sd.SectionName}] {pd.PropertyName}: {pd.OldValue} → {pd.NewValue}");

// Apply diff selectively
var result = target.Merge(diff, new MergeOptions
{
    ApplyAddedSections    = true,   // default: true
    ApplyRemovedSections  = false,  // default: false
    ApplyAddedProperties  = true,   // default: true
    ApplyRemovedProperties = false, // default: false
    ApplyModifiedProperties = true  // default: true
});
Console.WriteLine($"Total changes applied: {result.TotalChanges}");
```

---

## Snapshot / Undo

```csharp
var snapshot = new DocumentSnapshot(doc, maxSnapshots: 10);

snapshot.TakeSnapshot();
doc["Database"]["Host"].Value = "newhost";

snapshot.TakeSnapshot();
doc.RemoveSection("Logging");

// Undo
snapshot.Undo();   // restores Logging section
snapshot.Undo();   // restores original host

Console.WriteLine(snapshot.CanUndo);       // false
Console.WriteLine(snapshot.SnapshotCount); // 0
```

---

## INI File Format

Supported syntax:

```ini
; pre-comment for section
[SectionName]          ; inline comment
Key = Value            ; inline comment
QuotedKey = "value with spaces ; and semicolons"
BoolKey = true         ; true | false | 1 | 0 | yes | no
ArrayKey = {item1, item2, "item three"}
EscapedKey = "line1\nline2\ttabbed"
```

**Escape sequences in quoted values:** `\n`, `\t`, `\r`, `\\`, `\"`, `\0`, `\a`, `\b`, `\;`, `\#`

Values are automatically quoted on save when they contain `;`, `#`, `"`, newlines, or leading/trailing whitespace.

---

## License

MIT
