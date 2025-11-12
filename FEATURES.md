# IniEdit - Feature Guide

Comprehensive guide to all features available in IniEdit.

## 📝 Core Parsing Features

### Comment Support
IniEdit supports both pre-element and inline comments with multiple prefix characters (`;` and `#`).

```csharp
// Reading comments
var doc = IniConfigManager.Load("config.ini");
var section = doc["Database"];

// Access inline comment
string? inlineComment = section.Comment?.Value;

// Access pre-comments
foreach (var comment in section.PreComments)
{
    Console.WriteLine(comment.Value);
}

// Adding comments
section.Comment = new Comment("Main database configuration");
section.PreComments.Add(new Comment("Updated on 2024-01-01"));

var prop = section["Host"];
prop.Comment = new Comment("Production server");
```

### Type Conversion
Automatically convert property values to various .NET types.

```csharp
var config = doc["AppSettings"];

// Built-in type conversion
int timeout = config["Timeout"].GetValue<int>();
bool enabled = config["Enabled"].GetValue<bool>();
double threshold = config["Threshold"].GetValue<double>();

// Safe retrieval with default values
int maxRetries = config["MaxRetries"].GetValueOrDefault(3);
string theme = config["Theme"].GetValueOrDefault("Light");
```

### Array Support
Parse and serialize arrays with automatic quote handling for values containing special characters.

```csharp
// Parsing arrays
var servers = doc["Cluster"]["Servers"].GetValueArray<string>();
// From: Servers = {web1, web2, "server with spaces"}
// Result: ["web1", "web2", "server with spaces"]

var ports = doc["Network"]["Ports"].GetValueArray<int>();
// From: Ports = {8080, 8443, 9000}
// Result: [8080, 8443, 9000]

// Setting arrays
doc["Cluster"]["Servers"].SetValueArray(new[] { "node1", "node2", "node3" });
// Result: Servers = {node1, node2, node3}

doc["Cluster"]["Hosts"].SetValueArray(new[] { "server 1", "server 2" });
// Result: Hosts = {"server 1", "server 2"} (auto-quoted)
```

### Escape Sequences
Full support for standard escape sequences in values.

```csharp
// INI file content:
// Path = C:\\Users\\Admin\\Documents
// Message = Line1\nLine2\tTabbed
// Quote = He said \"Hello\"

var path = doc["Settings"]["Path"].Value;      // C:\Users\Admin\Documents
var msg = doc["Settings"]["Message"].Value;    // Line1\nLine2\tTabbed (with actual newline/tab)
var quote = doc["Settings"]["Quote"].Value;    // He said "Hello"
```

## 🔧 Configuration Options

### Duplicate Handling Policies
Configure how duplicate sections and keys are handled during parsing.

```csharp
var options = new IniConfigOption
{
    // Section duplicate policy
    DuplicateSectionPolicy = DuplicateSectionPolicyType.Merge,  // Merge properties from duplicate sections

    // Key duplicate policy
    DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin  // Last occurrence wins
};

var doc = IniConfigManager.Load("config.ini", options);

// Available policies:
// - FirstWin: Keep first occurrence only
// - LastWin: Keep last occurrence only
// - Merge: Merge all properties (sections only)
// - ThrowError: Throw exception on duplicate
```

### Case Sensitivity
All section and property lookups are case-insensitive by default.

```csharp
var doc = IniConfigManager.Load("config.ini");

// These are all equivalent
var section1 = doc["Database"];
var section2 = doc["database"];
var section3 = doc["DATABASE"];

var prop1 = section1["ConnectionString"];
var prop2 = section1["connectionstring"];
var prop3 = section1["CONNECTIONSTRING"];
```

### Error Collection
Collect all parsing errors for batch analysis instead of throwing on first error.

```csharp
var options = new IniConfigOption
{
    CollectParsingErrors = true
};

var doc = IniConfigManager.Load("config.ini", options);

// Check for errors
if (doc.ParsingErrors.Count > 0)
{
    Console.WriteLine($"Found {doc.ParsingErrors.Count} parsing errors:");

    foreach (var error in doc.ParsingErrors)
    {
        Console.WriteLine($"Line {error.LineNumber}: {error.Reason}");
        Console.WriteLine($"  Content: {error.Line}");
    }
}
```

