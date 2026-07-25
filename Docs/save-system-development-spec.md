# 게임 저장·이어하기 시스템 개발 명세서

> 프로젝트: DiaBlackJack
> 기획·개발 책임자: 이천서
> 작업 식별자: SV-00~SV-06
> 버전: v0.2
> 상태: SV-01 순수 스냅샷·검증 완료 · 다음 단계 SV-02
> 최종 갱신: 2026-07-26

## 1. 기술 목표

순수 런 상태를 검증 가능한 스냅샷으로 변환하고, Unity 파일 I/O 경계에서 버전이 있는 JSON을 원자적으로 저장한 뒤, 앱 재실행 시 새 런 객체로 복원한다.

```text
StageProgression / RunFlow 상태
        ↓ Capture
RunSaveSnapshot (순수 C#)
        ↓ Validate
RunSaveEnvelope v1
        ↓ Serialize
temporary → backup → primary

primary/backup
        ↓ Parse + Validate
RunRestoreFactory
        ↓ 새 객체 구성 완료 후 교체
StageProgressionRuntime.Session
```

복원은 기존 세션을 필드별로 덮어쓰지 않는다. 모든 데이터를 먼저 검증하고 새 `PlayerRunState`·`RunProgress`·세션을 구성한 뒤 성공한 객체만 Runtime에 교체한다.

## 2. 현재 코드 기준선

| 구성 | 현재 상태 | 필요한 변경 |
| --- | --- | --- |
| `Border.SaveLoad.Save` | 직렬화할 필드가 없는 빈 클래스 | 런 저장 전용 DTO와 분리 또는 교체 |
| `SaveLoadSystem` | ScriptableObject가 `SaveData`를 직접 보유 | 저장소·직렬화·조정 책임 분리 |
| `FileManager` | 기본 파일에 `WriteAllText` 직접 수행 | 경로 검증, 임시/백업, 원자 교체, 명시적 결과 |
| `PlayerRunState` | 영혼·일반/악마 덱과 마지막 ID를 보유하고 SV-01 캡처에 내부 읽기 제공 | 검증된 복원 경로 |
| 시작 악마 선택 | 기본 생성 경로가 악마 4장을 즉시 제공 | 후보 2장·1장 확정 상태 구현 뒤 첫 체크포인트 연결 |
| `RunProgress` | 스테이지·상태·보상을 보유하고 SV-01이 `StageCleared`·런 종료만 캡처 | 복원 Factory와 나머지 체크포인트 연결 |
| `StageProgression/Save` | 스키마 1 불변 스냅샷·검증기·현행 안정 상태 캡처 구현 | SV-02 파일 DTO·직렬화와 SV-03 복원 연결 |
| `StageProgressionSession` | 현재 전투와 보상을 조정 | 전투 객체를 제외한 체크포인트 캡처 경계 |
| `StageProgressionRuntime` | `DontDestroyOnLoad` 메모리 세션 | 새 게임 예약·이어하기·세션 원자 교체 |
| RF 골드·상점·사건 | 기획만 있고 구현 미완료 | 실제 타입이 생긴 뒤 읽기 전용 저장 계약 연결 |

기존 `SaveLoad` 코드는 Unity-facing 지원 계층이며 CoreLoop·StageProgression 순수성의 기준이 아니다. 순수 상태 타입에는 `UnityEngine`, `Application.persistentDataPath`, `JsonUtility`를 참조하지 않는다.

## 3. 제안 파일 구조

```text
Assets/01. Scripts/
├─ StageProgression/Save/
│  ├─ RunSaveSnapshot.cs
│  ├─ RunSaveCardSnapshot.cs
│  ├─ RunSaveDemonSnapshot.cs
│  ├─ RunCheckpointKind.cs
│  ├─ RunSaveValidator.cs
│  ├─ RunSaveCapture.cs
│  └─ RunRestoreFactory.cs
├─ SaveLoad/
│  ├─ RunSaveEnvelope.cs
│  ├─ RunSaveSerializer.cs
│  ├─ RunSaveRepository.cs
│  ├─ RunSaveLoadResult.cs
│  ├─ RunSaveWriteResult.cs
│  ├─ IRunSaveFileStore.cs
│  └─ SystemRunSaveFileStore.cs
├─ Bootstrap/
│  └─ RunSaveCoordinator.cs
└─ UI/
   └─ MainMenu 또는 진행 UI의 이어하기·저장 실패 연결
```

