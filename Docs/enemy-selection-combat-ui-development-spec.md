# 상대 선택·적 전투 정보 UI 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·개발 책임자: 이천서  
> 작업 식별자: EUI-00~EUI-05  
> 버전: v0.1  
> 상태: EUI-05 반복·실제 검증 완료, 1차 범위 마감
> 최종 갱신: 2026-07-20

## 1. 기술 목표

기존 고정 스테이지와 프로필 없는 독립 전투를 유지하면서 다음 단일 흐름을 추가한다.

```text
EnemyCombatProfileCatalog.Previews
  → OpponentSelectionGenerator
  → OpponentSelectionOffer
  → StageProgressionState.OpponentSelection
  → TrySelectOpponent(offerId, profileKey)
  → StageDefinition.CreateForEnemyProfile
  → StageBattleFactory
  → CoreLoopBattle
  → 등급별 EnemyCombatDisplaySnapshot
  → StageProgressionView / CoreLoopView
```

선택 UI는 프로필의 읽기 전용 미리보기만 소비하고, 전투 UI는 공개 정보에서 파생한 표시 스냅샷만 소비한다.

## 2. 현재 코드 기준선

- `EnemyCombatProfileCatalog.Default`에 일반 3종·엘리트 1종·보스 1종이 등록되어 있다.
- `EnemyProfilePreview`는 키·이름·등급·최대 영혼·요약·예상 보상을 제공한다.
- `StageDefinition.CreateForEnemyProfile(...)`가 키에서 최대 영혼을 파생하고 보스/일반 스테이지 불일치를 거절한다.
- `StageBattleFactory`가 프로필 전용 10장 덱과 새 정책 인스턴스로 실제 전투를 만든다.
- `StageProgressionSession`은 선택된 프로필의 보상 등급을 사용한다.
- `StageProgressionState.OpponentSelection`과 후보·제안·결정적 생성기가 EUI-01에서 구현되었다.
- `StageProgressionSession`은 선택 기능을 선택적으로 주입받고 Pending Offer·ActiveStage·활성 여부를 제공한다.
- `StageProgressionRuntime`은 EUI-02부터 결정적 상대 생성기를 주입하고 일반전 진입 시 선택 제안을 만든다.
- `StageProgressionPresenter`는 기존 `RunProgress` 호환 오버로드와 세션·집중 키 오버로드를 제공하고 후보 2명의 안전 미리보기를 표시한다.
- `StageProgressionController`는 현재 제안 ID와 집중 키를 로컬로 소유하며 선택 상태에서는 전투 씬을 열지 않고 같은 화면을 갱신한다.
- `StageProgressionView`는 후보 비교 카드·단일 집중 강조·확정 가능 상태를 기존 IMGUI 안에 표시하며 확정 이벤트는 Controller의 EUI-03 세션 처리로 연결된다.
- `StageProgressionSession.TrySelectOpponent(offerId, profileKey)`가 현재 제안·스테이지 인덱스·정확한 후보 키를 검증하고, 템플릿 ID·시드와 미리보기 이름으로 해석 완료 스테이지·전투를 먼저 준비한 뒤 성공 시에만 상태를 교체한다.
- `StageProgressionController`는 확정 이벤트를 현재 OfferId·집중 키와 함께 세션에 전달하고 성공한 `InBattle + Battle`에서만 `CoreLoopTest`를 연다.
- `EnemyObservationFactory.CreateNumberInferences(...)`가 정책 관측과 UI 표시에서 같은 공개 숫자 추론 계산을 재사용한다.
- `EnemyCombatDisplaySnapshotFactory`가 현재 전투와 프로필 키를 일반·엘리트·보스별 안전 표시 스냅샷으로 변환하며 정책 `Decide`를 호출하지 않는다.
- `CoreLoopPresenter`는 선택적으로 프로필 키를 받아 적 이름·등급·성향·정보 제목·값·경고를 ViewModel 문자열로 만든다.
- `CoreLoopController`는 진행 전투일 때만 `ActiveStage.BattleProfileKey`를 전달하고 프로필 없는 독립 전투는 명시적인 호환 표시를 사용한다.
- `CoreLoopView`는 기존 IMGUI 안에 적 정보 패널을 표시하며 720p에서 글꼴·여백·버튼 높이를 조정하는 반응형 규칙을 적용한다.
- EUI-04 기준 신규 14/14, StageProgression 117/117, CoreLoop 193/193, 전체 EditMode 310/310과 실제 일반·엘리트·보스의 1280×720·1920×1080 화면을 통과했다.

