# 카드 사용 시스템 진행 기록

> 프로젝트: DiaBlackJack  
> 기록·구현 책임자: 이천서  
> 현재 단계: CU-06 완료
> 다음 단계: 후속 카드 콘텐츠·최종 UI는 별도 작업으로 계획
> 최종 갱신: 2026-07-20

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
| CU-03 | 완료 | 자동 권총 7·8의 1~10 추측·성공/실패·직접 버스트·정보 은닉 | 신규 8개·CoreLoop 95/95·전체 EditMode 125/125, Console Error/Warning 0 |
| CU-04 | 완료 | 수정 구슬 순서 보존·해머 단일 비공개 교체·나이프 강제 드로우와 유지 정책 | 신규 18개·CoreLoop 113/113·전체 EditMode 143/143, Console Error/Warning 0 |
| CU-05 | 완료 | 카드별 표시·사용/선택 입력·최근 결과·독립/런 세션 전달과 종료 동기화 | 신규 8개·관련 28/28·CoreLoop 117/117·StageProgression 34/34·전체 EditMode 151/151, Game View·씬·Console 통과 |
| CU-06 | 완료 | 반복 회귀 테스트와 실제 런 승리·패배·재시작 검증, 기록 마감 | 신규 5/5·CoreLoop 122/122·StageProgression 34/34·전체 EditMode 156/156, 씬·Console 통과 |

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
| CU-D11 | 2026-07-19 | 자동 권총은 정상 라운드의 단일 비공개 카드만 대상 | 기본 규칙에서 비공개 카드 2장 이상은 발생하지 않으며 불필요한 다중 대상 분기를 만들지 않음 | 기본 라운드 배분 규칙 변경 시 |
| CU-D12 | 2026-07-19 | 수정 구슬·해머·나이프의 합계 초과는 `NumericBust`로 판정 | 숫자 합계 초과와 자동 권총의 직접 효과 버스트 원인을 구분 | 카드별 종료 원인 규칙 변경 시 |

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

상태: 완료

#### 수행 내용

- 기본 `CardEffectResolver`에 `AutoPistolEffectHandler`를 등록했다.
- 숫자 7·8 자동 권총이 같은 처리기를 사용하며 1~10 숫자 선택지만 제공한다.
- 정상 라운드의 상대 비공개 카드 한 장을 전제로 하고 다중 비공개 카드 지원은 추가하지 않았다.
- 상대 비공개 카드가 없으면 사용 카드를 공개하거나 상태를 바꾸기 전에 거절한다.
- 선언 숫자와 실제 숫자는 처리기 내부에서만 비교하고 상대 카드를 공개하지 않는다.
- 일치하면 `CardEffectBust`로 적 영혼 1 피해를 적용하고 적 차례 없이 다음 라운드 또는 승리로 진행한다.
- 불일치하면 피해 없이 사용 완료 결과를 남기고 기존 적 차례를 정확히 한 번 실행한다.
- 공개 `CardEffectResult`는 효과 유형·원본 카드 ID·성공·종료 여부만 유지해 비공개 숫자를 전달하지 않는다.
- 최종 View, 진행 세션, 수정 구슬·해머·나이프는 구현하지 않았다.

#### 변경 파일

- `Assets/01. Scripts/Runtime/CoreLoop/AutoPistolEffectHandler.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectResolver.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CoreLoopBattle.cs`
- `Assets/Tests/EditMode/CoreLoop/AutoPistolEffectTests.cs`
- 관련 문서

#### 검증

- Unity 6000.3.10f1 프로젝트와 MCP 연결·활성 `StageTest` 씬 일치 확인
- 자동 권총 전용 테스트 8/8 통과(job `5313dd71a905480d903e3bffe5d9c6a3`)
- CoreLoop EditMode 95/95 통과(job `6cd877a6354440c5b9a0d96e3949c459`)
- 전체 EditMode 125/125, 실패·건너뜀 0(job `6a77fc8ed74f48659b541b7797d16774`)
- 테스트 기반 시설 메시지를 정리한 뒤 Unity Console Error/Warning 0
- 모델·세션 단계이므로 Game View·씬 검증은 해당 없음
- 새 어셈블리·패키지·외부 에셋 변경 없음

