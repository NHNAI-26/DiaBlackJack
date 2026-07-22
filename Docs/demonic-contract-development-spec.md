# 악마 계약 시스템 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·개발 책임자: 이천서  
> 작업 식별자: DC-00~DC-08
> 버전: v1.0
> 상태: DC-08 광신도 계약 정책 밸런스·실전 검증 완료
> 최종 갱신: 2026-07-23

## 1. 기술 목표

완료된 `CoreLoopBattle`의 차례·라운드·영혼·카드 효과와 `StageProgressionSession`의 런 전투 동기화를 유지하면서 악마 계약을 별도 도메인으로 추가한다. 계약은 일반 카드와 일부 실행 경계를 공유하지만 카드 정의나 `CardEffectResolver`에 억지로 합치지 않는다.

첫 구현 원칙은 다음과 같다.

- 순수 C# 규칙 계층을 먼저 구현한다.
- 상태 변경 전 모든 시작 조건을 검사한다.
- 계약 후보와 효과 선택은 타입이 있는 불변 모델로 전달한다.
- Unity 화면은 읽기 전용 표시 모델만 사용한다.
- 난수, 셔플과 주사위는 테스트에서 결정적으로 주입할 수 있어야 한다.
- 명시적으로 필요한 훅만 추가하고 범용 이벤트·스크립팅 시스템은 만들지 않는다.

## 2. 현재 코드 기준선

DC-00 문서 작성 시점의 저장소 기준은 다음과 같다.

- `CoreLoopBattle`이 플레이어·적, 상태, 차례, 라운드, 영혼, 카드 효과와 적 정책을 소유한다.
- `CoreLoopState`는 일반 행동, 체인지 선택, 카드 효과 선택, 적 차례와 전투 종료를 구분한다.
- `CardEffectResolver`와 카드별 처리기가 수동 카드의 선택·효과·종료를 처리한다.
- 행동 중 숫자 버스트는 `VisibleHandValue`, 양쪽 스탠드 뒤 최종 승부는 전체 합을 사용한다.
- `CoreLoopSession`과 `StageProgressionSession`이 화면 입력을 전투에 전달한다.
- `PlayerRunState`는 일반 카드 런 덱과 영혼을 보존하고 `StageBattleFactory`가 실제 전투 덱을 만든다.
- CoreLoop·StageProgression Presenter/View/Controller와 GameScene 표시 계층이 전투 상태를 읽는다.
- 적 AI는 공개 관측에서 유효 행동 후보를 만들고 실행 직전 다시 검증한다.
- 최근 검증 기준선은 CU-M03 전체 EditMode 310/310 통과 기록이다.

DC-00은 문서 전용 작업이다. 코드, 테스트, 씬, 프리팹, 패키지와 외부 에셋을 변경하지 않았고 Unity 테스트를 다시 실행하지 않았다.

## 3. 책임 경계

```text
PlayerRunState
  └─ RunDemonDefinition 목록(런 동안 영구 소유)
       ↓ StageBattleFactory 변환
CoreLoopBattle
  ├─ DemonContractDeck(전투용 드로우·버림)
  ├─ DemonContractResolver(명시적 계약 훅)
  ├─ PendingDemonContractInteraction(후보·후속 선택)
  └─ ActiveDemonContract(전투 동안 활성)
       ↓ 읽기 전용 스냅샷
Presentation → View → Controller
       ↓ 입력 전달
CoreLoopSession / StageProgressionSession
```

### 3.1 CoreLoop 책임

- 계약 가능 여부와 불가 사유
- 영혼 비용, 사용 횟수와 차례 잠금
- 전투용 악마 덱·버림·활성 위치
- 후보 생성, 선택, 즉시 효과와 지속 훅
- 계약 효과에 따른 카드 이동·버스트·영혼·차례 처리
- 전투 종료 시 계약 상태 제거

### 3.2 StageProgression 책임

- 런 악마 카드의 정의 키와 물리 ID 보존
- 시작·획득 악마 덱의 유효성 검사
- 런 덱에서 매 전투 독립적인 악마 전투 덱 생성
- 계약 입력 전달 뒤 전투 종료·영혼 동기화
- 재시작 시 최초 악마 덱 복구

### 3.3 표시 계층 책임

- 현재 영혼, 비용, 사용 횟수와 불가 사유 표시
- 후보 3장과 활성 계약·가변 수치 표시
- 계약 선택·후속 선택 입력 전달
- 상대 비공개 값과 덱 순서를 포함하지 않는 안전한 결과 표시