## 3. 설계 원칙

1. 후보·표시 데이터는 프로필 카탈로그에서 파생한다.
2. 선택은 인덱스가 아니라 `ProfileKey`와 `OfferId`로 검증한다.
3. 후보 지정과 진행 상태 확정을 분리한다.
4. 전투 생성 성공 전에 진행 상태를 `InBattle`로 확정하지 않는다.
5. 추론 UI와 적 정책은 같은 공개 정보 계산을 재사용한다.
6. UI에 전체 `EnemyObservation`이나 정책 객체를 전달하지 않는다.
7. 문자열 변환은 Presenter, 입력 전달은 Controller, 그리기는 View가 맡는다.
8. 선택 기능을 주입하지 않은 기존 세션은 고정 스테이지 동작을 유지한다.
9. 새 asmdef·외부 패키지·ScriptableObject 프레임워크를 추가하지 않는다.
10. 1차 범위는 기존 IMGUI 컴포넌트를 확장하고 씬 직렬화를 최소화한다.

## 4. 상대 선택 데이터 타입

### 4.1 `OpponentSelectionCandidate`

불변 후보 한 명을 나타낸다.

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| `ProfileKey` | `string` | 안정적인 선택·전투 변환 키 |
| `Preview` | `EnemyProfilePreview` | 카탈로그에서 얻은 읽기 전용 미리보기 |

생성 시 키와 `Preview.ProfileKey`가 다르면 거절한다. UI 편의를 위해 이름·영혼·보상을 복제한 필드를 추가하지 않는다.

### 4.2 `OpponentSelectionOffer`

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| `OfferId` | `int` | 현재 제안을 식별하는 단조 증가 ID |
| `StageIndex` | `int` | 제안이 속한 현재 스테이지 |
| `Candidates` | 읽기 전용 목록 | 정확히 2개의 서로 다른 후보 |

생성 조건:

- 후보 수는 정확히 2개
- null·빈 키·중복 키 금지
- Boss 등급 금지
- Elite 등급 최대 1명
- `OfferId >= 0`, `StageIndex >= 0`

### 4.3 `OpponentSelectionGenerator`

권장 생성자:

```csharp
OpponentSelectionGenerator(
    EnemyCombatProfileCatalog catalog,
    int seed,
    int eliteOfferChancePercent = 35)
```

권장 API:

```csharp
OpponentSelectionOffer Generate(int stageIndex);
```

- 카탈로그에서 Boss를 제외하고 Normal·Elite만 분류한다.
- 0~100 밖의 엘리트 확률은 거절한다.
- 일반 프로필이 2개 미만이거나 엘리트 조합에 필요한 풀이 없으면 생성 전에 실패한다.
- 같은 시드와 같은 호출 순서에서 같은 제안을 만든다.
- 후보 순서도 같은 결정적 난수에서 파생한다.
- `OfferId`는 생성기 인스턴스 안에서 단조 증가하며 재시작 시 생성기를 새로 만들어 초기화한다.

## 5. 진행 상태 확장

`StageProgressionState`에 `OpponentSelection`을 추가한다.

### 5.1 세션 속성

`StageProgressionSession`에 다음 읽기 경계를 추가한다.

```csharp
public OpponentSelectionOffer PendingOpponentSelection { get; }
public StageDefinition ActiveStage { get; }
public bool IsOpponentSelectionEnabled { get; }
```

