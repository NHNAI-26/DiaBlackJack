# 카드 사용 시스템 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·개발 책임자: 이천서  
> 버전: v0.1  
> 상태: CU-04 완료 · CU-05 착수 가능
> 최종 갱신: 2026-07-19

## 1. 기술 목표

완료된 `CoreLoopBattle`의 차례·라운드·영혼 처리와 `StageProgressionSession`의 진행 동기화를 유지하면서 플레이어 카드 사용을 추가한다. 첫 구현은 순수 C# 규칙 계층과 EditMode 테스트를 우선하며, Unity 화면은 규칙 모델을 다시 계산하지 않고 읽기 전용 표시 모델만 사용한다.

## 2. 현재 기준선

2026-07-19 CU-00 문서 작성 시점의 기준은 다음과 같다.

- `BlackjackCard`: 고유 ID, 숫자, 공개 여부만 보유한다.
- `RunCardDefinition`: 고유 ID와 숫자만 보유한다.
- `CoreLoopState`: 플레이어 일반 차례와 체인지 선택 상태를 구분한다.
- `CoreLoopBattle`: 히트, 스탠드, 폴드, 체인지와 적 차례를 처리한다.
- `CoreLoopSession`: 전투 행동을 얇게 전달한다.
- `StageProgressionSession`: 런 전투 행동 뒤 종료·영혼을 동기화한다.
- `CoreLoopView`: 즉시 모드 디버그 UI로 카드 목록과 네 행동 버튼을 표시한다.
- 직전 전체 EditMode 회귀 기준은 82/82 통과다.

CU-00에서는 게임 코드, 씬, 패키지와 에셋을 변경하지 않는다. 82/82는 BA-05에서 확보한 기준이며 이번 문서 작성 과정에서 Unity 테스트를 다시 실행했다는 의미가 아니다.

### 2.1 CU-01 구현 결과

2026-07-19에 순수 C# 카드 정의·카탈로그와 물리 카드별 사용 상태를 구현했다. 기존 `(id, rank, isFaceUp)` 생성자는 숫자별 기본 정의로 연결해 호환성을 유지했고, `RunCardDefinition`은 안정된 `DefinitionKey`를 보존한 뒤 `StageBattleFactory`가 해당 키를 다시 해석한다. 알 수 없는 키는 명시적으로 실패한다.

수동 카드의 상태 초기화는 카드가 손에 들어오는 공통 경계인 `BlackjackHand.Add`에 모았다. 따라서 일반 드로우, 체인지 선택 카드와 후속 효과 드로우가 같은 규칙을 사용하며, 사용 완료 카드는 같은 손에 있는 동안 `Used`를 유지하고 버린 뒤 다시 드로우되어 손에 들어올 때만 `Available`로 돌아간다. 카드 효과 실행 API·선택 상태·UI는 CU-02 이후 범위로 남겼다.

CU-01 신규 19개 테스트를 포함한 전체 EditMode 101/101과 Unity Console Error/Warning 0을 확인했다. 데이터 단계이므로 씬과 Game View는 변경하거나 검증하지 않았다.

### 2.2 CU-02 구현 결과

2026-07-19에 카드 사용 가능 여부와 불가 사유, 선택 대기 모델, 효과 처리기 등록 경계와 완료 결과를 구현했다. `CoreLoopBattle`은 카드 ID로 사용을 요청받아 모든 시작 조건을 먼저 검사하고, 승인된 카드만 공개·`Resolving` 전이한 뒤 선택 대기 또는 즉시 완료를 처리한다. `PlayerResolvingCardEffect` 중에는 히트·스탠드·폴드·체인지·다른 카드 사용을 모두 거절한다.

손패는 ID 기반 조회·인출, 덱은 상단 카드 임시 분리·원래 다음 드로우 순서 반환을 제공한다. 카드 효과가 라운드를 끝낸 경우 `RoundEndCause.CardEffectBust`와 원인 카드 키를 기록하고 적 차례를 실행하지 않는다. 효과가 끝나지 않은 경우에만 기존 적 차례를 정확히 한 번 재사용한다. `CoreLoopSession`에는 카드 사용 시작·선택 전달만 추가했다.

