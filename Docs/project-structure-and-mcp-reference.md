# 프로젝트 구조 및 Unity MCP 참조 기록

> 프로젝트: DiaBlackJack  
> 확인 책임자: 이천서  
> 버전: v0.3
> 확인일: 2026-07-25

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
| 활성 씬 | `Assets/00. Scenes/GameScene.unity` |

## 3. 주요 프로젝트 구조

| 경로 | 역할 | 코어 루프 사용 여부 |
| --- | --- | --- |
| `Assets/00. Scenes` | 게임 및 테스트 씬 | 3단계 `CoreLoopTest` 통합 |
| `Assets/01. Scripts/Runtime` | 런타임 코드와 `Border` 어셈블리 | 사용 |
| `Assets/01. Scripts/Runtime/Core` | 로그, 스크린샷, 결정적 난수 공용 코드 | `DeterministicRng` 재사용 |
| `Assets/01. Scripts/CoreLoop` | 전투 규칙·상태·세션, 카드 효과와 적 프로필·공개 관측·정책·안전 표시 스냅샷 | AC-03 탐지기 선언·공개/전용 결과·비교 지식과 교체 무효화 추가 |
| `Assets/01. Scripts/Runtime/Input` | Input System 연결 | 1~2단계 제외 |
| `Assets/01. Scripts/Runtime/UI` | 공용 UI와 코어 루프 View | EUI-04 적 이름·등급·성향·추론·보스 예고 패널과 720p 반응형 배치 |
| `Assets/01. Scripts/Runtime/StageProgression` | 런·스테이지 순수 상태, 전투·보상·상대 후보와 선택 프로필 키 변환 | EUI-03 OfferId+ProfileKey 원자적 확정과 실제 프로필 전투·보상 연결 |
| `Assets/01. Scripts/Runtime/UI/StageProgression` | 진행 표시·입력과 씬 간 Runtime | EUI-03 확정 입력 전달·성공한 전투만 씬 전환, EUI-02 후보 비교·로컬 집중 재사용 |
| `Assets/02. ScriptableObjects` | 설정 및 데이터 에셋 | 코어 루프 제외 |
| `Assets/06.Packages/Tests/EditMode/CoreLoop` | 코어 루프·카드 효과·자동 카드 기반·적 프로필·정책·표시 테스트 | AC-03 기준 305개 |
| `Assets/06.Packages/Tests/EditMode/StageProgression` | 진행·보상·상대 후보·선택 상태·표시·프로필 전투 변환·반복 호환 테스트 | AC-03 회귀 기준 129개 |
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

CU-02 역시 기존 `Border`와 `DiaBlackJack.CoreLoop.Tests.EditMode` 어셈블리를 사용한다. 실제 카드 처리기는 내부 경계로 두고, 테스트 어셈블리에서만 처리기를 주입해 출시 카탈로그에 가짜 효과 정의를 남기지 않았다.

CU-03은 같은 어셈블리에 실제 `AutoPistolEffectHandler`를 추가하고 기본 처리기 목록에 등록했다. 비공개 숫자 비교와 결과 정보 은닉은 CoreLoop EditMode 테스트에서 검증했으며 새 런타임·테스트 어셈블리를 만들지 않았다.

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

### 7.9 카드 사용 CU-02 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트·활성 `StageTest` 씬 일치 |
| Unity 컴파일 | 사용 검증·선택 대기·효과 처리·종료 원인 기반 반영, 컴파일 오류 0 |
| 신규 테스트 | CU-02 경계 16개 통과 |
| CoreLoop 대상 | 87/87 통과, 실패·건너뜀 0(job `ee94e86a485b4a7c8a291cd3edfb64db`) |
| 전체 EditMode | 117/117 통과, 실패·건너뜀 0(job `28d0d40c89da4842a50663c3817e3281`) |
| Console | 테스트 기반 시설 메시지를 정리한 뒤 Error/Warning 0 |
| 씬·어셈블리·패키지·외부 에셋 변경 | 없음 |

이번 단계는 순수 규칙 기반이므로 Game View와 씬 직렬화 검증 대상이 아니다. `DiaBlackJack.slnx`의 프로젝트 행 순서 변경은 Unity 자동 생성 결과이며 의미 있는 프로젝트 변경으로 포함하지 않는다.

### 7.10 카드 사용 CU-03 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트·활성 `StageTest` 씬 일치 |
| Unity 컴파일 | 리볼버 처리기·기본 등록·전용 테스트 반영, 컴파일 오류 0 |
| 신규 테스트 | 리볼버 경계 8/8 통과(job `5313dd71a905480d903e3bffe5d9c6a3`) |
| CoreLoop 대상 | 95/95 통과, 실패·건너뜀 0(job `6cd877a6354440c5b9a0d96e3949c459`) |
| 전체 EditMode | 125/125 통과, 실패·건너뜀 0(job `6a77fc8ed74f48659b541b7797d16774`) |
| Console | 테스트 기반 시설 메시지를 정리한 뒤 Error/Warning 0 |
| 씬·어셈블리·패키지·외부 에셋 변경 | 없음 |

정상 라운드에는 상대 비공개 카드가 한 장뿐이라는 규칙에 맞춰 다중 비공개 카드 기능은 만들지 않았다. 이번 단계는 모델·CoreLoop 세션 검증 범위이므로 Game View와 씬 직렬화 검증 대상이 아니다.

### 7.11 카드 사용 CU-04 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트·활성 `StageTest` 씬 일치 |
| Unity 컴파일 | 수정 구슬·위협용 해머·보위 나이프 처리기와 카드 이동 경계 반영, 컴파일 오류 0 |
| 신규 테스트 | CU-04 경계 18/18 통과(job `77ea7ab525814da4bd75fd89e163015e`) |
| CoreLoop 대상 | 113/113 통과, 실패·건너뜀 0(job `cf493f9f57fa460189ba6dd9dd5fa1e0`) |
| 전체 EditMode | 143/143 통과, 실패·건너뜀 0(job `75dff0608451447492d29660b5469c93`) |
| Console | 테스트 기반 시설 메시지를 정리한 뒤 Error/Warning 0 |
| 씬·어셈블리·패키지·외부 에셋 변경 | 없음 |

수정 구슬 후보는 보류 효과가 임시 소유해 덱·손과 중복되지 않으며 선택하지 않은 카드는 기존 다음 드로우 순서대로 복구된다. 해머는 정상 라운드의 단일 비공개 카드만 공개 없이 교체하고, 나이프의 비버스트 선택은 이후 적 AI로 바꿀 수 있는 정책 경계에 격리했다. 이번 단계는 순수 규칙·CoreLoop 세션 범위이므로 Game View와 씬 직렬화 검증 대상이 아니다.

