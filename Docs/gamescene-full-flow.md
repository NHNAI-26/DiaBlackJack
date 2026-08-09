# MainMenuScene + GameScene 전체 게임 플로우 통합 계획

작성일: 2026-07-31  
범위: 제품 시작 화면은 `MainMenuScene`, 실제 런 전체는 `GameScene`에서 진행.

> 2026-08-08 현재 상태: `MainMenuScene`은 GameScene의 Map 프리팹과 `MainMenu` mood를 재사용하는 월드 메뉴다. Telegraph의 새 게임·튜토리얼·설정만 제공하며, 새 게임은 기존 저장을 즉시 덮어쓴다. 성공한 진입은 Telegraph·로고 퇴장 후 `GameScene`으로 이동한다. `GameFlowController`는 시작 악마 공개·상대 선택·전투·상점·결과 화면을 도메인 상태에서 판정한다.

## 1. 목표

제품은 `MainMenuScene`에서 시작한다. 메뉴에서 새 게임 또는 튜토리얼을 선택하면 퇴장 연출 뒤 `GameScene`으로 이동하고, 이후 정식 런은 다른 진행 씬으로 왕복하지 않고 GameScene 안에서 끝까지 진행한다.

1. `MainMenuScene`: Telegraph 새 게임/튜토리얼/설정, 상단 로고, GameScene Map 기반 배경
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
- 지급된 2장은 좌측 악마 덱에서 뒷면으로 나온 뒤 동시에 공개하며 카드 선택 입력은 제공하지 않음. 공개 뒤 확인 버튼으로 진행
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
- `StageProgressionController`가 시작, 시작 악마 공개 확인, 상대 선택, 정식 상점, 씬 전환 입력을 소유한다. 시작 악마 카드 선택 입력은 제거됐고 두 장 공개 뒤 확인 버튼이 승인되어야 상대 선택으로 전환한다.
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

- 새 게임: 기존 저장·예약을 확인 없이 교체하고 새 런 예약 후 퇴장 연출, `GameScene` 로드
- 새 게임마다 새 무작위 루트 시드 생성
- 튜토리얼: 인메모리 튜토리얼 런 생성 후 같은 퇴장 연출과 `GameScene` 로드
- 설정: 기존 `SettingsSystem`/`PauseSettingsCanvas`의 설정 패널만 재사용하며 시간 정지·Pause/Quit 패널은 사용하지 않음
- 이어하기·시작 예약 재개·종료는 메인메뉴에서 노출하지 않으며 기반 저장 API는 유지

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
- `RunResultView`: 종료 체크포인트 저장 실패 시 `RETRY SAVE`만 제공하는 예외 패널
- `RunResultDialogueSO`/`SpeechBubbleView`: 승패별 상인 대사와 한 글자 출력·클릭 진행
- `CodexOverlayView`: 도감 표시와 원래 화면 복귀
- `ContractInfoView`: 중앙 우측 종이 형태의 계약 설명·상태 표시
- `GameManager`: 전투만 담당
- `GameHudView`: 영혼, 골드, 라운드 등 전투 HUD
- `DeckPreviewView`: 계약서/덱 오버레이로 계속 사용

### 라운드 결과 표시

- `CoreLoopBattle`의 `ResolvingRound` 스텝을 GameScene 결과 스냅샷으로 사용한다. 도메인의 자동 다음 라운드 전이는 변경하지 않는다.
- `GameScenePresenter`는 해결 스냅샷이 살아 있는 동안 카드 ID, 화면 순서, 단계별 블랙잭 합, 마몬 보너스, 해결 ID와 양측 피해를 불변 `RoundComparisonPlan`으로 캡처한다. 다음 라운드로 동기 전이한 뒤에는 라이브 손패를 다시 읽지 않는다.
- `GameManager`는 양측 합계를 0으로 초기화하고 플레이어 공개 → 적 공개 → 플레이어 비공개 → 적 비공개 → 적 마몬 보너스 순으로 재생한다. 공개 카드는 화면 오른쪽부터 합산하며, 0.28초 정수 tween·텍스트 punch와 0.12초 간격을 사용한다.
- 비공개 카드는 `CardView`의 전체 flip 시간이 끝난 뒤 합산한다. 이미 앞면이면 flip 대기만 생략한다. 합산 중인 카드는 활성 종류와 사용 가능 여부에 상관없이 비교 전용 HDR 노란색 외곽선을 사용하고, 비교 종료 뒤 원래 호버·효과 강조색을 복원한다. 합계는 15까지 흰색, 16~21은 금색·HDR Glow가 연속 증가하고 21 초과 즉시 빨간색으로 바뀐다.
- 플레이어 마몬 최종 선택에서는 선택 UI를 숨긴 채 플레이어 공개·비공개 카드만 먼저 합산하고, 집계 완료 뒤 기존 선택 버튼만 활성화한다. 대기 계획에는 적 카드 투영을 넣지 않으며, 선택 해결 뒤 플레이어 보너스와 적 공개·비공개·마몬 보너스를 이어 재생한다.
- 플레이어·적 어느 쪽이든 성공한 리볼버 `Resolved` 또는 사탄 숫자 추측이 실제 상대 버스트 승리로 이어졌다면 합계 비교를 생략한다. 버스트 방지로 라운드가 계속되거나 실패·재시도·다른 종료 원인이면 일반 비교를 유지한다.
- `GameManager`는 해결 ID로 중복 재생을 막고 최종 결과를 최소 2.5초 유지한다. 재시작·전투 해제·비활성화 때 tween, 카드 강조와 임시 합계를 정리한다. 유지 완료 지점에서 GSV18 공통 영혼 감소 연출이 같은 해결 ID의 양측 실제 피해 기록을 재생한다.
- GSV17 최초 검증은 집중 7/7(job `0fd1ff796fa84107885b0a9e6986b489`)과 인접 83/83(job `2d54c14d1a714642a996160499fd2f11`)이 통과했다. 비공개 카드 정답 승리 비교 생략과 마몬 UI 지연 보정 뒤 집중 13/13(job `238991ddcc664da684c1b64d3d10ff75`), 인접 리볼버·사탄·마몬·프레젠테이션 102/102(job `d6951c79a69643b3b26de06f59a89398`)가 통과했다. 전체 CoreLoop 어셈블리와 1280×720·1920×1080 수동 확인은 이번 보정에서 실행하지 않았다.
- 비교 카드 전용 HDR 노란색 보정은 신규 2/2(job `07351fd8a6e746eab74fc3714bb94cff`)와 GSV17 전체 15/15(job `fcb7467daa524ee48ade5bcfc5f0b780`)을 통과했다. 인접 두 클래스는 107/109(job `b6efd712183846cf95c22ccf77770eb4`)이며 실패 2건은 범위 밖 카드 카탈로그 숫자·프리팹 자식 수 기대값 불일치다. 본 변경 반영 직후 Unity 컴파일과 Console Error 0을 확인했으나, 이후 병행 변경에서 범위 밖 컴파일 오류 11건이 발생해 최종 재컴파일 게이트와 수동 해상도 검증은 완료하지 않았다.