#### 다음 단계 진입 조건

- CU-04에서는 수정 구슬·위협용 해머·군용 나이프 처리기만 추가한다.
- View와 `StageProgressionSession` 전달은 CU-05 전까지 추가하지 않는다.

#### 추천 커밋 제목

`비공개 정보 노출 없이 자동 권총의 추측 전투를 완성`

### CU-04 — 나머지 일반 수동 카드

상태: 완료

#### 수행 내용

- 기본 효과 처리기에 수정 구슬, 위협용 해머와 군용 나이프를 등록했다.
- 수정 구슬은 덱 위 두 장을 보류 효과의 임시 소유 카드로 분리하고, 0장·첫째·둘째 중 하나를 선택하게 했다.
- 선택하지 않은 수정 구슬 후보는 기존 다음 드로우 순서를 보존해 덱 위로 반환하고, 선택 카드는 공개 손패로 이동한다.
- 해머는 사용 승인으로 공개된 원본 카드까지 포함해 자기 공개 카드만 비용 선택지로 만든다.
- 상대가 스탠드하지 않았다면 비용만 지불하고, 스탠드했다면 정확히 한 장인 기존 비공개 카드를 공개하지 않고 버린 뒤 새 비공개 카드로 교체하고 스탠드를 취소한다.
- 해머의 비공개 교체는 단일 비공개 카드와 덱 잔량을 승인 전에 검사해 조건 불충족 시 전투 상태를 바꾸지 않는다.
- 나이프 9·10은 상대 공개 합계 16 이하와 덱 한 장을 검사한 뒤 공개 강제 드로우를 수행한다.
- 나이프의 비버스트 결과는 교체 가능한 유지 정책에 위임하고, 현재 단순 적 정책은 항상 카드를 유지한다.
- 세 효과가 숫자 합계 초과를 만들면 `NumericBust`로 즉시 라운드를 끝내고 적 차례를 실행하지 않는다.
- 정상 라운드의 비공개 카드가 한 장이라는 기존 규칙을 유지했으며 다중 비공개 카드 지원은 추가하지 않았다.
- View, Controller, `StageProgressionSession`, 씬은 변경하지 않아 화면에서의 카드 사용은 아직 불가능하다.

#### 변경 파일

- `Assets/01. Scripts/Runtime/CoreLoop/CrystalOrbEffectHandler.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/ThreatHammerEffectHandler.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/MilitaryKnifeEffectHandler.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectResolver.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/CardEffectSelection.cs`
- `Assets/01. Scripts/Runtime/CoreLoop/BattleParticipant.cs`
- `Assets/Tests/EditMode/CoreLoop/RemainingCardEffectTests.cs`
- 관련 문서

#### 검증

- Unity 6000.3.10f1, MCP 인스턴스 `DiaBlackJack@5635a4cdcfecc8dd`, 프로젝트 루트와 활성 `StageTest` 씬 일치
- CU-04 전용 18/18 통과(job `77ea7ab525814da4bd75fd89e163015e`)
- CoreLoop 113/113 통과(job `cf493f9f57fa460189ba6dd9dd5fa1e0`)
- 전체 EditMode 143/143, 실패·건너뜀 0(job `75dff0608451447492d29660b5469c93`)
- 테스트 기반 시설 메시지를 정리한 뒤 Unity Console Error/Warning 0
- 모델·CoreLoop 세션 단계이므로 Game View·씬 직렬화 검증은 해당 없음
- 새 어셈블리·패키지·외부 에셋 변경 없음

#### 다음 단계 진입 조건

- CU-05에서 카드 사용 가능 상태, 선택지와 최근 결과를 전투 ViewModel에 노출한다.
- Controller와 `StageProgressionSession`에 카드 ID·선택 ID 전달 및 전투 종료 동기화를 연결한다.
- `CoreLoopTest`와 `StageTest`에서 네 카드의 실제 입력을 검증한다.

#### 추천 커밋 제목

`일반 카드 효과가 같은 선택과 카드 이동 규칙을 따르도록 완성`

### CU-05 — 화면과 런 전투 연결

상태: 완료

#### 실제 구현