### 7.12 카드 사용 CU-05 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트와 활성 `StageTest` 씬 일치 |
| Unity 컴파일 | 카드 표시·View·Controller·진행 세션 전달 반영, 컴파일 오류 0 |
| 관련 테스트 | Presenter·진행 통합 28/28 통과(job `7c3e07ddbc2e4b6aa11636abaa0dfd82`) |
| CoreLoop 대상 | 전체 실행에 포함된 117/117 통과 |
| StageProgression 대상 | 전체 실행에 포함된 34/34 통과 |
| 전체 EditMode | 151/151 통과, 실패·건너뜀 0(job `14c6473e2cb844419afd298523f7ba0e`) |
| 실제 런·Game View | `StageTest`→`CoreLoopTest` 동일 전투 연결, 수정 구슬·해머·리볼버·보위 나이프 사용·선택·최근 결과·입력 해제 확인 |
| 씬 검증 | `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Console | 최종 Error/Warning 0 |
| 씬 직렬화·어셈블리·패키지·외부 에셋 변경 | 없음 |

Presenter는 `CardUseAvailability`를 읽기 전용 UI 모델로 변환하고 상대 비공개 숫자를 노출하지 않는다. Controller는 독립·런 세션을 구분해 ID만 전달하며, 진행 세션은 기존 종료 동기화를 재사용한다. MCP 검증 중 Unity 창이 포커스를 잃으면 게임 프레임이 진행되지 않는 환경 특성을 확인해 검증 세션에서만 백그라운드 실행을 켰고, 실제 프레임에서는 다음 프레임 입력 잠금 해제가 정상 동작했다. GUI 이벤트 안에서 모델이 교체되는 경우는 선택 목록 스냅샷으로 이전 인덱스 접근을 차단했다. 카드 4종은 같은 공통 View·Controller 경로로 검증했으며 카드별 전용 UI 분기는 추가하지 않았다.

### 7.13 카드 사용 CU-06 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트 일치 |
| Unity 컴파일 | 검증 테스트 반영, 컴파일 오류 0 |
| 신규 반복 회귀 | 5/5 통과(job `3c6b86f1590e411db8a7e8cbe66cd86a`) |
| CoreLoop 대상 | 122/122 통과(job `d4727c3cc840426593170151fd879a3f`) |
| StageProgression 대상 | 34/34 통과(job `029e2eb3742345fe804bb0fdf39fb4fb`) |
| 전체 EditMode | 156/156 통과, 실패·건너뜀 0(job `3cffa1feb5e94b1ea6781a644700bc76`) |
| 반복 불변 조건 | 독립 재시작·수정 구슬 소유권·런 승리·패배 재시작 각 10회, 카드 중복·유실·상태 고착 0 |
| 정보 은닉 | 상대 비공개 숫자 1~10 모두 Presenter `?` 유지, 효과 결과에 실제 값 미포함 |
| 실제 런 흐름 | Controller·Runtime 경로로 보위 나이프 최종 승리와 수정 구슬 버스트 패배, 양쪽 결과 화면과 재시작 뒤 새 전투·영혼·카드 초기화 확인 |
| 씬 검증 | `CoreLoopTest`, `StageTest` 각각 누락 스크립트·깨진 프리팹·기타 문제 0 |
| Console | 최종 Error/Warning 0 |
| 런타임·씬·어셈블리·패키지·외부 에셋 변경 | 없음 |

CU-06은 새 런타임 계층을 추가하지 않고 기존 공개 세션·Controller 경로를 반복 검증했다. `CardUseSystemValidationTests`는 고정 드로우 순서를 사용해 덱의 가용 카드 수와 사용 중 카드 수 합계, 손패 ID 고유성, 새 전투 인스턴스와 사용 상태 초기화를 검사한다. 실제 흐름 검증을 위해 플레이 모드에서만 백그라운드 실행을 켰으며 프로젝트 설정과 씬에는 저장하지 않았다.

### 7.14 전투 보상 RW-01 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 패키지는 프로젝트에 유지되지만 현재 Codex 도구 표면 미노출, Unity 6000.3.10f1 배치 실행으로 대체 |
| Unity 컴파일 | 신규 런타임·테스트와 내부 테스트 접근 반영, 컴파일 오류 0 |
| 신규 RW-01 | 8/8 통과 |
| StageProgression 대상 | 42/42 통과 |
| 전체 EditMode | 164/164 통과, 실패·건너뜀 0 |
| 보상 생성 | 일반 10개·높은 등급 6개 유효성, 후보 3개 고유성, 같은 시드·순서 결정성 확인 |
| 런 덱 | 최대 ID 다음 값 발급, 같은 정의의 서로 다른 물리 ID, 영혼·최초 덱·다음 ID 재시작 복구 확인 |
| 상태·세션·UI | 변경 없음, RW-02 이후 범위 |
| 씬·프리팹·패키지·외부 에셋 | 변경 없음 |
| 배치 환경 메시지 | 라이선스 토큰 갱신 메시지가 있었으나 테스트 종료 코드 0과 결과 XML에 영향 없음 |

RW-01은 `Border` 런타임 어셈블리 안에 다섯 보상 타입을 추가했고 새 어셈블리는 만들지 않았다. `PlayerRunState`의 덱 변경 경계는 `internal`로 유지하고 `AssemblyInfo.cs`를 통해 StageProgression EditMode 테스트에만 직접 공개했다. Unity가 생성한 `.meta`는 신규 C# 파일과 함께 관리하며, 자동으로 확장되는 솔루션 목록과 테스트 결과 XML은 RW-01 커밋 범위에서 분리한다.

### 7.15 전투 보상 RW-02 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 기존 MCP for Unity 로컬 HTTP 서버 `127.0.0.1:8080/mcp`, 인스턴스 `DiaBlackJack@5635a4cdcfecc8dd` 사용 |
| Unity 컴파일 | 스크립트 갱신·컴파일 뒤 C# 컴파일 오류 0 |
| 신규 RW-02 | 7/7 통과 |
| StageProgression 대상 | 49/49 통과 |
| 전체 EditMode | 171/171 통과, 실패·건너뜀 0 |
| 상태 처리 | 일반·보스 보상 대기, 선택·건너뛰기, 실패 원자성·재시작 초기화 확인 |
| 세션·UI | 세션은 RW-03 전까지 내부 무보상 호환 경로, UI는 상태 문구만 호환하고 후보 화면은 RW-04 |
| 씬·프리팹·패키지·외부 에셋 | 변경 없음 |
| Console 기반 시설 메시지 | Test Framework의 결과 저장 경로 `Exception` 2건, 테스트 성공과 게임 코드에는 영향 없음 |

RW-02는 `Border` 어셈블리 안에 완료 목적지·보류 제안·해결 결과 타입을 추가하고 `RunProgress`의 공개 진행 경계를 보상 처리 API로 바꿨다. Codex의 직접 도구 표면에는 Unity MCP 함수가 없었지만 프로젝트에 이미 구성된 로컬 MCP 서버를 표준 JSON-RPC 세션으로 사용했으며, 새 패키지나 별도 도구를 설치하지 않았다. Unity가 생성한 신규 `.meta`만 대응 C# 파일과 함께 관리하고 자동 정렬된 솔루션 파일 변경은 제외했다.

### 7.16 전투 보상 RW-03 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 기존 MCP for Unity 로컬 HTTP 서버와 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 사용 |
| Unity 컴파일 | 스크립트 갱신·도메인 리로드 뒤 C# 컴파일 오류 0 |
| 신규 RW-03 | 6/6 통과 |
| StageProgression 대상 | 55/55 통과(job `8e6b12e7778d4aa188cca37f9ecbac6c`) |
| 전체 EditMode | 177/177 통과, 실패·건너뜀 0(job `43ccbfa9f8594d28a3d20d74b1a56d29`) |
| 세션 통합 | 승리 영혼 동기화, 일반·주입형 엘리트·보스 등급, 제안 1회 생성과 완료 목적지 확인 |
| 다음 전투·재시작 | 선택 카드만 다음 새 덱에 전달, 건너뛰기 덱 유지, 새 런 최초 덱·보상 상태 복구 확인 |
| 진행 화면·씬 | 보상 선택 UI는 RW-04 범위이며 씬·프리팹·패키지·외부 에셋 변경 없음 |
| Console | MCP WebSocket 미초기화 경고 1건과 Test Framework 사전·사후 처리 4건·결과 저장 `Exception` 2건만 존재; HTTP MCP·테스트 정상, 게임·컴파일 오류 0 |

신규 테스트 클래스 필터를 처음 실행했을 때 Unity 테스트 트리 갱신 시점 때문에 0개가 선택되었지만, 이후 StageProgression 어셈블리 전체 55개와 전체 EditMode 177개에서 신규 6개를 포함해 모두 통과했다. 세션은 `PlayerRunState`와 기존 전투 생성 경계를 재사용하고, 없는 엘리트 스테이지 종류나 별도 트랜잭션 계층을 추가하지 않고 보상 등급 선택 함수를 주입했다. 직접 도구 표면에 Unity MCP 함수가 없는 환경에서도 기존 로컬 MCP 표준 JSON-RPC 연결을 재사용했으며 새 도구나 패키지는 설치하지 않았다.

### 7.17 전투 보상 RW-04 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 기존 MCP for Unity 로컬 HTTP 서버와 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 사용 |
| Unity 컴파일 | 첫 갱신의 테스트 `Object` 이름 충돌 2건을 `UnityEngine.Object` 명시로 해결, 최종 C# 컴파일 오류 0 |
| 신규 RW-04 | 5/5 통과(job `04af4f3f4282422cb1a2543191614328`) |
| StageProgression 대상 | 60/60 통과(job `ec02529c0b4c4575b786443aa37a3e02`) |
| 전체 EditMode | 182/182 통과, 실패·건너뜀 0(job `b24b378821d14ba3aabfc3da8b0094f3`) |
| 씬 검증 | `StageTest`, `CoreLoopTest` 문제 0; 씬 파일 변경 없음 |
| Game View | 일반 선택·건너뛰기·다음 스테이지, 보스 높은 등급 보상·건너뛰기 뒤 `RunVictory` 확인 |
| 시각 검토 | 기존 StageTest 표현 언어와 기능 요구 기준 97/100, 통과; 외부 기준 이미지는 없어 픽셀 비교 제외 |
| 범위 | 진행 UI 런타임 3개와 표시 테스트만 변경; 프리팹·패키지·외부 에셋 추가 없음 |
| Console | MCP WebSocket과 Test Framework의 실행·결과 저장 메시지를 기반 시설 출력으로 분류하고 정리한 뒤 최종 Error/Warning 0 확인 |

RW-04는 `StageProgressionPresentation`, `StageProgressionView`, `StageProgressionController`의 기존 책임을 확장했으며 새 UI 프레임워크나 씬을 도입하지 않았다. Presenter만 카드 정의 키를 카탈로그로 해석하고 View는 렌더링·이벤트, Controller는 세션 API 전달만 담당한다. 일반 보상 해결 후에는 같은 `StageTest`에서 결과와 `NEXT STAGE`를 확인하고, 보스 해결 후에는 `RUN VICTORY`와 재시작을 표시한다. 후보 모델에는 상대 손패나 비공개 카드 정보가 없다.

1단계 MCP 전체 EditMode 실행은 한 차례 초기화 제한에 걸렸으나 대상 테스트 재실행은 통과했다. 2단계의 일시적 HTTP 연결 문제도 도메인 리로드 후 복구되었다. 3단계 최종 상태에서는 같은 프로젝트 인스턴스로 EditMode 27개, 씬 검증과 Game View 흐름을 확인했다.

3단계 최종 씬 직렬화 정리 뒤 Unity가 백그라운드 MCP ping에 일시적으로 응답하지 않았으나, 4단계 착수 시 동일 인스턴스 `DiaBlackJack@5635a4cdcfecc8dd`에 재연결되어 `ready_for_tools = true`를 확인했다. 이후 전체 테스트, 양쪽 종료 흐름, 씬과 Console 검증을 완료했으므로 해당 연결 문제는 해결로 기록한다. Test Framework의 결과 저장 경로 메시지는 테스트 실행 기반 시설 출력이며 게임 오류와 분리했다.

### 7.18 전투 보상 RW-05 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 기존 MCP for Unity 로컬 HTTP 서버와 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 사용 |
| Unity 상태 | Unity 6000.3.10f1, `StageTest` 활성, Play Mode 정지, 컴파일·에셋 갱신·테스트 작업 없음, `ready_for_tools = true` |
| 신규 RW-05 | 5/5 통과(job `510b0c9e4be045fda21770621254c995`), 다섯 흐름 각각 10회 |
| CoreLoop 대상 | 122/122 통과(job `4a81d3e33b724d70b402e1171ed45ea3`) |
| StageProgression 대상 | 65/65 통과(job `79bb38e05ca14656aedd514826e34fbd`) |
| 카드 사용 검증 | 5/5 통과(job `c82645aa55d04aafaea2b7e631c5cd07`) |
| 전체 EditMode | 187/187 통과, 실패·건너뜀 0(job `d4fb4369c64149928211bdb8ed72d50`) |
| 반복 불변 조건 | 옵션·정의·제안·런 카드 ID 고유성, 선택 카드 다음 덱 포함, 건너뛰기 덱 유지, 패배 무보상과 새 런 초기화 확인 |
| 실제 일반 흐름 | 선택 시 덱 4→5와 다음 전투 5장, 건너뛰기 시 덱·다음 전투 4장 확인 |
| 실제 보스 흐름 | `HighGrade`·`RunVictory`, 선택 4→5/건너뛰기 4 유지와 재시작 최초 덱 4장 확인 |
| 실제 패배 흐름 | 폴드 12회 뒤 영혼 0·`RunDefeat`·보상 없음, 재시작 뒤 영혼 12·첫 스테이지·최초 덱 복구 확인 |
| 씬 검증 | `StageTest`, `CoreLoopTest` 문제 0; 씬 파일 변경 없음 |
| 시각 검토 | RW-04 화면을 기준으로 1920x1080 일반·보스·패배 7개 화면 비교, 정렬·잘림·겹침 문제 없이 98/100 통과 |
| Console | 테스트 러너 로그와 MCP WebSocket 경고를 기반 시설 출력으로 분류하고 정리한 뒤 최종 Error/Warning 0 확인 |
| 패키지·에셋 | `Packages/manifest.json`, `Packages/packages-lock.json` 변경 없음; 런타임·프리팹·외부 에셋 추가 없음 |

RW-05는 `BattleRewardSystemValidationTests`만 추가하고 기존 공개 세션·진행·Controller 경계를 반복 검증했다. 최초 실제 일반 검증용 20장 순서는 보상 상태에 도달하지 않아 검증 전용 4장 결정적 덱으로 교정했으며 프로젝트 런타임과 씬 데이터는 바꾸지 않았다. `execute_code`의 Roslyn 미지원은 CodeDom 경로로 전환했으며, 이는 게임 코드 오류가 아니라 검증 도구 실행기 차이다.

### 7.19 적 전투 프로필 EP-05 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구와 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 사용 |
| Unity 상태 | Unity 6000.3.10f1, `StageTest` 활성, 컴파일·도메인 리로드 완료, `ready_for_tools = true` |
| 런타임 구조 | `EnemyAI/BossCombatDisplayModel.cs`, `EnemyAI/Policies/FinalBossEnemyPolicy.cs` 추가; 기존 `Border` 어셈블리 유지 |
| 프로필 연결 | `final-boss`만 `final-boss-three-phase` 정책 키와 최대 영혼 7 사용 |
| 신규 EP-05 | 16/16 통과(job `e31a081bb30848e69e17a3d281bec746`) |
| CoreLoop 대상 | 179/179 통과(job `5fdab48db1854ee1b748dea66fa353d8`) |
| 전체 EditMode | 244/244 통과, 실패·건너뜀 0(job `174cda0192144028ab7fa6d3a2e2dcb1`) |
| 통합 흐름 | 처형 구간 강제 드로우 예고→플레이어 행동→실행, 보상 선택·건너뛰기→`RunVictory`→영혼 7 재시작 확인 |
| Console | 전체 에셋 갱신·컴파일과 테스트 뒤 Error 0 |
| 씬·패키지·에셋 | UI·씬·프리팹·패키지·외부 에셋 변경 없음 |

EP-05는 `EnemyObservationFactory`, 카드 효과 처리기와 `StageProgressionSession`을 변경하지 않고 공개 관측·유효 후보·보상 완료 목적지를 그대로 재사용했다. 보스 정책이 보존하는 상태는 예고 범주, 라운드와 공개 플레이어 행동 수뿐이며 실제 비공개 카드나 덱 순서는 포함하지 않는다. 신규 C#과 대응 `.meta`, 보스 전용 테스트만 구조에 추가했다.

### 7.20 적 전투 프로필 EP-06 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구와 `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 사용 |
| Unity 상태 | Unity 6000.3.10f1, 최종 `StageTest` 활성, Play Mode 정지, 컴파일·도메인 리로드 완료, `ready_for_tools = true` |
| 변환 경계 | `StageDefinition.CreateForEnemyProfile` → `EnemyBattleConfigurationFactory` → `StageBattleFactory` → `CoreLoopBattle`; 기본 보상은 같은 미리보기에서 파생 |
| 호환 경계 | 키 없는 기존 스테이지는 표준 20장 적 덱과 `SimpleEnemyPolicy` 유지 |
| 신규 EP-06 | 16/16 통과(job `3ee83cfa15724b7a8fd74ce24a741ae5`) |
| StageProgression 대상 | 81/81 통과(job `38db396aa0da42f98a8ed9d4b5f92dcb`) |
| CoreLoop 대상 | 179/179 통과(job `9ae86e1e0e8c4a7faa4370372109b668`) |
| 전체 EditMode | 260/260 통과, 실패·건너뜀 0(job `171e6423368546b289c7ebc5f60b469d`) |
| 반복 불변 조건 | 다섯 프로필 각각 10회, 합계 50회 생성에서 정책 인스턴스·손패·덱·영혼 상태 격리 확인 |
| 실제 런 | `gunslinger → enforcer → final-boss`, 최대 영혼 `3 → 5 → 7`; 첫 전투 PlayerTurn·적 덱 10장·`GunslingerEnemyPolicy` 확인 |
| 씬 검증 | `StageTest`, `CoreLoopTest` 누락 스크립트·깨진 프리팹·기타 문제 0; 씬 파일 변경 없음 |
| Console | 전체 에셋 갱신·컴파일·테스트·실제 실행 뒤 Error 0 |
| 패키지·에셋 | 패키지·프리팹·외부 에셋 추가 또는 변경 없음 |

