# 정식 런 진행 개발 명세서

> 프로젝트: DiaBlackJack  
> 기획·통합 책임자: 이천서  
> 구현·검증 담당자: 이천서 (HONG 계획 범위 인수)
> 작업 식별자: RF-00~RF-05, GSV-06, GSV-08, GSV-09
> 버전: v1.6
> 상태: GSV-09 구현·자동 검증 완료 · 720p/1080p 실화면 검증 대기
> 최종 갱신: 2026-08-04

> **카드 성장 상점 통합 변경 안내 (2026-07-30)**
> 정식 런은 더 이상 `RewardSelection`이나 카드 보상 선택·건너뛰기를 진행 조건으로 사용하지 않는다. 일반전 승리는 골드를 한 번 정산한 뒤 상점을 열고, 보스 승리는 골드를 결과에 반영한 뒤 즉시 `RunVictory`로 끝난다. 기존 RW API와 상태는 향후 사건 재사용을 위해 보존하되 RF 화면과 세션에서는 호출하지 않는다.

> **시작 악마 지급 구현 (2026-07-31):** `StartingDemonGrantGenerator`가 기본 풀에서 서로 다른 2장을 결정적으로 생성한다. `StageProgressionSession.TryStartRun()`은 미지급 상태에서 두 장을 원자 지급하고 같은 세션에서 재호출해도 다시 추첨하지 않는다. 공개 카드는 선택 대상이 아니며, `TryCompleteStartingDemonReveal()`은 동시 공개 뒤 확인 버튼 입력을 받아 체크포인트 저장을 요청한다. 저장 성공 뒤 상대 선택으로 전환한다.

> **현행 상점 규칙 변경 안내 (2026-07-29)**
> 현행 기획은 일반 카드 3장·악마 카드 2장, 슬롯별 재고 1개와 구매 후 `SOLD OUT`, 방문 전체 카드 구매 상한 없음, 새로고침·재입고 없음이다. 라이터와 위스키는 방문당 각각 1회이며 둘 다 이용할 수 있고, 하나 이상 이용한 방문마다 다음 상점의 두 서비스 가격이 함께 1단계 오른다. 이용 개수는 상승폭에 영향을 주지 않는다. GameScene MVP의 기본 2골드·상승 +1은 임시값이며 정식 구현은 `Docs/formal-run-flow-design.md` v0.7을 우선 적용한다.

> **GSV-06 판매 슬롯 표시 개정 (2026-08-07)**
> 일반 카드 3장, 악마 카드 2장, 라이터, 위스키는 상점이 열려 있는 동안 각 상품의 상점 전용 월드 앵커를 Screen Space UI로 투영한 가격 배지를 항상 가진다. 배지는 상품명 없이 `CurrencyIconMarkup.GoldTag × 가격`만 표시한다. 기존 호버 설명은 그대로 독립 동작한다.
> 일반·악마 카드는 `Card.prefab`의 호버용 `TopPosition`·`BottomPosition`과 분리된 `ShopPriceAnchor`, 라이터·위스키는 각 프리팹의 독립 `PriceAnchor`를 사용한다. `ShopPriceBadge.prefab`의 피벗은 `(0.5, 1)`로 유지한다. 화면 밖으로 투영되는 배지는 방향을 전환하지 않고 HUD 컨테이너 안으로 제한한다. HUD는 직렬화된 전용 컨테이너에 이를 풀링한다. `ShopCardOfferStatusView`는 월드 가격을 그리지 않고 중앙 `SOLD OUT`만 담당한다. 구매 성공 카드의 배지 텍스트만 `품절`로 변경하며 실패·골드 부족·중복 입력은 상태와 슬롯을 바꾸지 않는다. 상점 종료나 상품 비활성화에는 배지를 숨기며 모든 Graphic Raycast를 끈다. 가격 컨테이너는 HUD의 첫 번째 sibling과 최저 Canvas sorting order를 사용해 다른 UI보다 뒤에 렌더한다.
> `ShopController`는 `Table Controller.prefab`에 저장된 Shop 미리보기 카드 5개의 로컬 위치·회전·크기를 상점 진입 전에 캡처하고 미리보기 오브젝트를 숨긴다. 단독·정식 상점의 실제 카드는 같은 순서의 캡처 Transform으로 생성하며, 저장 배치가 없을 때만 spacing 계산으로 대체한다. 라이터·위스키는 배치된 실제 Transform을 변경하지 않는다.