실제 구현에서는 책임이 작으면 타입을 합치며, 파일 수를 채우기 위해 빈 래퍼를 만들지 않는다.

## 4. 순수 스냅샷 모델

### 4.1 최상위 스냅샷

후보 API:

```csharp
public sealed class RunSaveSnapshot
{
    public int SchemaVersion { get; }
    public string ContentRevision { get; }
    public long SaveSequence { get; }
    public string RunId { get; }
    public string SavedAtUtc { get; }
    public RunCheckpointKind CheckpointKind { get; }
    public RunSaveStatus Status { get; }
    public int RootSeed { get; }
    public int CurrentStageIndex { get; }
    public string CurrentStageId { get; }
    public string NextContentKind { get; }
    public PlayerRunSaveSnapshot Player { get; }
    public RunRandomSaveSnapshot Random { get; }
    public IReadOnlyList<string> CompletedShopIds { get; }
    public IReadOnlyList<string> CompletedEventIds { get; }
}
```

공개 생성자는 유효성 검사 없이 임의 스냅샷을 만들 수 없게 `internal` 또는 Factory 중심으로 둔다. 테스트는 `InternalsVisibleTo`를 사용한다.

### 4.2 플레이어 스냅샷

```csharp
public sealed class PlayerRunSaveSnapshot
{
    public int MaximumSoul { get; }
    public int CurrentSoul { get; }
    public int CurrentGold { get; }
    public int LastIssuedCardId { get; }
    public int LastIssuedDemonCardId { get; }
    public string StartingDemonDefinitionKey { get; }
    public IReadOnlyList<RunSaveCardSnapshot> Cards { get; }
    public IReadOnlyList<RunSaveDemonSnapshot> DemonCards { get; }
}
```

`CurrentGold`는 RF 구현 전에는 0만 허용하는 임시 필드를 만들지 않는다. 스키마 필드는 v1에 포함하되 실제 캡처·복원 연결은 `PlayerRunState`의 골드 API가 구현된 뒤 활성화한다. 그 전 단계의 테스트 Fixture에서는 명시적으로 0을 사용한다.

### 4.3 카드 스냅샷

일반 카드는 다음을 저장한다.

- 물리 `Id`
- `DefinitionKey`
- 안정적인 무늬 코드

악마 카드는 다음을 저장한다.

- 물리 `Id`
- `DefinitionKey`

표시 이름, 숫자, 효과 종류와 가격은 카탈로그에서 다시 조회한다. 파생 값을 중복 저장하지 않는다.

### 4.4 난수·콘텐츠 재현

```csharp
public sealed class RunRandomSaveSnapshot
{
    public int OpponentOfferOrdinal { get; }
    public int BattleRewardOrdinal { get; }
    public int ShopOfferOrdinal { get; }
    public int EventOrdinal { get; }
    public string ReservedNextOfferId { get; }
}
```

실제 후보를 저장하는 편이 더 안전한 콘텐츠는 명시적 키 목록을 추가할 수 있다. `System.Random` 내부 필드나 Unity 난수 상태는 저장하지 않는다.

## 5. 파일 DTO와 직렬화

### 5.1 `RunSaveEnvelope`

`JsonUtility`는 프로퍼티와 다형성, 사전을 직접 처리하지 못하므로 파일 DTO는 `[Serializable]` 필드 기반의 평면 구조로 둔다. 순수 스냅샷과 파일 DTO는 Mapper로 분리한다.

권장 최상위 필드:

```text
schemaVersion
contentRevision
saveSequence
runId
savedAtUtc
checkpointKind
status
rootSeed
currentStageIndex
currentStageId
nextContentKind
player
random
completedShopIds[]
completedEventIds[]
```

Enum은 C# 선언 순서에 종속된 정수 대신 안정적인 문자열 코드로 저장한다.

### 5.2 버전

