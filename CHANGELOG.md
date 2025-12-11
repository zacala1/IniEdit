# Changelog

모든 주요 변경사항을 기록합니다.

## [0.1.0] - 2024-12-11

### 추가
- `MaxSections` - 최대 섹션 수 제한 옵션
- `MaxPropertiesPerSection` - 섹션당 최대 속성 수 제한
- `MaxValueLength` - 값 최대 길이 제한
- `MaxLineLength` - 줄 최대 길이 제한 (메모리 소진 공격 방지)
- `Document.DefaultSectionName` 상수 추가
- `Save_NullDocument_ThrowsArgumentNullException` 테스트

### 변경
- `Save` 메서드가 `IsQuoted` 속성을 변경하지 않도록 수정
- `LoadWithOptionsAsync`를 진정한 async I/O로 변환
- `HasSection`/`HasProperty`가 빈 문자열에 `ArgumentException` 발생
- `DocumentSnapshot`이 `Stack` 대신 `LinkedList` 사용 (O(1) trim)
- `MergeFromOnLastWin` 성능 개선 O(n*m) → O(n+m)
- 파라미터 이름 일관성 개선 (`registry` → `document`)

### 보안
- `FilteringExtensions`의 Regex에 100ms 타임아웃 추가 (ReDoS 방지)
- `Save` 메서드에 document null 체크 추가
- `Comment.Prefix` 검증 추가 (단일 문자만 허용)

### 문서
- README를 자연스러운 한국어로 재작성
- `docs/` 폴더 구조화
  - `configuration.md` - 설정 옵션 문서
  - `advanced.md` - 고급 기능 문서
  - `api-reference.md` - API 레퍼런스
- `FEATURES.md` 삭제

## [0.0.1] - 2024-12-10

### 추가
- 최초 릴리스
- INI 파일 파싱 및 저장
- 섹션, 속성, 주석 지원
- 중복 섹션/키 처리 정책
- 비동기 API 지원
- 환경변수 치환
- 스냅샷 및 Undo 기능
- 문서 비교 (Diff)
- Fluent Builder API
