# 테이블 도감 개발 명세

## DX-M13: 악마 도감 문구·자동 카드 무늬 분리

- `DemonCardDefinitionSO`는 계약 카드 호버용 효과·대가와 별도로 악마 도감용 효과·대가 override를 소유한다. override가 비어 있으면 기존 계약 문구를 사용해 기존 에셋 표시를 보존한다.
- `CardContentCatalogSO`는 도감 전용 효과·대가 사전을 만들고, 런타임 도감과 Inspector 프리뷰는 이 사전을 `CodexPresenter`에 전달한다. 계약 후보·활성 계약·상점·시작 공개의 `DemonCardView`는 기존 효과·대가만 사용한다.
- 적 시작 덱 도감은 자동 발동 카드를 occurrence와 관계없이 스페이드로 확정한 뒤 `(DefinitionKey, Suit)`로 묶는다. 일반·수동·패시브 카드의 기존 스페이드·클로버 교대는 유지한다.
- 내 덱 또는 도감 오버레이가 열린 동안 튜토리얼 대사 진행 입력을 소비하지 않는다. 오버레이가 닫힌 다음 입력부터 기존 클릭·Space·Enter 진행을 재개한다.

## DX-M12: 현재 상대 적 페이지 열기 계약

- `CodexNavigationState.TryShowEnemyPage(int pageIndex)`는 유효한 적 페이지 인덱스면 적 카테고리와 대상 인덱스를 함께 변경한다. 음수·범위 밖·현재 적 페이지와 같은 인덱스는 거부하고 상태를 바꾸지 않는다.
- 기존 `CodexController.Open()`은 적 도감 1페이지를 시작 위치로 사용한다. `Open(string enemyProfileKey)`는 `_enemyPages.ProfileKey`와 `StringComparer.Ordinal`로 정확히 일치하는 페이지를 선택하며, null·공백·미등록 key는 1페이지로 대체한다.
- `GameManager`는 도감 클릭 시 활성 `CoreLoopBattle`이 있으면 `CurrentEnemyProfileKey`를 전달하고, 전투가 없으면 인자 없는 `Open()`을 호출한다.
- 매번 열기 전에 시작 적 페이지를 다시 선택한다. 열린 뒤 Q/E, 탭, 계약 악마 바로가기와 카테고리별 페이지 기억은 기존 계약을 유지한다.
- 프리팹·Scene·직렬화 참조와 CoreLoop·StageProgression 규칙은 변경하지 않는다.

## DX-M11: 3D 책과 호버 계약

- `CodexBook.prefab` 루트의 `CodexClickable`과 `BoxCollider`는 유지하고 `SpriteRenderer`를 제거한다. 자식 모델은 `Assets/05. Arts/FBX/antique_book.fbx`, 머티리얼은 `Assets/05. Arts/Material/MAT_Book.mat`을 사용한다.
- 콜라이더는 모델 bounds인 center `(0.0665, 0.1007, 0.0014)`, size `(1.0392, 0.2923, 1.0853)`을 사용한다. 테이블 배치는 position `(2.224, 0.077, -0.339)`, rotation `(0, 8, 0)`, scale `(1, 1, 1)`이다.
- `CodexClickable.SetHovered(bool)`은 자식 Renderer를 `PostProcessOutlineRegistry`에 등록·해제한다. 머티리얼 `_StencilOutlineColor`를 우선 사용하고 폭은 플레이어 덱과 같은 4px이다. 비활성화·파괴 시 남은 등록을 제거한다.
- `GameManager`는 기존 Physics raycast에서 사용 가능한 `CodexClickable`을 판정하고, 입력 잠금·모달·선택 UI·덱/도감 오버레이·전투 해제 시 도감 호버를 초기화한다. 기존 클릭과 `CodexController.IsAvailable` 계약은 유지한다.
- 공개 도메인 API, CoreLoop·StageProgression 순수 규칙, `GameScene.unity`는 변경하지 않는다.

## DX-M10: 계약 악마 카드 이동 계약