- `PendingOpponentSelection`은 선택 상태에서만 존재한다.
- `ActiveStage`는 실제 전투·보상에 사용한 해석 완료 스테이지다.
- 선택 대기 중에는 `ActiveStage == null`, 전투 중·보상 중에는 선택된 스테이지를 유지한다.
- 고정 프로필·기존 호환 세션에서는 `ActiveStage`가 `Progress.CurrentStage`와 같다.

### 5.2 선택 기능 주입

기존 생성자 호환을 위해 `OpponentSelectionGenerator` 또는 동일 역할의 함수를 선택 인자로 추가한다.

- 주입 값이 null: 기존 고정 스테이지 흐름 유지
- 주입 값이 존재: 일반 전투 진입 전에 상대 선택 수행
- 최종 보스 스테이지: 선택 기능 주입 여부와 관계없이 고정 프로필로 바로 전투 생성

현재 프로토타입의 normal 스테이지 정의는 선택 해석에 필요한 ID·덱 시드를 제공하는 템플릿으로 사용한다. 선택 결과는 템플릿 ID·시드와 프로필 미리보기 이름을 이용해 새 `StageDefinition`으로 만들고 `RunProgress`의 원본 목록을 변경하지 않는다.

### 5.3 상태 전이 API

권장 공개 API:

```csharp
public bool TrySelectOpponent(int offerId, string profileKey);
```

EUI-03에서 위 확정 API와 선택 프로필 전투 생성을 구현했다.

처리 순서:

1. 현재 상태가 `OpponentSelection`인지 확인한다.
2. 현재 `PendingOpponentSelection.OfferId`와 입력 ID가 같은지 확인한다.
3. 후보 목록에 정확히 같은 `profileKey`가 있는지 확인한다.
4. 현재 스테이지 템플릿으로 `StageDefinition.CreateForEnemyProfile(...)`을 호출한다.
5. 선택된 스테이지로 `CoreLoopSession`을 생성하고 전투 시작 가능성을 확인한다.
6. 모든 준비가 성공하면 `ActiveStage`와 전투 세션을 교체한다.
7. 제안을 제거하고 진행 상태를 `InBattle`로 전환한다.

1~5 중 실패하면 제안·집중 후보·플레이어 영혼·덱·현재 스테이지·전투 참조를 변경하지 않는다.

### 5.4 시작·다음 스테이지·재시작

- `TryStartRun`: 선택 기능이 켜진 일반전이면 제안 생성 후 `OpponentSelection`, 고정 보스면 기존 `InBattle`
- `TryAdvanceToNextStage`: 다음 스테이지가 일반전이면 새 제안, 최종 보스면 즉시 고정 전투
- `TryRestartRun`: 제안 ID·생성기·집중 후보·활성 스테이지를 초기화하고 첫 상대 선택으로 복귀
- 전투 패배: 제안 없이 `RunDefeat`
- 보상 선택 중: 이전 `ActiveStage`를 유지해 보상 등급·표시에 사용

## 6. 상대 선택 표시 구조

### 6.1 ViewModel

```csharp
public sealed class OpponentCandidateViewModel
{
    public string ProfileKey { get; }
    public string DisplayName { get; }
    public string Grade { get; }
    public string MaximumSoul { get; }
    public string Summary { get; }
    public string RewardTier { get; }
    public bool IsFocused { get; }
}
```

`StageProgressionViewModel` 추가 속성:

```csharp
public int? OpponentOfferId { get; }
public IReadOnlyList<OpponentCandidateViewModel> OpponentCandidates { get; }
public string FocusedOpponentProfileKey { get; }
public bool CanFocusOpponent { get; }
public bool CanConfirmOpponent { get; }
```

`StageProgressionPresenter.Create`는 `RunProgress` 대신 `StageProgressionSession`과 선택적으로 집중된 키를 받는다.

```csharp
Create(StageProgressionSession session, string focusedProfileKey = null)
```

Presenter는 현재 제안에 없는 집중 키를 무시하고 `CanConfirmOpponent = false`로 만든다.

### 6.2 View와 Controller

`StageProgressionView` 이벤트:

```csharp
event Action<string> OpponentFocused;
event Action OpponentConfirmed;
```

