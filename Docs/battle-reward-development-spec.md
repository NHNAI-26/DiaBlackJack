# 전투 보상 시스템 개발 명세서

> 프로젝트: DiaBlackJack
> 기획·개발 책임자: 이천서
> 버전: v0.1
> 상태: RW-00~RW-05 구현·검증 완료
> 최종 갱신: 2026-07-20

## 1. 기술 목표

일반·엘리트·최종 보스 전투 승리 뒤 카드 후보 3장을 생성하고, 플레이어가 한 장을 현재 런 덱에 추가하거나 보상을 건너뛴 다음 진행을 확정하는 순수 C# 기반 시스템을 구현한다.

일반·엘리트 전투는 보상 완료 뒤 `StageCleared`, 최종 보스는 보상 완료 뒤 `RunVictory`가 된다. 패배에는 보상을 생성하지 않는다. 기존 코어 루프와 카드 사용 시스템은 전투 승패까지만 책임지고, 전투 보상은 `StageProgression` 영역에서 승리 결과와 다음 진행 사이를 중재한다.

## 2. 현재 코드 기준

2026-07-20 RW-00 문서 작성 시점의 구조는 다음과 같다.

- `StageProgressionState`에는 `NotStarted`, `InBattle`, `StageCleared`, `RunVictory`, `RunDefeat`만 있다.
- `RunProgress.TryCompleteCurrentStage()`는 일반 승리를 즉시 `StageCleared`, 최종 보스 승리를 즉시 `RunVictory`로 바꾼다.
- `PlayerRunState.Deck`은 생성 시 복사한 읽기 전용 목록이며 런 중 카드를 추가할 API가 없다.
- `PlayerRunState.ResetForNewRun()`은 영혼만 복구하고 덱은 복구하지 않는다.
- `RunCardDefinition`은 고유 정수 ID와 안정적인 카드 정의 키를 가진다.
- `CardDefinitionCatalog`는 현재 카드 정의 10개를 키로 조회할 수 있다.
- `StageProgressionSession`은 전투 결과의 영혼을 동기화한 뒤 `RunProgress`에 승리·패배를 전달한다.
- `StageTest`의 `StageProgressionRuntime`, `StageProgressionController`와 즉시 모드 UI가 진행 상태를 표시한다.
- 현행 `StageKind`는 일반 전투와 최종 보스만 구분한다. 엘리트 여부는 이후 상대 선택 시스템에서 보상 등급으로 전달해야 한다.

따라서 RW 구현은 즉시 완료 전이를 보상 대기 전이로 바꾸고, 런 덱의 현재본·초기본을 분리하는 변경을 포함한다.

### 2.1 RW-01 구현 결과

2026-07-20에 진행 상태와 UI를 바꾸지 않는 보상 기반을 구현했다.

- `BattleRewardTier`: 일반·높은 등급 구분
- `BattleRewardCatalog`: 명시적 정의 키 검증과 풀별 읽기 전용 조회
- `BattleRewardOption`, `BattleRewardOffer`: 정의 키가 중복되지 않는 불변 3장 제안
- `BattleRewardGenerator`: 같은 시드·요청 순서에서 같은 후보와 증가하는 제안 ID 생성
- `PlayerRunState`: 최초 덱 스냅샷, 현재 런 덱, 고유 증가 카드 ID와 재시작 복구
- `AssemblyInfo.cs`: StageProgression EditMode 테스트에 내부 덱 변경 경계만 공개
- `BattleRewardFoundationTests`: RW01-U01~RW01-U08 구현

`StageProgressionState`, `RunProgress`의 승리 전이, `StageProgressionSession`과 UI는 변경하지 않았다. 실제 보상 시작·선택·건너뛰기는 RW-02 범위다.

### 2.2 RW-02 구현 결과

2026-07-20에 승리 이후의 순수 보상 진행 상태와 처리 경계를 구현했다.

