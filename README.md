# IniEdit

A modern, feature-rich INI configuration parser and writer for .NET 8.0 with comprehensive support for comments, arrays, type conversion, and async operations.

## ✨ Features

### Core Features
- 📝 **Rich Comment Support** - Pre-element and inline comments with multiple prefix characters (`;`, `#`)
- 🔄 **Type Conversion** - Built-in conversion for primitive types and arrays
- 🔍 **Case-Insensitive** - Case-insensitive section and property lookups
- 🎯 **Duplicate Handling** - Configurable policies for duplicate sections and keys (FirstWin, LastWin, Merge, ThrowError)
- 📊 **Array Support** - Parse and serialize arrays with quoted string support: `{item1, item2, "quoted item"}`
- 🔐 **Escape Sequences** - Full support for `\0`, `\a`, `\b`, `\t`, `\r`, `\n`, `\;`, `\#`, `\"`, `\\`

### Modern Features
- ⚡ **Async I/O** - `LoadAsync()` and `SaveAsync()` for non-blocking file operations
- 🛡️ **Safe Access** - `TryGet` pattern for safe property and section retrieval
- 🔄 **Convenient Updates** - `SetProperty()` methods for update-or-create operations
- 📊 **Document Diff** - Compare documents and track changes
- 🔍 **Advanced Filtering** - LINQ-style queries and pattern-based searches
- 📸 **Snapshots** - Create snapshots and undo changes with built-in snapshot manager
- 🏗️ **Fluent API** - Build documents with a fluent builder pattern
- 🌍 **Environment Variables** - Substitute `${VAR}` and `%VAR%` syntax
- 📋 **Error Collection** - Collect all parsing errors for batch processing
- 🔒 **File Locking** - Configurable file sharing modes during load

### GUI Editor
- 🖥️ **Windows Forms Editor** - Visual INI file management with real-time preview
- ✏️ **Direct Editing** - Edit sections, properties, and comments visually
- 💾 **File Operations** - Load, save, and manage INI files with ease
- ↩️ **Undo/Redo** - Full command pattern implementation for all operations
- 📋 **Copy/Paste** - Copy and paste sections and properties
- 🕒 **Recent Files** - Quick access to recently opened files
- ⚠️ **Duplicate Detection** - Visual highlighting of duplicate keys
- ✅ **Validation** - Real-time validation with statistics
- 🔤 **Encoding Support** - Choose from multiple encoding options
- 🖱️ **Context Menus** - Right-click menus for quick actions
- 🔍 **Advanced Search** - Find and replace with regex support


## 🚀 Quick Start

### Basic Usage

```csharp
using IniEdit;

// Load an INI file
var doc = IniConfigManager.Load("config.ini");

// Access properties
string host = doc["Database"]["Host"].Value;
int port = doc["Database"]["Port"].GetValue<int>();

// Modify values
doc["Database"]["Host"].Value = "localhost";
doc["Database"].SetProperty("Port", 5432);

// Save changes
IniConfigManager.Save("config.ini", doc);
```

### Creating Documents

```csharp
// Create a new document
var doc = new Document();

// Add section with comments
var section = new Section("Database");
section.PreComments.Add(new Comment("Database configuration"));
section.Comment = new Comment("Main database settings");
doc.AddSection(section);

// Add property with comments
var prop = new Property("ConnectionString", "Server=localhost;Database=mydb");
prop.PreComments.Add(new Comment("Connection string for database"));
prop.Comment = new Comment("Local development server");
section.AddProperty(prop);

// Save to file
IniConfigManager.Save("config.ini", doc);
```

## 📚 Advanced Usage

### 1. Async I/O Operations

```csharp
// Load asynchronously
var doc = await IniConfigManager.LoadAsync("config.ini");

// Save asynchronously
await IniConfigManager.SaveAsync("config.ini", doc);

// Load with custom encoding
var doc = await IniConfigManager.LoadAsync("config.ini", Encoding.UTF8);
```

### 2. Safe Property Access

```csharp
// Safe section retrieval
if (doc.TryGetSection("Database", out var dbSection))
{
    Console.WriteLine($"Found database section with {dbSection.PropertyCount} properties");
}

// Safe property retrieval
if (dbSection.TryGetProperty("Host", out var hostProperty))
{
    Console.WriteLine($"Host: {hostProperty.Value}");
}

// Get value with default
int port = doc["Database"]["Port"].GetValueOrDefault(5432);
string host = doc["Database"]["Host"].GetValueOrDefault("localhost");
```

