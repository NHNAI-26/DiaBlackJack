# 테이블 도감 진행 기록

## DX-M12: 현재 상대 적 페이지 자동 표시

- `CodexNavigationState.TryShowEnemyPage`와 `CodexController.Open(string enemyProfileKey)`를 추가했다. 도감을 열 때 적 탭과 현재 전투 적 페이지를 다시 선택하며, null·공백·대소문자 불일치·미등록 key와 비전투 호출은 적 도감 1페이지로 대체한다.
- `GameManager` 도감 클릭은 `Battle`이 있을 때 `CurrentEnemyProfileKey`, 없을 때 기존 인자 없는 `Open()`을 사용한다. 도감을 연 뒤 Q/E·탭·계약 악마 바로가기는 기존 동작을 유지한다. 프리팹·Scene·직렬화 참조는 변경하지 않았다.
- 신규 `DXM12` 3/3(job `b666e54026b04f7d894396c326ba0bfb`)가 통과했다. 인접 `CodexPresentationTests`·`CodexAssetTests`는 33/37(job `ee01f55a96d84d9eb146b3abac64c203`)이며 실패 4건은 이번에 수정하지 않은 기존 Overlay 카드 셀 80×150 대 기대 72×120, English Name 최소 크기 1 대 기대 8, 수량 글자 35 대 기대 32, 테이블 책 위치 `(1.39, 0.08, 0.02)` 대 기대 `(2.22, 0.08, -0.34)` 불일치다.
- C# 컴파일 직후 Console Error는 0이고 GameScene validation은 issue 0, missing script 0, broken prefab 0이다. Play Mode exec에서 enforcer와 final-boss가 각각 일치 페이지를 열고, 비전투 기본 호출이 적 탭 0번 gunslinger 페이지를 여는 것을 확인했다. Play Mode·MainMenu 복원 뒤 범위 밖 기존 missing script와 Material Drawer 오류가 남았다.
- Play Mode 종료 뒤 초기 `MainMenuScene`으로 복원했다. 실행 중 나타난 다른 세션의 `GameManager` 리볼버 입력 대기, `CoreLoopPresentationTests.cs`, card-use·scene-presentation 문서 변경은 보존했으며 이 작업에서 수정하지 않았다.

## DX-M11: 테이블 도감 3D 책과 월드 호버

- 테이블 닫힌 도감의 2D SpriteRenderer를 `antique_book.fbx`·`MAT_Book` 3D 모델로 교체했다. 기존 `CodexClickable` 루트와 클릭 연결은 유지하고 모델 bounds 기준 콜라이더, 테이블 position `(2.224, 0.077, -0.339)`, rotation `(0, 8, 0)`, scale `(1, 1, 1)`을 적용했다. `GameScene.unity`는 저장하지 않았다.
- 도감 호버는 플레이어 덱과 같은 `PostProcessOutlineRegistry`, 머티리얼 주황색, 4px 폭을 사용한다. 스케일·효과음·툴팁은 추가하지 않았고 포인터 이탈, 입력 잠금, 모달·선택 UI·오버레이 전환, 비활성화·파괴 때 등록을 해제한다.
- 신규 `DXM11` 2/2(job `fdff46c42b12492884d486e71cda51e8`)가 통과했다. 인접 `CodexAssetTests`·`ShaderStencilOutlineTests`는 39/42(job `5d39627ddaf046488cea2f81d24f3e89`)이며 실패 3건은 이번에 수정하지 않은 기존 Overlay의 악마 카드 셀 80×150 대 기대 72×120, English Name 최소 크기 1 대 기대 8, 수량 글자 35 대 기대 32다.
- GameScene validation은 issue 0, missing script 0, broken prefab 0이다. 1280×720·1920×1080 Play Mode에서 책이 테이블 위 계획 위치·크기로 보이고 주황색 4px 외곽선이 표시되는 것을 확인했다. 호버 해제 뒤 Registry 대상 0건, `CodexController.Open()` 성공·책 숨김, 닫기 뒤 책 복원을 확인했다. `GameScene.unity`는 저장하지 않았다.
- DX-M11 대상 컴파일·테스트 시점의 C# 컴파일 오류는 0이었다. 최종 확인 시 병행 작업의 미추적 `GameSceneOpponentSelectionViewTests.cs`에서 TMPro 어셈블리 참조 오류 2건이 새로 발생했으며, 범위 밖 파일이라 수정하지 않았다.