`StageProgressionController`가 `_focusedOpponentProfileKey`를 소유한다.

- 후보 클릭: 현재 제안에 속하면 집중 키만 변경하고 다시 렌더링
- 확정: 현재 `OfferId`와 집중 키를 세션에 전달
- 선택 성공: 집중 키 초기화 후 전투 씬 로드
- 선택 실패: 입력 잠금 해제 후 같은 선택 화면 렌더링
- 새 제안·재시작: 이전 집중 키 초기화

Controller는 작업 성공 여부뿐 아니라 결과 상태를 보고 씬을 결정한다.

```text
성공 + InBattle + Battle != null → CoreLoopTest
성공 + OpponentSelection       → StageTest에서 Refresh
성공 + Reward/Result 상태      → StageTest에서 Refresh
```

## 7. 전투 정보 표시 데이터

### 7.1 `EnemyInferenceDisplayEntry`

```csharp
public readonly struct EnemyInferenceDisplayEntry
{
    public int Number { get; }
    public int? ProbabilityPercent { get; }
}
```

- 일반 적: 확률 존재
- 엘리트·보스: 원시 확률을 담지 않는다.

### 7.2 `EnemyCombatDisplaySnapshot`

도메인 값만 가진 불변 스냅샷으로 작성한다.

| 속성 | 설명 |
| --- | --- |
| `ProfileKey` | 실제 전투 프로필 키 |
| `DisplayName` | 미리보기 이름 |
| `Grade` | Normal / Elite / Boss |
| `Summary` | 핵심 성향 |
| `InformationMode` | Standard / Condensed / PhaseDependent |
| `InferenceEntries` | 일반 최대 3개, 엘리트 최대 2개, 보스 빈 목록 |
| `Confidence` | 엘리트·보스 신뢰도, 일반은 null |
| `BossPhase` | 보스만 값 존재 |
| `BossInferenceDirection` | 보스만 값 존재 |
| `BossTelegraphedAction` | 보스만 값 존재 |

정확한 다음 행동, `EnemyDecision.Score`, 이유 코드, 정책 키, 카드 ID, 비공개 카드 값과 덱 순서는 포함하지 않는다.

### 7.3 생성 경계

권장 내부 API:

```csharp
EnemyCombatDisplaySnapshotFactory.Create(
    CoreLoopBattle battle,
    string profileKey)
```

- 프로필 키는 카탈로그에서 검증한다.
- 숫자 추론은 `EnemyObservationFactory`와 같은 공개 구성 계산을 재사용한다.
- 계산 중복을 막기 위해 공개 숫자 추론 생성 부분을 내부 공통 함수로 추출한다.
- 일반 적은 확률 내림차순·숫자 오름차순 상위 3개를 복사한다.
- 엘리트는 `EnemyInferenceDisplayModel.CreateForElite` 결과를 사용한다.
- 보스는 `FinalBossEnemyPolicy.CurrentDisplay`가 있으면 사용하고, 아직 첫 결정 전이면 현재 영혼·공개 추론에서 구간·방향·신뢰도를 만들며 예고는 `None`으로 둔다.
- 프로필 없는 전투에는 null을 반환하거나 명시적인 `Unavailable` 스냅샷을 사용한다. 임의 프로필을 선택하지 않는다.

## 8. CoreLoop 표시 연결

`CoreLoopPresenter` 권장 API:

```csharp
Create(CoreLoopBattle battle, string profileKey = null)
```

`CoreLoopViewModel`에 다음 표시용 문자열·목록을 추가한다.

- `EnemyDisplayName`
- `EnemyGrade`
- `EnemySummary`
- `EnemyInformationTitle`
- `EnemyInformationLines`
- `EnemyWarning`

`CoreLoopController`는 진행 전투일 때 `StageProgressionSession.ActiveStage.BattleProfileKey`를 Presenter에 넘긴다. 독립 전투는 null을 넘긴다.

Presenter 문자열 규칙:

| 등급 | 제목 | 내용 |
| --- | --- | --- |
| Normal | `INFERENCE` | `7  30%` 형식 최대 3줄 |
| Elite | `ELITE INFERENCE` | `LIKELY 4 · 7`, `CONFIDENCE MEDIUM` |
| Boss | `BOSS PATTERN` | `PHASE`, `DIRECTION`, `CONFIDENCE`; 예고는 별도 경고 |
| 프로필 없음 | `ENEMY INFORMATION` | `NO PROFILE INFORMATION` |

`CoreLoopView`는 기존 적 참가자 패널 아래 또는 전투 메시지 위에 정보 패널을 그린다. 경고는 행동 버튼과 겹치지 않는 별도 행에 표시한다.

## 9. 공정성과 보안 경계

- UI 표시 계산은 플레이어 공개 카드·공개 버림·공개 덱 구성과 비공개 카드 장수만 사용한다.
- 실제 플레이어 비공개 카드 객체·값·물리 ID를 전달하지 않는다.
- 플레이어 덱의 저장 순서와 다음 카드도 전달하지 않는다.
- 후보 미리보기는 프로필의 내부 덱 정의·정책 키를 노출하지 않는다.
- 적 정책과 UI는 동일한 공개 계산 결과를 사용해야 한다.
- UI가 표시를 위해 정책의 `Decide`를 추가 호출하지 않는다. 추가 호출은 상태형 보스 예고를 오염시킬 수 있다.
- View와 Presenter는 전투 상태를 변경하지 않는다.

## 10. 실패 원자성과 입력 잠금

- 제안 생성 실패: 런 시작·다음 스테이지 전이를 확정하지 않는다.
- 잘못된 `OfferId`: false 반환, 상태 무변경
- 후보에 없는 키: false 반환, 상태 무변경
- 알 수 없는 프로필: 전투 생성 전 예외 또는 false, 현재 제안 유지
- 일반 선택 화면의 보스 키: 거절
- 두 번 확정: 첫 성공 뒤 상태가 바뀌므로 두 번째 입력 거절
- 처리 중 모든 후보·확정 버튼 잠금
- 씬 로드 실패는 진행 상태와 별도 기반 시설 오류로 기록하며 중복 전투를 생성하지 않는다.

## 11. 테스트 명세

EUI-00 기준 전체 EditMode 260/260을 회귀 기준으로 사용한다.

### EUI-01 후보·상태 기반 — 최소 10개

| ID | 검증 내용 |
| --- | --- |
| EUI01-U01 | 같은 시드·호출 순서에서 같은 후보·순서·OfferId |
| EUI01-U02 | 제안마다 후보 정확히 2명, 키 중복 없음 |
| EUI01-U03 | 0%에서 서로 다른 일반 2명 |
| EUI01-U04 | 100%에서 일반 1명+엘리트 1명 |
| EUI01-U05 | 보스·엘리트 2명·잘못된 확률·부족한 풀 거절 |
| EUI01-U06 | Offer 불변성과 null·중복·등급 오염 거절 |
| EUI01-U07 | 기능 미주입 세션의 기존 고정 전투 유지 |
| EUI01-U08 | 일반 시작·다음 스테이지·재시작이 선택 상태 생성 |
| EUI01-I01 | 보스 스테이지는 선택 없이 전투 시작 |
| EUI01-I02 | 선택 상태에서 전투·보상 입력 전부 거절 |

실제 EUI-01은 위 조건을 세분화한 13개 테스트로 구현했으며, 다음 스테이지 선택 제안과 재시작 시 OfferId 초기화도 함께 검증했다.

### EUI-02 선택 화면 — 최소 7개

| ID | 검증 내용 |
| --- | --- |
| EUI02-U01 | 후보 ViewModel이 미리보기 5개 필드와 키를 정확히 표시 |
| EUI02-U02 | 일반·엘리트·보상 등급 문자열 매핑 |
| EUI02-U03 | 집중 전에는 확정 불가, 후보 집중 뒤 확정 가능 |
| EUI02-U04 | 현재 제안에 없는 집중 키 무시 |
| EUI02-U05 | 선택 상태 외 후보·확정 입력 거절 |
| EUI02-I01 | 후보 클릭은 세션·런·전투 상태를 변경하지 않음 |
| EUI02-I02 | 입력 잠금 중 중복 클릭·확정 차단 |