- `CodexDemonCardPreviewView`는 `IPointerClickHandler`를 구현하고 렌더링된 `DefinitionKey`를 보관한다. 왼쪽 버튼만 `Clicked` 이벤트를 발생시키며 다른 버튼은 무시한다.
- `CodexOverlayView`는 생성한 계약 카드의 클릭 이벤트를 구독하고 `DemonPageRequested(string definitionKey)`로 올린다. 카드 제거 시 구독을 해제하며 도감이 닫혔거나 페이지 전환 중이면 요청을 무시한다.
- `CodexController`는 요청 key와 `_demonPages.DefinitionKey`가 정확히 일치하는 인덱스를 찾고 `CodexNavigationState.TryShowDemonPage(int pageIndex)`를 호출한다. 성공 시 `Next` 방향 전환을 한 번 시작한다.
- `TryShowDemonPage`는 음수·범위 밖·현재 악마 페이지와 같은 인덱스를 거부하고 카테고리와 인덱스를 바꾸지 않는다. 성공 시 기존 적 페이지 인덱스를 보존하면서 악마 카테고리와 대상 인덱스를 함께 변경한다.
- 정의 key가 없거나 도감이 닫혔거나 전환 중인 요청은 실패하며 현재 표시 상태를 유지한다. 프리팹·Scene에 새 직렬화 참조를 추가하지 않는다.

## DX-M09: 악마 상세 정보 표시 계약

- `DemonCodexPageViewModel.EnglishName`은 `DefinitionKey.ToUpperInvariant()`로 생성한다.
- `CodexOverlayView`는 프리팹의 `EnglishName` TMP를 직렬화 참조로 받아 페이지 전환마다 현재 악마 영문명을 갱신한다.
- 골드·영혼 행은 기존 `CurrencyIconMarkup.GoldTag`·`SoulTag`와 보라색 숫자 규칙을 재사용한다. 표시 문구는 각각 `상점 구매 가격`, `계약 영혼`이며 아이콘 에셋은 변경하지 않는다.
- 액티브 스킬·대가 본문은 `ActiveSkill`·`Cost`만 렌더링하고 제목 문자열이나 선행 빈 줄을 덧붙이지 않는다.

## DX-M07: 탭과 카드 시각 개선

- 책장 전환 Fade는 `Book/FadingContent`의 `CanvasGroup`에만 적용한다. 닫기, 적·악마 페이지, Previous/Next는 이 그룹 아래에 두고 적 정보·악마 카드 탭은 `Book` 직속 형제로 유지해 항상 불투명하게 표시한다.
- 전환 중 두 탭 입력은 모두 잠근다. 전환 완료·취소·비활성화 때 현재 카테고리 탭은 비활성, 반대 카테고리 탭은 활성 상태로 복원한다. 기존 0.12초 Fade와 Remake 0~4의 프레임당 0.08초 재생 계약은 유지한다.
- 계약 악마는 Screen Space Overlay 전용 `CodexDemonCardPreview.prefab`과 `CodexDemonCardPreviewView`를 사용한다. 72×120 셀 안에서 앞면은 2px inset과 `PreserveAspect`를 사용하고, 하단 18px 영역에 `DefinitionKey.ToUpperInvariant()`로 만든 English Name을 8~14 자동 크기로 표시한다.
- 계약 Grid는 6열, 셀 72×120, 가로 간격 5, 좌우 패딩 3으로 고정해 보스 계약 6장을 한 행에 표시한다. 계약 카드에는 수량·Fallback·호버·선택 기능을 추가하지 않는다.
- 시작 덱은 공용 `DeckPreviewCard.prefab`, `xN`, 호버를 유지한다. 도감의 `DeckTemplate/Count` 중첩 override만 32pt로 사용하고 원본 프리팹과 덱 검사창의 64pt는 변경하지 않는다.
- Inspector 프리뷰 스냅샷은 계약 템플릿의 활성 상태, 앞면 Sprite, English Name을 저장·복원한다. `CodexOverlayView`의 기존 public API와 이벤트는 유지하고 `CodexDemonReferenceViewModel.EnglishName`만 추가한다.

## DX-M06: 공용 카드와 페이지 전환

