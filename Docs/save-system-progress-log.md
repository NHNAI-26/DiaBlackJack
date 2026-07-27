# 게임 저장·이어하기 시스템 진행 기록

> 프로젝트: DiaBlackJack
> 기획·기록·구현 책임자: 이천서
> 작업 식별자: SV-00~SV-06
> 버전: v0.4
> 현재 단계: SV-03 현재 런 캡처·새 세션 복원 완료
> 다음 단계: SV-04 안정 체크포인트·결정적 재현
> 최종 갱신: 2026-07-27

## 1. 기록 원칙

- 기획 완료와 코드 구현 완료를 구분한다.
- 기존 파일이 있다는 이유만으로 저장 기능이 동작한다고 기록하지 않는다.
- 실행하지 않은 테스트와 Unity 화면을 통과로 기록하지 않는다.
- AI 대화 원문은 복사하지 않고 목적·제약·결정·산출물·검증으로 정리한다.
- 최종 규칙·구조·코드·화면 승인 책임자는 이천서로 기록한다.
- HONG의 예정 RF 구현은 실제 코드와 검증이 확인되기 전까지 완료 기여로 기록하지 않는다.
- 사용자의 명시적 요청 전에는 스테이징·커밋·푸시하지 않는다.

## 2. 현재 기준선

| 항목 | 현재 상태 |
| --- | --- |
| 저장 규칙 | `rule.md`에 안정 체크포인트·비저장 전투 상태·재현 정보 확정 |
| `Save` | 런 필드가 없는 빈 JSON 대상 |
| `SaveLoadSystem` | ScriptableObject에서 단일 파일 직접 저장·로드 |
| `FileManager` | `persistentDataPath` 직접 덮어쓰기, 백업·임시·버전 없음 |
| `PlayerRunState` | 영혼·일반/악마 덱 보유, SV-01 캡처용 마지막 발급 ID 내부 읽기 추가 |
| 시작 악마 선택 | 기본 악마 4장 즉시 생성, 새 규칙의 후보 2장·1장 확정 상태 미구현 |
| `RunProgress` | 스테이지·보상·런 승패 보유, 안정 상태 캡처 연결·복원 Factory 없음 |
| 순수 저장 경계 | 스키마 1 스냅샷·검증기·현행 안정 상태 캡처 구현 |
| `StageProgressionRuntime` | 실행 중 메모리 세션만 `DontDestroyOnLoad`로 유지 |
| 골드·상점·사건 | 기획 확정, 저장에 연결할 실제 전체 상태 API 미완료 |
| 이어하기 UI | 미구현 |
| 저장 파일 v1 | 미구현 |
| 외부 의존성 | 이번 단계 추가 없음 |

## 3. 단계 현황

| 단계 | 담당 | 상태 | 완료 증거 |
| --- | --- | --- | --- |
| SV-00 | 이천서(AI 문서·구조 대조 보조) | 완료 | 문서 4종·README·AI 활용·역할·구조 기록 |
| SV-01 | 이천서(AI 구현·검증 보조) | 완료 | 대상 7/7·StageProgression 141/141·전체 483/483·컴파일 오류 0 |
| SV-02 | 이천서(AI 구현·검증 보조) | 완료 | 대상 8/8·StageProgression 149/149·전체 491/491·컴파일 오류 0 |
| SV-03 | 이천서(AI 구현·검증 보조) | 완료 | 대상 7/7·StageProgression 156/156·전체 500/500·컴파일 오류 0 |
| SV-04 | 이천서(AI 구현·검증 보조) | 미착수 | 체크포인트·결정성 |
| SV-05 | 이천서(AI UI·검증 보조) | 미착수 | 새 게임·이어하기·실패 UI |
| SV-06 | 이천서(AI 통합·반복·기록 보조), HONG RF 상태 협업 | 미착수 | 골드·상점·사건·재실행 통합 |

## 4. SV-00 수행 기록

### 4.1 수행 내용

