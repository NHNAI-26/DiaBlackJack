# 자동 발동 카드 시스템 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·개발 책임자: 이천서  
> 작업 식별자: AC-00~AC-06, AC-RV00~AC-RV03
> 버전: v1.2
> 상태: 동시 선택·효과 우선순위 개발 명세 확정, 코드 이관 대기
> 최종 갱신: 2026-08-02

> **현행 획득 경로 변경(2026-07-30)**
> 정식 런에서는 `BattleRewardCatalog`가 아니라 RF-02에서 구현된 상점 생성기가 `CardDefinitionCatalog.All`에서 자동 발동 카드 5종을 포함한 일반 후보를 만든다. 구매된 자동 카드는 기존 `RunCardDefinition`·`StageBattleFactory` 경로로 다음 전투에 전달한다. RW 카탈로그는 유산 기능으로 유지한다.

> **현행 효과 변경(2026-07-30)**
> 독극물은 영혼 3 또는 현재 보유 영혼 전부를 담보로 걸고 승리 시 영혼을 5개로 돌려받는 문구를 사용한다. 부활초는 양쪽이 각각 영혼 1개를 버릴 수 있고, 지불한 참가자만 자기 손패 전부를 버린 뒤 비공개 1장과 공개 1장을 다시 받는다. 선택은 독립적이다. 아래 `CurrentSoul >= 2` 제한과 기존 `RestartRound` 기록은 과거 구현 증거이며 AC-RV03에서 현행 코드에서 제거했다.

> **현행 숫자 변경 안내**
> 현행 정의는 독극물 1, 부활초 2, 거짓말 탐지기 3, 화염 방사기 4, 회중시계 5다. AC-RV01에서 `poison-1`, `flamethrower-4`, `pocket-watch-5`와 관련 보상·런/전투 변환·테스트를 원자적으로 이관했다.

> **현행 정보 경계 안내 (2026-07-29)**
> `BlackjackHand`의 비공개 역할은 `IsFaceUp`과 별도다. 앞면 공개된 비공개 숫자는 양측의 확정 공개 정보지만 공개 합·중간 버스트·공개 카드 대상에는 포함하지 않는다. 거짓말 탐지기 결과는 아직 뒤집히지 않은 숫자를 새로 노출하지 않으며, 소유자 전용 비교 지식만 제공한다.

> **AC-RV02 재명세 (2026-07-29)**
> 사기꾼은 거짓말 탐지기 3장, 집행자는 독극물 3장·거짓말 탐지기 2장·화염 방사기 2장·회중시계 2장을 가진다. 집행자는 첫 라운드 포함 매 라운드 최초 배분 전에 플레이어의 드로우·버린 더미와 새 독극물 1장을 함께 섞는다. 같은 라운드부터 뽑을 수 있고 전투 종료 시 주입 카드를 모두 제거한다. EP-R05에서 프로필 플래그·전투 한정 물리 ID·누적·종료 정리를 구현·검증했다.

## 1. 기술 목표

기존 `CoreLoopBattle`의 전투 순서, `CardEffectResolver`의 수동 효과, 악마 계약 훅과 `StageProgressionSession`의 종료 동기화를 유지하면서 자동 발동 카드의 **공개 유입 → 효과 해결 → 버스트 검사 → 원래 처리 재개**를 추가한다.

핵심 기술 목표는 다음과 같다.

1. 최초 배분을 제외한 모든 공개 카드 유입을 하나의 조정 경계로 모은다.
2. UI 프레임을 넘어가는 선택에도 원래 처리의 재개 위치를 안전하게 보존한다.
3. 자동 카드 처리기와 수동 카드 처리기를 분리하되 카드 정의·선택 옵션·표시 기반은 재사용한다.
4. 플레이어와 적의 소유권·선택 주체를 분리해 양측 대칭 효과를 지원한다.
5. 숨은 정보는 효과가 허용한 비교 결과만 소유자에게 전달한다.
6. 기존 수동 카드·계약·런 흐름의 397개 회귀 기준을 깨지 않는다.

## 2. 현재 코드 기준선

### 2.1 재사용할 구조

| 현재 구조 | 재사용 내용 |
| --- | --- |
| `CardDefinition`, `CardDefinitionCatalog` | 안정 키, 숫자, 발동 종류와 효과 종류 |
| `BlackjackCard` | 물리 ID, 공개 여부, 정의 키와 사용 상태 |
| `BattleParticipant`, `BlackjackDeck`, `BlackjackHand` | 손패·드로우·버림과 카드 총수 보존 |
| `CardEffectChoiceOption` | 타입이 있는 선택 옵션 표현 |
| `CoreLoopBattle.Stepped` | 자동 효과 각 공개 단계의 화면 갱신 |
| `CombatantSide` | 효과 소유자·상대·선택 주체 구분 |
| `SoulPool` | 영혼 손실과 최대치 내 회복 |
| `RoundResolver`, `CompleteRound` | 숫자·카드·계약 버스트와 일반 승패 |
| `DemonContractResolver` | 스탠드 제한, 라운드 종료와 전투 종료 훅 |
| `CoreLoopSession`, `StageProgressionSession` | 입력 전달과 종료·지속 영혼 동기화 |
| `CoreLoopPresentation` | 숨은 값을 제외한 읽기 전용 표시 모델 |
| `EnemyObservation`과 정책 | 공개 정보 기반 적 선택 |

### 2.2 현재 부족한 경계

- `CardActivationKind.Automatic`은 분류만 있고 실행 경로가 없다.
- `CardEffectKind`에 자동 카드 5종이 없다.
- 시작 배분과 일반·강제·효과 드로우가 모두 `BattleParticipant.Draw` 또는 손패 추가를 직접 호출한다.
- 수동 카드 처리기는 효과가 카드를 추가한 직후 자체적으로 버스트를 계산한다.
- 자동 효과가 플레이어 입력을 기다렸다가 외부 히트·카드 효과를 재개할 구조가 없다.
- 라운드를 승패와 피해 없이 초기화하는 결과가 없다.
- 적이 합법적으로 획득한 숨은 카드 비교 지식을 보존·폐기할 구조가 없다.

AC-00은 문서만 작성하므로 코드·테스트·씬·패키지와 외부 에셋을 변경하지 않는다. DC-08의 전체 EditMode 397/397은 선행 증거이며 이번 단계에서 재실행한 결과가 아니다.

