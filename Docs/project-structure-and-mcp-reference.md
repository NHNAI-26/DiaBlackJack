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
| 활성 씬 | `Assets/00. Scenes/StageTest.unity` |

## 3. 주요 프로젝트 구조

| 경로 | 역할 | 코어 루프 사용 여부 |
| --- | --- | --- |
| `Assets/00. Scenes` | 게임 및 테스트 씬 | 3단계 `CoreLoopTest` 통합 |
| `Assets/01. Scripts/Runtime` | 런타임 코드와 `Border` 어셈블리 | 사용 |
| `Assets/01. Scripts/Runtime/Core` | 로그, 스크린샷, 결정적 난수 공용 코드 | `DeterministicRng` 재사용 |
| `Assets/01. Scripts/Runtime/CoreLoop` | 전투 규칙·상태·세션, 전투 행동과 카드 정의·사용 상태 기반 | CU-01 카드 카탈로그·인스턴스 상태까지 제공 |
| `Assets/01. Scripts/Runtime/Input` | Input System 연결 | 1~2단계 제외 |
| `Assets/01. Scripts/Runtime/UI` | 공용 UI와 코어 루프 View | BA-04 폴드·체인지 표시와 입력 사용 |
| `Assets/01. Scripts/Runtime/StageProgression` | 런·스테이지 순수 상태와 전투 연결 | CU-01에서 런 카드 정의 키 보존·해석 확장 |
| `Assets/01. Scripts/Runtime/UI/StageProgression` | 진행 표시·입력과 씬 간 Runtime | 별도 진행 작업 SP-03 사용 |
| `Assets/02. ScriptableObjects` | 설정 및 데이터 에셋 | 코어 루프 제외 |
| `Assets/Tests/EditMode/CoreLoop` | 코어 루프·전투 행동·카드 정의 규칙·표시·Controller 테스트 | CU-01 기준 71개 |
| `Assets/Tests/EditMode/StageProgression` | 진행 상태·전투·표시·전투 행동·런 카드 정의 통합 테스트 | CU-01 기준 30개 |
| `Docs` | 기획, 개발, AI·팀 기여 기록 | 진행 결과 갱신 |
| `Packages` | Unity 및 Git 패키지 참조 | 참조 상태 확인 |

## 4. 어셈블리 구조

### 4.1 기존 어셈블리

| 어셈블리 | 위치 | 용도 |
| --- | --- | --- |
| `Border` | `Assets/01. Scripts/Runtime/Border.asmdef` | 공용 런타임 코드 |
| `Border.Input` | `Assets/01. Scripts/Runtime/Input/Border.Input.asmdef` | Input System 연결 |
| `Border.Editor` | `Assets/Editor/Border.Editor.asmdef` | Editor 전용 코드 |

1~4단계 코어 루프는 새 런타임 어셈블리를 만들지 않고 `Border` 어셈블리 안의 `DiaBlackJack.CoreLoop` 및 `DiaBlackJack.CoreLoop.UI` 네임스페이스에 배치했다. 기존 어셈블리 수를 늘리지 않으면서 규칙·세션·표시 계층의 책임을 분리하기 위한 결정이다.

CU-01도 새 런타임·테스트 어셈블리를 만들지 않았다. 카드 사용 상태 전이 메서드는 공개 카드 사용 API로 노출하지 않고 `AssemblyInfo.cs`의 `InternalsVisibleTo`를 통해 CoreLoop EditMode 테스트에서만 직접 검증한다.

### 4.2 추가된 테스트 어셈블리

| 어셈블리 | 위치 | 참조 |
| --- | --- | --- |
| `DiaBlackJack.CoreLoop.Tests.EditMode` | `Assets/Tests/EditMode/CoreLoop` | `Border`, Unity Test Assemblies |
| `DiaBlackJack.StageProgression.Tests.EditMode` | `Assets/Tests/EditMode/StageProgression` | `Border`, Unity Test Assemblies |

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
| 활성 씬 | `StageTest` |
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
| 스크립트 정적 검증 | 1~3단계 구현 스크립트, 최종 오류 0, 경고 0 |
| Unity 컴파일 | 성공 |
| Unity Console | 테스트 후 Console을 초기화하고 승리·패배·재시작을 실행한 결과 게임 관련 Error·Warning 0건 |
| EditMode 테스트 | 전체 27개 통과, 실패 0, 건너뜀 0(최종 job `1bc6c0a7d5804be89004fe2e071a32f3`) |
| 씬 검증 | `CoreLoopTest` 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Game View | 결정적 카드 순서로 승리·패배 화면과 양쪽 재시작 후 초기 상태 확인 |

### 7.1 런·스테이지 SP-03 추가 검증