출시 카탈로그에는 아직 실제 효과 처리기를 등록하지 않았다. 따라서 기본 전투의 수동 카드는 `EffectNotImplemented`로 안전하게 거절되며, 테스트에서는 내부 처리기를 주입해 즉시 완료·단일 선택·연속 선택·효과 종료 경계를 검증했다. CU-02 신규 16개를 포함해 CoreLoop 87/87, 전체 EditMode 117/117이 통과했고 Console Error/Warning 0을 확인했다. 카드 4종의 실제 규칙, View, 진행 세션 전달, 씬·패키지·외부 에셋 변경은 CU-03 이후 범위로 남겼다.

### 2.3 CU-03 구현 결과

2026-07-19에 기본 효과 처리기 목록에 `AutoPistolEffectHandler`를 등록했다. 숫자 7·8 자동 권총은 정상 라운드의 상대 비공개 카드 한 장을 대상으로 1~10 선택지를 제공하며, 선언 값과 실제 숫자의 비교는 규칙 계층 내부에서만 수행한다. 두 장 이상의 비공개 카드를 위한 기능은 추가하지 않았고, 대상 카드가 없으면 승인 전에 상태 변경 없이 거절한다.

추측 성공 시 기존 `CardEffectBust` 결과로 적 영혼을 정확히 1 감소시키고 적 차례 없이 다음 라운드 또는 전투 승리로 진행한다. 실패 시 영혼 변화 없이 사용 카드를 `Used`로 완료하고 기존 적 차례를 정확히 한 번 실행한다. `CardEffectResult`의 공개 정보는 효과 유형·원본 카드 ID·성공 여부·라운드 종료 여부뿐이며 상대 비공개 숫자는 공개하거나 결과에 저장하지 않는다.

CU-03 신규 8개를 포함해 CoreLoop 95/95, 전체 EditMode 125/125가 통과했다. 최종 View 입력, `StageProgressionSession` 전달, 수정 구슬·해머·나이프, 씬·패키지·외부 에셋 변경은 CU-04 이후 범위로 남겼다.

### 2.4 CU-04 구현 결과

2026-07-19에 `CrystalOrbEffectHandler`, `ThreatHammerEffectHandler`, `MilitaryKnifeEffectHandler`를 기본 효과 처리기 목록에 등록했다. 수정 구슬은 덱 위 두 장을 효과 임시 영역에 분리한 뒤 0장·첫째·둘째 선택을 받고, 가져오지 않은 카드를 기존 다음 드로우 순서대로 복구한다. 선택 카드는 공개 손패로 이동하며 숫자 합계 초과는 기존 `RoundResolver`의 `NumericBust`로 즉시 처리한다.

위협용 해머는 사용 승인으로 공개된 원본 카드까지 포함해 플레이어 공개 카드만 비용으로 제시한다. 상대가 스탠드하지 않았다면 비용만 지불하고, 스탠드했다면 정상 규칙의 비공개 카드 정확히 한 장과 덱 한 장을 사전 검사한 뒤 기존 비공개 카드를 공개하지 않고 버리고 새 비공개 카드로 원자적으로 교체하며 스탠드를 취소한다. 교체 직후 합계 초과도 적 차례 전에 `NumericBust`로 처리한다.

군용 나이프 9·10은 상대 공개 합계가 16 이하이고 덱이 한 장 이상일 때만 공개 강제 드로우를 수행한다. 버스트가 아니면 교체 가능한 유지 정책에 결과를 위임하며, 현재 프로토타입 정책은 항상 유지한다. CU-04 전용 18/18, CoreLoop 113/113, 전체 EditMode 143/143이 통과했다. 이번 단계는 순수 규칙·CoreLoop 세션 범위이므로 View, `StageProgressionSession`, 씬, 패키지와 외부 에셋은 변경하지 않았으며 실제 화면 사용은 CU-05에서 연결한다.

