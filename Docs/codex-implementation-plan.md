# 테이블 도감 구현 계획

## DX-M05: 프로토타입 악마만 도감에 표시

1. 완료: 악마 페이지 생성 범위를 `PrototypeEnabledDemonKeys`로 제한한다.
2. 완료: 제외 악마 정의·서사가 존재하거나 일부 서사가 없어도 프로토타입 6종만 생성되는 회귀 테스트를 추가한다.
3. 완료: 대상 17/17과 전체 CoreLoop EditMode 743/750을 검증했다. 실패 7건은 작업 전부터 존재한 비관련 UI·에셋 회귀다.

## DX-M03: 편집 중 미리보기 유지

1. 완료: Auto Save가 호출하는 저장 전 콜백의 강제 종료 원인을 확인했다.
2. 완료: 저장 직전 원본 복원과 저장 직후 동일 페이지 자동 재개를 Prefab·Scene 양쪽에 연결했다.
3. 완료: 레이아웃 위치 수정과 카테고리·페이지 보존 회귀 테스트를 추가했다.
4. 완료: Codex Asset EditMode 13개를 실행했다.

## DX-M02: 적 도감 레이아웃 개편

1. 완료: 사용자 제공 Codex 프레임·외곽선·영혼/골드 아이콘 슬라이스를 확인하고 기존 import 설정을 보존했다.
2. 완료: 적 페이지 왼쪽 정보 블록과 오른쪽 4열 덱 ScrollRect를 재배치했다.
3. 완료: 영혼/골드 TMP를 숫자 전용으로 바꾸고 페이지 렌더 시 덱 스크롤 최상단 초기화를 유지했다.
4. 완료: GameScene의 덱 Content 크기·위치 인스턴스 오버라이드 2개를 제거했다.
5. 완료: Codex 프리팹/렌더 테스트, 1920×1080·1280×720 상하단 스크롤 확인, 전체 EditMode와 GameScene validate를 실행했다.

1. DX-00: 적 프로필 SO, 악마 카드 SO의 도감 서사와 AI 생성 책·책갈피 Sprite를 추가한다.
2. DX-01: 순수 Presenter, 적/악마 페이지 모델, 카테고리별 navigation을 구현하고 테스트한다.
3. DX-02: uGUI 도감 프리팹과 테이블 책 프리팹을 만들고 GameScene에 직렬화 연결한다.
4. DX-02 보강: 기존 카드 템플릿만 활성화하는 Scene/Prefab Mode 전용 Inspector 프리뷰와 저장 전 복원 생명주기를 추가하고, 도감 RectTransform의 custom 앵커를 표준 preset으로 정규화한다.
5. DX-03: GameManager·GameFlowController 입력/가용성 생명주기를 연결한다.
6. 대상 테스트, 전체 EditMode, GameScene validate, 720p/1080p 수동 순회를 검증한다.

적 정보는 `EnemyContentCatalogSO`, 악마 정보는 기존 `CardContentCatalogSO`에서 읽으며 도감 전용 콘텐츠 카탈로그는 만들지 않는다.
