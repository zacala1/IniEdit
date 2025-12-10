# API Reference

## IniConfigManager

INI 파일 로드/저장을 담당하는 정적 클래스입니다.

### Load

```csharp
Document Load(string filePath)
Document Load(string filePath, IniConfigOption option)
Document Load(string filePath, Encoding encoding, IniConfigOption option)
Document Load(Stream stream, Encoding encoding, IniConfigOption option)
```

### LoadAsync

```csharp
Task<Document> LoadAsync(string filePath)
Task<Document> LoadAsync(string filePath, Encoding encoding)
Task<Document> LoadAsync(Stream stream, Encoding encoding, IniConfigOption option, CancellationToken cancellationToken)
```

### LoadWithOptions

```csharp
Document LoadWithOptions(string filePath, LoadOptions options)
Task<Document> LoadWithOptionsAsync(string filePath, LoadOptions options)
```

### Save

```csharp
void Save(string filePath, Document doc)
void Save(string filePath, Encoding encoding, Document doc)
void Save(Stream stream, Encoding encoding, Document doc)
```

### SaveAsync

```csharp
Task SaveAsync(string filePath, Document doc)
Task SaveAsync(string filePath, Encoding encoding, Document doc)
Task SaveAsync(Stream stream, Encoding encoding, Document doc, CancellationToken cancellationToken)
```

### 이벤트

```csharp
event EventHandler<ParsingErrorEventArgs> ParsingError
```

---

## Document

INI 문서를 나타내는 클래스입니다.

### 생성자

```csharp
Document(IniConfigOption? option = null)
```

### 속성

| 이름 | 타입 | 설명 |
|------|------|------|
| `DefaultSection` | `Section` | 섹션 없는 속성들이 저장되는 기본 섹션 |
| `SectionCount` | `int` | 섹션 개수 |
| `ParsingErrors` | `IReadOnlyList<ParsingErrorEventArgs>` | 파싱 에러 목록 |
| `CommentPrefixChars` | `char[]` | 주석 문자 목록 |
| `DefaultCommentPrefixChar` | `char` | 기본 주석 문자 |

### 인덱서

```csharp
Section this[int index]     // 인덱스로 섹션 접근
Section this[string name]   // 이름으로 섹션 접근 (없으면 생성)
```

### 메서드

```csharp
// 섹션 조회
Section? GetSection(string name)
Section? GetSectionByIndex(int index)
bool TryGetSection(string name, out Section? section)
bool HasSection(string name)
IReadOnlyList<Section> GetSections()

// 섹션 추가/삭제
void AddSection(string name)
void AddSection(Section section)
void InsertSection(int index, string name)
void InsertSection(int index, Section section)
bool RemoveSection(string name)
bool RemoveSection(int index)
void Clear()

// 값 접근 (섹션 + 속성명으로 직접 접근)
T GetValue<T>(string sectionName, string propertyKey)
T GetValueOrDefault<T>(string sectionName, string propertyKey, T defaultValue)
bool TryGetValue<T>(string sectionName, string propertyKey, out T value)

// Fluent API
Document WithSection(string name)
Document WithSection(Section section)
Document WithDefaultProperty(string key, string value)
Document WithDefaultProperty<T>(string key, T value)
```

---

## Section

INI 섹션을 나타내는 클래스입니다.

### 생성자

```csharp
Section(string name)
```

### 속성

| 이름 | 타입 | 설명 |
|------|------|------|
| `Name` | `string` | 섹션 이름 |
| `PropertyCount` | `int` | 속성 개수 |
| `Comment` | `Comment?` | 인라인 주석 |
| `PreComments` | `CommentCollection` | 섹션 앞 주석들 |

### 인덱서

```csharp
Property this[int index]    // 인덱스로 속성 접근
Property this[string key]   // 키로 속성 접근 (없으면 생성)
```

### 메서드