표시 계층은 계약 가능 여부, 주사위 판정, 카운터와 카드 합을 직접 계산하지 않는다.

## 4. 데이터 모델

### 4.1 계약 정의

권장 순수 모델은 다음 정보를 가진다.

```text
DemonContractDefinition
- Key: string
- DisplayName: string
- Kind: DemonContractKind
- BaseSoulCost: int
- Summary: string
- CostSummary: string
```

1차 카탈로그 키는 안정된 영문 소문자 케밥 표기를 사용한다.

| 악마 | 정의 키 |
| --- | --- |
| 사탄 | `satan` |
| 벨페고르 | `belphegor` |
| 마몬 | `mammon` |
| 레비아탄 | `leviathan` |

표시 문구와 효과 실행을 문자열로 해석하지 않는다. `Kind`는 처리기를 찾는 안정된 타입 키이며 `Key`는 런 덱·보상·저장 경계에서 사용한다.

### 4.2 런 악마 카드

```text
RunDemonDefinition
- Id: int
- DefinitionKey: string
```

- ID는 런 안에서 고유하고 재시작 전까지 재사용하지 않는다.
- 정의 키는 `DemonContractCatalog`에 존재해야 한다.
- 최초 덱과 현재 덱을 분리해 재시작 시 네 장을 복구한다.
- 일반 카드 ID와 악마 카드 ID는 다른 도메인이므로 값이 겹쳐도 된다.

### 4.3 전투 악마 카드와 덱

```text
DemonContractCard
- Id: int
- Definition: DemonContractDefinition

DemonContractDeck
- DrawPile
- DiscardPile
- DrawOffer(count)
- RefillFromDiscardExcludingActive()
```

후보 생성 뒤 카드 소유 위치는 드로우, 후보, 버림, 활성 중 정확히 하나여야 한다. 후보를 전투 손패나 일반 카드 덱에 넣지 않는다.

DC-01 구현에서는 `TakeCandidates()`가 정확히 3장을 전투 후보 위치로 이동한다. 드로우 더미가 비면 버린 더미만 독립 난수로 섞어 보충하고, 이미 후보 또는 활성 위치에 있는 물리 ID는 보충 대상에서 제외한다. `AvailableCardCount`와 `CardsInPlayCount`의 합은 항상 최초 `TotalCardCount`와 같아야 한다.

`CoreLoopBattle.PlayerDemonDeck`은 독립 전투에서도 비어 있는 전용 덱 인스턴스를 가지며, `StageBattleFactory` 경로에서는 런 악마 카드의 ID와 정의 키를 보존한 새 전투 덱을 받는다. 악마 덱 시드는 플레이어 일반 덱 시드에서 별도 상수로 파생하고 각 덱이 자체 난수 상태를 가지므로 악마 후보 조회가 일반 카드 순서를 바꾸지 않는다.

### 4.4 보류 상호작용

하나의 타입으로 후보 선택과 악마별 후속 선택을 표현하되 문자열 명령은 사용하지 않는다.

```text
PendingDemonContractInteraction
- InteractionId: int
- Kind: DemonContractInteractionKind
- ContractKind: DemonContractKind?
- Options: IReadOnlyList<DemonContractOption>
- PublicPrompt: string

DemonContractOption
- OptionId: int
- ContractCardId: int?
- NumericValue: int?
- PublicLabel: string
```

`DemonContractInteractionKind`의 1차 값은 다음으로 제한한다.

- `ChooseContract`
- `BelphegorTopCard`
- `MammonReroll`
- `MammonApplyDie`

상대 비공개 카드 숫자와 실제 덱 다음 카드는 공개 옵션에 넣지 않는다. 벨페고르의 덱 위 카드는 소유자인 플레이어 전용 화면 모델에서만 숫자를 볼 수 있다.

### 4.5 활성 계약

```text
ActiveDemonContract
- SourceCardId
- Definition
- OwnerSide
- RuntimeState
```

`RuntimeState`는 악마마다 필요한 최소 값만 가진다.

- 벨페고르: 자동 스탠드 예약 여부
- 마몬: 현재 주사위 값, 이번 차례 재굴림 여부
- 레비아탄: 별도 가변 값 없음
- 사탄: 남은 정상 차례 6→0, 권능 카드 ID, 활성 여부