## 3. 목표 구조

```text
공개 카드 유입 요청
  └─ FaceUpCardEntryCoordinator
       ├─ 카드 추가·공개
       ├─ AutomaticCardResolver
       │    ├─ 즉시 완료
       │    └─ PendingAutomaticCardInteraction
       ├─ 원본 위치 처리
       ├─ 공개 합 버스트 검사
       └─ AutomaticResolutionContinuation 재개
             ├─ 플레이어/적 히트 완료
             ├─ 수동 카드 효과 완료
             ├─ 강제 드로우 효과 완료
             └─ 라운드 초기화로 취소
```

### 3.1 책임 분리

| 책임 | 소유 객체 |
| --- | --- |
| 자동 카드 정의·효과 등록 | `CardDefinitionCatalog`, `AutomaticCardResolver` |
| 공개 카드 진입과 발동 순서 | `FaceUpCardEntryCoordinator` |
| 카드별 규칙 | `IAutomaticCardEffectHandler` 구현체 |
| 선택 보류·오래된 입력 거절 | `PendingAutomaticCardInteraction` |
| 외부 처리 재개 | `AutomaticResolutionContinuation` |
| 라운드 승리 회복 예약·비교 지식 | `AutomaticCardBattleState` |
| 플레이어 입력 전달 | 두 세션과 Controller |
| 적 자동 선택 | `IAutomaticCardDecisionPolicy` |
| 안전한 화면 데이터 | Presenter와 ViewModel |

## 4. 카드 정의

### 4.1 효과 종류 확장

`CardEffectKind`에 다음 값을 명시적으로 추가한다.

```text
Poison
ResurrectionHerb
LieDetector
Flamethrower
PocketWatch
```

문자열 이름을 리플렉션으로 처리하지 않는다. 수동 카드와 자동 카드는 같은 효과 열거형을 사용하지만 실행기는 `Activation`을 기준으로 분리한다.

### 4.2 신규 정의

| 정의 키 | 표시명 | 숫자 | 발동 | 효과 |
| --- | --- | ---: | --- | --- |
| `poison-1` | 독극물 | 1 | Automatic | Poison |
| `resurrection-herb-2` | 부활초 | 2 | Automatic | ResurrectionHerb |
| `lie-detector-3` | 거짓말 탐지기 | 3 | Automatic | LieDetector |
| `flamethrower-4` | 화염 방사기 | 4 | Automatic | Flamethrower |
| `pocket-watch-5` | 회중시계 | 5 | Automatic | PocketWatch |

- 기존 숫자 생성자가 반환하는 기본 정의는 바꾸지 않는다.
- 같은 숫자의 여러 정의는 반드시 명시적 정의 키로 런 덱과 전투를 오간다.
- 알 수 없는 키는 기본 숫자 카드로 대체하지 않고 기존처럼 명시적으로 실패한다.
- 자동 카드는 `CardUseState`의 수동 사용 가능 상태로 초기화하지 않는다.
- 정의 키 변경과 함께 저장 콘텐츠 리비전을 `prototype-v2`로 올렸다. 명시적 마이그레이션은 추가하지 않으며 `prototype-v1`은 기존 `IncompatibleContentRevision` 경계에서 거절하고 새 런을 안내한다.

## 5. 공개 카드 유입 조정자

### 5.1 유입 원인

```text
FaceUpCardEntryCause
- PlayerHit
- EnemyHit
- CardEffect
- ForcedDraw
- GeneratedEffect
```

`InitialDeal`은 조정자를 호출하지 않거나 `TriggersAutomatic = false`로 명시한다. 숨은 카드 추가와 체인지 선택은 이 경계를 통과하지 않는다.

### 5.2 처리 프레임

```text
AutomaticResolutionFrame
- EntryId
- OwnerSide
- AddedCardId
- EntryCause
- Continuation
- PendingTriggerOrdinal
```

```text
AutomaticResolutionContinuation
- Kind
- ActingSide
- ParentSourceCardId?
- ParentEffectKind?
```

연속 처리 정보는 런타임 delegate나 Unity 콜백이 아니라 검증 가능한 불변 데이터로 보존한다. 지원 종류는 최소 다음과 같다.

```text
AfterPlayerHit
AfterEnemyHit
AfterManualCardEffect
AfterForcedDrawEffect
None
```

### 5.3 처리 알고리즘

1. 카드 ID의 현재 소유권과 공개 유입 여부를 검증한다.
2. 카드를 공개 손패에 정확히 한 번 추가한다.
3. 자동 정의가 아니면 공개 합 버스트를 검사하고 연속 처리를 재개한다.
4. 자동 정의이면 발동 프레임을 만들고 `AutomaticCardResolver`를 시작한다.
5. 선택이 필요하면 보류 상호작용을 저장하고 상태를 잠근다.
6. 완료되면 원본을 기본적으로 공개 손패에 유지한다. 부활초 소유자의 손패 재배분과 회중시계 선택 폐기처럼 카드 문구가 명시한 경우만 버린다.
7. 영혼 0·전투 종료·라운드 초기화 여부를 먼저 확인한다.
8. 계속 진행할 수 있으면 공개 합 버스트를 검사한다.
9. 버스트가 아니면 저장된 연속 처리를 정확히 한 번 소비한다.

한 단계에서 예외나 잘못된 입력이 발생하면 이미 검증된 카드 추가 이전 상태를 임의로 되돌리지 않는다. 시작 전 검증할 수 있는 항목은 모두 카드 이동 전에 검사하고, 승인된 발동 이후 내부 불변식 위반은 명시적인 예외로 처리한다.

### 5.4 기존 호출부 이관 대상

다음 공개 유입을 직접 `Draw`·`AddFaceUpCard`로 끝내지 않고 조정자에 연결한다.

- `CompletePlayerHit`
- 적 `Hit` 실행
- `CrystalOrbEffectHandler`의 선택 카드 획득
- `MilitaryKnifeEffectHandler`의 상대 강제 드로우
- 활성 사탄 계약 아랫면 행동의 상대 강제 드로우
- 향후 공개 카드를 추가하는 계약·카드 효과

`StartRound`, 체인지 선택 카드와 해머의 비공개 교체는 자동 발동에서 제외한다.

## 6. 자동 효과 실행 모델

### 6.1 처리기 인터페이스

