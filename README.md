# IniEdit

.NET 8.0용 INI 설정 파일 파서 및 에디터입니다.

주석, 배열, 타입 변환, 비동기 I/O를 지원하며 GUI 에디터도 포함되어 있습니다.

## 설치

```bash
# NuGet (예정)
dotnet add package IniEdit

# 또는 소스에서 빌드
git clone https://github.com/user/IniEdit.git
cd IniEdit
dotnet build
```

## 빠른 시작

```csharp
using IniEdit;

// 파일 로드
var doc = IniConfigManager.Load("config.ini");

// 값 읽기
string host = doc["Database"]["Host"].Value;
int port = doc["Database"]["Port"].GetValue<int>();

// 값 수정
doc["Database"]["Host"].Value = "localhost";
doc["Database"].SetProperty("Port", 5432);

// 저장
IniConfigManager.Save("config.ini", doc);
```

## 주요 기능

- **주석 처리**: 섹션/속성 앞 주석과 인라인 주석 지원 (`;`, `#`)
- **타입 변환**: `GetValue<T>()`로 기본 타입 자동 변환
- **배열 지원**: `{item1, item2, "quoted item"}` 형식
- **이스케이프 시퀀스**: `\0`, `\t`, `\n`, `\\`, `\"` 등
- **중복 처리 정책**: FirstWin, LastWin, Merge, ThrowError
- **비동기 I/O**: `LoadAsync()`, `SaveAsync()`
- **스냅샷**: 문서 상태 저장 및 복원
- **문서 비교**: 두 INI 파일 간 diff
- **환경 변수 치환**: `${VAR}`, `%VAR%` 문법

## 문서 생성하기

```csharp
var doc = new Document();

// 섹션 추가
var section = new Section("Database");
section.PreComments.Add(new Comment("데이터베이스 설정"));
doc.AddSection(section);

// 속성 추가
section.AddProperty("Host", "localhost");
section.SetProperty("Port", 5432);

IniConfigManager.Save("config.ini", doc);
```

출력:

```ini
;데이터베이스 설정
[Database]
Host = localhost
Port = 5432
```

## 비동기 처리

```csharp
var doc = await IniConfigManager.LoadAsync("config.ini");
await IniConfigManager.SaveAsync("config.ini", doc);
```

## 안전한 값 접근

```csharp
// TryGet 패턴
if (doc.TryGetSection("Database", out var section))
{
    if (section.TryGetProperty("Port", out var prop))
    {
        Console.WriteLine(prop.Value);
    }
}

// 기본값 지정
int port = doc["Database"]["Port"].GetValueOrDefault(5432);
```

## GUI 에디터

Windows Forms 기반 INI 에디터가 포함되어 있습니다.

![IniEdit Editor](https://github.com/user-attachments/assets/a0b7db2b-dfda-4396-bb8d-02ede8b96173)

```bash
cd IniEdit.GUI
dotnet run
```

주요 기능:

- 섹션/속성 트리 뷰
- 실시간 미리보기
- Undo/Redo
- 찾기/바꾸기 (정규식 지원)
- 중복 키 감지
- 인코딩 선택

## 자세한 문서

- [Configuration Options](docs/configuration.md) - 파싱 옵션 설정
- [Advanced Features](docs/advanced.md) - 스냅샷, Diff, 필터링 등
- [API Reference](docs/api-reference.md) - 전체 API 문서

## 라이선스

MIT License