- 페이지 안내는 왼쪽 `Q Previous`, 오른쪽 `{현재}/{전체} Next E`로 분리한다. 적 정보·악마 카드 탭의 비활성 Label은 기존 비활성 색상으로 어둡게 표시하며 배경 색상 전환도 유지한다.
- 닫기 버튼은 Normal alpha 0.5, Highlighted 1.0, Pressed 0.8을 사용한다.
- 계약 악마와 시작 덱은 `DeckPreviewCard.prefab` 중첩 인스턴스를 공용 템플릿으로 사용한다. 시작 덱은 카드 위 이름·랭크 Overlay 없이 앞면과 `xN`, 공용 호버 뱃지만 표시한다. 계약 악마는 수량·Fallback·호버·선택 프레임을 표시하지 않는다.
- 계약 Grid는 6열, 셀 70×112, 가로 간격 7, 좌우 패딩 4로 고정해 보스 계약 6장을 한 행에 표시한다. 시작 덱의 기존 4열 구성은 유지한다.
- `Q/E`와 탭 직접 선택은 동일한 전환 잠금을 사용한다. 콘텐츠 0.12초 Fade-out 뒤 다음은 Remake `0→1→2→3→4`, 이전은 `4→3→2→1→0`을 프레임당 0.08초 재생하고, 새 모델 렌더·0번 복원·0.12초 Fade-in 순으로 처리한다.
- Fade 대상 `Book`의 `CanvasGroup`은 펼친 책 배경과 Backdrop을 제외한 콘텐츠만 감싼다. 전환 중 상호작용과 Raycast를 차단하며 닫기·비활성화 때 Coroutine, 호버 뱃지, Sprite, alpha와 입력 잠금을 즉시 초기화한다.
- Inspector 프리뷰는 애니메이션 없이 즉시 렌더하고 저장 전후 스냅샷 복원 계약을 유지한다. `CodexOverlayView`의 기존 public API와 표시 모델은 변경하지 않는다.

## DX-M03: 미리보기 저장 수명주기

- `PrefabStage.prefabSaving`/`prefabSaved`와 `EditorSceneManager.sceneSaving`/`sceneSaved`을 저장 전 중단·저장 후 재개 쌍으로 처리한다.
- 재개 상태는 대상 `CodexOverlayView`, 카테고리, 적 페이지 인덱스, 악마 페이지 인덱스를 보존한다.
- 다른 프리팹이나 다른 Scene 저장은 활성 도감 미리보기에 영향을 주지 않는다.
- Play Mode 진입, Prefab Stage 종료, assembly reload, Editor 종료에서는 기존처럼 완전히 종료한다.

## DX-M02: 적 페이지 프리팹 계약

- `CodexOverlayView` 공개 API와 표시 모델은 유지한다. 영혼/골드 동적 TMP는 숫자만 출력한다.
- 초상화는 `RectMask2D` 뷰포트 안에서 비율 유지·중앙 크롭하고 `CodexFrame_0`으로 감싼다.
- 영혼/골드·설명·계약·덱 패널 외곽선은 Sliced `CodexOutline_0`을 사용한다.
- 시작 덱 `ScrollRect`는 세로 전용, Clamped, 스크롤바 없음이다. Content는 4열 고정 Grid, 셀 116×184, 간격 8×12, 좌우 패딩 8이다. 카드 아래 20px 수량 줄을 포함한다.
- 계약 카드는 `DeckPreviewCard.prefab` 템플릿과 6열을 사용하며 최대 6개를 1행으로 책 안에 표시한다.
- `GameScene`의 덱 Content 인스턴스 `m_SizeDelta.y`, `m_AnchoredPosition.y` 오버라이드는 제거한다.
- 프리팹 테스트는 아트 연결, Sliced 타입, 별도 아이콘, 필수 참조, Grid/ScrollRect 설정과 모델 렌더 값을 검증한다.

## DX-00: 콘텐츠 계약

`EnemyCombatProfileDefinitionSO`는 적 이름·초상화·등급·최대 영혼·처치 골드·행동 정책 key·시작 덱·설명·계약 악마·보스 고정 단계를 소유한다. `EnemyContentCatalogSO`는 적 SO 6개를 순서대로 참조하고 순수 `EnemyCombatProfileCatalog`과 `GoldRewardCatalog`을 생성한다.

`DemonCardDefinitionSO`는 기존 이름·앞면·가격·효과·대가와 함께 도감 서사 `codexLoreDescription`을 소유한다. `CardContentCatalogSO`가 악마별 서사 사전을 생성한다. 별도 도감 콘텐츠 SO는 사용하지 않는다.

## DX-01: 순수 표시 모델

`CodexPresenter`는 Unity 없는 읽기 전용 모델을 만든다.

- `EnemyCodexPageViewModel`: 이름, 프로필 key, 최대 영혼, 처치 골드, 설명, 계약 악마 목록, 묶인 시작 덱, 실제 총 장수 `StartingDeckCardCount`.
- `DemonCodexPageViewModel`: 이름, 정의 key, 구매 골드, 영혼 가격, 서사, 액티브 스킬, 대가.
- `CodexDeckCardViewModel`: 정의 key, 숫자, 표시 이름, 설명, suit, 1 이상 수량 `Count`.
- `CodexNavigationState`: 카테고리별 현재 인덱스와 책 전체 경계 이동. 적 마지막 장에서 `E`를 누르면 악마 카드 첫 장으로, 악마 카드 첫 장에서 `Q`를 누르면 적 마지막 장으로 이동한다. 책 전체의 첫 장과 마지막 장에서는 더 이동하지 않는다.