### GSV18 공통 영혼 감소 연출

- `CoreLoopBattle`은 모든 감소 경로를 공통 함수로 모으고 `SoulLossRecord`에 전투 내 단조 이벤트 ID, 대상, 감소 전·후·최대 영혼, 실제 감소량, 원인과 선택적 해결 ID를 남긴다. 0 감소는 남기지 않으며 라운드 피해 양측 기록은 같은 `RoundResolution.Id`에 연결한다.
- `GameSceneViewModel`은 누적 기록 스냅샷을 전달하고 `GameManager`는 마지막 큐 ID와 라운드 보류 큐로 재렌더 중복을 막는다. `Stepped`가 없는 유료 체인지도 현재 스냅샷을 타임라인에 추가한다.
- 라운드 피해는 HUD를 감소 전 값으로 고정한 채 GSV17 결과 2.5초 유지 뒤 재생한다. 리볼버·사탄 즉시 승리도 동일 완료 경로를 사용한다. 라운드 외 비용은 기존 카드·계약 애니메이션 뒤 바로 재생하고 완료 전에는 전투 입력 UI를 숨긴다.
- `SoulLossPresentation`은 HUD Canvas 아래에 TMP 토큰을 런타임 생성·풀링하고 기존 영혼 아이콘, 글꼴과 머티리얼을 재사용한다. 실제 감소 1당 토큰 하나를 0.12초 간격으로 약 0.72초 낙하시킨다. 플레이어는 화면 하단 중앙, 적은 현재 캐릭터 스프라이트 상단을 앵커로 쓰며 양측 공동 피해는 동시에 시작한다.
- 토큰 도착마다 `GameHudView`가 해당 영혼을 1 낮추고 빨간색·1.25배에서 0.3초 내 원래 상태로 복원한다. 플레이어 토큰마다 `PresentationManager`의 기존 `_ColorScreen`·`_BlendStrength`를 사용해 붉은 점멸을 한 번 재생하고 마지막에 원래 색·강도를 복원한다.
- 재시작·전투 해제·비활성화에서는 토큰 tween, HUD 임시값, 보류 기록과 화면 점멸을 함께 취소·복원한다. 영혼 회복과 별도 감소 SFX는 범위 밖이다.
- 구현 집중 테스트는 5/5(job `35520f94e62d429093888e0511fc7a91`), GSV17 회귀는 13/13(job `767dbda790b14238bca8107d6e9b4d4c`), 직접 영향 6개 클래스는 100/100(job `ce9be87a4379410b8c0e1e66fc80fd1b`) 통과했다. CoreLoop 전체 879건은 857건 통과·기존 비관련 22건 실패(job `212ac469256943188df91fc0911f9edb`)였으며 새 영혼 테스트 실패는 없다. 1280×720·1920×1080 수동 확인은 완료로 기록하지 않는다.

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
- 지급된 2장이 뒷면 이동 → 동시 공개 → 확인 버튼 → 상대 선택 순서를 지키며 카드 선택 입력을 요구하지 않음
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
- 공개 완료 뒤 확인 입력으로 상대 후보 2장 전환과 상대 확정 뒤 전투 진입 고정
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
- 시작 악마 화면에는 카드 선택 입력을 제공하지 않고, 두 장 공개 뒤 진행 확인 버튼만 제공
- 시작 악마 풀에서 서로 다른 2장을 동일 가중치로 추첨하고 둘 다 플레이어 악마 덱에 원자적으로 지급
- 지급된 시작 악마 2장은 좌측 덱에서 뒷면으로 이동한 뒤 동시에 공개
- 시작 악마 공개 화면에서는 상대 영혼 HUD를 표시하지 않으며 건너편 상인 연출은 유지
- 공개 연출 완료 뒤 확인 버튼을 활성화하고, 승인 뒤 상대 선택 화면으로 전환
- 지급 중 재호출·중복 초기화·저장 복원은 재추첨과 중복 지급 없이 기존 결과를 사용
- 상대 선택은 선택 강조와 확정을 분리하고 중복 클릭을 방지
- 상대 선택이 유효하게 성공한 뒤에만 다음 화면으로 전환
- 보스는 선택 입력 없이 빨간색 정보의 중앙 포스터 1장만 표시

완료 기준:

- 이미지 2 같은 2포스터 화면
- 일반 적 글자 검정, 엘리트 보라색
- 보스 빨간색 중앙 단일 포스터
- 선택 후보 이름, 영혼, 예상 골드가 도메인과 일치
- 전투 진입 전 상대 영혼 HUD가 노출되지 않고 전투 진입 시 정상 복원
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

구현 상태(2026-07-31, GF-04):

