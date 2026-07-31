# MainMenuScene + GameScene 전체 게임 플로우 통합 계획

작성일: 2026-07-31  
범위: 제품 시작 화면은 `MainMenuScene`, 실제 런 전체는 `GameScene`에서 진행.

> 2026-07-31 현재 상태: `MainMenuScene`과 저장 흐름 기반 새 런·이어하기·시작 예약 재개 진입을 구현했다. GF-01 흐름 계약 테스트와 GF-02 단일 씬 흐름 제어 기반도 완료했다. 성공한 메뉴 입력은 이제 `GameScene`으로 이동하며, `GameFlowController`가 시작 악마 공개·상대 선택·전투·상점·결과 화면을 도메인 상태에서 판정한다. 실제 시작 악마 공개·상대 선택 월드 UI는 GF-03 범위다.

## 1. 목표

제품은 `MainMenuScene`에서 시작한다. 메뉴에서 새 게임 또는 이어하기를 선택하면 `GameScene`으로 이동하고, 이후 정식 런은 다른 진행 씬으로 왕복하지 않고 GameScene 안에서 끝까지 진행한다.

1. `MainMenuScene`: 새 게임/이어하기/시작 예약 재개/종료. 설정은 후속 구현 자리만 비활성 표시
2. `GameScene`: 시작 악마 2장 자동 지급·공개
3. 상대 선택
4. 전투
5. 승리 골드 정산
6. 상점
7. 두 번째 상대 선택
8. 전투
9. 승리 골드 정산
10. 상점
11. 고정 보스 전투
12. RunVictory 또는 어느 전투에서든 RunDefeat
13. 재시작 또는 메인 화면 복귀

사용자가 제공한 이미지 방향:

- 상대 선택: 월드 공간 현상수배 포스터 2장
- 전투: 현재 GameScene 테이블
- 덱/정보: 도감 오버레이와 중앙 우측의 종이 형태 계약 정보 오브젝트
- 상점: 현재 GameScene 테이블과 상점 오브젝트를 재사용하되 상단 라이터·위스키, 중단 일반 카드 3장, 하단 악마 카드 2장으로 재배치

Notion `게임 플로우` 추가 기준:

- 새 게임은 새 무작위 루트 시드를 생성
- 시작 악마 풀 4종에서 서로 다른 2장을 동일 가중치로 무작위 추첨해 모두 지급
- 지급된 2장은 좌측 악마 덱에서 뒷면으로 나온 뒤 동시에 공개하며 선택 입력은 제공하지 않음
- 지급 결과는 첫 진행 전에 저장하고 이어하기에서는 재추첨하지 않음
- 일반 적 포스터 글자는 검정, 엘리트는 보라색
- 보스는 빨간색 정보의 포스터 1장을 중앙에 표시
- 영혼·골드는 아이콘과 숫자를 함께 표시
- `CHANGE` 버튼 자체에 현재 영혼 비용 표시
- 도감 버튼으로 현재 화면 위에 오버레이 표시
- 영혼 0 즉시 RunDefeat 입력 잠금, 다시하기·메인 메뉴 제공
- 종이 계약서는 정보·상태 표시용. 계약 시작 입력은 기존처럼 악마 카드 덱 클릭

## 2. 현재 상태 분석

### 현재 존재하는 도메인과 이관 대상

- `FormalRunSession`이 `Combat`, `Shop`, `RunVictory`, `RunDefeat`를 소유한다. 일반전 승리 뒤 상점, 상점 퇴장 뒤 다음 스테이지, 보스 승리까지 이미 구현됨.  
  근거: `Assets/01. Scripts/StageProgression/RunFlow/FormalRunSession.cs:6-12`, `:78-179`, `:234-270`
- 골드, 상점 상품, 제거, 회복, 저장/복원도 정식 런 도메인에 존재한다.  
  근거: `FormalRunSession.cs:116-165`, `Assets/01. Scripts/Bootstrap/StageProgressionRuntime.cs:22-61`