| 항목 | 결과 |
| --- | --- |
| 빌드 씬 | `StageTest` 0번, `CoreLoopTest` 1번, `SampleScene` 2번으로 등록 |
| 전체 EditMode | 50/50 통과, 실패·건너뜀 0(job `6820dd679cc5432fb4c509ff99a0d195`) |
| 씬 검증 | `StageTest`, `CoreLoopTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0 |
| 실제 흐름 | 진행 화면 → 첫 전투 → 결과 화면 → 다음 전투, 스테이지 0→1과 영혼 12 유지 확인 |
| Console | Test Framework 기반 시설 메시지를 구분한 뒤 초기화, 게임 관련 Error·Warning 0 |

### 7.2 런·스테이지 SP-04 최종 검증

| 항목 | 결과 |
| --- | --- |
| MCP 재연결 | `StageTest` 활성 씬, 빌드 인덱스 0, 도구 응답 정상 확인 |
| 전체 EditMode | 50/50 통과, 실패·건너뜀 0(job `e75bce0e07a44affa208681d2dfa57cb`) |
| 전체 흐름 | 일반전 2개와 최종 보스 승리, 두 번째 일반전 패배, 승리·패배 재시작 통과 |
| 반복 안정성 | 런 재시작 10회 모두 스테이지 0·영혼 12·`CoreLoopTest`로 초기화 |
| 씬·빌드 | `StageTest`, `CoreLoopTest` 문제 0. 빌드 인덱스 0·1과 `SampleScene` 2 유지 |
| Game View | `RUN VICTORY`, `RUN DEFEAT`, `RESTART RUN` 표시 확인 |
| Console | Test Framework 기반 시설 메시지를 구분한 뒤 초기화, 게임 관련 Error·Warning 0 |

### 7.3 전투 행동 BA-01 추가 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| 구현 전 기준선 | 전체 EditMode 50/50 통과(job `4ad5b78d5861437b8d22f89a3a34957e`) |
| Unity 컴파일 | 선택 상태·손패·덱·후보 선택 기반 반영, 컴파일 오류 0 |
| CoreLoop EditMode | 신규 기반 8개 포함 35/35 통과(job `05b3d109af5e46d58c9922ffc7c08691`) |
| 전체 EditMode | 58/58 통과, 실패·건너뜀 0(job `27be56769b124d0b84a9c4016547cbea`) |
| Console | Test Framework 사전·사후 처리와 결과 저장 메시지만 존재, 게임·컴파일 오류 0 |
| 씬 변경 | 없음 |

### 7.4 전투 행동 BA-02 추가 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| 구현 전 기준선 | 전체 EditMode 58/58 통과(job `9679bfd2bff04679b4d0f1c7df24115f`) |
| Unity 컴파일 | 폴드 판정·전투·세션·최근 결과 호환 반영, 컴파일 오류 0 |
| CoreLoop EditMode | 신규 폴드 6개 포함 41/41 통과(job `8e402ce78f5940cab0e2864adc3a7600`) |
| 전체 EditMode | 64/64 통과, 실패·건너뜀 0(job `6000c101d2fd41d2918d238d1c939bba`) |
| Console | MCP WebSocket 초기화 경고 1개와 Test Framework 기반 시설 메시지 6개를 분리 확인; HTTP 도구·테스트 정상, 초기화 후 게임·컴파일 Error·Warning 0 |
| 씬·패키지 변경 | 없음 |

### 7.5 전투 행동 BA-03 추가 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| 구현 전 기준선 | 전체 EditMode 64/64 통과(job `ebe3c93a2e8e48448b50dc09f07de4a3`) |
| Unity 컴파일 | 체인지 보류·후보 선택·라운드 제한·세션 전달 반영, 최종 컴파일 오류 0 |
| CoreLoop EditMode | 신규 체인지 8개 포함 49/49 통과(job `7ed1855082ee4e898787e3dbabd65f63`) |
| 전체 EditMode | 72/72 통과, 실패·건너뜀 0(job `ed5f5b4c18844c348f74b139592ecb01`) |
| Console | MCP WebSocket 초기화 경고 1개와 Test Framework 기반 시설 메시지 6개를 분리 확인; HTTP 도구·테스트 정상, 초기화 후 게임·컴파일 Error·Warning 0 |
| 씬·패키지 변경 | 없음 |

### 7.6 전투 행동 BA-04 추가 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| 구현 전 기준선 | 전체 EditMode 72/72 통과(job `af2d729eb16748ab836c22efe8fc0f9b`) |
| Unity 컴파일 | 폴드·체인지 표시 모델, View 입력과 Controller 전달 반영, 컴파일 오류 0 |
| CoreLoop EditMode | 신규 표시·Controller 6개 포함 55/55 통과(job `56da2fbdd5cd42f48e820495ed66d265`) |
| 전체 EditMode | 78/78 통과, 실패·건너뜀 0(job `4b6d6aeafa2d4f8daf682613960bf857`) |
| Game View | 네 행동, 후보 전용 화면, 체인지 사용 완료, 폴드 결과, 영혼 1 경고·패배를 실제 Controller 흐름으로 확인 |
| 씬 검증 | `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Console | 최종 초기화 후 게임·컴파일 Error·Warning 0 |
| 씬·패키지 변경 | 없음 |

BA-04 시점의 Controller는 런 전투 인스턴스에 직접 입력을 전달해 `StageProgressionSession` 종료·영혼 동기화를 수행하지 않았다. 이 책임은 BA-05에서 진행 세션의 명시적 전달 메서드로 옮기고 전체 흐름으로 검증했다.

