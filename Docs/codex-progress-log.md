# 테이블 도감 진행 기록

| 작업 | 상태 | 결과 |
| --- | --- | --- |
| DX-00 | 완료 | 적 6종을 개별 `EnemyCombatProfileDefinitionSO`로 이전하고 악마 서사를 각 `DemonCardDefinitionSO`에 통합했다. 별도 `CodexContentCatalogSO`는 제거했다. 펼친 책 1536×1024, 닫힌 책·책갈피 1024×1024 투명 PNG를 사용한다. |
| DX-01 | 완료 | 순수 Presenter와 navigation을 구현했다. 적 6종·악마 12종, 가격·설명·덱·경계 테스트가 통과했다. |
| DX-02 | 완료 | 도감·테이블 책 프리팹을 생성하고 GameScene에 설치했다. 1920×1080과 1280×720에서 적/악마 페이지와 테이블 책을 캡처해 잘림 없는 배치를 확인했다. |
| DX-03 | 완료 | GameManager 입력 차단, 덱 미리보기 상호 배제, Escape 우선 닫기, GameFlow 화면별 가용성, 강제 닫기·정리를 연결했다. |

## 검증

- 도감 대상 EditMode: job `6ed4572e2b814f14ba9a91f99b79464f`, 14/14 통과.
- StageProgression: job `cf6859d8584141038d11589f3a98d47e`, 260/260 통과.
- CoreLoop: job `ee6ea723a62a4cc8a95a59b52522c4c3`, 559개 중 557개 통과. 도감 밖에서 동시에 수정 중인 사용 카드 표시 `CUM10_U04`, `CUM10_U05` 두 테스트가 실패했다.
- 전체 EditMode: job `5f1141e138654367a5913f496f5c7ac5`, 833개 중 같은 2개만 실패했다. 해당 소유자의 `CardView`·카드 표시 작업은 수정하거나 되돌리지 않았다.
- GameScene validate: issue 0, missing script 0, broken prefab 0. 검증 직후 Console Error 0.
- Play Mode에서 `Time.timeScale == 1`을 유지한 채 적/악마 페이지를 열었고, 6개·12개 경계 순회와 닫힌 책 숨김을 확인했다.
- 임시 화면 캡처와 중복 생성본은 검증 후 제거했다. 실제 사용 아트는 `Assets/05. Arts/Texture/Codex/`에 보존했다.
- 오류 정리 후 도감 EditMode job `13c3795a09be4a9d96947350d478031f` 14/14 통과. GameScene validate issue 0, 최종 Console Error 0을 확인했다.
- SO 소유권 정규화 후 도감 job `b3a461ef6984439da873a38515d2d0e1` 14/14, 전체 EditMode job `61923d5755144c44b61620298ebe917f` 842/842 통과. GameScene validate issue 0, missing script 0, broken prefab 0, 최종 Console Error 0을 확인했다.
- 적·악마 카테고리 각각에 책 면 크기의 `LeftPage`와 `RightPage` RectTransform을 추가하고 기존 표시 요소를 해당 면 아래로 재배치했다. 화면상 배치와 직렬화 참조 유지 확인 후 `CodexAssetTests` job `45f56d8e930d4c0a9d1432c9a5d95c0b` 10/10 통과.
- Prefab Stage 편집을 방해하던 도감 에디터 프리뷰 기능과 전용 Custom Editor·테스트를 제거했다. 남은 `CodexAssetTests` job `98318796680d432ba7f20bbeef743a81` 9/9, 전체 EditMode job `2cd615fda05646d78be5e4944565d631` 849/849 통과.
- 자동 생성 없이 기존 계약·덱 템플릿을 1장씩 활성화하는 수동 Inspector 프리뷰를 다시 추가했다. 표시값 스냅샷 복원과 페이지 경계를 검증한 신규 테스트 job `ab830db5014749eaa79433c9f61fc2ee` 2/2, `CodexAssetTests` job `950d08687afe4b93981415ec2dc9f268` 11/11 통과. 컴파일 후 Console Error 0을 확인했다.
- 전체 EditMode job `f9ebbd0efd374c8eafb8d83f1f3d13b2` 851/851 통과. Prefab Mode에서 프리뷰 중 `DeckTemplate`을 선택해도 세션이 유지되고 카드 템플릿 수가 2개인 것을 확인했으며, 프리뷰 종료 뒤 Prefab Stage dirty false와 프리팹 SHA-256 불변을 확인했다. GameScene validate issue 0, missing script 0, broken prefab 0. 테스트 종료 과정의 기존 Material Drawer 오류 2건은 Console clear 후 idle에서 재발하지 않았고 최종 Error 0을 확인했다.
- 도감 프리팹의 custom RectTransform 32개를 가까운 고정 Anchor Preset으로 변환하고 기존 stretch preset은 유지했다. 기준 1920×1080 배치에서 크기·상대 위치를 보존했으며, 재임포트 후 custom 0개와 고정 요소의 양수 Width/Height를 확인했다. 사용자가 조정한 `DeckTemplate` top-left preset과 116×164 크기도 보존했다. `CodexAssetTests` job `4a3ae9566ee545548ef9e16c8102e036` 11/11, 전체 EditMode job `d953d1a6fc9a47029053744d321b9205` 851/851 통과. GameScene validate issue 0, missing script 0, broken prefab 0, 최종 Console Error 0.