## ⚡ Async I/O Operations

### Asynchronous Loading and Saving
Non-blocking file operations for better performance in async contexts.

```csharp
// Load asynchronously
var doc = await IniConfigManager.LoadAsync("config.ini");

// Load with custom encoding
var docUtf8 = await IniConfigManager.LoadAsync("config.ini", Encoding.UTF8);

// Save asynchronously
await IniConfigManager.SaveAsync("config.ini", doc);

// Save with custom encoding
await IniConfigManager.SaveAsync("config.ini", Encoding.UTF8, doc);
```

### Advanced Load Options
Fine-grained control over file loading behavior.

```csharp
var options = new LoadOptions
{
    FileShare = FileShare.ReadWrite,  // Allow other processes to access the file

    ConfigOption = new IniConfigOption
    {
        CollectParsingErrors = true,
        DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin
    },

    SectionFilter = name => name.StartsWith("App")  // Load only specific sections
};

var doc = await IniConfigManager.LoadWithOptionsAsync("config.ini", options);
```

## 🛡️ Safe Access Patterns

### TryGet Pattern
Safely retrieve sections and properties without auto-creating them.

```csharp
// Safe section retrieval
if (doc.TryGetSection("Database", out var dbSection))
{
    Console.WriteLine($"Found database section with {dbSection.PropertyCount} properties");
}
else
{
    Console.WriteLine("Database section not found");
}

// Safe property retrieval
if (dbSection?.TryGetProperty("Host", out var hostProp) ?? false)
{
    Console.WriteLine($"Host: {hostProp.Value}");
}
```

### Update or Create
Convenient methods to update existing properties or create them if they don't exist.

```csharp
var section = doc["AppSettings"];

// Update if exists, create if not
section.SetProperty("Theme", "Dark");
section.SetProperty("MaxConnections", 100);  // Generic version with type conversion

// Equivalent to:
// if (section.HasProperty("Theme"))
//     section["Theme"].Value = "Dark";
// else
//     section.AddProperty("Theme", "Dark");
```

## 📊 Document Comparison

### Diff and Change Tracking
Compare two documents and identify all changes.

```csharp
var original = IniConfigManager.Load("config.ini");
var modified = IniConfigManager.Load("config_new.ini");

var diff = original.Compare(modified);

if (diff.HasChanges)
{
    Console.WriteLine($"Added sections: {diff.AddedSections.Count}");
    Console.WriteLine($"Removed sections: {diff.RemovedSections.Count}");
    Console.WriteLine($"Modified sections: {diff.ModifiedSections.Count}");

    // Detailed change analysis
    foreach (var sectionDiff in diff.ModifiedSections)
    {
        Console.WriteLine($"\nSection: {sectionDiff.SectionName}");

        foreach (var propDiff in sectionDiff.ModifiedProperties)
        {
            Console.WriteLine($"  {propDiff.PropertyName}: '{propDiff.OldValue}' → '{propDiff.NewValue}'");
        }

        foreach (var added in sectionDiff.AddedProperties)
        {
            Console.WriteLine($"  + {added.Name} = {added.Value}");
        }

        foreach (var removed in sectionDiff.RemovedProperties)
        {
            Console.WriteLine($"  - {removed.Name} = {removed.Value}");
        }
    }
}
```

## 🔍 Advanced Filtering

### Pattern-Based Searches
Use LINQ-style queries and regex patterns to filter sections and properties.

```csharp
// Filter sections by regex pattern
var appSections = doc.GetSectionsByPattern("^App.*");
var configSections = doc.GetSectionsByPattern(".*Config$");

// Filter sections by predicate
var largeSections = doc.GetSectionsWhere(s => s.PropertyCount > 10);

// Filter properties within a section
var section = doc["Database"];
var portProperties = section.GetPropertiesByPattern(".*Port$");
var emptyProps = section.GetPropertiesWhere(p => p.IsEmpty);

// Find by value
var localhostProps = section.GetPropertiesWithValue("localhost");
var pathProps = section.GetPropertiesContaining("/var/");
```