EP-06은 새 어셈블리나 데이터 프레임워크를 만들지 않고 기존 `Border`, 프로필 카탈로그와 진행 세션을 재사용했다. `CoreLoopBattle.EnemyBehaviorPolicy`는 공개 게임 API가 아니라 친구 테스트 어셈블리에서 정책 주입을 확인하기 위한 `internal` 진단 경계다. 상대 후보 생성·선택 UI는 다른 담당 영역으로 남기고, 해당 코드가 안정적인 키 하나로 호출할 수 있는 변환 API만 제공했다. 실제 실행 점검의 Roslyn 컴파일러 미지원은 CodeDom으로 전환했으며 게임 코드 오류가 아니다.

### 7.21 상대 선택·적 전투 정보 UI EUI-01 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity 상태 | Unity 6000.3.10f1, `StageTest` 활성, Play Mode 정지, 컴파일·도메인 리로드 완료, `ready_for_tools = true` |
| 런타임 구조 | 기존 `Border` 안에 `StageProgression/OpponentSelection` 3개 타입 추가, 새 asmdef 없음 |
| 상태 구조 | `StageProgressionState.OpponentSelection`, `RunProgress` 내부 전이, 세션 Pending Offer·ActiveStage·선택 활성 여부 추가 |
| 호환 경계 | 생성기 미주입 세션과 `StageProgressionRuntime`은 기존 고정 전투 유지, 최종 보스는 선택 우회 |
| 신규 EUI-01 | 13/13 통과(job `d3707a140036404295062dd75fa6caca`) |
| StageProgression 대상 | 94/94 통과(job `69a8294a9ddb4ef6a2d5643d947498ca`) |
| CoreLoop 대상 | 179/179 통과(job `9192f912f7b944dca95000d3e2956b18`) |
| 전체 EditMode | 273/273 통과, 실패·건너뜀 0(job `2855caf85af54c7685005aa0da4296a6`) |
| 정적 진단 | 변경·신규 C# 7개 모두 오류·경고 0 |
| 갱신 복구 | 최초 스크립트 범위 갱신이 새 폴더를 포함하지 않아 타입 오류 4건 발생; 전체 에셋 갱신 후 해결 |
| Console | 테스트 기반 시설 로그 정리 후 최종 Error/Warning 0 |
| UI·씬·패키지 | View·Controller·Runtime·씬·프리팹·패키지·외부 에셋 변경 없음 |