여러 악마의 모든 필드를 하나의 거대한 상태 클래스에 넣지 않는다. 추가 계약에서는 같은 악마를 다시 선택할 수 있으므로 활성 계약은 정의 종류가 아닌 물리 계약 인스턴스 ID로 구분하는 컬렉션으로 관리한다. 같은 종류의 계약도 각자 카운터·주사위·원본 카드 ID와 대가 상태를 가진다.

### 4.6 임시 카드 출처와 소유권

계약이 생성·이동·추방하는 모든 카드에는 원래 소유자, 현재 전투 소유자와 생성 원인 계약 인스턴스를 추적할 수 있는 경계가 필요하다.

- 사탄의 권능·오망성: 전투 생성 카드, 숫자 합계 포함, 전투 종료 시 모든 위치에서 제거
- 파이몬 추방: 원본 카드 소유권 유지, 현재 전투 동안 추방, 전투 종료 시 전투 시작 덱 구성 복구, 플레이어 런 덱 무변경
- 벨리알 탈취: 현재 전투 소유권을 계약 소유자로 변경, 라운드 종료 시 현재 소유자 버림 더미, 전투 종료 시 원래 소유권 복구

일반 카드의 `DefinitionKey`만으로 임시 카드를 구분하지 않는다. 같은 정의 카드가 원래 덱에도 존재할 수 있으므로 물리 ID와 출처로 정리 대상을 찾는다.

## 5. 전투 상태와 공개 API

### 5.1 상태

`CoreLoopState`에 `PlayerResolvingDemonContract`를 추가하는 방식을 권장한다. 후보 선택, 벨페고르 미리보기와 마몬 선택은 `PendingDemonContractInteraction.Kind`로 구분한다.

이 상태에서는 다음 입력을 모두 거절한다.

- 히트, 스탠드, 체인지
- 일반 카드 사용과 카드 효과 선택
- 새 계약 시작
- 적 차례 실행

잘못된 옵션이나 오래된 `InteractionId`는 상태를 바꾸지 않고 `false`를 반환한다.

### 5.2 CoreLoopBattle 권장 API

```text
DemonContractAvailability PlayerDemonContractAvailability
PendingDemonContractInteraction PendingPlayerDemonContractInteraction
IReadOnlyList<ActiveDemonContract> ActivePlayerDemonContracts
DemonContractResult? LastDemonContractResult

bool TryBeginPlayerDemonContract()
bool TryResolvePlayerDemonContract(int interactionId, int optionId)
```

`DemonContractAvailability`는 최소한 다음을 제공한다.

- `CanBegin`
- `FailureReason`
- `SoulCost`
- `SoulAfterCost`
- `RemainingBaseUses`

조회 속성은 덱을 섞거나 후보를 뽑지 않으며 상태를 변경하지 않는다.

### 5.3 세션 API

`CoreLoopSession`과 `StageProgressionSession`은 동일한 두 입력을 얇게 전달한다.

```text
TryBeginPlayerDemonContract()
TryResolvePlayerDemonContract(interactionId, optionId)
```

`StageProgressionSession`은 승인된 계약 입력 뒤 기존 종료 동기화 경계를 호출한다. 마몬 6, 레비아탄 대가나 사탄 대가가 전투를 끝냈을 때 영혼·승패·보상이 한 번만 반영되어야 한다.

## 6. 계약 시작 트랜잭션

`TryBeginPlayerDemonContract()`는 다음 순서를 보장한다.

1. 플레이어 정상 차례, 계약 미사용, 보류 효과 없음, 영혼과 후보 수를 검사한다.
2. 정확히 3장을 제시할 수 있는지 덱 구조를 상태 변경 없이 확인한다.
3. 영혼 1을 적용한다.
4. 전투 기본 계약 사용 횟수를 1 증가시킨다.
5. 후보 3장을 덱에서 후보 임시 영역으로 이동한다.
6. 새 `InteractionId`를 발급하고 상태를 `PlayerResolvingDemonContract`로 바꾼다.

1~2가 실패하면 아무 것도 바꾸지 않는다. 3 이후의 내부 불변식 실패는 조용히 `false`로 삼키지 않고 개발 오류로 처리한다. UI 취소는 3 이전 확인 화면에서만 가능하므로 전투 API에는 비용 지불 후 취소 함수를 만들지 않는다.

## 7. 계약 선택 트랜잭션

`ChooseContract` 해결 순서는 다음과 같다.

