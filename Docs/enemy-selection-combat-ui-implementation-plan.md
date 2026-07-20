# 상대 선택·적 전투 정보 UI 구현 계획서

> 프로젝트: DiaBlackJack  
> 계획·구현 책임자: 이천서  
> 작업 식별자: EUI-00~EUI-05  
> 버전: v0.1  
> 현재 단계: EUI-04 적 전투 정보 UI 완료
> 다음 단계: EUI-05 반복 회귀·씬·화면·문서 마감
> 최종 갱신: 2026-07-20

## 1. 계획 목적

EP-06에서 완성한 프로필 키→실제 전투·보상 경계를 후보 생성, 선택 화면과 전투 정보 UI에 단계적으로 연결한다. 각 단계는 문서·코드·테스트·Unity 검증을 함께 완료하고 다음 단계로 넘어간다.

## 2. 운영 원칙

- 한 단계의 상태·테스트가 통과한 뒤 다음 단계 UI를 구현한다.
- 기존 고정 스테이지와 독립 `CoreLoopTest` 호환을 먼저 잠근다.
- 후보·추론 데이터는 카탈로그와 공개 정보 계산을 재사용한다.
- UI가 정책 결정을 추가 실행하거나 전투 상태를 직접 변경하지 않는다.
- 상대 선택·전투 UI는 이천서가 단일 소유한다.
- HONG의 랜덤 이벤트·정식 런 진행 파일과 Shim0Hwan의 아트 파일을 수정하지 않는다.
- 씬 파일은 스크립트 확장으로 해결할 수 없을 때만 한 단계에서 단독 수정한다.
- 사용자가 요청하기 전에는 스테이징·커밋·푸시하지 않는다.
- 각 단계 완료 시 한국어 커밋 제목을 추천한다.

## 3. 단계 현황

| 단계 | 목표 | 상태 | 완료 증거 |
| --- | --- | --- | --- |
| EUI-00 | 기획·개발 명세·계획·기록과 담당 변경 | 완료 | 문서 4종·README·AI·역할 기록 정적 검토 |
| EUI-01 | 후보 생성·선택 대기 상태 기반 | 완료 | 신규 13/13·StageProgression 94/94·전체 EditMode 273/273 |
| EUI-02 | 후보 비교·지정·확정 화면 | 완료 | 신규 9/9·StageProgression 103/103·전체 282/282·두 해상도 화면 |
| EUI-03 | 선택 키의 실제 전투·보상 통합 | 완료 | 신규 14/14·StageProgression 117/117·전체 296/296·실제 씬 전환 |
| EUI-04 | 일반·엘리트·보스 전투 정보 표시 | 완료 | 신규 14/14·CoreLoop 193/193·StageProgression 117/117·전체 310/310·두 해상도 화면 |
| EUI-05 | 반복 회귀·씬·화면·문서 마감 | 대기 | 신규 최소 5개·전체 EditMode·실제 런·씬·Console |

## 4. EUI-00 — 문서와 담당 경계

### 목표

코드 수정 전에 상대 선택·전투 정보 UI의 플레이 흐름, 임시 기획 결정, API, 파일 소유권과 검증 기준을 확정한다.

### 완료 작업

- `rule.md`, 전체 기획, 진행·프로필 문서와 현재 코드 대조
- 후보 2명·엘리트 최대 1명·보스 제외 규칙 확정
- 엘리트 제안 확률 35%, 후보 재추첨 없음, 별도 확정 입력을 임시 결정으로 기록
- 일반·엘리트·보스별 공개 정보 수준 확정
- 선택 상태·OfferId·ActiveStage·표시 스냅샷 기술 구조 확정
- 이천서는 상대 선택·전투 UI, HONG은 랜덤 이벤트·정식 런 진행으로 역할 변경
- AI 활용·외부 에셋 미사용·문서 책임자 이천서 기록

### 완료 게이트

- 문서 4종의 단계명·상태·API·테스트 ID가 일치한다.
- 구현 완료로 잘못 기록한 EUI 코드가 없다.
- 기존 전체 EditMode 260/260을 착수 기준으로 기록한다.

### 추천 커밋 제목

`docs : 상대 선택과 적 전투 정보 UI의 구현 기준을 확정`

## 5. EUI-01 — 후보 생성·선택 상태 기반

### 목표

UI 없이 결정적 후보 2명과 `OpponentSelection` 상태를 구현하고 기존 고정 전투 호환을 보장한다.

### 작업