EUI-01은 후보와 상태만 구현했으므로 실제 프로토타입 Runtime에 생성기를 주입하지 않았다. 선택 화면이 없는 상태에서 활성화하면 기존 Controller가 Battle null인 선택 상태를 전투 씬으로 보낼 수 있기 때문이다. EUI-02에서 진행 화면과 씬 이동 조건을 연결한 뒤 Runtime 활성화를 검토하고, 실제 프로필 전투 생성은 EUI-03에서 수행한다.

### 7.22 상대 선택·적 전투 정보 UI EUI-02 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity 상태 | Unity 6000.3.10f1, `StageTest` 활성, Play Mode 종료, 컴파일·도메인 리로드 완료, `ready_for_tools = true` |
| 표시 구조 | `OpponentCandidateViewModel`과 StageProgression 선택 필드 추가, 기존 `RunProgress` Presenter 오버로드 호환 유지 |
| 입력 구조 | Controller가 OfferId·집중 키를 로컬 보유, 제안 밖 키 무시, 클릭만으로 세션·진행·Battle 불변 |
| 화면 구조 | 기존 IMGUI에 후보 카드 2개·집중 강조·선택명·확정 가능 상태 추가, 씬 파일 수정 없음 |
| Runtime 구조 | 프로토타입 세션에 결정적 선택 생성기를 주입하고 `OpponentSelection` 성공 시 `StageTest`에서 다시 렌더링 |
| 신규 EUI-02 | 9/9 통과(job `394ecb4a561f4cc59530ae7b454ed25a`) |
| StageProgression 대상 | 103/103 통과(job `a6fb6dfea9934e02b804ba4db3c91fc6`) |
| CoreLoop 대상 | 179/179 통과(job `574504d9826f452e800b470bd3bf9b3d`) |
| 전체 EditMode | 282/282 통과, 실패·건너뜀 0(job `30fb5c670aac4662bff6910bdda0df1c`) |
| 정적 진단 | 변경·신규 C# 5개 모두 오류·경고 0 |
| 실제 실행 | Play Mode에서 START RUN 뒤 `OpponentSelection`, 후보 2명, Battle null, 미집중 확정 불가·집중 후 확정 가능 확인 |
| 화면 | 1280×720 미집중·1920×1080 집중 상태에서 겹침·잘림 없음 |
| 실패와 수정 | EditMode에서 Runtime `Awake` 직접 호출 시 `DontDestroyOnLoad` 제한 실패; 해당 테스트 제거 후 실제 Play Mode 검증으로 대체 |
| Console | 게임 코드 Error 0, MCP WebSocket 초기화 경고 1건은 패키지 기반 시설 로그 |
| UI·씬·패키지 | UI 런타임 스크립트만 변경, 씬·프리팹·Packages·외부 에셋 변경 없음 |

EUI-02는 실제 선택 화면을 열기 위해 Runtime에서 생성기를 활성화했지만 `TrySelectOpponent`를 추가하지 않았다. 따라서 집중과 확정 가능 상태는 화면 계층에서 완성되었고, 확정 키를 실제 전투·보상으로 원자적으로 변환하는 책임은 EUI-03에 남는다.

### 7.23 상대 선택·적 전투 정보 UI EUI-03 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity 상태 | Unity 6000.3.10f1, 최종 Play Mode 종료, 컴파일·도메인 리로드 완료, `ready_for_tools = true` |
| 세션 구조 | `TrySelectOpponent(offerId, profileKey)`가 상태·Pending Offer·현재 스테이지·정확한 후보를 검증하고 성공 뒤에만 ActiveStage·Pending Offer·Battle 교체 |
| 전투 변환 | 선택 미리보기 이름·키와 현재 템플릿 ID·종류·플레이어/적 덱 시드를 결합해 EP-06 Factory로 최대 영혼·10장 적 덱·전용 정책 생성 |
| Controller 구조 | 기존 View 확정 이벤트와 로컬 집중·입력 잠금·씬 라우팅 재사용, 성공한 InBattle+Battle만 `CoreLoopTest` 이동 |
| 신규 EUI-03 | 14/14 통과(job `d7e775363a0b47f8840b110863449e5a`) |
| StageProgression 대상 | 117/117 통과(job `cb4eeee92b7c4ef1a4e120536b55f528`) |
| CoreLoop 대상 | 179/179 통과(job `88a1b071ea9245b29a106c500e1e8725`) |
| 전체 EditMode | 296/296 통과, 실패·건너뜀 0(job `dbc8af7a87fc443c8ed748ffd325672b`) |
| 정적 진단 | `StageProgressionSession.cs`, `StageProgressionController.cs`, `OpponentSelectionIntegrationTests.cs` 오류·경고 0 |
| 반복 통합 | 첫 보상 뒤 OfferId 1 선택과 런 영혼·덱 보존, 두 번째 보상 뒤 고정 보스, 보스 승리 뒤 새 런 OfferId 0·초기 영혼·덱 복구 확인 |
| 실제 실행 | `StageTest`에서 집행관 확정 뒤 InBattle·Pending 제거·프로필 키 `enforcer`·최대 영혼 5·적 덱 10장을 확인하고 다음 프레임 `CoreLoopTest`와 전투 ViewModel 준비 확인 |
| 실패와 수정 | public NUnit 메서드의 private 열거형 매개변수로 CS0051 발생 후 접근 수준 수정; 실제 점검 코드의 internal 정책 속성 접근은 공개 상태만 읽도록 변경 |
| Console | 최종 게임 코드 Error/Warning 0 |
| 씬·패키지·에셋 | 씬·프리팹·Packages·외부 에셋 변경 없음 |

EUI-03은 EUI-02의 View·Presentation·Runtime 계약을 그대로 재사용해 세션과 Controller만 수정했다. 잘못된 OfferId·제안 밖 키·대소문자 불일치·빈 키와 Factory 예외는 상태·런 덱·플레이어 영혼·Pending Offer를 바꾸지 않는다. 선택 전투가 실제 프로필 정책을 사용하는지는 테스트 어셈블리의 기존 internal 진단 경계로 확인했으며, UI나 공개 게임 API에 정책 객체를 추가로 노출하지 않았다. 전투 중 일반·엘리트·보스 정보 표시는 EUI-04 범위다.

### 7.24 상대 선택·적 전투 정보 UI EUI-04 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity 상태 | Unity 6000.3.10f1, 최종 Play Mode 종료, 게임 뷰 Full HD 복구, 컴파일·도메인 리로드 완료 |
| 공개 추론 구조 | `EnemyObservationFactory.CreateNumberInferences`를 정책 관측과 표시 Factory가 공유 |
| 표시 구조 | `EnemyCombatDisplaySnapshotFactory`가 프로필·등급·요약과 제한된 추론/보스 필드만 가진 불변 스냅샷 생성 |
| UI 연결 | `CoreLoopController`가 진행 전투의 ProfileKey를 Presenter에 전달하고 View가 정보·경고 패널을 표시 |
| 정책 안전성 | 표시 생성 과정에서 적 정책 `Decide` 미호출, 비공개 카드 객체·덱 순서·카드 ID·정확한 다음 행동 미노출 |
| 신규 EUI-04 | 14/14 통과(job `fc32ed7441aa41e5aaae57b7a4a8bd07`) |
| CoreLoop 대상 | 193/193 통과(job `d0071b9bf2694bf986748635ec4e53fa`) |
| StageProgression 대상 | 117/117 통과(job `6136b5ec5c92428ba16cecaa9ab69063`) |
| 전체 EditMode | 310/310 통과, 실패·건너뜀 0(job `f5fa64aea08046eb8b77a7d1790d255c`) |
| 실제 실행 | 총잡이 NORMAL 상위 3개 확률, 집행관 ELITE 상위 2개·신뢰도, 최종 보스 BOSS 처형 구간·강제 드로우 예고 확인 |
| 화면 | 일반·엘리트·보스 예고 상태를 실제 1280×720·1920×1080 게임 뷰에서 각각 확인, 겹침·잘림 없음 |
| 반응형 수정 | 최초 720p 하단 카드 문구 잘림을 화면 높이별 글꼴·여백·버튼 높이로 수정하고 양쪽 해상도 재검증 |
| 실패와 수정 | 신규 타입 미반영 CS0246 5건은 전체 에셋 갱신으로 해결, 확률 기대값 1건은 기존 최대 나머지 동률 배분 규칙에 맞게 테스트 수정 |
| 씬·패키지·에셋 | 씬·프리팹·Packages·외부 에셋 변경 없음 |