1. 상태, `InteractionId`, 옵션과 후보 카드 소유권을 검증한다.
2. 선택 카드를 활성 영역으로, 나머지 둘을 악마 버린 더미로 이동한다.
3. 활성 계약을 등록한다.
4. 계약 즉시 효과를 실행한다.
5. 직접 버스트, 공개 합 숫자 버스트, 영혼 고갈과 전투 종료를 확인한다.
6. 전투가 계속되고 추가 선택이 없으면 최근 결과를 기록하고 적 차례를 실행한다.
7. 추가 선택이 있으면 같은 상태에서 새 `InteractionId`와 옵션으로 교체한다.

선택 승인 뒤 즉시 효과가 플레이어를 버스트시키거나 개별 대가가 영혼을 0으로 만들어도 계약 비용과 선택은 되돌리지 않는다. 영혼 0은 버스트와 구분되는 즉시 전투 패배이며 이후 효과·차례·AI 실행을 중단한다.

DC-02 구현에서는 `DemonContractAvailability` 조회가 영혼·덱·난수를 소비하지 않고 실패 사유와 계약 후 영혼을 제공한다. `PendingDemonContractInteraction`은 증가형 `InteractionId`와 물리 카드 ID가 있는 세 옵션을 보존하며, 잘못되거나 중복된 입력은 보류 객체와 카드 위치를 바꾸지 않는다. 선택 승인 뒤 `ActiveDemonContract` 하나를 등록하고 나머지 두 카드를 버린 더미로 이동한 다음 적 차례를 정확히 한 번 실행한다. `DemonContractResolver`의 기본 구성에는 미확정 악마 처리기를 등록하지 않으며, 주입식 처리 경계로 개별 대가 영혼 고갈 시 버스트 판정 없이 전투 패배·진행 세션 동기화가 이루어지는지만 검증했다.

## 8. 효과 처리 구조

### 8.1 처리기 경계

```text
IDemonContractHandler
- Kind
- Activate(context)
- OnPlayerTurnStarted(context)
- BeforePlayerHit(context)
- BeforeFinalResolution(context)
- AfterCardEffect(context, result)
- OnTurnCompleted(context)
```

모든 처리기가 모든 훅을 구현하도록 강제하지 않고 기본 무동작 기반 클래스 또는 작은 선택 인터페이스를 사용할 수 있다. 단, 리플렉션이나 문자열 이벤트 이름으로 훅을 찾지 않는다.

### 8.2 기존 카드 효과와의 연결

- 레비아탄은 리볼버 처리기를 복제하지 않는다.
- `CardEffectResult`가 정상 완료된 뒤 활성 계약 처리기에 결과를 전달한다.
- 레비아탄이 추가 버스트를 만들면 같은 카드 행동 안에서 라운드를 끝내고 적 차례를 실행하지 않는다.
- 대가의 영혼 손실도 같은 결과 흐름에서 처리한다.
- 계약 결과는 비공개 숫자나 전체 합을 저장하지 않는다.

### 8.3 버스트 경계

- 일반 히트·강제 히트·효과 중 숫자 판정: `VisibleHandValue`
- 양쪽 스탠드 최종 비교: 전체 손패 합
- 레비아탄: 최종 승부 전 공개 합만 조회
- 마몬 주사위 6: `ContractEffectBust`
- 계약 대가 영혼 고갈: 버스트가 아니라 영혼 고갈에 의한 전투 패배
- 악마 문구의 버스트: 숫자·카드·악마·계약 효과와 특수 패배 조건을 모두 포함

`RoundEndCause`에는 필요하면 `ContractEffectBust`를 추가한다. 기존 `CardEffectBust`로 뭉개지 않아야 피해 원인, UI와 플레이 테스트 기록을 구분할 수 있다.

## 9. 악마별 기술 명세

### 9.1 벨페고르

- `TryPlayerHit`가 실제 드로우 전에 활성 벨페고르를 확인한다.
- 덱 위 한 장을 임시 미리보기 영역으로 이동하지 않고 안전하게 조회하거나, 조회 중 소유권을 명확히 보존한다.
- `그대로 히트`는 해당 카드가 실제로 같은 카드인지 확인한 뒤 공개 드로우한다.
- `덱 아래로 이동`은 같은 카드를 아래로 보낸 뒤 차례를 종료한다.
- 상대 스탠드가 관측된 다음 플레이어 정상 차례에 자동 스탠드 예약을 설정한다.
- 예약된 차례의 행동이 끝나면 전투가 계속되는 경우에만 플레이어를 스탠드 처리한다.

덱 미리보기 값은 적 AI 관측과 공용 전투 로그에 포함하지 않는다.

