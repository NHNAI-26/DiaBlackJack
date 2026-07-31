# 카드 콘텐츠 진행 기록

| 작업 | 상태 | 결과 |
| --- | --- | --- |
| CC-00 | 완료 | Unity 없는 `CardContentCatalog`, 정의의 설명·가격·가중치·기본 덱 속성을 추가했다. |
| CC-01 | 완료 | 일반 15개, 악마 12개 SO와 통합 카탈로그 에셋을 만들고 한국어 문구·기존 key·Sprite를 이관했다. |
| CC-02 | 완료 | `CardContentBootstrap`을 StageTest, CoreLoopTest, GameScene에 배치하고 실행 전 변환·설치를 연결했다. |
| CC-03 | 완료 | 상점 가격/가중치, GameScene Sprite·설명/대가 표시를 카드 정의로 전환했다. |
| CC-04 | 완료 | Unity 6000.3.10f1 배치 EditMode 760개를 실행해 760개 통과·0 실패를 확인했다. 세 진입 씬의 Bootstrap 참조와 SO 변환도 EditMode에서 검증했다. |

- 2026-07-31 도감 콘텐츠 정규화: 악마 도감 서사를 각 `DemonCardDefinitionSO`로 이동하고 `CardContentBootstrap`에 적·골드 SO 카탈로그 설치를 연결했다.
- 연결 검증 job `f9899f673b014e6a9acf10a282efd38b` 31/31, 전체 EditMode job `61923d5755144c44b61620298ebe917f` 842/842 통과.