## 3. 설계 원칙

1. 카드 규칙은 Unity 객체에 의존하지 않는 순수 C#으로 구현한다.
2. 기존 `BlackjackCard(int id, int rank, bool isFaceUp = false)` 사용부와 테스트는 단계적으로 호환한다.
3. 문자열·리플렉션 기반 효과 디스패처를 만들지 않는다.
4. 카드 정의와 물리 카드 인스턴스의 변경 상태를 분리한다.
5. 사용 조건 검사는 상태 변경보다 먼저 끝낸다.
6. 선택 대기 중에는 단 하나의 카드 효과만 존재한다.
7. 효과 명령 뒤마다 즉시 종료 조건을 확인한다.
8. Presenter와 View는 숨겨진 값을 읽거나 규칙을 재계산하지 않는다.
9. 진행 시스템은 전투 내부 효과를 해석하지 않고 승인된 입력 전달과 전투 종료 동기화만 맡는다.
10. 계약이 재사용할 수 있는 효과 경계는 두되, 확정되지 않은 계약 추상화는 만들지 않는다.

## 4. 카드 데이터 모델

### 4.1 카드 정의

신규 순수 C# 정의는 최소 다음 정보를 가진다.

```text
CardDefinition
- Key: string
- DisplayName: string
- Rank: int
- Activation: CardActivationKind
- Effect: CardEffectKind
```

`Key`는 저장·런 덱·표시가 공유하는 안정 식별자다. 표시 이름을 키로 사용하지 않는다.

```text
CardActivationKind
- None
- Passive
- Manual
- Automatic

CardEffectKind
- None
- CrystalOrb
- ThreatHammer
- AutoPistol
- MilitaryKnife
```

CU 범위에서는 `Manual`만 실행한다. `Passive`와 `Automatic` 값은 분류와 향후 확장 경계를 위해 정의하되 자동 처리 코드는 추가하지 않는다.

### 4.2 기본 카드 카탈로그

| 키 | 이름 | 숫자 | 발동 | 효과 |
| --- | --- | ---: | --- | --- |
| `standard-ace-1` | 에이스 | 1 | Passive | None |
| `standard-plain-2` | 기본 카드 | 2 | None | None |
| `standard-plain-3` | 기본 카드 | 3 | None | None |
| `standard-plain-4` | 기본 카드 | 4 | None | None |
| `crystal-orb-5` | 수정 구슬 | 5 | Manual | CrystalOrb |
| `threat-hammer-6` | 위협용 해머 | 6 | Manual | ThreatHammer |
| `auto-pistol-7` | 자동 권총 | 7 | Manual | AutoPistol |
| `auto-pistol-8` | 자동 권총 | 8 | Manual | AutoPistol |
| `military-knife-9` | 군용 나이프 | 9 | Manual | MilitaryKnife |
| `military-knife-10` | 군용 나이프 | 10 | Manual | MilitaryKnife |

기존 숫자만 있는 생성 경로는 위 기본 키로 변환한다. 이후 같은 숫자에 여러 카드가 추가되면 `RunCardDefinition`이 명시적 키를 보존한다.

### 4.3 카드 인스턴스

`BlackjackCard`에 다음 읽기 전용·상태 정보를 추가한다.

```text
- Definition: CardDefinition
- UseState: CardUseState

CardUseState
- Unavailable
- Available
- Resolving
- Used
```

- 수동 카드가 손에 들어오면 `Available`이다.
- 수동 카드가 아니면 `Unavailable`이다.
- 승인된 효과가 끝나지 않았으면 `Resolving`이다.
- 효과가 끝나면 `Used`다.
- 덱에서 다시 손으로 들어올 때 정의에 맞게 초기화한다.
- ID, 숫자와 공개 여부의 기존 계약은 유지한다.