> **GSV-08 라이터 선택·나가기 UI 계약 (2026-08-03)**
> `DeckPreviewView`는 기존 열람 모드를 보존하면서 단일 선택 모드와 `SelectionConfirmed(int)`·`SelectionCancelled`를 제공한다. 각 물리 카드를 1슬롯으로 표시하고 클릭은 강조만 수행하며 비활성 카드는 입력을 받지 않는다. `GameHudView`는 `ShopLeaveRequested`와 표시·활성 API를 제공한다. `ShopLeaveRoot` Canvas는 덱 오버레이 100보다 높은 150이며, 버튼 배경은 `Brush_UI_9`다.

> **GSV-09 공용 기본 버튼 프리팹 계약 (2026-08-04)**
> `Assets/03. Prefabs/UI/DefaultButton.prefab`은 `234×66` RectTransform, `Brush_UI_9`를 비율 유지하는 `Image`, 표준 Color Tint `Button`, 전주완판본 TMP 28pt 자동 크기·굵게·중앙 정렬 `Label`을 가진다. 기본 문구는 `버튼`이며 사용처가 프리팹 오버라이드한다. `HUD.prefab`의 `ShopLeaveButton`은 이 프리팹의 중첩 인스턴스이고 문구 `상점 나가기`, `ShopLeaveRoot` 정렬 순서 150과 기존 직렬화 참조를 유지한다. 재생성 도구는 프리팹 누락을 명확한 오류로 거부한다. 신규 C# 공개 API, 래퍼, 로컬라이제이션 의존성은 없다.

> **현행 저장 규칙 변경 안내 (2026-07-26)**
> 시작 악마 2장 지급, 전투 승리 골드 정산, 상점 나가기, 사건 해결과 런 종료 뒤에만 체크포인트를 갱신한다. 시작 악마 지급 결과는 런 시드와 카드 키·물리 ID로 복원하며 재접속 시 다시 추첨하지 않는다.

> **Notion v0.7 골드 재명세 (2026-07-29)**
> 적은 `cowardly-gambler`, `gunslinger`, `cultist`, `trickster`, `enforcer`, `final-boss` 여섯 프로필이다. 옛 3·3·4·6·10은 RF-00 과거 기록으로만 보존한다. 현행 지급량 `3·4·6·7·9·15`는 RF-01B 카탈로그·정산에 구현됐다.

## 1. 기술 목표

완료된 `StageProgressionSession`의 전투 2회·보스 1회 구조를 보존하면서 정식 런 경로에서는 카드 보상 생성을 우회하고 일반 전투 승리 뒤 골드 정산과 상점 게이트를 삽입한다.

```text
FormalRunSession
├─ StageProgressionSession : 상대 선택·전투·승패·전투 스테이지, RW는 유산 기능으로 보존
├─ GoldRewardCatalog       : 선택된 적 프로필별 승리 골드
└─ ShopVisit               : 구매·제거·휴식·나가기
```

기존 전투 세션은 전투 세부 규칙을 계속 소유한다. 정식 런 세션은 끝난 전투를 참조 동일성으로 한 번만 정산해 골드를 지급하고, 일반전은 `StageCleared`에서 곧바로 다음 전투로 가지 않게 상점을 열며 보스는 즉시 `RunVictory`로 끝낸다.

## 2. 현재 기준선

- `RunProgress`는 일반 전투 2개와 마지막 보스 1개를 소유한다.
- 현재 `StageProgressionSession`은 상대 선택, 실제 전투, 카드 보상과 런 승패를 소유하므로 코드 변경 전에는 승리 시 `RewardSelection`으로 간다.
- 정식 런 이관에서는 일반전 승리를 `StageCleared`, 보스 승리를 `RunVictory`로 직접 완료하는 명시적 무카드보상 경계를 추가해야 한다. 기존 RW 경로는 삭제하지 않는다.
- `TryAdvanceToNextStage()`는 `StageCleared`에서 다음 상대 선택 또는 고정 보스를 만든다.
- `PlayerRunState`는 영혼과 런 덱을 소유하지만 골드는 아직 없다.
- EUI-05 기록 기준 전체 EditMode 315/315다. RF-00은 문서 전용이므로 재실행 결과로 주장하지 않는다.

