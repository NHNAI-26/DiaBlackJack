# 설정·일시정지 메뉴 진행 기록

> 프로젝트: DiaBlackJack
> 작업 범위: SET-00~SET-06
> 버전: v1.1
> 최종 갱신: 2026-08-09

## 1. 완료 내용

### SET-00

- 기존 `SettingsSystem`, 그래픽 UI, 볼륨 이벤트, `SoundManager`를 대조했다.
- 데모 설정 프리팹은 운영 씬에 연결하지 않기로 결정했다.
- 사용자 작업 중인 `GameScene`, `GameManager`, HUD·프리팹 변경을 보존했다.

### SET-01~SET-03

- `GameWindowMode`, `GameSettingsSnapshot`, `GameSettingsDefaultsSO`를 추가했다.
- `PlayerPrefsSettingsRepository`와 버전 1 키를 추가했다.
- 해상도 중복 제거·정렬·최고 주사율 선택과 Unity 화면 모드 매핑을 추가했다.
- `SettingsSystem`을 단일 `DontDestroyOnLoad` 서비스로 재구성했다.
- 시작·씬 로드 뒤 기존 Master/BGM/SFX 이벤트를 발행하도록 연결했다.

### SET-04

- `PauseSettingsController`와 `UISettingsArrowSelector`를 추가했다.
- `GameManager`에 일시정지 입력 차단과 임시 오버레이 닫기 API를 추가했다.
- ESC 모달 우선순위, 종료 확인, Editor Play Mode 종료를 연결했다.

### SET-05

- `SettingsSystem.prefab`, `PauseSettingsCanvas.prefab`, 기본값 에셋과 삼각형 Sprite를 생성했다.
- `SettingsSystem`을 `StageTest`와 `GameScene`에 배치했다.
- `PauseSettingsCanvas`를 `GameScene`에 배치했다.
- `GameScene` 검증 결과 missing script 0, broken prefab 0이었다.
- 1920×1080 설정 화면의 모든 선택기·슬라이더·뒤로 버튼이 화면 안에 있고
  서로 겹치지 않음을 확인했다.
- 플레이어 빌드에서 `Editor` 전용 도구가 런타임 어셈블리에 포함되던 문제를
  `UNITY_EDITOR` 경계로 차단했다.
- 중첩 배치된 `SettingsSystem`을 런타임 루트로 승격한 뒤
  `DontDestroyOnLoad`하도록 수정해 씬 전환 경고를 제거했다.
- 화면 적용 검증이 해상도뿐 아니라 실제 `FullScreenMode`도 비교하도록 보강했다.
  현재 Radeon 드라이버가 독점 전체 화면을 거부하거나 테두리 없는 모드로 내리면
  설정값만 성공한 것처럼 남기지 않고 이전 설정으로 복구한다.
- SO와 `CardDefinitionCatalog`의 카드 설명을 동기화해 테스트 실행 순서에 따라
  호버 설명이 달라지던 회귀를 제거했다.

### SET-06

- 해상도·창 모드 데이터, 적용 서비스, 운영 UI와 레거시 데모 그래픽 컴포넌트를 제거했다.
- `HoverTooltipSize`의 `Small / Normal / Large`와 `1 / 1.3 / 1.5` 배율을 추가했다.
- PlayerPrefs 버전 1 오디오를 유지하면서 기본 `Normal`로 읽고, 다음 저장에서 버전 2와 새 크기를 기록하도록 이관했다.
- 공유 설정 프리팹의 해상도 선택기를 `호버 툴팁 크기` 선택기로 바꾸고 창 모드 행을 제거했다.
- `GameHudView`가 설정 초기값과 변경 이벤트를 공용 `CardHoverTooltipRoot`에 적용하도록 연결했다.
- 기존 작업 중인 `HUD.prefab`과 `GameScene.unity`는 수정하지 않았다.

## 2. 검증 결과

