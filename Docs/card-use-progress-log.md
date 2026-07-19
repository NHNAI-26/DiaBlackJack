# 카드 사용 시스템 진행 기록

> 프로젝트: DiaBlackJack  
> 기록·구현 책임자: 이천서  
> 현재 단계: CU-02 완료
> 다음 단계: CU-03 — 자동 권총 세로 기능
> 최종 갱신: 2026-07-19

## 1. 기록 원칙

- 계획과 실제 구현을 구분한다.
- 코드가 없는 문서 단계는 구현 완료라고 기록하지 않는다.
- 각 단계의 변경 파일, 자동 테스트, 실제 화면 검증과 남은 위험을 기록한다.
- AI 대화 원문은 복사하지 않고 목적, 지시, 결과와 사람이 결정할 항목으로 정제한다.
- 기획·코드·검증의 최종 승인 책임자는 이천서로 기록한다.
- 사용자의 명시적 요청 전에는 스테이징·커밋하지 않는다.

## 2. 전체 현황

| 단계 | 상태 | 실제 결과 | 검증 |
| --- | --- | --- | --- |
| CU-00 | 완료 | 카드 사용 전용 문서 4종과 공통 기록 갱신 | 규칙·기획·현재 코드 정적 대조, Unity 미실행 |
| CU-01 | 완료 | 카드 정의·카탈로그·물리 카드 사용 상태·런 정의 키 보존 | 신규 19개 포함 전체 EditMode 101/101, Console Error/Warning 0 |
| CU-02 | 완료 | 사용 가능 판정·선택 대기·효과 처리·종료 원인·세션 전달 기반 | 신규 16개·CoreLoop 87/87·전체 EditMode 117/117, Console Error/Warning 0 |
| CU-03 | 대기 | 구현 없음 | 미실행 |
| CU-04 | 대기 | 구현 없음 | 미실행 |
| CU-05 | 대기 | 구현 없음 | 미실행 |
| CU-06 | 대기 | 구현 없음 | 미실행 |

## 3. 착수 기준선

### 3.1 완료된 선행 작업

- 코어 루프 4단계 완료
- 런·스테이지 진행 SP-00~SP-04 완료
- 전투 행동 확장 BA-00~BA-05 완료
- 히트, 스탠드, 폴드, 체인지의 독립·런 전투 전달 경로 존재
- 직전 전체 EditMode 82/82 통과

### 3.2 카드 사용 착수 전 코드 상태

- 전투 카드에는 ID, 숫자와 공개 여부만 있다.
- 런 카드 정의에는 ID와 숫자만 있다.
- 카드 정의, 효과, 사용 완료와 효과 선택 상태는 없다.
- UI는 카드 문자열만 표시하며 카드별 사용 입력이 없다.
- 진행 세션에는 카드 사용 전달 API가 없다.

### 3.3 Git 주의사항

CU-00 착수 직전 작업 트리는 깨끗했다. 이번 단계에서는 카드 사용 신규 문서 4종과 문서 색인·AI 활용·팀 역할 기록만 변경했다. CU-00 문서 기준은 후속 CU-01 코드와 분리해 커밋하는 것을 권장한다.

## 4. 결정 기록

