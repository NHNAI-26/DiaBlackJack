# 설정·일시정지 메뉴 개발 명세

> 프로젝트: DiaBlackJack
> 작업 범위: SET-00~SET-05
> 버전: v1.0
> 최종 갱신: 2026-07-30

## 1. 데이터

```csharp
public enum GameWindowMode
{
    Windowed,
    ExclusiveFullscreen,
    BorderlessFullscreen
}

public readonly struct GameSettingsSnapshot
{
    public int ResolutionWidth { get; }
    public int ResolutionHeight { get; }
    public GameWindowMode WindowMode { get; }
    public float MasterVolume { get; }
    public float BgmVolume { get; }
    public float SfxVolume { get; }
}
```

볼륨은 생성 시 `0~1`로 보정한다. 잘못된 화면 모드는
`BorderlessFullscreen`으로 보정한다.

## 2. 저장소

`PlayerPrefsSettingsRepository`는 버전 1의 프로젝트 전용 키를 사용한다.

```text
DiaBlackJack.Settings.Version
DiaBlackJack.Settings.ResolutionWidth
DiaBlackJack.Settings.ResolutionHeight
DiaBlackJack.Settings.WindowMode
DiaBlackJack.Settings.MasterVolume
DiaBlackJack.Settings.BgmVolume
DiaBlackJack.Settings.SfxVolume
```

버전이 없거나 다르면 로드 실패로 처리하고 기본값을 사용한다. 저장은 여섯 값을
기록한 뒤 `PlayerPrefs.Save()`를 호출한다.

## 3. 런타임 서비스

```csharp
SettingsSystem SettingsSystem.Current
GameSettingsSnapshot SettingsSystem.Snapshot
void PreviewDisplay(int width, int height, GameWindowMode mode)
void PreviewAudio(float master, float bgm, float sfx)
bool Save()
```

- `SettingsSystem`은 `DontDestroyOnLoad` 단일 인스턴스다.
- 시작 시 저장값을 검증하고 화면 설정을 적용한다.
- 씬 로드 뒤 Master/BGM/SFX 이벤트를 다시 발행한다.
- 화면 적용 결과가 요청과 다르면 직전 정상 스냅샷으로 복구한다.

## 4. 화면 모드 매핑

| 게임 값 | Unity 값 |
| --- | --- |
| `Windowed` | `FullScreenMode.Windowed` |
| `ExclusiveFullscreen` | `FullScreenMode.ExclusiveFullScreen` |
| `BorderlessFullscreen` | `FullScreenMode.FullScreenWindow` |

## 5. UI·게임 연결

- `PauseSettingsController`: ESC 모달 상태, `timeScale`, 저장, 종료를 담당한다.
- `UISettingsArrowSelector`: 좌우 버튼, 순환 인덱스, 표시와 잠금을 담당한다.
- `GameManager.SetPauseInputBlocked(bool)`: 전투·월드 입력을 차단한다.
- `GameManager.TryCloseTransientOverlay()`: 덱 미리보기 등 임시 오버레이를 우선 닫는다.
- `PauseSettingsCanvas.prefab`: uGUI/TMP 운영 UI다.
- `SettingsSystem.prefab`: 기본값 에셋과 기존 볼륨 이벤트 채널을 직렬화한다.

## 6. 테스트 경계

- 순수/저장 유틸: EditMode
- 화면 적용, ESC 모달, `timeScale`, 씬 전환: Play Mode 수동 검증
- 화면 크기: 720p와 1080p Game View
- 최종 게이트: 전체 EditMode 통과, 운영 씬·프리팹 누락 참조 0