- `GameFlowController`가 정식 상점 화면에서 카드 구매·라이터 제거·위스키 회복·나가기 입력을 현재 `ShopOfferId`와 함께 `FormalRunSession`으로 전달한다.
- `ShopController`는 정식 `StageProgressionViewModel`을 월드 상점 자산에 투영하며 일반 카드 3장·악마 카드 2장을 고정 배치한다.
- 일반 상품은 `DefinitionKey`를 `CardView`까지 전달해 각 정의의 고유 앞면을 사용한다.
- 악마 상품 생성 풀은 프로토타입 활성 7종(사탄·벨페고르·바알제붑·아스모데우스·마몬·바포메트·아자젤)으로 제한한다.
- 정식 상점에서는 라이터를 왼쪽, 위스키를 오른쪽에 두고 전투용 상대 영혼 HUD를 숨긴다.

#### GSV-07 시작 공개·상점 악마 후보 보정

- 시작 악마 두 장은 상점 악마 카드 홀더와 같은 월드 좌표·간격으로 배치한다.
- 동시 공개가 끝난 두 장은 호버할 수 있으며 계약 후보와 동일한 악마 상세 패널을 사용한다.
- 시작 지급 카드 클릭은 선택으로 처리하지 않는다. 기존 확인 버튼만 상대 선택 전환을 승인한다.
- 상점 일반 카드 3장은 같은 정의가 중복될 수 있다.
- 상점 악마 카드 2장은 서로 다른 정의이며 현재 플레이어 악마 덱의 정의를 후보 풀에서 제외한다.
- 저장 복원과 완료 상점 재생 시에도 현재 보유 악마를 제외하고, 같은 시드·같은 상태의 후보는 결정적으로 유지한다.
- 상품·서비스 상태는 거래 뒤 정식 view-model로 다시 생성하며, 별도 GameScene 골드·재고를 정식 흐름의 기준으로 사용하지 않는다.
- GF-04 전용 2/2, 전체 EditMode 808/808, `GameScene` validate 문제 0, 컴파일·게임 코드 오류 0을 확인했다. Console 1건은 Test Framework 결과 저장 안내다.
- 1280×720 실화면에서 서로 다른 일반 카드 앞면 3장, 프로토타입 악마 2장, 서비스 좌우 순서와 상대 HUD 비노출을 확인했다. 1920×1080 재촬영은 단계 6 전체 QA에 포함한다.

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

구현 상태(2026-07-31, GF-05; 2026-08-09 GFR01로 결과 표시 교체):

- 정상 저장 흐름의 중앙 `RunResultView`는 숨기고, 전투 상대 퇴장 → `readyStage` → 시작 악마 지급 상인 등장 → 말풍선 결과 대화 순서로 표시한다.
- 저장 실패 때만 기존 패널을 표시하고 `RETRY SAVE`를 허용한다. 재시도 성공 뒤 결과 대화를 시작한다.
- `RunResultDialogueSO`가 승리·계약 승리·패배 공통·상대 6종 전용 문구와 초당 글자 수를 소유한다. 누락 상대는 표시 이름 기반 기본 문구와 경고로 대체한다.
- 타이핑 중 클릭은 문장을 즉시 완성하고, 다음 클릭은 다음 문장으로 이동한다. 마지막 문장 뒤 클릭은 패배의 영혼 감소 연타·검정 페이드 또는 승리의 1.5초 URP 흐림을 재생한다. 승리는 전환 직전의 선명한 화면을 런타임 캡처하고 완전히 흐린 화면 위에서 캡처 알파를 1에서 0으로 낮춰 페이드처럼 연속해서 흐려진다. 흐림이 0.65초 선행한 뒤 그라데이션 눈꺼풀 닫힘·30% 재개방·최종 닫힘 순서로 이어진다. 완료 후 `StageProgressionRuntime.LoadMainMenuScene()`을 호출한다. 전환 중 입력은 잠그고 이동 실패 시 오버레이를 복구해 마지막 문장에서 재시도한다.
- `PlayerRunState.HasMadeDemonContract`는 실제 계약 성사 뒤 런 전체와 schema v2 저장·복원에서 유지되고 새 런에서 초기화된다. 시작 악마 지급은 계약으로 세지 않는다.
- 영혼 0 즉시 패배, 패배 후 상점 없음, 보스 승리 후 상점 없음, 런 종료 체크포인트는 기존 정식 세션·SV-06 계약을 그대로 사용한다.
- GF-05 전용 3/3과 전체 EditMode 811/811이 통과했고 컴파일·게임 코드 오류는 0이다.
- 1280×720 결과 화면의 정보 계층·입력 영역을 확인했으며 시각 판정 93/100을 기록했다. 신규 외부 에셋·오픈소스·패키지는 없다.

GFR01 결과 대화 빠른 미리보기:

- Scene 뷰의 `Quick Play > Run Result` 드롭다운이나 `Tools > DiaBlackJack > Quick Play > Run Result` 메뉴를 연다.
- `Victory/No Contract`, `Victory/Contracted`, 또는 `Defeat` 아래 상대 6종 중 하나를 선택하면 `GameScene` Play Mode로 바로 진입한다.
- 미리보기는 실제 런·저장을 만들거나 변경하지 않으며, 적 퇴장 → `readyStage` → 상인 등장 → 결과 대화를 재생한다.
- 타이핑 중 클릭은 현재 문장을 완성하고, 다음 클릭은 다음 문장으로 이동한다. 마지막 문장 뒤 클릭은 선택한 승패 종료 연출을 그대로 재생한 뒤 `MainMenuScene`으로 이동한다.
- 승리 보정은 선명 프레임 캡처·눈꺼풀 런타임 텍스처와 Base·UI·TextUI 카메라의 Volume Layer별 런타임 DOF만 사용하므로 Scene·Prefab·이미지 에셋을 변경하지 않는다. GFR02 10/10과 결과 대화·영혼 감소·GameScene 흐름 회귀 31/31이 통과했다.

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
- 사탄 활성 중 허용된 일반 행동과 현재 면 능력 동시 제공
- 사탄 일반 행동의 면 유지, 능력 성공의 행동 소비·면 전환, 실패·취소의 상태 보존
- 적 사탄의 일반 행동/능력 선택과 공개 정보만 사용하는 판단
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
- 시작 악마 화면은 카드 선택 없이 뒷면 배치·동시 공개하고, 확인 버튼 승인 후 상대 선택으로 전환.
- 저장 복원과 중복 초기화에서 시작 악마 재추첨·중복 지급 0.
- 사탄은 일반 행동을 선택하면 현재 면을 유지하고, 현재 면 능력이 성공한 경우에만 차례를 소비한 뒤 반대 면으로 전환함.
- 사탄 능력의 실패·취소·오래된 입력은 차례·카드·영혼·현재 면을 변경하지 않음.
- 적 사탄도 능력을 강제 사용하지 않고 합법 일반 행동과 능력을 공개 정보만으로 비교함.
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

