# 악마 계약 시스템 진행 기록

> 프로젝트: DiaBlackJack  
> 기획·기록·구현 책임자: 이천서  
> 작업 식별자: DC-00~DC-07  
> 버전: v0.7
> 현재 단계: DC-05 플레이어 계약 화면·런 연결 완료
> 다음 단계: DC-06 사탄 규칙 결정
> 최종 갱신: 2026-07-23

## 1. 기록 원칙

- 계획과 실제 구현을 분리한다.
- 코드가 없는 문서 단계는 구현 완료로 기록하지 않는다.
- 단계마다 작업자, 변경 파일, 직접 결정한 문제, 대상·전체 테스트와 실제 화면 결과를 남긴다.
- AI 대화 원문은 복사하지 않고 목적, 주요 지시, 결과와 사람의 결정을 기술적으로 정리한다.
- 기획·코드·검증의 최종 승인 책임자는 이천서로 기록한다.
- 팀원의 예정 업무는 실제 변경과 검증이 확인되기 전까지 완료 기여로 기록하지 않는다.
- 사용자의 명시적 요청 전에는 스테이징·커밋하지 않는다.

## 2. 현재 기준선

| 항목 | 현재 상태 |
| --- | --- |
| 일반 전투 행동 | 히트·스탠드·누적 비용 체인지 구현 완료 |
| 수동 일반 카드 | 수정 구슬·위협용 해머·리볼버·보위 나이프 구현 완료 |
| 중간·최종 합계 | 행동 중 공개 합, 양쪽 스탠드 뒤 전체 합으로 분리 완료 |
| 카드 표시 | 양측 비공개 카드가 플레이어 화면 왼쪽에 표시됨 |
| 런 영혼·일반 덱 | `PlayerRunState`와 전투 변환 구현 완료 |
| 적 AI | 공개 관측·카드 행동·적 5종 정책 구현 완료 |
| 계약 데이터·덱 | 네 정의·런 최초/현재 덱·전투 드로우/버림 덱·후보 3장 기반 구현 완료 |
| 계약 공통 행동 | 비용·전투당 횟수·후보 필수 선택·활성/버림·세션 전달 구현 완료 |
| 계약 개별 효과 | 벨페고르와 마몬·레비아탄 구현 완료, 사탄은 결정 대기 |
| 계약 UI·적 AI | 플레이어 UI 완료, 적 AI 미구현 |
| 최근 자동 기준선 | DC-05 전체 EditMode 369/369 통과 기록 |
| DC-00 검증 | 문서 전용, Unity 재실행 안 함 |

## 3. 단계 현황

| 단계 | 담당 | 상태 | 완료 증거 |
| --- | --- | --- | --- |
| DC-00 | 이천서(AI 문서·구조 대조 보조) | 완료 | 문서 4종·색인·AI 활용·역할 기록 |
| DC-01 | 이천서(AI 구현·검증 보조) | 완료 | 대상 19/19·전체 EditMode 329/329·컴파일 오류 0 |
| DC-02 | 이천서(AI 구현·검증 보조) | 완료 | 대상 13/13·전체 EditMode 342/342·컴파일 오류 0 |
| DC-03 | 이천서(AI 구현·검증 보조) | 완료 | 대상 9/9·전체 EditMode 351/351·컴파일 오류 0 |
| DC-04 | 이천서(AI 구현·검증 보조) | 완료 | 대상 11/11·전체 EditMode 362/362·컴파일 오류 0 |
| DC-05 | 이천서(AI 구현·화면·검증 보조) | 완료 | 대상 7/7·전체 369/369·두 씬·두 해상도·Console 0 |
| DC-06 | 이천서 | 기획 확인 대기 | 사탄 결정 기록·효과·교착 회귀 |
| DC-07 | 이천서(AI 구현·검증 보조 예정) | 미착수 | 적 계약·반복 전체 흐름·문서 마감 |

## 4. DC-00 수행 기록

### 4.1 수행 내용