### 7.7 전투 행동 BA-05 최종 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| 구현 전 기준선 | 전체 EditMode 78/78 통과(job `f6cee3da9b6d4890a9e5a87a82796c80`) |
| Unity 컴파일 | 진행 세션 전달과 Controller 경로 통합 반영, 컴파일·스크립트 오류 0 |
| StageProgression EditMode | 신규 4개 포함 27/27 통과. TestResults XML 실패 0(job `c18c0d5b62054e6a9ae4460ad0264835`) |
| 전체 EditMode | 82/82 통과, 실패·건너뜀 0(job `f22e7067ef064a948cb396c0783d477d`) |
| 실제 런 체인지 | `StageTest`→`CoreLoopTest`, 후보 2개·선택 완료·`InBattle`·`CHANGE (USED)` 확인 |
| 실제 런 폴드 | 영혼 1 경고 뒤 Controller 최종 폴드, `RunDefeat`·지속 영혼 0·`StageTest` 복귀 확인 |
| 실제 런 재시작 | 스테이지 0·지속/전투 영혼 12·새 전투 `PlayerTurn` 확인 |
| 씬 검증 | `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Console | 최종 초기화 후 게임·컴파일 Error·Warning 0 |
| 씬·패키지 변경 | 없음 |

대상 테스트는 Unity 결과 XML에서 27/27로 끝났지만 도메인 리로드 뒤 MCP 작업 상태만 `running`으로 남았다. 패키지의 `TestJobManager.ClearStuckJob` 복구 경로로 고아 상태를 정리한 뒤 새 작업에서 전체 82/82를 통과시켰다. 이는 게임 코드 실패가 아니라 MCP 작업 추적 복구로 분리해 기록한다.

### 7.8 카드 사용 CU-01 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| Unity 컴파일 | 카드 정의·상태·런 키 보존 반영, 컴파일 오류 0 |
| 신규 테스트 | CU-01 경계 19개 통과 |
| 관련 어셈블리 | CoreLoop 71개와 StageProgression 30개, 합계 101/101 통과(job `8af1abb4ef944a1a95ede1f9371d1544`) |
| 전체 EditMode | 101/101 통과, 실패·건너뜀 0(job `20dc73cd210d47e9a35346f86c5ec9c3`) |
| Console | Error/Warning 0 |
| 씬·패키지·외부 에셋 변경 | 없음 |

1단계 MCP 전체 EditMode 실행은 한 차례 초기화 제한에 걸렸으나 대상 테스트 재실행은 통과했다. 2단계의 일시적 HTTP 연결 문제도 도메인 리로드 후 복구되었다. 3단계 최종 상태에서는 같은 프로젝트 인스턴스로 EditMode 27개, 씬 검증과 Game View 흐름을 확인했다.

3단계 최종 씬 직렬화 정리 뒤 Unity가 백그라운드 MCP ping에 일시적으로 응답하지 않았으나, 4단계 착수 시 동일 인스턴스 `DiaBlackJack@5635a4cdcfecc8dd`에 재연결되어 `ready_for_tools = true`를 확인했다. 이후 전체 테스트, 양쪽 종료 흐름, 씬과 Console 검증을 완료했으므로 해당 연결 문제는 해결로 기록한다. Test Framework의 결과 저장 경로 메시지는 테스트 실행 기반 시설 출력이며 게임 오류와 분리했다.

## 8. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-19 | 이천서 | 프로젝트·어셈블리 구조, MCP 패키지 참조와 연결 상태 확인 |
| 2026-07-19 | 이천서 | 2단계 순수 상태 흐름 배치와 MCP 재연결·23개 테스트 결과 갱신 |
| 2026-07-19 | 이천서 | 3단계 UI·씬 배치와 27개 테스트·Game View·씬 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 4단계 MCP 재연결, 전체 27개 테스트와 승리·패배·재시작·씬·Console 최종 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-03 전용 진행 씬·공용 전투 씬 구조와 전체 EditMode 50/50·씬·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-04 전체 흐름·10회 재시작과 MCP·빌드·Game View·Console 최종 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 전투 행동 BA-01 CoreLoop 기반 파일·테스트 구조와 CoreLoop 35/35·전체 58/58 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 전투 행동 BA-02 폴드 판정·세션·표시 호환 구조와 CoreLoop 41/41·전체 64/64 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 전투 행동 BA-03 체인지 보류·선택·라운드 제한·세션 구조와 CoreLoop 49/49·전체 72/72 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 전투 행동 BA-04 표시·View·Controller 구조와 CoreLoop 55/55·전체 78/78·Game View·씬·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 전투 행동 BA-05 진행 세션 전달·동기화 구조와 진행 27/27·전체 82/82·실제 런 패배·재시작·씬·Console 최종 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 카드 사용 CU-01 카드 정의·사용 상태·런 키 보존 구조와 신규 19개·전체 EditMode 101/101·Console 검증 결과 갱신 |