```text
IAutomaticCardEffectHandler
- EffectKind
- Begin(AutomaticCardEffectContext)
- ResolveChoice(context, pending, selectedOption)
```

```text
AutomaticCardEffectStep
- PendingInteraction?
- Result?
- SourceDisposition
- RoundTransition?
```

```text
AutomaticSourceDisposition
- Discard
- RetainFaceUp
```

수동 `ICardEffectHandler`를 억지로 확장하지 않는다. 자동 처리기는 차례 소비가 없고, 선택 주체가 소유자 외 참가자가 될 수 있으며, 부활초처럼 바깥 효과를 취소할 수 있기 때문이다.

### 6.2 보류 상호작용

```text
PendingAutomaticCardInteraction
- InteractionId
- EntryId
- SourceCardId
- EffectKind
- OwnerSide
- DecisionSide
- Phase
- Prompt
- ChoiceKind
- Options
```

- `InteractionId`는 전투 안에서 증가하며 0으로 재사용하지 않는다.
- 플레이어 입력은 `InteractionId`와 `OptionId`를 함께 전달한다.
- 오래된 ID, 중복 선택, 다른 카드의 옵션과 선택 주체가 다른 입력은 무변경 거절한다.
- 적이 선택 주체이면 UI 입력을 열지 않고 적 자동 선택 정책으로 같은 옵션을 해결한다. `Stepped`가 자동 정책 실행 직전 상태를 전달하더라도 `PendingPlayerAutomaticInteraction`이 없는 프레임은 GameScene HUD와 CoreLoop IMGUI에서 숨긴다.
- 한 시점에는 자동 상호작용 하나만 존재한다.

### 6.3 전투 상태

`CoreLoopState`에 다음 상태를 추가한다.

```text
ResolvingAutomaticCardEffect
```

보류 상호작용의 `DecisionSide`로 플레이어 입력 대기와 적 자동 해결을 구분한다. 적 차례에 플레이어 선택이 생겨도 상태를 `EnemyTurn`으로 유지하지 않는다.

자동 상태에서는 다음을 거절한다.

- 히트와 스탠드
- 체인지 시작·선택
- 수동 카드 사용·선택
- 계약 시작·후속 선택
- 다른 자동 상호작용 입력

## 7. 전투 단위 자동 카드 상태

```text
AutomaticCardBattleState
- PendingInteractions
- PoisonWinRewards
- PlayerLieDetectorKnowledge?
- EnemyLieDetectorKnowledge?
- LastPublicResult?
- LastPlayerPrivateResult?
```

### 7.1 독극물 회복 예약

```text
PoisonWinReward
- SourceCardId
- OwnerSide
- RoundNumber
- HealAmount = 5
```

- 영혼 지불이 실제 완료됐을 때만 추가한다.
- 같은 물리 카드의 같은 발동에 중복 등록하지 않는다.
- `CompleteRound`의 피해 적용 뒤, 해당 소유자가 승리했고 아직 생존했다면 회복한다.
- 회복 뒤 예약을 제거한다.
- 패배·버스트·부활초 초기화·전투 종료에서도 예약을 제거한다.
- 여러 예약은 카드 ID 순서가 아니라 발동 순서로 적용하지만 최대 영혼 제한 때문에 최종 값은 최대치를 넘지 않는다.

### 7.2 거짓말 탐지기 지식

```text
HiddenCardComparisonKnowledge
- ObserverSide
- SubjectSide
- SubjectHiddenCardId
- DeclaredNumber
- IsAtLeastDeclaredNumber
- RoundNumber
```

- 실제 숫자는 저장하지 않는다.
- 플레이어 지식은 소유자 전용 표시 모델에서만 읽는다.
- 적 지식은 적 정책의 안전한 관측에 비교 조건으로만 전달한다.
- 대상 숨은 카드 ID 변경, 새 라운드, 전투 종료 시 폐기한다.

## 8. 카드별 처리 명세

### 8.1 독극물 처리기

선택 종류:

```text
PoisonDecision
- StandNow
- PaySoul
```

처리:

1. 소유자의 계약 제한을 포함한 실제 스탠드 가능 여부를 조회한다.
2. 가능할 때만 `StandNow`를 제공한다.
3. `PaySoul`은 `min(3, CurrentSoul)`을 잃는다.
4. 영혼 0이면 전투를 즉시 끝내고 예약과 연속 처리를 취소한다.
5. 생존하면 현재 라운드 독극물 승리 회복 예약을 추가한다.
6. 원본을 공개 손패에 유지하고 해당 숫자를 포함해 공개 합을 검사한다.

즉시 스탠드는 원래 행동의 추가 차례 소비가 아니다. 처리 완료 뒤 양측 스탠드 여부에 따라 기존 최종 승부 경계를 재사용한다.

### 8.2 부활초 처리기

선택 종류:

```text
ResurrectionHerbParticipantDecision
- Decline
- PaySoulAndRedeal
```

플레이어와 적은 각각 하나의 결정을 가진다. 한쪽의 `Decline`은 다른 쪽의 `PaySoulAndRedeal`을 막지 않는다. 소유자 결정을 먼저 받고 상대 결정을 이어서 받는다. 적은 자기 공개 합이 21 초과이거나 상대보다 3 이상 낮고 지불 뒤 생존할 때만 지불한다.

재배분 결과는 기존 `RoundResolution`에 가짜 승자를 넣지 않는다. 기존 전체 라운드 `RoundTransition`은 과거 구현이며 AC-RV03에서 참가자별 재배분 결과로 교체한다.

```text
ResurrectionHerbResult
- Cause: ResurrectionHerb
- HasWinner: false
- AppliesDamage: false
- PlayerRedealt: bool
- EnemyRedealt: bool
- BattleEndedBySoulLoss: bool
- CancelsContinuation: bool
```

처리 순서:

1. 원본 자동 상호작용을 닫는다.
2. 각 참가자의 결정을 독립적으로 검증한다.
3. `PaySoulAndRedeal`을 고른 참가자의 영혼만 1 감소시킨다.
4. 영혼이 0이면 즉시 전투를 끝내고 해당 참가자의 재배분을 중단한다.
5. 생존한 지불 참가자의 손패만 자기 버린 더미로 옮기고 스탠드를 해제한다.
6. 해당 참가자에게 비공개 카드 1장과 공개 카드 1장을 배분한다.
7. 실제 재배분이 발생했다면 그 참가자에게 귀속된 라운드 전용 자동 상태를 정리하고 바깥 수동 카드 효과의 재개 여부를 결과에 기록한다.
8. 소유자가 실제로 재배분됐다면 원본도 전체 손패와 함께 버린다. 소유자가 재배분되지 않았다면 원본을 공개 손패에 유지한다.