## 3. 파일 구조와 소유권

| 경로 | 역할 | 담당 |
| --- | --- | --- |
| `Assets/01. Scripts/StageProgression/RunFlow` | 골드 정산과 정식 순서 조정 | HONG |
| `Assets/01. Scripts/StageProgression/Shop` | 상점 제안·방문·거래 | HONG |
| `Assets/01. Scripts/StageProgression/PlayerRunState.cs` | 골드와 카드 제거 최소 확장 | HONG, 이천서 검토 |
| `Assets/01. Scripts/UI/StageProgression` | Runtime·Presenter·View·Controller 연결 | HONG, 이천서 검토 |
| `Assets/06.Packages/Tests/EditMode/StageProgression/RunFlow` | RF 단위·통합·화면·반복 테스트 | HONG |

새 asmdef, 패키지, 전용 씬과 외부 에셋은 추가하지 않는다.

## 4. `PlayerRunState` 확장

RF-01A에서 기존 3인자 생성자는 골드 0으로 시작하는 호환을 유지하고, 골드를 지정하는 새 생성 경로를 추가했다. 복원 경로는 저장된 현재 골드를 받되 새 런 초기 골드는 0으로 분리한다.

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

RF-01A는 `RunSaveCapture`가 `CurrentGold`를 스냅샷에 기록하고 `RunRestoreFactory`가 검증된 양수 골드를 복원하도록 함께 연결했다. 음수 골드는 기존 `RunSaveValidator`가 계속 `InvalidGold`로 거부한다.

## 5. 골드 보상 계약

### 5.1 `GoldRewardCatalog`

```csharp
public sealed class GoldRewardCatalog
{
    public static GoldRewardCatalog CreatePrototype();
    public int GetAmount(string profileKey);
}
```

- 키는 `cowardly-gambler`, `gunslinger`, `cultist`, `trickster`, `enforcer`, `final-boss`다.
- 지급량은 `cowardly-gambler=3`, `gunslinger=4`, `cultist=6`, `trickster=7`, `enforcer=9`, `final-boss=15`다. 임시 기본값이나 0 대체를 넣지 않는다.
- 중복·빈 키·0 이하 금액은 카탈로그 생성 시 거부한다.
- 알 수 없는 프로필은 0으로 대체하지 않고 명시적으로 실패한다.
- 골드는 UI 표시 이름이나 스테이지 번호가 아니라 `ActiveStage.BattleProfileKey`로 조회한다.

### 5.2 정산 시점

RF-01B에서는 `StageProgressionSession`이 종료된 전투의 승리 결과를 처음 동기화할 때 한 번 정산한다. 기존 `_processedBattle` 참조 동일성 방어를 함께 사용하므로 카드 선택·건너뛰기 API는 RF 정산 진입점이 아니다. RF-03의 `FormalRunSession`은 이 정산 경계를 재사용하면서 기존 카드 보상 화면만 우회한다.

- 일반전: 골드 추가 → 상점 생성 → 바깥 단계 `Shop`
- 보스: 골드 추가 → 기존 `RunVictory` 유지 → 상점 없음
- 실패 입력: 골드·단계 변화 없음

정식 런 UI는 기존 보상 API를 표시하거나 호출하지 않는다. 반드시 바깥 세션의 승리 정산 결과를 사용하고, 같은 전투 참조를 다시 동기화하면 무변경으로 거부한다.

## 6. 상점 데이터 계약

### 6.1 `ShopCardOption`

| 속성 | 규칙 |
| --- | --- |
| `OptionId` | 제안 안에서 고유, 0 이상 |
| `DeckKind` | 일반 카드 또는 악마 카드 |
| `DefinitionKey` | `DeckKind`에 대응하는 카탈로그의 유효한 정의 키 |
| `Price` | 옵션 생성 시 확정되는 조정 가능한 값 |

### 6.2 `ShopOffer`

