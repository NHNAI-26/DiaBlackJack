# 자동 발동 카드 시스템 진행 기록

> 프로젝트: DiaBlackJack  
> 기획·기록·구현 책임자: 이천서  
> 작업 식별자: AC-00~AC-06  
> 버전: v0.1  
> 현재 단계: AC-04 화염 방사기와 회중시계 완료
> 다음 단계: AC-05 부활초 라운드 초기화
> 최종 갱신: 2026-07-25

## 1. 기록 원칙

- 계획과 실제 구현을 분리한다.
- 코드가 없는 문서 단계는 구현 완료로 기록하지 않는다.
- 단계마다 작업자, 변경 파일, 결정한 문제, 대상·전체 테스트와 실제 화면 결과를 남긴다.
- AI 대화 원문은 복사하지 않고 목적, 지시, 결과와 사람의 결정을 기술적으로 정리한다.
- 기획·코드·검증의 최종 승인 책임자는 이천서로 기록한다.
- 팀원의 예정 업무는 실제 변경과 검증이 확인되기 전까지 완료 기여로 기록하지 않는다.
- 사용자의 명시적 요청 전에는 스테이징·커밋·푸시하지 않는다.

## 2. 현재 기준선

| 항목 | 현재 상태 |
| --- | --- |
| 일반 전투 | 히트·스탠드·체인지·버스트·최종 비교 구현 완료 |
| 수동 카드 | 수정 구슬·위협용 해머·리볼버·보위 나이프 구현 완료 |
| 특수 수동 카드 | 사탄의 권능 양면 효과 구현 완료 |
| 자동 카드 분류 | `CardActivationKind.Automatic`과 효과 종류 5종 존재 |
| 자동 카드 정의·실행 | 5종 정의 완료, 독극물·거짓말 탐지기·화염 방사기·회중시계 실제 처리기 구현 완료·부활초 미구현 |
| 자동 공개 유입 조정 | 플레이어·적 히트, 수정 구슬, 보위 나이프·사탄 권능 강제 드로우 연결 완료 |
| 플레이어 자동 선택 UI | 미구현 |
| 적 자동 선택 정책 | 미구현 |
| 런·보상 자동 카드 | 미등록 |
| 직전 자동 기준선 | DC-08 전체 EditMode 397/397 통과 기록 |
| AC-01 검증 | 대상 15/15·CoreLoop 283/283·전체 EditMode 412/412·컴파일 오류 0 |
| AC-02 검증 | 대상 12/12·CoreLoop 295/295·전체 EditMode 424/424·컴파일 오류 0 |
| AC-03 검증 | 대상 10/10·CoreLoop 305/305·전체 EditMode 434/434·컴파일 오류 0·Test Framework 결과 저장 안내 3건 |
| AC-04 검증 | 대상 11/11·CoreLoop 316/316·전체 EditMode 445/445·컴파일 오류 0 |

AC-02 착수 시 작업 트리는 깨끗했고 AC-01 커밋 `dd09d89`를 기준으로 삼았다. 열린 Unity의 MCP WebSocket이 ping에 응답하지 않아 저장되지 않은 편집기 상태를 위험에 빠뜨리는 강제 재시작은 하지 않았다. 동일 커밋의 임시 Git worktree에 AC-02 변경만 복제하고 Unity 6000.3.10f1 Headless EditMode로 컴파일과 테스트를 검증했다. 이번 단계는 CoreLoop 순수 C#과 EditMode 테스트·기록만 변경했으며 UI·진행 세션·씬·프리팹·Packages·외부 에셋은 변경하지 않았다.

AC-03 착수 시 작업 트리는 깨끗했고 Unity MCP의 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스가 Unity 6000.3.10f1, 비재생·비컴파일·도구 사용 가능 상태임을 확인했다. `GameScene`은 열린 상태만 확인하고 수정하지 않았다. 로컬 Unity에서 컴파일과 대상·CoreLoop·전체 EditMode를 검증했으며 UI·진행 세션·보상·적 자동 선언 정책·씬·프리팹·Packages·외부 에셋은 변경하지 않았다.

AC-04 착수 시 작업 트리는 깨끗했고 AC-03 커밋 `a2abc84`를 기준으로 삼았다. Unity MCP의 같은 인스턴스에서 Unity 6000.3.10f1, 비재생·비컴파일·도구 사용 가능 상태와 활성 `GameScene`을 확인했다. `GameScene`은 열려 있는 상태만 확인하고 수정하지 않았다. 로컬 Unity에서 컴파일과 대상·CoreLoop·전체 EditMode를 검증했으며 UI·진행 세션·보상·적 자동 선택 정책·씬·프리팹·Packages·외부 에셋은 변경하지 않았다.