- `StageProgressionState.RewardSelection`: 보상 해결 전 진행 입력 잠금
- `BattleRewardCompletionTarget`: 일반 완료와 최종 보스 런 승리 목적지 구분
- `PendingBattleReward`: 현재 제안과 완료 목적지를 함께 보존
- `BattleRewardResolution`: 제안·선택 옵션·추가 카드·건너뛰기와 완료 목적지 기록
- `RunProgress`: 보상 시작, 선택, 건너뛰기의 공개 `Try` API와 원자적 실패 처리
- 최종 보스: 높은 등급 제안과 `RunVictory` 목적지만 승인
- 재시작: 보류 제안, 최근 해결 결과와 RW-01에서 추가된 카드를 모두 초기화
- `BattleRewardStateTests`: RW02-U01~RW02-U07 구현

기존 즉시 완료 API는 공개 경계에서 제거했다. 다만 실제 전투 승리에서 제안을 만드는 책임은 RW-03이므로, 현재 `StageProgressionSession`만 내부 `TryCompleteCurrentStageWithoutReward()` 임시 호환 경로를 사용한다. 따라서 RW-02의 순수 상태 API는 보상을 강제하지만 실제 씬 승리는 아직 자동으로 `RewardSelection`에 진입하지 않는다.

### 2.3 RW-03 구현 결과

2026-07-20에 실제 전투 결과와 보상 처리, 다음 전투 덱 전달을 통합했다.

- `StageProgressionSession`: 승리 시 전투 영혼 동기화 뒤 제안을 한 번 생성하고 `RewardSelection`으로 전환
- 생성기 주입: 세션 수명 동안 하나의 `BattleRewardGenerator`를 사용해 제안 순서와 ID를 유지
- 등급 주입: 일반 스테이지는 기본 `Normal`, 미래 엘리트는 `Func<StageDefinition, BattleRewardTier>`로 `HighGrade`를 전달
- 보스 강제 규칙: 등급 주입과 무관하게 최종 보스는 `HighGrade`와 `RunVictory` 목적지 사용
- 세션 공개 API: 보상 선택과 건너뛰기를 `RunProgress`에 전달
- 다음 전투: 보상 해결 뒤 현재 `PlayerRunState.Deck`으로 새 전투를 생성
- 재시작: `RunProgress`가 최초 덱을 복구한 뒤 새 전투를 생성
- `BattleRewardSessionTests`: RW03-I01~RW03-I06 구현

RW-02의 내부 `TryCompleteCurrentStageWithoutReward()` 호환 경로는 제거했다. 이제 실제 세션의 모든 승리는 보상 대기를 거치며, 패배만 보상 없이 `RunDefeat`로 이동한다. 보상 후보 정보와 선택·건너뛰기 화면 입력은 RW-04 범위다.

### 2.4 RW-04 구현 결과

2026-07-20에 기존 `StageTest` 진행 화면에서 보상 후보를 확인하고 처리하는 표시·입력 계층을 구현했다.

- `BattleRewardOptionViewModel`: 옵션 ID, 정의 키, 표시 이름, 숫자, 등급과 효과 요약 제공
- `StageProgressionViewModel`: 보상 등급, 후보 3장, 선택·건너뛰기 가능 여부, 완료 목적지, 최근 결과와 현재 덱 장수 제공
- `StageProgressionPresenter`: 보류 제안을 `CardDefinitionCatalog`로 해석하며 보상 규칙이나 상대 정보를 재계산하지 않음
- `StageProgressionView`: 후보별 `SELECT`, 공통 `SKIP REWARD`, 보상 등급·완료 목적지·최근 처리 결과 표시
- `StageProgressionController`: 옵션 ID 또는 건너뛰기만 세션 API에 한 번 전달하고 즉시 모델을 갱신
- 일반 보상 완료 뒤 같은 화면에서 결과를 확인하고 `NEXT STAGE`, 보스 보상 완료 뒤 `RUN VICTORY`와 재시작 표시
- `BattleRewardPresentationTests`: RW04-P01~RW04-P05 구현

별도 보상 씬, 프리팹, 패키지와 외부 에셋은 추가하지 않았다. 첫 컴파일에서 테스트 정리 코드의 `Object` 이름 충돌 2건이 있었고 `UnityEngine.Object`로 명시해 해결했다.

### 2.5 RW-05 구현 결과

2026-07-20에 신규 기능이나 밸런스 변경 없이 완료된 공개 API의 반복 회귀와 실제 흐름을 검증해 1차 범위를 마감했다.