| 속성 | 규칙 |
| --- | --- |
| `OfferId` | 생성기 인스턴스에서 0부터 증가 |
| `VisitIndex` | 0 또는 1 |
| `CardOptions` | 자동 발동 카드를 포함할 수 있는 일반 3개와 악마 2개, 슬롯별 재고 1개 |
| `UtilityPriceLevel` | 앞선 이용 방문 수, 새 런은 0 |
| `WhiskeyPrice` | `기본 가격 + UtilityPriceLevel × 공통 상승폭` |
| `WhiskeyRecovery` | 조정 가능한 영혼 회복량 |
| `LighterPrice` | `기본 가격 + UtilityPriceLevel × 공통 상승폭` |

제안은 불변이다. 화면 재진입과 같은 방문의 거래 뒤에도 카드 후보·현재 가격은 바뀌지 않는다. 서비스 이용 결과는 상점을 정상적으로 나갈 때 다음 방문의 `UtilityPriceLevel`에 한 번만 반영한다.

### 6.3 `ShopVisit`

```csharp
public sealed class ShopVisit
{
    public ShopOffer Offer { get; }
    public IReadOnlyCollection<int> PurchasedOptionIds { get; }
    public bool HasRemovedCard { get; }
    public bool HasRested { get; }
    public bool HasUsedAnyUtility { get; }
    public bool IsClosed { get; }
    public ShopTransaction LastTransaction { get; }

    public bool TryBuyCard(int offerId, int optionId, PlayerRunState player);
    public bool TryRemoveCard(int offerId, int cardId, PlayerRunState player);
    public bool TryRest(int offerId, PlayerRunState player);
    public bool TryClose(int offerId);
}
```

거래 순서는 `제안·ID·방문 상태 검증 → 대상·골드 검증 → 결과 계산 → 비용과 효과 적용 → 사용 플래그·결과 기록`이다.

카드 구매에는 방문 전체 횟수 플래그를 두지 않고 구매한 `OptionId`만 재구매를 거부한다. 라이터와 위스키는 서로 독립된 1회 플래그를 사용하므로 같은 방문에서 둘 다 성공할 수 있다. `TryClose`는 둘 중 하나 이상 성공한 방문에 대해 바깥 세션의 가격 단계를 정확히 1만 올린다.

RF 범위는 같은 런타임 어셈블리 안에서 골드 소비와 카드 변경을 처리한다. 비용 소비 뒤 효과 적용이 실패할 수 없도록 모든 조건을 먼저 확인한다. 실패 시 골드·영혼·덱·사용 플래그가 모두 유지된다.

### 6.4 `ShopOfferGenerator`

- 일반 카드는 `CardDefinitionCatalog.All`에서 상점 허용 정의를 고르며 자동 발동 카드 5종을 포함한다. 기존 `BattleRewardCatalog`는 정식 상점의 데이터 원천으로 사용하지 않는다.
- 프로젝트 공용 `DeterministicRng`와 생성자 시드를 사용한다.
- 같은 시드·방문 순서는 같은 일반 3장·악마 2장을 만든다.
- 한 제안 안에서 같은 `DefinitionKey`를 중복하지 않는다.
- 엘리트 승리 플래그는 높은 등급 일반 카드의 가중치만 높이고 슬롯 수와 가격 규칙은 바꾸지 않는다. 정확한 가중치는 주입 가능한 정책 또는 데이터로 둔다.
- 같은 덱 종류 안의 후보는 서로 다른 정의 키다.
- 방문당 제안은 한 번만 생성한다.
- 재시작은 같은 설정의 새 생성기로 OfferId와 난수 상태를 초기화한다.

RF-02 구현 기본값은 일반 카드 3골드, 악마 카드 3골드, 라이터·위스키 2골드, 이용 방문당 공통 +1골드, 위스키 영혼 2 회복이다. 이는 GameScene 프로토타입과 맞춘 주입 가능한 값이며 최종 밸런스 확정이 아니다. 엘리트 승리 가중치는 숫자 5 이상 일반 카드의 선택 가중치를 2배로 적용하며 슬롯 수와 가격은 유지한다.

### 6.5 GSV-07 상점 후보 중복 계약