| ID | 날짜 | 결정 | 이유 | 재검토 |
| --- | --- | --- | --- | --- |
| CU-D01 | 2026-07-19 | 첫 범위는 플레이어 일반 수동 카드 4종 | 카드 사용 경험을 완성하면서 자동·계약·AI 범위 폭발 방지 | CU-06 이후 |
| CU-D02 | 2026-07-19 | 사용 승인 뒤 취소 불가 | 비공개 공개·덱 확인 후 무료 취소 악용 방지 | 최종 UX 검토 |
| CU-D03 | 2026-07-19 | 해머는 자기 공개 카드 한 장을 버리고 사용 카드도 선택 가능 | 원문의 대상 모호성을 구현 가능한 비용 규칙으로 확정 | 카드 플레이 테스트 |
| CU-D04 | 2026-07-19 | 나이프의 단순 적은 비버스트 강제 카드를 유지 | 적 AI 작업 전 결정적이고 최소인 응답 제공 | 적 행동 AI 구현 시 |
| CU-D05 | 2026-07-19 | 상태 하나와 보류 선택 모델 사용 | 카드별 상태 열거형 증가 방지 | 복수 동시 효과 필요 시 |
| CU-D06 | 2026-07-19 | 순수 C# 카드 카탈로그로 시작 | 기존 규칙 테스트 유지와 ScriptableObject 의존 방지 | 카드 편집 도구 착수 전 |
| CU-D07 | 2026-07-19 | 타입이 있는 명령 경계만 공통화 | 계약 재사용 가능성을 남기되 범용 DSL 과설계 방지 | 계약 명세 확정 후 |
| CU-D08 | 2026-07-19 | 사용 상태 초기화는 `BlackjackHand.Add`에 집중 | 일반 드로우·체인지·후속 효과 드로우가 같은 손 진입 규칙을 공유 | 별도 카드 보관 영역 추가 시 |
| CU-D09 | 2026-07-19 | 출시 카탈로그의 실제 효과 처리기는 카드별 단계에서 등록 | CU-02 기반 검증을 위해 가짜 출시 카드를 남기지 않음 | CU-03 실제 자동 권총 처리기 등록 시 |
| CU-D10 | 2026-07-19 | 효과 시작 조건 조회는 상태를 바꾸지 않음 | 승인 전 실패의 원자성과 UI용 가능 여부 조회를 같은 규칙으로 보장 | 새 처리기 `CanStart` 구현 시 |

## 5. 단계별 기록

### CU-00 — 구현 기준 확정

#### 수행 내용

- `rule.md`의 차례 행동, 일반 카드, 자동 발동 카드와 보류 사항을 대조했다.
- 전체 게임 기획서의 카드 사용 공통 규칙과 효과 처리 원칙을 대조했다.
- 현재 `BlackjackCard`, `RunCardDefinition`, `CoreLoopBattle`, 세션·진행·UI 구조를 확인했다.
- 포함·제외 범위와 카드 4종의 모호한 규칙을 프로토타입 결정으로 확정했다.
- 데이터, 상태, 공개 API, 효과 처리, 카드 이동, UI와 진행 연결을 개발 명세로 작성했다.
- CU-00~CU-06의 순차 구현과 단계별 완료 게이트를 작성했다.
- AI 활용 기술 기록과 이천서의 계획 담당 기록을 갱신했다.

#### 변경 파일

- `Docs/card-use-design.md`
- `Docs/card-use-development-spec.md`
- `Docs/card-use-implementation-plan.md`
- `Docs/card-use-progress-log.md`
- `Docs/README.md`
- `Docs/ai-usage-technical-document.md`
- `Docs/team-role-technical-document.md`

#### 검증

- 문서 간 작업 ID와 단계 순서 대조
- 카드별 규칙과 개발 API·테스트 항목 연결 확인
- 포함·제외 범위와 임시 결정·재검토 시점 확인
- 신규 문서 링크와 이천서 이름 확인
- 코드·씬·패키지·에셋 변경 없음
- 문서 단계이므로 Unity 컴파일·테스트·Game View 검증 미실행

#### 다음 단계 진입 조건

- 카드 사용 문서 4종을 구현 기준으로 사용한다.
- CU-00 문서 변경을 검토하고 후속 구현과 분리한다.
- CU-01은 카드 정의·사용 상태와 호환 테스트만 구현한다.

#### 추천 커밋 제목

`카드 효과 구현 전에 규칙과 책임 경계를 확정`

### CU-01 — 카드 정의와 사용 상태 기반

상태: 완료

#### 수행 내용

- `CardDefinition`과 발동·효과·사용 상태 열거형을 추가했다.
- 숫자 1~10을 문서의 안정된 키와 카드 효과 유형에 연결하는 순수 C# 카탈로그를 추가했다.
- 기존 `BlackjackCard(int id, int rank, bool isFaceUp = false)` 생성자는 그대로 유지하고 정의 기반 생성 경로를 추가했다.
- 물리 카드별 `Unavailable`, `Available`, `Resolving`, `Used` 상태와 안전한 내부 전이를 구현했다.
- `BlackjackHand.Add`에서 수동 카드만 사용 가능하게 초기화해 일반 드로우·체인지·재드로우가 같은 경계를 사용하게 했다.
- `RunCardDefinition`에 `DefinitionKey`를 추가하고 기존 숫자 생성자와 `StageBattleFactory`를 카탈로그에 연결했다.
- 알 수 없는 정의 키는 효과 없는 카드로 대체하지 않고 명시적 예외로 처리했다.
- 카드 효과 실행, 공개 사용 API, UI, 씬, 패키지와 외부 에셋은 추가하지 않았다.

