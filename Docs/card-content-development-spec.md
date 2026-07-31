# 카드 콘텐츠 개발 명세

## CC-00: 순수 런타임 카탈로그

`CardContentCatalog`은 normal/demon key 조회와 숫자별 기본 덱 정의 조회를 제공한다. 중복 key, 누락 또는 중복 기본 숫자 카드는 생성 시 거부한다.

## CC-01: Unity 저작 데이터

`NormalCardDefinitionSO`, `DemonCardDefinitionSO`, `CardContentCatalogSO`를 `Assets/01. Scripts/Content/`에 둔다. 카탈로그 SO는 모든 카드 SO를 참조하고 런타임 순수 카탈로그로 변환한다. 악마 SO는 도감 서사도 함께 소유한다. 잘못된 문자열, 비용, enum, Sprite, 빈 도감 서사는 검증 실패다.

## CC-02: 실행 경로 연결

`CardContentBootstrap`은 각 진입 씬에서 카드 카탈로그 SO를 먼저 변환·설치하고, 적 콘텐츠 SO가 생성한 적·골드 카탈로그도 순수 Catalog facade에 설치한다. CoreLoop와 StageProgression은 UnityEngine을 참조하지 않는다.

## CC-03: 상점·표시 전환

상점은 카드 정의의 기본 가격과 가중치를 읽는다. GameScene 카드 View는 SO의 Sprite를 조회하고, 카드 설명은 정의의 한국어 설명을 사용한다.

## CC-04: 검증 계약

- `OnValidate`와 런타임 변환은 빈 이름/설명, 음수 가격·영혼 비용, 0 이하 가중치, 잘못된 rank·enum, 누락 Sprite를 거부한다.
- `CardContentCatalog`은 normal/demon 각각의 중복 key와 숫자별 기본 카드의 누락·중복을 거부한다.
- 모든 초기 가격은 3, 모든 초기 가중치는 1이다. 따라서 기존 결정적 상점 결과와 기본 가격 동작은 유지된다.
- `prototype-v2` 저장의 key 검증은 설치된 카탈로그를 통해 계속 수행한다. key와 규칙 kind가 같으므로 저장 revision은 올리지 않는다.
- `GameSceneCardVisualCatalog`의 key→Sprite switch와 카드 효과/악마 텍스트 하드코딩은 제거한다.

## 테스트

27개 에셋의 key·가격·가중치·효과·대가·기본 덱 지정, Sprite 조회, 필드 검증 실패, 카드별 상점 가격, 기존 저장 key 호환성, 전체 EditMode 회귀를 확인한다.
