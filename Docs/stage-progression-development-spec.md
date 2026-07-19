# 런·스테이지 진행 시스템 개발 명세서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 버전: v0.1  
> 상태: SP-02 반영 기준안
> 최종 갱신: 2026-07-19

## 1. 기술 목표

기존 `DiaBlackJack.CoreLoop`의 단일 전투를 수정 범위가 작은 경계로 감싸고, 여러 전투를 하나의 런으로 연결한다. 진행 계층은 전투 세부 규칙을 소유하지 않으며 전투 결과와 플레이어의 지속 상태만 교환한다.

## 2. 설계 원칙

- 진행 상태는 순수 C# 객체가 소유한다.
- Unity `MonoBehaviour`는 입력 전달과 화면 갱신만 담당한다.
- 전투 코어 루프의 규칙 계산을 진행 계층에서 복제하지 않는다.
- 스테이지 정의와 런의 현재 상태를 분리한다.
- 유효하지 않은 상태의 입력은 상태를 바꾸지 않고 거부한다.
- 같은 전투 종료 결과를 두 번 처리해도 진행도가 두 번 증가하지 않게 한다.
- 새 외부 패키지를 추가하지 않는다.

## 3. 제안 파일 구조

| 경로 | 역할 |
| --- | --- |
| `Assets/01. Scripts/Runtime/StageProgression` | 런 상태, 스테이지 정의, 진행 세션과 전투 연결 |
| `Assets/01. Scripts/Runtime/UI/StageProgression` | 표시 모델, View와 Controller |
| `Assets/Tests/EditMode/StageProgression` | 상태 전이와 전투 연결 테스트 |
| `Assets/00. Scenes/StageProgressionTest.unity` | 런·스테이지 진행 전용 통합 씬 |

런타임 코드는 기존 `Border` 어셈블리에 두되 네임스페이스는 `DiaBlackJack.StageProgression`으로 분리한다. 테스트는 별도의 `DiaBlackJack.StageProgression.Tests.EditMode` 어셈블리를 사용한다.

## 4. 핵심 데이터

### 4.1 `StageDefinition`

| 필드 | 형식 | 규칙 |
| --- | --- | --- |
| `Id` | 문자열 | 경로 안에서 중복 불가, 빈 값 불가 |
| `DisplayName` | 문자열 | 화면 표시 이름 |
| `Kind` | `NormalCombat` 또는 `FinalBossCombat` | 최초 구현은 전투 두 종류만 허용 |
| `EnemyMaximumSoul` | 정수 | 1 이상 |
| `PlayerDeckSeed` | 정수 | 전투 시작 시 플레이어 덱 셔플 시드 |
| `EnemyDeckSeed` | 정수 | 적 덱 셔플 시드 |

목록의 배열 순서가 진행 순서다. `FinalBossCombat`은 마지막 항목에 정확히 하나만 존재해야 한다.

### 4.2 `PlayerRunState`

| 필드 | 규칙 |
| --- | --- |
| 현재 영혼 | 0 이상, 최대 영혼 이하 |
| 최대 영혼 | 1 이상, 최초 값 12 |
| 덱 구성 | 카드 ID와 숫자 목록, 중복 ID 불가 |

전투 중 손패, 공개 상태, 뽑을 더미와 버린 더미 위치는 런 상태에 저장하지 않는다. 덱의 구성만 유지한다.

### 4.3 `StageProgressionState`

```text
NotStarted
InBattle
StageCleared
RunVictory
RunDefeat
```

| 상태 | 의미 | 허용 입력 |
| --- | --- | --- |
| `NotStarted` | 런 생성 전 | 런 시작 |
| `InBattle` | 코어 루프 전투 진행 중 | 히트·스탠드 |
| `StageCleared` | 일반 전투 승리 결과 확인 중 | 다음 스테이지 |
| `RunVictory` | 최종 보스 처치 | 런 재시작 |
| `RunDefeat` | 플레이어 영혼 0 | 런 재시작 |

## 5. 책임 분리

### 5.0 `RunProgress` 순수 상태 기반