## DX-M10: 계약 악마 카드 바로가기

- 적 정보의 계약 악마 카드가 왼쪽 클릭된 `DefinitionKey`를 Overlay 이벤트로 전달하고, Controller가 악마 도감 페이지 목록의 실제 인덱스를 찾아 기존 다음 방향 책장 전환 한 번으로 바로 이동하도록 연결했다. 오른쪽 클릭, 없는 key, 범위 밖·중복 인덱스, 닫힘·전환 중 요청은 상태를 바꾸지 않는다.
- 신규 `DXM10` 테스트는 4/4(job `a9f0a6ad685c48f3a0dd20988fbfa6ec`) 통과했다. 인접 `CodexPresentationTests`·`CodexAssetTests`는 30/33(job `6e8a79af219549038e4c9272d13d9164`)이며 실패 3건은 기존 `ContractTemplate` 너비 0, 덱 셀 100×150 대 기대 116×184, 비활성 탭 alpha 0.8235 대 기대 0.5의 병행 프리팹 변경이다. 해당 변경은 되돌리지 않았다.
- C# 컴파일 오류와 최종 Console Error는 0이고 GameScene validation은 issue 0, missing script 0, broken prefab 0이다. 공유 Editor에서 다른 세션의 Play Mode·테스트가 반복되어 1280×720·1920×1080 수동 확인은 완료로 기록하지 않는다.

## DX-M09: 악마 상세 정보 문구 정리

- 악마 페이지의 새 `EnglishName` TMP에 정의 key 대문자 영문명을 연결했다. 구매·계약 행은 각각 `상점 구매 가격 + 기존 골드 아이콘 + 숫자`, `계약 영혼 + 기존 영혼 아이콘 + 숫자`로 바꾸고 아이콘 에셋은 유지했다. 액티브 스킬·대가 본문에서는 중복 제목과 선행 빈 줄을 제거했다.
- 최종 변경 테스트 2/2(job `2f31b18d6c8845eebc6e062c96b9a1da`)가 통과했다. 인접 `CodexPresentationTests`·`CodexAssetTests`는 26/29(job `71e1ff752e3a449fb6770d1f13d94c29`)이며 실패 3건은 병행 프리팹 편집값인 카드 크기 100×150 대 기존 기대 116×184 두 건과 비활성 탭 alpha 0.8235 대 기존 기대 0.5 한 건이다. 해당 레이아웃·색상은 되돌리지 않았다.
- GameScene validation은 issue 0, missing script 0, broken prefab 0이다. 컴파일 오류는 0이며 Console에는 Test Framework 결과 저장 로그 1건과 기존 URP Material Drawer 오류 1건이 남았다. 공유 Editor의 다른 테스트를 방해하지 않기 위해 1280×720·1920×1080 수동 확인은 완료로 기록하지 않는다.

## DX-M08: 공용 픽셀 호버 외곽선

- 도감 시작 덱의 공용 `DeckPreviewCardView`와 계약 악마의 전용 `CodexDemonCardPreviewView`에 전투 카드와 같은 상태색 픽셀 실루엣 외곽선을 적용했다. 계약 악마는 툴팁 없이도 기본 흰색 외곽선을 표시하고, 일반 카드는 기본/패시브 흰색·수동 사용 불가 빨강·수동 사용 가능 초록·자동 HDR 노랑·사용 완료 회색을 사용한다.
- 슬롯마다 전용 UI 머티리얼 인스턴스를 만들어 외곽선 visibility·색·스프라이트 UV가 다른 슬롯에 번지지 않게 했으며, 기존 금색 사각 `HoverFrame`은 제거하고 선택 프레임은 유지했다.
- 신규 도감·덱 외곽선 검증을 포함한 5/5(job `560a9e487c97402a8cd67b0cf9de87a3`)가 통과했다. 전체 EditMode는 1066/1072(job `05c1856140ec4523b0e842ad646b767c`)이며 도감 관련 잔여 실패 1건은 병행 편집 중인 `PortraitFrame` 스프라이트 상태다. GameScene validation 0 issues와 C# 컴파일 오류 0을 확인했다. 최종 Console에는 기존 셰이더·URP Material Drawer 오류 2건이 남았고 1280×720·1920×1080 수동 확인은 공유 Editor 사용 때문에 완료로 기록하지 않는다.