DC-03 구현에서는 기본 `DemonContractResolver`에 벨페고르만 등록한다. `IDemonContractPlayerHitPreviewHandler`와 `IDemonContractOwnerTurnHandler`가 히트 전 보류, 정상 차례 시작, 행동 완료와 라운드 종료 훅을 타입으로 연결한다. `BlackjackDeck.TryPeekTop`과 `TryMoveTopToBottom`은 같은 물리 ID를 재검증하며 카드 총수·가용 소유권을 바꾸지 않는다.

`PendingDemonContractInteraction`의 `BelphegorTopCard` 두 공개 옵션에는 카드 ID와 숫자가 없다. 실제 카드 ID·정의 키·숫자는 `PlayerDemonContractPreview`에만 저장하고 `EnemyObservation`·`PublicActionHistory`에는 전달하지 않는다. 그대로 히트는 기존 공개 드로우와 `VisibleHandValue` 버스트 경로를 재사용하며, 덱 아래 이동은 드로우 없이 행동만 종료한다. 상대 스탠드로 예약된 자동 스탠드는 다음 행동 전체가 끝난 뒤 한 번 소비하고 라운드 종료 시 초기화한다.

### 9.2 마몬

- 주사위는 `IDemonRandom` 또는 동일 목적의 주입 가능한 경계에서 1~6을 생성한다.
- 계약 즉시와 재굴림 직후 6이면 다른 선택 없이 플레이어를 계약 효과 버스트시킨다.
- 정상 차례 시작마다 재굴림 선택을 한 번만 만든다.
- 최종 해결 직전에 값 적용 선택이 남으면 라운드 해결을 보류한다.
- 적용한 값은 그 라운드의 최종 합 계산에만 전달하며 카드 객체를 만들지 않는다.
- 선택 완료 후 기존 `RoundResolver`에 보정 합 또는 명시적 합계 수정 인자를 전달한다.

비공개 카드가 포함되는 최종 합은 UI에 숫자로 노출하지 않는다.

DC-04 구현에서는 `IDemonDieRoller`를 마몬 처리기에 주입하고, 기본 전투는 전투별 결정적 난수원을 새로 생성한다. `MammonReroll`과 `MammonApplyDie` 상호작용은 물리 계약 ID를 함께 검증하며, 차례 선택은 행동 전 상태로 복귀하고 최종 선택은 `RoundResolver`의 플레이어 보정 합 경계로 직접 이어진다. 보정 합은 에이스를 포함한 전체 손패와 함께 다시 계산한다.

### 9.3 레비아탄

- `CardEffectKind.AutoPistol` 완료 결과만 감시한다.
- 기존 리볼버 성공으로 이미 라운드가 끝났다면 추가 판정과 대가를 실행하지 않는다.
- 실패 뒤 상대 공개 카드 합이 22 이상이면 `ContractEffectBust`로 종료한다.
- 그렇지 않으면 소유자 영혼 1을 차감하고 고갈 여부를 즉시 확인한다.
- 결과 모델에는 `Triggered`, `BustedTarget`, `PaidSoulCost`만 남기고 숨은 합은 넣지 않는다.

DC-04 구현의 `IDemonContractAfterCardEffectHandler`는 완료된 `CardEffectResult`만 받는다. 원 리볼버의 `RoundResolution`이 있으면 계약 훅보다 먼저 종료하므로 판정을 복제하지 않는다. 실패 시에만 레비아탄 처리기가 공개 합을 확인하고 안전한 `DemonContractEffectResult`와 선택적 계약 버스트 결과를 반환한다. DC-07 회귀에서는 이 판정이 비공개 숫자를 중간 합에 넣지 않도록 고정했다.

### 9.4 사탄

사탄은 DC-D05와 DC-D06의 남은 양면·뒤집기 상태를 확정하기 전 처리기를 등록하지 않는다. 권능 숫자 합계 포함과 전투 종료 제거는 확정 사항이다. 부분 등록이나 임시 기본값은 UI와 테스트가 잘못된 규칙을 정식 기능으로 간주하게 하므로 금지한다.

결정 뒤에는 최소한 다음 경계를 검증한다.

- 스탠드 불가 사유
- 모든 버스트 원인에 대한 방지와 카드 이동 완료 여부
- 카운터 감소 시점과 추가 행동 제외
- 영혼 2 대가와 계약 종료 또는 재설정
- 권능 카드의 물리 ID, 양면 상태, 사용 완료 상태와 제거 위치
- 권능 카드 숫자 변경 직후의 합계 재계산과 전투 종료 제거

## 10. 런 전투 변환