- `rule.md`와 전체 게임 기획의 계약 비용, 제공 방식, 악마 12종 원문과 MVP 4종을 대조했다.
- 현재 `CoreLoopBattle`, 카드 효과 처리기, `PlayerRunState`, `StageBattleFactory`, 진행 세션·표시·적 AI 경계를 확인했다.
- 기본 비용 1과 `현재 영혼 > 비용`, 전투당 1회, 위 3장 중 1장 선택, 미선택 버림, 전투 종료까지 지속을 현행 기준으로 정리했다.
- 계약이 플레이어의 정상 행동 1회를 소비하고 비용 지불 후 후보 공개 뒤에는 취소·환불할 수 없도록 프로토타입 결정을 기록했다.
- 시작 악마 덱이 미정인 문제를 우선 악마 4종 각 1장의 4장 덱으로 임시 결정했다.
- 벨페고르를 첫 수직 구현 대상으로 정하고 마몬·레비아탄의 선택·판정 순서를 구현 가능한 수준으로 구체화했다.
- 사탄의 카운터 4/6, 증가/감소, 종료/반복과 권능 카드 합계·수명 충돌을 DC-D05·D06 결정 게이트로 분리했다.
- 일반 카드 문서와 README의 진행 상태를 실제 완료된 CU-M03으로 맞췄다.

### 4.2 산출물

- `Docs/demonic-contract-design.md`
- `Docs/demonic-contract-development-spec.md`
- `Docs/demonic-contract-implementation-plan.md`
- `Docs/demonic-contract-progress-log.md`
- `Docs/README.md`
- `Docs/ai-usage-technical-document.md`
- `Docs/team-role-technical-document.md`
- 카드 사용 문서 4종 상태 표기 정리

### 4.3 검증

- 작업 ID, 문서 제목, 책임자, 현재·다음 단계와 내부 링크 검색
- `현재 영혼 > 비용`, 전투당 1회, 후보 3장, 시작 덱 4장과 우선 악마 4종의 문서 간 일치 확인
- 구현 완료와 예정 상태 분리 확인
- 코드·테스트·씬·프리팹·패키지·외부 에셋 변경 없음 확인
- 문서 전용 단계이므로 Unity 테스트·Play Mode·Game View는 재실행하지 않음

### 4.4 AI 활용과 사람 결정

AI는 저장소 검색, 원본 규칙과 현재 코드 책임 경계 대조, 충돌 탐지와 문서 초안을 보조했다. 계약을 MVP 핵심 작업으로 선택한 방향, 이천서를 책임자로 유지하는 결정과 최종 규칙 승인 책임은 이천서에게 있다. 시작 덱 4장, 행동 소비, 취소 불가와 악마별 프로토타입 세부안은 구현 전 이천서가 검토할 임시 결정으로 명시했다.

### 4.5 완료 증거

- 문서 4종과 색인 작성
- 카드 사용 상태 표기 CU-M03으로 동기화
- 신규 런타임·테스트·Unity 에셋 0개
- 최근 310/310은 CU-M03의 과거 기준선이며 DC-00에서 새로 실행한 결과가 아님

### 4.6 권장 커밋 제목

`계약 개발이 충돌 없는 기준에서 시작되도록 규칙과 경계를 고정하다`

### 4.7 악마 카드 공통 규칙 개정

- 계약 시작 비용과 개별 악마 대가를 분리했다. 시작 비용은 `현재 영혼 > 비용`을 유지하지만 개별 대가는 영혼을 0까지 감소시킬 수 있고 0이 되는 순간 즉시 전투 패배한다.
- `할 수 있다`를 소유자의 선택으로, `버스트`를 숫자·카드·악마·계약 효과와 특수 패배 조건 전체로 확정했다.
- 숫자가 적힌 계약 생성 특수 카드는 합계에 포함하고 숫자가 바뀌면 즉시 다시 계산하도록 확정했다.
- 특별한 추가 계약에서는 이미 활성인 악마와 같은 악마를 다시 선택할 수 있으며 계약별 물리 ID와 런타임 상태를 분리하도록 했다.
- 사탄 권능·바포메트 오망성·파이몬 추방·벨리알 탈취는 현재 전투의 여러 라운드에 유지하지만 전투 종료 시 생성 제거·전투 시작 덱 구성 복구·원소유권 복구를 수행해 런 덱을 영구 변경하지 않도록 확정했다.
- 적도 방향만 반대로 하여 플레이어와 같은 비용·선택·대가·중첩·정리 규칙을 사용한다.
- 파이몬 효과로 양측이 연속 버스트하는 경우의 피해 우선순위와 동일 악마별 구체적인 효과·대가 계산은 아직 확정하지 않았다.

문서 개정만 수행했으며 계약 런타임·테스트·씬·프리팹·패키지·외부 에셋은 변경하지 않았다.

## 5. DC-01 수행 기록

### 5.1 수행 내용