`Definition.Rank`와 인스턴스 `Rank`가 다를 수 없도록 생성자에서 검증하거나 숫자의 단일 원천을 정의 객체로 통일한다.

### 4.4 런 덱 데이터

`RunCardDefinition`은 안정 키를 보존해야 한다. 호환을 위해 기존 `(id, rank)` 생성자는 기본 카탈로그 키로 위임할 수 있다.

```text
RunCardDefinition
- Id
- DefinitionKey
- Rank (Definition에서 읽거나 생성 시 검증)
```

`StageBattleFactory`는 키를 `CardDefinitionCatalog`에서 해석해 `BlackjackCard`를 만든다. 알 수 없는 키는 효과 없는 카드로 조용히 대체하지 않고 명확한 예외로 실패시킨다.

## 5. 상태 모델

### 5.1 전투 상태 확장

`CoreLoopState`에 다음 상태 하나를 추가한다.

```text
PlayerResolvingCardEffect
```

카드별 선택 상태를 열거형으로 계속 추가하지 않는다. 현재 질문과 선택지는 `PendingCardEffect`가 표현한다.

### 5.2 선택 모델

```text
PendingCardEffect
- SourceCardId
- EffectKind
- Prompt
- ChoiceKind
- Options: IReadOnlyList<CardEffectChoiceOption>

CardEffectChoiceKind
- None
- TakePeekedCard
- DiscardOwnFaceUpCard
- DeclareNumber

CardEffectChoiceOption
- Id: int
- Label: string
- CardId: int?       // 필요한 경우만
- NumericValue: int? // 필요한 경우만
```

UI는 `Option.Id`만 전투 API로 돌려준다. 카드 ID, 배열 인덱스와 숫자 값을 UI가 임의로 변환하지 않는다.

### 5.3 상태 전이

```text
PlayerTurn
  └─ TryBeginPlayerCardUse(cardId)
       ├─ 거절 -> PlayerTurn, 변경 없음
       ├─ 즉시 종료 효과 -> ResolvingRound 또는 BattleEnded
       ├─ 선택 필요 -> PlayerResolvingCardEffect
       └─ 선택 불필요·전투 계속 -> EnemyTurn

PlayerResolvingCardEffect
  └─ TryResolvePlayerCardChoice(optionId)
       ├─ 거절 -> 동일 상태, 변경 없음
       ├─ 다음 선택 필요 -> PlayerResolvingCardEffect
       ├─ 효과 종료 -> ResolvingRound 또는 BattleEnded
       └─ 효과 완료 -> EnemyTurn
```

`PlayerResolvingCardEffect`에서는 히트, 스탠드, 폴드, 체인지 시작·선택, 다른 카드 사용을 모두 거절한다.

## 6. 공개 행동 API

### 6.1 `CoreLoopBattle`

```text
bool CanUsePlayerCard(int cardId)
bool TryBeginPlayerCardUse(int cardId)
bool TryResolvePlayerCardChoice(int optionId)

IReadOnlyList<CardUseAvailability> PlayerCardUseAvailability
PendingCardEffect PendingPlayerCardEffect
CardEffectResult? LastCardEffectResult
```

`CanUsePlayerCard`는 표시를 위한 읽기 전용 질의이며 상태를 바꾸지 않는다. `CardUseAvailability`에는 카드 ID, 가능 여부와 기계 판정용 사유 코드를 둔다. 표시 문구는 Presenter가 만든다.

### 6.2 세션 전달

`CoreLoopSession`과 `StageProgressionSession`에 같은 두 명령을 얇게 전달한다.

```text
TryBeginPlayerCardUse(cardId)
TryResolvePlayerCardChoice(optionId)
```

`StageProgressionSession`은 각 성공 호출 뒤 기존 `SynchronizeFinishedBattle()`을 실행한다. 카드별 효과나 선택 내용을 진행 계층에서 분기하지 않는다.

### 6.3 Controller 전달

```text
RequestUseCard(cardId)
RequestResolveCardEffectChoice(optionId)
```

