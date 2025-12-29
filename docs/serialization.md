# Serialization

IniEdit는 C# 객체와 INI 문서 간의 직렬화/역직렬화를 지원합니다.

## 빠른 시작

```csharp
using IniEdit;
using IniEdit.Serialization;

// 설정 클래스 정의
public class AppConfig
{
    public string AppName { get; set; } = string.Empty;

    [IniSection("Database")]
    public DatabaseConfig Database { get; set; } = new();
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
}

// 역직렬화: INI → 객체
var doc = IniConfigManager.Load("config.ini");
var config = IniSerializer.Deserialize<AppConfig>(doc);

// 직렬화: 객체 → INI
var newDoc = IniSerializer.Serialize(config);
IniConfigManager.Save("config.ini", newDoc);
```

## 설계 규칙

### 중첩 깊이 제한 (최대 1단계)

INI 형식의 플랫한 특성상 **중첩 깊이는 최대 1단계**로 제한됩니다.

```csharp
// ✅ 허용: 1단계 중첩
public class AppConfig
{
    public string AppName { get; set; }           // → DefaultSection

    [IniSection("Database")]
    public DatabaseConfig Database { get; set; }  // → [Database] 섹션

    [IniSection("Logging")]
    public LoggingConfig Logging { get; set; }    // → [Logging] 섹션
}

// ❌ 불허: 2단계 이상 중첩
public class DatabaseConfig
{
    [IniSection("Primary")]
    public ConnectionConfig Primary { get; set; }  // 에러 발생!
}
```

2단계 이상 중첩 시 `IniSerializationException`이 발생합니다:

```
IniSerializationException: Nesting depth exceeded. Property 'Primary' of type
'ConnectionConfig' is at depth 2, but maximum allowed depth is 1.

INI format only supports flat key-value structures. For complex nested objects,
consider using:
  - IniEdit.DocumentExporter.ToJson() for JSON export
  - A dedicated TOML or YAML library for complex configurations
```

### 순환 참조 불허

```csharp
// ❌ 에러
public class Config
{
    [IniSection("Self")]
    public Config? Self { get; set; }  // 순환 참조!
}
```

## Attributes

### `[IniSection]`

클래스나 속성에 INI 섹션 이름을 지정합니다.

```csharp
[IniSection("Database")]
public DatabaseConfig Database { get; set; }
```

결과 INI:
```ini
[Database]
Host = localhost
Port = 5432
```

### `[IniProperty]`

속성에 사용자 정의 키 이름이나 기본값을 지정합니다.

```csharp
public class DatabaseConfig
{
    [IniProperty("db_host")]
    public string Host { get; set; }

    [IniProperty(DefaultValue = 5432)]
    public int Port { get; set; }
}
```

### `[IniIgnore]`

직렬화/역직렬화에서 속성을 제외합니다.

```csharp
public class Config
{
    public string Name { get; set; }

    [IniIgnore]
    public string CachedValue { get; set; }  // 무시됨
}
```

## 지원 타입

| 타입 | 지원 | 예시 |
|------|------|------|
| 기본 타입 | ✅ | `int`, `bool`, `double`, `float`, `long` 등 |
| `string` | ✅ | `"hello"` |
| `enum` | ✅ | `LogLevel.Debug` |
| `DateTime` | ✅ | `2025-01-01` |
| `Guid` | ✅ | `a1b2c3d4-...` |
| 배열 | ✅ | `{item1, item2}` |
| `Nullable<T>` | ✅ | `int?`, `bool?` |
| 중첩 객체 | ⚠️ | 1단계만 허용 |
| `List<T>` | ❌ | 미지원 |
| `Dictionary<K,V>` | ❌ | 미지원 |

## 예제

### 다중 섹션

```csharp
public class AppConfig
{
    [IniSection("Server")]
    public ServerConfig Server { get; set; } = new();

    [IniSection("Logging")]
    public LoggingConfig Logging { get; set; } = new();
}

public class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
}

public class LoggingConfig
{
    public string Path { get; set; } = "/var/log";
    public LogLevel Level { get; set; } = LogLevel.Info;
}

public enum LogLevel { Debug, Info, Warning, Error }
```

결과 INI:
```ini
[Server]
Host = localhost
Port = 8080

[Logging]
Path = /var/log
Level = Info
```

### 배열

```csharp
public class Config
{
    public string[] Tags { get; set; } = { "web", "api" };
    public int[] Ports { get; set; } = { 80, 443 };
}
```

결과 INI:
```ini
Tags = {web, api}
Ports = {80, 443}
```

### 유효성 검사

직렬화 전에 타입 구조를 미리 검사할 수 있습니다:

```csharp
try
{
    IniSerializer.ValidateTypeStructure(typeof(MyConfig));
}
catch (IniSerializationException ex)
{
    Console.WriteLine($"Invalid config structure: {ex.Message}");
}
```

## 복잡한 구조가 필요할 때

INI의 한계로 인해 다음 경우에는 다른 형식을 권장합니다:

- 2단계 이상 중첩이 필요한 경우
- 객체 리스트가 필요한 경우 (예: `List<Server>`)
- Dictionary가 필요한 경우

대안:
1. `DocumentExporter.ToJson()` 사용
2. TOML 라이브러리 사용 (예: Tomlyn)
3. YAML 라이브러리 사용 (예: YamlDotNet)