### EUI-03 실제 전투 통합 — 14개 완료

| ID | 검증 내용 |
| --- | --- |
| EUI03-I01 4건 | 일반 3종·엘리트 선택이 실제 키·덱 10장·영혼·정책과 일치하고 Pending 제거·ActiveStage 설정·InBattle 전이 |
| EUI03-U01 4건 | 이전 OfferId·후보 외 보스 키·대소문자 불일치·빈 키 실패 원자성 |
| EUI03-U02 | 전투 Factory 예외에서 제안·진행·플레이어 상태 무변경 |
| EUI03-I02 2건 | 일반·엘리트 선택이 각각 Standard·HighGrade 보상으로 연결 |
| EUI03-I03 | 첫 선택→전투→보상→다음 선택에서 영혼·덱 유지와 OfferId 증가 |
| EUI03-I04 | 두 번째 선택→전투→보상→고정 보스 진입 |
| EUI03-I05 | 보스 승리·보상 뒤 재시작에서 첫 제안·영혼·덱 초기화 |

### EUI-04 전투 정보 UI — 14개 완료

| ID | 검증 내용 |
| --- | --- |
| EUI04-U01 | 일반 상위 3개 확률·정렬·동률 순서 |
| EUI04-U02 | 일반 표시와 정책 입력 추론 값 일치 |
| EUI04-U03 | 엘리트 상위 2개·신뢰도, 원시 확률 미노출 |
| EUI04-U04 | 첫 정책 결정 전에도 보스 구간·방향·신뢰도 안전 표시 |
| EUI04-U05 | 보스 예고 범주 표시·실행 뒤 해제 동기화 |
| EUI04-U06 | 비공개 카드 없음·추론 불가 안전 표시 |
| EUI04-U07 | 프로필 없는 독립 전투의 명시적 호환 표시 |
| EUI04-U08~U10 | 일반·엘리트·보스 Presenter 문자열과 정보량 규칙 |
| EUI04-U11 | 표시 생성 과정에서 적 정책 `Decide` 미호출 |
| EUI04-U12 | 알 수 없는 프로필 키에 임의 대체 정보 미생성 |
| EUI04-I01 | 플레이어 행동 뒤 Presenter 새 정보 반영 |
| EUI04-I02 | 표시 객체에 비공개 값·덱 순서·카드 ID·정확한 다음 행동 없음 |

### EUI-05 반복·실제 검증 — 5개 완료

| ID | 검증 내용 |
| --- | --- |
| EUI05-I01 | 일반+일반 선택→전투→보상 10회 |
| EUI05-I02 | 일반+엘리트 양쪽 선택→전투→보상 각 10회 |
| EUI05-I03 | 두 선택 전투→고정 보스→보상→재시작 10회 |
| EUI05-I04 | 오래된 제안·중복 확정·씬 왕복 상태 누출 10회 |
| EUI05-I05 | 기존 고정 세션·독립 전투·카드·보상 전체 회귀 |

실제 구현은 `OpponentSelectionSystemValidationTests`의 `EUI05-V01`~`V05` 다섯 테스트로 위 시나리오를 고정했다. V01·V03·V04·V05는 각 10회, V02는 일반과 엘리트를 각각 10회 실행한다. 카드 사용·보상 내부 규칙은 중복 테스트를 만들지 않고 기존 CoreLoop 193개와 StageProgression 122개 전체 회귀로 함께 검증한다.

## 12. 예상 파일 배치