1. `OpponentSelectionCandidate`, `OpponentSelectionOffer` 불변 타입을 추가한다.
2. `OpponentSelectionGenerator`에 시드와 엘리트 확률을 주입한다.
3. 일반 3종·엘리트 1종을 분리하고 보스를 제외한다.
4. 0%·100%·기본 35% 후보 조합을 구현한다.
5. `StageProgressionState.OpponentSelection`을 추가한다.
6. `StageProgressionSession`에 선택 기능 주입, Pending Offer와 ActiveStage 읽기 경계를 추가한다.
7. 시작·다음 스테이지·재시작·보스 우회 상태를 연결한다.
8. 선택 상태에서 모든 기존 전투·보상 입력을 거절한다.
9. 기존 기능 미주입 세션 테스트를 유지한다.

### 변경 예상 파일

- 신규 `Runtime/StageProgression/OpponentSelection/*.cs`
- `StageProgressionState.cs`
- `StageProgressionSession.cs`
- `StageProgressionRuntime.cs`
- 신규 `OpponentSelectionFoundationTests.cs`
- 기존 진행 상태·세션 테스트 최소 갱신

실제 구현에서는 선택 화면이 없는 상태에서 프로토타입 런을 선택 모드로 켜면 기존 Controller가 전투 씬을 먼저 여는 문제가 생기므로 `StageProgressionRuntime`은 수정하지 않았다. 선택 기능은 세션 생성자에 선택적으로 주입하며, 전용 테스트에서만 활성화했다. 기존 진행 테스트를 수정하지 않고 신규 테스트로 호환을 고정했다.

### 완료 게이트

- EUI01 시나리오 최소 10개 통과
- 같은 시드 결정성, 후보 2명·중복 없음·엘리트 최대 1명
- 보스 선택 우회와 고정 진행 호환
- 선택 상태에서 Battle null·기존 행동 거절
- StageProgression·전체 EditMode 회귀 통과
- 컴파일·Console Error 0

### 추천 커밋 제목

`feat : 중복 없는 상대 후보와 선택 대기 상태를 구현`

## 6. EUI-02 — 상대 선택 화면

### 목표

기존 `StageTest`에서 후보의 위험·성향·보상을 비교하고 오입력 없이 한 명을 확정할 수 있게 한다.

### 작업

1. `OpponentCandidateViewModel`과 StageProgression ViewModel 선택 필드를 추가한다.
2. Presenter가 세션 Pending Offer와 집중 키를 읽도록 확장한다.
3. 이름·등급·영혼·요약·예상 보상 문자열을 매핑한다.
4. `StageProgressionView`에 후보 카드 2개와 선택 강조·확정 버튼을 그린다.
5. Controller가 집중 키를 소유하고 후보 클릭은 화면 상태만 변경하게 한다.
6. 확정 전·처리 중·실패 후 버튼 활성 상태를 연결한다.
7. 시작·다음 스테이지가 선택 상태면 씬을 바꾸지 않고 화면을 갱신한다.
8. 1280×720·1920×1080 화면을 확인한다.

### 변경 예상 파일

- `StageProgressionPresentation.cs`
- `StageProgressionView.cs`
- `StageProgressionController.cs`
- 신규 `OpponentSelectionPresentationTests.cs`
- 필요 시 기존 `StageProgressionPresentationTests.cs`

### 완료 게이트

- EUI02 시나리오 최소 7개 통과
- 후보 정보가 미리보기와 일치
- 클릭만으로 세션·진행 상태가 변하지 않음
- 집중 후보만 확정 가능
- 중복 입력·제안 외 키 잠금
- 두 해상도에서 겹침·잘림 없음
- 씬 파일 변경 없이 실제 선택 화면 표시

### 실제 완료 결과

- `OpponentCandidateViewModel`과 선택 제안·집중 키를 받는 Presenter 오버로드를 추가했다.
- 후보 2명의 이름·등급·최대 영혼·성향 요약·예상 보상을 표시하고, 유효한 키 하나만 집중·확정 가능 상태로 만든다.
- Controller의 로컬 집중은 세션·진행·전투를 바꾸지 않으며 제안 밖 키와 대소문자 불일치를 무시한다.
- 프로토타입 Runtime이 선택 생성기를 주입하고 시작 성공이 `OpponentSelection`이면 `StageTest`에 남아 화면만 갱신한다.
- 확정 버튼의 실제 `TrySelectOpponent` 처리와 `CoreLoopTest` 이동은 계획대로 EUI-03에 남겼다.
- 신규 9/9, StageProgression 103/103, CoreLoop 179/179, 전체 EditMode 282/282와 1280×720·1920×1080 화면을 검증했다.