## 3. 단계 현황

| 단계 | 담당 | 상태 | 완료 증거 |
| --- | --- | --- | --- |
| AC-00 | 이천서(AI 문서·구조 대조 보조) | 완료 | 문서 4종·색인·AI 활용·역할 기록 |
| AC-01 | 이천서(AI 구현·검증 보조) | 완료 | 대상 15/15·CoreLoop 283/283·전체 412/412 |
| AC-02 | 이천서(AI 구현·검증 보조) | 완료 | 대상 12/12·CoreLoop 295/295·전체 424/424 |
| AC-03 | 이천서(AI 구현·검증 보조) | 완료 | 대상 10/10·CoreLoop 305/305·전체 434/434 |
| AC-04 | 이천서(AI 구현·검증 보조) | 완료 | 대상 11/11·CoreLoop 316/316·전체 445/445 |
| AC-05 | 이천서(AI 구현·검증 보조) | 미착수 | 부활초 라운드 초기화 대상 및 전체 테스트 |
| AC-06 | 이천서(AI 구현·화면·검증·기록 보조) | 미착수 | UI·런·보상·적 AI·반복 회귀·문서 마감 |

## 4. AC-00 수행 기록

### 4.1 수행 내용

- `rule.md`의 자동 발동 카드 5종과 현재 보류 사항을 확인했다.
- `game-design-document.md`의 최초 배분 비발동, 공개 유입, 기본 폐기와 버스트 순서를 기준으로 삼았다.
- 현재 `CardDefinitionCatalog`, `CardEffectResolver`, `CoreLoopBattle`, 카드 처리기, 적 AI, 진행 세션과 보상 카탈로그를 대조했다.
- 자동 발동을 수동 카드·계약과 분리된 시스템으로 정의했다.
- 기획서, 개발 명세서, 구현 계획서와 본 진행 기록을 작성했다.
- 이천서를 기획·개발 책임자로 기록하고 HONG의 골드·상점·정식 런, Shim0Hwan의 아트 영역을 제외했다.

### 4.2 확정한 프로토타입 결정

| 결정 | 결과 |
| --- | --- |
| 최초 배분 | 공개·비공개 모두 자동 발동하지 않음 |
| 발동 유입 | 최초 배분 뒤 공개 영역에 새로 들어온 카드 |
| 처리 순서 | 자동 효과 → 원본 위치 → 공개 합 버스트 → 바깥 처리 재개 |
| 기본 위치 | 효과 뒤 소유자 버린 더미 |
| 독극물 스탠드 충돌 | 계약의 스탠드 금지 우선 |
| 독극물 영혼 | 0까지 지불 가능, 0이면 즉시 전투 패배 |
| 부활초 영혼 1 | 양측 모두 2 이상일 때만 재시작 허용 |
| 부활초 종료 | 승패·일반 피해 없는 라운드 초기화 |
| 탐지기 정보 | 선언 공개, 이상·미만 결과는 소유자 전용 |
| 화염 방사기 순서 | 소유자 선택 뒤 상대 선택 |
| 회중시계 순환 | 사용 완료 수동 카드만 대상, 자동 카드 제외 |
| 런 등장 | 구현 완료 뒤 일반 보상 풀에 추가, 시작 덱·높은 등급 풀 유지 |

### 4.3 생성·변경 문서

- `Docs/automatic-card-design.md`
- `Docs/automatic-card-development-spec.md`
- `Docs/automatic-card-implementation-plan.md`
- `Docs/automatic-card-progress-log.md`
- `Docs/README.md`
- `Docs/ai-usage-technical-document.md`
- `Docs/team-role-technical-document.md`

### 4.4 검증 결과

| 검증 | 결과 |
| --- | --- |
| 문서 작업 ID | AC-00~AC-06 일치 |
| 책임자 | 네 문서 모두 이천서 명시 |
| 현재 단계 | AC-00 문서 완료·구현 미착수로 일치 |
| 다음 단계 | AC-01 공개 유입·자동 선택 기반으로 일치 |
| 카드 범위 | 독극물·부활초·거짓말 탐지기·화염 방사기·회중시계 5종 일치 |
| 코드·테스트 | 변경·실행 없음 |
| 씬·패키지·외부 에셋 | 변경 없음 |
| Unity | 재실행하지 않음 |