```text
Assets/01. Scripts/Runtime/StageProgression/OpponentSelection/
├─ OpponentSelectionCandidate.cs
├─ OpponentSelectionOffer.cs
└─ OpponentSelectionGenerator.cs

Assets/01. Scripts/Runtime/CoreLoop/EnemyAI/
├─ EnemyInferenceDisplayEntry.cs
├─ EnemyCombatDisplaySnapshot.cs
└─ EnemyCombatDisplaySnapshotFactory.cs

Assets/01. Scripts/Runtime/UI/StageProgression/
├─ StageProgressionPresentation.cs   (확장)
├─ StageProgressionView.cs           (확장)
└─ StageProgressionController.cs     (확장)

Assets/01. Scripts/Runtime/UI/CoreLoop/
├─ CoreLoopPresentation.cs           (확장)
├─ CoreLoopView.cs                   (확장)
└─ CoreLoopController.cs             (확장)

Assets/06.Packages/Tests/EditMode/StageProgression/
├─ OpponentSelectionFoundationTests.cs
├─ OpponentSelectionPresentationTests.cs
├─ OpponentSelectionIntegrationTests.cs
└─ OpponentSelectionSystemValidationTests.cs

Assets/06.Packages/Tests/EditMode/CoreLoop/
└─ EnemyCombatPresentationTests.cs
```

새 런타임·테스트 asmdef는 만들지 않고 기존 `Border`, `DiaBlackJack.CoreLoop.Tests.EditMode`, `DiaBlackJack.StageProgression.Tests.EditMode`를 사용한다.

## 13. 성능·품질 기준

- 후보 생성은 프로필 수에 선형이며 현재 5종에서 즉시 완료되어야 한다.
- 전투 정보 갱신은 현재 공개 덱 구성 한 번만 순회한다.
- Presenter는 프레임마다 정책 결정을 다시 실행하지 않는다.
- ViewModel과 표시 스냅샷은 외부에서 변경할 수 없다.
- 새 외부 패키지·이미지·오디오·폰트를 사용하지 않는다.
- `StageTest`, `CoreLoopTest` 씬 파일은 스크립트 확장만으로 가능하면 수정하지 않는다.

## 14. 완료 검증

- 변경 C# 컴파일 오류 0
- 단계별 신규 테스트와 관련 어셈블리 통과
- 전체 EditMode 회귀 통과
- 1280×720·1920×1080 선택·일반·엘리트·보스 화면 확인
- `StageTest`·`CoreLoopTest` 누락 스크립트·깨진 프리팹 0
- 실제 선택→전투→보상→다음 선택→보스→재시작 흐름 확인
- 최종 Console Error/Warning 0
- 패키지·외부 에셋 변경 없음 확인
- AI 활용과 이천서 역할 기록 갱신

## 15. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-20 | 이천서 | EUI-01 후보 불변 타입·결정적 생성기·선택 대기 상태·세션 주입과 신규 13/13·전체 EditMode 273/273 검증 결과 반영 |
| 2026-07-20 | 이천서 | 후보 생성·OfferId·선택 상태·ActiveStage·등급별 안전 표시 스냅샷·Presenter/View/Controller 연결과 EUI 테스트 명세를 구현 가능한 기준으로 확정 |
| 2026-07-20 | 이천서 | EUI-02 후보 ViewModel·세션 Presenter·로컬 집중·선택 상태 화면 갱신·프로토타입 Runtime 활성화와 신규 9/9·전체 EditMode 282/282·두 해상도 화면 검증 결과 반영 |
| 2026-07-20 | 이천서 | EUI-03 OfferId+ProfileKey 확정·실패 원자성·실제 프로필 전투/보상·두 번째 선택·고정 보스·재시작 통합과 신규 14/14·전체 EditMode 296/296·실제 씬 전환 검증 결과 반영 |
| 2026-07-20 | 이천서 | EUI-04 공개 추론 공유 계산·등급별 안전 표시 스냅샷·Presenter/View/Controller 연결과 신규 14/14·CoreLoop 193/193·StageProgression 117/117·전체 EditMode 310/310·두 해상도 화면 검증 결과 반영 |
| 2026-07-20 | 이천서 | EUI-05 반복 검증 5종·StageProgression 122/122·CoreLoop 193/193·전체 EditMode 315/315, 실제 두 선택·보상·고정 보스·재시작 씬 왕복과 두 해상도·Console 0 결과를 반영해 명세를 마감 |