## 10. 적·상인 월드 스페이스 말풍선

- `SpeechBubble.prefab`은 World Space Canvas를 유지하며 `EnemyCharacter.prefab`의 스프라이트 우측 자식으로 배치한다. `SpeechBubbleView`가 카메라 정렬과 상인 축소 시 월드 크기 보정을 담당하고, 모든 UI Graphic의 raycast target은 꺼서 카드·상점 클릭을 차단하지 않는다.
- 전투·상점 문구는 `SpeechProfileSO`가 소유한다. 한 entry는 대소문자를 구분하는 `cueKey`와 문장 목록이다. 기존 공통 cue와 상인 cue는 한국어 문장 2개를 유지하고, GSB-03에서 추가한 카드별 전용 cue와 플레이어 카드 반응 cue는 캐릭터별 한국어 문장 1개를 가진다. 각 `EnemyCombatProfileDefinitionSO`는 자기 `SpeechProfileSO`를 직접 참조하고 `speakerKey == enemy key`를 검증한다. 콘텐츠는 `Assets/02. ScriptableObjects/Speech`에서 수정한다.
- 게임 코드는 문장 대신 `SpeechCueKeys`만 발생시킨다. `EnemySpeechDirector`가 별도 `DeterministicRng`로 매 호출 문장을 고르므로 전투 규칙 RNG와 결과에는 영향이 없고 즉시 반복도 허용한다. 누락 key는 key 문자열 자체를 표시하며 같은 profile/key 경고는 한 번만 남긴다.
- cue 우선순위는 종료, 낮은 영혼, 피해, 행동, 전투 시작, 라운드 시작 순이다. 같은 전환에서는 가장 높은 하나만 표시하고 다음 전환의 새 cue는 이전 우선순위와 무관하게 교체한다. 전투 시작·낮은 영혼·종료는 전투당 한 번, 라운드 시작은 라운드당 한 번, 행동은 공개 행동 순번당 한 번, 피해는 `RoundResolution.Id`당 한 번이다.
- 행동 key는 적의 `Hit`, `Stand`, `Change`, 카드 사용과 계약, 양측 자동 카드 발동에서 발생한다. 플레이어의 수동·악마·자동 카드는 각각 공통 상대 반응 cue로 합친다. 피해는 `CardEffectBust`를 `combat.damage.card`, 일반 비교·숫자 버스트를 `combat.damage.round`, 계약 등 나머지를 `combat.damage.other`로 분류한다. 낮은 영혼은 `현재 영혼 > 0 && 현재 영혼 * 3 <= 최대 영혼`이 처음 성립할 때 발생한다. 기존 `EnemyActionLabel`은 회귀 호환용으로 남기되 말풍선 판정에는 사용하지 않는다.
- 상인은 `shop.greeting`, 구매 성공, 골드 부족, 판매 완료, 기타 구매 불가, 라이터, 위스키, 퇴장 key를 같은 `SpeechProfileSO` 형식으로 사용한다. 독립 상점과 정식 런 상점 모두 실패한 상품 클릭은 도메인 상태를 바꾸지 않고 이유 대사만 표시하며, 성공 대사는 실제 `Try*` 성공이 확인된 뒤에만 표시한다.
- 문구는 다음 cue가 올 때까지 유지한다. 종료 cue가 표시되면 입력과 독립 상점·정식 진행 화면 전환을 실시간 1.5초 잠근 뒤 기존 흐름을 재개한다. 전투 재바인딩과 런 재시작에서는 명시적으로 숨기고 상점 입장 문구는 남아 있던 전투 문구를 교체한다. 페이드, 타이핑, 클릭형 대화 진행은 범위 밖이다.

- 말풍선 텍스트와 계약 후보·수정구슬 후보·사탄 숫자 후보·시작 악마 지급 공개 연출의 임시 카드 계층은 사용자 레이어 7 `TextUI`에서 렌더한다. 기본 카메라는 이 레이어를 제외하고, 후처리 기능이 없는 `TextUI_Renderer`를 사용하는 Overlay Camera가 Camera Stack 마지막에서 합성한다. 임시 카드의 앞·뒷면은 인스턴스 전용 `NHNSpriteUberLit` Unlit 머티리얼을 사용하고 그림자·라이트 프로브·리플렉션 프로브를 받지 않는다. 말풍선 배경·꼬리와 평상시 전투 손패·상점·도감 카드는 기존 레이어와 Lit 머티리얼에 남아 NHN 조명·후처리를 유지한다.
- `TextUIOverlayCameraSync`는 Cinemachine 전환과 블렌드 중 실제 카메라의 위치·회전·투영행렬을 Overlay Camera에 렌더 직전 복사한다. 따라서 월드 스페이스 텍스트와 말풍선 배경이 분리되어 움직이지 않는다.
- `EnemyCharacter.prefab`의 `SpeechBubbleAnchor`만 캐릭터 기준 위치 `(-3.28, 0.69, 0)`를 소유한다. 그 아래 중첩된 `SpeechBubble.prefab`은 위치 0과 원본 크기·피벗·폰트·재질을 상속해 프리팹 수정이 씬 배치에도 그대로 반영된다.

