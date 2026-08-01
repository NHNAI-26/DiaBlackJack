# 테이블 도감 개발 명세

## DX-M02: 적 페이지 프리팹 계약

- `CodexOverlayView` 공개 API와 표시 모델은 유지한다. 영혼/골드 동적 TMP는 숫자만 출력한다.
- 초상화는 `RectMask2D` 뷰포트 안에서 비율 유지·중앙 크롭하고 `CodexFrame_0`으로 감싼다.
- 영혼/골드·설명·계약·덱 패널 외곽선은 Sliced `CodexOutline_0`을 사용한다.
- 시작 덱 `ScrollRect`는 세로 전용, Clamped, 스크롤바 없음이다. Content는 4열 고정 Grid, 셀 116×164, 간격 8×12, 좌우 패딩 8이다.
- 계약 카드는 기존 템플릿과 3열을 유지하며 최대 6개를 2행으로 책 안에 표시한다.
- `GameScene`의 덱 Content 인스턴스 `m_SizeDelta.y`, `m_AnchoredPosition.y` 오버라이드는 제거한다.
- 프리팹 테스트는 아트 연결, Sliced 타입, 별도 아이콘, 필수 참조, Grid/ScrollRect 설정과 모델 렌더 값을 검증한다.

## DX-00: 콘텐츠 계약

`EnemyCombatProfileDefinitionSO`는 적 이름·초상화·등급·최대 영혼·처치 골드·행동 정책 key·시작 덱·설명·계약 악마·보스 고정 단계를 소유한다. `EnemyContentCatalogSO`는 적 SO 6개를 순서대로 참조하고 순수 `EnemyCombatProfileCatalog`과 `GoldRewardCatalog`을 생성한다.

`DemonCardDefinitionSO`는 기존 이름·앞면·가격·효과·대가와 함께 도감 서사 `codexLoreDescription`을 소유한다. `CardContentCatalogSO`가 악마별 서사 사전을 생성한다. 별도 도감 콘텐츠 SO는 사용하지 않는다.

## DX-01: 순수 표시 모델

`CodexPresenter`는 Unity 없는 읽기 전용 모델을 만든다.

- `EnemyCodexPageViewModel`: 이름, 프로필 key, 최대 영혼, 처치 골드, 설명, 계약 악마 목록, 시작 덱.
- `DemonCodexPageViewModel`: 이름, 정의 key, 구매 골드, 영혼 가격, 서사, 액티브 스킬, 대가.
- `CodexDeckCardViewModel`: 정의 key, 숫자, 표시 이름, suit.
- `CodexNavigationState`: 카테고리별 현재 인덱스와 책 전체 경계 이동. 적 마지막 장에서 `E`를 누르면 악마 카드 첫 장으로, 악마 카드 첫 장에서 `Q`를 누르면 적 마지막 장으로 이동한다. 책 전체의 첫 장과 마지막 장에서는 더 이동하지 않는다.

적 정보는 `EnemyContentCatalogSO`가 생성한 `EnemyCombatProfileCatalog`, `GoldRewardCatalog`과 `CardContentCatalog`을 읽는다. 악마 정보는 `DemonCardDefinitionSO`에서 생성한 카드 정의와 서사 사전을 읽는다.

## DX-02: uGUI와 씬 연결

- `CodexOverlayView`: 전체 화면 Canvas, 차단막, 펼친 책, 닫기 버튼, 페이지 번호, 책갈피 두 개, 적/악마 페이지를 렌더링한다.
- 적 시작 덱은 `ScrollRect + GridLayoutGroup`, 계약 악마는 3열 미니 카드 그리드를 사용한다.
- `CodexController`: 열림 상태, 카테고리별 마지막 페이지, `Q/E`, 책갈피, 닫기를 소유한다.
- `CodexClickable`: GameManager의 기존 포인터 raycast로 여는 테이블 책 표식이다.
- 기준 해상도는 1920×1080이며 Canvas Scaler와 앵커로 1280×720을 지원한다.
- `CodexOverlayView` Custom Inspector는 Edit Mode와 Prefab Mode에서 적/악마·이전/다음·새로고침·끄기 프리뷰를 제공한다. 프리뷰는 새 오브젝트를 만들지 않고 기존 `ContractTemplate`과 `DeckTemplate`를 활성화해 각 목록의 첫 항목만 표시한다.
- 프리뷰 시작 전 표시값과 활성 상태를 보관하고, 끄기·Scene/Prefab 저장·Prefab Stage 종료·Play Mode 진입·assembly reload 전에 복원한다. 템플릿과 컨테이너의 RectTransform·scale·layout 수정값은 복원 대상이 아니다.
- 도감 프리팹의 RectTransform은 `custom` 앵커를 사용하지 않는다. 채움 용도의 기존 stretch preset은 유지하고, 나머지는 가까운 고정 preset과 양수 Width/Height를 직렬화해 Prefab Mode에서 직접 크기와 위치를 편집할 수 있게 한다.

## DX-03: 입력·생명주기

- 도감 열기 전에 덱 미리보기를 닫는다.
- 도감이 열린 동안 GameManager 전투·상점·HUD 입력과 카메라 전환을 막는다.
- `PauseSettingsController`가 호출하는 `TryCloseTransientOverlay()`는 도감을 덱 미리보기보다 먼저 닫는다.
- 전투/상점 이탈, GameManager 비활성화·재바인딩 때 도감을 닫고 임시 카드 슬롯을 정리한다.
- `GameFlowController`는 Combat/Shop에서만 `SetAvailable(true)`를 전달한다.

## 검증 계약

- 적 6종, 악마 12종이 누락·중복 없이 생성된다.
- 현재 영혼·골드·설명·계약 목록·덱 순서와 카드 가격·효과·대가가 일치한다.
- 카테고리를 직접 전환하면 카테고리별 마지막 페이지를 복원하고, `Q/E` 순차 이동은 적→악마 카드 경계를 끊김 없이 넘는 것이 보장된다.
- 프리팹 필수 버튼, ScrollRect, 두 카드 템플릿과 테이블 책 Collider가 존재한다.
- 에디터 프리뷰 전후 카드 템플릿 수는 2개로 유지되고, 프리뷰 종료 시 원래 표시값과 활성 상태가 복원된다.
- 도감 프리팹의 모든 RectTransform 앵커가 표준 preset이며 루트를 제외한 고정 앵커 요소는 양수 Width/Height를 가진다.
- GameScene missing script 0, Console Error 0, 전체 EditMode 실패 0을 유지한다.
- `CardContentBootstrap`은 카드 SO를 먼저 설치한 뒤 적 SO에서 순수 적·골드 카탈로그를 생성하고 설치한다. 순수 계층의 기본 카탈로그는 EditMode 테스트 호환 fallback으로만 유지한다.