## DX-M07: 도감 탭·카드 시각 개선

- 책장 Fade용 `CanvasGroup`을 `Book/FadingContent`로 옮기고 닫기·적/악마 페이지·Previous/Next만 그 아래에 배치했다. 두 탭은 `Book` 직속으로 유지하며 전환 중 양쪽 입력을 잠그고 완료·취소 때 현재 카테고리에 맞게 복원한다.
- 계약 악마를 72×120 전용 `CodexDemonCardPreview.prefab`으로 교체했다. 앞면은 2px inset과 비율 유지를 사용하고 하단에 정의 key의 대문자 English Name을 8~14 자동 크기로 표시한다. Grid는 6열, 간격 5, 좌우 패딩 3이며 레거시 `DeckPreviewCard` 계약 템플릿은 제거했다. 수량·Fallback·선택·툴팁은 없고 후속 DX-M08의 픽셀 외곽선만 함께 유지한다.
- 시작 덱은 공용 `DeckPreviewCard.prefab`, `xN`, 호버를 유지하며 도감 중첩 `Count`만 32pt로 축소했다. 원본 프리팹과 덱 검사창은 64pt다. Inspector Preview는 계약 카드의 활성 상태·앞면·영문명을 저장 전후 복원한다.
- DX-M07 및 Preview 회귀 8/8(job `28a5349006504ac5a683e9b7aeabb0f9`), Codex·DeckPreview 대상군 36/36(job `c7a7e454d86d41d8bc32be040ca6ae92`)가 통과했다.
- 전체 EditMode는 CoreLoop 788/793(job `8d2c9784ccb44a818dd7e79bdaf6f1f6`)과 StageProgression·Settings 279/279(job `2948390179804192b5a2c24cbee4c1dc`)다. 실패 5건은 적 설명·전투 카드 외곽선 팔레트·해머 대상·호버 오프셋·대사 중복의 병행 영역이다. GameScene validation issue 0, missing script 0, broken prefab 0과 컴파일 오류 0을 확인했고 Console을 비운 뒤 Error 0이다.
- Play Mode에서 Controller·View·Overlay Canvas가 열린 상태와 탭 잠금 상태는 런타임으로 확인했다. 자동 Game View 캡처는 Screen Space Overlay를 포함하지 않았고 Editor 해상도도 2560×1440에서 고정되어 1920×1080·1280×720 육안 검증은 완료로 기록하지 않는다.

## DX-M06: 공용 카드·페이지 전환 개선

- 페이지 문구를 `Q Previous`와 `{현재}/{전체} Next E`로 분리하고, 닫기 버튼 alpha와 비활성 탭 Label 색상을 확정했다.
- 시작 덱·계약 악마 템플릿을 실제 `DeckPreviewCard.prefab` 중첩 인스턴스로 교체했다. 시작 덱은 `xN`과 공용 호버를 유지하고 이름·랭크 Overlay를 제거했으며, 계약 악마는 수량·Fallback·호버·선택 표시를 끈다. 사용처가 사라진 `CodexCardThumbnailView`는 제거했다.
- 계약 Grid를 70×112 셀의 6열로 조정해 최종 보스 계약 6장을 한 행에 배치했다.
- `Book` 콘텐츠 `CanvasGroup`과 Remake 0~4 Sprite를 연결하고, Q/E·탭 전환에 0.12초 Fade-out → 방향별 5프레임(각 0.08초) → 렌더·0번 복원 → 0.12초 Fade-in을 적용했다. 전환 중 입력·Raycast를 차단하고 닫기·비활성화 때 전환·호버·표시 상태를 초기화한다.
- 신규 DXM06 5건은 최종 job `3a06782e726b4f4b8f291eea49506df0`에서 5/5 통과했다. Codex 표시 모델·덱 미리보기 포함 대상 job `2e38823b97a94f41a1746c77833dadf0`은 24/24 통과했다.
- 전체 EditMode job `a2d40697a4bf4f94b5db8d04894589dc`는 1059개 중 1054개 통과, 5개 실패다. DX-M06 외 기존/병행 자산·표시 상태 실패 4건과, 열려 있던 프리팹 편집 상태에서 `PortraitFrame`이 `circle_brush`로 저장된 Codex 기존 아트 계약 실패 1건이다. 해당 동시 편집 상태는 덮어쓰지 않았다.
- GameScene validation은 override 정리 전 issue 0, missing script 0, broken prefab 0이고 컴파일 및 최종 Console Error는 0이다. 병행 작업의 GameScene 저장 완료와 dirty=false를 확인한 뒤 Codex 루트/name/font override는 유지하고, 덱 Content의 낡은 높이·Y 위치 override 2개만 제거해 재임포트했다. 정리 후 validation 재호출은 MCP bridge timeout으로 결과를 받지 못했다. 1920×1080·1280×720 Play Mode 수동 검증은 동시 에디터 사용을 방해하지 않기 위해 수행하지 않았다. 앞서 만든 미저장 씬 진단 복사본은 `Temp/DXM06_GameScene_DirtyBackup.unity`에 보존했다.