- 시작 악마 도메인·저장·기존 StageProgression UI는 최신 자동 지급 규칙으로 이관됐다. `StartingDemonGrantGenerator`가 서로 다른 2장을 한 번 추첨해 모두 지급하고, `PendingStartingDemonGrant`는 선택 입력 없이 동시 공개 연출에만 사용된다. `PlayerRunState.StartingDemonGrantCompleted`와 악마 카드 2장이 스키마 v2에 저장되며 재접속 시 재추첨하지 않는다.
  GameScene 전체화의 잔여는 이 도메인을 월드 카드 배치·뒤집기 연출에 연결하는 표시 작업이다.
- 정식 런 회귀 기록은 `StageTest ↔ GameScene` 왕복 기준 완료 상태다.  
  근거: `Docs/formal-run-flow-implementation-plan.md:30-37`, `:164-168`, `:251-254`

### 현재 화면 구조

- `StageProgressionRuntime`은 `StageTest`와 `GameScene` 이름을 직렬화하고 `SceneManager.LoadScene`으로 왕복한다.  
  근거: `Assets/01. Scripts/Bootstrap/StageProgressionRuntime.cs:12-14`, `:115-123`
- `StageProgressionController`가 시작, 시작 악마 공개 완료, 상대 선택, 정식 상점, 씬 전환 입력을 소유한다. 시작 악마 선택 입력은 제거됐고 공개 타이머 종료 뒤 상대 선택으로 자동 전환한다.
  근거: `Assets/01. Scripts/UI/StageProgression/StageProgressionController.cs:102-203`, `:333-355`
- `GameManager`는 진행 세션이 `InBattle`일 때만 정식 전투를 채택한다. 아니면 독립 `CoreLoopSession`을 만든다.  
  근거: `Assets/01. Scripts/GameScene/GameManager.cs:143-164`
- 전투 종료 후 `GameManager`는 다시 `StageTest`를 로드한다.  
  근거: `Assets/01. Scripts/GameScene/GameManager.cs:2287-2297`
- `GameScene`에는 별도 `ShopController`가 있다. 이 컨트롤러가 자체 골드, 자체 구매 목록, 자체 방문 가격 상태를 가진다. 정식 `FormalRunSession.ActiveShop`과 중복 상태다.  
  근거: `Assets/01. Scripts/GameScene/ShopController.cs:47-63`, `GameManager.cs:695-967`, `:2221-2281`
- 현재 씬 루트는 `Map`, `Manager`, `UIHUD`, `UIDeckPreview`, `UIPauseSettings`, `Table Controller`, `Characters`, `EventSystem`. `Manager/GameManager`와 `Table Controller/ShopItems`가 이미 있으나 전체 진행용 화면 루트는 없다.  
  근거: 2026-07-31 Unity MCP `GameScene` hierarchy 조회.

### 핵심 간극

문제는 규칙 부족이 아니다. 화면 호스트와 상태 소유권이 둘로 갈린 것:

- 정식 상태: `StageProgressionRuntime → FormalRunSession`
- 진행 화면: `StageTest`
- 전투 화면: `GameScene/GameManager`
- 임시 상점 상태: `GameScene/ShopController`

따라서 `GameScene` 전체화의 핵심은 정식 세션을 유지한 채 모든 화면을 같은 씬 안에서 전환하고, 임시 상점 상태를 제거하는 것.

## 3. 권장 구조

### 단일 오케스트레이터

새 `GameFlowController`를 `GameScene`의 최상위 흐름 소유자로 둔다.

- 소유: `StageProgressionRuntime`, `FormalRunSession`, 현재 화면 모드
- 라우팅: 시작 악마 자동 지급·공개, 상대 선택, 전투, 상점, 결과
- 화면 전환: GameObject root 활성화/비활성화
- 저장 체크포인트 호출은 기존 `StageProgressionRuntime`/`RunSaveFlow` 경계 유지
- 전투 규칙은 계속 `GameManager → StageProgressionSession`
- 상점 규칙은 계속 `FormalRunSession.ActiveShop`

권장 화면 상태:

```text
Boot
StartingDemonReveal
OpponentSelection
Combat
CombatSettlement
Shop
RunVictory
RunDefeat
```