### 4.5 완료 판정

AC-00은 문서 단계로 완료했다. 자동 카드의 실제 정의·실행·UI·AI·보상 연결은 구현되지 않았으며, 다음 작업은 AC-01이다.

### 4.6 권장 커밋 제목

`자동 카드가 전투 흐름을 끊지 않도록 발동 순서와 재개 경계를 고정하다`

## 5. AC-01 수행 기록

### 5.1 수행 내용

- `poison-2`, `resurrection-herb-2`, `lie-detector-3`, `flamethrower-9`, `pocket-watch-9` 정의와 효과 종류를 추가했다.
- 수동 카드 처리기와 분리된 `AutomaticCardEffectResolver` 등록 경계와 주입식 테스트 처리기를 추가했다.
- 선택 주체·증가형 상호작용 ID·타입 선택지를 가진 `PendingAutomaticCardInteraction`과 공개 완료 결과를 추가했다.
- `ResolvingAutomaticCardEffect` 중 히트·스탠드·체인지 등 일반 입력을 거절하고 오래된 ID·중복 선택을 무변경 거절하게 했다.
- 플레이어·적 히트, 수정 구슬의 선택 공개, 보위 나이프·사탄 권능의 강제 공개 드로우를 같은 자동 해결 경계에 연결했다.
- 자동 카드 선택이 수동 카드 효과 안에서 대기할 때 원래 효과의 남은 판정과 행동 후속 처리를 한 번만 재개하게 했다.
- 최초 공개 배분, 체인지 결과, 위협용 해머의 비공개 교체는 발동 경계를 통과하지 않게 유지했다.

### 5.2 변경 파일

- `Assets/01. Scripts/CoreLoop/AutomaticCardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardSelection.cs`
- `Assets/01. Scripts/CoreLoop/CardDefinitionCatalog.cs`
- `Assets/01. Scripts/CoreLoop/CardEffectKind.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopState.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/CardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/CrystalOrbEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/MilitaryKnifeEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/SatanPowerEffectHandler.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/AutomaticCardFoundationTests.cs`
- 기존 카탈로그·적 정책 기대를 현행 정의 수에 맞춘 회귀 테스트

### 5.3 검증 결과

| 검증 | 결과 |
| --- | --- |
| AC-01 대상 | 15/15 통과 |
| CoreLoop EditMode | 283/283 통과 |
| 전체 EditMode | 412/412 통과 |
| Unity 컴파일 | 오류 0 |
| Console | 게임 코드 오류 0, Test Framework 실행 안내 경고만 확인 |
| 씬·프리팹·패키지·외부 에셋 | 변경 없음 |

### 5.4 제외·다음 단계

독극물 등 실제 자동 카드 효과, UI·세션 입력 전달, 보상 풀, 적 프로필·자동 선택 정책은 구현하지 않았다. 다음 단계는 AC-02 독극물의 스탠드/영혼 선택과 라운드 승리 회복 예약이다.

### 5.5 권장 커밋 제목

`공개 카드의 자동 효과가 모든 드로우 경로에서 한 번만 해결되게 하다`

## 6. AC-02 수행 기록

### 6.1 수행 내용

- 기본 자동 Resolver에 `PoisonEffectHandler`를 등록하고 소유자 방향 하나로 플레이어·적 양측을 처리했다.
- 실제 활성 계약을 조회해 스탠드 가능할 때만 `지금 즉시 스탠드` 옵션을 제공하고, 해결 시에도 다시 검증했다.
- 영혼 지불은 `min(3, 현재 영혼)`을 적용해 정확히 3·2·1에서도 0까지 내려가게 했다.
- 영혼 0에서는 독극물 원본을 버리고 활성 수동 카드 효과·자동 연속 처리·원래 행동과 적 후속 행동을 취소한 뒤 전투를 끝냈다.
- 생존 지불은 물리 카드 ID·소유자·라운드·회복량 5를 `AutomaticCardBattleState`에 발동 순서대로 보존했다.
- 라운드 피해 적용 뒤 살아 있는 승자에게만 예약을 적용하고 `SoulPool.Restore`로 최대 영혼을 넘지 않게 했다.
- 패배·새 라운드·전투 종료에서 예약을 정리했다. 부활초의 실제 승패 없는 초기화 호출은 AC-05에서 같은 정리 경계에 연결한다.

### 6.2 변경 파일