- `DemonContractDefinition`, `DemonContractKind`, `DemonContractCatalog`에 사탄·벨페고르·마몬·레비아탄의 안정 키, 표시명, 기본 영혼 비용, 효과·대가 요약을 등록했다.
- `RunDemonDefinition`이 런 물리 ID와 유효한 정의 키만 보존하게 하고, `PlayerRunState`에 최초/현재 악마 덱과 별도 다음 ID를 추가했다.
- 시작 덱은 프로토타입 결정대로 네 악마 각 1장이고, 같은 정의의 다른 물리 ID를 허용한다. 런 재시작은 획득 카드를 제거하고 최초 4장과 다음 ID를 복구한다.
- `DemonContractCard`와 `DemonContractDeck`을 일반 카드와 분리했다. 후보 3장 이동, 부족 시 무변경 거절, 버린 더미 보충, 활성·후보 카드 제외와 카드 총수 보존을 구현했다.
- `StageBattleFactory`가 런 악마 덱을 매 전투 새 전투 덱으로 변환하고, 일반 카드 덱과 분리된 파생 시드·난수 인스턴스를 사용하게 했다.
- `CoreLoopBattle`은 진행 전투에서 플레이어 악마 덱을 소유하고, 기존 독립 전투는 공유되지 않는 빈 덱으로 호환했다.
- 전체 회귀 중 기존 CU-M03 플레이어 비공개 카드 투영이 테스트 계약과 어긋난 상태를 발견해 `GameScenePresentation.cs`의 플레이어 표시 목록만 선두 배치로 복구했다. `GameScene.unity` 씬 에셋은 수정하지 않았다.

### 5.2 변경 파일

- `Assets/01. Scripts/CoreLoop/DemonContracts/*.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/StageProgression/RunDemonDefinition.cs`
- `Assets/01. Scripts/StageProgression/PlayerRunState.cs`
- `Assets/01. Scripts/StageProgression/StageBattleFactory.cs`
- `Assets/01. Scripts/GameScene/GameScenePresentation.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/DemonContractFoundationTests.cs`
- `Assets/06.Packages/Tests/EditMode/StageProgression/DemonContractStageIntegrationTests.cs`
- 계약 4종·색인·AI 활용·팀 역할·프로젝트 구조 기록 문서

### 5.3 검증

- DC-01 대상: 19/19 통과(job `064ec40fd3444462898eb2ba2c732719`).
- 전체 EditMode: 최종 329/329 통과, 실패·건너뜀 0(job `9db8defa9d234ecd8a1d129415446641`).
- CU-M03 표시 회귀: 단독 1/1 통과(job `a5d0763e9f604662ad4da41bdf12a801`).
- Unity 6000.3.10f1의 `DiaBlackJack@5635a4cdcfecc8dd`에서 에셋 갱신·컴파일 오류 0을 확인했다. 최종 테스트 뒤 Console의 Unity Test Framework 경고 4건과 결과 저장 `Exception` 2건은 게임 코드 오류와 분리했다.
- 데이터·덱 단계라 Game View와 Play Mode는 대상이 아니며 씬·프리팹·패키지·외부 에셋을 변경하지 않았다.

### 5.4 AI 활용과 사람 결정

AI는 기존 일반 카드·런·전투 변환 패턴과 DC-01 완료 게이트를 대조해 코드·테스트 초안, Unity MCP 컴파일·테스트와 문서 정리를 보조했다. 네 장 시작 덱, 일반/악마 덱 분리, 전투별 복사와 단계 범위는 이천서가 승인한 기획 기준을 따랐다. 개별 악마 효과와 계약 행동을 미리 구현하지 않았으며 최종 코드·기획 승인 책임은 이천서에게 있다.

### 5.5 권장 커밋 제목

`악마 카드가 일반 덱을 침범하지 않고 런과 전투 사이에서 보존되게 하다`

## 6. DC-02 수행 기록

### 6.1 수행 내용