## 11. GF-06 전체 QA 완료 기록

- 완료일: 2026-07-31
- 담당: 이천서
- `MainMenuScene → GameScene → 시작 악마 자동 지급 → 상대 선택 → 전투 → 상점 → 상대 선택 → 전투 → 상점 → 보스 → RunVictory`를 실제 정식 세션으로 통과했다.
- 첫 상점에서 카드 구매 성공 뒤 동일 슬롯 중복 구매와 상점 이탈 뒤 오래된 Offer 입력이 상태를 바꾸지 않음을 확인했다.
- 첫 상점 체크포인트를 종료·이어하기로 복원해 2스테이지, 골드, 덱 수, 시작 악마 무재추첨을 확인했다.
- 상점 위스키 사용, 다음 전투 반영, 보스 승리, 결과 화면, 같은 GameScene 새 런 재시작과 자원 초기화를 확인했다.
- 비전투 화면에서 씬 참조가 Unity fake-null 상태로 남아 전투 HUD가 노출되던 회귀를 `ResolveSceneReferences()`의 Unity null 검사로 수정했다.
- 전투 행동의 현재 체인지 비용을 `CHANGE -0`, 이후 `CHANGE -N`으로 표시하도록 보정했다.
- 시작 악마가 만든 런타임 카드 정의를 덱 미리보기가 정적 카탈로그에서 다시 조회해 예외가 나던 문제를 수정했다. 덱 스냅샷이 카드 자체 설명을 보존하도록 변경하고 `GF06_U01` 회귀 테스트를 추가했다. 유료 체인지 라벨은 `GF06_U02`로 고정했다.
- 1920×1080에서 메인 메뉴·상점·결과, 1280×720에서 상대 선택·전투 HUD를 확인했다. 최종 시각 판정은 94/100이다.
- GF-06 회귀 2/2와 전체 EditMode 813/813, `MainMenuScene`·`GameScene` 누락 스크립트/깨진 프리팹 0, Console Error 0이다.
- QA 전에 백업한 사용자 `run-save.json`·`run-save.bak`은 SHA-256 일치 상태로 원위치 복원했다.
- 씬·프리팹·외부 에셋·오픈소스·패키지는 변경하지 않았다.

GF-00~GF-06의 현재 계획 범위는 완료다. 이후 작업은 실제 빌드 입력 QA, 접근성 글자 크기 검토, 미구현 설정·도감 등 별도 기능 단위로 분리한다.

## 11. GameScene 무드 전환 연결

- `StartingDemonReveal`, `OpponentSelection`: `readyStage`
- `Shop`: `shopStage`
- `Combat`: 현재 상대 프로필에 대응하는 테마
  - `cowardly-gambler`: `cowardlyGambler`
  - `gunslinger`: `gunslinger`
  - `cultist`: `fanatic`
  - `trickster`: `fraud`
  - `enforcer`: `executor`
  - `final-boss`: `bossStage`
- 승리·패배 화면은 상인 결과 대화를 위해 `readyStage` 무드를 사용한다.
- 동일한 화면이 다시 렌더링돼도 같은 BGM을 재추첨하거나 재시작하지 않는다.
- `GameFlowController`가 화면 상태와 현재 전투 프로필을 무드 ID로 변환하고, 씬의 `MoodController`가 창문·조명·BGM을 1초 동안 전환한다.

담당자 이천서가 위 매핑과 최종 승인 책임을 가진다. 2026-08-03 기준 무드 집중 회귀 19/19, GameScene 프로필 등록 8/8, 기본 건슬링어 런타임 적용, 씬 검증 문제 0건, Console 오류·경고 0건을 확인했다. 전체 CoreLoop EditMode는 660/664이며 실패 4건은 기존 도감·망치 대상 순서·호버 반사 호출 회귀다.

## 2026-08-06 라운드 전환 후 적 행동 대사 복구

- `PublicActionHistory`가 라운드마다 초기화되는데 `EnemySpeechDirector`가 행동 순번만 전투 전체 증가값처럼 비교해, 다음 라운드의 낮아진 순번을 이미 표시한 행동으로 오판했다.
- 행동 중복 판정을 기존 `EnemySpeechCue`의 전투·라운드·행동 순번 식별자로 변경했다. 같은 행동 프레임은 계속 한 번만 표시하고, 새 라운드의 `Hit`·`Stand`·`Change`·카드·계약 행동은 이전 라운드 종료 대사를 교체한다. 기존 cue 우선순위와 라운드별 행동 기록 초기화는 유지한다.
- 기준 흐름은 `MainMenuScene`에서 시작한 정식 런의 `StageProgressionRuntime → GameScene → GameManager` 전투다. 독립 GameScene 전용 분기는 변경하지 않았다.
- 신규 `GSB02_U09` 1/1(job `daf181f127754f7487dc3003af744284`), Director 관련 U05~U09 5/5(job `aa00f90293ca4d3284a12ebd965cb66b`)가 통과했다. 말풍선 클래스 전체는 24/25(job `779932a0d3ea46ac9caadc4d9a49a1dc`)이며, 실패 1건 `GSB01_U11`의 카메라 스택 기대값 128/실제 160은 작업 전부터 기록된 기존 회귀다. 컴파일 직후 Console Error는 0건이었고, 클래스 전체 실행에서는 기존 머티리얼 드로어·URP 셰이더 오류가 재현됐다.
- 사용자 저장을 덮을 수 있는 실제 새 게임 Play QA는 실행하지 않았다. 구현에서 씬·프리팹·대사 SO·외부 에셋·패키지는 변경하지 않았다. 클래스 전체 테스트 뒤 `GameScene.unity`의 소유 불명 작업 트리 변경이 새로 감지되어 정리하지 않고 보존했다.

## GSB-03 카드 행동별 적 대사 분리