- 최초 스키마는 1이다.
- `schemaVersion <= 0`은 손상으로 거부한다.
- 현재보다 높은 버전은 `UnsupportedNewerVersion`으로 거부한다.
- 과거 버전은 등록된 명시적 Migration이 있을 때만 순차 변환한다.
- 콘텐츠 카탈로그와 호환되지 않는 `contentRevision`은 자동으로 추측 복원하지 않는다.

v1에서는 마이그레이션 프레임워크를 과도하게 만들지 않고 결과 코드와 확장 지점만 둔다.

## 6. 검증 불변식

`RunSaveValidator`는 파일을 Runtime 객체로 만들기 전에 다음을 모두 검사한다.

1. 스키마·콘텐츠 리비전이 지원된다.
2. 런 ID, 체크포인트 코드와 진행 위치가 비어 있지 않다.
3. 최대 영혼은 양수이고 현재 영혼은 `0..최대`다.
4. 골드는 0 이상이다.
5. 진행 중 저장의 현재 영혼은 1 이상이다.
6. 승리·패배 저장은 이어하기 불가 상태다.
7. 일반 덱은 최소 1장이다.
8. 일반 카드 ID와 악마 카드 ID는 각 집합에서 중복되지 않는다.
9. 모든 정의 키와 무늬가 현재 카탈로그에 존재한다.
10. 마지막 발급 ID는 현재 해당 덱의 최대 ID 이상이다.
11. 시작 악마 키가 있으면 악마 덱에 같은 정의가 최소 1장 존재한다.
12. 스테이지 인덱스와 ID가 현재 경로에 일치한다.
13. 체크포인트 종류와 다음 콘텐츠 종류의 조합이 허용된다.
14. 완료한 상점·사건 ID는 비어 있거나 중복되지 않는다.
15. 생성 순번은 음수가 아니다.
16. 전투·보상·상점 거래·사건 선택 같은 불안정 상태는 파일에 나타나지 않는다.

검증 실패는 예외 원문 대신 타입화된 이유 코드로 반환한다.

## 7. 체크포인트 정책

후보 타입:

```csharp
public enum RunCheckpointKind
{
    StartingDemonSelected,
    CombatSettlementCompleted,
    ShopExited,
    EventResolved,
    RunEnded
}
```

`RunCheckpointPolicy`는 현재 런 상태와 완료 원인을 받아 저장 가능 여부를 판단한다. UI는 임의로 `Save()`를 호출하지 않고 성공한 도메인 전이의 결과를 저장 조정자에 전달한다.

```text
입력 성공
→ 도메인 상태가 안정 상태인지 확인
→ Snapshot Capture
→ Validate
→ Repository Write
→ 성공 후 다음 콘텐츠 진입 허용
```

실패 입력, 중복 입력과 오래된 OfferId는 저장 요청을 만들지 않는다.

## 8. 런 저장소 계약

### 8.1 결과 타입

예외를 UI까지 던지지 않고 명시적 결과를 사용한다.

```csharp
public enum RunSaveWriteStatus
{
    Success,
    ValidationFailed,
    SerializationFailed,
    TemporaryWriteFailed,
    VerificationFailed,
    ReplaceFailed
}

public enum RunSaveLoadStatus
{
    SuccessPrimary,
    SuccessBackup,
    NoSave,
    ReservationOnly,
    Corrupted,
    UnsupportedVersion,
    IncompatibleContent
}
```

결과에는 사용자용 문구가 아니라 상태 코드와 내부 진단 요약만 둔다. Presenter가 표시 문구를 결정한다.

### 8.2 파일 저장소

`IRunSaveFileStore`는 경로 문자열과 읽기·쓰기·이동·존재 확인을 추상화한다. 테스트는 메모리 또는 임시 디렉터리 구현을 사용해 실제 `Application.persistentDataPath`를 건드리지 않는다.

`SystemRunSaveFileStore`만 Unity의 영구 데이터 경로를 해석한다. 파일 이름에 디렉터리 이동 문자열이나 절대 경로를 허용하지 않는다.

### 8.3 원자적 쓰기 순서

1. `run-save.tmp` 정리 가능 여부 확인
2. 직렬화 문자열을 임시 파일에 기록
3. 임시 파일 재로드·검증
4. 기존 백업 삭제가 아닌 교체 준비
5. 기존 기본 파일을 백업으로 이동
6. 임시 파일을 기본 파일로 이동
7. 6이 실패하면 백업을 기본으로 복구