- `DemonContractAvailability`와 `DemonContractFailureReason`이 계약 가능 여부, 비용 1, 계약 후 영혼, 남은 기본 횟수와 실패 원인을 덱·영혼·난수 변경 없이 제공하게 했다.
- `CoreLoopState.PlayerResolvingDemonContract`와 `PendingDemonContractInteraction`을 추가했다. 후보 세 장은 서로 다른 옵션·물리 카드 ID를 가지며 증가형 `InteractionId`가 오래된 입력을 차단한다.
- `TryBeginPlayerDemonContract`는 정상 플레이어 차례·미사용 횟수·`현재 영혼 > 비용`·후보 세 장을 먼저 확인한다. 승인 뒤에만 영혼과 횟수를 적용하고 후보를 보류 위치로 이동한다.
- 후보 공개 뒤 히트·스탠드·체인지·일반 카드·추가 계약을 잠근다. 취소 API를 만들지 않았고 잘못된 ID·옵션과 중복 입력은 상태·영혼·카드 위치를 바꾸지 않는다.
- `TryResolvePlayerDemonContract`가 선택 카드 하나를 물리 ID 기반 `ActiveDemonContract`로 등록하고 나머지 두 장을 악마 버린 더미로 보낸다. 정상 완료는 적 차례를 정확히 한 번 실행한다.
- `DemonContractResolver`와 `DemonContractContext`에는 주입 가능한 활성화 경계만 추가했다. 기본 Resolver는 사탄을 포함한 미확정 개별 악마 효과를 등록하지 않는다.
- 테스트용 처리기가 기본 비용 뒤 남은 영혼을 0으로 만들면 라운드 버스트 없이 즉시 `BattleEnded`·플레이어 패배가 되고 적 AI가 실행되지 않게 했다.
- `CoreLoopSession`과 `StageProgressionSession`이 계약 시작·선택 입력을 전달하며, 계약 대가 패배 시 지속 영혼 0과 `RunDefeat`를 한 번만 동기화한다.
- 같은 악마 정의라도 다른 물리 카드 ID와 별도 런타임 상태 객체를 가지는 모델을 검증했다. 실제 추가 계약 횟수 부여는 루시퍼·유물 범위이므로 구현하지 않았다.

### 6.2 변경 파일

- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractSelection.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractResolver.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopState.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopSession.cs`
- `Assets/01. Scripts/StageProgression/StageProgressionSession.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/DemonContractActionTests.cs`
- `Assets/06.Packages/Tests/EditMode/StageProgression/DemonContractSessionIntegrationTests.cs`
- 계약 4종·색인·AI 활용·팀 역할·프로젝트 구조 기록 문서

### 6.3 검증

- DC-02 대상: 13/13 통과(job `dd7785952657473788ff823903610e8d`).
- 전체 EditMode: 342/342 통과, 실패·건너뜀 0(job `a52e53b9d58143d4b2fb593528acd67a`).
- Unity 6000.3.10f1의 `DiaBlackJack@5635a4cdcfecc8dd`에서 에셋 갱신·도메인 리로드 뒤 컴파일 오류 0을 확인했다.
- 테스트 뒤 Console에는 Unity Test Framework 기반 시설 경고 3건과 결과 저장 `Exception` 2건만 있으며 게임 코드 오류와 구분했다.
- 데이터·상태·세션 단계라 Game View와 Play Mode는 대상이 아니다. 씬·프리팹·패키지·외부 에셋은 변경하지 않았다.

### 6.4 AI 활용과 사람 결정

AI는 DC-02 완료 게이트와 기존 체인지·카드 효과의 보류 상태, `CoreLoopSession`·`StageProgressionSession` 종료 동기화 패턴을 대조해 코드·테스트 초안과 Unity MCP 검증을 보조했다. 기본 악마 효과를 임의로 구현하지 않고 주입식 활성화 경계로 대가 사망만 검증했으며, 비용·횟수·취소 불가·필수 선택·행동 소비는 이천서가 승인한 기획 기준을 따랐다. 최종 코드·기획 승인 책임은 이천서에게 있다.

### 6.5 권장 커밋 제목

`영혼을 건 계약 선택이 취소 악용과 중복 입력 없이 한 번만 성립하게 하다`

## 7. DC-03 수행 기록

### 7.1 수행 내용

- `BelphegorDemonContractHandler`와 물리 계약별 `BelphegorRuntimeState`를 추가하고 기본 Resolver에는 확정된 벨페고르만 등록했다.
- `TryPlayerHit`는 활성 벨페고르가 있으면 드로우 전에 `BelphegorTopCard` 상호작용을 열고, 공개 옵션에는 실제 카드 ID·숫자를 넣지 않는다.
- `PlayerDemonContractPreview`에만 소유자용 카드 ID·정의 키·숫자를 제공하고, 해결 시 덱 위 물리 ID를 다시 확인한다.
- 그대로 히트는 기존 공개 드로우와 `VisibleHandValue` 버스트 경로를 재사용한다. 덱 아래 이동은 같은 카드를 드로우 더미 맨 아래로 옮기고 손패·가용 카드 수를 바꾸지 않은 채 행동을 끝낸다.
- 적 스탠드 뒤 플레이어 정상 차례 시작에 자동 스탠드를 예약하고, 그 차례의 행동 전체가 끝난 뒤 한 번만 소비한다. 라운드 또는 전투가 끝나면 예약과 미리보기를 정리한다.
- 카드 사용·체인지는 벨페고르 미리보기를 만들지 않으며, 적 관측·공용 행동 기록에는 미리보기 카드 정보가 추가되지 않는다.

### 7.2 변경 파일

- `Assets/01. Scripts/CoreLoop/BlackjackDeck.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractSelection.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractResolver.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/BelphegorDemonContractHandler.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/BelphegorDemonContractTests.cs`
- `Assets/06.Packages/Tests/EditMode/StageProgression/BelphegorStageIntegrationTests.cs`
- 계약 문서 4종·색인·AI 활용·팀 역할·프로젝트 구조 기록

### 7.3 검증

- DC-03 대상: 9/9 통과(job `4ae2493405764b6eb784424d87918ffc`).
- 전체 EditMode: 351/351 통과, 실패·건너뜀 0(job `81d2761c997a451494ec009af715d459`).
- Unity 6000.3.10f1의 `DiaBlackJack@5635a4cdcfecc8dd`에서 외부 변경 강제 갱신·도메인 리로드 뒤 컴파일 오류 0을 확인했다.
- Console에는 MCP WebSocket 미초기화 경고 1건, Unity Test Framework 사전·사후 처리 경고 4건과 결과 저장 `Exception` 2건만 남았으며 게임·컴파일 오류와 구분했다.
- 이번 단계는 순수 전투 상태·표시 모델 범위이므로 Game View와 Play Mode를 실행하지 않았다. `.unity` 씬·UI·프리팹·패키지·외부 에셋은 변경하지 않았다.

### 7.4 AI 활용과 사람 결정

AI는 이천서가 확정한 벨페고르 규칙을 기존 보류 상호작용·덱·적 관측·차례 종료 경계에 대조해 코드와 테스트 초안을 작성하고 로컬 Unity MCP 검증을 보조했다. 미리보기 정보는 플레이어 전용 모델로 분리하고 기존 공개 합 버스트·최종 전체 합·세션 전달을 재사용했다. 능력·대가의 기획 확정과 최종 코드 승인 책임은 이천서에게 있다.

### 7.5 권장 커밋 제목

`첫 계약이 덱 정보와 차례 제약을 숨은 정보 유출 없이 바꾸게 하다`

## 8. DC-04 수행 기록

### 8.1 수행 내용

- 주입 가능한 `IDemonDieRoller`, 결정적 기본 난수원과 물리 계약별 `MammonRuntimeState`를 추가했다.
- 계약 즉시 공개 굴림과 정상 차례 시작 전 유지·재굴림을 구현했다. 차례 선택은 행동을 소비하지 않고, 재굴림에서 새로 나온 6만 `ContractEffectBust`로 즉시 라운드를 끝낸다.
- 양쪽 스탠드 뒤 `MammonApplyDie` 선택이 최종 비교를 보류한다. 미적용, 적용 승리와 적용 숫자 버스트가 구분되며 에이스를 포함한 전체 손패와 주사위 값을 함께 계산한다.
- 완료된 리볼버 결과 뒤의 `IDemonContractAfterCardEffectHandler`를 추가했다. 원 리볼버 성공은 그대로 종료하고, 실패 뒤 상대 전체 합 22 이상이면 레비아탄이 상대만 계약 버스트시킨다.
- 리볼버와 레비아탄이 모두 상대를 버스트시키지 못하면 플레이어 영혼을 정확히 1 감소시키며, 0이면 추가 적 차례 없이 즉시 전투 패배로 종료한다.
- `DemonContractEffectResult`에는 `Triggered`, `BustedTarget`, `PaidSoulCost`만 공개해 상대 비공개 숫자와 전체 합을 결과·UI 경계에서 차단했다.

### 8.2 변경 파일

- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/HandValueCalculator.cs`
- `Assets/01. Scripts/CoreLoop/RoundResolver.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractSelection.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractResolver.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/MammonDemonContractHandler.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/LeviathanDemonContractHandler.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/MammonAndLeviathanDemonContractTests.cs`
- 계약 문서 4종·색인·AI 활용·팀 역할·프로젝트 구조 기록

### 8.3 검증