- `rule.md`의 시작 악마 선택, 전투 보상·골드, 상점 나가기, 사건 해결과 런 종료 체크포인트를 확인했다.
- 전투·계약·보상·상점·사건 선택 중에는 새 체크포인트를 만들지 않는 기준을 고정했다.
- `game-design-document`, 런·스테이지와 정식 런 문서의 재현성·팀 경계를 대조했다.
- 기존 `Save`, `SaveLoadSystem`, `FileManager`가 실제 런 상태·버전·백업·복원을 제공하지 않음을 기록했다.
- `PlayerRunState`, `RunProgress`, `StageProgressionRuntime`의 현행 상태 소유권을 확인했다.
- 시작 악마 선택 전 재추첨을 막기 위해 진행 체크포인트와 별도의 런 예약을 정의했다.
- 단일 자동 저장 슬롯, JSON 기본/백업/임시 파일, 저장 실패 시 진행 차단을 임시 기획으로 확정했다.
- 순수 스냅샷, 파일 저장소, 복원, 체크포인트, 메뉴와 RF 통합을 SV-01~SV-06으로 나눴다.

### 4.2 작성 파일

- `Docs/save-system-design.md`
- `Docs/save-system-development-spec.md`
- `Docs/save-system-implementation-plan.md`
- `Docs/save-system-progress-log.md`
- `Docs/README.md`
- `Docs/ai-usage-technical-document.md`
- `Docs/team-role-technical-document.md`
- `Docs/project-structure-and-mcp-reference.md`

### 4.3 직접 결정한 사항

| ID | 결정 | 이유 |
| --- | --- | --- |
| SV-D01 | 로컬 런 저장 1슬롯 | 프로토타입 UI·복구 경우 최소화 |
| SV-D02 | 자동 체크포인트만 제공 | 중간 저장을 이용한 선택 되돌리기 방지 |
| SV-D03 | 시작 악마 선택 전 런 예약 | 첫 체크포인트 전 후보 재추첨 방지 |
| SV-D04 | JSON 기본·백업·임시 파일 | 디버깅 가능성과 부분 쓰기 보호 |
| SV-D05 | 저장 성공 전 다음 콘텐츠 차단 | 메모리와 디스크 진행 불일치 방지 |
| SV-D06 | 런 종료 파일은 이어하기 불가 | 결과 확정과 진행 중 런 구분 |
| SV-D07 | 마지막 발급 카드 ID 별도 저장 | 제거된 최고 ID 재사용 방지 |
| SV-D08 | 루트 시드+종류별 순번 | `System.Random` 내부 상태 직렬화 방지 |

### 4.4 검증

| 검증 | 결과 |
| --- | --- |
| 기준 문서 | `rule.md`, 전체 기획, SP·RF·RW 문서 대조 완료 |
| 현재 코드 | SaveLoad 3개, `PlayerRunState`, `RunProgress`, Runtime 정적 확인 |
| 문서 간 ID | SV-00~SV-06 일치 |
| 구현 | 수행하지 않음 |
| 테스트·Unity | 수행하지 않음 |
| 코드·씬·프리팹·Packages | 변경 없음 |
| 외부 에셋·오픈소스 | 추가 없음 |

### 4.5 제외·의존성

- 현재 단계에서는 저장 런타임·테스트·씬을 만들지 않았다.
- HONG의 골드·상점 상태를 저장 시스템이 추측해서 구현하지 않았다.
- 사건 시스템이 없으므로 사건 저장을 구현된 기능으로 기록하지 않았다.
- 설정·클라우드·메타 성장·다중 슬롯은 범위 밖이다.

### 4.6 다음 단계

SV-01에서 UnityEngine·파일 I/O 없이 스키마 1의 순수 런 스냅샷과 검증기를 구현한다. RF 미구현 데이터는 임시 도메인 객체로 만들지 않고 스키마·계약에만 남긴다.

### 4.7 권장 커밋 메시지

```text
docs : 중도 종료가 선택 재추첨으로 이어지지 않게 저장 기준을 고정하다

Constraint: 전투·선택 중간 상태는 저장하지 않고 마지막 안정 체크포인트로 복귀
Rejected: 기존 빈 Save 클래스만 확장 | 버전·복구·순수 상태 경계가 불명확
Confidence: high
Scope-risk: narrow
Tested: 기준 문서·현재 SaveLoad와 런 상태 정적 대조
Not-tested: 저장 코드·Unity 실행
```

## 5. SV-01 수행 기록

### 5.1 수행 내용

