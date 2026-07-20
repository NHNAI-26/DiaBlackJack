# 정식 런 진행 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·통합 책임자: 이천서  
> 구현 예정 담당자: HONG  
> 작업 식별자: RF-00~RF-05  
> 버전: v0.1  
> 상태: RF-01 착수 가능, 구현 미착수  
> 최종 갱신: 2026-07-20

## 1. 기술 목표

완료된 `StageProgressionSession`의 전투 2회·보스 1회 구조를 보존하면서 일반 전투의 카드 보상 뒤에 골드 정산과 상점 게이트를 삽입한다.

```text
FormalRunSession
├─ StageProgressionSession : 상대 선택·전투·카드 보상·전투 스테이지
├─ GoldRewardCatalog       : 선택된 적 프로필별 승리 골드
└─ ShopVisit               : 구매·제거·휴식·나가기
```

기존 전투 세션은 전투 세부 규칙을 계속 소유한다. 정식 런 세션은 보상 입력을 감싸 골드를 지급하고, `StageCleared`에서 곧바로 다음 전투로 가지 않게 상점을 연다.

## 2. 현재 기준선

- `RunProgress`는 일반 전투 2개와 마지막 보스 1개를 소유한다.
- `StageProgressionSession`은 상대 선택, 실제 전투, 카드 보상과 런 승패를 소유한다.
- 일반 전투 보상 완료는 `StageCleared`, 보스 보상 완료는 `RunVictory`다.
- `TryAdvanceToNextStage()`는 `StageCleared`에서 다음 상대 선택 또는 고정 보스를 만든다.
- `PlayerRunState`는 영혼과 런 덱을 소유하지만 골드는 아직 없다.
- EUI-05 기록 기준 전체 EditMode 315/315다. RF-00은 문서 전용이므로 재실행 결과로 주장하지 않는다.

## 3. 파일 구조와 소유권

| 경로 | 역할 | 담당 |
| --- | --- | --- |
| `Assets/01. Scripts/Runtime/StageProgression/RunFlow` | 골드 정산과 정식 순서 조정 | HONG |
| `Assets/01. Scripts/Runtime/StageProgression/Shop` | 상점 제안·방문·거래 | HONG |
| `Assets/01. Scripts/Runtime/StageProgression/PlayerRunState.cs` | 골드와 카드 제거 최소 확장 | HONG, 이천서 검토 |
| `Assets/01. Scripts/Runtime/UI/StageProgression` | Runtime·Presenter·View·Controller 연결 | HONG, 이천서 검토 |
| `Assets/06.Packages/Tests/EditMode/StageProgression/RunFlow` | RF 단위·통합·화면·반복 테스트 | HONG |

새 asmdef, 패키지, 전용 씬과 외부 에셋은 추가하지 않는다.

## 4. `PlayerRunState` 확장

기존 3인자 생성자는 골드 0으로 시작하는 현재 호환을 유지한다. 골드를 지정하는 새 생성 경로만 추가한다.

```csharp
public int CurrentGold { get; }
internal void AddGold(int amount);
internal bool TrySpendGold(int amount);
internal bool TryRemoveCard(int cardId);
```

불변 조건:

- 골드는 0 이상이며 오버플로를 허용하지 않는다.
- 음수 지급·소비는 거부한다.
- 소비 실패는 골드를 바꾸지 않는다.
- 덱이 1장일 때 제거를 거부한다.
- 제거한 물리 카드 ID를 같은 런에서 재사용하지 않는다.
- 재시작하면 최초 골드와 최초 덱으로 복구한다. 프로토타입 최초 골드는 0이다.

## 5. 골드 보상 계약

### 5.1 `GoldRewardCatalog`

```csharp
public sealed class GoldRewardCatalog
{
    public static GoldRewardCatalog CreatePrototype();
    public int GetAmount(string profileKey);
}
```

- 키는 `gunslinger`, `cultist`, `trickster`, `enforcer`, `final-boss`다.
- 지급량은 각각 3, 3, 4, 6, 10이다.
- 중복·빈 키·0 이하 금액은 카탈로그 생성 시 거부한다.
- 알 수 없는 프로필은 0으로 대체하지 않고 명시적으로 실패한다.
- 골드는 UI 표시 이름이나 스테이지 번호가 아니라 `ActiveStage.BattleProfileKey`로 조회한다.

### 5.2 정산 시점

`FormalRunSession.TrySelectBattleReward` 또는 `TrySkipBattleReward`가 기존 세션에서 성공한 뒤 한 번 정산한다.