SP-01에서는 전투 객체를 참조하지 않는 `RunProgress`가 경로, 현재 인덱스와 진행 상태 전이만 소유하도록 구현했다. `PlayerRunState`는 영혼과 불변 덱 구성을, `RunCardDefinition`은 카드 ID와 숫자만 보존한다. 전투 카드의 공개 상태·손패·더미 위치는 이 계층에 전달하지 않는다.

`RunProgress`의 공개 전이 메서드는 `StartRun`, `TryCompleteCurrentStage`, `TryAdvanceToNextStage`, `TryDefeatRun`, `TryRestartRun`이다. SP-02의 통합 세션은 전투 결과를 확인한 뒤 이 메서드만 호출한다.

### 5.1 `StageProgressionSession`

- `RunProgress`를 소유하고 그 상태 전이 메서드만 호출한다.
- 현재 `CoreLoopSession`을 보유하고 전투 행동을 전달한다.
- 현재 전투 종료 결과를 한 번만 반영한다.
- 일반 전투 승리, 최종 승리와 런 패배를 구분한다.
- 다음 스테이지와 런 재시작을 생성한다.

SP-02 구현에서는 전투가 종료될 때만 플레이어의 남은 영혼을 `PlayerRunState`에 반영한다. 일반전 승리 후 종료된 전투는 결과 확인을 위해 유지하고, 다음 스테이지 입력이 승인되면 새 `CoreLoopSession`으로 교체한다. `CoreLoopSession.TryRestart`는 호출하지 않는다.

### 5.2 전투 생성기

- 현재 `PlayerRunState`와 `StageDefinition`을 받아 `CoreLoopBattle`을 생성한다.
- 플레이어 덱은 유지된 구성으로 새로 만들고 지정 시드로 섞는다.
- 플레이어 영혼은 런의 현재값으로 시작한다.
- 적 덱과 영혼은 스테이지 정의로 새로 만든다.
- 전투 종료 후 플레이어 영혼을 런 상태에 되돌린다.

실제 `StageBattleFactory`는 런 덱의 카드 ID·숫자를 새 `BlackjackCard`로 복사하고 스테이지의 양쪽 시드로 새 덱을 생성한다. 전투 중 카드의 공개 상태와 더미 위치는 다음 전투에 전달하지 않는다.

### 5.3 표시 계층

`StageProgressionViewModel`은 다음 값만 제공한다.

- 현재 스테이지 번호와 전체 수
- 스테이지 표시 이름과 종류
- 현재 진행 상태
- 기존 전투 표시 모델
- 다음 스테이지, 전투 행동과 런 재시작 가능 여부
- 런 승리·패배 문구

View는 스테이지 완료 여부나 전투 결과를 계산하지 않는다.

## 6. 기존 코어 루프 변경 경계

기존 `SoulPool`과 `BattleParticipant`는 최대 영혼으로 시작하는 생성자를 유지한다. SP-02에서 다음 호환 경로를 추가했다.

- `SoulPool`에 최대값과 현재값을 함께 받는 생성 경로 추가
- `BattleParticipant`와 `CoreLoopBattle`이 플레이어 시작 영혼을 전달할 수 있게 보완
- 기존 생성자는 최대 영혼으로 시작하는 현재 동작을 유지

`CoreLoopSession.TryRestart`의 의미는 단일 전투 재시작으로 유지한다. 진행 시스템에서는 이를 호출하지 않고 새 스테이지 전투 또는 새 런을 생성한다.

## 7. 상태 전이

```text
NotStarted --StartRun--> InBattle(인덱스 0 전투 생성)
InBattle --일반 전투 승리--> StageCleared
StageCleared --Continue--> InBattle(인덱스 + 1 전투 생성)
InBattle --최종 보스 승리--> RunVictory
InBattle --플레이어 패배--> RunDefeat
RunVictory/RunDefeat --RestartRun--> InBattle(인덱스 0, 영혼 12 전투 생성)
```

`Continue` 처리 중 다음 인덱스가 범위를 벗어나면 명시적인 오류다. 최종 보스 승리는 반드시 `RunVictory`로 직접 전환되어야 한다.