- `RunSaveSnapshot`에 스키마·콘텐츠 리비전·저장 순번·런/스테이지·체크포인트·플레이어·난수·완료 이력을 불변 프로퍼티로 정의했다.
- 플레이어 스냅샷에 영혼·골드·일반/악마 덱·마지막 발급 ID·시작 악마 키를 포함했다.
- 일반 카드는 물리 ID·정의 키·무늬, 악마 카드는 물리 ID·정의 키만 보존하고 파생 표시 값은 저장하지 않았다.
- 생성자가 전달받은 카드·악마·상점·사건 목록을 방어 복사하도록 했다.
- `RunSaveValidator`가 스키마·콘텐츠 리비전·메타데이터·영혼·골드·카탈로그 키·무늬·중복 ID·마지막 발급 ID·시작 악마 소유·스테이지·생성 순번·체크포인트 조합을 타입화된 오류로 검증하게 했다.
- `RunSaveCapture`는 현행 코드에서 실제로 안정 상태인 `StageCleared`, `RunVictory`, `RunDefeat`만 받아들이고 `InBattle`, `RewardSelection` 등은 `UnstableState`로 거부하게 했다.
- `PlayerRunState`의 마지막 발급 일반/악마 카드 ID는 같은 런타임 어셈블리에서만 읽을 수 있도록 `internal` 프로퍼티로 노출했다.
- 아직 실제 상태가 없는 골드·시작 악마 선택·상점·사건·콘텐츠 생성 순번은 캡처에서 0·`null`·빈 목록으로 유지했으며 임시 도메인 타입을 만들지 않았다.

### 5.2 구현 파일

- `Assets/01. Scripts/StageProgression/Save/RunCheckpointKind.cs`
- `Assets/01. Scripts/StageProgression/Save/RunSaveSnapshot.cs`
- `Assets/01. Scripts/StageProgression/Save/RunSaveValidator.cs`
- `Assets/01. Scripts/StageProgression/Save/RunSaveCapture.cs`
- `Assets/01. Scripts/StageProgression/PlayerRunState.cs`
- `Assets/06.Packages/Tests/EditMode/StageProgression/RunSaveSnapshotTests.cs`
- 위 신규 폴더·스크립트의 Unity `.meta`

### 5.3 검증

| 검증 | 결과 |
| --- | --- |
| Unity 컴파일 | 성공, 테스트 실행 전 Console 오류 0건 |
| SV01-U01~U07 | 7/7 통과, 최종 MCP 작업 `9fc18bc720c743b3b7c45c1480cd206b` |
| StageProgression | 141/141 통과, MCP 작업 `26a321d927394f3c9296b642b5be941b` |
| 전체 EditMode | 483/483 통과, 최종 MCP 작업 `50c0a8eaf78c4e00a49ed20d38f3b73b` |
| 테스트 후 Console | Test Framework 사전·사후 처리와 결과 저장 경로 안내 3건, 게임 코드 실패 없음 |
| 순수성 | `StageProgression/Save`의 `UnityEngine`·파일 I/O 참조 0건 |
| 코드·씬·프리팹·Packages | 규칙 코드·테스트·문서만 변경, 씬·프리팹·Packages 무변경 |
| 외부 에셋·오픈소스 | 추가 없음 |

### 5.4 완료한 테스트 항목

| ID | 결과 |
| --- | --- |
| SV01-U01 | 영혼·일반/악마 덱·물리/마지막 발급 ID와 무늬 보존 |
| SV01-U02 | 일반·악마 덱의 중복 ID 거부 |
| SV01-U03 | 알 수 없는 일반/악마 정의 키와 무효 무늬 거부 |
| SV01-U04 | 현재 최대보다 작은 마지막 발급 ID 거부 |
| SV01-U05 | 체크포인트·다음 콘텐츠·런 상태 조합 검증 |
| SV01-U06 | 전투·보상 선택 중 캡처 거부 |
| SV01-U07 | 원본 목록 변경과 스냅샷 격리 |

### 5.5 제외·다음 단계

- JSON DTO·직렬화·실제 파일 쓰기·백업·복원은 구현하지 않았다.
- 시작 악마 선택·골드·상점·사건의 실제 상태가 생기기 전까지 해당 값을 캡처 완료로 간주하지 않는다.
- 다음 SV-02에서는 이 검증된 스냅샷을 필드 기반 v1 DTO로 변환하고 임시 파일 검증 뒤 기본·백업을 교체하는 저장소를 구현한다.