모든 파일 연산은 테스트에서 단계별 실패를 주입할 수 있어야 한다.

## 9. 불러오기와 복원

### 9.1 불러오기

- 기본 파일을 먼저 읽는다.
- JSON 파싱과 도메인 검증을 모두 통과해야 성공이다.
- 기본 파일 실패 시 백업을 같은 절차로 읽는다.
- 읽기만으로 파일을 자동 수정하지 않는다.
- 유효한 터미널 저장은 `NoContinueTerminalSave`로 구분한다.

### 9.2 복원

`RunRestoreFactory`는 다음 순서로 새 객체를 만든다.

1. 카드·악마 정의 키를 현재 카탈로그에서 모두 확인
2. `RunCardDefinition`, `RunDemonDefinition` 목록 생성
3. 영혼·골드·마지막 발급 ID를 가진 새 `PlayerRunState` 생성
4. 루트 시드와 콘텐츠 리비전으로 현재 경로 정의 생성
5. 스테이지 인덱스·다음 콘텐츠를 가진 새 `RunProgress` 생성
6. 생성 순번을 가진 후보·보상·상점·사건 생성기 생성
7. 새 세션 전체를 검증
8. `StageProgressionRuntime`이 기존 세션을 한 번에 교체

어느 단계든 실패하면 기존 Runtime 세션과 파일을 변경하지 않는다.

## 10. 런 예약

`RunReservation`은 다음 정보만 저장한다.

```text
reservationVersion
runId
rootSeed
startingDemonOfferId
startingDemonDefinitionKeys[2]
createdAtUtc
```

- 새 게임 확인 뒤 후보 화면 전에 기록한다.
- 동일 예약을 다시 열면 같은 후보를 보여 준다.
- 선택 성공 뒤 첫 체크포인트가 저장된 경우에만 예약을 삭제한다.
- 체크포인트 쓰기 실패 시 예약과 선택 결과를 메모리에 유지하고 재시도한다.
- 예약 파일은 `이어하기`가 아니라 `새 런 시작 선택 계속`으로 분류한다.

## 11. Runtime·UI 연결

### 11.1 Runtime

`RunSaveCoordinator`는 다음만 조정한다.

- 시작 시 저장 상태 검사
- 새 게임 예약 생성
- 성공 전이 뒤 체크포인트 쓰기
- 이어하기용 새 세션 구성
- 저장 중 중복 요청 차단
- 최근 저장·오류 상태 노출

전투 규칙·골드 계산·상점 거래 결과를 직접 계산하지 않는다.

### 11.2 메인 메뉴

- `Continue` 활성 여부
- `New Game` 덮어쓰기 확인
- 백업 복구 안내
- 호환되지 않는 저장 안내
- 새 런 선택 예약 재개

### 11.3 진행 화면

- 저장 중·완료 아이콘
- 저장 실패 재시도
- 다음 콘텐츠 이동 잠금

`GameScene`은 체크포인트를 만들지 않으므로 1차 저장 UI를 추가하지 않는다.

## 12. 시작 선택·RF·사건 통합 계약

첫 체크포인트를 연결하려면 시작 악마 후보 2장과 확정된 1장을 소유하는 실제 도메인 상태가 먼저 필요하다. 저장 시스템은 현재 `PlayerRunState.CreatePrototypeDemonDeck()`의 악마 4장을 새 규칙의 선택 결과로 간주하지 않는다.

저장 시스템은 HONG의 내부 `RunFlow`·`Shop` 필드를 직접 읽지 않는다. RF 쪽에서 다음 읽기 전용 정보를 제공해야 한다.

- 현재 골드
- 상점 방문 ID와 완료 여부
- 다음 상점 상품을 재현하는 시드·순번 또는 상품 키
- 상점 나가기 성공 결과
- 사건 ID, 선택 결과와 완료 여부
- 다음 콘텐츠 종류

실제 타입이 없을 때 임시 상점·사건 클래스를 저장 시스템에 만들지 않는다.

## 13. 테스트 명세

### 13.1 SV-01 스냅샷·검증