### Document-Wide Search
Find properties across all sections.

```csharp
// Find all properties with a specific name
var allHosts = doc.FindPropertiesByName("Host");
foreach (var (section, property) in allHosts)
{
    Console.WriteLine($"{section.Name}.{property.Name} = {property.Value}");
}

// Find all properties with a specific value
var allLocalhost = doc.FindPropertiesByValue("localhost");

// Create filtered document copy
var filteredDoc = doc.CopyWithSections(s => s.Name.Contains("Database"));
```

## 📸 Snapshots and Undo

### Simple Snapshots
Create point-in-time snapshots for manual undo operations.

```csharp
// Create snapshot
var snapshot = doc.CreateSnapshot();

// Make changes
doc["Database"]["Host"].Value = "new-host";
doc["Database"]["Port"].Value = "5433";

// Restore from snapshot
doc.RestoreFromSnapshot(snapshot);
```

### Snapshot Manager with History
Manage multiple snapshots with built-in undo/redo capability.

```csharp
var manager = new DocumentSnapshot(doc, maxSnapshots: 10);

// Take snapshot before changes
manager.TakeSnapshot();
doc["Database"]["Host"].Value = "host1";

// Take another snapshot
manager.TakeSnapshot();
doc["Database"]["Port"].Value = "5433";

// Undo last change (port reverts)
if (manager.CanUndo)
{
    manager.Undo();
    Console.WriteLine($"Snapshots: {manager.SnapshotCount}");
}

// Undo again (host reverts)
if (manager.CanUndo)
{
    manager.Undo();
}
```

## 🏗️ Fluent Builder API

### Building Documents Programmatically
Create complex document structures with a fluent interface.

```csharp
var doc = new DocumentBuilder()
    .WithDefaultProperty("Version", "1.0")
    .WithDefaultProperty("AppName", "MyApp")
    .WithSection("Database", db => db
        .WithProperty("Host", "localhost")
        .WithProperty("Port", 5432)
        .WithQuotedProperty("ConnectionString", "Server=localhost;Database=mydb")
        .WithComment("Database configuration")
        .WithPreComment("This section contains database settings"))
    .WithSection("Logging", log => log
        .WithProperty("Level", "Info")
        .WithProperty("File", "/var/log/app.log")
        .WithPreComment("Logging settings"))
    .Build();

// Convert existing document to builder for modifications
var builder = doc.ToBuilder();
var modifiedDoc = builder
    .WithSection("NewSection", s => s.WithProperty("Key", "Value"))
    .Build();
```

## 🌍 Environment Variable Substitution

### Variable Expansion
Automatically substitute environment variables in property values.

```ini
# config.ini
[Paths]
TempDir = ${TEMP}/myapp
HomeDir = %USERPROFILE%/myapp
LogPath = ${LOG_DIR}/app.log
```

```csharp
var doc = IniConfigManager.Load("config.ini");

// Substitute all variables in document
doc.SubstituteEnvironmentVariables();

Console.WriteLine(doc["Paths"]["TempDir"].Value);
// Output: C:/Users/User/AppData/Local/Temp/myapp

// Substitute in specific section only
doc["Paths"].SubstituteEnvironmentVariables();

// Substitute single property
doc["Paths"]["TempDir"].SubstituteEnvironmentVariables();

// Check if substitution occurred
var property = doc["Paths"]["LogPath"];
if (property.TrySubstituteEnvironmentVariables(out string result))
{
    Console.WriteLine($"Substituted: {result}");
}
else
{
    Console.WriteLine("No environment variables found");
}
```

## ✅ Validation

### Document Validation
Validate INI documents against custom rules.

```csharp
var validator = new IniConfigValidator(doc);

// Add validation rules
validator.AddRule("Database section must exist",
    d => d.HasSection("Database"));

validator.AddRule("Database.Host must be set",
    d => d.TryGetSection("Database", out var db) &&
         db.TryGetProperty("Host", out var host) &&
         !host.IsEmpty);

validator.AddRule("Port must be between 1-65535",
    d => d.TryGetSection("Database", out var db) &&
         db.TryGetProperty("Port", out var port) &&
         int.TryParse(port.Value, out int portNum) &&
         portNum >= 1 && portNum <= 65535);

// Run validation
var results = validator.Validate();

if (!results.IsValid)
{
    foreach (var error in results.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}
```

