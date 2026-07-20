# 프로젝트 구조 및 Unity MCP 참조 기록

> 프로젝트: DiaBlackJack  
> 확인 책임자: 이천서  
> 버전: v0.1  
> 확인일: 2026-07-20

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
| `Assets/01. Scripts/Runtime/CoreLoop` | 전투 규칙·상태·세션, 카드 효과와 적 프로필·공개 관측·정책 | EP-06 선택 전투가 프로필 전용 덱·영혼·정책을 주입 |
| `Assets/01. Scripts/Runtime/Input` | Input System 연결 | 1~2단계 제외 |
| `Assets/01. Scripts/Runtime/UI` | 공용 UI와 코어 루프 View | CU-05 카드별 사용·효과 선택·최근 결과 표시와 입력 사용 |
| `Assets/01. Scripts/Runtime/StageProgression` | 런·스테이지 순수 상태, 전투·보상·상대 후보와 선택 프로필 키 변환 | EUI-01 `OpponentSelection` 후보·상태·세션 주입, EP-06 프로필 전투 변환 연결 |
| `Assets/01. Scripts/Runtime/UI/StageProgression` | 진행 표시·입력과 씬 간 Runtime | RW-04 보상 후보·완료 목적지·결과 표시와 선택/건너뛰기 입력 사용 |
| `Assets/02. ScriptableObjects` | 설정 및 데이터 에셋 | 코어 루프 제외 |
| `Assets/06.Packages/Tests/EditMode/CoreLoop` | 코어 루프·카드 효과·적 프로필·일반/엘리트/보스 정책과 반복 회귀 테스트 | EP-06 기준 179개 |
| `Assets/06.Packages/Tests/EditMode/StageProgression` | 진행·보상·상대 후보·선택 상태·프로필 전투 변환·호환 테스트 | EUI-01 기준 94개 |
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
| Unity 컴파일 | 자동 권총 처리기·기본 등록·전용 테스트 반영, 컴파일 오류 0 |
| 신규 테스트 | 자동 권총 경계 8/8 통과(job `5313dd71a905480d903e3bffe5d9c6a3`) |
| CoreLoop 대상 | 95/95 통과, 실패·건너뜀 0(job `6cd877a6354440c5b9a0d96e3949c459`) |
| 전체 EditMode | 125/125 통과, 실패·건너뜀 0(job `6a77fc8ed74f48659b541b7797d16774`) |
| Console | 테스트 기반 시설 메시지를 정리한 뒤 Error/Warning 0 |
| 씬·어셈블리·패키지·외부 에셋 변경 | 없음 |

정상 라운드에는 상대 비공개 카드가 한 장뿐이라는 규칙에 맞춰 다중 비공개 카드 기능은 만들지 않았다. 이번 단계는 모델·CoreLoop 세션 검증 범위이므로 Game View와 씬 직렬화 검증 대상이 아니다.

### 7.11 카드 사용 CU-04 검증

| 항목 | 결과 |
| --- | --- |
| MCP 연결 | `DiaBlackJack@5635a4cdcfecc8dd`, Unity 6000.3.10f1, 프로젝트 루트·활성 `StageTest` 씬 일치 |
| Unity 컴파일 | 수정 구슬·위협용 해머·군용 나이프 처리기와 카드 이동 경계 반영, 컴파일 오류 0 |
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
| 실제 런·Game View | `StageTest`→`CoreLoopTest` 동일 전투 연결, 수정 구슬·해머·자동 권총·군용 나이프 사용·선택·최근 결과·입력 해제 확인 |
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
| 실제 런 흐름 | Controller·Runtime 경로로 군용 나이프 최종 승리와 수정 구슬 버스트 패배, 양쪽 결과 화면과 재시작 뒤 새 전투·영혼·카드 초기화 확인 |
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
| 2026-07-19 | 이천서 | 카드 사용 CU-02 선택 대기·효과 완료·종료 원인 구조와 신규 16개·CoreLoop 87/87·전체 EditMode 117/117·Console 검증 결과 갱신 |
| 2026-07-19 | 이천서 | 카드 사용 CU-03 자동 권총 단일 비공개 카드 추측·정보 은닉 구조와 신규 8개·CoreLoop 95/95·전체 EditMode 125/125·Console 검증 결과 갱신 |
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