Controller는 독립 전투와 런 전투 중 활성 세션만 선택한다. 성공 여부와 관계없이 기존 입력 잠금·화면 갱신 규칙을 적용한다.

## 7. 효과 처리 구조

### 7.1 책임

| 구성요소 | 책임 |
| --- | --- |
| `CardUseValidator` | 공통·카드별 사전 조건과 불가 사유 계산 |
| `CardEffectResolver` | 정의된 효과 시작·선택 재개·완료 제어 |
| `CardEffectContext` | 전투, 사용자·상대, 사용 카드와 현재 효과 데이터 접근 |
| `PendingCardEffect` | 선택 사이에 필요한 최소 상태 보존 |
| 전투 모델 | 명령 적용, 버스트·차례·라운드·전투 종료 |

첫 구현에서 각 카드 효과는 명시적인 타입 분기로 연결한다. 효과 문구를 문자열로 해석하거나 임의 파라미터 배열을 실행하지 않는다.

### 7.2 공통 명령 경계

효과 처리기는 아래 전투 명령을 재사용한다.

- 카드 공개
- 덱에서 지정 수 임시 분리
- 임시 카드를 손에 공개 추가
- 임시 카드를 기존 순서대로 덱 위에 반환
- 손패 카드 버리기
- 스탠드 취소
- 비공개 카드 교체
- 강제 공개 드로우
- 직접 버스트
- 버스트·전투 종료 확인

명령은 카드 효과가 손패·덱 내부 컬렉션을 직접 수정하지 않게 한다. 계약 작업은 나중에 같은 명령 경계를 호출할 수 있다.

### 7.3 원자성과 실패

- 시작 전 검증에서 필요한 카드, 덱 잔량과 최소 선택지를 확보한다.
- 유효하지 않은 카드 ID·옵션 ID는 `false`를 반환하고 상태를 바꾸지 않는다.
- 선택 대기 중 임시로 덱에서 분리한 카드는 `PendingCardEffect`가 소유하며 덱·손·버린 더미 중복 소유를 금지한다.
- 이미 승인된 효과에는 일반 취소·롤백 API를 두지 않는다.
- 검증 후 예외가 발생하면 프로그래밍 오류로 취급한다. 예외를 삼키거나 부분 성공으로 계속하지 않는다.

## 8. 손패와 덱 기능

### 8.1 손패

다음 기능을 ID 기반으로 제공한다.

```text
bool TryGetCard(int cardId, out BlackjackCard card)
bool Contains(int cardId)
bool TryTakeCard(int cardId, out BlackjackCard card)
IReadOnlyList<BlackjackCard> GetFaceUpCards()
```

카드 참조를 받은 호출자가 내부 목록을 수정할 수 없게 한다. 버림은 `BattleParticipant`가 손에서 원자적으로 제거한 뒤 해당 참가자의 덱에 전달한다.

### 8.2 덱 임시 분리와 반환

수정 구슬을 위해 다음 의미의 기능을 추가한다.

```text
bool CanDraw(count)
IReadOnlyList<BlackjackCard> TakeTop(count)
void ReturnToTop(cardsInNextDrawOrder)
```

- `TakeTop`은 기존 드로우와 같은 다음 카드 순서를 반환한다.
- 분리된 카드는 덱의 사용 가능 카드 수에서 제외된다.
- `ReturnToTop` 인자는 다음 드로우 순서다.
- 반환 시 카드 ID 소유권·중복 검사를 수행한다.
- 덱 소진 재순환은 기존 결정적 셔플 규칙을 재사용한다.

### 8.3 참가자 기능

- 카드 사용을 위한 손패 조회와 공개
- 손패 카드 버리기
- 지정 카드를 공개 상태로 손에 추가
- 비공개 카드 교체와 스탠드 취소
- 강제 드로우 카드 유지·버림

효과 처리기가 `IsStanding` setter나 손패 내부 목록에 직접 접근하지 않도록 명시적 메서드로 제공한다.

## 9. 카드별 기술 흐름