- `Assets/01. Scripts/CoreLoop/PoisonEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardBattleState.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/SoulPool.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/PoisonAutomaticCardTests.cs`
- 자동 발동 문서 4종과 공통 AI 활용·역할·구조 기록

### 6.3 검증 결과

| 검증 | 결과 |
| --- | --- |
| AC-02 대상 | 12/12 통과 |
| CoreLoop EditMode | 295/295 통과 |
| 전체 EditMode | 424/424 통과 |
| Unity 컴파일 | 오류 0 |
| 실행 환경 | Unity 6000.3.10f1 별도 Headless 검증 worktree |
| 규칙 경계 | 사탄 스탠드 제한, 영혼 6·3·2·1, 부모 수정 구슬 취소, 승리·패배·중첩·적 대칭 통과 |
| 씬·프리팹·Packages·외부 에셋 | 변경 없음 |

### 6.4 제외·다음 단계

독극물 UI 입력, 적 자동 선택 정책, 진행 세션·보상 연결은 AC-06 범위로 유지했다. 부활초가 독극물 회복 예약을 버리는 실제 라운드 초기화 연결은 AC-05에서 검증한다. 다음 단계는 AC-03 거짓말 탐지기의 선언 선택, 소유자 전용 비교 지식과 숨은 정보 폐기다.

### 6.5 권장 커밋 제목

`독극물의 생존 선택이 계약 제한과 영혼 위험을 정확히 감수하게 하다`

## 7. AC-03 수행 기록

### 7.1 수행 내용

- 기본 자동 Resolver에 `LieDetectorEffectHandler`를 등록하고 1~10 숫자 선언 옵션을 제공했다.
- 상대 비공개 카드가 정확히 한 장일 때만 `Rank >= DeclaredNumber`를 계산하고 실제 숫자는 결과에 복사하지 않았다.
- 공개 결과에는 소유자·선언 숫자·판정 가능 여부만, 소유자 전용 지식에는 관측자·대상·선언·이상 여부·라운드만 공개했다.
- 대상 숨은 카드 ID는 내부 무효화 키로만 보존하고 공개 속성과 적 관측에서는 감췄다.
- 플레이어 체인지, 플레이어·적 위협용 해머 교체, 새 라운드와 전투 종료에서 이전 비교 지식을 폐기했다.
- 적 소유 결과는 플레이어 전용 접근자에 노출하지 않고 `EnemyObservation.LieDetectorComparisonKnowledge`에만 전달했다.
- 비공개 카드가 0장 또는 여러 장인 경우 실제 숫자를 읽지 않고 판정 불가로 끝낸 뒤 원본을 버렸다.

### 7.2 변경 파일

- `Assets/01. Scripts/CoreLoop/LieDetectorEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/LieDetectorResult.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardBattleState.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/CoreLoopBattle.cs`
- `Assets/01. Scripts/CoreLoop/CardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/PlayerChangeSelection.cs`
- `Assets/01. Scripts/CoreLoop/EnemyAI/EnemyObservation.cs`
- `Assets/01. Scripts/CoreLoop/EnemyAI/EnemyObservationFactory.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/LieDetectorAutomaticCardTests.cs`
- 자동 발동 문서 4종과 공통 AI 활용·역할·구조 기록

### 7.3 검증 결과

| 검증 | 결과 |
| --- | --- |
| AC-03 대상 | 10/10 통과 |
| CoreLoop EditMode | 305/305 통과 |
| 전체 EditMode | 434/434 통과 |
| Unity 컴파일·Console | 컴파일 오류 0, Test Framework 결과 저장 안내 3건만 확인 |
| 규칙 경계 | 1~10·이상/미만·양측 소유·판정 불가·체인지·해머·새 라운드·전투 종료 통과 |
| 정보 경계 | 공용 결과에 실제 숫자·비교 결과 없음, 적 관측은 합법적 비교 조건만 포함 |
| 씬·프리팹·Packages·외부 에셋 | 변경 없음 |

### 7.4 제외·다음 단계

플레이어 자동 선택 UI, 진행 세션·보상 연결, 적 자동 선언 정책과 사기꾼 덱 등록은 AC-06 범위로 유지했다. 다음 단계는 AC-04 화염 방사기의 소유자→상대 순차 폐기 선택과 회중시계의 사용 완료 수동 카드 재활성화다.

### 7.5 권장 커밋 제목

`탐지기가 허용한 비교 정보만 소유자에게 남기도록 숨은 값 경계를 지키다`

## 8. AC-04 수행 기록

### 8.1 수행 내용