- `PlayerCardViewModel`과 효과 선택 표시 모델에 카드별 ID·숫자·이름·사용 상태·사용 가능 여부·불가 사유, 선택 안내와 안전한 최근 결과를 추가했다.
- `CoreLoopView`에 카드별 `USE` 버튼과 효과 선택 전용 화면을 추가하고 선택 중 일반 행동과 다른 카드 입력을 차단했다.
- `CoreLoopController`가 카드 ID·선택 ID를 독립 `CoreLoopSession` 또는 런 `StageProgressionSession`에 전달하도록 연결했다.
- `StageProgressionSession`에 카드 사용 시작·선택 완료 전달을 추가하고 기존 `SynchronizeFinishedBattle()`로 승리·패배와 지속 영혼을 한 번만 반영했다.
- 플레이 모드 입력 잠금은 다음 프레임에 해제하고, GUI 이벤트 중 모델이 교체되어도 선택 목록 스냅샷을 사용하도록 보완했다.

#### 변경 파일

- `Assets/01. Scripts/Runtime/UI/CoreLoop/CoreLoopPresentation.cs`
- `Assets/01. Scripts/Runtime/UI/CoreLoop/CoreLoopView.cs`
- `Assets/01. Scripts/Runtime/UI/CoreLoop/CoreLoopController.cs`
- `Assets/01. Scripts/Runtime/StageProgression/StageProgressionSession.cs`
- `Assets/Tests/EditMode/CoreLoop/CoreLoopPresentationTests.cs`
- `Assets/Tests/EditMode/StageProgression/StageProgressionBattleTests.cs`
- 카드 사용·AI 활용·팀 역할·프로젝트 구조 관련 문서

#### 검증

- 신규 8개를 포함한 Presenter·진행 통합 28/28 통과(job `7c3e07ddbc2e4b6aa11636abaa0dfd82`)
- CoreLoop 117개와 StageProgression 34개를 포함한 전체 EditMode 151/151, 실패·건너뜀 0(job `14c6473e2cb844419afd298523f7ba0e`)
- `StageTest`에서 런 시작 후 `CoreLoopTest`가 진행 세션의 동일 전투 인스턴스를 사용하는 것을 확인했다.
- Game View에서 수정 구슬 3개 선택, 해머 공개 카드 비용 선택, 자동 권총 1~10 선언, 군용 나이프 즉시 결과와 선택 중 일반 행동 차단·최근 결과·입력 잠금 해제를 확인했다.
- `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0, 최종 Console Error/Warning 0
- 씬 직렬화·어셈블리·패키지·외부 에셋 변경 없음

#### 검증 중 보완

MCP가 Unity 창의 포커스를 가져가지 않은 상태에서는 게임 프레임이 진행되지 않아 다음 프레임 입력 해제도 대기했다. 검증 세션에서만 백그라운드 실행을 켜 실제 프레임 진행을 재현했고, 프레임이 진행되면 잠금이 해제됨을 확인했다. 또한 효과 선택과 같은 GUI 이벤트 안에서 모델이 교체될 때 이전 목록 인덱스를 읽을 수 있는 경로를 선택 목록 스냅샷으로 제거했다.

#### 다음 단계 진입 조건

- CU-06에서 카드 4종의 정상·거절·버스트, 재드로우, 독립 재시작과 런 승리·패배·재시작을 반복 검증한다.
- 숨은 정보 미노출과 카드 중복·유실·입력 고착 0건을 최종 확인하고 문서를 마감한다.

#### 추천 커밋 제목

`카드 사용 선택이 실제 전투 화면과 런 결과까지 이어지도록 연결`

### CU-06 — 전체 검증과 마감

상태: 완료

#### 실제 구현

- `CardUseSystemValidationTests` 5개로 독립 전투 재시작, 수정 구슬 후보 선택, 상대 비공개 숫자 1~10 은닉, 런 승리·패배 재시작을 고정 드로우 순서로 검증했다.
- 각 재시작과 수정 구슬 선택을 10회 반복하며 카드 총수·소유권·손패 ID 고유성, 새 전투 인스턴스, 영혼과 사용 상태 초기화를 확인했다.
- 런타임 규칙·UI·씬은 수정하지 않고 이미 구현된 공개 경로를 최종 검증했다.

#### 변경 파일

- `Assets/Tests/EditMode/CoreLoop/CardUseSystemValidationTests.cs`
- 카드 사용 문서 4종, 문서 색인, AI 활용·팀 역할·프로젝트 구조 기록

#### 검증

- 신규 CU-06 5/5 통과(job `3c6b86f1590e411db8a7e8cbe66cd86a`)
- CoreLoop 122/122 통과(job `d4727c3cc840426593170151fd879a3f`)
- StageProgression 34/34 통과(job `029e2eb3742345fe804bb0fdf39fb4fb`)
- 전체 EditMode 156/156, 실패·건너뜀 0(job `3cffa1feb5e94b1ea6781a644700bc76`)
- 실제 Controller·Runtime 경로에서 군용 나이프 최종 승리→런 재시작과 수정 구슬 버스트 패배→런 재시작을 확인했다. 재시작 뒤 각각 영혼 12·2, 새 `CoreLoopTest` 전투와 `Available` 카드를 확인했다.
- `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0, 최종 Console Error/Warning 0
- 런타임·씬·패키지·외부 에셋 변경 없음