`FormalRunPhase`를 복제하는 새 도메인 상태가 아니다. Unity 화면 모드만 나타내는 enum.

### MainMenuScene 경계

`MainMenuScene`은 게임 규칙이나 런 진행을 소유하지 않는다.

- 새 게임: 기존 저장 초기화/새 런 예약 후 `GameScene` 로드
- 새 게임마다 새 무작위 루트 시드 생성
- 이어하기: 유효한 저장이 있을 때만 활성화, 복원 예약 후 `GameScene` 로드
- 설정: 기존 설정 시스템 재사용
- 종료: Player 빌드에서 종료, Editor에서는 무동작 또는 개발 로그

`StageProgressionRuntime`/`RunSaveFlow`가 실제 세션 생성·복원을 계속 담당한다. 메뉴는 저장 존재 여부와 진입 의도만 전달한다. `MainMenuScene`에서 전투·상점·상대 선택 상태를 직접 만들지 않는다.

제품 씬 흐름:

```text
MainMenuScene
  ├─ 새 게임 ─┐
  └─ 이어하기 ┴─> GameScene
                    ├─ 시작 악마 2장 자동 지급·공개
                    ├─ 상대 선택
                    ├─ 전투/상점 반복
                    ├─ RunVictory/RunDefeat
                    ├─ 재시작
                    └─ 메인 화면 복귀 -> MainMenuScene
```

### 뷰 분리

- `StartingDemonRevealView`: 자동 지급된 시작 악마 2장의 뒷면 배치·동시 공개 연출
- `OpponentSelectionView`: 현상수배 포스터 2장
- `FormalShopView`: 정식 `ShopVisit` 렌더링
- `RunResultView`: 승리/패배/재시작
- `CodexOverlayView`: 도감 표시와 원래 화면 복귀
- `ContractInfoView`: 중앙 우측 종이 형태의 계약 설명·상태 표시
- `GameManager`: 전투만 담당
- `GameHudView`: 영혼, 골드, 라운드 등 전투 HUD
- `DeckPreviewView`: 계약서/덱 오버레이로 계속 사용

Presenter는 기존 `StageProgressionPresentation`을 재사용하거나 GameScene 전용 어댑터를 얇게 추가한다. 포맷·버튼 활성 조건을 MonoBehaviour에 다시 작성하지 않는다.

### 상태 단일화

`GameScene/ShopController`의 렌더링 자산은 재사용 가능. 아래 상태는 정식 도메인으로 교체:

- `ShopController.Gold` → `PlayerRunState.CurrentGold`
- 자체 offer 목록 → `FormalRunSession.ActiveShop.Offer`
- 자체 구매/제거/회복 → `TryBuyShopCard`, `TryRemoveShopCard`, `TryRestAtShop`
- 자체 나가기 → `TryLeaveShop`
- 자체 방문/가격 단계 → `FormalRunSession.CompletedShopCount`, `UtilityPriceLevel`
- `GameManager`의 `_purchasedNormalCards`, `_purchasedDemonContractKeys`, `_removedNormalCards` → 제거. 다음 전투 덱은 `PlayerRunState`에서 생성

독립 GameScene 개발 모드는 별도 명시 플래그로만 유지. 정식 플레이 기본 경로와 상태를 공유하지 않는다.

## 4. 구현 단계

### 단계 0 — 새 작업 ID와 문서 기준 확정

기존 RF-05는 `StageTest ↔ GameScene` 기준으로 완료됨. 새 범위를 RF 후속 작업으로 기록한다. 예: `GF-00~GF-05`.

변경:

- `Docs/game-scene-full-flow-design.md`
- `Docs/game-scene-full-flow-development-spec.md`
- `Docs/game-scene-full-flow-implementation-plan.md`
- `Docs/game-scene-full-flow-progress-log.md`
- 필요 시 `Docs/rule.md`, `Docs/game-design-document.md`

결정 사항:

- 앱 시작 씬은 `MainMenuScene`, 실제 런 목적지는 `GameScene`
- 새 게임은 새 무작위 루트 시드를 만들고, 이어하기는 저장된 루트 시드를 복원
- `StageTest`는 테스트 전용으로 보존
- 시작 악마 풀은 사탄·벨페고르·바알제붑·마몬 4종이며 서로 다른 2장을 동일 가중치로 추첨해 모두 지급
- 시작 악마 선택 입력은 제공하지 않고, 지급 결과를 첫 진행 전에 저장해 이어하기 재추첨을 차단
- 상대 선택 이미지의 영혼/골드 수치가 현재 카탈로그와 일치하도록 Presenter 데이터 사용

완료 기준:

- 화면 순서, 입력, 취소 가능 여부, 저장 체크포인트, 실패 복귀가 표로 확정됨
- 새 게임/이어하기/설정/종료와 메인 화면 복귀 정책이 확정됨
- 기존 RF 문서를 과거 완료 이력으로 보존

### 단계 1 — 흐름 계약 테스트부터 추가

코드 이동 전 현재 정식 도메인 동작을 고정한다.

대상:

- `Assets/06.Packages/Tests/EditMode/StageProgression/FormalRunSystemValidationTests.cs`
- 신규 `GameSceneFullFlowPresentationTests.cs`
- 신규 `GameSceneFullFlowControllerTests.cs` 또는 MonoBehaviour 의존 없는 coordinator 테스트

고정할 시나리오:

- 저장 없음: 이어하기 비활성
- 새 게임: 이전 완료/실패 런 상태를 재사용하지 않고 새 GameScene 런 생성
- 이어하기: 마지막 안정 체크포인트 복원
- 새 런 → 시작 악마 2장 자동 지급·공개 → 상대 → 전투
- 시작 악마 2장이 중복 없이 결정되고 둘 다 플레이어 악마 덱에 추가됨
- 지급된 2장이 뒷면 이동 → 동시 공개 → 자동 진행 순서를 지키며 선택·확정 입력을 요구하지 않음
- 동일 런의 중복 초기화와 저장 복원에서 시작 악마를 다시 추첨하거나 중복 지급하지 않음
- 상대 선택에서 일반/엘리트 색상과 보스 단일 포스터 규칙을 지킴
- 일반전 승리 → 골드 1회 → 상점
- 상점 구매/제거/회복 → 다음 전투 덱·영혼·골드 반영
- 두 번째 상점 퇴장 → 고정 보스
- 보스 승리 → 상점 없이 RunVictory
- 각 전투 패배 → RunDefeat
- 재시작 → 골드/상점 가격/구매 상태 초기화
- 저장 복원: 시작 악마 2장 지급 완료, 전투 정산 완료, 상점 퇴장 완료
- 상대·상점의 오래된 offer ID/중복 클릭과 화면 전환 중 입력은 무변경 거부

완료 기준:

- 기존 왕복 구조를 제거해도 보존해야 할 동작이 테스트로 잠김
- 순수 테스트에서 `UnityEngine` 참조 없음

구현 결과 — 2026-07-31, GF-01 완료:

- `GameSceneFullFlowPresentationTests` 5개 추가
- 시작 악마 2장 무선택 지급·공개 표시와 서로 다른 정의 키 고정
- 공개 완료 뒤 상대 후보 2장 자동 전환과 상대 확정 뒤 전투 진입 고정
- 일반전→상점→상대 선택→일반전→상점→고정 보스→RunVictory 순서 고정
- 오래된 상대·상점 offer와 중복 입력의 무변경 거부 고정
- 재시작 시 골드·상점 단계 초기화, 지급 악마 2장 유지, 재추첨 금지 고정
- 신규 5/5, 전체 EditMode 803/803 통과
- 테스트 파일은 `UnityEngine`을 참조하지 않음

### 단계 2 — `GameFlowController` 도입

신규:

- `Assets/01. Scripts/GameScene/GameFlowController.cs`
- `Assets/01. Scripts/GameScene/GameFlowScreen.cs`
- 각 `.meta`

변경:

- `Assets/01. Scripts/Bootstrap/StageProgressionRuntime.cs`
- `Assets/01. Scripts/GameScene/GameManager.cs`

