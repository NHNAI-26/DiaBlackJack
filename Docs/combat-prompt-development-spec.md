# 전투 공통 Prompt UI 개발 명세

## 순수 계약

`CombatPromptId`는 `None = 0`, 실제 24개 ID는 `1..24`의 고정 숫자를 사용한다.

`CombatPromptRequest` 필드:

- `Id`
- `SourceDisplayName`
- `ContextText`
- `CurrentCount`
- `RequiredCount`

`PendingCardEffect`, `PendingAutomaticCardInteraction`, `PendingDemonContractInteraction`은 문자열 대신 `PromptId`를 노출한다. `CoreLoopViewModel`은 선택 안내를 `SelectionPrompt` 하나로 제공한다.

## 카탈로그

`CombatPromptCatalogSO.TryResolve(request, out text)`가 다음 토큰을 치환한다.

- `{source}`
- `{context}`
- `{current}`
- `{required}`

검증은 빈 문구, 중복/누락 ID, 미지원·잘못 닫힌 토큰을 탐지한다.

## GameScene

`GameSceneCombatHudViewModel`은 `SelectionPrompt`와 비선택 상태용 `HeaderText`를 분리한다. `GameHudView`는 선택 안내를 `CombatPromptView`로만 렌더한다. 리볼버 숫자 선택기는 숫자·이동·확정 UI만 가진다.

`SelectionPrompt`가 존재하는 동안 입력이 잠기면
`GameSceneCombatHudPresenter`는 전체 선택 HUD를 `Hidden`으로 투영한다.
사탄·수정 구슬·카드 교체의 별도 월드 선택기와 손패 카드 직접 선택 명령·강조도
같은 가시성 판정을 사용한다. 잠금 해제 뒤 단일 `RefreshView`에서 Prompt와 선택
표면을 함께 표시한다. 선택용 카메라와 선행 연출 순서는 바꾸지 않는다.

`CombatPrompt.prefab`의 `RectTransform`이 배치의 유일한 원본이다. HUD 빌더는 원본 프리팹을 중첩하고 필수 참조만 검증하며, 앵커·피벗·위치·크기를 코드에서 지정하거나 인스턴스 오버라이드로 저장하지 않는다.

## 레거시 IMGUI

`CoreLoopView`는 직렬화된 같은 `CombatPromptCatalogSO`로 `SelectionPrompt`를 해석한다. `CoreLoopTest` 씬에 참조를 저장하며 `StageTest`에서 전투 씬으로 진입해도 동일 자산을 사용한다.

## 실패 처리

- 잘못된 요청 숫자와 `None` ID는 생성 단계에서 거부한다.
- 런타임 카탈로그 누락/오류는 ID별 한 번만 오류를 기록하고 숨긴다.
- 선택 종료, 입력 잠금, 상점, 전투 외 연출에서는 `SelectionPrompt = null`이며
  Prompt가 소유하는 선택 표면도 함께 숨긴다.
