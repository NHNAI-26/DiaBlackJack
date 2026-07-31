# 테이블 도감 진행 기록

| 작업 | 상태 | 결과 |
| --- | --- | --- |
| DX-00 | 완료 | 적 초상화·악마 서사 `CodexContentCatalogSO`와 펼친 책 1536×1024, 닫힌 책·책갈피 1024×1024 투명 PNG를 추가했다. |
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