지불하지 않은 참가자의 영혼·기존 손패·스탠드 상태는 불변이어야 한다. 둘 다 거절하면 원본 부활초를 공개 손패에 유지하는 것 외 전투 상태를 바꾸지 않는다. 참가자별 재배분은 새 라운드를 시작하거나 라운드 번호를 증가시키지 않는다.

### 8.3 거짓말 탐지기 처리기

선택 종류는 기존 숫자 선언 옵션을 재사용하며 범위는 1~10이다.

1. 선언 직전에 상대 비공개 카드가 정확히 한 장인지 확인한다.
2. 정확히 한 장이면 `Rank >= DeclaredNumber`만 계산한다.
3. 선언 숫자는 공개 결과에, 비교 결과는 소유자 전용 결과에 기록한다.
4. 실제 숫자는 결과·공용 행동 기록·상대용 ViewModel에 기록하지 않는다.
5. 비공개 카드 불변식이 깨졌으면 `판정 불가` 결과로 완료한다.
6. 원본을 공개 손패에 유지한다.

### 8.4 화염 방사기 처리기

현행 목표 단계:

```text
CaptureBothCandidates
PlayerCommit
ResolveEnemyCommit
RevealAndApplyBoth
Complete
```

각 선택에는 `건너뛰기`와 현재 선택 주체의 발동 시점 공개 카드 ID를 제공한다. 원본 화염 방사기는 소유자 후보에서 제외한다.

- 발동 시점에 선택 주체가 스탠드했거나 후보가 없으면 그 주체만 자동 건너뛴다.
- 플레이어가 상대 결정을 보지 않고 먼저 확정한 뒤 적 결정을 계산·공개한다.
- 양쪽 카드 ID를 모두 검증한 뒤 같은 완료 경계에서 각 소유자의 버린 더미로 이동한다.
- 공개 카드이므로 결과에 선택 카드 ID와 표시명을 포함할 수 있다.
- 두 단계를 마친 뒤 원본을 공개 손패에 유지하고 해당 숫자를 포함해 공개 합을 검사한다.

### 8.5 회중시계 처리기

단계:

```text
ReactivateManualCard
ChooseSourceDisposition
Complete
```

재사용 후보는 다음 조건을 모두 만족한다.

- 소유자의 현재 공개 손패에 있다.
- `Activation == Manual`이다.
- `UseState == Used`다.
- 원본 회중시계가 아니다.

유효 대상이 없으면 첫 단계만 자동 건너뛰고 원본 회중시계의 `Discard` 또는 `RetainFaceUp` 선택은 반드시 연다. 선택 카드의 상태를 `Available`로 되돌리는 것은 카드 효과를 자동 실행하지 않는다.

## 9. 외부 효과와의 우선순위

| 상황 | 우선순위 |
| --- | --- |
| 자동 카드 공개와 중간 버스트 | 자동 효과·원본 위치 처리 후 버스트 |
| 독극물 즉시 스탠드와 계약 스탠드 금지 | 계약 제한 우선 |
| 자동 효과의 영혼 0과 남은 선택 | 즉시 전투 패배, 나머지 취소 |
| 부활초와 바깥 수동 카드 효과 | 부활초 초기화 우선, 바깥 효과 취소 |
| 자동 카드 버스트와 계약 버스트 방지 | 기존 계약 방지 훅 적용 |
| 여러 자동 카드 | 공개 영역 진입 순서대로 직렬 처리 |
| 자동 카드와 일반 악마 효과의 동시 대기 | 자동 카드 우선 |
| 버스트 방지·대체·즉시 종료 악마 효과 | 해당 판정을 바꾸는 데 필요한 시점에 선행 가능 |
| 라운드 승리와 독극물 회복 | 피해 적용·생존 확인 뒤 회복 |

기존 수동 처리기가 공개 카드를 추가하고 즉시 버스트를 만드는 부분은 조정자 결과를 기다리도록 이관한다. 자동 효과를 건너뛴 채 기존 버스트를 먼저 계산하면 안 된다.

## 10. 공개 API와 세션

### 10.1 전투 API

```text
PendingAutomaticCardInteraction PendingPlayerAutomaticInteraction
AutomaticCardPublicResult? LastAutomaticCardResult
AutomaticCardPrivateResult? PlayerAutomaticPrivateResult

bool TryResolvePlayerAutomaticCardChoice(
    int interactionId,
    int optionId)
```

- 조회 API는 상태를 변경하지 않는다.
- 플레이어가 선택 주체인 상호작용만 공개 입력으로 해결할 수 있다.
- 잘못된 ID와 옵션은 `false`를 반환하고 카드·영혼·상태를 바꾸지 않는다.

### 10.2 세션 전달

`CoreLoopSession`은 위 입력을 그대로 전투에 전달한다. `StageProgressionSession`은 `InBattle`일 때만 전달하고 승인된 입력 뒤 기존 종료 동기화를 한 번 호출한다.

부활초는 전투가 끝난 것이 아니므로 진행 세션의 영혼 동기화나 스테이지 결과 처리를 호출하지 않는다. 자동 효과로 영혼이 0이 되거나 전투가 끝난 경우에만 기존 동기화 경로를 사용한다.

## 11. 적 AI 명세

### 11.1 선택 정책 경계

```text
IAutomaticCardDecisionPolicy
- Decide(AutomaticCardDecisionObservation)
```

관측에는 다음만 포함한다.

- 효과 종류와 현재 단계
- 양측 공개 합·스탠드·영혼
- 선택 가능한 공개 카드와 사용 상태
- 적이 이미 합법적으로 얻은 탐지기 비교 지식
- 재현 가능한 결정 시드

플레이어 비공개 숫자와 실제 덱 순서는 포함하지 않는다.

### 11.2 최초 정책