`StageBattleFactory`는 일반 카드와 악마 카드를 서로 다른 변환 함수로 처리한다.

```text
RunCardDefinition → BlackjackCard → BlackjackDeck
RunDemonDefinition → DemonContractCard → DemonContractDeck
```

- 일반 카드 카탈로그와 악마 카탈로그를 합치지 않는다.
- 전투마다 새 덱, 새 버린 더미, 새 활성 상태와 새 처리기 런타임 상태를 만든다.
- 런 악마 덱의 물리 ID와 정의 키를 그대로 보존한다.
- 전투 셔플 시드는 기존 전투 시드에서 독립적으로 파생해 일반 카드 순서를 바꾸지 않는다.
- 재시작은 최초 악마 덱과 다음 ID를 복구한다.
- 전투 결과 동기화 전 임시 생성 카드를 제거하고 파이몬 추방·벨리알 소유권을 복구해 런 일반 덱이 영구 변경되지 않게 한다.

## 11. 표시 모델

권장 표시 모델은 다음 범위로 제한한다.

```text
DemonContractAvailabilityViewModel
DemonContractOptionViewModel
ActiveDemonContractViewModel
DemonContractResultViewModel
```

후보 표시에는 정의 키 대신 이름·능력·대가·선택 ID를 제공한다. 활성 표시는 카운터·주사위 등 공개 가능한 런타임 값만 포함한다. 벨페고르 미리보기는 플레이어 전용 선택 모델에만 실제 카드 정보를 담고 공용·적 표시 스냅샷에서는 제외한다.

GameScene 연결은 우선 스크립트 기반으로 진행하고 `GameScene.unity`의 앵커·직렬화 변경은 별도 시각 작업으로 분리한다. 기존 일반 카드의 좌우 투영 순서와 비공개 정보 은닉을 바꾸지 않는다.

DC-05 구현에서는 `DemonContractPanelViewModel`이 가용성·비용·계약 후 영혼·남은 횟수, 현재 상호작용 ID·종류, 후보·활성 계약·소유자 전용 미리보기와 최근 결과를 한 경계로 묶는다. 후보는 `DemonContractChoiceViewModel`에서 제목·능력·대가·선택 가능 여부를 분리하며 사탄은 DC-06 전까지 선택 불가로 표시한다. `CoreLoopView`와 `GameManager`는 확인 전 로컬 상태만 사용하고 승인 시 기존 `CoreLoopSession` 또는 `StageProgressionSession` 명령을 호출한다. 독립 전투는 `DemonContractDeck.CreatePrototype`으로 네 장을 주입하고, 런 전투는 기존 `PlayerRunState`→`StageBattleFactory` 변환을 유지한다.

두 UI는 후보 설명과 선택 버튼을 분리하고 720p·1080p 높이에 맞는 레이아웃을 사용한다. `GameScene.unity`·`CoreLoopTest.unity` 직렬화는 변경하지 않았고 표시만 스크립트에서 확장했다. 계약 결과의 런 영혼·패배·보상 동기화는 기존 세션 경계를 재사용하며, 신규 표시·Controller 테스트 7/7과 전체 EditMode 369/369로 회귀를 확인했다.

## 12. 적 AI 연결

DC-07에서 적 행동 후보에 계약을 추가했다.

- 정책에는 후보 악마의 공개 정의, 자기 영혼, 계약 후 영혼, 현재 공개 전투 정보만 전달한다.
- 플레이어 비공개 카드, 플레이어 덱 순서와 주입 난수의 미래 결과는 전달하지 않는다.
- 계약 후보 생성은 행동 실행 승인 시점에만 이루어지며 정책 평가가 덱 순서를 바꾸지 않는다.
- 광신도는 안전보다 계약 가중치를 높이되 `현재 영혼 > 비용`을 우회하지 않는다.
- 적 계약 선택과 후속 선택도 실행 직전 재검증한다.

후보 평가는 악마 덱을 건드리지 않는 일반 `DemonContract` 후보로 시작한다. 실행 승인이 난 뒤에만 비용을 내고 실제 후보 3장을 가져오며, 이후 선택 후보에는 공개 정의와 정책에 필요한 소유자 전용 값만 담는다. `CultistEnemyPolicy`는 계약을 일반 히트보다 높게 평가하지만 계약 후 영혼이 사탄 종료 대가 이하라면 다른 후보를 우선한다.

DC-08에서는 계약 종류 점수를 다음 효용 조건으로 고정했다.