작업:

1. Runtime이 GameScene에서 새 런/이어하기 세션을 제공하게 함.
2. `GameFlowController`가 `FormalSession.Phase`, 시작 악마 지급 완료 여부, `StageProgressionState`를 읽어 화면 결정.
3. 상대 확정 뒤 씬 재로드 없이 현재 `GameManager`에 전투 세션을 바인딩.
4. 전투 종료 이벤트 뒤 `GameManager`가 `LoadProgressionScene()` 대신 controller에 완료 알림.
5. `GameManager`의 `Awake` 단발 세션 결정 구조를 `BindBattle(StageProgressionSession)` 가능한 생명주기로 변경.
6. 전투 재바인딩 시 이벤트, coroutine, hover, animation, hand view, deck stack을 완전히 초기화.
7. 영혼 0이 확정되는 프레임에 전투 입력을 잠그고 RunDefeat 화면으로 전환.

주의:

- `GameManager`가 2,300줄 이상. 새 흐름 책임까지 넣지 않는다.
- 동기 전투 규칙과 animation timeline 순서를 깨지 않는다.
- 런 세션은 Runtime 소유. GameManager가 새 `FormalRunSession`을 만들지 않는다.

완료 기준:

- 씬 로드 없이 `OpponentSelection → Combat → Shop`
- 첫 전투 종료 뒤 GameManager 전투 입력 완전 잠금
- 다음 전투 시작 시 이전 카드/애니메이션/hover 상태 잔존 0

구현 결과 — 2026-07-31, GF-02 완료:

- `GameFlowScreenResolver`가 `FormalRunPhase`, 시작 악마 공개 대기와 `StageProgressionState`를 읽어 통합 화면을 결정한다.
- `GameFlowController`가 기존 `StageProgressionRuntime.FormalSession`만 채택하고 시작 공개 완료, 상대 집중·확정, 정식 상점 입력을 기존 도메인 API로 전달한다.
- 상대 확정과 다음 전투 준비는 씬 재로드 없이 `GameManager.BindBattle(StageProgressionSession)`로 연결한다.
- `GameManager`는 정식 전투 완료를 controller에 알리고, 통합 controller가 없는 `StageTest` 회귀 경로에서만 기존 진행 씬 복귀를 사용한다.
- 전투 해제·재바인딩 시 timeline, coroutine, hover, 계약 후보, 덱 미리보기, 해머·리볼버 상태, 손패, 덱 스택과 합계 표시를 초기화한다.
- 새 게임·이어하기·예약 재개 성공 목적지를 `GameScene`으로 교체했다.
- `GameScene`의 기존 `GameManager` 오브젝트에 `GameFlowController`를 연결했고 씬 validate 결과 누락 스크립트·깨진 prefab 0이다.
- GF-02 화면 판정 2/2, GF 전체 7/7, 전체 EditMode 805/805를 Unity MCP로 통과했다. Console 오류 0.
- GF-03 화면 자산 전이므로 GF-02만으로 시작 악마·상대 선택 UI가 표시되는 것으로 간주하지 않는다.

### 단계 3 — 시작 악마 자동 지급·공개와 상대 선택을 GameScene 뷰로 구현

신규 권장:

- `GameScene/StartingDemonRevealView.cs`
- `GameScene/OpponentSelectionView.cs`
- 시작 악마 공개 카드/상대 선택 포스터 prefab
- GameScene 전용 presentation adapter가 필요하면 순수 C# 파일

재사용:

- `StageProgressionPresentation`
- 시작 악마 2장 지급 결과를 표현하는 전용 view model
- 상대 preview/예상 골드/영혼/등급 데이터
- `CharacterView` 또는 기존 적 sprite catalog

씬:

- `FlowScreens/StartingDemonReveal`
- `FlowScreens/OpponentSelection`
- 카메라 또는 overlay 전환

입력:

- New Input System `EventSystem` 기반 UI 또는 현재 raycast 패턴 중 하나로 통일
- 시작 악마 화면에는 선택·확정 입력을 제공하지 않음
- 시작 악마 풀에서 서로 다른 2장을 동일 가중치로 추첨하고 둘 다 플레이어 악마 덱에 원자적으로 지급
- 지급된 시작 악마 2장은 좌측 덱에서 뒷면으로 이동한 뒤 동시에 공개
- 공개 연출 완료 뒤 상대 선택 화면으로 자동 전환
- 지급 중 재호출·중복 초기화·저장 복원은 재추첨과 중복 지급 없이 기존 결과를 사용
- 상대 선택은 선택 강조와 확정을 분리하고 중복 클릭을 방지
- 상대 선택이 유효하게 성공한 뒤에만 다음 화면으로 전환
- 보스는 선택 입력 없이 빨간색 정보의 중앙 포스터 1장만 표시

완료 기준:

- 이미지 2 같은 2포스터 화면
- 일반 적 글자 검정, 엘리트 보라색
- 보스 빨간색 중앙 단일 포스터
- 선택 후보 이름, 영혼, 예상 골드가 도메인과 일치
- 시작 악마 지급이 실패하면 악마 덱을 부분 변경하지 않고 진행을 중단
- 상대 선택 실패 시 상태와 화면 그대로
- 1280×720, 1920×1080에서 잘림 없음

### 단계 4 — 정식 상점을 GameScene 자산에 바인딩

변경:

- `Assets/01. Scripts/GameScene/ShopController.cs`
- `Assets/01. Scripts/GameScene/ShopUtilityItemView.cs`
- `Assets/01. Scripts/GameScene/GameManager.cs`
- 필요 시 신규 `FormalShopPresenter.cs`, `FormalShopView.cs`

작업:

1. `ShopController.Open(ShopVisit, PlayerRunState)` 또는 view-model 기반 Render API 도입.
2. 상품 클릭은 option ID로 `FormalRunSession.TryBuyShopCard`.
3. 라이터는 보유 런 카드 ID로 `TryRemoveShopCard`.
4. 위스키는 `TryRestAtShop`.
5. 나가기는 현재 offer ID로 `TryLeaveShop`.
6. 거래 뒤 정식 session에서 다시 렌더.
7. `GameManager`의 독립 구매/삭제 리스트와 직접 battle deck patch 삭제.
8. `ShopController` 자체 골드/방문/가격/랜덤 생성은 독립 디버그 adapter로 격리하거나 제거.
9. 상단 라이터·위스키, 중단 일반 카드 3장, 하단 악마 카드 2장으로 화면 구성.

완료 기준:

- 화면 가격, 보유 골드, SOLD OUT, 서비스 사용 상태가 `ShopVisit`과 일치
- 세 상품 영역의 순서가 720p·1080p에서 유지되고 서로 겹치지 않음
- 거래 결과가 저장되고 다음 전투에 반영
- 한 클릭 한 거래
- 상점 나가기 뒤 씬 로드 없이 다음 선택 또는 보스 준비

### 단계 5 — 결과, 저장, 시작 경로 마감

변경:

- `GameFlowController`
- `RunResultView`
- `StageProgressionRuntime`
- 신규 `MainMenuScene`
- 신규 `MainMenuController`/`MainMenuView`
- Build Settings
- 메뉴 진입 코드가 있으면 해당 파일

작업:

- Build Settings 0번을 `MainMenuScene`으로 설정
- 새 게임/이어하기/설정/종료 연결
- RunVictory/RunDefeat overlay
- 재시작과 메인 화면 복귀
- 영혼 0 즉시 RunDefeat 전환과 입력 잠금
- 이어하기 시 체크포인트에 맞는 화면 복원
- GameScene 직접 Play는 명시된 개발 모드 처리
- `StageTest`는 회귀/개발 씬으로 보존하되 제품 Build Settings에서 제외 검토
- 독립 CoreLoop 전투는 `CoreLoopTest`로 유지

완료 기준:

- 앱 실행 시 MainMenuScene 표시
- 새 게임/이어하기 후 GameScene 진입
- GameScene 진입 후 진행 씬 로드 0회로 전체 런 완료
- 체크포인트 3종 복원 후 올바른 화면
- 패배 후 상점 생성 없음
- 보스 승리 후 상점 생성 없음
- 결과 화면에서 메인 화면 복귀 가능

