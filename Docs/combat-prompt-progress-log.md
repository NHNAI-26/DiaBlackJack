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
- 2026-08-07: Unity가 중첩 프리팹 루트 `RectTransform` 배치를 원본 변경 전파 대상으로 취급하지 않는 문제를 확인했다. HUD에 붙는 루트는 전면 스트레치 컨테이너로 고정하고, 실제 표시 위치·앵커·크기는 `CombatPrompt.prefab/LayoutRoot`가 온전히 소유하도록 변경했다.
- 2026-08-07: Prompt 전용 빌더가 리볼버 프리팹을 재생성하지 않게 분리하고, 이미 올바르게 연결된 Prompt 인스턴스는 재생성하지 않도록 변경했다.

## CP-04

- 2026-08-08: 입력 잠금 중 새 선택 상태가 생기면 전체 선택 HUD를 `Hidden`으로
  투영하고, 잠금 해제 뒤 Prompt와 선택 UI를 같은 렌더에서 함께 표시하도록
  변경했다.
- 2026-08-08: 사탄·수정 구슬·카드 교체 월드 선택기를 공통 HUD 가시성에
  연결하고, 손패 카드의 직접 선택 명령과 강조를 잠금 동안 제거한 뒤 해제
  렌더에서 복구하도록 변경했다.
- 2026-08-08: CoreLoop 규칙, 선택 결과, 선택 카메라와 무기 연출 순서는
  변경하지 않았다. 씬·프리팹 수정도 없다.

## 검증

- 2026-08-07: 구현 코드 컴파일 오류 0을 확인했다.
- 2026-08-07: 즉시 표시·숨김 테스트 Unity MCP job `bef1a24ae91c44a1a73086443be8ac57` 1/1 통과, 전체 `CP01` job `a996ad5fe4a44dd1804c0357c77acf28` 8/8 통과.
- 2026-08-07: Unity MCP job `f0d5da395ef743c2a8b78186d08b994c`에서 `CP01` 8/8 통과. 24개 도메인 상태-ID 매핑과 1280×720/1920×1080 상단 배치·선택기 비가림 계산 검증을 포함한다.
- 2026-08-07: Unity MCP job `e2111188208a4703964cb8c491ae4c7d`의 값 일치 검증만으로는 중첩 프리팹 루트 `RectTransform`의 변경 미전파를 검출하지 못했음을 확인했다.
- 2026-08-07: `DiaBlackJack/Build Combat Prompt` 실행 전후 `CombatPrompt.prefab` SHA-256가 `854937669D7C72CF6899798CD1D019F41ED4FDDF7B4F5DE2888E9FFE892FC9C1`로 동일함을 확인했다.
- 2026-08-07: Unity MCP job `07d27acfdb494f87829a7e603b4f4ec2`에서 `CoreLoopPresentationTests` + `GameSceneCombatHudPresentationTests` 127/127 통과.
- 2026-08-07: CoreLoop EditMode 어셈블리 job `00afbac4a77946a1900c7fcf7fafcdf2`는 859건 중 849 통과, 현재 다른 세션이 수정 중인 Codex/카드 스프라이트/덱 외곽선/말풍선/라이터 에셋 테스트 10건이 실패했다. Prompt 관련 실패는 없었다.
- 2026-08-07: `GameScene` validation 결과 0 issues, missing script 0, broken prefab 0.
- 2026-08-07: `GameScene` Play Mode smoke 후 Console 게임 오류 0을 확인했다.
- 2026-08-07: `LayoutRoot`에 HUD 오버라이드가 없음을 검증한 Unity MCP job `f9ccfc024d1c4e1caa8305370d3adccf` 1/1 통과, 전체 `CP01` job `a4f6385daa3a4919b863deb41be95324` 8/8 통과.
- 2026-08-07: 수정 후 `GameScene` validation 0 issues, missing script 0, broken prefab 0을 확인했다.
- 2026-08-08: 스크립트 컴파일 후 Console Error 0을 확인했다.
- 2026-08-08: 최종 `CP04` Unity MCP job
  `0df1563672e64e26b939ea79121a890f` 3/3 통과. 잠금 HUD, 사탄 선택
  게이트, 직접 선택 명령·강조 제거와 복구를 검증했다.
- 2026-08-08: `CombatPromptTests` + `GameSceneCombatHudPresentationTests` +
  `CardSelectionFanLayoutTests` job `68a1c16e878e400e9dfd790ebd4d76b6`
  114/114 통과.
