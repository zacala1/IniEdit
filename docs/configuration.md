# Configuration Options

IniEdit의 파싱 동작을 설정하는 방법입니다.

## IniConfigOption

`IniConfigOption` 클래스로 파싱 옵션을 지정할 수 있습니다.

```csharp
var options = new IniConfigOption
{
    DuplicateSectionPolicy = DuplicateSectionPolicyType.Merge,
    DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin,
    CollectParsingErrors = true
};

var doc = IniConfigManager.Load("config.ini", options);
```

## 중복 섹션 처리

같은 이름의 섹션이 여러 개 있을 때 어떻게 처리할지 지정합니다.

| 정책 | 설명 |
|------|------|
| `FirstWin` | 첫 번째 섹션만 유지 (기본값) |
| `LastWin` | 마지막 섹션만 유지 |
| `Merge` | 모든 섹션의 속성을 하나로 병합 |
| `ThrowError` | 중복 발견 시 예외 발생 |

```csharp
// 예: 섹션 병합
var options = new IniConfigOption
{
    DuplicateSectionPolicy = DuplicateSectionPolicyType.Merge
};
```

## 중복 키 처리

같은 섹션 내에 동일한 키가 여러 개 있을 때 처리 방법입니다.

| 정책 | 설명 |
|------|------|
| `FirstWin` | 첫 번째 값만 유지 (기본값) |
| `LastWin` | 마지막 값만 유지 |
| `ThrowError` | 중복 발견 시 예외 발생 |

```csharp
var options = new IniConfigOption
{
    DuplicateKeyPolicy = DuplicateKeyPolicyType.LastWin
};
```

## 파싱 에러 수집

기본적으로 파싱 에러가 발생하면 해당 줄을 건너뜁니다. `CollectParsingErrors`를 활성화하면 모든 에러를 수집할 수 있습니다.

```csharp
var options = new IniConfigOption
{
    CollectParsingErrors = true
};

var doc = IniConfigManager.Load("config.ini", options);

if (doc.ParsingErrors.Count > 0)
{
    foreach (var error in doc.ParsingErrors)
    {
        Console.WriteLine($"Line {error.LineNumber}: {error.Reason}");
    }
}
```

## 주석 문자 설정

기본적으로 `;`와 `#`을 주석 문자로 인식합니다. 필요하면 변경할 수 있습니다.

```csharp
var options = new IniConfigOption
{
    CommentPrefixChars = new[] { ';' },  // #은 주석으로 인식하지 않음
    DefaultCommentPrefixChar = ';'
};
```

## LoadOptions

파일 로드 시 더 세밀한 제어가 필요하면 `LoadOptions`를 사용합니다.

```csharp
var options = new LoadOptions
{
    // 다른 프로세스의 파일 접근 허용
    FileShare = FileShare.ReadWrite,

    // 파싱 옵션
    ConfigOption = new IniConfigOption
    {
        CollectParsingErrors = true
    },

    // 특정 섹션만 로드
    SectionFilter = name => name.StartsWith("App")
};

var doc = await IniConfigManager.LoadWithOptionsAsync("config.ini", options);
```

## 보안 제한

신뢰할 수 없는 INI 파일을 파싱할 때 메모리 소진 공격을 방지합니다.

```csharp
var options = new IniConfigOption
{
    // 최대 섹션 수 (0 = 무제한)
    MaxSections = 100,

    // 섹션당 최대 속성 수 (0 = 무제한)
    MaxPropertiesPerSection = 500,

    // 값의 최대 길이 (0 = 무제한)
    MaxValueLength = 10000,

    // 한 줄의 최대 길이 (0 = 무제한)
    MaxLineLength = 8192,

    CollectParsingErrors = true
};

var doc = IniConfigManager.Load("untrusted.ini", options);

// 제한 초과 시 파싱 에러로 수집됨
foreach (var error in doc.ParsingErrors)
{
    Console.WriteLine(error.Reason);
}
```

웹 서버나 외부 입력을 처리할 때 이 옵션들을 설정하는 것을 권장합니다.

## 인코딩 지정

```csharp
// UTF-8 (기본값)
var doc = IniConfigManager.Load("config.ini");

// 다른 인코딩
var doc = IniConfigManager.Load("config.ini", Encoding.GetEncoding("euc-kr"));

// 저장 시 인코딩 지정
IniConfigManager.Save("config.ini", Encoding.UTF8, doc);
```