### 5.6 권장 커밋 메시지

```text
feat : 런 상태가 파일 형식과 섞이지 않게 저장 스냅샷 경계를 세우다

Constraint: 전투·보상 선택 중 상태와 미구현 RF 데이터를 저장하지 않음
Rejected: 빈 Save 클래스에 런 필드를 직접 추가 | 순수 상태와 Unity 파일 책임이 결합됨
Confidence: high
Scope-risk: narrow
Directive: 파일 DTO는 RunSaveSnapshot과 분리하고 검증을 통과한 값만 기록할 것
Tested: SV01 7/7, StageProgression 141/141, 전체 EditMode 483/483, 컴파일 오류 0
Not-tested: JSON 왕복·실제 파일 실패·백업 복구·프로세스 재실행
```

## 6. SV-02 수행 기록

### 6.1 수행 내용

- `RunSaveEnvelope` 계층을 Unity `JsonUtility`용 필드 DTO로 만들고 순수 `RunSaveSnapshot`과 분리했다.
- 체크포인트·런 상태·카드 무늬는 enum 정수 대신 `combat-settlement-completed`, `in-progress`, `spade` 같은 안정 문자열로 저장한다.
- `RunSaveSerializer`가 v1 전체 필드를 왕복하고 미래 스키마·콘텐츠 리비전 불일치·손상 JSON을 구분하게 했다.
- `IRunSaveFileStore`로 파일 시스템을 추상화하고 `SystemRunSaveFileStore`만 `Application.persistentDataPath`와 .NET 파일 API를 사용하게 했다.
- 저장 파일명은 `run-save.json`, `run-save.bak`, `run-save.tmp`로 고정했으며 절대 경로·하위 경로·경로 이동 문자열을 거부한다.
- 저장 시 임시 파일을 다시 읽어 역직렬화와 도메인 검증까지 통과한 뒤 기존 기본 파일을 백업으로 옮기고 교체한다.
- 마지막 임시→기본 이동이 실패하면 백업을 기본 파일로 되돌려 직전 정상 저장을 보존한다.
- 불러오기는 기본 파일을 먼저 검증하고 실패하면 백업을 사용하며 양쪽 손상·미지원 버전·콘텐츠 불일치·터미널 저장을 명시적 결과로 반환한다.
- 기존 `save.game`가 없거나 비어 있으면 `NoSave`로 처리하고, 레거시 `SaveLoadSystem` 자체는 변경하지 않았다.

### 6.2 구현 파일

- `Assets/01. Scripts/SaveLoad/RunSaveEnvelope.cs`
- `Assets/01. Scripts/SaveLoad/RunSaveSerializer.cs`
- `Assets/01. Scripts/SaveLoad/RunSaveResults.cs`
- `Assets/01. Scripts/SaveLoad/IRunSaveFileStore.cs`
- `Assets/01. Scripts/SaveLoad/SystemRunSaveFileStore.cs`
- `Assets/01. Scripts/SaveLoad/RunSaveRepository.cs`
- `Assets/06.Packages/Tests/EditMode/StageProgression/RunSaveRepositoryTests.cs`
- 위 신규 스크립트의 Unity `.meta`

### 6.3 검증

| 검증 | 결과 |
| --- | --- |
| Unity 컴파일 | 최초 미초기화 지역 변수 1건을 수정한 뒤 재컴파일 오류 0건 |
| SV02-U01~U08 | 8/8 통과, 최종 MCP 작업 `a77adba995914df8ba88ca9a89f4a137` |
| StageProgression | 149/149 통과, 최종 MCP 작업 `b76f39b70e80469eae011991e162d887` |
| 전체 EditMode | 491/491 통과, 최종 MCP 작업 `0bd47501216943e7b2ef4e8880720bd1` |
| 테스트 후 Console | Test Framework/MCP 사전·사후 처리·결과 저장 안내 10건, 컴파일·게임 코드 오류 0건 |
| 실패 원자성 | 메모리 파일 저장소에서 임시 쓰기 실패와 최종 교체 실패를 주입하고 이전 기본 파일 보존 확인 |
| 순수성 | `StageProgression/Save` 변경 없음·Unity 참조 0 유지, 파일 I/O는 `SaveLoad`에 한정 |
| 씬·프리팹·Packages | 변경 없음 |
| 외부 에셋·오픈소스 | 추가 없음 |

