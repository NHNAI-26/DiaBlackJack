# 전투 행동 확장 개발 명세서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 버전: v0.2
> 상태: BA-05 이력 보존·현행 규칙 이관 완료
> 최종 갱신: 2026-07-21

> **현행 규칙 변경 안내 (2026-07-21)**
> 본문의 BA-00~BA-05 내용은 v0.1 구현 당시의 기술 기록이다. 2026-07-21에 폴드 삭제, 체인지 전투 누적 비용 `0, 1, 2, 3… 영혼`, 플레이어 스탠드 비교 패배 1 피해와 숫자·카드 효과 버스트 2 피해를 실제 코드에 이관했다. 아래 폴드 및 라운드당 1회 체인지 설명은 구현 이력 보존용이며, 현재 동작은 `Docs/combat-action-design.md` v0.2와 다음 현행 기술 기준을 따른다.

## 0. 현행 기술 기준

| 경계 | 현행 구현 |
| --- | --- |
| 행동 계약 | `Hit`, `Stand`, `Change`, 카드 사용만 유지하고 플레이어·적 `Fold` 공개 API와 결과 타입을 제거한다. |
| 비용 상태 | `CoreLoopBattle.CompletedPlayerChangeCount`가 전투 내 완료 횟수를 소유하고 `NextPlayerChangeSoulCost`가 다음 비용을 노출한다. 새 전투 인스턴스에서만 0으로 초기화한다. |
| 사용 조건 | 행동 가능 상태, 비공개 카드 정확히 1장, 후보 2장 드로우 가능, 선택 미진행과 함께 `Player.Soul.Current > NextPlayerChangeSoulCost`를 모두 만족해야 한다. |
| 비용 시점 | 모든 사전 조건이 성립한 뒤 체인지 시작 시 영혼을 차감한다. 정확히 비용만큼만 보유한 경우는 상태·카드·영혼을 바꾸지 않고 거절한다. |
| 카드 이동 | 기존 비공개 카드를 손에서 제거해 공개한 뒤 버림패로 보내고 후보 2장을 공개한다. 후보 선택 완료 시 선택 카드만 새 비공개 카드로 들어가며 미선택 후보는 버림패로 간다. |
| 표시 | `CHANGE` 버튼은 다음 비용과 사용 후 잔여 영혼을 표시한다. 공개 카드 효과명은 `REVOLVER`, `BOWIE KNIFE`를 사용한다. |
| 검증 | Unity 6000.3.10f1 전체 EditMode 306/306 통과, 실패·건너뜀 0. `GameScene` 변경 없음. |

## 1. 기술 목표

현재 `DiaBlackJack.CoreLoop`의 명시적 행동 API를 유지하면서 폴드와 2단계 체인지를 추가한다. 전투 규칙은 순수 C# 계층이 소유하고 Unity 계층과 런·스테이지 진행 계층은 입력 전달과 표시·결과 동기화만 담당한다.

## 2. 현재 기준선

현재 구현은 다음 구조를 사용한다.

- `CoreLoopBattle`: 라운드 상태, 히트·스탠드, 적 차례와 판정
- `CoreLoopSession`: 단독 전투 행동 전달과 재시작
- `BattleParticipant`: 손패, 덱, 영혼과 스탠드 상태
- `BlackjackDeck`: 뽑을 더미, 버린 더미와 재순환
- `RoundResolver`·`RoundDamageApplier`: 라운드 결과와 중복 피해 방지
- `CoreLoopPresentation`·`CoreLoopView`·`CoreLoopController`: 표시와 입력
- `StageProgressionSession`: 히트·스탠드·폴드·체인지 전달과 종료 결과 동기화

BA-01 기반 추가 후 CoreLoop 35/35, 전체 EditMode 58/58이 통과했다. BA-02 폴드 테스트 6개 추가 후 CoreLoop 41/41, 전체 64/64가 통과했다. BA-03 체인지 테스트 8개를 추가한 뒤 CoreLoop 49/49, 전체 EditMode 72/72가 통과했다. BA-04 표시·Controller 테스트 6개를 추가한 뒤 CoreLoop 55/55, 전체 EditMode 78/78이 통과했다. BA-05 착수 기준선 78/78을 확인했고 진행 통합 테스트 4개를 추가한 뒤 진행 27/27, 전체 EditMode 82/82가 통과했다.

