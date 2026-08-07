# 전투 공통 Prompt UI 진행 로그

## CP-00

- 2026-08-07: 안내만 통합하고 버튼·숫자 다이얼·월드 선택기는 유지하는 범위를 확정했다.
- 2026-08-07: 24개 고정 ID, 단일 SO, 프리팹 배치 기준, 동일 문구 공유 정책을 문서화했다.

## CP-01

- 2026-08-07: 순수 Pending 타입의 Prompt 문자열을 `CombatPromptId`로 교체했다.
- 2026-08-07: `CoreLoopViewModel.SelectionPrompt`와 GameScene의 `SelectionPrompt`/`HeaderText` 분리를 구현했다.

## CP-02

- 2026-08-07: 24개 한국어 문구와 4개 토큰을 가진 `CombatPromptCatalogSO`를 추가했다.
- 2026-08-07: 공통 `CombatPromptView`와 레거시 IMGUI의 동일 SO 해석을 구현했다.
- 2026-08-07: `CombatPromptView`의 페이드 트윈을 제거하고 표시·숨김을 즉시 적용하도록 변경했다.

## CP-03

- 2026-08-07: `CombatPrompt.prefab` 생성, HUD 중첩, 리볼버 Prompt 제거, `CoreLoopTest` 참조 배선을 완료했다.
- 2026-08-07: HUD 빌더의 Prompt 배치 하드코딩을 제거했다. 빌더는 원본 프리팹을 그대로 중첩하며 필수 참조와 공용 카탈로그 연결만 검증한다.

## 검증

- 2026-08-07: 구현 코드 컴파일 오류 0을 확인했다.
- 2026-08-07: 즉시 표시·숨김 테스트 Unity MCP job `bef1a24ae91c44a1a73086443be8ac57` 1/1 통과, 전체 `CP01` job `a996ad5fe4a44dd1804c0357c77acf28` 8/8 통과.
- 2026-08-07: Unity MCP job `f0d5da395ef743c2a8b78186d08b994c`에서 `CP01` 8/8 통과. 24개 도메인 상태-ID 매핑과 1280×720/1920×1080 상단 배치·선택기 비가림 계산 검증을 포함한다.
- 2026-08-07: Unity MCP job `e2111188208a4703964cb8c491ae4c7d`에서 갱신된 `CP01` 7/7 통과. HUD 인스턴스가 원본 `CombatPrompt.prefab`의 앵커·피벗·위치·크기와 대응 원본을 그대로 유지하는지 검증했다.
- 2026-08-07: `DiaBlackJack/Build Combat Prompt` 실행 전후 `CombatPrompt.prefab` SHA-256가 `854937669D7C72CF6899798CD1D019F41ED4FDDF7B4F5DE2888E9FFE892FC9C1`로 동일함을 확인했다.
- 2026-08-07: Unity MCP job `07d27acfdb494f87829a7e603b4f4ec2`에서 `CoreLoopPresentationTests` + `GameSceneCombatHudPresentationTests` 127/127 통과.
- 2026-08-07: CoreLoop EditMode 어셈블리 job `00afbac4a77946a1900c7fcf7fafcdf2`는 859건 중 849 통과, 현재 다른 세션이 수정 중인 Codex/카드 스프라이트/덱 외곽선/말풍선/라이터 에셋 테스트 10건이 실패했다. Prompt 관련 실패는 없었다.
- 2026-08-07: `GameScene` validation 결과 0 issues, missing script 0, broken prefab 0.
- 2026-08-07: `GameScene` Play Mode smoke 후 Console 게임 오류 0을 확인했다.
- 실제 Game View에서의 1280×720/1920×1080 육안 검증은 수행하지 않았다.