적 정보는 `EnemyContentCatalogSO`가 생성한 `EnemyCombatProfileCatalog`, `GoldRewardCatalog`과 `CardContentCatalog`을 읽는다. 악마 정보는 `DemonCardDefinitionSO`에서 생성한 카드 정의와 서사 사전을 읽는다. `CodexPresenter.CreateDemonPages`는 `DemonContractCatalog.PrototypeEnabledDemonKeys`의 순서와 범위만 사용한다. 제외 악마의 정의·서사·에셋은 도감 생성 조건이 아니다.

## DX-02: uGUI와 씬 연결

- `CodexOverlayView`: 전체 화면 Canvas, 차단막, 펼친 책, 닫기 버튼, 페이지 번호, 책갈피 두 개, 적/악마 페이지를 렌더링한다.
- 적 시작 덱은 `ScrollRect + GridLayoutGroup`, 계약 악마는 6열 미니 카드 그리드를 사용한다.
- 적 시작 덱 `ScrollRect`는 `Elastic` 경계를 사용하며 elasticity 0.1, 관성 활성, 감속률 0.135를 유지한다.
- 적 시작 덱은 원래 프로필 순서에서 suit를 확정하되 자동 발동 카드는 항상 스페이드로 고정한 뒤 `(DefinitionKey, Suit)`가 같은 카드만 묶는다. 개별 카드 ID는 표시 모델에 넣지 않으며 같은 숫자라도 정의가 다르거나 같은 정의라도 스페이드·클로버가 다르면 별도 항목이다.
- 시작 덱 모든 항목 아래 `xN` 수량을 항상 표시한다. 계약 악마 템플릿과 악마 상세 페이지에는 수량을 추가하지 않는다.
- 시작 덱 카드 호버는 `"{Rank}. {DisplayName}"` 제목과 카드 효과 설명을 `CardHoverBadgeRequest`로 전달해 GameHUD 공용 카드 호버 뱃지를 사용한다. 뱃지는 카드 우측 중앙에 왼쪽 중앙 피벗으로 붙고, 공용 `DeckPreviewCardView`의 `Deck Card Hover Badge Offset`을 카드 로컬 UI 좌표로 더한다.
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
- 페이지 변경·카테고리 전환·닫기·비활성화 때 현재 호버 항목이 소유한 GameHUD 뱃지만 즉시 해제한다.
- `GameFlowController`는 Combat/Shop에서만 `SetAvailable(true)`를 전달한다.

## 검증 계약

- 적 6종, 프로토타입 악마 6종이 누락·중복 없이 생성된다.
- 프로토타입 제외 악마의 정의나 서사가 남아 있어도 해당 도감 페이지는 생성되지 않는다.
- 현재 영혼·골드·설명·계약 목록·덱 순서와 카드 가격·효과·대가가 일치한다.
- 시작 덱의 묶음 수와 `StartingDeckCardCount`가 각각 표시 항목 수와 실제 카드 장수에 일치하고, 모든 묶음 수량이 1 이상이다.
- 카테고리를 직접 전환하면 카테고리별 마지막 페이지를 복원하고, `Q/E` 순차 이동은 적→악마 카드 경계를 끊김 없이 넘는 것이 보장된다.
- 프리팹 필수 버튼, ScrollRect, 두 카드 템플릿과 테이블 책 Collider가 존재한다.
- 에디터 프리뷰 전후 카드 템플릿 수는 2개로 유지되고, 프리뷰 종료 시 원래 표시값과 활성 상태가 복원된다.
- 도감 프리팹의 모든 RectTransform 앵커가 표준 preset이며 루트를 제외한 고정 앵커 요소는 양수 Width/Height를 가진다.
- GameScene missing script 0, Console Error 0, 전체 EditMode 실패 0을 유지한다.
- `CardContentBootstrap`은 카드 SO를 먼저 설치한 뒤 적 SO에서 순수 적·골드 카탈로그를 생성하고 설치한다. 순수 계층의 기본 카탈로그는 EditMode 테스트 호환 fallback으로만 유지한다.
