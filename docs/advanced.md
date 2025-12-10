# Advanced Features

고급 기능 사용법입니다.

## 스냅샷

문서 상태를 저장하고 나중에 복원할 수 있습니다.

```csharp
// 스냅샷 생성
var snapshot = doc.CreateSnapshot();

// 변경 작업
doc["Database"]["Host"].Value = "new-server";
doc["Database"]["Port"].Value = "3306";

// 원래 상태로 복원
doc.RestoreFromSnapshot(snapshot);
```

### DocumentSnapshot 관리자

여러 스냅샷을 관리하고 Undo 기능을 구현할 때 사용합니다.

```csharp
var manager = new DocumentSnapshot(doc, maxSnapshots: 10);

// 변경 전 스냅샷 저장
manager.TakeSnapshot();
doc["Database"]["Host"].Value = "server1";

manager.TakeSnapshot();
doc["Database"]["Port"].Value = "5433";

// Undo
if (manager.CanUndo)
{
    manager.Undo();  // Port 변경 취소
}

manager.Undo();  // Host 변경 취소
```

## 문서 비교 (Diff)

두 문서의 차이점을 비교합니다.

```csharp
var original = IniConfigManager.Load("config.ini");
var modified = IniConfigManager.Load("config_new.ini");

var diff = original.Compare(modified);

if (diff.HasChanges)
{
    Console.WriteLine($"추가된 섹션: {diff.AddedSections.Count}");
    Console.WriteLine($"삭제된 섹션: {diff.RemovedSections.Count}");
    Console.WriteLine($"수정된 섹션: {diff.ModifiedSections.Count}");

    foreach (var sectionDiff in diff.ModifiedSections)
    {
        Console.WriteLine($"\n[{sectionDiff.SectionName}]");

        foreach (var propDiff in sectionDiff.ModifiedProperties)
        {
            Console.WriteLine($"  {propDiff.PropertyName}: {propDiff.OldValue} -> {propDiff.NewValue}");
        }
    }
}
```

## 필터링

### 섹션 필터링

```csharp
// 정규식으로 섹션 찾기
var appSections = doc.GetSectionsByPattern("^App.*");

// 조건으로 필터링
var largeSections = doc.GetSectionsWhere(s => s.PropertyCount > 5);

// 필터링된 문서 복사본 생성
var filtered = doc.CopyWithSections(s => s.Name.Contains("Database"));
```

### 속성 필터링

```csharp
var section = doc["Database"];

// 이름 패턴으로 찾기
var portProps = section.GetPropertiesByPattern(".*Port$");

// 값으로 찾기
var localhostProps = section.GetPropertiesWithValue("localhost");

// 빈 속성 찾기
var emptyProps = section.GetPropertiesWhere(p => p.IsEmpty);
```

### 문서 전체 검색

```csharp
// 모든 섹션에서 특정 이름의 속성 찾기
var allHosts = doc.FindPropertiesByName("Host");
foreach (var (section, property) in allHosts)
{
    Console.WriteLine($"{section.Name}.{property.Name} = {property.Value}");
}

// 값으로 검색
var localhosts = doc.FindPropertiesByValue("localhost");
```

## 환경 변수 치환

INI 파일에서 환경 변수를 사용할 수 있습니다.

```ini
[Paths]
TempDir = ${TEMP}/myapp
HomeDir = %USERPROFILE%/myapp
```

```csharp
var doc = IniConfigManager.Load("config.ini");

// 전체 문서에서 환경 변수 치환
doc.SubstituteEnvironmentVariables();

// 특정 섹션만
doc["Paths"].SubstituteEnvironmentVariables();

// 특정 속성만
doc["Paths"]["TempDir"].SubstituteEnvironmentVariables();

// 치환 성공 여부 확인
if (doc["Paths"]["LogDir"].TrySubstituteEnvironmentVariables(out string result))
{
    Console.WriteLine($"치환 결과: {result}");
}
```

## Fluent Builder

프로그래밍 방식으로 문서를 생성할 때 유용합니다.

```csharp
var doc = new DocumentBuilder()
    .WithDefaultProperty("Version", "1.0")
    .WithSection("Database", db => db
        .WithProperty("Host", "localhost")
        .WithProperty("Port", 5432)
        .WithComment("데이터베이스 설정"))
    .WithSection("Logging", log => log
        .WithProperty("Level", "Info")
        .WithProperty("Path", "/var/log/app.log"))
    .Build();
```

기존 문서를 Builder로 변환해서 수정할 수도 있습니다.

```csharp
var builder = existingDoc.ToBuilder();
var modified = builder
    .WithSection("NewSection", s => s.WithProperty("Key", "Value"))
    .Build();
```

## 배열 처리

```csharp
// INI 파일: Servers = {web1, web2, "server 3"}

// 배열 읽기
string[] servers = doc["Cluster"]["Servers"].GetValueArray<string>();
// 결과: ["web1", "web2", "server 3"]

int[] ports = doc["Network"]["Ports"].GetValueArray<int>();
// INI: Ports = {8080, 8443, 9000}
// 결과: [8080, 8443, 9000]

// 배열 쓰기
doc["Cluster"]["Servers"].SetValueArray(new[] { "node1", "node2" });
// 결과: Servers = {node1, node2}

// 공백이 포함된 값은 자동으로 따옴표 처리
doc["Cluster"]["Hosts"].SetValueArray(new[] { "server 1", "server 2" });
// 결과: Hosts = {"server 1", "server 2"}
```

## Validation

문서를 검증 규칙으로 확인할 수 있습니다.

```csharp
var validator = new IniConfigValidator(doc);

validator.AddRule("Database 섹션 필수",
    d => d.HasSection("Database"));

validator.AddRule("Host 값 필수",
    d => d.TryGetSection("Database", out var db) &&
         db.TryGetProperty("Host", out var host) &&
         !host.IsEmpty);

validator.AddRule("Port 범위 확인",
    d => d.TryGetSection("Database", out var db) &&
         db.TryGetProperty("Port", out var port) &&
         int.TryParse(port.Value, out int p) &&
         p >= 1 && p <= 65535);

var result = validator.Validate();
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```