## 3. 설계 원칙

- 기존 `TryPlayerHit`, `TryPlayerStand`와 같은 명시적 행동 메서드를 사용한다.
- 이번 범위에서 범용 `BattleAction` 명령 계층을 만들지 않는다.
- 유효하지 않은 입력은 `false`를 반환하고 상태를 변경하지 않는다.
- 카드가 여러 위치에 동시에 존재하지 않게 한다.
- 체인지 후보 선택은 하나의 진행 중 상태로 보존한다.
- 후보를 뽑은 뒤 일반 행동으로 빠져나갈 수 없게 한다.
- 라운드 종료 결과와 피해는 정확히 한 번만 적용한다.
- View와 진행 계층은 전투 규칙을 계산하지 않는다.
- 새 외부 패키지를 추가하지 않는다.

## 4. 공개 행동 API

각 계층은 기존 히트·스탠드와 같은 방식으로 다음 메서드를 전달한다.

```csharp
bool TryPlayerFold();
bool TryBeginPlayerChange();
bool TrySelectChangedCard(int candidateIndex);
```

범용 문자열·열거형 디스패처를 추가하지 않는다. 카드 사용과 계약까지 요구사항이 확정된 뒤 공통 행동 모델의 필요성을 다시 판단한다.

## 5. 상태 모델

### 5.1 `CoreLoopState` 확장

```text
Initializing
StartingRound
PlayerTurn
PlayerChoosingChangeCard
EnemyTurn
ResolvingRound
BattleEnded
```

`PlayerChoosingChangeCard`에서는 히트, 스탠드, 폴드와 체인지 재시작을 모두 거부하고 유효한 후보 선택만 허용한다.

### 5.2 상태 전이

```text
PlayerTurn --Fold--> ResolvingRound(PlayerFold)
ResolvingRound --영혼 0--> BattleEnded
ResolvingRound --전투 계속--> StartingRound

PlayerTurn --BeginChange--> PlayerChoosingChangeCard
PlayerChoosingChangeCard --Select(0|1)--> EnemyTurn
EnemyTurn --기존 규칙--> PlayerTurn / ResolvingRound
```

잘못된 후보 번호는 `PlayerChoosingChangeCard`를 유지한다.

## 6. 라운드 결과 확장

`RoundOutcome`에 `PlayerFold`를 추가한다.

```text
PlayerFold: playerDamage 1, enemyDamage 0
```

`RoundResolver`는 합계 비교와 별도로 폴드 결과를 만드는 명시적 경로를 제공한다. `CoreLoopBattle`은 폴드 결과를 기존 `RoundDamageApplier`에 전달해 라운드 번호 기반 중복 적용 방지를 그대로 사용한다.

기존 라운드 종료 정리 로직은 계산된 결과와 명시적 결과를 모두 받을 수 있게 작은 내부 메서드로 합친다. 폴드를 위해 별도의 전투 종료 복제 로직을 만들지 않는다.

### 6.1 BA-02 구현 결과

`RoundResolver.ResolvePlayerFold`가 `PlayerFold`, 플레이어 피해 1, 적 피해 0의 명시적 결과를 만든다. `CoreLoopBattle.TryPlayerFold`는 일반 플레이어 행동 조건을 재사용하고, 적 차례나 합계 비교 없이 이 결과를 공통 `CompleteRound` 경로에 전달한다.

공통 경로는 피해 적용, 양쪽 손패 정리, 전투 종료 확인과 다음 라운드 시작을 기존 판정과 동일하게 처리한다. 플레이어 영혼이 1이면 피해 후 전투가 끝나며 새 라운드를 만들지 않는다. `CoreLoopSession`은 폴드 입력을 전투에 그대로 전달한다.

`RoundOutcome` 확장 뒤 표시 계층의 분기 누락으로 예외가 발생하지 않도록 최근 결과 문자열도 함께 보완했다. 폴드 버튼과 Controller 입력 연결은 BA-04 범위로 유지한다.

## 7. 체인지 데이터와 카드 이동

### 7.1 진행 중 데이터