### 9.1 수정 구슬

1. `CanDraw(2)` 검사
2. 카드 2장 임시 분리
3. `TakePeekedCard` 옵션 세 개 생성
4. 선택 없음이면 두 장을 원래 다음 드로우 순서로 반환
5. 카드 선택이면 선택 카드를 공개해 손에 추가, 나머지 한 장을 덱 위로 반환
6. 플레이어 숫자 버스트 확인

선택지에는 실제 카드 ID를 보존하며 View에는 플레이어에게만 숫자·이름을 표시한다.

### 9.2 위협용 해머

1. 승인 후 사용 카드 공개
2. 버릴 수 있는 자기 공개 카드 ID 옵션 생성
3. 선택 카드를 손에서 제거해 자기 덱 버린 더미로 이동
4. 상대 비스탠드면 완료
5. 상대 스탠드면 취소 후 비공개 카드 원자적 교체
6. 상대 숫자 버스트 확인

상대 스탠드 상태에서 교체 조건을 만족하지 않으면 시작 검증에서 거절한다.

### 9.3 자동 권총

1. 상대 비공개 카드 정확히 한 장 검사
2. 1~10 선택 옵션 생성
3. 선택 값을 숨겨진 카드 숫자와 규칙 계층에서 비교
4. 성공 시 `RoundResolver`의 카드 효과 버스트 결과로 적 영혼 피해 적용
5. 실패 시 결과만 기록하고 효과 완료

Presenter에는 실제 숨은 카드 값이 전달되지 않는다.

### 9.4 군용 나이프

1. 상대 공개 합 `<= 16`, `CanDraw(1)` 검사
2. 상대가 공개 카드 한 장 강제 드로우
3. 상대 전체 합 버스트면 즉시 라운드 종료
4. 아니면 `IForcedDrawRetentionPolicy`에 유지·버림 문의
5. 버림이면 방금 뽑은 카드만 상대 덱 버린 더미로 이동
6. 효과 완료 후 기존 적 차례 흐름 실행

기본 `SimpleForcedDrawRetentionPolicy`는 비버스트 카드를 유지한다. 정책은 플레이어나 상대의 허용되지 않은 비공개 정보에 접근하지 않지만, 상대는 자신의 손패를 아는 주체이므로 자기 전체 합은 사용할 수 있다.

## 10. 라운드 결과 확장

자동 권총 등 직접 버스트를 숫자 초과와 구분해 기록할 수 있어야 한다.

권장 구조는 기존 피해량 결정을 깨지 않으면서 원인 정보를 추가하는 것이다.

```text
RoundEndCause
- TotalComparison
- NumericBust
- Fold
- CardEffectBust
```

`RoundOutcome`을 무리하게 카드별로 늘리지 않는다. 승패·피해는 기존 결과를 재사용하고 `Cause`와 선택적인 `SourceCardKey`로 로그 문구를 만든다.

카드 효과 버스트의 기본 피해는 대상에 따른 기존 버스트 피해다.

- 플레이어 버스트: 플레이어 영혼 2 감소
- 적 버스트: 적 영혼 1 감소
- 별도 카드 피해: 카드 정의가 명시한 경우에만 추가

## 11. 표시 모델과 UI

### 11.1 읽기 전용 모델

```text
PlayerCardViewModel
- CardId
- Rank
- DisplayName
- IsFaceUp
- UseState
- CanUse
- DisabledReason

CardEffectChoiceViewModel
- OptionId
- Label
```

`CoreLoopViewModel`에는 플레이어 카드 목록, 현재 효과 안내, 선택지와 최근 효과 결과를 추가한다. 상대 카드 ViewModel에는 정의 키나 숨은 숫자를 노출하지 않는다.

### 11.2 최소 View