- 적 리볼버는 각 발사마다 `before → hit/miss`, 나이프·망치는 `before → bust/no_bust` 순서로 재생한다. 리볼버 결과는 `CardEffectResult.Succeeded`, 나이프·망치 결과는 `EndedRound`를 사용한다. 레비아탄 재발사는 같은 공개 행동 안에서도 발사 순번 2로 분리한다.
- 적 악마·자동 카드 cue는 프로필의 실제 계약·덱 구성에서 계산한다. 광신도 2종과 최종 보스 6종의 악마 카드, 사기꾼 1종과 집행자 4종의 자동 카드만 해당 프로필에 요구하며 보유하지 않은 전용 cue는 에셋에 두지 않는다.
- 플레이어 수동·악마·자동 카드에는 적별 공통 반응 cue 3개를 사용한다. 전용 cue 누락 시 수동·자동은 `combat.action.use_card`, 악마는 `combat.action.demon_contract`로 폴백한다.
- 순수 CoreLoop에는 자동 카드 발동 순번·라운드·소유자·정의 키 관측값만 추가했다. 공개 행동 기록과 AI 결정 표면은 변경하지 않았다. 말풍선 중복 식별자는 전투·라운드·이벤트 종류·행동/발동 순번·발사 순번·단계를 사용한다.
- `GameManager`는 전 대사를 연출 시작 전에, 결과 대사를 연출 대기 종료 뒤에 표시한다. 결과 대사는 `stepSeconds` 동안 유지한 뒤 다음 스냅샷이나 승리·패배 대사로 넘어가며, 미소비 결과 대사가 있으면 승리·패배 대사를 먼저 표시하지 않는다. 수정구슬은 별도 결과 cue 없이 기존 사용·피해·종료 대사를 유지한다.
- 여섯 `SpeechProfileSO`에 필요한 전용·반응 cue 79개를 한국어 1문장씩 추가했다. 씬·프리팹은 변경하지 않았다.
- GSB03 범주는 5/5(job `ce9ac19328284ae884dc4071658189ee`) 통과했다. `GameSceneSpeechBubbleTests` 전체는 27/29(job `f67c80cd94584fa984e48e188aea6877`)이며, 실패는 작업 전부터 이번 변경 파일 밖에 있던 `GSB01_U09` 말풍선 앵커 기대값 `(-3.28, 0.69, 0)`/실제 `(-3.28, 2.59, 0)`과 `GSB01_U11` 카메라 mask 기대값 128/실제 160 두 건이다. 대상 구현 컴파일과 Console Error는 0건이었다. 이후 추가한 결과 대사 1초 유지 변경은 `validate_script` 오류 0을 확인했으나, 공유 Editor가 다른 세션 소유의 Play Mode 전환에 머물러 최종 Unity 재컴파일은 실행하지 못했다. 같은 이유로 리볼버·나이프·플레이어 자동 카드의 실제 Play Mode 입력 QA도 수행하지 않았다.

## GSV10 GameScene 적 선택 WANTED UI

- 기존 GameScene IMGUI 상대 선택을 `OpponentSelectionOverlay.prefab`과 재사용 가능한 `OpponentWantedPoster.prefab`의 uGUI 구조로 교체했다. 오버레이는 정렬 순서 110의 1920×1080 `CanvasScaler`, 비활성 `ContentRoot`, 좌우 포스터 슬롯 2개를 직렬화하며 선택·확인 `Button`은 두지 않는다.
- 포스터는 기존 `wanted.png`, 프로필별 초상화, 한국어 TMP 폰트, 영혼·골드 아이콘을 재사용한다. 이름, `×영혼`, 3줄 설명, `×처치 골드`를 표시하고 자식 Graphic의 raycast는 모두 끈다. 루트만 포인터를 받아 호버 시 금색 `Outline`과 1.04배 확대를 적용·복원한다.
- 왼쪽 클릭은 포스터 입력을 즉시 잠근 뒤 프로필 키를 한 번만 방출한다. `GameFlowController.RequestSelectOpponent`는 현재 화면, Offer ID, 후보 키, 저장·처리 입력 잠금을 검증한 뒤 `TrySelectOpponent`를 한 번 호출한다. 우클릭·중복 클릭·비활성 슬롯은 무시하며 기존 포커스·확정 API는 레거시 호환용으로 유지한다.
- `GameScene` 루트에 `UIOpponentSelection` 프리팹 인스턴스를 추가했고, `GameManager.prefab`과 해당 씬 인스턴스에 남아 있던 레거시 `OpponentSelectionView` 컴포넌트를 제거했다. 작업 전부터 존재한 GameScene 덱·렌더링 변경과 `Table Controller.prefab` 변경은 정리하지 않고 보존했다.
- GSV10 집중 테스트는 최종 4/4(job `3c88a28161324eba973265efc6e5d00f`), 기존 `OpponentSelectionPresentationTests`와 `GameSceneFullFlowPresentationTests`는 20/20(job `27e7234fb75f47c1a702a4cd6466a794`), StageProgression EditMode 어셈블리는 269/269(job `40b6fdca667e4a9c8041dd704c6575ba`) 통과했다.
- Unity 컴파일과 최종 Console Error는 0건이며 `GameScene` 검증은 missing script·broken prefab을 포함해 issue 0건이다. 저장 데이터를 변경하지 않는 Play Mode 런타임 표시 주입으로 1280×720과 1920×1080에서 좌우 배치, 초상화·이름·영혼·설명·골드, 첫 포스터 호버 외곽선·확대와 잘림 없음을 확인했다. 클릭 1회·우클릭·비활성 입력 계약은 GSV10 자동 테스트로 확인했으며, 실제 정식 저장 세션의 전투 전환 수동 입력은 수행하지 않았다.
- 도감 프리팹의 편집 미리보기 방식을 따라 `OpponentWantedPosterView`와 `OpponentSelectionView` 전용 Inspector 미리보기를 추가했다. 포스터는 실제 적 프로필을 이전·다음으로 넘길 수 있고, 오버레이는 실제 후보 2장을 쌍으로 넘길 수 있다. 두 미리보기 모두 호버 상태를 직접 확인하며 새 미리보기 오브젝트를 만들지 않고 프리팹에 직렬화된 슬롯만 사용한다.
- 미리보기 중 저장·Play Mode 전환·스크립트 리로드가 발생하면 이름·초상화·수치·활성 상태·외곽선·배율 등 저작 값을 먼저 복원한다. 프리팹 저장 뒤에는 같은 적 페이지와 호버 상태로 미리보기를 재개하므로, 미리보기 데이터는 저장되지 않고 사용자가 수정한 `RectTransform` 배치는 유지된다.
- 미리보기 계약을 포함한 GSV10은 6/6(job `41a1a25ce5694421b33126079305e6b9`), 인접 프레젠테이션 테스트는 20/20(job `7657cc3f24934cde91b78c350dd52484`), StageProgression EditMode 어셈블리는 272/272(job `d66d3cb7c8964845b79d3dbc060bbf3f`) 통과했다. 어셈블리 실행 중 기존 머티리얼 드로어·URP 셰이더 오류가 재현됐고, Console 정리 후 최종 Unity 재컴파일 기준 Error는 0건이다.