- `ShopOfferGenerator.Generate`는 현재 보유 악마 정의 키를 입력받는다.
- 일반 카드 슬롯은 같은 `DefinitionKey`를 허용한다.
- 악마 카드 슬롯은 서로 다른 `DefinitionKey`만 허용한다.
- 현재 보유 악마 정의 키는 악마 후보 풀에서 제거한 뒤 가중 추첨한다.
- 제외 후 악마 후보가 2장 미만이면 불완전한 상점을 만들지 않고 불변식 위반으로 중단한다.
- `FormalRunSession`은 신규 상점 생성, 저장 복원, 완료 상점 RNG 재생 모두에 현재 악마 덱 정의 키를 전달한다.

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
    public int UtilityPriceLevel { get; }

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

RF-03 구현에서는 `StageProgressionSession` 생성자의 `usesBattleRewards`를 정식 런에서만 `false`로 설정한다. 기본값 `true`는 기존 RW 선택·건너뛰기 회귀를 보존한다. 전투 종료 동기화 알림은 내부 이벤트로 정식 세션에 전달되므로 `CombatSession`의 기존 전투 입력 API를 사용해도 바깥 `Phase`가 상점·승리·패배로 즉시 맞춰진다.

### 7.1 상태 규칙

```text
NotStarted
→ Combat(상대 선택·전투·골드 정산)
→ Shop(방문 0)
→ Combat(상대 선택·전투·골드 정산)
→ Shop(방문 1)
→ Combat(고정 보스·골드 결과 기록)
→ RunVictory
```

어느 전투에서든 패배하면 `RunDefeat`다.

- 일반전 승리 정산 후 내부 상태는 `StageCleared`, 바깥 단계는 `Shop`이다.
- 상점에서 나가기 전에는 `TryAdvanceToNextStage()`를 호출하지 않는다.
- 상점 거래는 스테이지 인덱스를 바꾸지 않는다.
- 첫 상점 나가기 뒤 인덱스 1의 상대 선택, 둘째 상점 뒤 인덱스 2의 고정 보스를 준비한다.
- 보스 승리는 골드 정산 뒤 `RunVictory`이며 카드 보상과 상점을 만들지 않는다.
- 재시작은 골드·상점·OfferId·거래 결과·서비스 가격 단계를 0으로 되돌리고 내부 전투 세션을 새 런 기준으로 복구한다.

### 7.2 다음 전투 생성 순서

상점 거래가 바꾼 영혼과 덱이 다음 전투에 반영돼야 하므로, `TryLeaveShop()` 승인 뒤에만 `CombatSession.TryAdvanceToNextStage()`를 호출한다. 다음 전투 준비 중 예외가 발생하면 상점을 닫거나 완료 수를 증가시키지 않고 오류를 노출한다.

## 8. Runtime·표시 연결

- `StageProgressionRuntime`은 정식 모드에서 `FormalRunSession`을 소유한다.
- `CoreLoopController`에는 `FormalRunSession.CombatSession`을 제공한다.
- 진행 Controller의 상대·보상·상점·재시작 입력은 정식 런 세션으로 전달한다.
- Presenter는 현재 골드, 최근 획득 골드, 상점 후보·서비스·가격·이용 가능 여부·거래 결과를 문자열 모델로 만든다.
- View는 가격·가능 여부를 계산하지 않고 그리기와 이벤트 전달만 한다.
- 상점 상태에서는 기존 `NEXT STAGE`를 숨긴다.
- 첫 상점 뒤 같은 진행 씬에서 상대 선택을 표시하고, 둘째 상점 뒤 보스가 준비되면 `GameScene`으로 이동한다.

RF-04 구현에서 `StageProgressionRuntime.FormalSession`은 현재 저장 흐름의 세션 참조가 바뀌면 새 정식 세션으로 다시 결합한다. 새 게임 Factory와 이어하기 `RunRestoreFactory` 모두 `usesBattleRewards: false`를 사용한다. 기존 테스트나 독립 사용자가 기본 세션을 주입하면 `UsesBattleRewards == true`인 유산 표시 경로를 그대로 유지한다.

`StageProgressionPresenter.Create(FormalRunSession)`은 현재·최근 획득 골드, 상대별 확정 골드, 상점 후보·판매 완료·서비스 가격/이용 여부·최근 거래와 카드 제거 대상을 완성된 문자열·가능 여부로 만든다. View는 계산하지 않고 IMGUI로 렌더링하며 Controller만 정식 세션 거래 API를 호출한다.