### 단계 6 — 시각·입력·회귀 검증

자동:

- 신규 대상 테스트 전부
- StageProgression 전체
- CoreLoop 전체
- 전체 EditMode
- 씬 validate, missing script 0
- Console Error 0

실화면:

- MainMenuScene 새 게임/이어하기/설정/종료
- 1280×720 전체 런
- 1920×1080 전체 런
- 시작 악마 2장 자동 지급·동시 공개
- 상대 후보 2명과 선택 강조
- 전투 HUD
- 영혼·골드 아이콘과 숫자
- `CHANGE` 버튼의 현재 영혼 비용
- 도감 오버레이 열기·닫기·원래 화면 복귀
- 종이 계약 정보 오브젝트 표시와 악마 덱 계약 입력 분리
- 첫·둘째 상점
- 보스
- 승리/패배/재시작
- 저장 후 Editor 재진입 복원
- 결과 화면에서 MainMenuScene 복귀

각 시각 수정 반복마다 `visual-verdict` 실행. 기준 스크린샷과 verdict JSON 저장.

## 5. 수용 기준

- Build Settings 0번 씬은 `MainMenuScene`.
- 제품 경로에서 `SceneManager.LoadScene("StageTest")` 호출 0회.
- `MainMenuScene`은 런 도메인 상태를 직접 소유하지 않음.
- 유효한 저장이 없으면 이어하기 비활성.
- 새 게임마다 새 루트 시드 생성. 이어하기는 저장된 루트 시드 보존.
- 새 런 시작 악마는 풀 4종에서 서로 다른 2장을 동일 가중치로 추첨해 모두 지급.
- 시작 악마 화면은 선택 입력 없이 뒷면 배치·동시 공개 후 상대 선택으로 자동 전환.
- 저장 복원과 중복 초기화에서 시작 악마 재추첨·중복 지급 0.
- `GameScene` 한 씬에서 목표 순서 완료.
- 런 경제 상태 소유자는 `PlayerRunState`/`FormalRunSession` 하나뿐.
- 상점 offer 생성자는 정식 `ShopOfferGenerator` 하나뿐.
- 전투 3회가 각각 새 battle 인스턴스를 사용.
- 구매/제거/회복이 다음 전투에 정확히 반영.
- 골드 지급은 승리 전투 참조당 정확히 1회.
- 패배, 중복, 오래된 ID, 실패 입력은 상태 무변경.
- 저장 체크포인트 복원 후 중복 골드/상점 재생성 없음.
- 720p/1080p 입력 가능 영역 잘림 없음.
- 상점 배치는 상단 서비스, 중단 일반 카드 3장, 하단 악마 카드 2장.
- 영혼·골드는 아이콘과 숫자를 함께 표시.
- `CHANGE` 버튼은 현재 비용 표시.
- 계약 정보 오브젝트는 계약 시작 입력을 소유하지 않음.
- 전체 EditMode 기존 통과 수보다 테스트 수가 줄지 않고 실패 0.
- Console Error 0, missing script 0.

## 6. 위험과 대응

### GameManager 비대화

위험: 흐름·상점까지 넣으면 변경 충돌과 회귀 증가.  
대응: `GameFlowController`가 흐름, `GameManager`는 전투만 소유.

### 상점 이중 상태

위험: 표시 골드와 실제 골드 불일치, 다음 전투 덱 누락.  
대응: `ShopController`를 렌더/입력 adapter로 축소. 정식 session 외 상태 제거.

### 같은 씬에서 전투 재사용

위험: 이전 이벤트 구독, coroutine, animation trigger, card object 잔존.  
대응: `BindBattle`/`UnbindBattle` 생명주기와 전투 간 상태 초기화 테스트.

### 씬/prefab 동시 편집

위험: YAML 충돌, serialized reference 손실.  
대응: 한 명이 GameScene/prefab 소유. 코드 먼저, 씬 wiring 마지막. 변경 전후 hierarchy와 missing refs 검증.