## GSH02 공용 호버 설명 SO

- 라이터·위스키, 양측 드로우/디스카드 덱, HIT·STAND·CHANGE, 계약서, 도감, 맘몬 주사위의 12개 설명을 `HoverDescriptionSO`로 저작했다. 기본/상태별 본문과 `{price}`·`{amount}`·`{gold}` 토큰을 지원하며 빈 제목·본문, 중복 상태 키, 지원하지 않거나 치환되지 않은 토큰을 거부한다.
- `HoverDescriptionTarget`이 월드 기준점과 위/아래 방향, 런타임 토큰을 `CardHoverBadgeRequest`로 변환한다. `GameManager`는 일반·악마 카드가 아닌 월드 대상을 이 컴포넌트로 찾고 기존 `GameHudView.ShowCardHoverBadge`만 사용한다. 입력 잠금·모달·포인터 이탈 시 설명을 정리하며, 도감 내부 카드처럼 오버레이가 소유한 기존 호버 경로는 유지한다.
- 상점의 구형 월드 `HoverBadge` 자식은 제거했다. 독립·정식 상점 모두 실제 가격과 위스키 회복량을 토큰으로 전달하고, 최대 영혼에서는 `soul-full` 본문을 사용한다. 정식 표시 모델은 `WhiskeyRecoveryAmount`와 `IsPlayerSoulFull`을 투영한다.
- 비활성 전투 명령도 표시 중에는 Collider를 유지해 설명을 볼 수 있다. `TryGetCommand`는 기존대로 비활성 클릭을 거부하며 월드 명령의 호환용 `Tooltip` 문자열은 비워 둔다. 플레이어 덱의 `DeckClickable`은 유지했고 상대 덱에는 추가하지 않았다.
- 신규 `GSH02` 9/9(job `f20c52c8608f4634984100678f719b59`, 최종 재실행 job `3f946cb09ab04c01a56bac381d22e358`)와 영향 클래스 102/102(job `28ccaa5c2ee04bb1b3bebd7602d2011f`)가 통과했다. 전체 EditMode는 1,099개 중 1,094개 통과(job `d8c69b8c0dc64fa18f9302f46a5674cc`)이며 실패 5건은 병행 도감 3건, 기존 덱 외곽선 1건, 기존 말풍선 카메라 스택 1건이다. 최종 재컴파일 직후 Console Error는 0건이다. Play QA에서는 기존 `CardView` 머티리얼 드로어 오류 1건이 재현됐고 이 작업의 호버 경로 오류는 없었다.
- 저장 데이터를 변경하지 않는 Play Mode 표시 주입으로 1920×1080과 1280×720에서 12개 고유 설명의 공용 툴팁 활성, 한국어 제목·본문, 미해결 토큰 0건을 확인했다. 1080p 캡처에서 비활성 HIT의 설명 표시·Collider 유지·클릭 거부를, 720p 캡처에서 위스키의 회복량·가격·영혼 최대 변형을 확인했다. 플레이어 드로우 덱에는 클릭 컴포넌트가 있고 상대 드로우 덱에는 없음을 런타임에서 확인했다. 운영체제 실제 마우스로 12개를 순차 이동하는 입력 검증은 수행하지 않았다.

## GSH03 상점 카드 호버 문구 통일

- 정식 상점과 독립 상점의 일반 카드는 별도 영문 요약이나 `PRICE ... GOLD`를 툴팁 본문에 넣지 않고, 전투 카드와 같은 `CardDefinition.Description`을 사용한다. 구매 가격은 기존 `ShopCardOptionViewModel.Price`·`PriceAmount`와 카드 아래 `ShopCardOfferStatusView`에만 유지한다.
- 악마 카드는 기존 전투 공용 `Summary`·`CostSummary` 상세를 유지한다. 라이터·위스키는 별도 가격표가 없으므로 기존 한국어 기능·가격·회복량·영혼 최대 상태 설명을 유지한다. 공개 API, 씬, 프리팹, SO는 변경하지 않았다.
- 신규 `GSH03` 2/2(job `90a3ffaa5f2c4f4e87d5b8d1ea7fa40a`)와 영향 클래스 `ShopControllerTests`·`FormalRunPresentationTests` 24/24(job `064d86d72c974d22b67f1f4ba58029a7`)가 통과했다. 전체 EditMode는 작업 정책에 따라 실행하지 않았고, 최종 재컴파일 직후 Console Error는 0건이다.
- 저장을 변경하지 않는 Play Mode 정식 상점 표시 주입으로 1920×1080과 1280×720에서 일반 카드 툴팁의 한국어 제목·전투 동일 본문·가격 문구 미포함·잘림 없음을 확인했다. 카드 가격표 5개, 악마 상세, 라이터·위스키 값 보존은 자동 테스트로 확인했다. Game View를 1920×1080, 활성 씬을 clean `MainMenuScene`으로 복원했다.