## DX-M05: 프로토타입 악마 도감 범위 정리

- 도감 악마 페이지를 전체 12종이 아닌 `DemonContractCatalog.PrototypeEnabledDemonKeys`의 6종만 생성하도록 변경했다.
- 제외 악마의 정의·SO·에셋은 삭제하지 않았다. 프로토타입 출현 범위와 도감 표시 범위만 일치시켰다.
- 구현·검증 책임자는 이천서이며 AI는 코드 추적·테스트·문서 동기화를 보조했다.
- 대상 EditMode job `917cb1d9f68640b09f932ae8cdb19ae3` 17/17 통과. 전체 CoreLoop EditMode job `8d3dacde7ae541e4abed3f7c1ec2fcf0`은 750개 중 743개 통과했으며 실패 7건은 기존 UI·에셋 회귀다. 컴파일과 Console Error는 0건이다.

## DX-M04: 시작 덱 수량 묶음·공용 호버 뱃지

- 도감·덱 검사 관련 필터 EditMode job `6692a6688e1c4b2f80172ef66d0dbf8f` 58/58, 전체 EditMode job `52f09303e89347fb894d757610bc69b8` 907/907 통과.
- GameScene validation issue 0, missing script 0, broken prefab 0. 검증 종료 후 Console을 비우고 재확인한 Error 0.
- Play Mode 1920×1080·1280×720에서 도감·뽑을 카드·버린 카드 창을 각각 확인했다. `x1`/`x2`, 수량 줄 간격, 스크롤 영역, 오버레이 위 공용 뱃지, 화면 내 상·하 방향 배치가 보였고 도감 닫기 뒤 헤더·본문 뱃지 비활성화를 확인했다.
- 후속으로 방향별 오프셋을 폐기하고 도감·덱 카드 전용 `Deck Card Hover Badge Offset` 하나를 노출했다. 덱 카드 우측 중앙과 툴팁 왼쪽 중앙 피벗 결합, 로컬 오프셋 적용을 `GameSceneDeckPreviewTests` job `5d97c1bbac9d4a76a36a00b8197beef8` 5/5로 확인했다. 테스트 종료 때 기존 Test Framework/URP Material Drawer 로그가 기록됐고 Console을 비운 뒤 신규 Error 0을 확인했다.

## DX-M03: 편집 중 미리보기 유지

- Prefab Mode Auto Save가 오브젝트 이동 때 `prefabSaving`을 발생시키며 미리보기를 종료하던 동작을 수정했다.
- 저장 직전 authored 값을 복원하고 저장 완료 후 같은 카테고리·페이지로 새 스냅샷을 생성해 자동 재개한다. Scene 저장도 같은 수명주기를 사용한다.
- 레이아웃 위치 수정값이 중단·재개 뒤 유지되는 테스트 `DX02_U07_EditorPreviewResumesAfterSaveAndKeepsLayoutEdit`를 추가했다.
- Codex Asset tests: job `46ba5e665dbb443c9dabfadc23552277`, 13/13 통과. Codex 관련 컴파일/Console Error 0.