- `BattleRewardSystemValidationTests`: 일반 선택, 일반 건너뛰기, 보스 선택, 보스 건너뛰기와 패배·재시작을 각각 10회 반복
- 제안마다 옵션 ID와 정의 키 3개의 고유성, 한 세션의 제안 ID 고유성과 런 카드 ID 고유성을 확인
- 일반 선택 카드는 실제 다음 전투 덱에 포함되고 건너뛰기는 덱을 바꾸지 않음을 확인
- 보스 보상은 항상 `HighGrade`와 `RunVictory` 목적지를 사용하고, 선택·건너뛰기 뒤 재시작하면 최초 덱으로 복구됨을 확인
- 패배에는 보류 보상과 해결 결과가 생기지 않으며 재시작 뒤 영혼·스테이지·덱·보상 상태가 초기화됨을 확인
- 신규 5/5, CoreLoop 122/122, StageProgression 65/65, 카드 사용 검증 5/5와 전체 EditMode 187/187 통과
- `StageTest`와 `CoreLoopTest` 씬 문제 0, 실제 일반·보스·패배 흐름과 시각 판정 98/100, 최종 Console Error/Warning 0 확인

런타임, 씬, 프리팹, 패키지와 외부 에셋은 변경하지 않았다. RW-05의 제품 변경은 반복 회귀 테스트와 검증·기여 문서뿐이다.

## 3. 설계 원칙

1. 보상 규칙은 Unity 객체와 분리된 순수 C#로 구현한다.
2. 보상 후보는 카드 인스턴스가 아니라 카드 정의 키를 가진다.
3. 카드 인스턴스 ID는 선택 승인 순간에만 발급한다.
4. 보상 생성은 주입한 난수 또는 시드로 재현할 수 있어야 한다.
5. UI는 규칙을 다시 계산하지 않고 읽기 전용 표시 모델만 사용한다.
6. 잘못된 입력은 상태·덱·영혼을 부분 변경하지 않는다.
7. 일반 완료 목적과 최종 보스 완료 목적을 보류 보상에 저장한다.
8. 보상 후보는 한 전투 승리에 대해 한 번만 생성한다.

## 4. 데이터 모델

### 4.1 `BattleRewardTier`

```text
Normal
HighGrade
```

- 일반 적 승리: `Normal`
- 엘리트 승리: `HighGrade`
- 최종 보스 승리: `HighGrade`

현재 코드에는 엘리트 전투 타입이 없으므로 RW 단계에서는 세션이 보상 등급을 명시적으로 받는 경계를 제공한다. 최종 보스는 스테이지 종류로 `HighGrade`를 결정할 수 있다.

### 4.2 `BattleRewardCompletionTarget`

```text
StageCleared
RunVictory
```

보상 시작 시 확정하여 선택·건너뛰기 완료 뒤의 목적지를 바꿀 수 없게 한다.

### 4.3 `BattleRewardCatalog`

- 일반 풀과 높은 등급 풀을 카드 정의 키 목록으로 보유한다.
- 기본 카탈로그의 키 목록은 `battle-reward-design.md` 6장을 따른다.
- 생성 시 모든 키가 `CardDefinitionCatalog`에 존재하는지 검증한다.
- 각 풀에는 서로 다른 유효 정의가 최소 3개 있어야 한다.
- 숫자나 표시 이름으로 등급을 추론하지 않는다.

### 4.4 `BattleRewardOption`

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `OptionId` | `int` | 한 제안 안에서 사용하는 0~2 선택 ID |
| `DefinitionKey` | `string` | 카드 정의 카탈로그 키 |

표시 이름·숫자·효과 설명은 `DefinitionKey`로 카탈로그에서 읽는다. 후보 단계에서는 런 카드 ID를 만들지 않는다.

### 4.5 `BattleRewardOffer`

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `OfferId` | `int` 또는 불변 값 | 중복 처리 방지용 제안 식별자 |
| `Tier` | `BattleRewardTier` | 사용한 보상 풀 |
| `Options` | 읽기 전용 3개 | 서로 다른 카드 정의 후보 |

생성자는 후보 수 3개, 선택 ID 중복 없음, 정의 키 중복 없음과 빈 키 금지를 검증한다.

### 4.6 `PendingBattleReward`

`BattleRewardOffer`와 `BattleRewardCompletionTarget`을 묶어 `RunProgress`가 보유한다. 보상 완료 시 제거하며 완료 후 재사용하지 않는다.