`CoreLoopBattle`은 체인지 진행 중에만 아래 값을 보유한다.

- 교체할 기존 비공개 카드 1장
- 후보 카드 2장
- 현재 라운드의 체인지 사용 여부

외부에는 후보를 수정할 수 없는 `IReadOnlyList<BlackjackCard>`로 제공한다. 평상시에는 빈 목록이어야 한다.

### 7.2 손패 기능

`BlackjackHand`에는 임의 인덱스 삭제 대신 도메인 목적이 명확한 비공개 카드 인출 기능을 추가한다.

```csharp
bool TryTakeSingleHiddenCard(out BlackjackCard hiddenCard);
```

비공개 카드가 정확히 한 장이 아니면 체인지 시작을 승인하지 않는다. 향후 카드 효과가 복수 비공개 카드를 허용한다면 해당 규칙과 API를 별도 수정한다.

### 7.3 덱 기능

체인지 시작 전 후보 2장을 뽑을 수 있는지 확인할 수 있게 덱의 가용 카드 수 또는 `CanDraw(int count)`를 제공한다. 가용 수는 뽑을 더미와 버린 더미의 합이며 현재 손패와 보류 중인 기존 비공개 카드는 포함하지 않는다.

후보는 기존 `Draw()`와 버린 더미 재순환 규칙을 사용한다. 기존 비공개 카드는 후보 추첨이 끝날 때까지 덱 밖에 보류해 같은 체인지에서 다시 나오지 않게 한다.

### 7.4 원자적 처리

- 사전 조건 실패: 카드 이동 없음
- 체인지 시작 성공: 기존 비공개 카드와 후보 2장이 전투의 보류 상태로 이동
- 잘못된 후보 선택: 보류 상태 유지, 카드 이동 없음
- 올바른 후보 선택: 선택 카드만 손패, 나머지 2장은 버린 더미
- 라운드·전투 재시작: 보류 상태가 남아 있지 않아야 함

실행 중 예외에 대한 범용 롤백 시스템은 만들지 않는다. 시작 전 조건 검증과 테스트로 부분 이동을 방지한다.

### 7.5 BA-01 구현 결과

BA-01에서는 `PlayerChangeSelection`을 추가해 기존 비공개 카드 1장과 후보 2장의 서로 다른 ID를 검증하고, 유효한 후보 선택을 한 번만 완료하도록 했다. 잘못된 인덱스는 선택 상태와 카드 분할 결과를 바꾸지 않는다.

`BlackjackHand.TryTakeSingleHiddenCard`는 비공개 카드가 정확히 한 장일 때만 손패에서 제거한다. 0장 또는 2장 이상이면 손패를 변경하지 않는다. `BlackjackDeck.CanDraw`는 뽑을 더미와 버린 더미의 가용 카드 수만 검사하며 실제 드로우를 발생시키지 않는다.

라운드당 체인지 사용 여부는 아직 완성 행동이 없으므로 BA-01에서 사용되지 않는 필드로 추가하지 않았다. BA-03에서 `CoreLoopBattle`의 행동 시작·완료와 `StartRound` 초기화가 함께 구현될 때 추가한다.

### 7.6 BA-03 구현 결과

`CoreLoopBattle.TryBeginPlayerChange`는 플레이어 차례, 라운드 내 미사용, 비공개 카드 정확히 1장과 후보 2장 확보 가능 여부를 모두 확인한 뒤에만 체인지를 시작한다. `BattleParticipant`는 기존 비공개 카드를 손에서 보류하고 후보 2장을 덱에서 인출해 공개하며, 전투는 `PlayerChoosingChangeCard` 상태로 전환된다.

`TrySelectChangedCard`는 유효하지 않은 번호를 받으면 보류 상태를 그대로 유지한다. 유효한 선택은 선택 후보를 비공개 손패로 넣고 기존 비공개 카드와 미선택 후보를 버린 더미로 이동한 뒤, 진행 중 선택을 제거하고 해당 라운드의 사용 여부를 완료로 표시한다. 이후 기존 적 차례를 실행해 별도 차례 처리 경로를 만들지 않는다.