EUI-04는 기존 `EnemyInferenceDisplayModel`과 `FinalBossEnemyPolicy.CurrentDisplay`를 수정하지 않고 재사용했다. 프로필 없는 독립 전투와 알 수 없는 프로필 키는 추정 프로필을 만들지 않으며, 표시를 읽는 행위가 적 정책 상태를 바꾸지 않는다. 전체 선택·보상·보스·재시작 반복 마감은 EUI-05에서 수행하고, 정식 아트 UI는 그 이후 별도 범위로 유지한다.

### 7.25 상대 선택·적 전투 정보 UI EUI-05 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity 상태 | Unity 6000.3.10f1, 최종 `StageTest` 활성, Play Mode 종료, Game View Full HD 복구, 컴파일·도메인 리로드 완료 |
| 신규 구조 | 제품 런타임을 늘리지 않고 `OpponentSelectionSystemValidationTests.cs`와 대응 `.meta`만 추가 |
| 반복 범위 | 일반+일반, 일반·엘리트 양쪽, 두 선택→고정 보스→보상→재시작, 오래된 제안·중복 확정·상태 격리, 고정 세션·독립 전투를 10회씩 검증 |
| 신규 EUI-05 | 5/5 통과(job `a310891b04cf47d696965511abe46c9d`) |
| StageProgression 대상 | 122/122 통과(job `4f27cb748daf437a89a0781ae5173a8c`) |
| CoreLoop 대상 | 193/193 통과(job `5890540805494edaa1114ebc17f8aa0f`) |
| 전체 EditMode | 315/315 통과, 실패·건너뜀 0(job `d9ea1cffb1804b559dc028350d1c199b`) |
| 실제 씬 왕복 | 두 후보 확정·일반/높은 등급 보상·고정 보스·보스 보상·RunVictory·재시작을 `StageTest`↔`CoreLoopTest`로 확인 |
| 실제 화면 | 선택·일반·엘리트·보스를 1280×720·1920×1080에서 확인, 안정화 캡처 기준 겹침·잘림·입력 누락 없음 |
| 씬 유효성 | `StageTest`, `CoreLoopTest` 누락 스크립트·깨진 프리팹 0, Build Settings 활성 |
| Console | 테스트 러너 안내를 비운 뒤 실제 선택 화면 실행·종료 Error/Warning 0 |
| 씬·패키지·에셋 | 씬·프리팹·Packages·외부 에셋 변경 없음 |

EUI-05는 자동 반복과 실제 씬 검증의 책임을 분리했다. 결정적 테스트가 전체 흐름의 10회 안정성과 상태 초기화를 보장하고, Play Mode가 `StageProgressionRuntime`의 씬 간 세션 보존·Controller 입력·실제 화면을 확인한다. 첫 고정 세션 테스트는 재시작 뒤 `NotStarted`를 기대해 실패했지만 기존 호환 계약인 즉시 `InBattle`을 유지하도록 기대만 수정했다.

### 7.26 현행 전투 규칙 이관 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, 컴파일·도메인 리로드 완료, 검증 종료 시 도구 사용 가능 상태 |
| 구조 변경 | `RoundResolver`·`CoreLoopBattle`·세션에서 폴드 계약 제거, EnemyAI 후보·정책에서 폴드 제거, 체인지 완료 횟수와 다음 비용을 전투가 소유 |
| 카드 이동 | 기존 비공개 카드를 공개 후 버림패로 이동하고 후보 2장 선택을 진행하도록 `BattleParticipant` 경계 수정 |
| UI | Fold 이벤트·버튼 제거, Change 비용·사용 후 잔여 영혼 표시, 공개 카드 효과명 갱신 |
| 전체 EditMode | 306/306 통과, 실패·건너뜀 0(job `7132717ccc23450c9720b439a6558625`) |
| Console | 스크립트 재컴파일 오류 0; 테스트 러너 사전·사후 처리와 결과 저장 안내만 확인 |
| 제외 경계 | `GameScene`, 정식 런·상점 코드, Packages, 외부 에셋 변경 없음 |

현행 체인지 조건은 `Player.Soul.Current > NextPlayerChangeSoulCost`로 고정했다. 정확히 비용만큼의 영혼만 있으면 시작 전에 거절하므로 체인지 비용이 직접 런 패배를 만들지 않는다. 과거 BA·EP 문서의 폴드 기록은 구현 이력으로 유지하고, 현재 API와 검증 결과는 각 문서 상단의 현행 이관 안내에서 구분한다.

### 7.27 위협용 해머 현행 규칙 이관 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, `GameScene` 활성 상태에서 스크립트 재컴파일 완료 |
| 규칙 구조 | 상대 공개 카드 선택·폐기, 스탠드 취소와 비공개 교체, 자신의 카드 비용 제거 |
| 적 AI | 선택 종류로 플레이어 손패 대상을 조회하고 집행관·최종 보스가 최고 숫자 공개 카드를 우선 |
| UI | `GameScenePresentation`의 상대 대상 분류와 효과 설명 갱신 |
| 전체 EditMode | 308/308 통과, 실패·건너뜀 0(job `e9931602f6824c68aa8642bde7aee103`) |
| 제외 경계 | `GameScene.unity`, Packages, 외부 에셋 변경 없음 |

최초 CoreLoop 어셈블리 실행에서는 188개 중 1개가 실패했다. 공개 카드 제거 뒤 적이 반드시 스탠드한다는 해머 효과와 무관한 테스트 가정을 제거한 뒤 전체 회귀를 재실행해 308/308 통과를 확인했다. MCP 테스트 작업이 도메인 리로드 뒤 완료 상태를 놓친 경우 패키지에 이미 구현된 `ClearStuckJob` 경계를 실행 코드로 호출해 고착 상태만 정리했으며 프로젝트 파일은 변경하지 않았다.

### 7.28 보위 나이프·중간 버스트 현행 규칙 이관 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | Unity MCP 직접 도구, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, 스크립트 컴파일·도메인 리로드 완료 |
| 판정 구조 | 행동 도중 `VisibleHandValue`와 명시적 `ResolveNumericBust`, 양쪽 스탠드 뒤 전체 손패 `Resolve` 분리 |
| 보위 나이프 | 공개 합 버스트 시 즉시 종료, 비버스트 시 방금 강제 드로우한 카드 반드시 폐기, 유지 정책 제거 |
| 영향 테스트 | CoreLoop 10/10 + 진행 동기화 3/3 통과 |
| 전체 EditMode | 309/309 통과, 실패·건너뜀 0(job `4abba102590f4392b20faae5b8869e10`) |
| Console | 테스트 러너 사전·사후 안내를 비운 뒤 Error/Warning 0 |
| 제외 경계 | `GameScene`, Packages, 외부 에셋 변경 없음 |

첫 클래스 단위 테스트 요청은 Unity 테스트 러너 초기화 제한으로 시작되지 않았고 코드 테스트 실패는 없었다. 정확한 테스트 메서드 10개를 지정해 통과시킨 뒤 전체 회귀에서 구 규칙의 비공개 포함 즉시 버스트를 기대하던 진행 테스트 3개를 발견했다. 공개 합이 실제로 21을 넘는 픽스처로 교정한 뒤 해당 3개와 전체 309개를 재실행했다. 최종 재검증 요청 시 Editor가 Play Mode 전환 중이라 한 번 거절되어 Play Mode를 종료했고, 이어서 309/309 통과를 확인했다.

### 7.29 악마 계약 DC-01 데이터·런/전투 덱 기반 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, `GameScene` 활성, 에셋 갱신·컴파일·도메인 리로드 완료 |
| CoreLoop 구조 | `DemonContractDefinition`·종류·카탈로그·물리 카드·전용 드로우/버림 덱을 `CoreLoop/DemonContracts`에 분리 |
| StageProgression 구조 | `RunDemonDefinition`과 `PlayerRunState` 최초/현재 악마 덱, `StageBattleFactory`의 별도 런→전투 변환 추가 |
| 전투 경계 | `CoreLoopBattle.PlayerDemonDeck`이 전투별 독립 인스턴스를 소유하며 독립 전투는 공유되지 않는 빈 덱으로 호환 |
| 난수 경계 | 악마 덱은 플레이어 일반 덱 시드에서 파생한 별도 시드와 자체 `DeterministicRng`를 사용 |
| 대상 테스트 | DC-01 19/19 통과(job `064ec40fd3444462898eb2ba2c732719`) |
| 전체 EditMode | 329/329 통과, 실패·건너뜀 0(job `9db8defa9d234ecd8a1d129415446641`) |
| 표시 회귀 | 기존 CU-M03 플레이어 투영 불일치 복구 후 단독 1/1 통과(job `a5d0763e9f604662ad4da41bdf12a801`) |
| Console | 컴파일·게임 오류 0; 최종 테스트 뒤 Test Framework 경고 4건과 결과 저장 `Exception` 2건만 존재 |
| 씬·패키지·에셋 | `GameScene.unity`, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-01은 일반 카드 카탈로그·덱과 악마 계약 카탈로그·덱을 서로 다른 타입으로 유지한다. 물리 ID 값은 두 도메인에서 겹칠 수 있지만 객체와 소유 위치가 합쳐지지 않는다. 계약 후보를 조회하는 DC-02 이후 API는 이번 단계의 `CanTakeCandidates`·`TakeCandidates` 경계를 사용해야 하며, UI 조회가 이 명령을 직접 실행해 난수를 소비해서는 안 된다.