`GameManager`는 `StageProgressionRuntime`에 진행 중인 전투가 있으면 독립 `CoreLoopSession`을 만들지 않고 해당 `StageProgressionSession`의 전투를 채택한다. 모든 전투 입력은 진행 세션을 통해 전달하여 승패 동기화와 골드 정산을 한 번만 수행한다. `GameScene`에서 생성한 정식 Runtime은 진행·전투 목적지를 모두 `GameScene`으로 두므로 선택 확정과 전투 종료마다 같은 씬을 다시 불러 현재 단계에 맞는 화면을 구성한다. `StageTest`의 씬 작성 Runtime은 기존 `StageTest → GameScene → StageTest` 테스트 경계를 유지한다.

## 9. 자동 테스트 명세

### 9.1 골드·상점 단위 테스트

| ID | 검증 | 기대 결과 |
| --- | --- | --- |
| RF-U01 | 프로필별 골드 | 3·4·6·7·9·15와 높은 영혼 구간 증가, `엘리트 > 일반`, `보스 > 엘리트` 일치 |
| RF-U02 | 승리 전투 1회 정산 | 같은 종료 전투를 반복 동기화해도 1회만 지급 |
| RF-U03 | 패배·잘못된·중복 입력 | 골드 변화 없음 |
| RF-U04 | 복수 카드 구매 | 골드가 허용하는 모든 미판매 슬롯 구매 가능, 같은 슬롯 재구매 거부 |
| RF-U05 | 라이터 | 현재 단계 가격 차감, 대상 1장 제거, ID 미재사용, 두 번째 이용 거부 |
| RF-U06 | 위스키 | 현재 단계 가격 차감, 영혼 회복·최대값 제한, 두 번째 이용 거부 |
| RF-U07 | 부족한 골드·잘못된 ID | 모든 상태 무변경 |
| RF-U08 | 방문당 서비스 제한 | 라이터·위스키 각각 두 번째 사용 거부, 서로 다른 두 서비스는 모두 허용 |
| RF-U09 | 상점 생성 결정성 | 같은 시드·방문 순서에 같은 후보 |
| RF-U10 | 서비스 가격 단계 | 하나 또는 둘 이용 시 다음 방문 +1단계, 미이용 시 유지 |
| RF-U11 | 재시작 | 골드·가격 단계 0, 최초 덱·영혼·제안 상태 |

### 9.2 정식 순서 통합 테스트

| ID | 검증 | 기대 결과 |
| --- | --- | --- |
| RF-I01 | 첫 일반전 승리 | 카드 보상 없이 골드 지급, 상점 0, 인덱스 0 유지 |
| RF-I02 | 첫 상점 나가기 | 인덱스 1, 상대 선택 |
| RF-I03 | 둘째 일반전 승리 | 카드 보상 없이 누적 골드, 상점 1, 인덱스 1 유지 |
| RF-I04 | 둘째 상점 나가기 | 인덱스 2, 고정 보스, 후보 없음 |
| RF-I05 | 보스 승리 | 카드 보상 없이 보스 골드, 상점 없음, `RunVictory` |
| RF-I06 | 첫째·둘째·보스 패배 | `RunDefeat`, 후속 골드·상점 없음 |
| RF-I07 | 상점 회복·덱 변경 뒤 전투 | 새 전투 시작 상태와 런 상태 일치 |
| RF-I08 | 오래된 상점 ID·닫힌 상점 입력 | 무변경 거부 |
| RF-I09 | 전체 재시작 10회 | 상대·골드·상점 상태 격리, `RewardSelection` 미진입 |
| RF-I10 | 기존 고정 세션 | 정식 런 미주입 시 기존 회귀 유지 |

### 9.3 화면·실제 흐름