체인지 선택 중에는 일반 행동이 거부되고 취소 경로를 제공하지 않는다. 같은 라운드의 재사용은 거부하며 새 라운드의 `StartRound`에서 사용 여부와 보류 참조를 초기화한다. `CoreLoopSession`은 시작과 후보 선택을 현재 전투에 전달한다. UI와 런 진행 전달은 각각 BA-04와 BA-05 범위다.

## 8. 클래스별 변경 경계

| 파일·클래스 | 변경 책임 |
| --- | --- |
| `CoreLoopState.cs` | 체인지 선택 상태 추가 |
| `RoundResolver.cs` | `PlayerFold` 결과와 피해 1 정의 |
| `BlackjackHand.cs` | 비공개 카드 확인·인출 기능 |
| `BlackjackDeck.cs` | 후보 2장 확보 가능 여부 제공 |
| `PlayerChangeSelection.cs` | 기존 비공개 카드와 후보 2장의 불변 조건·단일 선택 결과 보존 |
| `BattleParticipant.cs` | 체인지용 카드 이동과 라운드 제한 초기화 보조 |
| `CoreLoopBattle.cs` | 행동 승인, 상태 전이, 후보와 폴드 판정 소유 |
| `CoreLoopSession.cs` | 행동 3종 전달 |
| `CoreLoopPresentation.cs` | 폴드·체인지 가능 여부, 후보, 결과 문구 |
| `CoreLoopView.cs` | 행동 버튼과 후보 선택 입력 |
| `CoreLoopController.cs` | 단독·런 전투로 행동 전달 |
| `StageProgressionSession.cs` | 행동 전달 후 기존 종료 동기화 실행 |

새 런 상태, 새 씬 또는 새 패키지는 필요하지 않다.

## 9. 행동 가능 조건

### 9.1 전투 모델

`CoreLoopBattle`이 다음 읽기 전용 값을 제공한다.

- `CanPlayerFold`
- `CanBeginPlayerChange`
- `CanSelectChangedCard`
- `HasPlayerChangedThisRound`
- `PlayerChangeCandidates`

가능 여부는 View에서 재계산하지 않는다.

### 9.2 표시 모델

`CoreLoopViewModel`에 다음 값을 추가한다.

- `CanFold`
- `CanChange`
- `IsChoosingChangeCard`
- 후보 0·1의 표시 문자열 또는 읽기 전용 후보 목록
- 폴드 시 예상 영혼 결과 문구

후보는 플레이어 표시 모델에만 포함한다. 적 카드 표시와 적 정책 입력에는 포함하지 않는다.

### 9.3 BA-04 구현 결과

`CoreLoopBattle.CanPlayerFold`와 기존 체인지 읽기 값을 Presenter가 사용해 `CanFold`, `CanChange`, `IsChoosingChangeCard`와 후보 목록을 만든다. 폴드는 항상 영혼 1 비용을 표시하고 현재 영혼이 1이면 패배 경고를 함께 보여 준다. 체인지는 라운드당 1회와 사용 완료 상태를 구분한다.

`CoreLoopView`는 평상시 히트·스탠드와 폴드·체인지를 두 행으로 배치한다. 체인지 선택 중에는 일반 행동을 그리지 않고 후보 2개만 표시하며, 각 입력은 이벤트로 Controller에 전달한다. `CoreLoopController`는 단독 전투에서는 `CoreLoopSession`, 현재 런 전투에서는 전투 인스턴스로 입력을 전달하고 매 입력 뒤 표시 모델을 갱신한다. EditMode 테스트에서는 Unity 예약 호출을 사용하지 않고 입력 잠금을 즉시 해제하며, 실제 플레이 모드의 다음 프레임 해제 동작은 유지한다.

BA-04 시점에는 `StageProgressionSession`의 행동 전달·종료 동기화를 추가하지 않았다. 따라서 당시 런 전투 Controller 입력 자체는 동작했지만 폴드로 전투가 끝날 때 런 패배와 지속 영혼을 동기화하는 책임은 BA-05로 남겼다. 새 씬·패키지는 추가하지 않았다.

## 10. 세션과 진행 시스템 연결

`CoreLoopSession`과 `StageProgressionSession`은 세 행동을 그대로 전달한다. `StageProgressionSession`은 승인된 폴드 또는 체인지 선택 뒤 기존 `SynchronizeFinishedBattle()`을 호출한다.