### 7.30 악마 계약 DC-02 행동·선택·세션 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, `GameScene` 활성, 에셋 갱신·도메인 리로드·컴파일 완료 |
| 상태 모델 | `DemonContractAvailability`, `PlayerResolvingDemonContract`, 증가형 `InteractionId`와 후보 세 옵션 |
| 선택 트랜잭션 | 비용·횟수 승인 뒤 후보 이동, 물리 카드 하나 활성·둘 버림, 오래된·중복 입력 무변경 거절 |
| 효과 경계 | 기본 Resolver에는 개별 악마 처리기 없음; 주입 처리기로 대가 영혼 고갈·후속 AI 중단만 검증 |
| 세션 경계 | `CoreLoopSession`·`StageProgressionSession` 전달과 계약 대가 패배의 지속 영혼·런 결과 단일 동기화 |
| 대상 테스트 | DC-02 13/13 통과(job `dd7785952657473788ff823903610e8d`) |
| 전체 EditMode | 342/342 통과, 실패·건너뜀 0(job `a52e53b9d58143d4b2fb593528acd67a`) |
| Console | 컴파일·게임 오류 0; 테스트 뒤 Test Framework 기반 시설 경고 3건과 결과 저장 `Exception` 2건만 존재 |
| 씬·패키지·에셋 | `.unity` 씬, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-02는 계약 화면과 개별 악마 효과를 구현하지 않는다. 선택 중 입력 잠금은 전투 상태와 세션 API에서 이미 강제되며, DC-03은 이 활성 계약·Resolver 경계를 사용해 벨페고르의 플레이어 전용 미리보기만 추가해야 한다. 공용 로그와 적 AI에는 덱 위 카드 값이나 순서를 전달하지 않는다.

### 7.31 악마 계약 DC-03 벨페고르 수직 기능 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, `GameScene` 활성, 외부 에셋 강제 갱신·도메인 리로드·컴파일 완료 |
| 덱 경계 | 이동 없는 `TryPeekTop`, 예상 물리 ID를 확인하는 `TryMoveTopToBottom`, 카드 총수·가용 소유권 보존 |
| 효과 경계 | 기본 Resolver에 벨페고르만 등록, 물리 계약별 런타임 상태와 명시적 히트 전·차례 시작·행동 완료·라운드 종료 훅 |
| 정보 은닉 | 공개 옵션에는 카드 ID·숫자 없음, `PlayerDemonContractPreview`에만 실제 카드 정보 제공, 적 관측·공용 로그 불변 |
| 차례 경계 | 적 스탠드 뒤 다음 정상 차례 행동 완료 시 자동 스탠드 1회, 라운드·전투 종료 초기화 |
| 대상 테스트 | DC-03 9/9 통과(job `4ae2493405764b6eb784424d87918ffc`) |
| 전체 EditMode | 351/351 통과, 실패·건너뜀 0(job `81d2761c997a451494ec009af715d459`) |
| Console | 컴파일·게임 오류 0; MCP 경고 1건, Test Framework 경고 4건과 결과 저장 `Exception` 2건만 존재 |
| 씬·UI·패키지·에셋 | `.unity` 씬, UI, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-03은 전투 규칙과 소유자 전용 표시 모델까지만 구현했다. `CoreLoopSession`과 `StageProgressionSession`의 기존 계약 해결 입력이 벨페고르 후속 선택도 그대로 전달하므로 세션 API를 늘리지 않았다. DC-04는 같은 선택·훅 경계를 사용하되 마몬 난수와 레비아탄 카드 결과 연결을 추가하고, 사탄·계약 화면은 앞당기지 않는다.

### 7.32 악마 계약 DC-04 마몬·레비아탄 수직 기능 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 프로젝트의 MCP for Unity 패키지를 확인했으나 현재 Codex 세션에 Unity MCP 도구가 노출되지 않아, 이미 열린 Unity Editor에서 공식 `TestRunnerApi`를 사용 |
| Unity | 6000.3.10f1, 에셋 갱신·도메인 리로드·컴파일 완료 |
| 마몬 경계 | 주입 가능한 주사위 난수, 물리 계약별 현재 눈·차례 선택·최종 선택 상태, 일반 행동을 소비하지 않는 유지/재굴림과 최종 합산 선택 |
| 레비아탄 경계 | 리볼버 본효과 실패 뒤에만 실행되는 후처리, 상대 공개 합 22 이상 계약 효과 버스트와 미만 시 영혼 1 지불 |
| 종료 규칙 | 새로 굴린 마몬 6과 레비아탄 역버스트는 `ContractEffectBust`, 영혼 0은 즉시 전투 패배로 기존 종료 경계에 합류 |
| 정보 은닉 | 공용 효과 결과는 발동·버스트 대상·지불 영혼만 노출하고 상대 비공개 합과 미래 주사위 결과를 보존하지 않음 |
| 대상 테스트 | DC-04 11/11 통과 |
| 전체 EditMode | 362/362 통과, 실패·건너뜀·미결정 0 |
| 컴파일 | 최종 Unity 에셋 갱신과 도메인 리로드 성공, 컴파일 오류 0 |
| 씬·UI·패키지·에셋 | `.unity` 씬, UI, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-04는 마몬과 레비아탄의 전투 규칙 및 안전한 결과 경계까지만 구현했다. 마몬의 주사위는 손패나 공개 합에 카드처럼 추가되지 않고 최종 선택 때만 에이스 재계산을 포함한 전체 합에 더해진다. 레비아탄은 리볼버가 원래 성공하면 개입하지 않으며, 실패했을 때만 상대 공개 합을 판정한다. DC-07 회귀에서 비공개 숫자는 최종 승부 전 레비아탄 판정에도 포함하지 않는 현행 규칙으로 고정했다. 사탄과 계약 UI·씬 연결은 DC-05 이후 단계에서 완료했다.

### 7.33 악마 계약 DC-05 플레이어 화면·런 연결 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, 최종 `GameScene` 활성, Play Mode 종료, Game View Full HD 복구, 컴파일·도메인 리로드 완료 |
| 표시 경계 | `DemonContractPanelViewModel`에 가용성·비용·후보·활성 상태·소유자 전용 미리보기·안전한 최근 결과만 투영 |
| 입력 경계 | 비용 확인 전 취소는 UI 로컬 상태, 승인 뒤 기존 `CoreLoopSession`·`StageProgressionSession`의 상호작용 ID 명령 전달 |
| 독립·런 | 독립 전투는 네 장 프로토타입 악마 덱 주입, 런 전투는 기존 런 악마 덱 변환과 영혼·패배·보상 동기화 재사용 |
| 사탄 경계 | 후보 정보는 보이되 `DC-06 구현 예정`으로 선택 불가, 처리기·카운터·권능 카드는 추가하지 않음 |
| 대상 테스트 | DC-05 7/7 통과(job `6c374216652b415abf5278a0b4125cd1`) |
| 전체 EditMode | 369/369 통과, 실패·건너뜀 0(job `a4889b47af04497da60243494220f866`) |
| 실제 화면 | `GameScene`·`CoreLoopTest`의 계약 후보를 1280×720·1920×1080에서 확인, 이름·능력·대가·선택 버튼 잘림 없음 |
| 시각 판정 | 최초 긴 버튼 문구 잘림 58점에서 전용 후보 카드 레이아웃으로 교정 뒤 94점 통과 |
| Console | Test Framework 기반 시설 출력 6건을 분리하고 비운 뒤 Error/Warning 0 |
| 씬·패키지·에셋 | `.unity` 씬, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-05는 두 기존 UI 스크립트만 확장해 씬 직렬화 변경을 피했다. 벨페고르의 덱 위 숫자는 소유자 전용 문구로만 표시하고 상대 비공개 카드 숫자는 기존처럼 0 또는 `?`로 투영했다. 당시 사탄은 DC-D05·D06 확정 전까지 비활성 상태로 유지했으며, 후속 DC-06에서 해당 규칙과 UI를 구현했다.

