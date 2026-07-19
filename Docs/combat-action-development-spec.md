# 전투 행동 확장 개발 명세서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 버전: v0.1  
> 상태: BA-01 기반 구현 완료 기준
> 최종 갱신: 2026-07-19

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
- `StageProgressionSession`: 런 전투 행동 전달과 종료 결과 동기화

BA-01 착수 직전 전체 EditMode 50/50 통과를 다시 확인했다. BA-01 기반 테스트 8개를 추가한 뒤 CoreLoop 35/35, 전체 EditMode 58/58이 통과했다.

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

## 10. 세션과 진행 시스템 연결

`CoreLoopSession`과 `StageProgressionSession`은 세 행동을 그대로 전달한다. `StageProgressionSession`은 승인된 폴드 또는 체인지 선택 뒤 기존 `SynchronizeFinishedBattle()`을 호출한다.

- 폴드로 플레이어 영혼이 0: `RunDefeat`로 동기화
- 폴드 후 전투 계속: `InBattle` 유지
- 체인지 시작·완료: 전투가 끝나지 않았다면 `InBattle` 유지
- 향후 적 행동 결과로 전투 종료: 기존 승리·패배 처리

진행 계층에 폴드 피해량, 후보 카드 또는 체인지 제한을 저장하지 않는다.

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