- 2026-08-08: CoreLoop EditMode 어셈블리 job
  `88a8557754a44a25914018e20d6ebcef`는 882건 중 869 통과, 13건 실패했다.
  실패는 Codex·카드 콘텐츠/프리팹·덱 외곽선·말풍선·라이터·상점 에셋의
  기존/동시 작업 영역이며 CP-04 대상 테스트 실패는 없다.
- 2026-08-08: `GameScene` validation 결과 0 issues, missing script 0,
  broken prefab 0을 확인했다.
- 실제 Game View에서의 1280×720/1920×1080 육안 검증은 수행하지 않았다.

## CP-04 자동카드 결과 확장

- 2026-08-08: `AutomaticCardResultPromptRequest`와 5개 고정 ID를 추가하고, 결과 완료 시 만든 Prompt 스냅샷을 행동 턴 종료에서 제거하도록 구현했다. 기존 `LastAutomaticCardResult` 규칙 기록은 유지했다.
- 2026-08-08: 별도 HUD 결과 패널과 IMGUI 결과 박스를 제거했다. 선택 안내 우선, 결과 차선의 공통 `CombatPromptView` 렌더링으로 통합했다.
- 2026-08-08: 5종 결과 템플릿과 소유자·유지/버림·양측 결정·선언·비공개 비교·결과 상태 라벨을 `CombatPromptCatalogSO`로 이동했다.
- 2026-08-08: `DebugManager`에 `AutomaticCardDebugPanel`을 추가하고 GameScene 참조를 연결했다. 플레이어/상대 각 5종은 결정적 덱에서 실제 CoreLoop Hit/Stand 및 선택 경로를 실행한다.

## CP-04 자동카드 결과 검증

- 최종 Unity MCP job `8aeafec26fcd41c2ae294d8ced59c250`: `CP04` 17/17 통과.
- Unity MCP job `1ec70e1d6d254e05b21ceb55c07ebc1f`: `CP01` 9/9 통과.
- Unity MCP job `2c11fcb74be84665a0e7d937537b83e9`: `CoreLoopPresentationTests` + `GameSceneCombatHudPresentationTests` 136/136 통과.
- CoreLoop EditMode 어셈블리 job `cc2ae62fdb204896a556bb795e816b15`: 899건 중 885 통과, 14건 실패. 실패는 Codex·카드 콘텐츠/프리팹·외곽선·말풍선·라이터·상점 등 기존/동시 작업 에셋이며 CP-04 대상 실패는 없다.
- `GameScene` validation 0 issues, missing script 0, broken prefab 0. 최종 컴파일 오류 0, Console 오류 0을 확인했다.
- 실제 Game View 1280×720/1920×1080 육안 검증은 수행하지 않았다.

## CP-04 자동카드 1인칭·적 이름 표시

- 2026-08-08: 자동카드 결과 템플릿을 상태 나열에서 문장형 한국어로
  교체했다. 플레이어는 `나는/내`, 적은 현재
  `CoreLoopViewModel.EnemyDisplayName`을 사용하며 프로필 없는 전투는
  `적`으로 대체한다.
- 2026-08-08: `{enemy}`, `{ownerPossessive}` 토큰과 적 이름을 받는
  카탈로그·뷰 오버로드를 추가했다. 규칙 요청 구조, Prompt 우선순위,
  즉시 표시·숨김, 턴 종료 수명은 변경하지 않았다.
- 최종 Unity MCP job `6ecde15789624eee84b55a39aa4c3071`:
  `CP04` 24/24 통과. 5종 문장 치환, 6개 적 프로필 이름, 독립 전투
  대체 문구, 빈 대체 라벨 검증을 포함한다.
- Unity MCP job `370591b24e5f4d609ba7123e448d88d8`:
  `CoreLoopPresentationTests` + `GameSceneCombatHudPresentationTests`
  136/136 통과.
- `CP01` job `377576bffbb14f418abc7fd9d18fb318`은 9건 중
  8건 통과했다. 실패 1건은 이번 작업에서 수정하지 않은
  `HUD.prefab`의 중첩 `CombatPrompt` `m_fontSizeMax = 32`
  오버라이드를 검출한 기존 프리팹 위치 검사다.
- `GameScene` validation 0 issues, missing script 0, broken prefab 0.
  최종 컴파일 오류 0, Console 오류 0을 확인했다.