### 7.34 악마 계약 DC-06 사탄·양면 권능 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, 최종 `GameScene` 활성, Play Mode 종료, Game View Full HD 복구 |
| 계약 경계 | `SatanDemonContractHandler`가 정상 차례 카운터·스탠드/버스트 제한·0 도달 영혼 대가와 계약 종료 담당 |
| 카드 경계 | `SatanPowerEffectHandler`가 화염 강제 히트·괴력 두 수 선언 담당, 덱은 임시 물리 ID 등록·면 변형·전투 종료 제거만 제공 |
| 전투 경계 | `CoreLoopBattle`이 정상 차례 훅·버스트 방지 질의·계약 종료 뒤 영혼 패배와 공개 합 재검사 순서를 조정 |
| 표시 경계 | 계약 패널은 남은 정상 차례·현재 권능 면을 표시하고 `CanPlayerStand`는 전투의 제한 결과를 직접 투영 |
| 대상 테스트 | DC-06 8/8 통과(job `7a2056a9dd554125b85b7925bfd7b754`) |
| 전체 EditMode | 377/377 통과, 실패·건너뜀 0(job `177c50b40d45421fb58bb7166f2c6fdd`) |
| 실제 화면 | `GameScene`·`CoreLoopTest`에서 계약 선택·활성 상태·스탠드 잠금·권능을 1280×720·1920×1080으로 확인 |
| 시각 판정 | 720p 간격·1080p 패널 상한 교정 뒤 94점 통과 |
| Console | 최종 컴파일 오류와 Console Error 0 |
| 씬·패키지·에셋 | `.unity` 씬, 프리팹, Packages, 외부 에셋 변경 없음 |

DC-06은 일반 카드 카탈로그에 권능 면 정의만 추가하고 계약 상태는 `DemonContracts`에 유지한다. 카드 효과 실행은 기존 `CardEffectResolver` 확장점을 재사용하지만 카운터·대가·스탠드/버스트 규칙은 계약 처리기가 소유하므로 일반 카드와 계약 수명이 섞이지 않는다. 이 안정된 플레이어 계약 API를 사용하는 DC-07 적 AI 계약과 반복 검증도 다음 절과 같이 완료했다.

### 7.35 악마 계약 DC-07 적 AI·소유자 대칭 검증

| 항목 | 결과 |
| --- | --- |
| 검증 경로 | 로컬 표준 HTTP Unity MCP 세션, `DiaBlackJack@5635a4cdcfecc8dd` 인스턴스 |
| Unity | 6000.3.10f1, `GameScene` 활성, Play Mode 종료, 에셋 갱신·도메인 리로드·컴파일 완료 |
| 후보 경계 | 관측 시 일반 계약 후보만 생성해 덱을 보존하고, 실행 승인 뒤 비용·횟수·3장 후보를 다시 검증 |
| 정책 경계 | 광신도가 계약을 히트보다 우선하고 네 악마·벨페고르·마몬 후속 옵션을 타입으로 선택하며 보장된 사탄 영혼 사망을 회피 |
| 효과 경계 | `DemonContractResolver`의 소유자 방향 API가 적 사탄·벨페고르·마몬·레비아탄에 플레이어와 같은 규칙 적용 |
| 전투 변환 | `StageBattleFactory`가 광신도에게만 별도 시드의 4장 적 악마 덱을 주입하고 다른 적은 빈 덱 유지 |
| 표시·은닉 | 적 활성 계약은 `상대`로 표시, 적 벨페고르 미리보기·플레이어 비공개 숫자·덱 순서·미래 난수는 공용 모델에서 제외 |
| 대상 테스트 | DC-07 12/12 통과(`df579d383fcb43e5a5c2edbe332e1c8d`), 네 악마 종류별 10전과 Cultist 덱 50회 격리 포함 |
| 회귀 테스트 | CoreLoop 260/260(`eccf46eb3be44fb9950826cbbf059a9e`)·전체 EditMode 389/389(`14c1b1d8ca644c34a0d4c20e4151467c`) |
| CoreLoop | 260/260 통과, 실패·건너뜀 0 |
| 전체 EditMode | 389/389 통과, 실패·건너뜀 0 |
| 실제 화면 | `GameScene` Full HD Play Mode 스모크 통과, DC-05~06 반응형 배치와 씬 직렬화 무변경 |
| Console | 최종 Error/Warning 0 |
| 씬·패키지·에셋 | `.unity` 씬, 프리팹, Packages, 외부 에셋·오픈소스 변경 없음 |

DC-07은 기존 `EnemyObservation`→후보→정책→실행 재검증 구조와 계약 처리기를 재사용했다. 적과 플레이어의 활성 계약 컬렉션은 소유권·표시를 분리하지만 효과 코드를 복제하지 않는다. 적 전용 계약 선택은 동기적으로 해결되므로 기존 씬 입력을 추가하지 않았고, `GameScene.unity` 변경 없이 현재 패널에서 상대 계약 상태를 확인할 수 있다.

### 7.36 악마 계약 DC-08 광신도 선택 밸런스 검증

| 경계 | 현행 구조 |
| --- | --- |
| 선택 효용 | 사탄은 종료 대가 뒤 생존, 레비아탄은 `Available` 리볼버 보유 시에만 우선 |
| 선택 분산 | 벨페고르·마몬 동일 점수와 전투 시드 기반 후보 순서로 추가 난수 없이 결정 |
| 정보 경계 | 벨페고르는 공개 합, 마몬 최종 선택만 전체 합 사용; 플레이어 비공개·미래 난수 추가 없음 |
| 반복 검증 | 400시드 계약 분포·같은 시드 50회 재현·100회 자동 전투 종료 |
| Unity 검증 | 대상 8/8(`544d50f9e7c447fa8a173b60d6b9b7ea`)·CoreLoop 268/268(`2faa464b6e624f338bdb5e87dc544439`)·전체 397/397(`16c721d04d79449cbd89c9b42a67320c`) |
| 변경 범위 | `CultistEnemyPolicy.cs`, `CultistContractBalanceTests.cs`와 문서; 씬·프리팹·Packages 무변경 |

DC-08은 새로운 밸런스 시스템이나 런타임 계측기를 추가하지 않고 기존 정책 점수와 결정적 후보 순서를 활용했다. 현재 광신도 최대 영혼 3·리볼버 없는 덱 구성에서는 사탄·레비아탄이 선택되지 않으며, 프로필이 바뀌면 같은 효용 조건으로 자동 재평가된다.

### 7.37 자동 발동 카드 AC-01 공통 기반

| 경계 | 현행 구조 |
| --- | --- |
| 정의 | `CardEffectKind`와 `CardDefinitionCatalog`에 자동 카드 5종 등록, 기존 등급 기본 정의는 유지 |
| 처리기 | 수동 `CardEffectResolver`와 분리된 `AutomaticCardEffectResolver`, 테스트 처리기 주입 가능 |
| 상호작용 | 선택 주체·증가형 ID·타입 선택지를 가진 `PendingAutomaticCardInteraction`, 완료 뒤 공개 `AutomaticCardResult` |
| 전투 상태 | `ResolvingAutomaticCardEffect` 동안 일반 입력 잠금, 오래된·중복 ID 무변경 거절 |
| 공개 유입 | 플레이어·적 히트, 수정 구슬 선택 공개, 보위 나이프·사탄 권능 강제 공개 드로우 |
| 제외 유입 | 최초 공개·비공개 배분, 체인지 결과, 위협용 해머 비공개 교체 |
| 재개 | 열거형 연속 처리로 히트 또는 부모 수동 카드의 남은 판정과 행동 후속 처리를 한 번만 재개 |
| Unity 검증 | 대상 15/15·CoreLoop 283/283·전체 EditMode 412/412·컴파일 오류 0 |
| 변경 보호 | UI·진행 세션·씬·프리팹·Packages·외부 에셋 무변경 |

AC-01은 실제 독극물 등 카드 효과를 등록하지 않고 주입식 가짜 처리기로 공통 수명주기만 검증했다. 따라서 기본 전투에 자동 카드가 보상으로 들어오는 경로는 아직 없으며, 다음 AC-02부터 실제 처리기를 한 종씩 등록한다.

### 7.38 자동 발동 카드 AC-02 독극물

| 경계 | 현행 구조 |
| --- | --- |
| 처리기 | `PoisonEffectHandler`가 소유자 방향 하나로 즉시 스탠드·영혼 지불 선택을 처리 |
| 계약 제한 | `CoreLoopBattle.CanOwnerStandForAutomaticCard`가 실제 활성 계약의 스탠드 제한을 조회 |
| 영혼 위험 | `min(3, 현재)` 지불, 0이면 원본 폐기 뒤 부모 효과·원래 행동·적 후속 처리 취소와 즉시 전투 종료 |
| 회복 예약 | `AutomaticCardBattleState`가 물리 카드 ID·소유자·라운드·회복량 5를 발동 순서로 보존 |
| 라운드 해결 | 피해 적용 뒤 살아 있는 승자에게만 `SoulPool.Restore` 적용, 최대 영혼 상한 뒤 예약 전체 정리 |
| 대칭성 | 플레이어·적이 같은 처리기와 예약 상태를 사용하며 선택 주체만 소유자로 투영 |
| Unity 검증 | 대상 12/12·CoreLoop 295/295·전체 EditMode 424/424·컴파일 오류 0 |
| 변경 보호 | UI·세션·보상·적 정책·GameScene·프리팹·Packages·외부 에셋 무변경 |

열린 Unity MCP 세션은 인스턴스 목록에는 남았지만 ping에 응답하지 않았다. 저장되지 않은 편집기 상태를 보호하기 위해 강제 재시작하지 않고 `dd09d89` 임시 Git worktree에 AC-02 변경만 복제해 Unity 6000.3.10f1 Headless로 검증했다. 부활초가 승패 없이 라운드를 초기화할 때 예약을 버리는 실제 호출은 AC-05에서 공용 정리 경계에 연결한다.