### 추천 커밋 제목

`feat : 상대의 위험과 보상을 비교해 선택할 수 있는 화면을 구현`

## 7. EUI-03 — 실제 전투·보상 통합

### 목표

확정된 후보 키를 실제 프로필 전투로 원자적으로 변환하고 두 번의 선택 전투와 고정 보스를 진행한다.

### 작업

1. `TrySelectOpponent(offerId, profileKey)`를 구현한다.
2. 현재 스테이지 템플릿 ID·시드와 선택 프로필로 해석 완료 `StageDefinition`을 만든다.
3. 전투 생성 성공 뒤에만 ActiveStage·Battle·상태를 교체한다.
4. 선택 성공 시 `CoreLoopTest`로 이동한다.
5. 전투 승리·보상 뒤 다음 일반 스테이지에서 새 Offer를 생성한다.
6. 두 번째 전투 보상 뒤 최종 보스는 선택 없이 고정 프로필로 시작한다.
7. 플레이어 영혼·런 덱·보상 카드가 선택 화면 왕복에서도 유지되는지 확인한다.
8. 오래된 OfferId·후보 외 키·보스 키의 실패 원자성을 검증한다.

### 변경 예상 파일

- `StageProgressionSession.cs`
- `StageProgressionController.cs`
- `StageProgressionPresentation.cs`
- `StageProgressionRuntime.cs`
- 신규 `OpponentSelectionIntegrationTests.cs`

### 완료 게이트

- EUI03 시나리오 최소 8개 통과
- 일반 3종·엘리트 각각 실제 영혼·덱 10장·정책·보상 일치
- 확정 전 Battle 없음, 성공 뒤 Pending 없음
- 첫 전투 보상→두 번째 선택에서 영혼·덱 유지
- 두 번째 보상→최종 보스 직접 진입
- 실패 입력에서 진행·제안·플레이어 상태 무변경
- 실제 선택→전투→보상 흐름 확인

### 실제 완료 결과

- `TrySelectOpponent(offerId, profileKey)`가 현재 상태·제안 ID·스테이지 인덱스·정확한 후보 키를 검증한다.
- 템플릿 ID·종류·양쪽 덱 시드와 후보 미리보기 이름·프로필 키로 해석 완료 스테이지와 시작된 전투를 먼저 준비하고 성공 뒤에만 상태를 교체한다.
- 오래된 ID·후보 밖 키·대소문자 불일치·빈 키는 false, 전투 Factory 예외는 상태 무변경으로 전파된다.
- Controller 확정 이벤트가 성공 시 집중을 비우고 `CoreLoopTest`, 실패 시 같은 선택 화면으로 복귀한다.
- 일반 3종·엘리트 1종의 실제 영혼·10장 덱·정책과 일반/높은 등급 보상을 검증했다.
- 첫 보상 뒤 두 번째 선택에서 영혼·런 덱을 유지하고, 두 번째 보상 뒤 고정 보스에 직접 진입하며, 보스 승리 뒤 재시작은 OfferId 0·초기 덱으로 복구한다.
- 신규 14/14, StageProgression 117/117, CoreLoop 179/179, 전체 EditMode 296/296와 실제 집행관 선택→`CoreLoopTest` 전환을 검증했다.

### 추천 커밋 제목

`feat : 선택한 상대를 실제 스테이지 전투와 보상으로 연결`

## 8. EUI-04 — 적 전투 정보 UI

### 목표

전투 중 적 프로필과 AI가 실제 사용하는 공개 추론을 등급에 맞는 정보량으로 보여 준다.

### 작업

1. 공개 숫자 추론 생성을 관측과 표시가 공유하는 내부 함수로 정리한다.
2. `EnemyInferenceDisplayEntry`, `EnemyCombatDisplaySnapshot`과 Factory를 추가한다.
3. 일반 적 상위 3개 정수 확률 표시를 구현한다.
4. 집행관 상위 2개 숫자·신뢰도 표시를 기존 모델과 연결한다.
5. 보스 구간·추론 방향·신뢰도·강행동 예고를 기존 정책 상태와 연결한다.
6. 프로필 없는 독립 전투는 명시적인 정보 없음 상태로 유지한다.
7. `CoreLoopPresenter`에 프로필 키를 전달하고 ViewModel 문자열을 만든다.
8. `CoreLoopView`에 적 이름·등급·성향·전략 정보·경고 패널을 추가한다.
9. UI 갱신을 위해 정책 `Decide`를 추가 호출하지 않는지 테스트한다.
10. 일반·엘리트·보스 실제 화면을 두 해상도에서 확인한다.

