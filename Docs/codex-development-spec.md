# 테이블 도감 개발 명세

## DX-00: 콘텐츠 계약

`CodexContentCatalogSO`는 적 프로필 key별 초상화와 악마 정의 key별 서사 설명을 소유한다. 현재 적 프로필과 카드 카탈로그를 기준으로 누락 key, 중복 key, 빈 설명, 빈 Sprite를 거부한다.

## DX-01: 순수 표시 모델

`CodexPresenter`는 Unity 없는 읽기 전용 모델을 만든다.

- `EnemyCodexPageViewModel`: 이름, 프로필 key, 최대 영혼, 처치 골드, 설명, 계약 악마 목록, 시작 덱.
- `DemonCodexPageViewModel`: 이름, 정의 key, 구매 골드, 영혼 가격, 서사, 액티브 스킬, 대가.
- `CodexDeckCardViewModel`: 정의 key, 숫자, 표시 이름, suit.
- `CodexNavigationState`: 카테고리별 현재 인덱스와 경계 이동.

적 정보는 `EnemyCombatProfileCatalog`, `GoldRewardCatalog`, `CardContentCatalog`을 읽는다. 악마 정보는 `CardContentCatalog`과 도감 서사 카탈로그를 읽는다.

## DX-02: uGUI와 씬 연결

- `CodexOverlayView`: 전체 화면 Canvas, 차단막, 펼친 책, 닫기 버튼, 페이지 번호, 책갈피 두 개, 적/악마 페이지를 렌더링한다.
- 적 시작 덱은 `ScrollRect + GridLayoutGroup`, 계약 악마는 3열 미니 카드 그리드를 사용한다.
- `CodexController`: 열림 상태, 카테고리별 마지막 페이지, `Q/E`, 책갈피, 닫기를 소유한다.
- `CodexClickable`: GameManager의 기존 포인터 raycast로 여는 테이블 책 표식이다.
- 기준 해상도는 1920×1080이며 Canvas Scaler와 앵커로 1280×720을 지원한다.

## DX-03: 입력·생명주기

- 도감 열기 전에 덱 미리보기를 닫는다.
- 도감이 열린 동안 GameManager 전투·상점·HUD 입력과 카메라 전환을 막는다.
- `PauseSettingsController`가 호출하는 `TryCloseTransientOverlay()`는 도감을 덱 미리보기보다 먼저 닫는다.
- 전투/상점 이탈, GameManager 비활성화·재바인딩 때 도감을 닫고 임시 카드 슬롯을 정리한다.
- `GameFlowController`는 Combat/Shop에서만 `SetAvailable(true)`를 전달한다.

## 검증 계약

- 적 6종, 악마 12종이 누락·중복 없이 생성된다.
- 현재 영혼·골드·설명·계약 목록·덱 순서와 카드 가격·효과·대가가 일치한다.
- 페이지 경계와 카테고리별 마지막 페이지 복원이 보장된다.
- 프리팹 필수 버튼, ScrollRect, 두 카드 템플릿과 테이블 책 Collider가 존재한다.
- GameScene missing script 0, Console Error 0, 전체 EditMode 실패 0을 유지한다.