### 4.7 `BattleRewardResolution`

최근 완료 결과 표시를 위해 다음 정보만 남긴다.

- 선택 또는 건너뛰기 여부
- 선택한 정의 키와 새 런 카드 ID(선택 시)
- 완료 목적지

보상 후보 전체를 영구 기록하거나 다음 런으로 넘기지 않는다.

## 5. 보상 생성 규칙

`BattleRewardGenerator`는 카탈로그와 결정적 난수 공급자를 받는다.

1. 요청한 등급의 유효한 정의 목록을 읽는다.
2. 중복 없이 3개를 선택한다.
3. 뽑힌 순서대로 `OptionId` 0, 1, 2를 부여한다.
4. 새 `OfferId`를 부여하고 불변 제안을 반환한다.

같은 시드·같은 카탈로그·같은 요청 순서는 같은 결과를 만들어야 한다. 후보가 3개 미만이거나 등록되지 않은 키가 있으면 제안을 만들지 않고 명시적으로 실패한다. 다른 풀로 대체하거나 중복 후보를 채우지 않는다.

가중치, 재추첨, 천장과 덱 보유 카드 기반 제외는 RW 범위에 포함하지 않는다.

## 6. 진행 상태와 전이

`StageProgressionState`에 `RewardSelection`을 추가한다.

```text
일반·엘리트 승리
InBattle -> RewardSelection -> StageCleared

최종 보스 승리
InBattle -> RewardSelection -> RunVictory

패배
InBattle -> RunDefeat
```

`RewardSelection`에서는 다음 스테이지, 런 재시작, 전투 행동과 추가 보상 시작을 모두 거절한다.

기존 `TryCompleteCurrentStage()`의 즉시 완료 의미는 제거하거나 내부 승리 접수 경계로 축소한다. 공개 API 이름이 완료를 암시하여 혼동을 만들면 `TryBeginBattleReward(...)`로 교체하고 기존 테스트와 호출부를 함께 마이그레이션한다.

## 7. 공개 API 기준

구체적인 이름은 코드 규칙에 맞춰 조정할 수 있으나 다음 책임은 유지한다.

### 7.1 `RunProgress`

```csharp
bool TryBeginBattleReward(
    BattleRewardOffer offer,
    BattleRewardCompletionTarget completionTarget);

bool TrySelectBattleReward(int optionId);
bool TrySkipBattleReward();
```

- `TryBeginBattleReward`: 살아 있는 플레이어의 `InBattle`에서만 보류 보상을 저장하고 `RewardSelection`으로 전환한다.
- `TrySelectBattleReward`: 현재 제안의 후보만 승인하고 카드 한 장을 덱에 추가한 뒤 목적 상태로 이동한다.
- `TrySkipBattleReward`: 덱을 바꾸지 않고 목적 상태로 이동한다.
- 모든 `Try` 실패는 원자적이어야 하며 `false`를 반환한다.

### 7.2 `PlayerRunState`

```csharp
internal RunCardDefinition AddRewardCard(string definitionKey);
```

- 카탈로그에 존재하는 정의 키만 승인한다.
- 같은 정의 키의 기존 카드가 있어도 추가할 수 있다.
- 물리 카드 ID는 현재 런에서 이미 발급한 최대값보다 큰 값으로 만든다.
- 동일 런에서 삭제 기능이 생겨도 발급 ID를 재사용하지 않도록 다음 ID 상한을 별도로 가진다.

### 7.3 `StageProgressionSession`

세션은 전투 승리가 확정되면 다음을 한 번에 수행한다.

1. 전투의 지속 영혼을 `PlayerRunState`에 동기화한다.
2. 현재 전투가 최종 보스인지, 전달된 상대 등급이 엘리트인지 판정한다.
3. 알맞은 보상 등급과 완료 목적지를 결정한다.
4. 후보를 한 번 생성한다.
5. `RunProgress.TryBeginBattleReward`를 호출한다.

선택·건너뛰기 API도 `StageProgressionSession`이 전달한다. 보상이 완료될 때 전투 결과를 다시 적용하거나 후보를 다시 생성하지 않는다.

## 8. 런 덱과 재시작

`PlayerRunState`는 다음 두 덱 개념을 분리한다.

