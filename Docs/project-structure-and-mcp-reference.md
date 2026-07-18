# 프로젝트 구조 및 Unity MCP 참조 기록

> 프로젝트: DiaBlackJack  
> 확인 책임자: 이천서  
> 버전: v0.1  
> 확인일: 2026-07-19

## 1. 확인 목적

코어 루프 구현 전에 Unity 프로젝트의 어셈블리 경계, 공용 코드, 테스트 위치와 MCP 연결 상태를 확인한다. 이 기록은 단계별 구현 위치를 결정하고 패키지 또는 MCP 연결 문제를 코드 문제와 구분하기 위한 기준이다.

## 2. Unity 프로젝트 정보

Unity MCP의 프로젝트 정보와 로컬 프로젝트 설정이 다음과 같이 일치한다.

| 항목 | 확인 값 |
| --- | --- |
| 프로젝트명 | DiaBlackJack |
| 프로젝트 루트 | `C:/Users/이천서/Documents/GitHub/DiaBlackJack` |
| Unity 버전 | 6000.3.10f1 |
| 대상 플랫폼 | StandaloneWindows64 |
| Assets 경로 | `C:/Users/이천서/Documents/GitHub/DiaBlackJack/Assets` |
| 활성 씬 | `Assets/00. Scenes/CoreLoopTest.unity` |

## 3. 주요 프로젝트 구조

| 경로 | 역할 | 코어 루프 사용 여부 |
| --- | --- | --- |
| `Assets/00. Scenes` | 게임 및 테스트 씬 | 3단계 `CoreLoopTest` 통합 |
| `Assets/01. Scripts/Runtime` | 런타임 코드와 `Border` 어셈블리 | 사용 |
| `Assets/01. Scripts/Runtime/Core` | 로그, 스크린샷, 결정적 난수 공용 코드 | `DeterministicRng` 재사용 |
| `Assets/01. Scripts/Runtime/Input` | Input System 연결 | 1~2단계 제외 |
| `Assets/01. Scripts/Runtime/UI` | 공용 UI와 코어 루프 View | 3단계 `UI/CoreLoop` 사용 |
| `Assets/02. ScriptableObjects` | 설정 및 데이터 에셋 | 코어 루프 제외 |
| `Assets/Tests/EditMode/CoreLoop` | 코어 루프 규칙 테스트 | 새로 구성 |
| `Docs` | 기획, 개발, AI·팀 기여 기록 | 진행 결과 갱신 |
| `Packages` | Unity 및 Git 패키지 참조 | 참조 상태 확인 |

## 4. 어셈블리 구조

### 4.1 기존 어셈블리

| 어셈블리 | 위치 | 용도 |
| --- | --- | --- |
| `Border` | `Assets/01. Scripts/Runtime/Border.asmdef` | 공용 런타임 코드 |
| `Border.Input` | `Assets/01. Scripts/Runtime/Input/Border.Input.asmdef` | Input System 연결 |
| `Border.Editor` | `Assets/Editor/Border.Editor.asmdef` | Editor 전용 코드 |

1~3단계 코어 루프는 새 런타임 어셈블리를 만들지 않고 `Border` 어셈블리 안의 `DiaBlackJack.CoreLoop` 및 `DiaBlackJack.CoreLoop.UI` 네임스페이스에 배치했다. 기존 어셈블리 수를 늘리지 않으면서 규칙·세션·표시 계층의 책임을 분리하기 위한 결정이다.

### 4.2 추가된 테스트 어셈블리

| 어셈블리 | 위치 | 참조 |
| --- | --- | --- |
| `DiaBlackJack.CoreLoop.Tests.EditMode` | `Assets/Tests/EditMode/CoreLoop` | `Border`, Unity Test Assemblies |

Unity가 테스트 어셈블리를 인식하면서 `DiaBlackJack.slnx`에 해당 테스트 프로젝트 참조가 자동 추가되었다.

## 5. Unity MCP 참조

### 5.1 패키지 참조