- 일반전: 골드 추가 → 상점 생성 → 바깥 단계 `Shop`
- 보스: 골드 추가 → 기존 `RunVictory` 유지 → 상점 없음
- 실패 입력: 골드·보상·단계 변화 없음

정식 런 UI는 기존 보상 API를 직접 호출하지 않는다. 직접 호출하면 골드 정산을 우회하므로 반드시 바깥 세션을 사용한다.

## 6. 상점 데이터 계약

### 6.1 `ShopCardOption`

| 속성 | 규칙 |
| --- | --- |
| `OptionId` | 제안 안에서 고유, 0 이상 |
| `DefinitionKey` | 기존 일반 보상 카탈로그의 유효한 카드 |
| `Price` | RF 프로토타입 4 |

### 6.2 `ShopOffer`

| 속성 | 규칙 |
| --- | --- |
| `OfferId` | 생성기 인스턴스에서 0부터 증가 |
| `VisitIndex` | 0 또는 1 |
| `CardOptions` | 서로 다른 정의 3개 |
| `RestPrice/Recovery` | 3 / 3 |
| `RemovalPrice` | 3 |

제안은 불변이다. 화면 재진입과 거래 뒤에도 카드 후보·가격은 바뀌지 않는다.

### 6.3 `ShopVisit`

```csharp
public sealed class ShopVisit
{
    public ShopOffer Offer { get; }
    public bool HasPurchasedCard { get; }
    public bool HasRemovedCard { get; }
    public bool HasRested { get; }
    public bool IsClosed { get; }
    public ShopTransaction LastTransaction { get; }

    public bool TryBuyCard(int offerId, int optionId, PlayerRunState player);
    public bool TryRemoveCard(int offerId, int cardId, PlayerRunState player);
    public bool TryRest(int offerId, PlayerRunState player);
    public bool TryClose(int offerId);
}
```

거래 순서는 `제안·ID·방문 상태 검증 → 대상·골드 검증 → 결과 계산 → 비용과 효과 적용 → 사용 플래그·결과 기록`이다.

RF 범위는 같은 런타임 어셈블리 안에서 골드 소비와 카드 변경을 처리한다. 비용 소비 뒤 효과 적용이 실패할 수 없도록 모든 조건을 먼저 확인한다. 실패 시 골드·영혼·덱·사용 플래그가 모두 유지된다.

### 6.4 `ShopOfferGenerator`

- 기존 `BattleRewardCatalog.CreateDefault()`의 일반 풀을 소비한다.
- `System.Random`과 생성자 시드를 사용한다.
- 같은 시드·방문 순서는 같은 후보를 만든다.
- 후보 3개는 서로 다른 정의 키다.
- 방문당 제안은 한 번만 생성한다.
- 재시작은 같은 설정의 새 생성기로 OfferId와 난수 상태를 초기화한다.

## 7. `FormalRunSession`

```csharp
public enum FormalRunPhase
{
    NotStarted,
    Combat,
    Shop,
    RunVictory,
    RunDefeat
}

public sealed class FormalRunSession
{
    public StageProgressionSession CombatSession { get; }
    public FormalRunPhase Phase { get; }
    public ShopVisit ActiveShop { get; }
    public int CompletedShopCount { get; }
    public int LastGoldReward { get; }

    public bool TryStartRun();
    public bool TrySelectOpponent(int offerId, string profileKey);
    public bool TrySelectBattleReward(int optionId);
    public bool TrySkipBattleReward();
    public bool TryBuyShopCard(int offerId, int optionId);
    public bool TryRemoveShopCard(int offerId, int cardId);
    public bool TryRestAtShop(int offerId);
    public bool TryLeaveShop(int offerId);
    public bool TryRestartRun();
}
```

### 7.1 상태 규칙

```text
NotStarted
→ Combat(상대 선택·전투·카드 보상)
→ Shop(방문 0)
→ Combat(상대 선택·전투·카드 보상)
→ Shop(방문 1)
→ Combat(고정 보스·카드 보상)
→ RunVictory
```

어느 전투에서든 패배하면 `RunDefeat`다.

- 일반전 카드 보상 완료 후 기존 상태는 `StageCleared`, 바깥 단계는 `Shop`이다.
- 상점에서 나가기 전에는 `TryAdvanceToNextStage()`를 호출하지 않는다.
- 상점 거래는 스테이지 인덱스를 바꾸지 않는다.
- 첫 상점 나가기 뒤 인덱스 1의 상대 선택, 둘째 상점 뒤 인덱스 2의 고정 보스를 준비한다.
- 보스 카드 보상 완료는 골드 정산 뒤 `RunVictory`이며 상점을 만들지 않는다.
- 재시작은 골드·상점·OfferId·거래 결과와 내부 전투 세션을 새 런 기준으로 복구한다.