- 기본 자동 Resolver에 `FlamethrowerEffectHandler`와 `PocketWatchEffectHandler`를 등록했다.
- 화염 방사기는 소유자→상대 순으로 공개 카드 폐기 또는 건너뛰기를 요청하고, 각 단계에서 스탠드와 현재 후보를 다시 조회한다.
- 원본 화염 방사기는 소유자 후보에서 제외하며, 선택된 공개 카드는 선택 주체의 실제 손패를 재검증한 뒤 해당 소유자의 버린 더미로 옮긴다.
- 첫 폐기를 반영한 뒤 상대 후보를 새로 만들며, 양측 단계가 끝난 뒤 원본을 버리고 기존 공개 합 버스트·행동 재개 경계를 사용한다.
- `BlackjackCard.TryReactivate`를 추가해 수동 `Used` 카드만 `Available`로 되돌리게 했다.
- 회중시계는 소유자의 공개된 사용 완료 수동 카드만 후보로 만들고, 자동 카드·자기 자신·이미 사용 가능한 카드를 제외한다.
- 대상 재활성화 또는 건너뛰기 뒤 원본 유지·폐기를 별도 선택으로 처리한다. 유지는 원본의 자동 `Unavailable` 상태와 공개 위치를 보존해 같은 손 재발동을 만들지 않는다.
- 플레이어·적 소유자 방향을 같은 처리기로 검증하되 적 자동 선택 정책은 구현하지 않았다.

### 8.2 변경 파일

- `Assets/01. Scripts/CoreLoop/FlamethrowerEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/PocketWatchEffectHandler.cs`
- `Assets/01. Scripts/CoreLoop/AutomaticCardEffectResolver.cs`
- `Assets/01. Scripts/CoreLoop/BlackjackCard.cs`
- `Assets/06.Packages/Tests/EditMode/CoreLoop/FlamethrowerAndPocketWatchTests.cs`
- 자동 발동 문서 4종과 공통 README·AI 활용·역할·구조 기록

### 8.3 검증 결과

| 검증 | 결과 |
| --- | --- |
| AC-04 대상 | 11/11 통과 |
| CoreLoop EditMode | 316/316 통과 |
| 전체 EditMode | 445/445 통과 |
| Unity 컴파일·Console | 최종 컴파일 오류 0 |
| 화염 방사기 경계 | 소유자→상대, 스탠드·후보 없음 건너뛰기, 원본 제외, 각 소유자 버린 더미, 원본 폐기 뒤 버스트 통과 |
| 회중시계 경계 | `Manual + Used` 한정, 자동·원본·Available 제외, 유지·폐기 합계, 같은 손 비재발동, 적 소유 대칭 통과 |
| 씬·프리팹·Packages·외부 에셋 | 변경 없음 |

### 8.4 제외·다음 단계

플레이어 자동 선택 UI, 진행 세션·보상 연결, 적 자동 폐기·재활성화 정책은 AC-06 범위로 유지했다. 다음 단계는 AC-05 부활초가 양측 영혼과 라운드 상태를 승패 없이 한 번만 초기화하고, 독극물 예약과 부모 수동 효과 재개를 취소하는 경계다.

### 8.5 권장 커밋 제목

`양측 카드 폐기와 재사용 선택이 순서와 소유권을 잃지 않게 하다`

## 9. 검증 누적표

| 단계 | 대상 테스트 | CoreLoop | StageProgression | 전체 EditMode | 화면·씬·Console | 비고 |
| --- | --- | --- | --- | --- | --- | --- |
| 착수 기준 | DC-08 대상 8/8 | 268/268 | 선행 통과 | 397/397 | GameScene Full HD·Error/Warning 0 | 선행 기록 |
| AC-00 | 미실행 | 미실행 | 미실행 | 미실행 | 미실행 | 문서 전용 |
| AC-01 | 15/15 | 283/283 | 129/129 | 412/412 | 컴파일 오류 0·씬 무변경 | 공통 기반 완료 |
| AC-02 | 12/12 | 295/295 | 129/129(전체 포함) | 424/424 | Headless 컴파일 오류 0·씬 무변경 | 독극물 완료 |
| AC-03 | 10/10 | 305/305 | 129/129(전체 포함) | 434/434 | 컴파일 오류 0·Test Framework 저장 안내 3건·씬 무변경 | 탐지기 완료 |
| AC-04 | 11/11 | 316/316 | 129/129(전체 포함) | 445/445 | 컴파일 오류 0·씬 무변경 | 화염 방사기·회중시계 완료 |
| AC-05 | 미착수 | 미착수 | 해당 없음 | 미착수 | 전이 결과 확인 예정 | 부활초 |
| AC-06 | 미착수 | 미착수 | 미착수 | 미착수 | 두 씬·두 해상도·Console 예정 | 통합 마감 |