## 8. 불변 조건

- 스테이지 목록은 비어 있을 수 없다.
- 현재 인덱스는 항상 목록 범위 안에 있다.
- 최종 보스는 마지막 스테이지다.
- `InBattle` 상태에서만 전투 행동을 전달한다.
- `StageCleared` 상태에서만 다음 스테이지로 이동한다.
- 전투 결과 하나는 한 번만 진행 상태에 반영된다.
- 플레이어 영혼 0 상태로 다음 스테이지를 만들지 않는다.
- 런 재시작 후 이전 전투·이벤트·표시 결과를 참조하지 않는다.

## 9. 자동 테스트 명세

### 9.1 단위·상태 테스트

| ID | 검증 내용 | 기대 결과 |
| --- | --- | --- |
| SP-U01 | 유효한 경로로 런 시작 | 인덱스 0, 영혼 12, 새 전투와 `InBattle` |
| SP-U02 | 런 시작 전 전투 입력 | 거부되고 상태 변화 없음 |
| SP-U03 | 일반 전투 승리 처리 | `StageCleared`, 인덱스 즉시 증가하지 않음 |
| SP-U04 | 다음 스테이지 이동 | 인덱스가 정확히 1 증가 |
| SP-U05 | 승리 후 다음 전투 생성 | 플레이어 남은 영혼 유지, 적 상태 초기화 |
| SP-U06 | 플레이어 패배 처리 | `RunDefeat`, 인덱스 유지 |
| SP-U07 | 최종 보스 승리 처리 | `RunVictory`, 다음 스테이지 미생성 |
| SP-U08 | 같은 종료 결과 재처리 | 진행도 중복 증가 없음 |
| SP-U09 | 유효하지 않은 입력 | 상태와 인덱스 변화 없음 |
| SP-U10 | 런 재시작 | 인덱스 0, 영혼 12, 이전 결과 제거 |
| SP-U11 | 잘못된 경로 정의 | 생성 시 명시적 예외 |
| SP-U12 | 유지된 덱 구성으로 새 전투 생성 | 카드 수·ID·숫자 보존, 전투 더미 상태는 신규 |

### 9.2 흐름 테스트

| ID | 검증 내용 | 기대 결과 |
| --- | --- | --- |
| SP-F01 | 일반전 2개와 보스전 연속 승리 | 진행도 1/3→2/3→3/3, `RunVictory` |
| SP-F02 | 첫 전투에서 영혼 손실 후 승리 | 두 번째 전투에 남은 영혼 유지 |
| SP-F03 | 중간 스테이지 패배 | `RunDefeat`, 다음 전투 없음 |
| SP-F04 | 승리·패배 양쪽에서 런 재시작 | 항상 첫 스테이지의 깨끗한 상태 |
| SP-F05 | 런 재시작 10회 | 이벤트 중복과 진행 불가 상태 없음 |

기존 `CL-U01~CL-U12`, `CL-F01~CL-F06`도 전부 회귀 테스트로 실행한다.

## 10. 수동 검증

- `StageProgressionTest` 씬에서 현재 진행도를 확인한다.
- 일반 전투 승리 후 결과 화면에서 다음 스테이지로 이동한다.
- 두 번째 전투에 이전 전투의 남은 플레이어 영혼이 표시되는지 확인한다.
- 최종 보스 승리와 중간 패배 화면을 각각 확인한다.
- 런 재시작 후 진행도와 모든 상태가 초기화되는지 확인한다.
- Unity Console의 게임 관련 Error·Exception이 0인지 확인한다.

## 11. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 진행 계층의 데이터, 상태 전이, 코어 루프 연결 경계와 SP-U/SP-F 테스트 명세 작성 |
| 2026-07-19 | 이천서 | SP-01 실제 구현에 맞춰 `RunProgress` 순수 상태 기반과 SP-02 통합 세션의 책임 경계 명시 |
| 2026-07-19 | 이천서 | SP-02 실제 구현에 맞춰 전투 생성기·통합 세션·현재 영혼 호환 생성 경로와 결과 동기화 경계 명시 |