### 6.4 완료한 테스트 항목

| ID | 결과 |
| --- | --- |
| SV02-U01 | v1 JSON 전체 필드 왕복과 안정 문자열 코드 보존 |
| SV02-U02 | 임시 기록·재검증 뒤 기본→백업→기본 교체 순서 |
| SV02-U03 | 임시 쓰기 실패 시 이전 기본 파일 보존 |
| SV02-U04 | 최종 교체 실패 시 백업에서 이전 기본 파일 복원 |
| SV02-U05 | 손상된 기본 파일 대신 유효한 백업 불러오기 |
| SV02-U06 | 기본·백업 모두 손상 시 명시적 `Corrupted` |
| SV02-U07 | 미래 스키마와 콘텐츠 리비전 불일치 구분 |
| SV02-U08 | 빈 레거시 `save.game`를 `NoSave`로 분류 |

### 6.5 제외·다음 단계

- 실제 런 객체를 스냅샷에서 새 세션으로 복원하는 Factory와 Runtime 연결은 아직 없다.
- 실제 영구 데이터 경로의 프로세스 종료·재실행 검증은 Runtime과 UI가 연결되는 SV-05~SV-06에서 수행한다.
- 터미널 저장은 이어하기 불가로 분류하지만 결과 화면·메뉴 표시는 아직 연결하지 않았다.
- 다음 SV-03에서는 일반/악마 카드 물리 ID와 마지막 발급 ID, 영혼·스테이지 상태를 새 런 객체로 왕복 복원한다.

### 6.6 권장 커밋 메시지

```text
feat : 저장 실패가 마지막 정상 런을 지우지 않게 파일 교체를 보호하다

Constraint: 현재 스키마 1만 지원하며 Runtime 복원·메뉴 연결은 후속 단계
Rejected: 기본 파일 직접 덮어쓰기 | 임시 쓰기나 검증 실패가 마지막 정상 저장을 파괴함
Confidence: high
Scope-risk: narrow
Directive: 새 필드는 순수 스냅샷 검증과 안정 문자열 DTO를 함께 갱신할 것
Tested: SV02 8/8, StageProgression 149/149, 전체 EditMode 491/491, 컴파일 오류 0
Not-tested: 실제 프로세스 종료·재실행, Runtime 세션 복원, 메뉴 UI
```

## 7. SV-03 수행 기록

### 7.1 작업 정보

| 항목 | 내용 |
| --- | --- |
| 작업자 | 이천서(AI 구현·테스트·Unity MCP·기록 보조) |
| 수행일 | 2026-07-27 |
| 기준 커밋 | `61b6d08` |
| 변경 범위 | `PlayerRunState`, `RunProgress`, 제안 생성기·세션 순번, `RunSaveCapture`, `RunRestoreFactory`, `RunRestoreFactoryTests`, 저장·공통 기록 |

### 7.2 구현 내용

- `PlayerRunState.Restore` 내부 경계에서 영혼·일반/악마 덱·마지막 발급 ID를 함께 검증해 새 객체를 만든다.
- 현재 덱에서 제거된 최고 ID가 있어도 마지막 발급 ID 다음부터 새 일반·악마 카드를 발급한다.
- `RunProgress.Restore`는 현행 안정 상태인 `StageCleared`·`RunVictory`·`RunDefeat`와 저장된 스테이지 인덱스만 재구성한다.
- `RunSaveCapture.TryCapture(StageProgressionSession, ...)`이 현재 상대·보상 제안 생성 순번을 스냅샷에 기록한다.
- `RunRestoreFactory`는 주입된 경로 Factory로 루트 시드에 맞는 스테이지를 만들고, 저장된 순번만큼 상대·보상 생성기를 재생한다.
- 모든 검증과 객체 생성이 완료된 뒤에만 `RunRestoreResult`로 새 `StageProgressionSession`과 다음 콘텐츠·시작 악마 키를 반환한다.
- `PlayerRunState`가 아직 소유하지 않는 양수 골드는 손실시키지 않고 `InvalidGold`로 거부한다.