## 10. 결정 및 문제 대장

| ID | 항목 | 상태 | 결정·대응 | 재검토 조건 |
| --- | --- | --- | --- | --- |
| AC-D01 | 최초 공개 배분 자동 카드 위치 | 임시 확정 | 발동 없이 공개 영역 유지 | 첫 플레이 테스트 |
| AC-D02 | 독극물과 스탠드 금지 계약 | 임시 확정 | 계약 제한 우선 | 새로운 스탠드 강제 효과 추가 시 |
| AC-D03 | 부활초 영혼 1과 동시 사망 | 임시 확정 | 양측 2 이상에서만 재시작 | 동시 사망 결과 모델 도입 시 |
| AC-D04 | 탐지기 결과 공개 범위 | 임시 확정 | 소유자 전용 비교 결과 | PvP·관전자 UI 도입 시 |
| AC-D05 | 화염 방사기 선택 순서 | 임시 확정 | 소유자→상대 | 동시 선택 시스템 도입 시 |
| AC-D06 | 회중시계 대상과 순환 | 임시 확정 | 수동 `Used`만 대상 | 자동 카드 상태 모델 변경 시 |
| AC-D07 | 보상 등급 | 임시 확정 | 일반 풀만 추가 | 카드 등급 체계 확정 시 |
| AC-R01 | 공개 유입 직접 호출이 여러 파일에 분산 | 대응 완료 | AC-01 공통 자동 해결 경계와 열거형 재개로 이관 | 새 공개 유입 경로 추가 시 |
| AC-R02 | 부활초가 바깥 효과를 중단해야 함 | 대응 예정 | 전용 연속 처리 취소 결과 | AC-05 완료 게이트 |
| AC-R03 | 열린 Unity MCP ping 단절 | 우회 완료 | 편집기 강제 종료 없이 임시 worktree의 별도 Headless Unity로 동일 소스 컴파일·테스트 | 본 작업공간 MCP 재연결 시 재확인 |

## 11. 다음 작업 — AC-05

AC-05에서는 다음만 구현한다.

- `ResurrectionHerbEffectHandler`
- 양측 영혼 2 이상 조건과 재시작·거절 선택
- 승패·일반 피해·정확히 21 보너스·독극물 회복 없는 라운드 초기화
- 양측 영혼 1 감소와 손패·스탠드·자동 상태·탐지기 지식·독극물 예약 정리
- 계약 라운드 종료 훅 한 번 호출
- 부모 수동 효과·자동 연속 처리·차례 재개 취소
- 새 라운드 최초 배분 비발동과 플레이어·적 대칭 회귀

UI, 진행 세션, 적 자동 선택 정책, 보상 풀과 적 프로필은 AC-05에서 구현하지 않는다.

## 12. 단계별 완료 기록 양식

```text
### AC-0N — 작업명

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

## 13. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-25 | 이천서 | AC-04 화염 방사기 소유자→상대 순차 폐기·회중시계 수동 카드 재활성화와 원본 유지/폐기를 구현하고 대상 11/11·CoreLoop 316/316·전체 445/445로 검증, 다음 단계를 AC-05로 전환 |
| 2026-07-25 | 이천서 | AC-03 거짓말 탐지기 선언·소유자 전용 비교·지식 폐기를 구현하고 대상 10/10·CoreLoop 305/305·전체 434/434로 검증, 다음 단계를 AC-04로 전환 |
| 2026-07-25 | 이천서 | AC-02 독극물 계약 제한 선택·영혼 0 즉시 패배·물리 카드별 승리 회복과 양측 대칭을 구현하고 대상 12/12·CoreLoop 295/295·전체 424/424로 검증, 다음 단계를 AC-03으로 전환 |
| 2026-07-25 | 이천서 | AC-01 자동 카드 정의·공개 유입·보류 선택·입력 잠금·수동 효과 재개 기반 구현과 대상 15/15·CoreLoop 283/283·전체 412/412 검증, 다음 단계를 AC-02로 전환 |
| 2026-07-23 | 이천서 | AC-00 기준선·프로토타입 결정·검증 누적표·문제 대장·AC-01 착수 범위와 완료 기록 양식 수립 |