#### 변경 파일

- `Assets/01. Scripts/Runtime/CoreLoop/CardActivationKind.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectKind.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardUseState.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardDefinition.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardDefinitionCatalog.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BlackjackCard.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BlackjackHand.cs`
- `Assets/01. Scripts/Runtime/StageProgression/RunCardDefinition.cs`
- `Assets/01. Scripts/Runtime/StageProgression/StageBattleFactory.cs`
- `Assets/01. Scripts/Runtime/AssemblyInfo.cs`
- `Assets/Tests/EditMode/CoreLoop/CardDefinitionTests.cs`
- `Assets/Tests/EditMode/StageProgression/StageProgressionStateTests.cs`
- `Assets/Tests/EditMode/StageProgression/StageProgressionBattleTests.cs`
- 관련 문서

#### 검증

- Unity 6000.3.10f1 프로젝트와 MCP 연결 일치 확인
- 스크립트 컴파일 완료, Unity Console Error/Warning 0
- 신규 CU-01 테스트 19개 통과
- CoreLoop·StageProgression 대상 어셈블리 101/101 통과
- 전체 EditMode 101/101 통과
- 데이터 기반 단계이므로 Game View·씬 검증은 해당 없음

#### 다음 단계 진입 조건

- CU-02에서는 이 정의·상태를 사용해 공통 검증, 보류 선택과 타입이 있는 효과 처리 경계만 구현한다.
- 카드별 실제 효과와 UI를 미리 추가하지 않는다.

#### 추천 커밋 제목

`카드 효과를 안전하게 확장할 수 있도록 정의와 사용 상태를 분리`

### CU-02 — 효과 선택과 처리 기반

상태: 완료

#### 수행 내용

- `PlayerResolvingCardEffect`와 기계 판독 가능한 카드 사용 불가 사유를 추가했다.
- `PendingCardEffect`, 선택 종류·옵션과 `CardEffectResult`를 불변 모델로 작성했다.
- `CardEffectResolver`가 타입별 처리기를 등록하고 선택 없음·단일 선택·연속 선택을 같은 단계 결과로 반환하게 했다.
- `TryBeginPlayerCardUse`는 모든 시작 조건을 먼저 검사한 뒤 승인된 카드만 공개·`Resolving`으로 전이한다.
- `TryResolvePlayerCardChoice`는 현재 보류 목록의 유효 옵션만 처리하고 잘못된 입력은 상태와 호출 횟수를 유지한다.
- 선택 대기 중 히트·스탠드·폴드·체인지·다른 카드 사용을 모두 차단했다.
- 손패 ID 조회·인출, 덱 상단 임시 분리·다음 드로우 순서 반환과 카드 이동 명령 경계를 추가했다.
- `RoundEndCause.CardEffectBust`와 원인 카드 키를 기록하고, 효과 종료 시 적 차례를 건너뛰게 했다.
- `CoreLoopSession`에 카드 사용 시작·선택 전달을 추가했다.
- 실제 카드 효과·View·진행 세션 전달은 추가하지 않았고 출시 전투에서는 미구현 효과를 명시적으로 거절한다.

#### 변경 파일

- `Assets/01. Scripts/Runtime/CoreLoop/CardUseAvailability.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectSelection.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectResult.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectResolver.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BlackjackHand.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BlackjackDeck.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BattleParticipant.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CoreLoopSession.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CoreLoopState.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/RoundResolver.cs`
- `Assets/Tests/EditMode/CoreLoop/CardEffectFoundationTests.cs`
- 관련 문서

#### 검증