- 사탄: 현재 영혼이 종료 대가 2보다 클 때만 생존 가능한 선택으로 평가한다.
- 레비아탄: `OwnCards`에 `CardUseState.Available`인 `AutoPistol` 정의가 있을 때만 평가한다. 계약 선택 대기 중 `CanUse`는 입력 잠금 때문에 거짓일 수 있으므로 카드의 영구 사용 상태와 정의를 사용한다.
- 벨페고르·마몬: 같은 기본 점수를 주고 `EnemyPolicyDecisionSelector`의 안정적인 후보 순서를 동점 기준으로 사용한다. 후보 순서는 전투 시드로 재현되며 추가 난수를 소비하지 않는다.
- 마몬 재굴림: `MammonRerollCeiling = 2`를 경계로 유지한다.

`CultistContractBalanceTests`는 400개 프로토타입 시드의 계약 사용·선택 분포, 같은 시드 재현, 사탄 생존 여유, 레비아탄 리볼버 보유, 벨페고르 공개 합 21 경계, 마몬 2/3 재굴림과 최종 전체 합, 100회 자동 전투 종료를 검증한다.

`StageBattleFactory`는 광신도 프로필에만 전투별 4장 악마 덱을 주입한다. 적 활성 계약은 플레이어와 분리된 컬렉션으로 보존하되 동일한 `DemonContractResolver` 처리기를 소유자 방향만 바꾸어 사용한다. 벨페고르 미리보기, 마몬 차례·최종 선택, 레비아탄 카드 후속 훅, 사탄 스탠드·버스트·카운터·권능 수명은 모두 적 소유자에도 같은 규칙으로 적용한다.

## 13. 테스트 명세

### 13.1 데이터·덱

- 네 정의 키와 비용·종류가 정확하다.
- 알 수 없는 키, 중복 물리 ID와 3장 미만 덱을 거절한다.
- 후보·버림·활성·드로우 사이 카드 총수와 ID가 보존된다.
- 전투마다 덱·활성 상태가 독립적이고 런 재시작 시 초기 덱이 복구된다.

### 13.2 계약 시작·선택

- 영혼이 비용과 같거나 적으면 무변경 거절한다.
- 영혼 2에서 비용 1을 내고 후보를 연다.
- 전투당 두 번째 기본 계약을 거절한다.
- 후보 공개 뒤 다른 행동과 취소를 거절한다.
- 잘못된 `InteractionId`·옵션은 무변경 거절한다.
- 선택 카드 1장만 활성화되고 나머지 2장은 버린다.

### 13.3 악마 효과

- 벨페고르의 미리보기, 히트, 덱 아래 이동, 자동 스탠드와 정보 은닉
- 마몬의 결정적 주사위, 6 버스트, 재굴림 1회, 최종 합 적용·미적용
- 레비아탄의 기존 리볼버 성공, 공개 합 추가 성공, 최종 실패 대가와 숨은 합 미노출
- 사탄 결정 뒤 스탠드 금지, 버스트 방지, 카운터·대가와 권능 카드 수명

### 13.4 통합·UI

- 독립 전투와 런 전투가 같은 계약 결과를 만든다.
- 계약 효과 승리·패배가 보상과 지속 영혼에 한 번만 반영된다.
- 계약 선택 중 일반 입력이 잠긴다.
- 1280×720·1920×1080에서 후보 3장과 대가가 잘리지 않는다.
- GameScene의 양측 비공개 최좌측 표시와 정보 은닉이 유지된다.

### 13.5 반복 검증

- 네 악마 정상·거절·위험 경로 각 10회
- 전투 종료·다음 전투·런 재시작 각 10회
- 사탄 권능·오망성 제거, 파이몬 추방 복구와 벨리알 원소유권 복구 각 10회
- 동일 악마 두 계약의 물리 ID·런타임 상태 격리와 개별 대가 각 10회
- 오래된 상호작용 ID와 중복 입력 각 10회
- 고정 시드 카드 총수·소유권·난수 재현 50회

## 14. 예상 파일 배치

```text
Assets/01. Scripts/CoreLoop/DemonContracts/
  DemonContractDefinition.cs
  DemonContractCatalog.cs
  DemonContractDeck.cs
  DemonContractSelection.cs
  DemonContractResolver.cs
  Handlers/

Assets/01. Scripts/StageProgression/
  RunDemonDefinition.cs
  PlayerRunState.cs
  StageBattleFactory.cs
  StageProgressionSession.cs

Assets/01. Scripts/UI/CoreLoop/
  CoreLoopPresentation.cs
  CoreLoopView.cs
  CoreLoopController.cs

Assets/06.Packages/Tests/EditMode/CoreLoop/
Assets/06.Packages/Tests/EditMode/StageProgression/
```