- DC-04 대상 테스트 11/11 통과.
- Unity 전체 EditMode 362/362 통과, 실패·건너뜀·미결정 0.
- Unity 6000.3.10f1의 열린 `DiaBlackJack` Editor에서 신규 스크립트·테스트 어셈블리와 임시 검증 러너 제거 뒤 최종 도메인 리로드·컴파일 오류 0을 확인했다.
- 현재 Codex 세션에는 프로젝트에 설치된 MCP for Unity의 도구 서버가 노출되지 않아, 열린 Editor의 공식 `TestRunnerApi`를 일회성으로 호출했다. 임시 러너·어셈블리 설정 변경은 검증 직후 제거·복구했다.
- 순수 전투 규칙 단계이므로 Game View·Play Mode는 대상이 아니다. `.unity` 씬·UI·프리팹·Packages·외부 에셋은 변경하지 않았다.

### 8.4 AI 활용과 사람 결정

AI는 이천서가 확정한 마몬·레비아탄 규칙을 기존 보류 상호작용, 라운드 해석, 리볼버 완료 결과와 영혼 고갈 경계에 대조해 코드·테스트·문서 초안을 보조했다. 리볼버 효과를 복제하지 않고 완료 결과 뒤의 계약 훅만 연결했으며, 상대 전체 합은 내부 판정에만 사용했다. 주사위 유지·재굴림·최종 적용 시점, 레비아탄 직접 버스트와 대가, 사탄·UI 제외 범위와 최종 승인 책임은 이천서에게 있다.

### 8.5 권장 커밋 제목

`계약의 난수와 카드 연계 위험이 같은 종료 규칙 아래 판정되게 하다`

## 9. DC-05 수행 기록

### 9.1 수행 내용

- 계약 가용성·비용·계약 후 영혼·남은 사용 횟수, 현재 상호작용과 후보·활성 계약·최근 결과를 묶은 `DemonContractPanelViewModel`을 추가했다.
- 비용 지불 전 확인·취소는 UI 로컬 상태로만 유지하고, 확인 뒤 기존 세션 명령을 호출해 후보 3장과 상호작용 ID를 표시한다.
- 후보 이름·능력·대가와 선택 버튼을 분리했다. 미확정 사탄은 후보에 존재하더라도 `DC-06 구현 예정`으로 비활성화하며 다른 구현 완료 악마 선택은 유지한다.
- 벨페고르의 덱 위 숫자는 `PLAYER ONLY`, 마몬의 현재 주사위와 레비아탄 조건은 활성 계약 상태로 표시하고 상대 비공개 합은 노출하지 않는다.
- `CoreLoopView`와 `GameManager`에 계약 확인·후보/후속 선택·활성 결과·입력 잠금을 연결했다. 독립 전투는 네 장 프로토타입 덱, 런 전투는 기존 `StageProgressionSession` 전달·종료 동기화를 사용한다.
- 첫 1280×720 GameScene 캡처에서 긴 비용·설명을 버튼 하나에 넣어 잘린 문제를 발견했다. 일반 행동은 짧은 버튼과 별도 비용 안내로, 후보는 제목·능력·대가·선택 버튼이 분리된 반응형 카드로 수정했다.

### 9.2 변경 파일

- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractPresentation.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractSelection.cs`
- `Assets/01. Scripts/CoreLoop/DemonContracts/DemonContractDeck.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopPresentation.cs`
- `Assets/01. Scripts/UI/CoreLoop/CoreLoopView.cs`
- `Assets/01. Scripts/UI/CoreLoop/CoreLoopController.cs`
- `Assets/01. Scripts/GameScene/GameManager.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/DemonContractPresentationTests.cs`
- 계약 문서 4종·색인·AI 활용·팀 역할·프로젝트 구조와 시각 판정 기록

### 9.3 검증

- DC-05 대상 테스트 7/7 통과(job `6c374216652b415abf5278a0b4125cd1`).
- Unity 전체 EditMode 369/369 통과, 실패·건너뜀 0(job `a4889b47af04497da60243494220f866`).
- Unity MCP 표준 HTTP 세션 `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1에서 컴파일 오류 0을 확인했다.
- `GameScene`과 `CoreLoopTest`의 계약 후보 화면을 실제 1280×720·1920×1080에서 확인했다. 이름·능력·대가·선택 버튼은 잘림 없이 표시되고 사탄 비활성·구현 완료 후보 선택 가능 상태가 구분된다.
- 시각 판정은 최초 58점 수정 필요에서 레이아웃 교정 뒤 94점 통과로 갱신했다.
- 테스트 뒤 Test Framework 사전·사후 경고 4건과 결과 저장 안내 `Exception` 2건은 기반 시설 출력으로 분리했고 Console을 비운 뒤 Error/Warning 0을 확인했다.
- `.unity` 씬·프리팹·Packages·외부 에셋은 변경하지 않았다.