## DX-M02: 적 정보 페이지 레이아웃 개편

- `CodexOverlay.prefab` 적 페이지만 재배치했다. 초상화 `CodexFrame_0`, 제목 `CodexFrame_2`, Sliced `CodexOutline_0`, 별도 `SoulIcon`/`GoldIcon` 연결을 완료했다.
- 왼쪽 계약 영역은 3열×2행 최대 6개가 책 안에 들어오도록 조정했다. 오른쪽은 116×164, 4열, 8×12 간격, 세로 전용 ScrollRect와 `RectMask2D`를 적용했다.
- `CodexOverlayView`의 영혼/골드 출력은 숫자 전용으로 변경했다. 공개 API와 도감 표시 모델은 변경하지 않았다.
- `GameScene`의 덱 Content `m_SizeDelta.y`, `m_AnchoredPosition.y` 인스턴스 오버라이드만 제거했다.
- Codex Asset tests: job `bd308aa51364466e8809a016550bc14c`, 12/12 통과. Codex Presentation tests: job `8542e5492e834b4094e0333f554cd74f`, 6/6 통과.
- 전체 EditMode: job `81adb8024f934f838f4628c19dcf0905`, 880개 중 878개 통과. 실패 2개는 비도감 기존 영역 `GSH01_U10_TablePrefabAuthorsThreeWorldCommands`, `MOO01_U04_WindowGlowUsesPropertyBlockOnly`이다.
- GameScene validate: issue 0, missing script 0, broken prefab 0. Codex 필터 Console Error 0.
- 1920×1080과 1280×720에서 최종 보스 25장 덱의 상단·하단, 6개 계약 카드, 책 바깥 노출 없음과 스크롤 위치를 확인했다.

## 2026-08-09 도감 스크롤 경계 보완

- 시작 덱 스크롤 경계를 `Clamped`에서 `Elastic`으로 바꿨다. 기존 elasticity 0.1, 관성, 감속률 0.135와 세로 전용 입력은 유지했다.
- 관련 에셋 테스트에서 새 경계와 기존 관성 값을 확인했다. 전체 EditMode는 1,304/1,324이며 잔여 20건은 기존 자산·UI 기대값 불일치다.

| 작업 | 상태 | 결과 |
| --- | --- | --- |
| DX-00 | 완료 | 적 6종을 개별 `EnemyCombatProfileDefinitionSO`로 이전하고 악마 서사를 각 `DemonCardDefinitionSO`에 통합했다. 별도 `CodexContentCatalogSO`는 제거했다. 펼친 책 1536×1024, 닫힌 책·책갈피 1024×1024 투명 PNG를 사용한다. |
| DX-01 | 완료 | 순수 Presenter와 navigation을 구현했다. 적 6종·악마 12종, 가격·설명·덱·경계 테스트가 통과했다. |
| DX-02 | 완료 | 도감·테이블 책 프리팹을 생성하고 GameScene에 설치했다. 1920×1080과 1280×720에서 적/악마 페이지와 테이블 책을 캡처해 잘림 없는 배치를 확인했다. |
| DX-03 | 완료 | GameManager 입력 차단, 덱 미리보기 상호 배제, Escape 우선 닫기, GameFlow 화면별 가용성, 강제 닫기·정리를 연결했다. |
| DX-M01 | 완료 | `Q/E` 페이지 이동이 적 마지막 장과 악마 카드 첫 장 사이를 연속해서 넘도록 수정했다. 책 전체 첫 장·마지막 장의 정지는 유지하고 런타임·에디터 프리뷰 경계 표시를 일치시켰다. |

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
- DX-M01 도감 navigation job `2167f897d3ec48ddbeb39f87a81850e9` 6/6 통과. 도감 자산 job `0f67f04198d64980a2cd1d32e1d49a0d`는 11개 중 10개, 전체 EditMode job `f7b5af3e3d20467bbfbfc3bd2017b710`은 859개 중 857개 통과했다. 잔여 2건은 기존 도감 오버레이 좌표 기대값과 창문 광원 색상 정밀 비교 실패이며 이번 navigation 변경과 무관하다. 컴파일 및 Console Error 0.