### 변경 예상 파일

- 신규 `CoreLoop/EnemyAI/EnemyCombatDisplay*.cs`
- `EnemyObservationFactory.cs` 또는 공통 추론 헬퍼
- `EnemyInferenceDisplayModel.cs`
- `FinalBossEnemyPolicy.cs`는 공개 표시 접근에 필요한 최소 변경만 허용
- `CoreLoopPresentation.cs`
- `CoreLoopView.cs`
- `CoreLoopController.cs`
- 신규 `EnemyCombatPresentationTests.cs`

### 완료 게이트

- EUI04 시나리오 최소 10개 통과
- 일반·엘리트·보스 정보량 규칙 준수
- 정책 추론과 UI 표시 값 일치
- 보스 예고 생성·해제 화면 동기화
- 비공개 값·덱 순서·카드 ID·정확한 다음 행동 미노출
- 기존 카드·행동 버튼과 패널 겹침 없음
- 프로필 없는 독립 전투 회귀 유지

### 실제 구현 결과

- 공개 숫자 추론 계산을 `EnemyObservationFactory` 내부 경계로 모아 적 정책 관측과 UI가 같은 결과를 사용한다.
- 새 안전 스냅샷 Factory는 일반 상위 3개 확률, 엘리트 상위 2개·신뢰도, 보스 구간·방향·신뢰도·예고만 제공하며 비공개 전투 객체를 보유하지 않는다.
- Presenter는 정책을 실행하지 않고 표시 문자열만 만들며, Controller가 진행 전투의 프로필 키를 전달한다.
- 기존 IMGUI 정보 패널을 확장하고 720p 반응형 글꼴·여백·버튼 높이를 적용해 카드 효과 설명까지 화면 안에 유지했다.
- 신규 14/14, CoreLoop 193/193, StageProgression 117/117, 전체 EditMode 310/310을 통과했다.
- 일반·엘리트·보스 경고 상태를 1280×720과 1920×1080에서 확인했으며 씬·프리팹·Packages·외부 에셋은 변경하지 않았다.

### 추천 커밋 제목

`feat : 적 등급에 맞는 추론 정보와 보스 예고를 전투 화면에 표시`

## 9. EUI-05 — 반복 회귀와 마감

### 목표

선택→전투→보상→다음 선택→보스→재시작 전체 흐름과 UI 상태 격리를 반복 검증하고 문서를 실제 결과로 마감한다.

### 작업

1. 일반+일반, 일반+엘리트의 양쪽 후보 선택 흐름을 각각 반복한다.
2. 두 번의 선택 전투와 고정 보스·보상·재시작을 10회 반복한다.
3. 오래된 제안·중복 확정·씬 왕복·집중 키·예고 상태 누출을 반복 검사한다.
4. CoreLoop, StageProgression, 카드 사용, 전투 보상과 전체 EditMode를 실행한다.
5. 1280×720·1920×1080 선택·일반·엘리트·보스 화면을 확인한다.
6. `StageTest`, `CoreLoopTest` 씬과 최종 Console을 검증한다.
7. 문서 4종, README, 프로젝트 구조, AI 활용과 이천서 역할 기록을 마감한다.

### 완료 게이트

- EUI05 시나리오 최소 5개와 모든 관련 회귀 통과
- 후보·OfferId·집중 키·ActiveStage·전투·보상·예고 상태 누출 없음
- 실제 전체 흐름 10회 성공
- 양쪽 해상도에서 입력·텍스트·패널 문제 없음
- 양쪽 씬 문제 0, 최종 Console Error/Warning 0
- 패키지·외부 에셋 무변경 확인
- 계획과 실제 결과 차이 기록

### 추천 커밋 제목

`test : 상대 선택부터 전투 보상까지 반복 검증해 UI 연동을 마감`

## 10. 의존 관계

```text
EP-06 프로필→전투·보상 경계 완료
  → EUI-00 문서·담당
  → EUI-01 후보·상태
  → EUI-02 선택 화면
  → EUI-03 실제 전투 통합
  → EUI-04 전투 정보 UI
  → EUI-05 전체 검증

HONG 랜덤 이벤트·정식 런 진행
  → EUI 공개 상태·TrySelectOpponent 계약만 소비
  → EUI 내부 후보·정책·표시 계산은 수정하지 않음
```