### 9.4 AI 활용과 사람 결정

AI는 계약 문서의 DC-05 완료 게이트를 현재 Presenter·IMGUI View·Controller·독립/런 세션에 대조해 표시 모델, 입력 연결, 테스트와 두 해상도 시각 교정을 보조했다. Unity MCP로 리소스·Editor 상태를 확인하고 테스트·Play Mode·Game View·Console을 검증했다. 비용 확인 뒤 취소 가능, 지불 뒤 필수 선택, 사탄 비활성화와 최종 화면·코드 승인 책임은 이천서에게 있다.

### 9.5 권장 커밋 제목

`계약의 이득과 대가를 확인한 선택이 실제 런 결과까지 이어지게 하다`

## 10. 결정 대장

| ID | 날짜 | 상태 | 결정·쟁점 | 근거·다음 조치 |
| --- | --- | --- | --- | --- |
| DC-D01 | 2026-07-22 | 프로토타입 | 시작 악마 덱은 사탄·벨페고르·마몬·레비아탄 각 1장 | 원문 미정, MVP 네 악마의 노출과 후보 3장 보장 |
| DC-D02 | 2026-07-22 | 프로토타입 | 계약은 정상 행동 1회 소비 | 카드 사용과 같은 차례 리듬, 첫 플레이 테스트 뒤 재검토 |
| DC-D03 | 2026-07-22 | 프로토타입 | 비용 지불 후 후보 공개 뒤 취소·환불 불가 | 정보만 확인하고 되돌리는 악용 방지 |
| DC-D04 | 2026-07-22 | 프로토타입 | 레비아탄은 직접 효과에 한해 상대 전체 합을 내부 판정 | 일반 중간 버스트와 구분, UI에는 성공 여부만 공개 |
| DC-D05 | 2026-07-22 | 미확정 | 사탄 카운터 증가/감소, 4/6, 종료/반복 | DC-06 전에 이천서 확정 필요 |
| DC-D06 | 2026-07-22 | 부분 확정 | 사탄 권능은 숫자 합계 포함·전투 종료 제거, 양면 물리 카드·뒤집기 사용 상태는 미확정 | DC-06 전에 남은 항목 확정 필요 |
| DC-D07 | 2026-07-22 | 부분 확정 | 동일 악마 추가 계약 허용·별도 인스턴스, 카드별 효과·대가 계산은 미확정 | 추가 계약 콘텐츠 착수 전 세부 설계 |
| DC-D08 | 2026-07-22 | 확정 | 생성·이동·추방 카드는 전투 동안 유지하고 종료 시 생성 제거·전투 시작 덱 구성 복구·탈취 원소유권 복구 | 런 덱 영구 변경 없음 |
| DC-D09 | 2026-07-22 | 확정 | `할 수 있다`는 선택, 모든 유형의 버스트 포함, 개별 대가로 영혼 0 즉시 패배 | 적에게도 대칭 적용 |
| DC-D10 | 2026-07-22 | 미확정 | 파이몬 발동 연쇄로 양측이 버스트할 때의 피해·승패 우선순위 | 파이몬 개별 개정 전 확정 |

## 11. 검증 누적표

| 단계 | 대상 테스트 | 전체 EditMode | Game View | 씬 | Console | 비고 |
| --- | --- | --- | --- | --- | --- | --- |
| 착수 기준 | CU-M03 신규 1/1 | 310/310 | 양측 비공개 최좌측 통과 | `GameScene.unity` 무변경 | 오류 0 | 선행 작업 기록 |
| DC-00 | 미실행 | 미실행 | 미실행 | 미실행 | 미실행 | 문서 전용 |
| DC-01 | 19/19 | 329/329 | 해당 없음 | 씬 에셋 무변경 | 컴파일 오류 0·테스트 기반 시설 6건 | 데이터·덱 완료 |
| DC-02 | 13/13 | 342/342 | 해당 없음 | 씬 에셋 무변경 | 컴파일 오류 0·테스트 기반 시설 5건 | 상태·세션 완료 |
| DC-03 | 9/9 | 351/351 | 해당 없음 | 씬 에셋 무변경 | 컴파일 오류 0·기반 시설 7건 | 벨페고르 수직 기능 완료 |
| DC-04 | 11/11 | 362/362 | 결과 모델 확인 | 씬·UI 무변경 | 컴파일 오류 0 | 마몬·레비아탄 완료 |
| DC-05 | 7/7 | 369/369 | 1280×720·1920×1080 통과 | `GameScene`·`CoreLoopTest` 통과 | 최종 0 | 플레이어 UI·런 완료 |
| DC-06 | 예정 | 예정 | 예정 | 두 씬 예정 | 예정 | 사탄 |
| DC-07 | 예정 | 예정 | 두 해상도 예정 | 최종 예정 | 예정 | 적 AI·반복 검증 |