- 기존 참가자 카드 문자열은 유지해 회귀를 줄인다.
- 행동 영역 위 또는 아래에 플레이어 카드별 `USE` 버튼 목록을 추가한다.
- 선택 상태에서는 일반 행동과 카드 목록을 숨기거나 비활성화하고 현재 효과 선택지만 표시한다.
- 카드가 많아질 경우 스크롤·최종 카드 레이아웃은 후속 UI 작업으로 둔다.

### 11.3 입력 잠금

Controller는 이벤트를 받으면 즉시 입력을 잠그고 세션 호출과 렌더 갱신 뒤 해제한다. EditMode에서 Unity 예약 호출을 사용할 수 없는 경우 기존 BA-04의 즉시 해제 경로를 유지한다.

## 12. 진행 시스템 연결

런 전투 카드 사용은 반드시 `StageProgressionSession`을 통과한다.

1. 진행 상태가 `InBattle`인지 확인한다.
2. 내부 `CoreLoopSession`에 명령을 전달한다.
3. 실패하면 동기화 없이 `false`를 반환한다.
4. 성공하면 `SynchronizeFinishedBattle()`을 호출한다.
5. 전투가 끝나지 않았으면 진행 상태와 지속 영혼은 그대로 둔다.
6. 전투가 끝났으면 기존 승리·패배 경로로 현재 영혼과 진행 상태를 반영한다.

Controller가 `_stageSession.Battle`을 직접 조작하는 경로를 추가하지 않는다.

## 13. 자동 테스트 명세

### 13.1 CU-01 — 데이터·사용 상태

- 숫자별 기본 정의가 올바른 키·발동·효과로 매핑된다.
- 기존 숫자 생성자가 동일 숫자·ID 동작을 보존한다.
- 알 수 없는 정의 키는 명확히 실패한다.
- 수동 카드만 손에 들어올 때 사용 가능해진다.
- 사용 완료 카드는 같은 손에서 다시 사용할 수 없다.
- 버린 뒤 다시 드로우하면 사용 상태가 초기화된다.
- 런 카드 키가 전투 카드 정의로 보존된다.

### 13.2 CU-02 — 효과 선택 기반

- 플레이어 차례가 아니면 시작을 거절한다.
- 없는 카드, 비수동 카드와 사용 완료 카드를 거절한다.
- 승인 전 실패는 공개·카드 이동·상태·차례를 바꾸지 않는다.
- 승인된 비공개 사용 카드는 공개된다.
- 선택 상태에서 모든 다른 행동을 거절한다.
- 잘못된 옵션 ID는 선택 상태를 보존한다.
- 정상 선택 완료 뒤 적 차례가 정확히 한 번 실행된다.
- 효과 중 라운드 종료 시 적 차례가 실행되지 않는다.

### 13.3 CU-03 — 자동 권총

- 1~10 외 선택을 제공하지 않는다.
- 숨은 숫자 일치 시 적 버스트와 영혼 1 피해를 적용한다.
- 불일치 시 피해 없이 적 차례로 진행한다.
- 효과 결과·표시에 실제 숨은 값이 포함되지 않는다.
- 사용 완료 뒤 재사용을 거절한다.
- 카드 효과로 적 영혼 0이면 전투 승리로 끝난다.

### 13.4 CU-04 — 나머지 카드

- 수정 구슬 0장·첫 카드·둘째 카드 선택의 카드 소유권과 다음 드로우 순서가 맞다.
- 수정 구슬 선택 드로우 버스트가 즉시 처리된다.
- 해머가 자신의 공개 카드만 비용으로 제시하고 사용 카드 자체도 허용한다.
- 상대 비스탠드 시 해머가 비용만 처리한다.
- 상대 스탠드 시 취소·비공개 교체·카드 소유권이 정확하다.
- 해머 교체로 상대가 버스트하면 즉시 처리한다.
- 나이프 조건 `공개 합 <= 16`과 덱 잔량을 검사한다.
- 나이프 강제 히트 버스트와 비버스트 유지 정책이 맞다.

### 13.5 CU-05 — 표시·세션·런