`Packages/manifest.json`과 `Packages/packages-lock.json`에서 다음 참조를 확인했다.

| 항목 | 값 |
| --- | --- |
| 패키지 ID | `com.coplaydev.unity-mcp` |
| Manifest 참조 | `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main` |
| Lock source | `git` |
| Lock hash | `c14de1e6dc01ab42d2bb358730cff954bce0ce6b` |
| Manifest/Lock 일치 | 일치 |

Manifest가 `main` 브랜치를 참조하므로 패키지를 다시 해석할 때 원격 변경이 들어올 수 있다. 현재 Lock hash가 실제 사용 리비전을 기록하지만, 프로토타입 안정화 전에는 검증된 태그 또는 커밋으로 Manifest도 고정하는 것을 권장한다.

### 5.2 MCP 연결 상태

| 항목 | 확인 값 |
| --- | --- |
| 연결 인스턴스 수 | 1 |
| 인스턴스 | `DiaBlackJack@5635a4cdcfecc8dd` |
| 연결 방식 | HTTP |
| Editor 상태 | Idle, 컴파일·도메인 리로드 없음 |
| 도구 사용 가능 | `ready_for_tools = true` |
| 활성 씬 | `CoreLoopTest` |
| 테스트 도구 그룹 | 현재 세션에서 활성화 |

MCP가 제공한 프로젝트 경로와 실제 작업 경로가 같으므로 다른 Unity 인스턴스에 잘못 연결된 상태가 아니다.

## 6. 1단계 통합 결정

- 기존 `Border.Core.DeterministicRng`를 덱 섞기에 재사용한다.
- 코어 규칙은 Unity 오브젝트와 씬에 의존하지 않는다.
- 런타임 코드는 `Assets/01. Scripts/Runtime/CoreLoop`에 둔다.
- 테스트는 별도 EditMode 테스트 어셈블리에 둔다.
- `CoreLoopTest` 씬, Input, UI와 ScriptableObject는 1단계에서 수정하지 않는다.
- MCP를 통해 스크립트 컴파일 상태, 정적 검증, Console 오류와 EditMode 테스트를 확인한다.

## 7. 검증 결과

| 항목 | 결과 |
| --- | --- |
| MCP 프로젝트 정보 | 로컬 프로젝트와 일치 |
| 스크립트 정적 검증 | 1~3단계 변경 스크립트, 최종 오류 0, 경고 0 |
| Unity 컴파일 | 성공 |
| Unity Console | 게임 컴파일 오류 0건. Test Framework 결과 저장 경로 메시지 1건이 `Exception` 유형으로 분류됨 |
| EditMode 테스트 | 전체 27개 통과, 실패 0 |
| 씬 검증 | `CoreLoopTest` 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Game View | 시작, 행동 후 갱신, 승리·재시작 화면 확인 |

1단계 MCP 전체 EditMode 실행은 한 차례 초기화 제한에 걸렸으나 대상 테스트 재실행은 통과했다. 2단계의 일시적 HTTP 연결 문제도 도메인 리로드 후 복구되었다. 3단계 최종 상태에서는 같은 프로젝트 인스턴스로 EditMode 27개, 씬 검증과 Game View 흐름을 확인했다.

최종 씬 직렬화 정리 후 자산 재읽기 자체는 Editor 로그에서 완료되었지만, 다른 전체 화면 애플리케이션이 전면에 있는 동안 Unity가 백그라운드 MCP ping에 응답하지 않아 준비 대기가 시간 초과되었다. 이는 필수 검증 완료 뒤 발생한 Editor 포커스 문제이며 코드·씬 오류로 분류하지 않는다.

## 8. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 프로젝트·어셈블리 구조, MCP 패키지 참조와 연결 상태 확인 |
| 2026-07-19 | 이천서 | 2단계 순수 상태 흐름 배치와 MCP 재연결·23개 테스트 결과 갱신 |
| 2026-07-19 | 이천서 | 3단계 UI·씬 배치와 27개 테스트·Game View·씬 검증 결과 갱신 |