- 최초 덱 스냅샷: 생성 시 복사한 새 런 기준 덱
- 현재 런 덱: 보상 카드가 추가되는 가변 목록의 읽기 전용 노출

`ResetForNewRun()`은 영혼을 최대치로 복구하고 현재 런 덱을 최초 덱 스냅샷으로 다시 복사한다. 다음 카드 ID도 최초 덱의 최대 ID 다음 값으로 초기화한다. 이전 런의 보류 보상, 완료 결과와 획득 카드는 남기지 않는다.

외부에는 계속 `IReadOnlyList<RunCardDefinition>`만 노출하여 임의 변경을 막는다.

## 9. 표시 모델과 입력

`StageProgressionViewModel` 또는 보상 전용 읽기 모델은 `RewardSelection`에서 다음을 제공한다.

- 승리한 전투 또는 적 표시명
- `Normal`/`HighGrade` 표시 문구
- 후보별 선택 ID, 정의 키, 카드 이름, 숫자, 효과 요약
- `CanSelectReward`, `CanSkipReward`
- 보상 뒤 목적지가 스테이지 완료인지 런 승리인지 설명하는 문구
- 최근 선택 결과와 현재 덱 장수

`StageProgressionView`는 후보마다 `SELECT` 버튼을 만들고 `SKIP REWARD`를 제공한다. Controller는 옵션 ID만 세션에 전달한다. View와 Controller가 카드를 직접 덱에 넣거나 진행 상태를 바꾸지 않는다.

RW-04까지는 기존 `StageTest` 즉시 모드 UI를 재사용한다. 별도 보상 씬, 프리팹, 애니메이션과 사운드는 만들지 않는다.

## 10. 실패와 원자성 규칙

- 전투 중·스테이지 완료·런 결과 상태에서 선택 또는 건너뛰기를 거절한다.
- 패배한 플레이어는 보상을 시작할 수 없다.
- 보상 시작 실패 시 상태와 보류 보상은 그대로다.
- 잘못된 옵션 ID 선택 시 덱, 다음 카드 ID, 상태와 보류 보상을 바꾸지 않는다.
- 선택 카드 생성이 실패하면 진행 상태를 완료하지 않는다.
- 선택 성공 시 카드 추가, 보류 제거와 상태 전이는 하나의 승인 작업으로 처리한다.
- 건너뛰기 성공 시 덱과 다음 카드 ID를 바꾸지 않는다.
- 성공한 제안을 두 번 처리할 수 없다.
- 보상 중 전투 결과 동기화를 다시 호출해도 두 번째 보상을 만들지 않는다.

## 11. 테스트 명세

### RW-01 데이터·생성·덱

| ID | 검증 내용 |
| --- | --- |
| RW01-U01 | 기본 카탈로그의 일반·높은 등급 키가 모두 유효하다 |
| RW01-U02 | 일반 풀에서 서로 다른 후보 3개를 만든다 |
| RW01-U03 | 높은 등급 풀에서 서로 다른 후보 3개만 만든다 |
| RW01-U04 | 같은 시드와 호출 순서는 같은 후보 순서를 만든다 |
| RW01-U05 | 3개 미만·중복·미등록 키 카탈로그를 거절한다 |
| RW01-U06 | 선택 정의를 고유 증가 ID로 현재 덱에 추가한다 |
| RW01-U07 | 같은 정의 키를 추가해도 물리 ID가 다르다 |
| RW01-U08 | 재시작 시 최초 덱과 다음 ID가 복구된다 |

### RW-02 진행 상태

| ID | 검증 내용 |
| --- | --- |
| RW02-U01 | 일반 승리는 `RewardSelection`으로 이동하며 즉시 `StageCleared`가 아니다 |
| RW02-U02 | 최종 보스 승리는 `RewardSelection`으로 이동하며 즉시 `RunVictory`가 아니다 |
| RW02-U03 | 일반 보상 선택·건너뛰기 뒤 `StageCleared`가 된다 |
| RW02-U04 | 보스 보상 선택·건너뛰기 뒤 `RunVictory`가 된다 |
| RW02-U05 | 패배에는 보상이 생기지 않고 `RunDefeat`가 된다 |
| RW02-U06 | 잘못된·중복 입력은 상태와 덱을 바꾸지 않는다 |
| RW02-U07 | 보상 중 다음 진행·재시작·새 보상 시작을 거절한다 |

