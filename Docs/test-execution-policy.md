# 세션 단위 테스트 실행 정책

## 목적

EditMode 회귀 테스트 1,074개를 삭제하지 않고 보존한다. 구현 중에는 이번 작업과 관련된 테스트만 실행해 반복 시간을 줄인다. 전체 회귀는 넓은 변경과 마감 관문에서만 실행한다.

2026-08-05 기준 구성은 테스트 파일 84개다.

- CoreLoop: 52개 파일, `DiaBlackJack.CoreLoop.Tests.EditMode`
- StageProgression: 31개 파일, `DiaBlackJack.StageProgression.Tests.EditMode`
- Settings: 1개 파일, `DiaBlackJack.Settings.Tests.EditMode`

실제 테스트 케이스 수는 매 세션 `mcpforunity://tests`의 `totalCount`로 확인한다. 매개변수화 테스트 때문에 파일 수나 `[Test]` 특성 수와 같지 않다.

## 실행 단계

### 1. 집중 실행 — 기본

구현 반복마다 아래만 실행한다.

- 이번 세션에서 추가하거나 수정한 테스트
- 같은 상태 전이 또는 같은 공개 API를 검증하는 인접 테스트 클래스
- 버그 수정이면 재현 테스트와 직접 회귀 테스트

Unity MCP 선택 기준은 작은 것부터 `test_names`, `group_names`, `category_names` 순서로 사용한다. 필터 없는 `run_tests`는 기본 실행이 아니다.

신규 테스트는 기존 메서드명 작업 ID 접두사를 유지하고 NUnit 범주도 붙인다.

```csharp
[TestFixture]
[Category("RF01")]
public sealed class FormalRunGoldRewardTests
{
    // RF01_U01_...
}
```

한 클래스에 여러 작업 ID가 섞이면 메서드에 `Category`를 붙인다. 범주 문자열은 메서드 접두사의 첫 구간과 같게 쓰며 하이픈은 제외한다. 예: `RF01`, `CUM17`, `DCUI08`.

### 2. 영향 어셈블리 실행 — 공용 경계 변경

다음 변경은 영향받은 테스트 어셈블리만 한 번 실행한다.

- 상태 머신, 세션 오케스트레이션, 전투 참가자 공용 상태
- 카드·적·보상·상점 카탈로그와 팩토리
- 저장 스냅샷 또는 복원 계약
- 여러 테스트 클래스가 공유하는 Presenter 모델

폴더 기준 기본 선택:

- `CoreLoop/`, 전투 `GameScene/`, 전투 HUD: CoreLoop
- `StageProgression/`, `Bootstrap/`, 정식 런·보상·상점·저장: StageProgression
- `Settings/`: Settings
- 두 영역의 계약을 함께 바꾸면 두 어셈블리만 실행

### 3. 전체 EditMode 실행 — 마감 관문

아래 경우에만 전체를 실행한다.

- 릴리스, 병합, 배포 전 검증
- 두 개 이상 테스트 어셈블리에 걸친 공용 계약 변경
- `asmdef`, 패키지, 컴파일 정의, 테스트 기반 시설 변경
- 결정성 RNG, 핵심 덱 순환, 전역 카탈로그처럼 파급 범위를 좁히기 어려운 변경
- 사용자가 전체 회귀를 요청한 경우
- 기능 구현 계획이 전체 관문을 명시한 경우

집중 테스트가 통과한 뒤 전체를 한 번만 실행한다. 단순 문서, 국소 표시 문자열, 에디터 미리보기 변경은 관련 테스트·컴파일·Console·필요한 화면 검증으로 끝낸다.

## 결과 기록

아래 셋을 구분해 기록한다.

- 집중: 실행한 작업 ID, 테스트명 또는 클래스와 통과 수
- 영향 범위: 실행한 어셈블리와 통과 수
- 전체: 실제로 필터 없는 EditMode 전체를 실행했을 때만 전체 회귀로 표기

기존 실패가 있으면 이번 변경으로 새로 생긴 실패인지 재현해 분리한다. 실행하지 않은 범위를 통과로 기록하지 않는다.