```csharp
// 속성 조회
Property? GetProperty(string key)
Property? GetProperty(int index)
bool TryGetProperty(string key, out Property? property)
bool HasProperty(string key)
IReadOnlyList<Property> GetProperties()

// 속성 추가/삭제
void AddProperty(string name, string value)
void AddProperty(Property property)
void AddPropertyRange(IEnumerable<Property> collection)
void InsertProperty(string targetKey, string name, string value)
void InsertProperty(int index, Property property)
bool RemoveProperty(string name)
bool RemoveProperty(int index)
void Clear()

// 값 설정/조회
void SetProperty(string key, string value)
void SetProperty<T>(string key, T value)
T GetPropertyValue<T>(string key)
T GetPropertyValueOrDefault<T>(string key, T defaultValue)
bool TryGetPropertyValue<T>(string key, out T value)

// 병합
void MergeFrom(Section section, DuplicateKeyPolicyType policy)

// 복제
Section Clone()

// Fluent API
Section WithProperty(string key, string value)
Section WithProperty<T>(string key, T value)
Section WithComment(string comment)
Section WithPreComment(string comment)
```

---

## Property

키-값 속성을 나타내는 클래스입니다.

### 생성자

```csharp
Property(string name)
Property(string name, string value)
```

### 속성

| 이름 | 타입 | 설명 |
|------|------|------|
| `Name` | `string` | 속성 이름 (키) |
| `Value` | `string` | 속성 값 |
| `IsEmpty` | `bool` | 값이 비어있는지 여부 |
| `IsQuoted` | `bool` | 저장 시 따옴표로 감쌀지 여부 |
| `Comment` | `Comment?` | 인라인 주석 |
| `PreComments` | `CommentCollection` | 속성 앞 주석들 |

### 메서드

```csharp
// 값 조회
T GetValue<T>()
T GetValueOrDefault<T>(T defaultValue)
T GetValueOrDefault<T>()
bool TryGetValue<T>(out T value)

// 배열
T[] GetValueArray<T>()
void SetValueArray<T>(T[] values)

// 값 설정
void SetStringValue(string value)
void SetValue<T>(T value)

// 복제
Property Clone()

// Fluent API
Property WithValue(string value)
Property WithValue<T>(T value)
Property WithQuoted(bool quoted = true)
Property WithComment(string comment)
Property WithPreComment(string comment)
```

---

## IniConfigOption

파싱 옵션 클래스입니다.

### 속성

| 이름 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `DuplicateSectionPolicy` | `DuplicateSectionPolicyType` | `FirstWin` | 중복 섹션 처리 정책 |
| `DuplicateKeyPolicy` | `DuplicateKeyPolicyType` | `FirstWin` | 중복 키 처리 정책 |
| `CollectParsingErrors` | `bool` | `false` | 파싱 에러 수집 여부 |
| `CommentPrefixChars` | `char[]` | `[';', '#']` | 주석 문자 |
| `DefaultCommentPrefixChar` | `char` | `';'` | 기본 주석 문자 |

---

## LoadOptions

파일 로드 옵션 클래스입니다.

### 속성

| 이름 | 타입 | 설명 |
|------|------|------|
| `FileShare` | `FileShare` | 파일 공유 모드 |
| `ConfigOption` | `IniConfigOption` | 파싱 옵션 |
| `SectionFilter` | `Func<string, bool>?` | 섹션 필터 |

---

## Enums

### DuplicateSectionPolicyType

```csharp
enum DuplicateSectionPolicyType
{
    FirstWin,   // 첫 번째 유지
    LastWin,    // 마지막 유지
    Merge,      // 병합
    ThrowError  // 예외 발생
}
```

### DuplicateKeyPolicyType

```csharp
enum DuplicateKeyPolicyType
{
    FirstWin,   // 첫 번째 유지
    LastWin,    // 마지막 유지
    ThrowError  // 예외 발생
}
```