### RW-03 세션 통합

| ID | 검증 내용 |
| --- | --- |
| RW03-I01 | 전투 승리의 영혼 동기화 뒤 일반 보상을 한 번 생성한다 |
| RW03-I02 | 최종 보스는 높은 등급 보상과 `RunVictory` 목적지를 사용한다 |
| RW03-I03 | 미래 엘리트 입력은 높은 등급 보상을 사용한다 |
| RW03-I04 | 선택 카드가 다음 일반 전투 덱에 포함된다 |
| RW03-I05 | 건너뛰면 다음 전투 덱 수가 변하지 않는다 |
| RW03-I06 | 런 재시작 뒤 이전 획득 카드가 새 전투에 포함되지 않는다 |

### RW-04 표시·입력

| ID | 검증 내용 |
| --- | --- |
| RW04-P01 | 표시 모델은 후보 3개의 이름·숫자·효과와 선택 ID를 제공한다 |
| RW04-P02 | 보상 중 전투·다음·재시작 입력이 비활성화된다 |
| RW04-P03 | 선택과 건너뛰기 입력이 세션 API로 한 번 전달된다 |
| RW04-P04 | 보스 보상은 완료 뒤 런 승리임을 표시한다 |
| RW04-P05 | 상대 비공개 카드 정보가 보상 모델에 유출되지 않는다 |

### RW-05 반복·전체 회귀

- 일반 선택, 일반 건너뛰기, 보스 선택, 보스 건너뛰기와 패배 흐름을 각각 10회 반복한다.
- 후보 3개 고유성, 카드 ID 고유성, 덱 소유권, 다음 전투 반영과 재시작 초기화를 확인한다.
- CoreLoop, StageProgression, 카드 사용 검증과 전체 EditMode를 모두 통과한다.
- `CoreLoopTest`와 `StageTest` 씬 문제 0, Console Error/Warning 0을 확인한다.

## 12. 수동 검증 시나리오

1. `StageTest`에서 일반 전투를 승리한다.
2. 결과가 즉시 다음 진행으로 넘어가지 않고 후보 3장을 표시하는지 확인한다.
3. 한 장을 선택하고 덱 수 증가와 `StageCleared` 전환을 확인한다.
4. 다음 전투에서 획득 카드가 실제 덱에 포함되는지 확인한다.
5. 별도 실행에서 보상을 건너뛰고 덱 수가 유지되는지 확인한다.
6. 최종 보스를 승리하여 높은 등급 후보 3장을 확인한다.
7. 선택 또는 건너뛰기 전에는 런 승리가 아니고, 처리 뒤 `RunVictory`가 되는지 확인한다.
8. 런을 재시작하여 최초 덱과 첫 전투가 복구되는지 확인한다.

## 13. 완료 불변조건

- 한 승리에는 보상 제안 하나만 존재한다.
- 한 제안에는 서로 다른 정의 3개만 존재한다.
- 선택 성공은 덱 카드를 정확히 하나 늘린다.
- 건너뛰기는 덱을 바꾸지 않는다.
- 일반·엘리트와 최종 보스의 완료 목적지가 뒤바뀌지 않는다.
- 최종 보스도 보상 완료 전에는 `RunVictory`가 아니다.
- 패배에는 보상이 없다.
- 새 런은 최초 덱에서 시작한다.
- UI는 보상 규칙이나 비공개 전투 정보를 다시 계산하지 않는다.

## 14. 예상 파일 경계

```text
Assets/01. Scripts/Runtime/StageProgression/
├─ BattleRewardTier.cs
├─ BattleRewardCompletionTarget.cs
├─ BattleRewardCatalog.cs
├─ BattleRewardOption.cs
├─ BattleRewardOffer.cs
├─ BattleRewardGenerator.cs
├─ PendingBattleReward.cs
├─ BattleRewardResolution.cs
├─ PlayerRunState.cs
├─ RunProgress.cs
├─ StageProgressionState.cs
└─ StageProgressionSession.cs

Assets/01. Scripts/Runtime/UI/StageProgression/
├─ StageProgressionPresenter.cs
├─ StageProgressionViewModel.cs
├─ StageProgressionView.cs
└─ StageProgressionController.cs

Assets/06.Packages/Tests/EditMode/StageProgression/
├─ BattleRewardCatalogTests.cs
├─ BattleRewardGeneratorTests.cs
├─ BattleRewardStateTests.cs
├─ BattleRewardSessionTests.cs
└─ BattleRewardPresentationTests.cs
```

