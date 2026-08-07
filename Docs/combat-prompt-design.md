# 전투 공통 Prompt UI 설계

## 목표

전투 중 플레이어에게 다음 선택을 요구하는 안내 문구를 하나의 공통 UI로 통합한다. 카드 사용 자체는 기존처럼 클릭 즉시 실행하며, 버튼·숫자 다이얼·월드 카드 선택기는 유지한다.

## 범위

- 카드 교체 1종
- 수동 카드 선택 3종
- 자동 카드 선택 6종
- 악마 계약 상호작용 14종
- `GameScene` uGUI와 `CoreLoopTest`/`StageTest` IMGUI가 동일한 문구 자산 사용

상태 제목, 버튼 라벨, 효과 결과, 상점 말풍선은 범위 밖이다.

## 결정

- 순수 규칙 계층은 표시 문자열 대신 명시적 숫자의 `CombatPromptId`만 보유한다.
- 표시 요청은 `CombatPromptRequest` 하나로 통합한다.
- 문구와 토큰 치환은 단일 `CombatPromptCatalogSO`가 담당한다.
- `GameScene`은 `CombatPrompt.prefab` 하나만 표시하고, IMGUI는 같은 SO를 직접 해석한다.
- 누락·잘못된 항목은 한 번 오류를 기록하고 안내를 숨긴다.
- 같은 Prompt ID의 데이터 갱신은 문구만 바꾸고 다시 깜빡이지 않는다.

## 시각 규칙

- 위치·앵커·피벗·크기는 `CombatPrompt.prefab`에 저장된 `RectTransform`을 유일한 기준으로 삼는다.
- HUD 빌더와 런타임은 프리팹 배치를 계산하거나 덮어쓰지 않는다.
- 반투명 배경과 자동 크기 조절 TMP
- raycast 비차단
- 표시와 숨김은 보간 없이 즉시 적용한다.