## 12. 구현 완료 기록 양식

```text
### DC-0N — 작업명

- 작업자:
- 날짜:
- 변경 파일:
- 구현 내용:
- 직접 결정·해결한 문제:
- 기존 시스템 변경과 이유:
- AI 보조 범위:
- 대상 테스트 결과:
- 전체 회귀 결과:
- 실제 씬·해상도·Console 결과:
- 외부 에셋·오픈소스 변경:
- 제외·잔여 위험:
- 이천서 최종 검토:
- 권장 커밋 제목:
```

## 13. 현재 위험

| 위험 | 현재 대응 |
| --- | --- |
| 사탄 상충 규칙을 임의 구현 | DC-06 착수 게이트로 차단 |
| 계약을 카드 효과에 과도하게 일반화 | 별도 정의·덱·Resolver와 명시적 결과 훅 사용 |
| 비용만 지불하고 후보 생성 실패 | 후보 3장 사전 검증 뒤 비용 처리 |
| 숨은 정보가 UI·AI에 노출 | 안전한 결과·표시 모델과 정보 은닉 테스트 |
| 마몬 최종 선택으로 상태 교착 | 명시적 보류 상호작용과 오래된 ID 거절 |
| HONG의 정식 런·상점 작업과 충돌 | RunFlow·Shop 영역 미변경, 악마 런 덱 경계만 소유 |
| 계약 효과가 기존 중간 버스트를 변경 | 레비아탄 직접 효과만 예외로 기록하고 일반 판정 회귀 유지 |
| 계약 임시 카드가 런에 남음 | 물리 출처 추적과 전투 종료 생성 제거·추방 복구·원소유권 복구 |
| 동일 악마 상태가 합쳐짐 | 계약 인스턴스 ID별 카운터·주사위·대가 분리 |

## 14. 다음 작업

다음 작업은 **DC-06 사탄 규칙 결정**이다. 구현에 앞서 종말 카운터의 증가·감소 방향, 시작값 4/6, 0·6 도달 뒤 계약 종료 또는 반복, 권능 카드의 양면 물리 구조와 뒤집힌 면 사용 상태를 이천서가 확정해야 한다.

결정 전에는 사탄 처리기·카운터·권능 카드를 구현하지 않는다. DC-05 화면은 사탄 후보를 `DC-06 구현 예정`으로 표시하고 선택을 막아 미확정 규칙이 작동하는 것처럼 보이지 않게 한다.

## 15. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-22 | 이천서 | DC-00 기준선·결정 대장·검증 누적표·다음 작업과 완료 기록 양식 수립 |
| 2026-07-22 | 이천서 | 악마 카드 공통 선택·버스트·개별 대가·생성 카드 합계·동일 악마 추가 계약과 전투 한정 임시 카드 수명 개정 기록 |
| 2026-07-22 | 이천서 | DC-01 네 정의·런/전투 덱·후보 이동·보충·전투 변환 구현과 대상 19/19·전체 329/329·컴파일 오류 0 기록, 테스트 기반 시설 출력 6건을 분리하고 다음 작업을 DC-02로 전환 |
| 2026-07-22 | 이천서 | DC-02 비용·횟수 가용성, 필수 후보 선택·상호작용 ID·활성 계약·대가 사망·세션 전달 구현과 대상 13/13·전체 342/342 기록, 다음 작업을 DC-03으로 전환 |
| 2026-07-22 | 이천서 | DC-03 벨페고르 플레이어 전용 미리보기·동일 ID 히트/덱 이동·행동 후 자동 스탠드·정보 은닉 구현과 대상 9/9·전체 351/351 기록, 다음 작업을 DC-04로 전환 |
| 2026-07-22 | 이천서 | DC-04 마몬 주입식 주사위·차례/최종 선택, 레비아탄 리볼버 후속 버스트·영혼 대가·정보 은닉 구현과 대상 11/11·전체 362/362 기록, 다음 작업을 DC-05로 전환 |
| 2026-07-23 | 이천서 | DC-05 계약 비용 확인·후보 설명·활성 상태·소유자 전용 미리보기·독립/런 UI를 구현하고 대상 7/7·전체 369/369·두 씬·두 해상도·Console 0 뒤 다음 작업을 DC-06 결정으로 전환 |