### 3. Update or Create Properties

```csharp
var section = doc["AppSettings"];

// Update if exists, create if not
section.SetProperty("Theme", "Dark");
section.SetProperty("Timeout", 30);  // Generic version with type conversion

// Equivalent to:
// if (section.HasProperty("Theme"))
//     section["Theme"].Value = "Dark";
// else
//     section.AddProperty("Theme", "Dark");
```

### 4. Document Diff

```csharp
var original = IniConfigManager.Load("config.ini");
var modified = IniConfigManager.Load("config_new.ini");

// Compare documents
var diff = original.Compare(modified);

if (diff.HasChanges)
{
    Console.WriteLine($"Added sections: {diff.AddedSections.Count}");
    Console.WriteLine($"Removed sections: {diff.RemovedSections.Count}");
    Console.WriteLine($"Modified sections: {diff.ModifiedSections.Count}");

    // Show detailed changes
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

### 5. Advanced Filtering

```csharp
// Filter sections by pattern
var appSections = doc.GetSectionsByPattern("^App.*");

// Filter properties by condition
var portProperties = section.GetPropertiesByPattern(".*Port$");
var emptyProps = section.GetPropertiesWhere(p => p.IsEmpty);

// Document-wide search
var allHosts = doc.FindPropertiesByName("Host");
foreach (var (section, property) in allHosts)
{
    Console.WriteLine($"{section.Name}.{property.Name} = {property.Value}");
}

// Find properties by value
var localhosts = doc.FindPropertiesByValue("localhost");

// Create filtered copy
var filteredDoc = doc.CopyWithSections(s => s.Name.Contains("Database"));
```

### 6. Snapshots and Undo

```csharp
// Create a simple snapshot
var snapshot = doc.CreateSnapshot();
// ... make changes ...
doc.RestoreFromSnapshot(snapshot);

// Use snapshot manager with undo history
var manager = new DocumentSnapshot(doc, maxSnapshots: 10);

// Take snapshot before changes
manager.TakeSnapshot();
doc["Database"]["Host"].Value = "new-host";

// Take another snapshot
manager.TakeSnapshot();
doc["Database"]["Port"].Value = "5433";

// Undo last change
if (manager.CanUndo)
{
    manager.Undo();  // Port reverts to previous value
    Console.WriteLine($"Snapshots remaining: {manager.SnapshotCount}");
}

// Undo again
manager.Undo();  // Host reverts to original value
```

### 7. Fluent API Builder

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

// Convert existing document to builder
var builder = doc.ToBuilder();
var modifiedDoc = builder
    .WithSection("NewSection", s => s.WithProperty("Key", "Value"))
    .Build();
```

### 8. Environment Variable Substitution

```ini
# config.ini
[Paths]
TempDir = ${TEMP}/myapp
HomeDir = %USERPROFILE%/myapp
LogPath = ${LOG_DIR}/app.log
```

```csharp
var doc = IniConfigManager.Load("config.ini");

// Substitute all environment variables in document
doc.SubstituteEnvironmentVariables();

Console.WriteLine(doc["Paths"]["TempDir"].Value);
// Output: C:/Users/User/AppData/Local/Temp/myapp

// Substitute in specific section
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

### 9. Error Collection

```csharp
var options = new IniConfigOption
{
    CollectParsingErrors = true,
    DuplicateKeyPolicy = DuplicateKeyPolicyType.FirstWin
};

var doc = IniConfigManager.Load("config.ini", options);

// Check for parsing errors
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

### 10. File Locking Options

```csharp
var options = new LoadOptions
{
    FileShare = FileShare.ReadWrite,  // Allow other processes to read/write
    ConfigOption = new IniConfigOption
    {
        CollectParsingErrors = true
    },
    SectionFilter = name => name.StartsWith("App")  // Load only specific sections
};

// Load with custom options
var doc = await IniConfigManager.LoadWithOptionsAsync("config.ini", options);
```

### 11. Array Handling