### 7.3 결정·원자성

- 상대 제안은 `rootSeed`, 보상 제안은 현행 Runtime의 시드 관계와 같은 `rootSeed + 1`로 재생한다.
- 복원 Factory는 기존 Runtime 세션을 입력으로 받거나 수정하지 않고 완성된 교체 후보만 반환한다.
- 최초 StageProgression 회귀에서 보상 제안 ID를 런 재시작마다 0으로 초기화하는 불필요한 변경이 기존 4개 반복 테스트를 깨뜨렸다. 해당 변경을 제거해 기존 증가 ID 계약을 보존했다.
- 새 세션 생성 전 검증·재생 어느 단계에서든 실패하면 `false`·`null`·타입화된 오류를 반환한다.

### 7.4 검증 결과

| 검증 | 결과 |
| --- | --- |
| SV03-I01~I07 | 7/7 통과, Unity MCP job `962d38bab0294705a97422ed6beaf0da` |
| StageProgression 회귀 | 156/156 통과, Unity MCP job `36f7082eb15d40079615550d0f1cbf02` |
| 전체 EditMode | 500/500 통과, Unity MCP job `6e5ee6c6a77249e48b29a4376515f3c6` |
| 컴파일 | 스크립트 전체 새로고침 후 최종 오류 0건 |
| Console | Test Framework 사전·사후 처리 경고 4건과 결과 저장 안내 2건, 게임 코드 오류 0건 |

### 7.5 제외·다음 단계

- Runtime에 실제 세션을 교체하는 `RunSaveCoordinator`·메뉴 연결은 SV-05 범위다.
- 시작 악마·상점·사건 안정 상태는 실제 소유 타입 구현 후 SV-04·SV-06에서 연결한다.
- 골드는 HONG의 RF `PlayerRunState` 구현 후 읽기 전용 계약으로 복원을 확장한다.
- `GameScene`·씬·프리팩·Packages·외부 에셋·오픈소스는 변경하지 않았다.

### 7.6 권장 커밋 메시지

```text
feat : 이어하기가 기존 세션을 반쯤 덮어쓰지 않게 런을 새로 복원하다

Constraint: 골드·시작 악마·상점·사건의 실제 런 상태는 후속 단계
Rejected: 기존 세션 필드별 덮어쓰기 | 중간 실패가 메모리 런을 반쯤 복원함
Confidence: high
Scope-risk: moderate
Directive: 복원 후보 전체 검증 전에 Runtime.Session을 교체하지 말 것
Tested: SV03 7/7, StageProgression 156/156, 전체 EditMode 500/500, 컴파일 오류 0
Not-tested: 실제 프로세스 종료·재실행, Runtime 세션 교체, 메뉴 UI, 양수 골드 복원
```

## 8. 결정 및 문제 대장