## 11. 파일 소유권

| 경로 | 주 담당 | 규칙 |
| --- | --- | --- |
| `Runtime/StageProgression/OpponentSelection` | 이천서 | 후보·제안·결정성 |
| `Runtime/CoreLoop/EnemyAI/*Display*` | 이천서 | 공정한 표시 스냅샷 |
| `Runtime/UI/StageProgression` | 이천서 | EUI 단계 동안 단일 수정 |
| `Runtime/UI/CoreLoop` | 이천서 | EUI 전투 정보·기존 행동 UI 호환 |
| `StageProgressionSession.cs` | 이천서 | 선택·전투 원자성, EUI 단계 동시 수정 금지 |
| `StageTest.unity`, `CoreLoopTest.unity` | 이천서(필요 시) | 스크립트로 불가능할 때만 수정, 자동 병합 금지 |
| HONG의 이벤트·런 흐름 신규 경로 | HONG | EUI 공개 계약만 호출 |
| `Assets/05. Arts`, `LevelDesign.unity` | Shim0Hwan | EUI 코드 작업에서 수정 금지 |

## 12. 주요 위험과 대응

| 위험 | 영향 | 대응 |
| --- | --- | --- |
| 선택 전 상태가 `InBattle`로 보임 | 빈 전투·입력 오작동 | 명시적 `OpponentSelection` 상태 |
| 후보 화면 값과 실제 적 불일치 | 선택 신뢰 붕괴 | 미리보기·전투 모두 같은 ProfileKey 사용 |
| 인덱스로 선택 | 순서 변경·오입력 | OfferId+ProfileKey 검증 |
| 확정 전 상태 변경 | 취소 불가·중복 생성 | 집중과 세션 확정 분리 |
| UI가 정책을 추가 호출 | 보스 예고 상태 오염 | 읽기 전용 표시 Factory, Decide 호출 금지 |
| 비공개 값 노출 | AI 공정성 훼손 | 안전 스냅샷 타입과 정보 누출 테스트 |
| 진행 UI 파일 동시 수정 | 병합 충돌 | EUI 동안 이천서 단일 소유 |
| 전체 런 이벤트까지 범위 확장 | 일정 초과 | HONG 경계 유지, EUI는 전투 선택만 담당 |
| IMGUI 공간 부족 | 버튼·정보 겹침 | 두 해상도 실제 검증, 정보량 등급별 제한 |

## 13. 커밋 원칙

- 단계별 코드·테스트·문서 기록을 함께 커밋한다.
- 커밋 제목은 해결한 사용자 목적을 한국어로 작성한다.
- Lore 본문에는 제약·거절 대안·검증·미검증을 기록한다.
- 신규 C#과 대응 `.meta`를 함께 관리한다.
- 아트·이벤트·패키지 변경을 EUI 커밋에 섞지 않는다.
- 사용자가 요청하기 전에는 스테이징하거나 커밋하지 않는다.

## 14. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-20 | 이천서 | EUI-00~EUI-05를 문서, 후보·상태, 선택 화면, 실제 전투 통합, 전투 정보 UI, 반복 검증 단계로 분리하고 담당·파일 소유권·완료 게이트·한국어 커밋 제목을 확정 |
| 2026-07-20 | 이천서 | EUI-01 결정적 후보·불변 제안·선택 상태·세션 주입을 구현하고 신규 13/13·StageProgression 94/94·CoreLoop 179/179·전체 EditMode 273/273으로 완료 처리 |
| 2026-07-20 | 이천서 | EUI-02 후보 비교·로컬 집중·확정 가능 상태·선택 상태 씬 이동 차단을 구현하고 신규 9/9·StageProgression 103/103·CoreLoop 179/179·전체 EditMode 282/282·두 해상도 화면으로 완료 처리 |
| 2026-07-20 | 이천서 | EUI-03 선택 키의 원자적 실제 전투·보상 변환, 두 번째 선택·고정 보스·재시작을 구현하고 신규 14/14·StageProgression 117/117·CoreLoop 179/179·전체 EditMode 296/296·실제 씬 전환으로 완료 처리 |
| 2026-07-20 | 이천서 | EUI-04 등급별 안전 표시 스냅샷·공유 추론·전투 정보 패널과 720p 반응형 배치를 구현하고 신규 14/14·CoreLoop 193/193·StageProgression 117/117·전체 EditMode 310/310·두 해상도 화면으로 완료 처리 |