- 카드별 사용 가능·완료·불가 사유를 정확히 표시한다.
- 효과 선택 중 일반 행동을 표시·입력 모두에서 잠근다.
- Controller가 독립·런 세션에 올바른 ID를 전달한다.
- 런 시작 전·전투 외 카드 사용을 거절한다.
- 런 카드 효과 승리·패배가 진행 상태와 지속 영혼에 한 번만 반영된다.
- 선택 중 씬 전환이나 중복 입력이 발생하지 않는다.

### 13.6 전체 회귀

- 기존 CoreLoop, StageProgression, 전투 행동 테스트가 모두 통과한다.
- 신규 테스트는 카드 ID와 고정 드로우 순서를 사용해 결정적이다.
- 비공개 값이 실패 메시지·표시 문자열에 포함되지 않는지 검사한다.

## 14. 수동 검증

`CoreLoopTest`에서 다음을 실제 화면 입력으로 확인한다.

1. 공개 자동 권총 사용 → 숫자 선택 → 성공·실패 결과
2. 비공개 카드 사용 → 즉시 공개 → 선택 중 다른 입력 잠금
3. 수정 구슬 후보 두 장과 가져오지 않음
4. 해머 비용 카드 선택과 스탠드 취소·비공개 교체
5. 나이프 강제 히트와 버스트
6. 사용 완료 카드의 재사용 방지

`StageTest`에서 다음을 확인한다.

1. 런 시작 → 전투 진입 → 카드 사용
2. 카드 효과 승리 후 다음 스테이지 진행
3. 카드 효과 패배 후 `RunDefeat`과 영혼 0
4. 재시작 후 카드 사용 상태가 새 전투 기준으로 초기화

양쪽 씬 문제 0, 게임 관련 Console Error/Warning 0을 확인한다.

## 15. 완료 불변 조건

- 한 카드 ID가 동시에 덱·손·버린 더미·효과 임시 영역 둘 이상에 존재하지 않는다.
- 하나의 전투에는 최대 하나의 보류 카드 효과만 존재한다.
- 사용 완료 카드는 다시 드로우되기 전까지 재사용되지 않는다.
- 숨은 상대 카드 값은 규칙 계층 밖으로 나오지 않는다.
- 효과로 전투가 끝나면 후속 명령과 적 행동을 실행하지 않는다.
- 실패한 API는 관찰 가능한 전투 상태를 바꾸지 않는다.
- 런 진행 계층은 카드 효과의 종류를 알지 않는다.

## 16. 예상 변경 경로

```text
Assets/01. Scripts/Runtime/CoreLoop/
Assets/01. Scripts/Runtime/StageProgression/
Assets/01. Scripts/Runtime/UI/CoreLoop/
Assets/Tests/EditMode/CoreLoop/
Assets/Tests/EditMode/StageProgression/
Docs/
```

신규 패키지와 외부 에셋은 필요하지 않다. 씬 변경은 CU-05에서 실제 UI 연결에 필요할 때만 허용하며, 현재 즉시 모드 UI 구조로 충분하면 씬 직렬화 변경을 만들지 않는다.

## 17. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 카드 정의·사용 상태·효과 선택·공개 API·진행 연결·자동 및 수동 검증을 CU-01 착수 가능한 수준으로 명세 |
| 2026-07-19 | 이천서 | CU-01 카드 카탈로그·인스턴스 상태·런 정의 키 보존 구현 결과와 전체 EditMode 101/101 검증 반영 |
| 2026-07-19 | 이천서 | CU-02 카드 사용 검증·선택 대기·효과 완료·종료 원인 기반과 전체 EditMode 117/117 검증 반영 |
| 2026-07-19 | 이천서 | CU-03 자동 권총 단일 비공개 카드 추측·성공/실패·정보 은닉 구현과 전체 EditMode 125/125 검증 반영 |
| 2026-07-19 | 이천서 | CU-04 수정 구슬 순서 보존·해머 단일 비공개 교체·나이프 강제 드로우와 정책 경계 구현, 전체 EditMode 143/143 검증 반영 |