- 카드 선택과 건너뛰기 양쪽의 골드 표시
- 복수 카드 구매, 라이터·위스키 독립 이용, 현재 가격 단계와 최근 거래 표시
- 일반 상점과 라이터 선택 상태의 `나가기` 표시
- 두 상점 뒤 각각 상대 선택·보스 이동
- 1280×720·1920×1080 레이아웃
- 전체 EditMode, 실제 두 씬 왕복, Console Error·Exception 0
- GSV-08 선택 변경·확인 1회·취소 무변경·선택 불가 카드 차단과 100슬롯·선택 프레임·확인 버튼·`Brush_UI_9` 직렬화
- GSV-06 7개 상품별 유효 앵커·항상 표시 가격 요청, Table Controller 저작 Transform과 런타임 7상품 배치 일치, 구매 성공 카드만 `품절`, 중앙 `SOLD OUT` 유지, 실패 원자성, 비활성/상점 종료 숨김, Raycast 비차단
- Scene View Shop 미리보기의 카드 5개·아이템 2개 앵커 연결선·마커·라벨

### 9.4 RFM02 실제 게임 진입 계약

- `StageTest`의 씬 작성 Runtime은 기존 기본 악마 덱과 시작 흐름을 유지한다.
- `GameScene` 직접 실행 시에만 별도 정식 Runtime을 만들고 빈 악마 덱으로 시작한다.
- 정식 Runtime은 새 런의 결정적 루트 시드에서 서로 다른 악마 2장을 한 번 만들고 모두 `DemonDeck`에 추가한다. 두 장 공개 연출 완료와 체크포인트 저장이 끝난 뒤 첫 상대 제안을 준비한다.
- 일반 전투 전 상대 후보는 정확히 2명이며, 선택한 프로필 키만 실제 `CoreLoopBattle` 생성에 사용한다.
- 활성 정식 전투가 없는 `GameScene`은 씬을 이동하지 않고 코드로 진행 View·Controller를 생성한다.
- 상대 확정 시 같은 `GameScene`을 다시 불러 선택 UI를 제거하고 생성된 전투를 `PlayerTurn`으로 시작한다.
- 고정 상대 독립 전투는 `allowStandaloneBattleForDebug`가 켜진 경우에만 허용한다.
- 저장·이어하기는 기존 루트 시드와 무작위 지급된 시작 악마 2장을 그대로 복원한다.

## 10. 외부 에셋·오픈소스

새 외부 에셋·오픈소스·패키지는 필요하지 않다. Unity, NUnit, 현재 프로젝트 코드와 두 씬을 재사용한다. 외부 자료를 추가하게 되면 이름·버전·URL·라이선스·사용 위치를 기록하고 이천서 검토 전에는 병합하지 않는다.