| 날짜 | 검증 | 결과 |
| --- | --- | --- |
| 2026-08-09 | SET-06 컴파일 | Unity 스크립트 컴파일, Console Error 0 |
| 2026-08-09 | SET06 대상 EditMode | 12/12 통과, job `9c276e34bf4348e5b38deec315b12b5d` |
| 2026-08-09 | Settings EditMode | 18/19 통과, job `bde93c3489584e5fa7af6288c84c9e23`; SET06은 전부 통과, 기존 `UIR01_U07`의 `Brush_UI_8` 기대와 현재 `Brush_UI_16` 불일치 1건 |
| 2026-08-09 | 관련 HUD EditMode | 121/122 통과, job `5c737ffde87e40b4b4c092fa9d25bffd`; 작업 전부터 수정 중인 `HUD.prefab` 외곽선 색상 기대 불일치 1건 |
| 2026-08-09 | 전체 EditMode | 1269/1288 통과, job `c16eeccd0aa14a02bed1d2c4e7021780`; SET06 실패 0, 현재 병행 작업·에셋 기대 불일치 19건 |
| 2026-08-09 | `GameScene`·`MainMenuScene` validation | 두 씬 모두 missing script 0, broken prefab 0 |
| 2026-08-09 | 운영·레거시 설정 프리팹 검사 | 두 프리팹 모두 missing script 0, missing prefab 0; 운영 선택기 1개와 삭제 대상 행 부재 확인 |
| 2026-07-30 | 설정 EditMode | 최종 14/14 통과, job `9e9b4d00fa7946ccbc7fb4921fb4d790` |
| 2026-07-30 | 전체 EditMode | 최종 781/781 통과, job `afbf02707a0d4ba7aa713f40087e1a30` |
| 2026-07-30 | 직접 GameScene 1280x720 | 일시정지·설정·종료 취소·볼륨 표시·화살표 방향 확인 |
| 2026-07-30 | 일시정지 시간 | 메뉴에서 0, 복귀 후 1 확인 |
| 2026-07-30 | 설정 저장 | 설정 화면 종료 뒤 PlayerPrefs 버전 키 생성 확인 |
| 2026-07-30 | 씬 서비스 수 | StageTest 1개, 전환 뒤 GameScene 1개 확인 |
| 2026-07-30 | 직접 GameScene Console | 설정 화면 조작 뒤 오류·경고 0 |
| 2026-08-02 | 1920×1080 Game View | 시각 판정 96/100, 패널 잘림·겹침 0 |
| 2026-08-02 | Windows 창 모드 | 1280×720 `Windowed` 요청·실제 상태 일치 |
| 2026-08-02 | Windows 테두리 없는 전체 화면 | 네이티브 해상도 `FullScreenWindow` 요청·실제 상태 일치 |
| 2026-08-02 | Windows 독점 전체 화면 | 드라이버의 DXGI 거부·모드 강등 확인, 실제 모드 검증과 이전값 복구 추가 |
| 2026-08-02 | 설정 저장 재실행 | 1920×1080·테두리 없는 전체 화면 저장 스냅샷 재로드 확인 |
| 2026-08-02 | `StageTest → GameScene` 강제 전환 5회 | 매회 런타임 오브젝트 1,547개, missing script 0, Console 오류·경고 0 |
| 2026-08-02 | 전체 EditMode | 891/891 통과, job `9f62901d9c1647fcb1b1a088af4c5837` |
| 2026-08-02 | Windows Release 전체 씬 빌드 | 211.58MB, 오류 0, job `build-3985a4d314` |

## 3. 잔여 위험

- SET-06의 1280×720·1920×1080 수동 화면 검증은 아직 남아 있다. 공유 Editor의
  자동 화면 캡처에서 MCP `ScreenshotUtility` PlayerLoop 재귀 오류가 발생해 이번 검증에서는 제외했다.
- 대상 재컴파일과 SET06 재실행 시 Console Error는 0이었다. 전체·영향 테스트 뒤에는
  기존 머티리얼 drawer 오류 2건이 다시 기록됐으며 SET06 스택은 없다.
- 현재 전체 EditMode 실패 19건은 SET06 대상 테스트와 분리되어 있다. 이 중 설정 어셈블리의
  `UIR01_U07`은 현재 임포트된 패널 Sprite 이름 `Brush_UI_16`과 과거 기대값
  `Brush_UI_8`의 불일치다.
- 과거 Radeon 독점 전체 화면 실패 경로는 SET-06에서 화면 설정 기능과 함께 제거했다.
- 과거의 간헐적 Missing Script 로그는 정적 검사와 강제 전환 5회에서 재현되지 않았다.
  현재 확인된 관련 경고는 중첩 `SettingsSystem`의 비루트 영속화였으며 수정 완료했다.
- 테스트 러너의 Performance Testing 준비·정리 메시지와 개발 빌드의 MCP 로컬 연결
  실패 메시지는 제품 설정 로직 오류와 구분한다.

## 4. UIR01 brush/default 프리팹 전환

- `PauseSettingsCanvas.prefab`의 일시정지·설정·종료 확인 패널 3개에 `Brush_UI_8` sliced 배경을 적용했다.
- 계속하기·설정·게임 종료·뒤로·예·아니오 6개 일반 버튼을 `DefaultButton.prefab` 중첩 인스턴스로 교체했다. 해상도·창 모드 화살표는 기존 `TriangleArrow`, 슬라이더와 선택기 구조는 유지했다.
- 기존 직렬화 필드, 배치, `settingsOnlyMode`, 뒤로가기와 `timeScale` 계약을 유지했다. 같은 설정 프리팹을 참조하는 `GameScene`과 `MainMenuScene`에 별도 씬 중복 수정 없이 반영된다.
- `UIR01_U07` 단독 1/1(job `074ce40322864817876f22622e6130ce`)과 기존 설정 회귀를 포함한 Settings EditMode 16/16(job `7059400cbedc4d31ba7b8149b00efeba`)이 통과했다. 런타임에서 설정 패널 활성, 뒤로 버튼 입력 후 패널 비활성, `timeScale` 1 유지와 한국어 버튼 라벨·전용 화살표 보존을 확인했다.
- 두 씬의 설정 프리팹 인스턴스는 각각 1개이며 missing script 0, broken prefab 0이다. 컴파일·씬 검증 직후 Console Error는 0건이었고, Play QA에서는 설정과 무관한 기존 머티리얼 drawer 오류 2건이 재현됐다.