## 튜토리얼 대사·행동 강조 표시

- 튜토리얼 대사는 `TutorialScriptSO`에서 `**굵게**`, `(색상)다음 어절`, `(전체 색상)현재 줄 끝` 표기를 사용한다. 런타임 변환 태그는 타자 효과의 표시 글자 수에서 제외한다.
- 대사 끝의 `(히트 버튼 하이라이트)` 같은 지시문은 화면에 출력하지 않는다. 다음 행동 게이트가 실제로 열린 뒤 전체 화면을 어둡게 하고 Hit, Stand, Change, 현재 리볼버 카드 또는 계약서만 원형으로 밝힌다.
- 계약 전 아스모데우스 카드는 튜토리얼 바인딩 직후부터 테이블에 남아 호버 정보가 표시된다. 계약 뒤에는 별도 카드를 숨기고 실제 계약 카드가 대사의 화자가 된다.
- 시작 흐름은 상대 입장 완료, 첫 대사 7줄, 1라운드 손패 진입, 2초 대기, 블랙잭 설명 순서다. 1라운드 결과 설명이 끝날 때까지 이전 손패 표시를 유지하고, 2라운드 전환에서 실제 카드 퇴장과 새 손패 진입을 직렬화한다.

## 튜토리얼 대사 타이밍·호버·행동 해골 보완

- 색상 마크업은 현재 굵은 영역을 넘지 않으며 `(색 빼기)`에서 즉시 종료된다. 명령 문자열은 출력되지 않는다.
- 플레이어 행동 뒤 상대 행동과 GameManager 타임라인이 모두 끝날 때까지 다음 대사를 보류한다. 리볼버는 카드 선택과 숫자 5 선택·결과 완료 게이트를 분리했다.
- 내 덱 안내는 플레이어 드로우 덱만 강조한다. 덱 미리보기를 실제로 열고 닫아야 다음 대사로 진행한다.
- 대사 중 카드·악마·덱·전투 버튼·계약서·도감 호버와 툴팁은 유지하되 월드 클릭 명령은 전달하지 않는다.
- 양쪽 행동 해골은 라운드 시작부터 표시하고 버튼 비활성화·대사·상대 턴에도 마지막 위치를 유지한다. 새 라운드에는 보이는 상태로 홈 위치에 복귀하며 패배 디졸브만 예외다.
- 아스모데우스 마지막 대사는 강제 Hit와 전투 타임라인, 비교·영혼 감소, 패자 해골 디졸브와 상대 퇴장이 끝난 뒤 출력한다.

## 튜토리얼 리볼버·상대 타임라인·행동 해골 재보완

- 리볼버 카드 게이트는 숫자 선택 UI가 실제로 열린 시점에 끝난다. 설명 대사가 끝난 뒤 숫자 5 선택 게이트를 열고, Pending 카드 효과와 결과 타임라인이 모두 끝난 뒤 다음 대사로 진행한다.
- 튜토리얼 게이트는 전투 타임라인 재생 중 논리 최종 상태를 평가하지 않는다. 제한 UI의 재렌더 요청도 타임라인 완료 후 한 번만 적용해 상대 행동이 선행 표시됐다가 되돌아가는 현상을 막는다.
- 행동 해골은 일반 전투와 같이 행동 전에는 숨긴다. 튜토리얼이 다음 라운드 화면을 보류하는 동안에는 Stand 위치를 유지하고, 실제 라운드 전환 시 숨김·홈 초기화한다.

## 튜토리얼 하이라이트 종료·말풍선 화자별 배치

- Hit·Stand·Change는 유효한 행동이 수락되는 즉시 스포트라이트를 제거한다. 리볼버는 카드 사용 수락 시, 계약서는 계약 후보 UI가 실제 렌더될 때 제거한다. 게이트와 다음 대사 대기 순서는 유지한다.
- 플레이어 드로우 덱 스포트라이트는 바깥 암부 84%, 페더링 10px을 사용한다. 다른 대상은 기존 72%, 16px을 유지한다.
- 화자 추적 오프셋은 계약 전 `(-56, 72)`px, 계약 후 `(56, 72)`px을 사용한다. 그 안의 `SpeechTextBubble`은 계약 전 `(-115, 0)`, 계약 후 `(126.6, 30.743)`으로 배치한다. 계약 후 배경·꼬리를 수평 반전하고 텍스트는 상쇄 반전해 읽는 방향을 유지한다.
- 신규 회귀 5/5가 통과했다. 전체 EditMode는 1,318개 중 1,299개 통과했으며, 잔여 19건은 기존 도감·카드·말풍선·상점·설정 자산 기준 불일치다. C# 컴파일 오류는 0건이다.

## 튜토리얼 자동 발동 안내·공용 대사 입력

- 튜토리얼과 런 승리·패배 대사는 좌클릭 외에 Space, Enter, Numpad Enter로도 진행한다. 타자 출력 중 첫 입력은 현재 줄만 완성하고, 다음 입력부터 다음 줄로 이동하는 기존 규칙을 공유한다.
- 튜토리얼 3라운드 Change 후보는 1과 10을 유지한다. 선택 뒤 상대가 스페이드 `3. 거짓말 탐지기`를 Hit하고 숫자 6을 선언하며, 자동 카드 연출이 끝난 뒤 실제 비교 결과에 따라 `6 미만` 또는 `6 이상` 안내를 출력한다.
- 자동 발동 카드 안내 5줄이 끝나면 3초 기다린 뒤 `그나저나 … 게임이 정말 따분하군.`으로 계약·악마·도감 설명을 시작한다. `(하늘색)`은 `#64D8FF`로 변환하고, 닫히지 않은 어절 색상 표기가 굵은 영역 안에 있으면 해당 굵은 영역 전체를 채색한다.
- 내 덱 또는 도감 오버레이가 열린 동안에는 튜토리얼 대사 진행 입력을 무시한다. 오버레이가 닫힌 다음 프레임부터 클릭·Space·Enter 입력을 다시 받는다.