- 폴드로 플레이어 영혼이 0: `RunDefeat`로 동기화
- 폴드 후 전투 계속: `InBattle` 유지
- 체인지 시작·완료: 전투가 끝나지 않았다면 `InBattle` 유지
- 향후 적 행동 결과로 전투 종료: 기존 승리·패배 처리

진행 계층에 폴드 피해량, 후보 카드 또는 체인지 제한을 저장하지 않는다.

### 10.1 BA-05 구현 결과

`StageProgressionSession`에 `TryPlayerFold`, `TryBeginPlayerChange`, `TrySelectChangedCard`를 추가했다. 각 메서드는 런 상태와 전투 세션을 먼저 확인하고 `CoreLoopSession`에 행동을 전달한 뒤 기존 `SynchronizeFinishedBattle()`을 호출한다. 진행 계층은 전투 규칙을 복제하지 않으며, 전투가 끝난 경우에만 전투 영혼을 지속 상태로 옮기고 스테이지 완료 또는 런 패배를 적용한다.

`CoreLoopController`는 런 전투에서 `CoreLoopBattle`을 직접 호출하지 않고 세 행동 모두 `StageProgressionSession`을 경유한다. 비치명 폴드는 전투 영혼만 감소한 채 `InBattle`을 유지하고, 영혼 0 폴드는 `RunDefeat`와 지속 영혼 0으로 동기화한다. 체인지 완료 후 적 버스트로 전투가 끝나는 경우도 `StageCleared`로 동기화한다. 런 시작 전과 종료 후 행동은 상태 변경 없이 거부한다.

진행 통합 테스트 4개를 추가해 비치명 폴드, 폴드 패배, 런 체인지 유지와 체인지 중 스테이지 클리어를 검증했다. 실제 Game View에서도 런 체인지, 영혼 1 폴드 경고, 진행 화면의 `RUN DEFEAT`·영혼 0과 재시작 후 스테이지 0·영혼 12·새 전투를 확인했다. 새 씬·패키지·외부 에셋은 추가하지 않았다.

## 11. UI 입력 흐름

### 11.1 평상시

Controller는 기존 입력 잠금을 적용한 뒤 현재 세션의 명시적 메서드를 호출한다. `false` 반환 시 즉시 잠금을 풀고 표시를 새로 그린다.

### 11.2 체인지 선택

1. `CHANGE` 입력을 전달한다.
2. 성공하면 상태와 후보를 즉시 다시 표시한다.
3. 다음 프레임에 잠금을 풀되 일반 행동은 모델의 가능 여부로 비활성화한다.
4. 후보 버튼 입력을 `TrySelectChangedCard(index)`로 전달한다.
5. 성공하면 적 차례 처리까지 끝난 결과를 표시한다.

체인지 시작과 선택 사이에 씬을 전환하지 않는다.

## 12. 자동 테스트 명세

### 12.1 규칙·상태 테스트

| ID | 검증 내용 | 기대 결과 |
| --- | --- | --- |
| BA-U01 | 플레이어 차례 폴드 | `PlayerFold`, 영혼 -1, 라운드 종료 |
| BA-U02 | 영혼 1에서 폴드 | 전투 `PlayerDefeat` |
| BA-U03 | 폴드 후 카드 위치 | 양측 손패 0, 각 덱의 버린 더미 증가 |
| BA-U04 | 폴드 중복 입력 | 추가 피해·추가 라운드 없음 |
| BA-U05 | 잘못된 상태의 폴드 | `false`, 상태 변화 없음 |
| BA-U06 | 체인지 시작 | 선택 상태, 후보 2장, 일반 행동 불가 |
| BA-U07 | 체인지 후보 선택 | 선택 카드가 비공개 손패, 나머지 카드 폐기 |
| BA-U08 | 잘못된 후보 번호 | 상태·손패·덱·버린 더미 변화 없음 |
| BA-U09 | 같은 라운드 재사용 | 거부, 상태 변화 없음 |
| BA-U10 | 다음 라운드 체인지 | 제한 초기화 후 다시 사용 가능 |
| BA-U11 | 후보 부족 | 시작 거부, 카드 이동 없음 |
| BA-U12 | 기존 비공개 카드 재등장 | 같은 체인지 후보에서 제외 |
| BA-U13 | 체인지 완료 후 차례 | 적 행동 후 정상 상태 전이 |
| BA-U14 | 후보 정보 비공개 | 적 표시·정책 입력에 후보 없음 |