- Unity 6000.3.10f1 프로젝트와 MCP 연결·활성 `StageTest` 씬 일치 확인
- CU-02 신규 경계 테스트 16개 통과
- CoreLoop EditMode 87/87 통과(job `ee94e86a485b4a7c8a291cd3edfb64db`)
- 전체 EditMode 117/117, 실패·건너뜀 0(job `28d0d40c89da4842a50663c3817e3281`)
- 테스트 기반 시설 메시지를 정리한 뒤 Unity Console Error/Warning 0
- 규칙 기반 단계이므로 Game View·씬 검증은 해당 없음
- 새 런타임·테스트 어셈블리, 패키지와 외부 에셋 변경 없음

#### 다음 단계 진입 조건

- CU-03에서는 실제 자동 권총 처리기만 등록해 숫자 선택·성공·실패·효과 버스트를 세로로 완성한다.
- View와 `StageProgressionSession` 전달은 CU-05 전까지 추가하지 않는다.

#### 추천 커밋 제목

`카드 효과를 안전하게 처리하도록 선택과 완료 경계를 고정`

### CU-03 — 자동 권총 세로 기능

상태: 대기

CU-02의 원자성·선택 잠금·종료 테스트 통과 후 착수한다.

### CU-04 — 나머지 일반 수동 카드

상태: 대기

자동 권총으로 전체 규칙 경로를 검증한 뒤 카드 이동 명령을 확장한다.

### CU-05 — 화면과 런 전투 연결

상태: 대기

카드 4종의 순수 규칙 테스트가 완료된 뒤 UI와 진행 시스템에 연결한다.

### CU-06 — 전체 검증과 마감

상태: 대기

전체 회귀, 실제 화면, 런 승리·패배·재시작과 반복 검증을 수행한다.

## 6. 검증 누적표

| 단계 | 대상 테스트 | 전체 EditMode | Game View | 씬 | Console | 비고 |
| --- | --- | --- | --- | --- | --- | --- |
| 착수 기준 | BA-05 진행 27/27 | 82/82 | 통과 | 문제 0 | 오류 0 | 선행 작업에서 확보 |
| CU-00 | 미실행 | 미실행 | 미실행 | 미실행 | 미실행 | 문서 전용 |
| CU-01 | 신규 19/19·관련 어셈블리 101/101 | 101/101 | 해당 없음 | 해당 없음 | Error/Warning 0 | 데이터 기반, 씬 변경 없음 |
| CU-02 | 신규 16/16·CoreLoop 87/87 | 117/117 | 해당 없음 | 해당 없음 | Error/Warning 0 | 규칙 기반, 씬·패키지 변경 없음 |
| CU-03 | 대기 | 대기 | 해당 없음 | 해당 없음 | 대기 | 모델·세션 |
| CU-04 | 대기 | 대기 | 해당 없음 | 해당 없음 | 대기 | 모델·세션 |
| CU-05 | 대기 | 대기 | 대기 | 대기 | 대기 | UI·진행 |
| CU-06 | 대기 | 대기 | 대기 | 대기 | 대기 | 최종 마감 |

## 7. 미해결 사항

- 해머 버림 대상과 나이프 적 선택 정책은 프로토타입 임시 결정이며 플레이 테스트 후 재검토한다.
- 자동 발동 카드의 선택·효과 후 위치는 별도 작업에서 카드별로 확정한다.
- 계약 효과와 카드 명령의 실제 공통화 범위는 계약 개발 명세가 생기기 전까지 확대하지 않는다.
- 최종 카드 UI의 레이아웃·애니메이션·현지화는 기능 검증 뒤 별도 UI 작업으로 둔다.
- 카드 보상·강화·삭제가 시작되기 전에 런 카드 정의 키의 저장·마이그레이션 방식을 재검토한다.

## 8. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 카드 사용 CU-00 기준선, 결정 대장, 단계별 상태와 검증 누적표 작성 |
| 2026-07-19 | 이천서 | CU-01 카드 정의·사용 상태·런 정의 키 보존 구현과 신규 19개·전체 EditMode 101/101 검증 기록 |
| 2026-07-19 | 이천서 | CU-02 사용 검증·선택 대기·효과 처리·종료 원인 기반 구현과 신규 16개·전체 EditMode 117/117 검증 기록 |
