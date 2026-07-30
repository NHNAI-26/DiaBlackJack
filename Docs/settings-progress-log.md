# 설정·일시정지 메뉴 진행 기록

> 프로젝트: DiaBlackJack
> 작업 범위: SET-00~SET-05
> 버전: v1.0
> 최종 갱신: 2026-07-30

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

## 2. 검증 결과

| 날짜 | 검증 | 결과 |
| --- | --- | --- |
| 2026-07-30 | 설정 EditMode | 최종 14/14 통과, job `9e9b4d00fa7946ccbc7fb4921fb4d790` |
| 2026-07-30 | 전체 EditMode | 최종 781/781 통과, job `afbf02707a0d4ba7aa713f40087e1a30` |
| 2026-07-30 | 직접 GameScene 1280x720 | 일시정지·설정·종료 취소·볼륨 표시·화살표 방향 확인 |
| 2026-07-30 | 일시정지 시간 | 메뉴에서 0, 복귀 후 1 확인 |
| 2026-07-30 | 설정 저장 | 설정 화면 종료 뒤 PlayerPrefs 버전 키 생성 확인 |
| 2026-07-30 | 씬 서비스 수 | StageTest 1개, 전환 뒤 GameScene 1개 확인 |
| 2026-07-30 | 직접 GameScene Console | 설정 화면 조작 뒤 오류·경고 0 |

## 3. 미완료·후속 확인

- 1080p Game View와 Windows Standalone 실기기 화면 모드 전환은 아직 확인하지 않았다.
- 강제 `SceneManager.LoadScene`으로 실행한 `StageTest → GameScene` 진단에서
  `The referenced script (Unknown) on this Behaviour is missing!` 로그 1건이 발생했다.
  두 씬과 설정 프리팹의 정적 검증은 누락 0건이므로 정상 게임 진행 경로에서 원인을
  별도 확인해야 한다.
- 테스트 러너가 출력하는 Performance Testing 준비·정리 경고는 제품 코드 경고와
  구분한다.
