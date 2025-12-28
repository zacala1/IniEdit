# 비동기 파일 I/O 리팩토링

대규모 INI 파일 처리 시 UI가 멈추는 문제를 해결하기 위한 비동기 파일 I/O 변환 작업.

## 작업 범위

### 1. IniEdit 라이브러리 (IniConfigManager)

**새로운 비동기 메서드 추가:**

```csharp
// IniConfigManager.cs에 추가
public static async Task<Document> LoadAsync(string filePath, Encoding? encoding = null, IniConfigOption? config = null, CancellationToken cancellationToken = default)
public static async Task<Document> LoadAsync(Stream stream, Encoding? encoding = null, IniConfigOption? config = null, CancellationToken cancellationToken = default)
public static async Task SaveAsync(Document doc, string filePath, Encoding? encoding = null, SaveOptions? options = null, CancellationToken cancellationToken = default)
public static async Task SaveAsync(Document doc, Stream stream, Encoding? encoding = null, SaveOptions? options = null, CancellationToken cancellationToken = default)

// LoadOptions.cs에 추가
public static async Task<Document> LoadWithOptionsAsync(string filePath, LoadOptions options, CancellationToken cancellationToken = default)
```

**구현 시 고려사항:**
- `FileStream`을 `FileOptions.Asynchronous` 플래그로 생성
- `StreamReader.ReadToEndAsync()` 사용
- `StreamWriter.WriteAsync()` 사용
- `CancellationToken` 지원으로 작업 취소 가능하게

### 2. IniEdit.GUI (MainForm)

**비동기로 변환할 메서드:**

| 메서드 | 위치 | 설명 |
|--------|------|------|
| `LoadIniFile()` | MainForm.cs | 파일 로드 후 UI 갱신 |
| `SaveFile()` | MainForm.cs | 현재 문서 저장 |
| `PerformAutoBackup()` | MainForm.cs | 자동 백업 수행 |

**UI 개선사항:**
- 파일 작업 중 진행률 표시 (ProgressBar 또는 StatusStrip)
- 작업 중 UI 비활성화 방지 (async/await로 자연스럽게 해결)
- 작업 취소 버튼 추가 (선택적)

**패턴 예시:**

```csharp
private async Task LoadIniFileAsync(string filePath)
{
    try
    {
        SetStatusMessage("파일 로드 중...");
        _isLoading = true;

        // 비동기 로드 (UI 스레드 블로킹 없음)
        var doc = await IniConfigManager.LoadAsync(filePath, Encoding.UTF8);

        // UI 갱신 (이미 UI 스레드에 있음)
        _currentDocument = doc;
        RefreshSectionList();

        SetStatusMessage("로드 완료");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"파일 로드 실패: {ex.Message}");
    }
    finally
    {
        _isLoading = false;
    }
}
```

### 3. 테스트 추가

**IniEdit.Tests에 추가:**
- `IniConfigManagerAsyncTests.cs` - 비동기 Load/Save 테스트
- 대용량 파일 (1MB+) 처리 테스트
- 취소 토큰 동작 테스트

## 작업 순서

1. IniConfigManager에 async 메서드 추가 (기존 동기 메서드 유지)
2. 단위 테스트 작성 및 통과 확인
3. MainForm의 파일 작업을 async로 변환
4. UI 피드백 개선 (진행률 표시)
5. 통합 테스트

## 주의사항

- 기존 동기 API는 하위 호환성을 위해 유지
- ConfigureAwait(false)는 라이브러리에서 사용, GUI에서는 생략
- 파일 잠금 해제 시점 주의 (using 블록 활용)
- 예외 처리 시 AggregateException 고려

## 예상 영향

- 대용량 파일 로드 시 UI 응답성 대폭 개선
- 자동 백업이 백그라운드에서 수행되어 사용자 작업 방해 없음
- 메모리 사용량은 동일 (전체 파일을 메모리에 로드하는 방식 유지)