실제 파일 분리는 기존 코드 크기와 응집도에 따라 줄일 수 있다. 작은 값 객체를 파일마다 무조건 분리하지 말고 현재 프로젝트 관례를 우선한다.

## 15. 외부 의존성과 자산

- 신규 Unity 패키지나 오픈소스 라이브러리를 추가하지 않는다.
- 생성은 `System.Random`을 직접 전역 사용하지 않고 테스트 가능한 난수 경계로 감싼다.
- 신규 외부 이미지·오디오·폰트 에셋을 사용하지 않는다.
- 기존 TextMesh Pro와 즉시 모드 UI 범위 안에서 검증한다.

## 16. 검증 기준선

직전 CU-06의 확인 기준은 신규 반복 회귀 5/5, CoreLoop 122/122, StageProgression 34/34, 전체 EditMode 156/156, 두 테스트 씬 문제 0과 Console Error/Warning 0이다. RW-00은 문서만 작성했으며, RW-01에서는 신규 8/8, StageProgression 42/42와 전체 EditMode 164/164를 통과했다. RW-02에서는 신규 7/7, StageProgression 49/49와 전체 EditMode 171/171을 통과했다. RW-03에서는 신규 통합 6/6, StageProgression 55/55와 전체 EditMode 177/177을 통과했다. RW-04에서는 표시·입력 5/5, StageProgression 60/60와 전체 EditMode 182/182를 통과했다. RW-05에서는 10회 반복 검증 5/5, CoreLoop 122/122, StageProgression 65/65, 카드 사용 검증 5/5와 전체 EditMode 187/187를 Unity 6000.3.10f1의 로컬 MCP 연결로 통과했다. `StageTest`와 `CoreLoopTest` 씬 문제는 0개였고, 일반 선택·건너뛰기·다음 전투, 보스 선택·건너뛰기·런 승리, 패배와 재시작을 실제 Game View에서 확인했다. MCP WebSocket과 Test Framework 기반 시설 메시지를 확인·분류한 뒤 Console을 정리했으며 최종 Error/Warning은 0개다.

## 17. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-20 | 이천서 | 보상 데이터, 결정적 생성, 덱 추가·초기화, 진행 상태·세션·화면 API와 RW-01~RW-05 테스트 기준을 구현 가능한 명세로 확정 |
| 2026-07-20 | 이천서 | 일반 풀 10개와 높은 등급 풀 6개를 정의 키 목록으로 임시 확정하고 최종 보스 보상 완료 뒤 `RunVictory` 전이를 명시 |
| 2026-07-20 | 이천서 | RW-01 보상 카탈로그·불변 제안·결정적 생성·런 덱 추가와 재시작 복구 구현, 신규 8/8·StageProgression 42/42·전체 164/164 검증 결과 반영 |
| 2026-07-20 | 이천서 | RW-02 보상 선택 상태·보류 제안·선택·건너뛰기·완료 결과와 목적지 검증, 신규 7/7·StageProgression 49/49·전체 171/171 결과 및 RW-03 임시 세션 경계 반영 |
| 2026-07-20 | 이천서 | RW-03 전투 승리·보상 생성·명시적 엘리트 등급·선택/건너뛰기·다음 전투 덱·재시작 통합, 신규 6/6·StageProgression 55/55·전체 177/177 결과 반영 |
| 2026-07-20 | 이천서 | RW-04 후보 읽기 모델·기존 진행 화면의 선택/건너뛰기·결과 표시와 실제 일반/보스 흐름 구현, 신규 5/5·StageProgression 60/60·전체 182/182 결과 반영 |
| 2026-07-20 | 이천서 | RW-05 다섯 흐름 각 10회 반복, 신규 5/5·CoreLoop 122/122·StageProgression 65/65·카드 사용 5/5·전체 187/187와 실제 화면·씬·Console 최종 검증 결과 반영 |
