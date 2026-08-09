# 설정·일시정지 메뉴 개발 명세

> 프로젝트: DiaBlackJack
> 작업 범위: SET-00~SET-06
> 버전: v1.1
> 최종 갱신: 2026-08-09

## 1. 데이터

```csharp
public enum HoverTooltipSize
{
    Small,
    Normal,
    Large
}

public readonly struct GameSettingsSnapshot
{
    public HoverTooltipSize HoverTooltipSize { get; }
    public float MasterVolume { get; }
    public float BgmVolume { get; }
    public float SfxVolume { get; }
}
```

잘못된 크기는 `Normal`, 볼륨은 `0~1`로 보정한다. 크기 배율은 각각
`(1,1,1)`, `(1.3,1.3,1)`, `(1.5,1.5,1)`이다.

## 2. 저장소

`PlayerPrefsSettingsRepository`는 버전 2 키를 사용한다.

```text
DiaBlackJack.Settings.Version
DiaBlackJack.Settings.HoverTooltipSize
DiaBlackJack.Settings.MasterVolume
DiaBlackJack.Settings.BgmVolume
DiaBlackJack.Settings.SfxVolume
```

버전 1 저장은 오디오를 유지하고 툴팁 크기를 `Normal`로 이관한다. 다음 저장에서
버전 2를 기록하고 과거 `ResolutionWidth`, `ResolutionHeight`, `WindowMode` 키를
삭제한다. 알 수 없는 버전은 로드 실패로 처리한다.

## 3. 런타임 서비스

```csharp
SettingsSystem SettingsSystem.Current
GameSettingsSnapshot SettingsSystem.Snapshot
void PreviewHoverTooltipSize(HoverTooltipSize size)
void PreviewAudio(float master, float bgm, float sfx)
bool Save()
```

- `SettingsSystem`은 `DontDestroyOnLoad` 단일 인스턴스다.
- 크기 변경은 스냅샷을 갱신하고 `Changed`를 즉시 발행한다.
- 씬 로드 뒤 Master/BGM/SFX 이벤트를 다시 발행한다.
- 해상도 조회, `Screen.SetResolution`, 화면 모드 복구는 수행하지 않는다.

## 4. UI·게임 연결

- `PauseSettingsController`는 `작게 / 보통 / 크게` 선택을 서비스에 전달한다.
- `UISettingsArrowSelector`는 좌우 버튼과 순환 인덱스를 담당한다.
- `GameHudView`는 초기 스냅샷과 `Changed` 이벤트로 `CardHoverTooltipRoot` 배율을 갱신한다.
- `SettingsSystem`이 없으면 `Normal` 배율을 사용한다.
- `PauseSettingsCanvas.prefab`은 `GameScene`과 `MainMenuScene`이 공유한다.
- 공유 설정 Canvas와 `MainMenuCanvas`는 현재 씬의 `UIOverlayCamera`를 이름과 씬 소속으로 찾아 `Screen Space Camera`에 연결한다. 연결 시 전체 자식 계층을 `UI` 레이어로 맞추며 기존 정렬 순서 200/100, Canvas Scaler와 입력 배선은 유지한다.
- 설정 패널의 `UI_Brush_Grey_Deck` 머터리얼은 교체하지 않고 UI 오버레이 카메라의 후처리 경로에서 렌더링한다.
- 레거시 데모 설정 UI에는 그래픽 설정 컴포넌트를 남기지 않는다.

## 5. 테스트 경계

- 크기 보정·배율 매핑·PlayerPrefs 이관과 왕복: Settings EditMode
- 설정 프리팹 구조·HUD 이벤트 반영: Settings EditMode
- 관련 HUD 표시 회귀: CoreLoop EditMode
- 최종 게이트: 전체 EditMode, 두 운영 씬 validation, Missing Script와 Console Error 0
- 수동 화면 확인: 1280×720과 1920×1080