### 최신 시작 악마 자동 지급 규칙

위험: 현행 코드의 2장 중 1장 선택 구조와 권위 규칙 문서의 2장 모두 자동 지급 구조가 충돌.
대응: 단계 0에서 자동 지급을 후속 문서의 기준으로 명시하고, 선택용 API와 단일 시작 악마 저장 필드를 이관한다. 과거 RF 기록은 수정하지 않는다.

## 7. 권장 작업 분할

순차 의존이 큼. 기본은 한 명이 통합 소유.

1. 도메인/테스트 계약
2. GameFlowController와 GameManager 재바인딩
3. 시작 악마 자동 지급·공개/상대 선택 뷰
4. 정식 상점 adapter
5. 씬 wiring
6. 전체 QA

병렬 가능:

- 코드 흐름과 시각 prefab 제작
- 순수 Presenter 테스트와 scene mockup

병렬 금지:

- GameScene YAML 동시 편집
- GameManager와 ShopController의 상태 소유권을 서로 다른 작업자가 독립 변경

## 8. 최종 권장

기존 RF 도메인을 버리고 GameManager 안에 새 전체 게임을 만들지 않는다.

권장안:

`StageProgressionRuntime/FormalRunSession` 유지  
`GameFlowController` 추가  
`GameManager` 전투 전용화  
`ShopController` 정식 session adapter화  
`StageTest` 화면을 GameScene 월드 공간 뷰로 대체

이 경로가 저장, 골드 1회 정산, 상점 원자성, 상대 선택, 보스 흐름, 기존 798개 기준 회귀를 가장 안전하게 보존한다.

## 9. 관련 프로젝트 문서 처리

### 구현 전 함께 수정할 현재 기준 문서

- `Docs/rule.md`
  - 새 런에서 서로 다른 시작 악마 2장을 모두 지급하고 선택 화면을 제공하지 않는 현행 권위 규칙을 유지한다.
  - 구현과 후속 문서는 이 규칙을 따라야 한다.
- `Docs/stage-progression-design.md`
  - 시작 악마 2장을 모두 지급한다고 기록되어 있다.
  - `rule.md`와 같은 자동 지급 방식으로 유지하고, 코드 이관 범위를 추가한다.
- `Docs/scene-presentation-design.md`
  - 제품 흐름을 `StageTest ↔ GameScene` 왕복으로 기록한다.
  - 상점 배치도 상단 일반 카드 3장·악마 카드 2장으로 기록한다.
  - `MainMenuScene → GameScene 단일 런`과 상단 서비스·중단 일반·하단 악마 배치로 갱신해야 한다.
- `Docs/game-design-document.md`
  - 메인 메뉴, 단일 GameScene 런, 시작 악마 2장 자동 지급·공개, 도감, 게임 오버, 최신 상점 배치를 요약 기준에 반영한다.

### 새 후속 작업 링크만 추가할 문서

- `Docs/formal-run-flow-design.md`
- `Docs/formal-run-flow-development-spec.md`
- `Docs/formal-run-flow-implementation-plan.md`
- `Docs/formal-run-flow-progress-log.md`

RF-05의 `StageTest ↔ GameScene` 결과는 당시 실제 완료 이력이다. 과거 내용을 새 구조로 덮어쓰지 않는다. 문서 상단이나 변경 기록에 “GameScene 단일 런 후속은 `Docs/gamescene-full-flow.md` 참조” 링크만 추가한다.

### 과거 증거로 보존할 문서

- 각 기능의 `*-progress-log.md`
- `Docs/ai-usage-technical-document.md`
- `Docs/team-role-technical-document.md`
- `Docs/project-structure-and-mcp-reference.md`의 과거 작업별 검증 기록

해당 문서의 `StageTest`, `CoreLoopTest`, 당시 테스트 수와 화면 왕복 기록은 역사적 증거다. 새 계획에 맞춰 소급 수정하지 않는다. 실제 구현 완료 후 현재 구조 요약과 새 검증 항목만 추가한다.