### 12.2 표시·통합 테스트

| ID | 검증 내용 | 기대 결과 |
| --- | --- | --- |
| BA-P01 | 플레이어 차례 표시 | Fold·Change 가능 상태 정확 |
| BA-P02 | 선택 상태 표시 | 후보 2개만 선택 가능 |
| BA-P03 | 사용 후 표시 | Change 비활성화, 후보 제거 |
| BA-P04 | 폴드 최근 결과 | 영혼 -1 문구 표시 |
| BA-I01 | 단독 전투 폴드·체인지 | 세션과 Controller 전달 성공 |
| BA-I02 | 런 전투 폴드 | 영혼 유지 또는 `RunDefeat` 동기화 |
| BA-I03 | 런 전투 체인지 | 진행 상태 유지, 전투 상태 정상 |
| BA-I04 | 기존 히트·스탠드 | 동작과 표시 회귀 없음 |
| BA-I05 | 전투·런 재시작 | 후보·제한·결과가 초기화됨 |

## 13. 수동 검증

- `CoreLoopTest`에서 네 행동 버튼의 활성 상태를 확인한다.
- 폴드 후 영혼 1 감소와 새 라운드 시작을 확인한다.
- 영혼 1 폴드에서 패배 화면으로 전환되는지 확인한다.
- 체인지 후보 2개가 보이고 일반 행동이 잠기는지 확인한다.
- 선택 카드가 새 비공개 카드로 유지되고 같은 라운드의 Change가 비활성화되는지 확인한다.
- `StageTest`에서 진입한 전투에서도 같은 행동과 영혼 동기화를 확인한다.
- 승리·패배 후 재시작해 후보·사용 상태가 남지 않는지 확인한다.
- Unity Console의 게임 관련 Error·Exception이 0인지 확인한다.

## 14. 완료 불변 조건

- 모든 카드는 뽑을 더미, 버린 더미, 손패, 체인지 보류 영역 중 정확히 한 곳에만 있다.
- 전투 전체 카드 수는 행동 전후 동일하다.
- 체인지 선택 상태에는 기존 비공개 1장과 후보 2장이 존재한다.
- 체인지 완료 후 보류 영역은 비어 있다.
- 라운드당 체인지 완료 횟수는 0 또는 1이다.
- 폴드 결과 하나당 플레이어 피해는 정확히 1이다.
- 진행 시스템의 플레이어 영혼은 전투 종료 시 전투 값과 같다.

## 15. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 현재 전투 구조를 기준으로 폴드·2단계 체인지 API, 상태, 카드 이동, UI·진행 연결과 테스트 명세 작성 |
| 2026-07-19 | 이천서 | BA-01 선택 상태·손패 인출·덱 가용 수·후보 분할 기반과 전체 EditMode 58/58 검증 결과 반영 |
| 2026-07-19 | 이천서 | BA-02 폴드 결과·영혼 피해·공통 라운드 종료·세션 전달과 CoreLoop 41/41·전체 64/64 결과 반영 |
| 2026-07-19 | 이천서 | BA-03 체인지 보류·후보 공개·선택 완료·라운드 제한과 CoreLoop 49/49·전체 72/72 결과 반영 |
| 2026-07-19 | 이천서 | BA-04 표시 모델·네 행동 버튼·후보 전용 입력·Controller 전달과 CoreLoop 55/55·전체 78/78·Game View 검증 결과 반영 |
| 2026-07-19 | 이천서 | BA-05 진행 세션의 행동 전달·전투 종료와 영혼 동기화, 진행 27/27·전체 82/82·실제 패배·재시작 검증 결과 반영 |
| 2026-07-21 | 이천서 | 폴드 계약 제거, 체인지 전투 누적 비용·엄격한 영혼 조건·공개 폐기·표시·세션·적 AI 이관과 전체 EditMode 306/306 결과 반영 |
