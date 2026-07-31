# 테이블 도감 구현 계획

1. DX-00: 적 프로필 SO, 악마 카드 SO의 도감 서사와 AI 생성 책·책갈피 Sprite를 추가한다.
2. DX-01: 순수 Presenter, 적/악마 페이지 모델, 카테고리별 navigation을 구현하고 테스트한다.
3. DX-02: uGUI 도감 프리팹과 테이블 책 프리팹을 만들고 GameScene에 직렬화 연결한다.
4. DX-02 보강: 기존 카드 템플릿만 활성화하는 Scene/Prefab Mode 전용 Inspector 프리뷰와 저장 전 복원 생명주기를 추가하고, 도감 RectTransform의 custom 앵커를 표준 preset으로 정규화한다.
5. DX-03: GameManager·GameFlowController 입력/가용성 생명주기를 연결한다.
6. 대상 테스트, 전체 EditMode, GameScene validate, 720p/1080p 수동 순회를 검증한다.

적 정보는 `EnemyContentCatalogSO`, 악마 정보는 기존 `CardContentCatalogSO`에서 읽으며 도감 전용 콘텐츠 카탈로그는 만들지 않는다.