### 7.2 다음 전투 생성 순서

상점 거래가 바꾼 영혼과 덱이 다음 전투에 반영돼야 하므로, `TryLeaveShop()` 승인 뒤에만 `CombatSession.TryAdvanceToNextStage()`를 호출한다. 다음 전투 준비 중 예외가 발생하면 상점을 닫거나 완료 수를 증가시키지 않고 오류를 노출한다.

## 8. Runtime·표시 연결

- `StageProgressionRuntime`은 정식 모드에서 `FormalRunSession`을 소유한다.
- `CoreLoopController`에는 `FormalRunSession.CombatSession`을 제공한다.
- 진행 Controller의 상대·보상·상점·재시작 입력은 정식 런 세션으로 전달한다.
- Presenter는 현재 골드, 최근 획득 골드, 상점 후보·서비스·가격·이용 가능 여부·거래 결과를 문자열 모델로 만든다.
- View는 가격·가능 여부를 계산하지 않고 그리기와 이벤트 전달만 한다.
- 상점 상태에서는 기존 `NEXT STAGE`를 숨긴다.
- 첫 상점 뒤 같은 진행 씬에서 상대 선택을 표시하고, 둘째 상점 뒤 보스가 준비되면 `CoreLoopTest`로 이동한다.

## 9. 자동 테스트 명세

### 9.1 골드·상점 단위 테스트

| ID | 검증 | 기대 결과 |
| --- | --- | --- |
| RF-U01 | 프로필별 골드 | 3·3·4·6·10 |
| RF-U02 | 카드 선택/건너뛰기 정산 | 양쪽 모두 1회 지급 |
| RF-U03 | 패배·잘못된·중복 입력 | 골드 변화 없음 |
| RF-U04 | 카드 구매 | 4 차감, 선택 카드 1장 추가 |
| RF-U05 | 카드 제거 | 3 차감, 대상 1장 제거, ID 미재사용 |
| RF-U06 | 휴식 | 3 차감, 영혼 3 회복·최대값 제한 |
| RF-U07 | 부족한 골드·잘못된 ID | 모든 상태 무변경 |
| RF-U08 | 방문당 서비스 제한 | 같은 서비스 두 번째 사용 거부 |
| RF-U09 | 상점 생성 결정성 | 같은 시드·방문 순서에 같은 후보 |
| RF-U10 | 재시작 | 골드 0, 최초 덱·영혼·제안 상태 |

### 9.2 정식 순서 통합 테스트

| ID | 검증 | 기대 결과 |
| --- | --- | --- |
| RF-I01 | 첫 일반전 보상 | 골드 지급, 상점 0, 인덱스 0 유지 |
| RF-I02 | 첫 상점 나가기 | 인덱스 1, 상대 선택 |
| RF-I03 | 둘째 일반전 보상 | 누적 골드, 상점 1, 인덱스 1 유지 |
| RF-I04 | 둘째 상점 나가기 | 인덱스 2, 고정 보스, 후보 없음 |
| RF-I05 | 보스 보상 | 보스 골드, 상점 없음, `RunVictory` |
| RF-I06 | 첫째·둘째·보스 패배 | `RunDefeat`, 후속 보상·골드·상점 없음 |
| RF-I07 | 상점 회복·덱 변경 뒤 전투 | 새 전투 시작 상태와 런 상태 일치 |
| RF-I08 | 오래된 상점 ID·닫힌 상점 입력 | 무변경 거부 |
| RF-I09 | 전체 재시작 10회 | 상대·보상·골드·상점 상태 격리 |
| RF-I10 | 기존 고정 세션 | 정식 런 미주입 시 기존 회귀 유지 |

### 9.3 화면·실제 흐름

- 카드 선택과 건너뛰기 양쪽의 골드 표시
- 구매·제거·휴식 가능/불가와 최근 거래 표시
- 두 상점 뒤 각각 상대 선택·보스 이동
- 1280×720·1920×1080 레이아웃
- 전체 EditMode, 실제 두 씬 왕복, Console Error·Exception 0

## 10. 외부 에셋·오픈소스

새 외부 에셋·오픈소스·패키지는 필요하지 않다. Unity, NUnit, 현재 프로젝트 코드와 두 씬을 재사용한다. 외부 자료를 추가하게 되면 이름·버전·URL·라이선스·사용 위치를 기록하고 이천서 검토 전에는 병합하지 않는다.

## 11. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-20 | 이천서 | 골드 상태·적별 정산·상점 거래·정식 런 조정 API와 테스트 기준 확정 |