| ID | 항목 | 상태 | 결정·대응 | 재검토 조건 |
| --- | --- | --- | --- | --- |
| SV-D01 | 저장 슬롯 수 | 임시 확정 | 로컬 런 1슬롯 | 클라우드·프로필 도입 |
| SV-D02 | 저장 가능 시점 | 확정 | 다섯 안정 체크포인트만 | `rule.md` 변경 |
| SV-D03 | 시작 선택 전 종료 | 임시 확정 | 런 예약으로 같은 후보 복원 | 시작 흐름 구현 시 |
| SV-D04 | 파일 손상 | 임시 확정 | 기본 실패 시 백업 | 플랫폼 저장 API 도입 |
| SV-D05 | 저장 실패 후 진행 | 임시 확정 | 현재 안전 화면에서 차단·재시도 | UX 테스트 |
| SV-D06 | 터미널 저장 | 임시 확정 | 결과 유지, 이어하기 비활성 | 메타 통계 도입 |
| SV-D09 | 스냅샷 생성 경계 | 확정 | 내부 생성자·방어 복사·공개 읽기 전용 프로퍼티 | 스키마 버전 변경 |
| SV-D10 | 현행 캡처 허용 상태 | 확정 | `StageCleared`·`RunVictory`·`RunDefeat`만 허용 | 시작·RF·사건 상태 구현 |
| SV-D11 | 파일 이름 | 확정 | `run-save.json`·`run-save.bak`·`run-save.tmp`, 레거시 `save.game` | 플랫폼 저장 정책 변경 |
| SV-R01 | 기존 `Save`가 비어 있음 | 대응 완료 | 비어 있거나 없는 `save.game`는 `NoSave`, 비어 있지 않은 레거시는 지원하지 않음 | 레거시 마이그레이션 필요 시 |
| SV-R02 | 직접 덮어쓰기 | 해결 | 임시 재검증·기본→백업·임시→기본, 실패 시 백업 복원 | 플랫폼 원자 저장 API 도입 |
| SV-R03 | RF 실제 상태 없음 | 선행 필요 | 읽기 전용 Snapshot 계약만 정의 | HONG RF 구현 완료 |
| SV-R04 | 사건 시스템 없음 | 선행 필요 | 빈 사건 프레임워크를 만들지 않음 | 사건 작업 착수 |
| SV-R05 | 현재 생성기 상태 재현 | SV-03 현행 범위 해결 | 루트 시드·상대/보상 순번으로 재생 | 상점·사건 생성기 구현 시 |
| SV-R06 | 시작 악마 선택 상태 없음 | 선행 필요 | 현재 기본 악마 4장을 선택 결과로 간주하지 않음 | 시작 악마 선택 구현 완료 |

## 9. 계획 검증표

| 단계 | 대상 검증 | 전체 회귀 | 실제 파일 | 화면·프로세스 |
| --- | --- | --- | --- | --- |
| SV-00 | 문서·구조 대조 | 미실행 | 미실행 | 미실행 |
| SV-01 | 7/7 통과 | StageProgression 141/141·전체 483/483 | 없음 | 컴파일 오류 0·Test Framework 안내 3건 |
| SV-02 | 8/8 통과 | StageProgression 149/149·전체 491/491 | 메모리 실패 주입 | 컴파일 오류 0·기반 시설 안내 10건 |
| SV-03 | 7/7 통과 | StageProgression 156/156·전체 500/500 | 메모리 새 세션 후보 | 컴파일 오류 0·기반 시설 출력 6건 |
| SV-04 | 체크포인트 4개 이상 | 전체 EditMode | 임시/기본 | 없음 |
| SV-05 | 예약·메뉴·실패 3개 이상 | 전체 EditMode | 영구 경로 격리 | 두 해상도 |
| SV-06 | 반복·터미널·재실행 3개 이상 | 전체 EditMode | 실제 기본·백업 | 종료·재실행·Console |

## 10. 단계별 완료 기록 양식

```text
### SV-0N — 작업명

- 작업자:
- 날짜:
- 기준 커밋:
- 변경 파일:
- 구현 내용:
- 직접 결정·해결한 문제:
- 실패 원자성:
- AI 보조 범위:
- 대상 테스트 결과:
- 전체 회귀 결과:
- 파일·재실행 결과:
- 화면·Console 결과:
- 외부 에셋·오픈소스 변경:
- 제외·잔여 위험:
- 이천서 최종 검토:
- 권장 커밋 메시지:
```

## 11. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-27 | 이천서 | SV-03 일반/악마 덱·마지막 발급 ID·안정 스테이지·상대/보상 순번 캡처·새 세션 원자 복원과 대상 7/7·StageProgression 156/156·전체 500/500·컴파일 오류 0 완료 기록 |
| 2026-07-26 | 이천서 | SV-02 v1 JSON·안정 문자열·임시 재검증·원자 교체·백업 불러오기·명시적 실패 결과와 대상 8/8·StageProgression 149/149·전체 491/491·컴파일 오류 0 완료 기록 |
| 2026-07-26 | 이천서 | SV-01 스키마 1 불변 스냅샷·타입화된 검증·현행 안정 상태 캡처와 대상 7/7·StageProgression 141/141·전체 483/483·컴파일 오류 0 완료 기록, Test Framework 안내 3건 분리 |
| 2026-07-26 | 이천서 | SV-00 기준 문서·현재 코드 대조, 체크포인트·런 예약·파일 복구·결정성·팀 의존성 결정과 SV-01 착수 범위 기록 |