```csharp
// Parse arrays
var property = new Property("Servers", "{server1, server2, \"server with spaces\"}");
string[] servers = property.GetValueArray<string>();
// Result: ["server1", "server2", "server with spaces"]

int[] ports = doc["Database"]["Ports"].GetValueArray<int>();
// From: Ports = {5432, 5433, 5434}

// Set arrays
property.SetValueArray(new[] { "web1", "web2", "web3" });
// Result: {web1, web2, web3}

property.SetValueArray(new[] { "server with spaces", "normal" });
// Result: {"server with spaces", normal}
```

### 12. Duplicate Handling Policies

```csharp
var options = new IniConfigOption
{
    // Section duplicate policy
    DuplicateSectionPolicy = DuplicateSectionPolicyType.Merge,  // Merge duplicate sections

    // Key duplicate policy
    DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin  // Last value wins
};

var doc = IniConfigManager.Load("config.ini", options);

// Policies:
// - FirstWin: Keep first occurrence
// - LastWin: Keep last occurrence
// - Merge: Merge sections (for sections only)
// - ThrowError: Throw exception on duplicate
```

## 🖥️ GUI Editor

IniEdit includes a Windows Forms-based editor for visual INI file management:

![IniEdit Editor](https://github.com/user-attachments/assets/a0b7db2b-dfda-4396-bb8d-02ede8b96173)

### Features
- **Visual Editing**: List view for sections and properties
- **Comment Management**: Edit pre-element and inline comments
- **Property Editor**: Direct property value editing
- **File Operations**: Load, save, and manage INI files with encoding selection
- **Real-time Preview**: See changes as you edit
- **Undo/Redo**: Unlimited undo/redo for all operations
- **Copy/Paste**: Copy and paste sections and properties between documents
- **Recent Files**: Quick access to recently opened files with max limit
- **Duplicate Detection**: Visual highlighting of duplicate keys with warning colors
- **Validation**: Real-time validation with statistics (sections, properties, duplicates, errors)
- **Context Menus**: Right-click context menus for sections and properties
- **Advanced Search**: Find and replace with regex, case-sensitive, and whole word options

### Running the Editor

```bash
cd IniEdit.GUI
dotnet run
```

Or build and run the executable:
```bash
dotnet build -c Release
./bin/Release/net8.0-windows/IniEditor.exe
```

## 📖 API Reference

### Core Classes

#### `Document`
- `Section this[string name]` - Get or create section
- `Section DefaultSection` - Default section for properties without section
- `IReadOnlyList<ParsingErrorEventArgs> ParsingErrors` - Collected parsing errors
- `bool TryGetSection(string name, out Section section)` - Safe section retrieval
- `Document CreateSnapshot()` - Create deep copy
- `void RestoreFromSnapshot(Document snapshot)` - Restore from snapshot
- `DocumentDiff Compare(Document other)` - Compare with another document

#### `Section`
- `Property this[string key]` - Get or create property
- `int PropertyCount` - Number of properties
- `bool TryGetProperty(string key, out Property property)` - Safe property retrieval
- `void SetProperty(string key, string value)` - Update or create property
- `void SetProperty<T>(string key, T value)` - Generic update or create
- `Section Clone()` - Create deep copy

#### `Property`
- `string Value` - Property value
- `bool IsQuoted` - Whether value should be quoted when saved
- `bool IsEmpty` - Whether value is null or empty
- `T GetValue<T>()` - Get typed value
- `T GetValueOrDefault<T>(T defaultValue)` - Safe value retrieval
- `T[] GetValueArray<T>()` - Parse array value
- `void SetValueArray<T>(T[] values)` - Set array value
- `Property Clone()` - Create deep copy

#### `IniConfigManager` (static)
- `Document Load(string filePath)` - Load from file
- `Task<Document> LoadAsync(string filePath)` - Load asynchronously
- `void Save(string filePath, Document doc)` - Save to file
- `Task SaveAsync(string filePath, Document doc)` - Save asynchronously
- `Document LoadWithOptions(string filePath, LoadOptions options)` - Load with advanced options

### Extension Methods

See [FEATURES.md](FEATURES.md) for complete API documentation with examples.

## 🎯 Use Cases

- **Application Configuration**: Manage app settings with type safety
- **Game Configuration**: Store game settings, levels, and player data
- **Server Configuration**: Configure web servers, databases, and services
- **Localization**: Manage translation files with comments
- **Testing**: Create and compare configuration snapshots
- **Configuration Migration**: Track and apply configuration changes
- **Multi-environment Setup**: Filter and merge configurations per environment