| ID | 검증 |
| --- | --- |
| SV01-U01 | 영혼·일반/악마 덱·ID가 스냅샷에 보존된다 |
| SV01-U02 | 중복 카드 ID를 거부한다 |
| SV01-U03 | 알 수 없는 정의 키·무늬를 거부한다 |
| SV01-U04 | 마지막 발급 ID가 현재 최대 ID보다 작으면 거부한다 |
| SV01-U05 | 체크포인트·다음 콘텐츠 조합을 검증한다 |
| SV01-U06 | 전투·보류 선택 상태 캡처를 거부한다 |
| SV01-U07 | 원본 컬렉션 변경이 스냅샷에 영향을 주지 않는다 |

### 13.2 SV-02 파일·버전

| ID | 검증 |
| --- | --- |
| SV02-U01 | v1 왕복 직렬화가 모든 필드를 보존한다 |
| SV02-U02 | 쓰기 성공 시 기본·백업 순서가 맞다 |
| SV02-U03 | 임시 쓰기 실패가 기존 기본 파일을 보존한다 |
| SV02-U04 | 교체 실패 시 백업으로 기본 파일을 복구한다 |
| SV02-U05 | 손상 기본 파일에서 백업을 읽는다 |
| SV02-U06 | 둘 다 손상되면 이어하기를 비활성화한다 |
| SV02-U07 | 높은 버전과 콘텐츠 불일치를 구분한다 |
| SV02-U08 | 빈 레거시 `save.game`을 저장 없음으로 처리한다 |

### 13.3 SV-03 복원

| ID | 검증 |
| --- | --- |
| SV03-I01 | 저장 전후 영혼·덱·악마 덱·ID가 같다 |
| SV03-I02 | 제거된 최고 ID 뒤에도 새 카드 ID를 재사용하지 않는다 |
| SV03-I03 | 스테이지 인덱스·다음 콘텐츠가 복원된다 |
| SV03-I04 | 복원 실패가 기존 세션을 바꾸지 않는다 |
| SV03-I05 | 같은 시드·순번에 같은 상대·보상 후보를 만든다 |

### 13.4 SV-04~SV-06 체크포인트·UI·반복

| ID | 검증 |
| --- | --- |
| SV04-I01 | 시작 악마 선택 성공 뒤 한 번 저장한다 |
| SV04-I02 | 카드 보상·골드 완료 뒤 한 번 저장한다 |
| SV04-I03 | 상점 나가기·사건 완료 뒤 한 번 저장한다 |
| SV04-I04 | 실패·중복·오래된 입력은 저장하지 않는다 |
| SV05-I01 | 전투·보상·상점 중 종료가 이전 체크포인트로 돌아간다 |
| SV05-I02 | 런 예약 재접속에 같은 시작 후보를 표시한다 |
| SV05-I03 | 저장 실패 중 다음 콘텐츠 입력을 잠근다 |
| SV06-I01 | 저장·종료·이어하기 10회에서 상태와 ID가 격리된다 |
| SV06-I02 | 승리·패배 저장은 이어하기를 비활성화한다 |
| SV06-I03 | 실제 프로세스 재실행·두 해상도·Console이 통과한다 |

## 14. 외부 의존성과 보안

- 새 패키지와 오픈소스 의존성을 추가하지 않는다.
- Unity의 기존 JSON·파일 API와 .NET 표준 I/O를 사용한다.
- 저장 파일은 암호화하거나 신뢰할 수 있는 서버 데이터로 취급하지 않는다.
- 파일 내용은 불신 입력으로 보고 길이·버전·키·범위를 검증한다.
- 예외 전체 경로와 사용자 이름을 게임 UI에 노출하지 않는다.
- 외부 자료나 라이브러리가 추가되면 이름·버전·URL·라이선스·사용 위치를 AI 활용 문서에 기록한다.

## 15. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-26 | 이천서 | SV-01 실제 구현에 맞춰 불변 스냅샷·카탈로그/ID/스테이지/체크포인트 검증·`StageCleared`/런 종료 캡처·Unity 비의존 기준과 7개 대상 테스트 완료 상태 반영 |
| 2026-07-26 | 이천서 | 순수 스냅샷·파일 DTO·검증·원자 저장·백업 불러오기·복원 Factory·런 예약·RF 연동·SV 테스트 명세 수립 |