| 카드 | 적 결정 기준 |
| --- | --- |
| 독극물 | 스탠드 가능하고 공개 합이 안전하면 스탠드, 아니면 생존 가능한 영혼 지불 |
| 부활초 | 적 자신의 공개 상태가 불리하고 영혼 1 지불 뒤 생존 가능하면 자기 손패 재배분, 상대 선택은 별도 정책 주체가 같은 기준으로 판단 |
| 거짓말 탐지기 | 공개 추정 분포 누적 확률이 50%에 가까운 경계값 선언 |
| 화염 방사기 | 공개 합 17 이상이면 가장 높은 유효 카드 폐기, 그 외 건너뛰기 |
| 회중시계 | 현재 사용 조건을 만족할 가능성이 높은 수동 카드 우선, 이후 버스트 위험이면 원본 폐기 |

정책이 잘못된 옵션을 반환하면 결정 검증기가 거절하고 안전한 첫 유효 옵션을 선택한다. 선택 이유는 디버그 가능한 코드로 기록하되 숨은 값을 포함하지 않는다.

## 12. 표시 명세

### 12.1 표시 모델

```text
AutomaticCardInteractionViewModel
- InteractionId
- SourceCardId
- SourceDisplayName
- Prompt
- Options

AutomaticCardResultViewModel
- SourceDisplayName
- OwnerLabel
- PublicSummary
- PrivateSummary?
```

- `PrivateSummary`는 플레이어가 효과 소유자인 경우에만 채운다.
- 적 거짓말 탐지기의 비교 결과는 플레이어 ViewModel에 포함하지 않는다.
- 자동 효과 보류 중에는 자동 효과 선택만 활성화한다.
- 카드 숫자·위치·영혼과 스탠드는 전투 스냅샷에서 읽고 View가 재계산하지 않는다.

### 12.2 GameScene 연결 원칙

- 기존 `CoreLoopController`, `CoreLoopPresentation`, `GameManager`의 세션 경계를 사용한다.
- 가능한 경우 기존 카드 선택 패널과 결과 영역을 재사용한다.
- 새 씬이나 프리팹을 만들지 않는다.
- `GameScene.unity` 직렬화 변경 없이 코드 기반 표시 연결을 우선한다.
- 1280×720과 1920×1080에서 선택지가 잘리지 않는지 확인한다.

## 13. 런·보상·적 프로필 연결

### 13.1 런 카드

- `RunCardDefinition`은 신규 정의 키를 그대로 보존한다.
- `StageBattleFactory`는 기존 카탈로그 조회로 자동 카드를 전투 카드로 만든다.
- 전투마다 자동 발동 보류·회복 예약·탐지기 지식은 새로 생성한다.
- 런 재시작은 획득 전 최초 덱과 자동 전투 상태를 복구한다.

### 13.2 보상 풀

- AC-01~AC-05에서는 테스트용 명시 카탈로그로만 신규 카드를 검증했다.
- AC-06에서 구현 완료된 5종을 기본 일반 보상 풀에 추가했다.
- 높은 등급 풀은 변경하지 않는다.
- 후보 표시 설명은 자동 발동 시점과 효과 뒤 위치를 포함한다.

### 13.3 적 프로필

- 검증된 거짓말 탐지기를 사기꾼 덱의 일반 카드 한 장과 교체했다.
- 다른 자동 카드는 카드 성향과 밸런스가 확정된 프로필에만 명시적으로 추가한다.
- 자동 카드 때문에 모든 적 덱을 일괄 교체하지 않는다.

## 14. 예상 파일

### 14.1 신규 런타임 후보

- `Assets/01. Scripts/CoreLoop/AutomaticCards/AutomaticCardResolver.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/AutomaticCardSelection.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/AutomaticCardResult.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/FaceUpCardEntryCoordinator.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/PoisonEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/ResurrectionHerbEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/LieDetectorEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/FlamethrowerEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCards/PocketWatchEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/EnemyAI/AutomaticCardDecisionPolicy.cs`

실제 구현에서는 기존 파일 패턴을 재사용하고, 책임이 겹치는 타입을 불필요하게 새 파일로 분리하지 않는다.

AC-02 실제 구현은 기존 CoreLoop 파일 배치를 따라 `AutomaticCardBattleState.cs`와
`PoisonEffectHandler.cs`를 `Assets/01. Scripts/CoreLoop` 바로 아래에 두었다.
독극물 예약은 전투 객체가 소유하고, `SoulPool.Restore`가 최대 영혼 상한을 보장한다.

AC-03 실제 구현도 같은 배치를 따라 `LieDetectorEffectHandler.cs`와
`LieDetectorResult.cs`를 CoreLoop 바로 아래에 두었다. `LieDetectorPublicResult`에는
선언 숫자와 판정 가능 여부만 두고, `HiddenCardComparisonKnowledge`의 실제 숨은 카드
ID는 어셈블리 내부 추적에만 사용한다. 플레이어 전용 접근자는 플레이어 소유 지식만,
`EnemyObservation.LieDetectorComparisonKnowledge`는 적 소유 지식만 제공하며 두
경계 모두 실제 카드 숫자를 저장하지 않는다.

AC-04 실제 구현은 `FlamethrowerEffectHandler.cs`와
`PocketWatchEffectHandler.cs`를 같은 CoreLoop 배치에 추가했다.
`AutomaticCardEffectContext`가 현재 공개 후보 조회, 선택 순간 폐기 재검증과 소유자
재활성화 명령을 제공한다. `BlackjackCard.TryReactivate`는 수동 `Used` 카드만
`Available`로 전환한다. 두 처리기는 기존 `PendingAutomaticCardInteraction`을
단계마다 새로 발급하므로 별도 가변 단계 객체 없이 선택 순서와 오래된 입력 거절을
보존한다.

### 14.2 주요 수정 후보

- `CardEffectKind.cs`, `CardDefinitionCatalog.cs`
- `CoreLoopState.cs`, `CoreLoopBattle.cs`, `BattleParticipant.cs`
- 공개 카드를 추가하는 기존 카드 처리기
- `CoreLoopSession.cs`, `StageProgressionSession.cs`
- `CoreLoopPresentation.cs`, 현재 View·Controller·GameManager
- `EnemyObservation`과 필요한 적 정책
- `BattleRewardCatalog.cs`, 사기꾼 프로필

### 14.3 테스트 후보

