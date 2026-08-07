# 전투 공통 Prompt UI 구현 계획

## CP-00 — 설계와 계약 확정

- 24개 Prompt 범위와 ID 숫자 고정
- 선택 안내와 상태 제목의 책임 분리
- SO 토큰과 실패 정책 확정

검증: 설계·개발 명세 문서 검토.

## CP-01 — 순수 모델과 프레젠테이션

- Pending 타입의 문자열을 `CombatPromptId`로 교체
- `CombatPromptRequest`와 `CoreLoopViewModel.SelectionPrompt` 추가
- GameScene HUD 모델에 `SelectionPrompt`/`HeaderText` 분리

검증: `[Category("CP01")]`, 관련 프레젠테이션 테스트, CoreLoop EditMode 어셈블리.

## CP-02 — SO와 공통 View

- 24개 한국어 문구 카탈로그 생성
- 토큰 치환·검증·오류 1회 기록
- `CombatPromptView` 즉시 표시·숨김 및 동일 ID 문구 갱신
- IMGUI가 같은 SO 사용

검증: 카탈로그 단위 테스트와 컴파일/Console 오류 확인.

## CP-03 — 에셋 배선과 화면 검증

- `CombatPrompt.prefab` 생성 및 HUD 중첩
- 위치·앵커·피벗·크기는 프리팹 직렬화 값을 그대로 사용하고 HUD 빌더의 배치 하드코딩 제거
- 리볼버 전용 Prompt 제거
- `CoreLoopTest` SO 참조 연결
- HUD 빌더 재현성 갱신

검증: 에셋 테스트, 빌더 실행 전후 프리팹 해시 비교, GameScene validation, 해상도별 수동 확인.

## CP-04 — Prompt와 선택 UI 표시 시점 동기화

- 입력 잠금 중 Prompt 선택 상태의 전체 HUD를 `Hidden` 처리
- 사탄·수정 구슬·카드 교체 월드 선택기를 같은 HUD 가시성에 연결
- 손패 직접 선택 명령과 강조를 잠금 동안 제거하고 해제 렌더에서 복구
- CoreLoop 규칙, 선택 결과, 카메라·무기 연출 순서 유지

검증: `[Category("CP04")]`, `CombatPromptTests`,
`GameSceneCombatHudPresentationTests`, 카드 선택 관련 테스트,
CoreLoop EditMode 어셈블리, GameScene 해상도별 수동 확인.