### 7.39 자동 발동 카드 AC-03 거짓말 탐지기

| 경계 | 현행 구조 |
| --- | --- |
| 처리기 | `LieDetectorEffectHandler`가 소유자 방향 하나로 1~10 선언과 정확히 한 장인 상대 비공개 카드 비교를 처리 |
| 공용 결과 | `LieDetectorPublicResult`가 소유자·선언 숫자·판정 가능 여부만 제공하고 이상/미만과 실제 숫자를 제외 |
| 전용 지식 | `HiddenCardComparisonKnowledge`가 관측자·대상·선언·이상 여부·라운드만 공개하고 대상 카드 ID는 내부 무효화에만 사용 |
| 플레이어 경계 | `CoreLoopBattle.PlayerHiddenCardComparisonKnowledge`가 플레이어가 소유한 결과만 제공 |
| 적 관측 | `EnemyObservation.LieDetectorComparisonKnowledge`가 적이 합법적으로 얻은 비교 조건만 제공 |
| 폐기 | 플레이어 체인지·양측 해머 교체·새 라운드·전투 종료에서 대상 ID 또는 라운드 상태로 지식 제거 |
| 판정 불가 | 상대 비공개 카드가 정확히 한 장이 아니면 숫자를 읽지 않고 공용 판정 불가 결과 뒤 원본 폐기 |
| Unity 검증 | 대상 10/10·CoreLoop 305/305·전체 EditMode 434/434·컴파일 오류 0·Test Framework 결과 저장 안내 3건 |
| 변경 보호 | `GameScene`·UI·세션·보상·적 자동 정책·프리팹·Packages·외부 에셋 무변경 |

AC-03은 `mcpforunity://custom-tools`, 인스턴스, 편집기 상태와 프로젝트 정보를 확인한 뒤 연결된 `DiaBlackJack@5635a4cdcfecc8dd` 편집기에서 컴파일과 테스트를 수행했다. 활성 `GameScene`은 열린 상태만 확인하고 씬 직렬화는 수정하지 않았다. 전체 회귀에서 기존 적 관측의 `PlayerHidden...` 속성 금지 검사가 새 비교 속성명을 거절해, 실제 값뿐 아니라 API 명명도 효과 중심 경계로 교정했다.

## 8. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-03 탐지기 선언·공개/전용 결과·적 비교 관측·체인지/해머/라운드/전투 종료 지식 폐기 구조와 대상 10/10·CoreLoop 305/305·전체 434/434·컴파일 오류 0·Test Framework 결과 저장 안내 3건 검증 결과 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-02 독극물 처리기·계약 제한·영혼 0 취소·승리 회복 예약 구조와 대상 12/12·CoreLoop 295/295·전체 424/424·컴파일 오류 0 검증 결과 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-01 Resolver·보류 선택·공개 유입·연속 처리 구조와 대상 15/15·CoreLoop 283/283·전체 412/412·컴파일 오류 0 검증 결과 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-08 광신도 사탄 생존·레비아탄 리볼버 효용 조건, 벨페고르·마몬 결정적 분산과 400시드·100자동전투 검증 구조 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-07 무변경 적 후보·광신도 정책·소유자 대칭 4종 처리·Cultist 전용 덱·안전 표시 구조와 대상 12/12·CoreLoop 260/260·전체 389/389·Console 0 검증 결과 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-06 사탄 계약/권능 처리기·임시 카드·전투 훅·표시 구조와 대상 8/8·전체 377/377·두 씬·두 해상도·Console 0 검증 결과 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-05 표시 모델·비용 확인·후보/후속 입력·독립/런 UI와 대상 7/7·전체 369/369·두 씬·두 해상도·Console 0 검증 결과 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-04 마몬 주사위 선택·최종 합산과 레비아탄 리볼버 후처리·정보 은닉 구조, 대상 11/11·전체 EditMode 362/362·컴파일 오류 0 검증 결과 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-03 벨페고르 처리기·플레이어 전용 미리보기·동일 ID 덱 처리·행동 후 자동 스탠드 구조와 대상 9/9·전체 351/351·컴파일 오류 0 검증 결과 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-02 비용·횟수·필수 후보 선택·상호작용 ID·활성 계약·대가 패배·세션 전달 구조와 대상 13/13·전체 342/342·컴파일 오류 0 검증 결과 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-01 별도 정의·런/전투 덱·전투 변환 구조와 Unity MCP 대상 19/19·전체 329/329·컴파일 오류 0 및 테스트 기반 시설 출력 6건 분리 결과 추가 |
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
| 2026-07-19 | 이천서 | 카드 사용 CU-02 선택 대기·효과 완료·종료 원인 구조와 신규 16개·CoreLoop 87/87·전체 EditMode 117/117·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 카드 사용 CU-03 리볼버 단일 비공개 카드 추측·정보 은닉 구조와 신규 8개·CoreLoop 95/95·전체 EditMode 125/125·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 카드 사용 CU-04 수정 구슬 임시 소유·해머 단일 비공개 교체·나이프 정책 경계와 신규 18개·CoreLoop 113/113·전체 EditMode 143/143·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 카드 사용 CU-05 카드 표시·선택 UI·독립/런 전달 구조와 관련 28/28·CoreLoop 117/117·StageProgression 34/34·전체 EditMode 151/151·Game View·씬·Console 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 카드 사용 CU-06 반복 회귀 5/5·CoreLoop 122/122·StageProgression 34/34·전체 EditMode 156/156, 실제 런 승리·패배 재시작·씬·Console 최종 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 전투 보상 RW-01 런타임·테스트 구조와 신규 8/8·StageProgression 42/42·전체 EditMode 164/164 Unity 배치 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 전투 보상 RW-02 상태 런타임·테스트 구조와 로컬 Unity MCP 신규 7/7·StageProgression 49/49·전체 EditMode 171/171 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 전투 보상 RW-03 실제 승리·보상 생성·등급 주입·다음 전투 덱 통합과 신규 6/6·StageProgression 55/55·전체 EditMode 177/177 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 전투 보상 RW-04 기존 진행 화면 후보·선택·건너뛰기·결과 표시 구조와 신규 5/5·StageProgression 60/60·전체 EditMode 182/182·Game View·씬 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 전투 보상 RW-05 반복 회귀 5/5·CoreLoop 122/122·StageProgression 65/65·카드 사용 5/5·전체 187/187, 실제 일반/보스/패배·재시작·씬·시각·Console 최종 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-05 보스 전용 정책·표시 모델 구조와 신규 16/16·CoreLoop 179/179·전체 EditMode 244/244·Console Error 0 검증 결과 갱신 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-06 선택 키의 실제 덱·영혼·정책·보상 변환 구조와 신규 16/16·StageProgression 81/81·CoreLoop 179/179·전체 EditMode 260/260·실제 런·씬·Console 검증 결과 갱신 |
| 2026-07-20 | 이천서 | EUI-01 상대 후보·선택 상태·세션 주입 구조와 신규 13/13·StageProgression 94/94·CoreLoop 179/179·전체 EditMode 273/273·스크립트 진단·Console 검증 결과 갱신 |
| 2026-07-20 | 이천서 | EUI-02 후보 표시·로컬 집중·선택 상태 화면 유지 구조와 신규 9/9·StageProgression 103/103·CoreLoop 179/179·전체 EditMode 282/282·두 해상도·Play Mode 검증 결과 갱신 |
| 2026-07-20 | 이천서 | EUI-03 OfferId+ProfileKey 원자적 확정·선택 프로필 전투·보상·두 선택·고정 보스·재시작 구조와 신규 14/14·StageProgression 117/117·CoreLoop 179/179·전체 EditMode 296/296·실제 전투 씬 전환 검증 결과 갱신 |
| 2026-07-20 | 이천서 | EUI-04 공개 추론 공유·등급별 안전 스냅샷·전투 정보/보스 예고 패널·720p 반응형 구조와 신규 14/14·StageProgression 117/117·CoreLoop 193/193·전체 EditMode 310/310·두 해상도 화면 검증 결과 갱신 |
| 2026-07-20 | 이천서 | EUI-05 반복 검증 파일과 신규 5/5·StageProgression 122/122·CoreLoop 193/193·전체 EditMode 315/315·실제 두 씬·두 해상도·Console 0 검증 결과를 추가해 상대 선택·전투 정보 UI 구조 기록 마감 |
| 2026-07-21 | 이천서 | 현행 폴드 삭제·체인지 전투 누적 비용 구조와 Unity MCP 전체 EditMode 306/306·컴파일 오류 0 검증 결과를 추가하고 `GameScene` 무변경 확인 |
| 2026-07-22 | 이천서 | 위협용 해머 상대 공개 카드 제거·적 AI·GameScene 표시 이관과 Unity MCP 전체 EditMode 308/308 검증 결과 추가 |
| 2026-07-22 | 이천서 | 보위 나이프 비버스트 강제 폐기·공개 합 중간 버스트·전체 합 최종 승부 구조와 영향 13/13·전체 EditMode 309/309·Console 0 검증 결과 추가 |