- `AutomaticCardFoundationTests.cs`
- `PoisonAutomaticCardTests.cs`
- `LieDetectorAutomaticCardTests.cs`
- `FlamethrowerAndPocketWatchTests.cs`
- `ResurrectionHerbAutomaticCardTests.cs`
- `AutomaticCardPresentationTests.cs`
- `AutomaticCardStageIntegrationTests.cs`
- `AutomaticCardSystemValidationTests.cs`

## 15. 테스트 명세

### 15.1 AC-01 공통 기반

| ID | 검증 |
| --- | --- |
| AC01-U01 | 최초 배분 자동 카드는 발동하지 않고 위치를 유지한다 |
| AC01-U02 | 플레이어·적 일반 히트의 자동 카드가 공개 즉시 보류된다 |
| AC01-U03 | 수정 구슬·강제 드로우 유입도 같은 경계를 사용한다 |
| AC01-U04 | 체인지·해머 비공개 카드는 발동하지 않는다 |
| AC01-U05 | 자동 선택 중 모든 일반 입력을 거절한다 |
| AC01-U06 | 오래된 상호작용·중복 옵션은 무변경 거절한다 |
| AC01-U07 | 완료 뒤 공개 합 버스트와 원래 행동이 정확히 한 번만 처리된다 |
| AC01-U08 | 카드 ID와 전체 카드 수가 유입·폐기 전후 보존된다 |

### 15.2 AC-02 독극물

| ID | 검증 |
| --- | --- |
| AC02-U01 | 스탠드 선택이 소유자만 즉시 스탠드시키고 원본을 공개 손패에 유지한다 |
| AC02-U02 | 스탠드 금지 계약에서는 해당 옵션이 없다 |
| AC02-U03 | 영혼 3 이상은 3, 미만은 전부 잃는다 |
| AC02-U04 | 영혼 0이면 남은 효과·적 행동 없이 즉시 패배한다 |
| AC02-U05 | 지불 뒤 라운드 승리만 5 회복하고 최대치를 넘지 않는다 |
| AC02-U06 | 패배·부활초 초기화에서는 회복하지 않는다 |
| AC02-U07 | 같은 라운드의 여러 예약이 중복 없이 해결된다 |

### 15.3 AC-03 거짓말 탐지기

| ID | 검증 |
| --- | --- |
| AC03-U01 | 1~10 선언만 승인한다 |
| AC03-U02 | 정확한 이상·미만 결과를 소유자에게만 제공한다 |
| AC03-U03 | 아직 뒤집히지 않은 실제 비공개 숫자가 공용 결과·상대 UI·관측에 새로 노출되지 않는다 |
| AC03-U04 | 적 사용 시 미공개 실제 숫자 없이 비교 지식만 정책에 전달되며, 이미 앞면 공개된 숫자는 기존 공개 정보로 유지된다 |
| AC03-U05 | 비공개 카드 교체와 새 라운드에서 지식이 폐기된다 |
| AC03-U06 | 비공개 카드가 정확히 한 장이 아니면 판정 불가로 끝난다 |

### 15.4 AC-04 화염 방사기·회중시계

| ID | 검증 |
| --- | --- |
| AC04-U01 | 플레이어가 상대 선택을 보지 않고 먼저 확정하며, 이후 상대 선택 공개 뒤 양쪽 결과를 동시에 적용한다 |
| AC04-U02 | 스탠드한 참가자와 후보 없는 참가자를 건너뛴다 |
| AC04-U03 | 원본 화염 방사기를 폐기 후보에서 제외한다 |
| AC04-U04 | 양측 선택 뒤 원본 화염 방사기를 유지하고 그 숫자를 포함해 공개 합과 버스트를 계산한다 |
| AC04-U05 | 회중시계가 사용 완료 수동 카드만 재활성화한다 |
| AC04-U06 | 자동 카드·자기 자신·사용 가능한 카드는 대상이 아니다 |
| AC04-U07 | 회중시계 유지·폐기가 합계와 카드 위치에 반영된다 |
| AC04-U08 | 유지한 회중시계가 같은 손에서 재발동하지 않는다 |

AC-04 실제 검증은 위 ID를 11개 NUnit 실행 케이스로 구성했다. 플레이어와 적 소유자
방향, 후보 자동 건너뛰기, 원본 제외, 선택 뒤 버린 더미, 원본 유지 상태의 공개 합,
수동 카드 재사용과 유지 카드 비재발동을 포함해 대상 11/11, CoreLoop 316/316,
전체 EditMode 445/445를 통과했다.

### 15.5 AC-05 부활초 — 과거 구현 회귀

아래 AC05 테스트는 양측 강제 라운드 재시작을 검증한 과거 기록이다. AC-RV03 완료 기준으로 재사용하지 않는다.

| ID | 검증 |
| --- | --- |
| AC05-U01 | 양측 영혼 2 이상에서만 재시작 옵션을 제공한다 |
| AC05-U02 | 활성화가 양측 영혼 1과 손패를 정확히 한 번 처리한다 |
| AC05-U03 | 일반 라운드 피해와 독극물 회복을 적용하지 않는다 |
| AC05-U04 | 계약 라운드 종료 훅을 한 번 호출한다 |
| AC05-U05 | 바깥 수동 효과·자동 대기열·차례 재개를 취소한다 |
| AC05-U06 | 새 라운드 최초 배분 자동 카드는 발동하지 않는다 |
| AC05-U07 | 거절은 영혼·라운드 번호·다른 카드 위치를 바꾸지 않는다 |

AC-05 실제 구현은 `ResurrectionHerbEffectHandler`가 거절을 항상 제공하고 양측 현재 영혼이
모두 2 이상일 때만 재시작을 제공한다. 재시작 완료 흐름은 일반 라운드 해결과 분리된
`RoundTransition`을 만들고 `HasWinner=false`, `AppliesDamage=false`,
`CancelsContinuation=true`를 보장한다. 원본 자동 상호작용을 닫은 뒤 양측 영혼 1,
독극물 예약·탐지기 지식·계약 라운드 훅, 부모 수동 효과 취소, 양측 손패 폐기,
기존 `StartRound` 한 번의 순서로 처리한다. 최근 실제 `RoundResolution`은 덮어쓰지 않는다.

수정 구슬이 선택한 부활초와 보위 나이프가 적에게 강제로 뽑힌 부활초까지 포함한 11개
NUnit 실행 케이스가 통과했다. AC-05 대상 11/11, CoreLoop 327/327, 전체 EditMode
456/456이며 Unity 컴파일 오류는 0이다.

