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

## 5. 2026-08-09 UI 후처리 카메라 연결

- `MainMenuCanvas`와 공유 `PauseSettingsCanvas`를 `Screen Space Camera`로 저작하고, 런타임에는 현재 씬의 `UIOverlayCamera`만 찾아 다시 연결한다.
- 연결 시 전체 Canvas 계층을 `UI` 레이어로 정규화한다. 정렬 순서 100/200, 해상도 스케일러, 입력과 `UI_Brush_Grey_Deck` 머터리얼은 유지했다.
- 신규 SET10 2/2, MainMenuScene·GameScene validation 각 0 issues, 컴파일·Console Error 0을 확인했다. Play Mode 1920×1080·1280×720에서 메인 메뉴와 설정 패널의 배치·카메라 연결·`UI_Brush_Grey_Deck` 표시를 확인했다.
- `UIR01_U07` 단독 1/1(job `074ce40322864817876f22622e6130ce`)과 기존 설정 회귀를 포함한 Settings EditMode 16/16(job `7059400cbedc4d31ba7b8149b00efeba`)이 통과했다. 런타임에서 설정 패널 활성, 뒤로 버튼 입력 후 패널 비활성, `timeScale` 1 유지와 한국어 버튼 라벨·전용 화살표 보존을 확인했다.
- 두 씬의 설정 프리팹 인스턴스는 각각 1개이며 missing script 0, broken prefab 0이다. 컴파일·씬 검증 직후 Console Error는 0건이었고, Play QA에서는 설정과 무관한 기존 머티리얼 drawer 오류 2건이 재현됐다.

## 6. 2026-08-09 전투 HUD·일시정지 메뉴 개선

- 전투 영혼 표기를 `나` 또는 실제 적 이름과 현재/최대 영혼 수치의 2줄 구성으로 통일했다. 피해 연출 중 임시 수치에도 같은 포맷을 사용하며 빈 적 이름만 `상대`로 대체한다.
- Master/BGM/SFX 슬라이더 배경 트랙을 Fill Area와 같은 380px로 맞춰 채움 시작점의 회색 여백을 제거했다.
- 일시정지 메뉴에 `타이틀로` 버튼을 추가하고 `계속하기 → 타이틀로 → 설정 → 게임 종료` 순서로 100px 간격 배치했다. 일반 런은 런타임을 유지하고 튜토리얼만 임시 런타임을 정리한 뒤 메인 메뉴로 이동한다.
- 일시정지 설정 버튼은 공용 `DefaultButton`의 확대·눌림·사운드 피드백을 그대로 사용한다. 메인 메뉴 SETTINGS 영역도 기존 `SettingCollider → Telegraph.SetHoveredButton` 공용 경로를 회귀 테스트로 고정했다.
- 집중 EditMode는 SET11/MMUI01 7/7 통과(job `2d16a0273ed34f70b7b074c76b725ecf`). 전체 EditMode는 1328/1348 통과(job `94116adc253e47ca9840ebdbbdd1c656`)했고 이번 변경 관련 신규 실패는 없다. 기존 에셋·기대값 불일치 20건은 별도 잔여 항목이다.
- `MainMenuScene`, `GameScene` validation은 각각 0 issues이며 최종 Console Error는 0건이다. MCP Game View 캡처는 활성 Screen Space UI를 누락해 배경만 기록되므로 1080p/720p 육안 판정은 남아 있다.

## 7. 2026-08-09 일시정지 호버·배경 입력 차단 보강

- 공용 `UIButtonScaleFeedback` Tween을 unscaled update로 전환해 `Time.timeScale = 0`인 일시정지 중에도 1.08배 호버와 눌림 애니메이션이 진행되게 했다.
- 일시정지 메뉴가 보이는 동안 전체 화면 Backdrop raycast와 공용 gameplay blocker를 함께 활성화한다. 튜토리얼 대사 진행, 라이터 드래그, 리볼버 숫자 선택, 시작 악마 카드 호버를 차단하고 재개 시 즉시 해제한다.
- 일시정지 메뉴를 제외한 모든 `BaseRaycaster`를 메뉴가 열린 동안 비활성화하고 닫을 때 원래 활성 상태로 복구한다. 시작 악마 획득 `확인` 콜백에도 차단 검사를 추가해 직접 호출 경로까지 방어한다.
- SET12 집중 EditMode 4/4 통과(job `53b570d1fe2f4a4db79abea769488021`). 전체 EditMode는 1332/1352 통과(job `648aa98a373447bdb714e6dae4241e21`)했고 기존과 같은 에셋·기대값 불일치 20건만 남았다.