#### 남은 제한

- CU-05에서 확인한 카드 4종의 공통 화면 흐름을 재사용했다. 최종 레이아웃·애니메이션·현지화는 이번 기능 완료 범위가 아니다.
- 해머 버림 대상과 나이프의 비버스트 적 선택은 프로토타입 임시 결정으로 유지한다.

#### 추천 커밋 제목

`카드 사용의 규칙부터 런 결과까지 반복 검증해 작업을 마감`

## 6. 검증 누적표

| 단계 | 대상 테스트 | 전체 EditMode | Game View | 씬 | Console | 비고 |
| --- | --- | --- | --- | --- | --- | --- |
| 착수 기준 | BA-05 진행 27/27 | 82/82 | 통과 | 문제 0 | 오류 0 | 선행 작업에서 확보 |
| CU-00 | 미실행 | 미실행 | 미실행 | 미실행 | 미실행 | 문서 전용 |
| CU-01 | 신규 19/19·관련 어셈블리 101/101 | 101/101 | 해당 없음 | 해당 없음 | Error/Warning 0 | 데이터 기반, 씬 변경 없음 |
| CU-02 | 신규 16/16·CoreLoop 87/87 | 117/117 | 해당 없음 | 해당 없음 | Error/Warning 0 | 규칙 기반, 씬·패키지 변경 없음 |
| CU-03 | 신규 8/8·CoreLoop 95/95 | 125/125 | 해당 없음 | 해당 없음 | Error/Warning 0 | 모델·세션, 씬·패키지 변경 없음 |
| CU-04 | 신규 18/18·CoreLoop 113/113 | 143/143 | 해당 없음 | 해당 없음 | Error/Warning 0 | 모델·세션, 씬·패키지 변경 없음 |
| CU-05 | 신규 8/8·관련 28/28 | 151/151 | 카드 4종 사용·선택·결과 통과 | 문제 0 | Error/Warning 0 | UI·독립/런 전달, 씬 변경 없음 |
| CU-06 | 신규 5/5·CoreLoop 122/122·진행 34/34 | 156/156 | CU-05 카드 4종 화면 + 실제 런 승리·패배 재시작 통과 | 문제 0 | Error/Warning 0 | 10회 반복·정보 은닉·소유권, 최종 마감 |

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
| 2026-07-19 | 이천서 | CU-03 자동 권총 단일 비공개 카드 추측·성공/실패·정보 은닉 구현과 신규 8개·전체 EditMode 125/125 검증 기록 |
| 2026-07-19 | 이천서 | CU-04 수정 구슬·위협용 해머·군용 나이프 구현과 신규 18개·CoreLoop 113/113·전체 EditMode 143/143 검증 기록 |
| 2026-07-19 | 이천서 | CU-05 카드 표시·효과 선택 UI·독립/런 세션 전달·종료 동기화 구현과 신규 8개·전체 EditMode 151/151·Game View·씬·Console 검증 기록 |
| 2026-07-20 | 이천서 | CU-06 반복 회귀 5개·전체 EditMode 156/156, 실제 런 승리·패배 재시작과 양쪽 씬·Console 최종 검증으로 카드 사용 1차 범위 마감 |