## 📋 Complete API Reference

### Document Class
- `Section this[string name]` - Get or create section
- `Section DefaultSection` - Default section for properties without section header
- `IReadOnlyList<ParsingErrorEventArgs> ParsingErrors` - All parsing errors
- `bool TryGetSection(string name, out Section section)` - Safe section retrieval
- `bool HasSection(string name)` - Check if section exists
- `void AddSection(Section section)` - Add new section
- `Document CreateSnapshot()` - Create deep copy for undo
- `void RestoreFromSnapshot(Document snapshot)` - Restore from snapshot
- `DocumentDiff Compare(Document other)` - Compare with another document

### Section Class
- `Property this[string key]` - Get or create property
- `string Name` - Section name
- `int PropertyCount` - Number of properties
- `Comment? Comment` - Inline comment
- `CommentCollection PreComments` - Comments before section
- `bool TryGetProperty(string key, out Property property)` - Safe property retrieval
- `bool HasProperty(string key)` - Check if property exists
- `void AddProperty(Property property)` - Add new property
- `void SetProperty(string key, string value)` - Update or create property
- `void SetProperty<T>(string key, T value)` - Generic update or create
- `Section Clone()` - Create deep copy

### Property Class
- `string Name` - Property name
- `string Value` - Property value
- `bool IsQuoted` - Whether value should be quoted when saved
- `bool IsEmpty` - Whether value is null or empty
- `Comment? Comment` - Inline comment
- `CommentCollection PreComments` - Comments before property
- `T GetValue<T>()` - Get typed value with conversion
- `T GetValueOrDefault<T>(T defaultValue)` - Safe value retrieval
- `T[] GetValueArray<T>()` - Parse array value
- `void SetValueArray<T>(T[] values)` - Set array value
- `Property Clone()` - Create deep copy

### IniConfigManager (Static)
- `Document Load(string filePath)` - Load from file
- `Document Load(string filePath, IniConfigOption option)` - Load with options
- `Task<Document> LoadAsync(string filePath)` - Load asynchronously
- `Task<Document> LoadAsync(string filePath, Encoding encoding)` - Load async with encoding
- `Document LoadWithOptions(string filePath, LoadOptions options)` - Load with advanced options
- `Task<Document> LoadWithOptionsAsync(string filePath, LoadOptions options)` - Async load with options
- `void Save(string filePath, Document doc)` - Save to file
- `void Save(string filePath, Encoding encoding, Document doc)` - Save with encoding
- `Task SaveAsync(string filePath, Document doc)` - Save asynchronously
- `Task SaveAsync(string filePath, Encoding encoding, Document doc)` - Async save with encoding

## 🎨 Extension Methods Summary

### Document Extensions
- `GetSectionsWhere(Func<Section, bool> predicate)` - Filter sections by condition
- `GetSectionsByPattern(string pattern)` - Filter sections by regex
- `FindPropertiesByName(string name)` - Find all properties with name
- `FindPropertiesByValue(string value)` - Find all properties with value
- `CopyWithSections(Func<Section, bool> filter)` - Create filtered copy
- `SubstituteEnvironmentVariables()` - Replace env vars in all properties
- `ToBuilder()` - Convert to fluent builder

### Section Extensions
- `GetPropertiesWhere(Func<Property, bool> predicate)` - Filter properties
- `GetPropertiesByPattern(string pattern)` - Filter by regex
- `GetPropertiesWithValue(string value)` - Find by exact value
- `GetPropertiesContaining(string substring)` - Find by substring
- `CopyWithProperties(Func<Property, bool> filter)` - Create filtered copy
- `SubstituteEnvironmentVariables()` - Replace env vars in section

### Property Extensions
- `SubstituteEnvironmentVariables()` - Replace env vars in property
- `TrySubstituteEnvironmentVariables(out string result)` - Try replace with result