실제 폴더명은 현재 저장소 구조를 따르며, 새 어셈블리나 패키지는 만들지 않는다.

## 15. 변경 제한

- 일반 카드와 악마 카드를 하나의 카탈로그·덱으로 합치지 않는다.
- 계약을 `CardEffectKind`의 특수 카드처럼 구현하지 않는다.
- 확정되지 않은 사탄 규칙을 임시 코드로 숨기지 않는다.
- 계약 임시 카드 정리를 정의 키 전체 삭제로 처리하지 않는다.
- 같은 악마 계약을 종류 하나의 전역 상태로 합치지 않는다.
- 계약 조회 속성이 셔플·드로우·난수를 소비하지 않게 한다.
- UI가 숨은 카드·덱 순서·주사위 미래 결과를 계산하거나 노출하지 않게 한다.
- 정식 런·상점 HONG 담당 코드를 선점하지 않는다.
- 씬·프리팹·패키지·외부 에셋은 해당 단계의 명시적 필요가 없으면 변경하지 않는다.

## 16. 완료 정의

- DC-01~DC-08의 대상 테스트와 전체 EditMode가 모두 통과한다.
- 네 우선 악마의 표시 문구·효과·대가·발동 순서가 일치한다.
- 계약과 기존 카드·AI·진행·보상 회귀가 함께 통과한다.
- 실제 독립·런 전투, 두 해상도와 Console을 검증한다.
- 외부 에셋이 추가되면 출처·라이선스, AI가 사용되면 구조·주요 지시·검증을 기록한다.
- 이천서의 실제 구현 영역만 팀 역할 문서에 완료로 기록한다.

## 17. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-22 | 이천서 | DC-00 데이터·상태·트랜잭션·명시적 효과 훅·런 변환·UI·AI·테스트 명세 수립 |
| 2026-07-22 | 이천서 | 동일 악마 인스턴스 분리, 개별 대가 사망, 계약 생성 카드 합계와 사탄·바포메트·파이몬·벨리알의 전투 종료 원상복구 명세 추가 |
| 2026-07-22 | 이천서 | DC-01 실제 구현에 맞춰 네 정의의 요약·대가, 후보 3장 이동·버림 보충, 빈 독립 전투 덱과 런→전투 변환·독립 시드 계약 기록 |
| 2026-07-22 | 이천서 | DC-02 실제 구현에 맞춰 비용·횟수 가용성, 증가형 상호작용 ID, 필수 선택·활성/버림 이동, 주입식 활성화 처리와 CoreLoop·StageProgression 세션 전달 기록 |
| 2026-07-22 | 이천서 | DC-03 실제 구현에 맞춰 벨페고르 선택 훅, 소유자 전용 미리보기, 동일 ID 공개 히트·덱 아래 이동, 행동 종료 자동 스탠드와 정보 은닉 기록 |
| 2026-07-22 | 이천서 | DC-04 실제 구현에 맞춰 주입식 6면체 난수, 마몬 차례·최종 선택, 에이스 포함 보정 합, 레비아탄 카드 완료 후 훅과 숨은 합 비노출 결과 기록 |
| 2026-07-23 | 이천서 | DC-05 실제 구현에 맞춰 계약 패널 표시 모델, 비용 확인·취소, 후보 설명·사탄 비활성화, 활성 상태·소유자 전용 정보, 독립/런 Controller와 두 씬 반응형 UI 기록 |
| 2026-07-23 | 이천서 | DC-06 실제 구현에 맞춰 정상 차례 훅·스탠드/버스트 제한·영혼 대가·임시 권능 등록/변형/정리·화염 강제 히트·괴력 두 수 선언·안전 표시와 8개 회귀 테스트 기록 |
| 2026-07-23 | 이천서 | DC-07 실제 구현에 맞춰 무변경 적 계약 후보, 실행 후 실제 3장 선택, 광신도 위험 회피, 적 소유 4종 대칭 처리·표시, Cultist 전용 덱과 대상 12/12·전체 389/389 회귀 기록 |
| 2026-07-23 | 이천서 | DC-08 실제 구현에 맞춰 사탄 생존 여유·레비아탄 리볼버 보유 조건, 벨페고르·마몬 결정적 균형 분산, 마몬 임계값과 400시드·100자동전투 검증 명세 기록 |