## 11. 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-08-07 | HONG | GSV-06 상점 가격 표시를 상품별 월드 앵커→Screen Space UI 계약으로 개정했다. 후속으로 상품명을 제거하고 독립 가격 프리팹·HUD 최하단 Canvas·7개 요청·골드 아이콘 가격·`품절`·중앙 `SOLD OUT`·비활성/종료 숨김·비차단 Graphic을 테스트 기준에 반영했다. |
| 2026-07-31 | 이천서 | RF-05 실화면 게이트를 완료했다. 진행 IMGUI의 보유 카드는 720p에서 4열, 그보다 큰 화면에서 5열로 배치하고 서비스·나가기 버튼은 남은 너비를 균등 사용한다. 1280×720 정식 런 왕복과 1920×1080 상대 선택을 확인했으며 `FormalRunPresentationTests` 6/6(작업 `b50ce54dc6dc43dfba83d08d684e2b9f`)·전체 EditMode 798/798(작업 `96c65780a79e41a48a0d850c06f868d4`)을 통과했다. Console의 Exception 1건은 Test Framework 결과 저장 안내이며 게임 코드 오류는 0이다. |
| 2026-07-30 | 이천서 | RFM02 진입 책임을 정정했다. GameScene 직접 실행은 Runtime·진행 선택 UI를 만들지 않고 기존 독립 `CoreLoopSession`을 즉시 시작한다. 외부 진행 세션이 `InBattle`과 Battle을 보유한 경우에만 GameManager가 그 전투를 채택한다. StageTest의 기존 세션 팩터리는 유지한다. |
| 2026-07-30 | 이천서 | RF-05 반복 검증 8건을 추가했다. 10회 전체 승리·재시작, 상점 네 행동과 중복 거부, 오래된 상대·상점 입력, 세 전투 위치별 10회 패배·재시작, 네 일반 프로필 선택·골드, 두 씬 Build Settings·Runtime 목적지·필수 컴포넌트·누락 스크립트를 검증했다. 전체 EditMode 744/744 통과. |
| 2026-07-30 | 이천서 | 정식 런의 전투 목적지를 `GameScene`으로 바꾸고 `GameManager`에 진행 세션 입력 위임, 실제 프로필·골드 표시, 종료 후 진행 씬 복귀와 독립 MVP 호환 경계를 추가했다. 런타임·StageProgression 테스트 어셈블리 컴파일 및 RF 비Unity 42/42를 재확인했으며 실제 Play Mode 왕복은 Unity MCP 복구 뒤 검증한다. |
| 2026-07-30 | 이천서 | RF-04 Runtime 재결합, 신규/복원 세션의 RW 우회, CoreLoop 전투 세션 제공, 정식 Presenter·IMGUI 상점·Controller 거래 입력을 구현했다. RF04 6건은 어셈블리 컴파일, 그중 Unity 비의존 5건은 실행 통과했고 기존 RF 누적 비Unity 42/42를 확인했다. Unity MCP 8080은 TCP 연결되지만 initialize handshake가 실패해 실제 EditMode·720p/1080p·씬 왕복은 미실행으로 남겼다. |
| 2026-07-30 | 이천서 | RF-03 `FormalRunSession`, 무카드보상 완료 경계, 전투 결과 동기화, 두 상점·가격 단계·재시작을 구현했다. 첫/둘째 일반전·보스·세 위치 패배·상점 변경 전달·오래된 ID·기존 RW 회귀를 RF03 12건으로 검증했고 RF-01~03 누적 37/37 및 두 어셈블리 컴파일을 확인했다. |
| 2026-07-30 | 이천서 | RF-02 불변 옵션·제안, 결정적 일반 3/악마 2 생성기, 복수 구매·라이터·위스키·무료 나가기 거래와 `PlayerRunState` 최소 덱 제거 경계를 구현했다. 자동 카드 5종 출현·구매 후 전투 전달, 엘리트 가중치, 실패 원자성, 가격 단계와 재시작을 대상 11/11 및 두 어셈블리 컴파일로 확인했다. |
| 2026-07-30 | 이천서 | RF-01B `GoldRewardCatalog`와 `StageProgressionSession`의 종료 승리 전투별 1회 지급을 구현했다. 여섯 확정 금액·등급 역전 방지·잘못된 정의/조회·일반/보스 승리·중복·패배·구형 무프로필 호환을 RF01B 7개로 고정하고 RF-01 전체 대상 14/14 및 두 어셈블리 컴파일을 확인했다. |
| 2026-07-30 | 이천서 | RF-01A에서 `PlayerRunState.CurrentGold`, 지급·소비·재시작과 저장 캡처·복원을 구현하고 대상 7/7 및 Border·StageProgression 테스트 어셈블리 컴파일을 확인; Unity MCP 부재로 전체 Test Runner는 미실행 |
| 2026-07-30 | 이천서 | 정식 런의 카드 보상 입력 의존을 제거하고 종료 전투 참조별 1회 골드 정산, 일반전 직후 상점, 보스 직후 승리 경계로 재명세했다. 상점 일반 후보에 자동 발동 카드 5종과 무중복 정의 키, 엘리트 높은 등급 가중치를 추가하고 기존 RW API는 삭제 없이 비활성·재사용 경계로 유지했다. |
| 2026-07-29 | 이천서 | 여섯 프로필 골드를 3·4·6·7·9·15로 확정해 카탈로그·RF-U01 기대값과 RF-01B 착수 조건 갱신 |
| 2026-07-29 | 이천서 | RF-01을 골드 상태 RF-01A와 지급량 확정 후 카탈로그·정산 RF-01B로 분리하고 여섯 프로필 키·현행 폴더 경로·테스트 기대를 재명세 |
| 2026-07-29 | 이천서 | 카드 슬롯별 재고와 방문 전체 구매 상한 없음, 라이터·위스키 독립 1회, 이용 방문당 다음 상점 공통 가격 +1단계, 무료 나가기·재시작 초기화 계약과 RF-U04~U11 검증 기준으로 개정 |
| 2026-07-20 | 이천서 | 골드 상태·적별 정산·상점 거래·정식 런 조정 API와 테스트 기준 확정 |