### 15.7 AC-RV03 부활초 개정

| ID | 검증 |
| --- | --- |
| ACRV03-U01 | 양쪽에 서로 독립적인 거절·영혼 1 지불 선택을 제공한다 |
| ACRV03-U01A | 플레이어가 상대 결정을 보기 전에 먼저 확정하고, 상대 결정 공개 뒤 양쪽 결과를 동시에 적용한다 |
| ACRV03-U02 | 플레이어만 지불하면 플레이어 영혼·손패만 변경한다 |
| ACRV03-U03 | 적만 지불하면 적 영혼·손패만 변경한다 |
| ACRV03-U04 | 둘 다 지불하면 각자 영혼 1과 자기 손패를 정확히 한 번 처리한다 |
| ACRV03-U05 | 둘 다 거절하면 원본을 공개 손패에 유지하며 영혼·기존 손패·라운드는 불변이다 |
| ACRV03-U06 | 지불로 영혼 0이 된 참가자는 즉시 사망하고 재배분하지 않는다 |
| ACRV03-U07 | 생존한 지불 참가자는 비공개 1장과 공개 1장을 다시 받는다 |
| ACRV03-U08 | 라운드 번호·비지불자 스탠드·비지불자 자동 상태를 바꾸지 않는다 |
| ACRV03-U09 | 재배분은 일반 피해·21 보너스·독극물 승리 회복을 만들지 않는다 |
| ACRV03-I01 | 플레이어·적 AI 선택과 GameScene 표시가 같은 참가자별 결과를 사용한다 |

### 15.6 AC-06 통합

| ID | 검증 |
| --- | --- |
| AC06-I01 | 독립·런 세션이 플레이어 자동 선택을 전달한다 |
| AC06-I02 | 적 차례의 플레이어 대상 선택도 교착 없이 완료된다 |
| AC06-I03 | 적 AI가 5종을 유효 옵션만으로 결정한다 |
| AC06-I04 | 일반 보상으로 얻은 자동 카드가 다음 전투에서 발동한다 |
| AC06-I05 | 사기꾼 탐지기 사용이 숨은 정보 없이 동작한다 |
| AC06-I06 | 승리·패배·재시작 각 10회에서 상태와 카드 총수가 격리된다 |
| AC06-I07 | 두 씬·두 해상도·Console·전체 EditMode가 통과한다 |

AC-06 실제 구현은 `AutomaticCardDecisionPolicy`를 적 전투 정책과 분리하고,
`AutomaticCardDecisionObservationFactory`가 공개 합·영혼·공개 옵션·합법적인 비교
지식만 투영하도록 했다. 기본 정책은 5종의 현재 유효 옵션 중 하나만 반환하며 잘못된
옵션이나 정책 예외는 첫 유효 옵션으로 안전하게 대체한다. 플레이어 선택은
`CoreLoopSession`과 `StageProgressionSession`에서 같은 전투 API로 전달하고,
Presenter는 보류 선택·공개 결과·소유자 전용 요약을 구분한다. View·Controller와
`GameManager`는 씬 직렬화 없이 코드 기반 선택·결과 패널을 사용한다.

일반 보상 풀은 기존 10종에 자동 카드 5종을 추가해 15종이 되었고 높은 등급 6종은
유지했다. 사기꾼 덱은 10장을 유지하면서 일반 카드 한 장을 거짓말 탐지기로 교체했다.
신규 CoreLoop 12건과 StageProgression 3건, 합계 15/15가 통과했다. 테스트 메서드
인벤토리는 CoreLoop 339건·StageProgression 132건·합계 471건이다. Unity가 없어도
실행 가능한 461건은 461/461 통과했으며, UnityEngine 또는 NUnit Editor
`TestContext`가 필요한 10건은 독립 실행기에서 실행하지 못했다. Unity 편집기의
런타임·CoreLoop 테스트 어셈블리 컴파일과 StageProgression의 Unity 응답 파일 기반
Roslyn 컴파일은 성공했다. Unity MCP 연결과 임시 Batchmode 라이선스 IPC가
사용 불가하여 I07의 Editor 전체 실행·두 씬·두 해상도·Console은 후속 검증으로 남긴다.

### 15.7 AC-RV01 숫자·정의 키 이관

| ID | 검증 |
| --- | --- |
| ACRV01-U01 | 자동 카드 5종이 숫자 1~5와 새 정의 키를 사용한다 |
| ACRV01-U02 | 과거 자동 카드 정의 키 3종을 카탈로그가 거절한다 |
| ACRV01-U03 | 숫자 1~5의 기본 카드 정의가 자동 카드로 대체되지 않는다 |
| ACRV01-I01 | 일반 보상 풀이 새 키만 제공한다 |
| ACRV01-I02 | 사기꾼 덱의 거짓말 탐지기 참조가 유지된다 |
| ACRV01-I03 | 런 카드가 실제 전투에서 새 숫자로 변환된다 |
| ACRV01-I04 | `prototype-v1` 저장은 키 추측 전에 콘텐츠 불일치로 거절된다 |

`CardDefinitionCatalog`의 독극물·화염 방사기·회중시계 키와 숫자를 각각
`poison-1`/1, `flamethrower-4`/4, `pocket-watch-5`/5로 바꿨다. 부활초와
거짓말 탐지기는 기존 키·숫자를 유지한다. 보상과 적 프로필은 카탈로그 상수를 참조하므로
별도 중복 문자열을 만들지 않았고, `RunCardDefinition → StageBattleFactory → BlackjackCard`
변환을 통합 테스트로 고정했다. 같은 숫자의 기본 정의는 카탈로그 등록 순서를 유지해
`GetDefaultForRank(1..5)` 결과가 바뀌지 않는다.

저장 스키마는 1을 유지하고 콘텐츠 리비전만 `prototype-v2`로 올렸다. 과거 키를 현재
키로 추측하는 마이그레이션은 카드 정체성 오판 위험 때문에 추가하지 않았다. 전용 테스트는
11/11, 전체 EditMode는 522/522 통과했다. `GameScene`과 `CoreLoopTest`를 재생해
전투 화면이 정상 표시됨을 확인했고 Console Error/Warning은 0건이었다. 런타임 UI·씬·
프리팹·Packages·외부 에셋은 변경하지 않았다.

## 16. 불변식

1. 물리 카드 ID는 전투의 모든 일반 카드 위치에서 유일하다.
2. 자동 발동 하나는 같은 공개 유입에 대해 최대 한 번 시작한다.
3. 한 시점의 보류 자동 상호작용은 최대 하나다.
4. 보류 상호작용의 선택 주체만 입력할 수 있다.
5. 자동 효과가 끝나기 전에는 공개 합 버스트와 바깥 연속 처리를 실행하지 않는다.
6. 바깥 연속 처리는 완료·버스트·초기화 중 정확히 하나의 경로로 소비된다.
7. 적 관측과 공용 결과에는 아직 뒤집히지 않은 플레이어 비공개 숫자가 없다. 앞면 공개된 숫자는 공개 사실에서 얻은 값으로만 전달한다.
8. 라운드·전투 종료 후 독극물 예약, 탐지기 지식과 보류 상호작용이 남지 않는다.
9. 부활초 재배분은 승패와 일반 피해를 만들지 않고 지불한 참가자의 손패만 변경한다.
10. 자동 카드 구현은 골드·상점과 정식 런 순서를 변경하지 않는다.

## 17. 외부 의존성과 에셋

- 새 패키지와 오픈소스 의존성을 추가하지 않는다.
- 기존 Unity 6000.3.10f1과 현재 테스트 어셈블리를 사용한다.
- 신규 이미지·사운드·폰트와 외부 에셋을 추가하지 않는다.
- UI는 현재 코드 기반 카드·선택 표시를 재사용한다.
- 외부 자료가 추가되면 이름, 버전, URL, 라이선스와 사용 위치를 AI 활용 기술 문서에 기록한다.

## 18. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-30 | 이천서 | 부활초를 참가자별 `Decline`/`PaySoulAndRedeal` 선택으로 재명세했다. 지불한 참가자만 영혼 1을 잃고 자기 손패를 재배분하며, 지불로 영혼 0이면 즉시 사망하고 새 라운드를 시작하지 않는 AC-RV03 테스트 기준을 추가했다. |
| 2026-07-30 | 이천서 | AC-RV03 처리기를 소유자·상대 2단계 선택으로 교체하고 지불자 전용 재배분·영혼 0 종료·부모 효과 취소를 구현했다. ACRV03 11/11, 독극물 주입 회귀, DC08_I02·GSH01_U06와 전체 EditMode 792/792를 검증했다. |
| 2026-07-30 | 이천서 | 자동 카드 5종의 정식 획득 데이터 원천을 RF-02 상점 생성기로 재명세하고, 구매 후 기존 런 카드 ID·전투 변환을 재사용하도록 경계를 추가했다. RW 등록과 과거 테스트는 제거하지 않으며 코드 이관은 미완료다. |
| 2026-07-29 | 이천서 | AC-RV02 집행자 독극물 주입을 `BeginRound` 최초 배분 전 가용 덱 초기화·추가·혼합과 전투 종료 임시 카드 제거로 구현하고 CoreLoop 466/466·전체 655/655 검증 반영 |
| 2026-07-29 | 이천서 | AC-RV02 집행자 독극물의 배분 전 혼합·같은 라운드 드로우·전투 누적·종료 제거와 결정적 셔플 요구를 확정 |
| 2026-07-29 | 이천서 | Notion v0.7 적 덱 수량과 집행자의 전투 한정 독극물 주입을 AC-RV02 도메인 요구로 추가하고 기존 자동 효과·정보 경계는 유지; 코드·테스트 미변경 |
| 2026-07-29 | 이천서 | 비공개 역할·앞뒷면 분리와 앞면 공개 숫자/미공개 숫자의 적 관측·거짓말 탐지기 정보 경계를 현행 규칙에 맞게 개정 |
| 2026-07-28 | 이천서 | AC-RV01 새 키·숫자, 기본 숫자 정의 보존, 보상·런/전투 변환, `prototype-v2`와 과거 저장 거절 정책을 구현하고 전용 11/11·전체 522/522·두 전투 화면·Console 0 검증 반영 |
| 2026-07-25 | 이천서 | AC-06 공개 정보 전용 적 자동 정책·독립/런 입력·안전 표시·일반 보상 5종·사기꾼 탐지기 통합과 대상 15/15·Unity 비의존 461/461·컴파일 성공 반영, Editor 전용 10건과 화면 검증은 환경 복구 뒤 재실행으로 기록 |
| 2026-07-27 | 이천서 | 노션 v0.6의 자동 카드 숫자 1~5와 목표 정의 키, 보상·적 프로필·저장 콘텐츠 리비전의 원자적 AC-RV01 이관 명세 추가 |
| 2026-07-25 | 이천서 | AC-05 부활초 처리기·전용 `RoundTransition`·양측 영혼/상태 정리·부모 효과 취소 경계와 대상 11/11·CoreLoop 327/327·전체 456/456 검증 반영 |
| 2026-07-25 | 이천서 | AC-03 거짓말 탐지기 선언·공개/소유자 전용 결과·적 비교 관측·체인지/해머/라운드/전투 종료 지식 폐기와 대상 10/10·CoreLoop 305/305·전체 434/434 검증 반영 |
| 2026-07-25 | 이천서 | AC-02 독극물 처리기·전투 단위 회복 예약·영혼 회복 API·연속 처리 취소 경계와 대상 12/12·CoreLoop 295/295·전체 424/424 검증 반영 |
| 2026-07-25 | 이천서 | AC-01 Resolver·선택 모델·공개 유입·열거형 연속 처리 기반과 대상 15/15·CoreLoop 283/283·전체 412/412 검증 반영 |
| 2026-07-23 | 이천서 | 자동 공개 유입 조정자, 보류 상호작용·연속 처리, 5종 처리기, 정보 은닉·AI·UI·런 연결과 AC-01~AC-06 테스트 명세 수립 |
| 2026-08-02 | 이천서 | AC-RV04에서 `AutomaticCardResult`에 플레이어·상대 확정 결과와 화염 방사기 대상 카드 ID를 추가했다. 결과 객체는 양측 결정 완료 뒤에만 생성하며 GameScene은 그 공개 결과만 표시한다. 아자젤 중복 버스트보다 공개 유입 자동 카드를 먼저 해결하는 연속 처리 회귀를 추가했다. |
