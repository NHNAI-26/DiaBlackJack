# 팀원 역할 기술서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 버전: v0.1  
> 최종 갱신: 2026-08-07

## 1. 기록 목적

팀원별 계획 업무와 실제 구현 영역을 구분해 기록한다. 제출 시에는 아이디어 제안이 아니라 실제로 제작·수정·검증한 결과를 파일, 씬, 기능 또는 산출물 단위로 제시한다.

현재 저장소의 Git 작성자 기록에서는 **이천서(Cheonseo Lee)**, **Shim0Hwan**, **HONG**이 확인되었다. 역할 계획과 실제 커밋 기여는 구분하며, 커밋으로 확인되지 않은 기능을 완료로 추정하지 않는다.

### 2026-08-04 이천서 추가 구현 기록

이천서는 상점 위스키 연출의 실제 음용 시점에 기존 `drinkWhiskey` SFX가 직접 재생되도록 연결하고, 애니메이션 이벤트와의 중복 재생 가능성을 제거했다. 또한 라이터로 제거할 카드의 정의·무늬에 맞는 앞면 스프라이트가 소각 연출에 표시되도록 선택 정보와 연출 프리팹을 연결했다.

## 2. 팀 구성과 계획 역할

| 팀원 | 계획 역할 | 계획 업무 | 실제 완료 상태 |
| --- | --- | --- | --- |
| 이천서 | 프로젝트·기획 문서, 전투 코어·적 프로필·상대 선택·전투 정보 UI·정식 런·저장 통합 담당 | 규칙·구조 결정, Notion v0.7 적 6종 프로필·AI·카드 효과·전투 변환, 상대 선택·등급별 적 정보 UI, RF-01~RF-05 골드·상점·정식 런, 저장·이어하기 Runtime/UI, 테스트·완료 기록 관리 | EP-R06과 RF-01~RF-05 구현·자동 회귀·720p/1080p 실화면 검증 완료(AI 보조) |
| Shim0Hwan(Git 식별자) | 아트·레벨 환경 담당 | FBX·텍스처·재질·셰이더, 전투 공간·조명·분위기와 아트 리소스 관리 | 아트 오브젝트·셰이더·`LevelDesign` 씬·시네머신 패키지 관련 커밋 확인 |
| HONG(Git 식별자) | 정식 런 진행 후속 개발 계획 | RF 초기 분업 후보 | RF 범위는 이천서가 인수해 RF-01~RF-05 완료; HONG 실제 기여는 별도 커밋 기준으로 기록 |

위 역할은 제작 계획이며 실제 구현 기록이 아니다. 역할이 확정되거나 바뀌면 팀원 이름, 담당 범위와 변경 사유를 갱신한다.

## 3. 실제 기여 기록

### 3.1 이천서

| 날짜 | 영역 | 실제 수행 내용 | 관련 파일/산출물 | 검증/비고 |
| --- | --- | --- | --- | --- |
| 2026-08-02 | 적 거짓말 탐지기 UI 회귀 보정 | 적 거짓말 탐지기의 자동 선언 직전 중간 프레임이 플레이어 선택 패널로 표시되는 원인을 확인하고 GameScene HUD·CoreLoop IMGUI 표시를 차단했다. | `GameSceneCombatHudPresentation.cs`, `CoreLoopView.cs`, `GameSceneCombatHudPresentationTests.cs`, 자동 카드 기획·개발·진행 문서 | 이천서 기획·구현·검증 책임, AI 경로 추적·코드·테스트·문서화 보조. 관련 25/25, 전체 888/890 통과. 잔여 2건은 기존 프리팹 참조·광원 정밀 비교 실패. 씬·프리팹·외부 에셋·패키지 변경 없음. |
| 2026-07-31 | 상대 악마 7종 정합성 DC-R07 | 집행자의 파이몬 계약을 제거하고 보스 고정 쌍을 프로토타입 7종 안으로 교체했다. 바알제붑은 자기·상대 공개 카드 중 각각 최고 숫자를 버리고, 보스는 아스모데우스 강제 히트 선택을 실제 적 턴에서 해결하도록 정책을 개선했다. | `EnemyCombatProfileCatalog.cs`, `CultistEnemyPolicy.cs`, `FinalBossEnemyPolicy.cs`, 적 계약·보스·프로필 회귀, 규칙·계약·적 프로필·AI 활용 문서 | 이천서 기획·구현·검증 책임, AI 문서/코드 불일치 추적·코드·테스트·문서화 보조. 대상 64/64·전체 EditMode 821/821, 컴파일 오류 0. 외부 에셋·오픈소스·패키지·씬·프리팹 변경 없음. |
| 2026-07-31 | 시작 악마 확인 UI·마몬 차례 선택 정정 | 시작 악마 2장을 선택 없이 모두 지급한 상태로 공개하고 확인 버튼 뒤 상대 선택으로 이동하게 했다. 마몬은 매 소유자 차례 시작마다 유지/재굴림을 묻고, 유지는 정상 행동·재굴림은 차례 종료로 정정했다. | `StartingDemonRevealView.cs`, `GameFlowController.cs`, `StageProgressionView.cs`, 마몬 처리기·전투·적 AI·표시, DCR03_U14와 관련 기획 문서 | 이천서 기획·구현·검증 책임, AI 코드 추적·회귀 테스트·문서 정리 보조. 마몬·표시·적 AI 대상 51/51, CoreLoop 543/543, 전체 EditMode 817/817, 컴파일 오류 0. 기존 프로젝트 에셋만 사용. |
| 2026-07-31 | GameScene 전체 QA GF-06 | 1080p 정식 런 전 구간과 720p 상대 선택·전투 HUD, 상점 거래·체크포인트 이어하기·보스 결과·새 런을 검증하고 비전투 HUD, CHANGE 비용, 런타임 카드 덱 미리보기 회귀를 수정했다. | `GameFlowController.cs`, `GameSceneCombatHudPresentation.cs`, `DeckCardDisplaySnapshot.cs`, `GameScenePresentation.cs`, GF-06 회귀 테스트·전체 흐름·정식 런·AI 활용 기록 | 이천서 기획·구현·검증 책임, AI MCP 조작·원인 분석·코드·테스트·시각 판정·문서화 보조. 신규 2/2·전체 EditMode 813/813, MainMenuScene·GameScene 문제 0, Console Error 0, 시각 판정 94/100. 사용자 저장 해시 일치 복원, 씬·프리팹·외부 에셋·오픈소스·패키지 변경 없음. |
| 2026-07-31 | GameScene 결과·시작 경로 GF-05 | 승리·패배 결과 오버레이, 최종 영혼·골드·저장 상태, 새 런·메인 메뉴 복귀와 저장 실패 재시도를 구현했다. | `RunResultPresentation.cs`, `RunResultView.cs`, `GameFlowController.cs`, `StageProgressionRuntime.cs`, GF-05 테스트·전체 흐름·정식 런·AI 활용 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·실화면·문서화 보조. 전용 3/3·전체 EditMode 811/811, 컴파일·게임 코드 오류 0, 1280×720 시각 판정 93/100. 씬·프리팹·외부 에셋·오픈소스·패키지 추가 없음. |
| 2026-07-31 | GameScene 통합 흐름 GF-02 | 메뉴 성공 진입을 GameScene으로 교체하고, 정식 런 상태 기반 화면 판정·입력 중계·씬 재로드 없는 전투 바인딩·전투 완료 알림·재바인딩 표시 초기화를 구현했다. | `GameFlowScreen.cs`, `GameFlowController.cs`, `GameManager.cs`, 손패·덱 스택, `MainMenuController.cs`, `GameScene.unity`, GF 테스트·통합 계획·AI 활용 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·씬 연결·문서화 보조. GF 판정 2/2·누적 7/7·전체 EditMode 805/805, 씬 문제·Console 오류 0. GF-03 실제 시작 악마·상대 선택 UI와 외부 에셋·패키지는 미변경. |
| 2026-07-31 | 시작 악마 자동 지급 이관 | 시작 악마 2택 1 구조를 서로 다른 2장 모두 지급으로 교체하고, 지급 멱등성·스키마 v2 저장·예약 재접속 무재추첨·입력 없는 동시 공개·상대 선택 자동 전환을 구현했다. | `StartingDemonGrant.cs`, `PlayerRunState.cs`, 진행 세션·저장·Runtime·진행 UI, 지급/저장 회귀, RF·SV·전체 흐름 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조. 전체 EditMode 784/784, 컴파일 오류 0. GameScene 월드 카드 연출·씬·프리팹·외부 에셋·오픈소스·패키지 변경 없음. |
| 2026-07-31 | 정식 런 마감 RF-05 | 1280×720 정식 런 전체 왕복과 1920×1080 상대 선택을 검증하고, 720p 상점 보유 카드 영역을 4열 그리드로 수정했다. 팀원별 C/F 드라이브 차이를 위해 `AGENTS.md`의 Unity·프로젝트 경로를 동적으로 해석하도록 개정했다. | `StageProgressionView.cs`, 정식 런 문서 4종, `AGENTS.md`, RF05 검증 캡처 | 이천서 기획·구현·검증 책임, AI MCP 조작·화면 비교·코드·문서 보조. 대상 6/6·전체 EditMode 798/798, 게임 코드 오류 0(Test Framework 안내 3건), 외부 에셋·오픈소스·패키지 추가 없음. 라이브 전투 종료 조건만 MCP로 제어 |
| 2026-07-31 | 저장·이어하기 SV-06 | 정식 런 전투 정산·상점 나가기·보스 승패 체크포인트, 완료 상점 수·공통 가격 단계·루트 시드 복원과 실패 중 상점 입력 차단을 구현했다. | `RunSave*`, `RunRestoreFactory`, `FormalRunSession`, `StageProgressionRuntime`, 진행 Controller, SV06 테스트·저장 문서 4종 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; 신규 5/5·저장/정식 런 59/59·전체 EditMode 798/798·게임 코드 오류 0(Test Framework 안내 1건). 사건·실제 앱 재실행·두 해상도 잔여, 외부 에셋·오픈소스·패키지 변경 없음 |
| 2026-07-31 | 적 계약 정책 EPR07 | 광신도 전용 악마 덱을 벨페고르·바알제붑 2종으로 제한하고 상대 공개 카드 2장 기준 선택으로 이관했다. 영혼·카드 사용 가능 여부·벨리알 분기를 제거했다. | `CultistEnemyPolicy.cs`, `EnemyCombatProfileCatalog.cs`, 광신도·프로필 회귀와 계약·EP·공통 문서 | 이천서 기획·구현·검증 책임, AI 현행 대조·코드·테스트·문서화 보조. 대상 21/21·전체 EditMode 793/793, 외부 에셋·오픈소스·패키지 추가 없음. |
| 2026-07-30 | 자동 카드 AC-RV03·회귀 복구 | 부활초를 소유자→상대 독립 선택과 지불 참가자 전용 손패 재배분으로 이관하고, 집행자 독극물의 라운드 최초 배분 전 삽입·셔플을 재검증했다. 광신도 바알제붑 연속 선택 정체와 HUD 고정 버튼 브러시도 복구했다. | CoreLoop 자동 카드·적 AI, `HUD.prefab`, ACRV03/DC08/GSH01 테스트, 자동 카드 문서 4종·AI 활용 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·원인 분석·코드·테스트·문서화 보조. ACRV03 11/11·전체 EditMode 792/792, 외부 에셋·오픈소스·패키지 추가 없음. |
| 2026-07-30 | 정식 런 진입 RFM02 | GameScene 직접 실행은 시작 악마·상대 선택 메뉴 없이 기존 독립 전투 화면을 즉시 시작하고, 활성 진행 전투가 전달된 경우에만 런 세션을 채택하도록 책임을 정정했다. 중복 카드 콘텐츠 부트스트랩의 GameManager 호스트 삭제도 방지했다. | `StageProgressionRuntime.cs`, `GameManager.cs`, `CardContentBootstrap.cs`, `FormalRunSystemValidationTests.cs`, 정식 런 4종·AI 활용 기록 | 이천서 의도 정정·기획·구현·검증 책임, AI 원인 추적·코드·테스트·문서화 보조. StageTest 기존 테스트 흐름 유지, 외부 에셋·패키지·씬·프리팹 변경 없음 |
| 2026-07-30 | 무기 피격 표정 CU-M09 | 해머의 상대 표정 반응을 제거하고, 리볼버 성공 시 실제 총구 화염 프레임 전까지 공격 위기 표정을 유지한 뒤 피격 표정으로 전환하도록 구현했다. | GameScene 표시·조정자·리볼버 이벤트 수신기, 성공 애니메이션 클립 2개, CUM09 테스트와 카드 사용·씬 문서 | 이천서 기획·구현·검증 책임, AI 원인 추적·코드·테스트·문서화 보조; 대상 6/6·컴파일·게임 코드 오류 0. 전체 766건 중 기존 광신도 자동전투 실패 1건은 별도 후속, 새 외부 에셋·패키지 없음 |
| 2026-07-30 | 저장·카드 연출·적 AI 보정 | 새 런별 루트 시드 발급과 저장 보존, 보위 나이프 강제 드로우/폐기 프레임, 광신도 수동 카드 우선 평가를 구현했다. | `RunSaveFlow.cs`, 카드 효과/`CardHand.cs`, `CultistEnemyPolicy.cs`, SV05·CUM08·EPR05 테스트와 관련 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; 3개 어셈블리 직접 컴파일·순수 대상 2건 통과. 저장 대상·전체 Unity 회귀·GameScene 육안 검증은 MCP 미노출로 미실행, 외부 에셋·새 패키지 없음 |
| 2026-07-30 | 정식 런 화면 RF-04 | 저장 세션 재결합, 신규/복원 RW 우회, CoreLoop 전투 연결과 진행 IMGUI의 골드·상점 구매·카드 제거·회복·나가기 입력을 구현했다. | Runtime·SaveFlow·Restore, 진행 Presenter/View/Controller, `CoreLoopController.cs`, `FormalRunPresentationTests.cs`, RF·공통 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; RF04 6건 컴파일·비Unity 5건·누적 42/42·씬 직렬화 확인. MCP가 닫힌 8080을 가리켜 전체 EditMode·두 해상도·씬 왕복 미실행, 외부 에셋·새 패키지 없음 |
| 2026-07-30 | 정식 런 순서 RF-03 | 기존 RW 기본 경로를 보존하면서 정식 런 전용 무카드보상 완료·전투 결과 동기화·두 상점·고정 보스·승패·재시작을 구현했다. | `RunProgress.cs`, `StageProgressionSession.cs`, `StageProgression/RunFlow`, 상점 생성기·방문, `FormalRunSessionTests.cs`, RF·공통 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; RF03 12건·RF-01~03 누적 37/37·두 어셈블리 컴파일. 전체 EditMode·씬은 MCP 미연결로 미실행, 외부 에셋·새 패키지 없음 |
| 2026-07-30 | 정식 런 상점 RF-02 | 자동 카드 포함 일반 3장·악마 2장 결정적 제안, 슬롯별 복수 구매, 라이터·위스키 독립 1회, 공통 가격 단계·무료 나가기와 최소 덱 제거를 구현했다. | `StageProgression/Shop`, `PlayerRunState.cs`, `FormalRunShopTests.cs`, RF·저장·자동 카드·공통 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; RF02 11/11·두 어셈블리 컴파일. 전체 EditMode·씬은 MCP 미연결로 미실행, 외부 에셋·새 패키지 없음 |
| 2026-07-30 | 정식 런 골드 RF-01B | 적 6종의 확정 골드 카탈로그와 종료 승리 전투 참조별 1회 지급을 구현하고 패배·중복·구형 무프로필 호환 경계를 고정했다. | `GoldRewardCatalog.cs`, `StageProgressionSession.cs`, `FormalRunGoldRewardTests.cs`, 정식 런 4종·README·AI 활용·역할 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서화 보조; RF01B 7/7·RF-01 전체 14/14·두 어셈블리 컴파일. 전체 EditMode·씬은 MCP 미연결·Editor 실행으로 미실행, 외부 에셋·새 패키지 없음 |
| 2026-07-30 | RF 카드 성장 상점 통합 기획 | 무료 전투 카드 보상과 상점 카드 구매의 중복을 제거하고 일반전 승리 골드→상점, 보스 즉시 승리, 자동 발동 카드 5종 상점 편입과 기존 RW 보존 경계를 확정했다. | `rule.md`, 전체 기획, RW/RF/SP/AC/SV 4종, 씬 연출, README·구조·AI 활용·팀 역할 문서 | 이천서 기획 판단·최종 승인·문서 책임, AI 코드/문서 차이 대조와 개정 초안 보조; 문서만 변경, 코드·테스트·씬·프리팹·Packages·외부 에셋 무변경, 구현 이관·Unity 검증 미실행 |
| 2026-07-30 | GameScene 카드 스프라이트 GSV-02 | 자동 발동 카드 5종의 도상 매핑과 악마 카드 12종의 프로젝트 확정 순서를 고정하고 플레이어·공개 적 손패, 악마 상점, 전투 계약 후보에 전용 이미지를 연결했다. | `GameScenePresentation.cs`, `CardView.cs`, `DemonCardView.cs`, `ShopController.cs`, `GameManager.cs`, 계약 표시 모델, `Card.prefab`, `GameSceneCardSpriteTests.cs`, 자동/계약/씬/구조/AI 활용 문서 | 이천서 악마 순서 제공·매핑 승인·구현·검증 책임, AI PNG·카탈로그·직렬화 대조와 코드·테스트·문서화 보조; GSV-02 19/19·전체 687/687. 원본 GameScene Play Mode 미검증, 기존 팀 에셋 재사용·외부 에셋/새 패키지 없음 |
| 2026-07-29 | GameScene 적 스프라이트 GSV-01 | 적 6종의 기본·공격 위기·공격받음 스프라이트와 상인 이미지를 프로필 키로 연결하고, 독립 GameScene의 표시 프로필과 실제 최대 영혼·덱·AI·계약 설정을 일치시켰다. | `CharacterView.cs`, `GameScenePresentation.cs`, `GameManager.cs`, `EnemyCharacter.prefab`, GSV/CoreLoop 표시 테스트, 씬 연출·구조·AI 활용 문서 | 이천서 기획·상태 의미 정정·구현·검증 책임, AI 에셋/프로필 대조·코드·테스트·프리팹 직렬화·문서화 보조; 전용 3/3·전체 668/668·세 상태 프리팹/PSB 원본 일치 100/100. 원본 GameScene Play Mode는 미검증, 외부 에셋·새 패키지 없음 |
| 2026-07-29 | 적 전투 프로필 EP-R06·계약 DC-R06 | 최종 보스의 바포메트/마몬→아스모데우스/레비아탄→아자젤/루시퍼 고정 쌍을 전투 시작·생존 영혼 5·2에 연결하고 복수 경계·비역행·치명 피해 중단·일반 계약 차단과 현재 효과 표시를 구현했다. | `FixedDemonContractPhaseDefinition.cs`, 적 프로필/전투 설정, `StageBattleFactory.cs`, `CoreLoopBattle.cs`, `DemonContractDeck.cs`, 계약 표시, `EnemyBossContractPhaseTests.cs`, 규칙·EP/DC·공통 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·테스트 우선 구현·실패 분석·문서화 보조; 전용 10/10·전체 EditMode 665/665. GameScene·MonoBehaviour UI·씬·프리팹·Packages·외부 에셋 무변경, 골드·저장·정식 런 UI는 HONG RF 범위 유지 |
| 2026-07-29 | 적 전투 프로필 EP-R05 | 겁쟁이 수동 카드 15%, 광신도 벨페고르·바알제붑·벨리알 전용 계약과 성공까지 재시도, 전 적 공통 체인지·사기꾼 비용 1, 집행자 파이몬·매 라운드 독극물 주입/누적/전투 종료 제거를 프로필 전투에 구현했다. 공개 관측에는 공개 카드 사용 가능 여부만 추가하고 미공개 비공개 카드 정보는 보존했다. | `CoreLoopBattle.cs`, `EnemyAI/`, `EnemyProfiles/`, `PlayerChangeSelection.cs`, `DemonContractDeck.cs`, `StageBattleFactory.cs`, EPR05 테스트, EP·BA·AC·DC·공통 문서 | 이천서 기획·구현·검증 책임, AI 구조 대조·테스트·회귀 진단·문서화 보조; 정책 12/12·CoreLoop 466/466·StageProgression 189/189·전체 655/655. GameScene·UI·씬·프리팹·Packages·외부 에셋 무변경 |
| 2026-07-29 | 적 전투 프로필 EP-R04 | 적 6종의 확정 최대 영혼·전용 덱을 실제 전투 생성 카탈로그에 이관하고 겁쟁이 공개 합 15 스탠드, `집행자` 표시명, 보스 영혼 8 표시 호환, 아스모데우스 숫자 7 이하 제한을 구현했다. 광신도 영혼 증가로 드러난 과거 사탄 선택 자동 전투 정체도 차단했다. | `EnemyCombatProfileCatalog.cs`, `CowardlyGamblerEnemyPolicy.cs`, `CultistEnemyPolicy.cs`, `BossCombatDisplayModel.cs`, 아스모데우스 처리기·카탈로그, CoreLoop/StageProgression 관련 테스트, EP·카드 사용·계약·공통 문서 | 이천서 기획·구현·검증 책임, AI 값 대조·테스트·정체 진단·문서화 보조; EPR04 14/14·CoreLoop 451/451·StageProgression 189/189·전체 EditMode 640/640 통과. GameScene·씬·프리팹·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-29 | Notion v0.7 기획 전반 재명세 | 최신 원본의 적 6종 영혼·덱·능력·계약, 사기꾼 체인지, 집행자 자동 카드, 아스모데우스 제한과 RF/저장 파급을 기능별 문서로 이관하고 과거 완료 증거와 새 미구현 작업을 분리 | 규칙·전체 기획, EP/EUI·DC·AC·BA·RW·SP·RF·SV 문서, 씬 연출·README·AI 활용·구조 기록 | 이천서 기획·문서 책임, AI Notion 조회·차이 분석·문서 구조화 보조; 코드·테스트·씬·프리팹·Packages·외부 에셋·오픈소스 무변경, Unity 테스트 미실행 |
| 2026-07-29 | 카드·계약·GameScene CU-M07 | 레비아탄 리볼버를 첫 실패 조건부 재예측으로 개정하고 첫 성공 종료·첫 실패 연출 뒤 재준비·두 번째 성공 무비용·두 번 실패 영혼 1과 계약 표시 문구를 구현·문서화 | `LeviathanDemonContractHandler.cs`, `DemonContractCatalog.cs`, `DemonContractPresentation.cs`, `CoreLoopBattle.cs`, `GameScenePresentation.cs`, `GameManager.cs`, `MammonAndLeviathanDemonContractTests.cs`, 규칙·카드 사용·계약·씬 연출·공통 기록 | 이천서 기획·구현·검증 책임, AI 규칙 충돌 분석·코드·테스트·문서·Unity MCP 보조; 신규 4/4·CoreLoop 442/442·전체 631/631·GameScene 재예측 상태 확인, 씬·프리팹·Animator Controller·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-29 | GameScene 리볼버 CU-M06 | 리볼버 카드 사용 직후 준비 오브젝트를 표시하고 숫자 예측 뒤 성공·실패 사격 Animator 상태로 이어지는 흐름을 구현·문서화 | `GameScenePresentation.cs`, `GameManager.cs`, `CoreLoopPresentationTests.cs`, 카드 사용 문서 4종·씬 연출·공통 기록 | 이천서 기획·구현·검증 책임, AI Animator 전이 원인 분석·코드·테스트·문서·Unity MCP 보조; 대상 2/2·CoreLoop 438/438·전체 627/627·실제 준비/성공/실패 상태 확인, 씬·프리팹·Controller·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-29 | GameScene 상점 RFM01 | 카드 슬롯별 재고를 유지하며 방문 전체 구매 상한을 제거하고, 라이터·위스키 독립 1회·이용 방문당 다음 상점 공통 가격 +1단계·새 런 초기화·일반/라이터 선택 화면 `나가기`를 구현하고 관련 기획 문서를 개정 | `ShopController.cs`, `GameManager.cs`, `ShopControllerTests.cs`, `rule.md`, 전체 기획·씬 연출·정식 런 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서·Unity MCP 보조; 전용 3/3·전체 EditMode 626/626·GameScene 두 상점 가격 2→3·두 나가기 화면, 씬·프리팹·Packages·외부 에셋·오픈소스 무변경; HONG 정식 RF는 미완료 유지 |
| 2026-07-29 | 코어 루프 표시 CL-M01 | 플레이어 합계를 비공개 역할 포함 `총합`과 제외한 `공개 카드 합`으로 분리해 CoreLoopTest·GameScene에 연결하고 적은 공개 합만 유지 | `CoreLoopPresentation.cs`, `CoreLoopView.cs`, `TableTotalsView.cs`, `GameManager.cs`, `CoreLoopPresentationTests.cs`, 코어 루프 문서 4종·씬 연출·공통 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·문서·Unity MCP 보조; 전용 2/2·전체 EditMode 623/623·GameScene 두 줄 화면·게임 코드 오류 0, 씬·프리팹·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-29 | 적 카드 호버 CU-M05 | 공개된 적 카드와 앞면 공개된 비공개 역할 카드의 숫자·이름·수동·자동 효과 정보를 기존 HUD 배지에 연결하고 미공개 적 카드의 문자열을 차단 | `GameScenePresentation.cs`, `CardView.cs`, `CoreLoopPresentationTests.cs`, 카드 사용 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 원인 분석·테스트·코드·문서·Unity MCP 보조; 신규 3/3·전체 EditMode 622/622·GameScene HUD 화면·게임 코드 오류 0, 씬·프리팹·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-29 | 카드·적 AI CU-M04·EP-R02 | 비공개 역할과 앞뒷면을 분리하고, 공개 합·중간 버스트·공개 카드 대상·최종 승부를 재정렬했으며 공개된 플레이어 비공개 숫자에 모든 적이 사용 가능한 리볼버로 확정 대응하도록 구현·문서화 | `BlackjackHand.cs`, `BattleParticipant.cs`, CoreLoop 카드 효과·적 관측/정책, `GameScenePresentation.cs`, CUM04 및 회귀 테스트, 규칙·카드 사용·적 프로필·AI 활용 문서와 최신 노션 | 이천서 기획·구현·검증 책임, AI 충돌 분석·코드·테스트·문서/노션·MCP 보조; 전용 5/5·CoreLoop 430/430·StageProgression 189/189·전체 619/619, 씬·프리팹·Packages·외부 에셋·오픈소스 무변경 |
| 2026-07-28 | 악마 계약 DC-R05 | 실제 시작 악마 2장 제안·1장 덱·저장 재진입을 확인하고, 활성 마몬·사탄 물리 카드 ID 입력과 일반/루시퍼 후보 레이아웃을 독립·런 화면 코드에 연결 | `CoreLoopBattle.cs`, 두 세션, 계약 표시 모델, `CoreLoopView/Controller`, `GameManager.cs`, `DemonContractPresentationTests.cs`, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·입력 경계·테스트·MCP·시각 교정 보조; 신규 4/4·CoreLoop 425/425·StageProgression 189/189·합계 614/614·저장 재진입·두 화면 720p/1080p·시각 92점·게임 오류 0; 씬·프리팹·외부 에셋 무변경, 물리 월드 카드 연출은 별도 아트·씬 작업 |
| 2026-07-28 | 악마 계약 DC-R04 5/5 | 루시퍼의 현재 악마 덱 최대 5장·건너뛰기·미선택 폐기·별도 계약 인스턴스·기본 비용 비중복·복합 대가·재귀 연쇄를 플레이어·적 대칭으로 구현 | `LuciferDemonContractHandler.cs`, 계약 선택/Resolver·표시, CoreLoop·광신도 관측/정책, `LuciferDemonContractTests.cs`, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 모호성·후보 수명·연쇄 분석·코드·테스트·MCP 보조; 전용 10/10·CoreLoop 421/421·전체 610/610·컴파일 오류 0, GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 악마 계약 DC-R04 4/5 | 바포메트 상대 1~5·소유자 1~3 오망성, 계약별 양측 묶음·드로우 더미 소진·전투 위치 회수·가용 덱 초기화·재삽입·버스트 방지/대체를 플레이어·적 대칭으로 구현 | 바포메트 처리기, `DemonContractCardState`, `BlackjackDeck`, CoreLoop·적 AI, `BaphometDemonContractTests.cs`, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 모호성·임시 카드 중첩 분석·코드·테스트·MCP 보조; 전용 7/7·광신도 9/9·CoreLoop 410/410·전체 599/599, GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 악마 계약 DC-R04 3/5 | 파이몬 선택 추방·단일 임시 승자 대가와 벨리알 공개 카드 탈취·즉시 재사용·스탠드 취소·라운드 영혼 대가·전투 종료 소유권 복구를 플레이어·적 대칭으로 구현 | 파이몬·벨리알 처리기, `DemonContractCardState`, 계약 Resolver·선택, CoreLoop·적 AI, `PaimonAndBelialDemonContractTests.cs`, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 연쇄 판정·카드 ID 충돌·소유권 수명 분석·코드·테스트·MCP 보조; 전용 10/10·광신도 9/9·CoreLoop 402/402·전체 591/591, GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 악마 계약 DC-R03 | 마몬 행동 소비 재굴림과 레비아탄 독립 2회 발동·적 대칭을 구현하고 정보 은닉·종료·영혼 대가 회귀를 고정 | 마몬·레비아탄 처리기, 계약 선택/Resolver, CoreLoop·세션·적 AI·표시 모델, DCR03 테스트, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 규칙 대조·코드·테스트·MCP 보조; 전용 14/14·CoreLoop 367/367·전체 556/556·게임 코드 오류 0, GameScene·씬·프리팹·Packages·외부 에셋 무변경, 다중 레비아탄 중첩·월드 입력은 후속 |
| 2026-07-28 | 악마 계약 DC-R02 | 사탄 종말 카운트 4·소유자 정상 차례 훅·영혼 대가 1회·활성 계약 양면 행동·자동 카드 재개·적 대칭을 구현하고 별도 권능 카드를 제거 | 사탄 계약 처리기, 계약 선택/Resolver, CoreLoop·세션·적 AI·표시 모델, `SatanDemonContractTests.cs`, 계약 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 최신 규칙 대조·코드·테스트·재진입 분석·MCP 보조; 전용 12/12·CoreLoop 363/363·전체 553/553·게임 코드 오류 0, GameScene·씬·프리팹·Packages·외부 에셋 무변경, 월드 카드 입력은 DC-R05 |
| 2026-07-28 | 악마 계약 UI DC-M01 | 계약 완료 뒤 계속 남던 `DEMON CONTRACT` 상시 상태 상자를 GameScene과 CoreLoopTest에서 제거하고 확인·후보 선택 기능은 유지 | `GameManager.cs`, `CoreLoopView.cs`, 악마 계약 설계·진행 기록, AI 활용 기록 | 이천서 기획·구현·검증 책임, AI 렌더링 경로 탐색·삭제·MCP 보조; CoreLoop 362/362·전체 EditMode 551/551·활성 계약 GameScene 화면·Console 0, 씬·프리팹·Packages·외부 에셋 무변경 |
| 2026-07-28 | 적 전투 프로필 EP-R01 | 겁쟁이 도박사의 덱 합계를 18장으로 정정하고 기존 3종을 유지한 네 번째 일반 적, 최대 영혼 2·합계 14 스탠드·수동 카드 비사용·빈 악마 덱·일반 보상으로 구현 | `EnemyCombatProfileCatalog.cs`, `EnemyBehaviorPolicyCatalog.cs`, `CowardlyGamblerEnemyPolicy.cs`, EPR01 테스트 2개와 기존 회귀, 적 프로필 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 합산 오류 발견·구조 대조·테스트·코드·MCP 보조; 전용 9/9·CoreLoop 362/362·StageProgression 172/172·전체 534/534·게임 코드 오류 0, 최종 결과 저장 안내 3건 분리; UI·GameScene·씬·프리팹·Packages·외부 에셋 무변경 |
| 2026-07-28 | 자동 발동 카드 AC-RV01 | 독극물·화염 방사기·회중시계의 숫자와 정의 키를 최신 기획에 맞추고 보상·런/전투 변환·저장 호환 정책을 원자적으로 이관 | `CardDefinitionCatalog.cs`, `RunSaveSnapshot.cs`, AC-RV01 테스트 2개와 기존 회귀, 자동 카드 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·테스트·코드·MCP 보조; 전용 11/11·전체 522/522·GameScene/CoreLoopTest·Console 0, 씬·프리팹·외부 에셋 무변경 |
| 2026-07-27 | 악마 계약 DC-R01 | 시작 악마 결정적 2장 제안·1장 선택, 일반 계약 후보 0·1·2장, 루시퍼 5장 상수 분리, 재시작·시작 체크포인트 복원을 구현하고 광신도 강제 후보 회귀를 교정 | `CoreLoop/DemonContracts`, `CultistEnemyPolicy.cs`, `StageProgression/StartingDemonSelection.cs`, `PlayerRunState.cs`, 세션·Save 경계, DCR01 테스트·계약 문서 4종 | 이천서 기획·구현·검증 책임, AI 구조 대조·코드·테스트·MCP 보조; 전용 14/14·CoreLoop 349/349·StageProgression 162/162·전체 511/511, GameScene·씬·프리팹 무변경 |
| 2026-07-27 | 최신 노션 기획 이관 DC-R00·AC-RV00·EP-R00 | 악마 12종·자동 카드 숫자 1~5·겁쟁이 도박사 규칙을 구현 기준 문서에 반영하고 후속 코드 이관 작업을 분리 | `Docs/rule.md`, `game-design-document.md`, 악마 계약·자동 카드·적 프로필 문서 4종씩, README·AI 활용·팀 역할 기록 | 이천서 기획·문서 책임, AI 노션 조회·차이 분석·초안 보조; 코드·테스트·씬·프리팹·Packages·외부 에셋 변경 및 Unity 재검증 없음 |
| 2026-07-19 | 게임 기획 | 원본 규칙을 바탕으로 전체 게임 기획과 프로토타입 임시 결정 정리 | `Docs/game-design-document.md` | 문서 작성 완료, 팀 검토 필요 |
| 2026-07-19 | 개발 기획 | 최소 코어 루프의 포함·제외 범위와 완료 조건 정의 | `Docs/core-loop-design.md` | 구현 전 기준 문서 |
| 2026-07-19 | 기술 설계 | 코어 루프 상태, 책임 분리, 데이터와 테스트 구조 정의 | `Docs/core-loop-development-spec.md` | 실제 코드 구현 전 |
| 2026-07-19 | 구현 계획 | 코어 루프 작업 분해, 의존성, 4일 일정과 검증 게이트 작성 | `Docs/core-loop-implementation-plan.md` | 구현 착수 전 계획 |
| 2026-07-19 | 진행 관리 | 단계별 착수·구현·검증·변경 기록 양식과 1단계 준비 항목 작성 | `Docs/core-loop-progress-log.md` | 구현 전 진행 기록 |
| 2026-07-19 | AI·출처 기록 | AI 활용 방식, 주요 지시, 외부 패키지 출처와 확인 과제 정리 | `Docs/ai-usage-technical-document.md` | 라이선스 일부 재확인 필요 |
| 2026-07-19 | 코어 루프 1단계 | 프로젝트·MCP 구조 확인, 카드·덱·합계·판정·영혼 피해 구현 관리 | `Assets/01. Scripts/Runtime/CoreLoop`, `Assets/Tests/EditMode/CoreLoop` | AI 보조 구현, EditMode 15/15 통과, 최종 코드 리뷰 필요 |
| 2026-07-19 | 코어 루프 2단계 | 전투·라운드 상태, 히트·스탠드, 단순 적 정책과 자동 다음 라운드 구현 관리 | `Assets/01. Scripts/Runtime/CoreLoop`, `Assets/Tests/EditMode/CoreLoop/CoreLoopFlowTests.cs` | AI 보조 구현, EditMode 전체 23/23 통과, 최종 코드 리뷰 필요 |
| 2026-07-19 | 코어 루프 3단계 | 최소 UI·입력 잠금·승패 표시·새 전투 재시작 구현 및 씬 통합 관리 | `Assets/01. Scripts/Runtime/UI/CoreLoop`, `CoreLoopSession.cs`, `Assets/00. Scenes/CoreLoopTest.unity` | AI 보조 구현, EditMode 전체 27/27·Game View·씬 검증 통과, 최종 리뷰 필요 |
| 2026-07-19 | 코어 루프 4단계 | 전체 회귀 테스트와 승리·패배·재시작 수동 흐름을 검증하고 완료 증거·AI 활용·실제 담당 기록을 마감 | `Docs/core-loop-progress-log.md`, 구현 계획·AI·팀 역할·MCP 참조 문서 | AI 검증 보조, EditMode 27/27·씬 문제 0·게임 관련 Console 오류 0; 이천서 최종 승인 대기 |
| 2026-07-19 | 런·스테이지 진행 기획 | 진행 시스템을 코어 루프와 분리하고 런 상태, 전투 연결 경계, 작업 분해와 검증 기준 수립 | `Docs/stage-progression-design.md`, `stage-progression-development-spec.md`, `stage-progression-implementation-plan.md`, `stage-progression-progress-log.md` | AI 문서 초안 보조, SP-00에서 구현 기준 확정, 실제 구현은 별도 SP-01 기록 참조 |
| 2026-07-19 | 런·스테이지 SP-00 | 신규 문서의 임시 결정을 구현 기준으로 확정하고 저장소·Unity MCP·기존 전투 회귀 기준선 검증 | `Docs/stage-progression-progress-log.md`, `stage-progression-implementation-plan.md` | AI 검증 보조, EditMode 27/27·씬 문제 0·게임 관련 Console 오류 0, SP-01 착수 가능 |
| 2026-07-19 | 런·스테이지 SP-01 | 스테이지 경로, 지속 플레이어 상태, 진행 상태 전이와 입력 유효성 검사를 전투·UI와 독립된 순수 C# 기반으로 구현 관리 | `Assets/01. Scripts/Runtime/StageProgression`, `Assets/Tests/EditMode/StageProgression`, 관련 진행 문서 | AI 구현·테스트 보조, 신규 13/13·전체 EditMode 40/40·스크립트 진단·Console 검증 통과; 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 런·스테이지 SP-02 | 기존 전투 생성 동작을 보존하면서 현재 영혼 전달, 스테이지별 새 전투 생성, 전투 종료 결과와 런 상태 동기화 구현 관리 | 코어 변경 3개, `StageBattleFactory.cs`, `StageProgressionSession.cs`, `StageProgressionBattleTests.cs`, 관련 문서 | AI 구현·테스트 보조, 진행 어셈블리 18/18·전체 EditMode 45/45·스크립트 진단·Console 검증 통과; 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 런·스테이지 SP-03 | 진행 표시·상태별 입력, 씬 간 런 세션 유지와 전용 진행 화면·공용 전투 화면 연결 구현 관리 | `Assets/01. Scripts/Runtime/UI/StageProgression`, `CoreLoopController.cs`, `StageTest.unity`, `StageProgressionPresentationTests.cs`, 관련 문서 | AI 구현·테스트 보조, 진행 어셈블리 23/23·전체 EditMode 50/50·씬 문제 0·단계 간 영혼 유지 흐름 통과; 이천서 최종 코드·화면 승인 대기 |
| 2026-07-19 | 런·스테이지 SP-04 | 전체 회귀, 일반전 2개·최종 보스 승리, 두 번째 일반전 패배, 양쪽 재시작·10회 반복과 씬·Console 최종 검증 관리 | 진행 문서 4종, AI 활용·팀 역할·프로젝트 구조 문서, `Temp/SP04Validation` 로컬 화면 증거 | AI 검증·기록 보조, EditMode 50/50·씬 문제 0·게임 관련 Console 오류 0; 코드 변경 없이 전체 작업 마감, 이천서 최종 승인 대기 |
| 2026-07-19 | 전투 행동 BA-00 | 폴드·체인지 1차 범위, 규칙 충돌, 상태·카드 이동 명세와 BA-00~BA-05 구현·검증 계획 수립 | `Docs/combat-action-design.md`, `combat-action-development-spec.md`, `combat-action-implementation-plan.md`, `combat-action-progress-log.md` | AI 문서 초안·구조 대조 보조, 코드·씬 변경과 Unity 테스트 없음; 이천서 기획 기준 최종 검토 필요 |
| 2026-07-19 | 전투 행동 BA-01 | 선택 상태, 비공개 카드 원자적 인출, 덱 후보 확보 검사와 후보 선택 불변 조건 구현 관리 | CoreLoop 상태·손패·덱·`PlayerChangeSelection.cs`, `CombatActionFoundationTests.cs`, 관련 문서 | AI 구현·테스트 보조, Unity 컴파일 오류 0·CoreLoop 35/35·전체 EditMode 58/58; 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 전투 행동 BA-02 | 폴드 결과·영혼 피해·즉시 라운드 종료와 CoreLoop 세션 전달 구현 관리 | `RoundResolver.cs`, `CoreLoopBattle.cs`, `CoreLoopSession.cs`, 표시 호환, `CombatActionFoldTests.cs`, 관련 문서 | AI 구현·테스트 보조, Unity 컴파일 오류 0·CoreLoop 41/41·전체 EditMode 64/64; 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 전투 행동 BA-03 | 체인지 보류·후보 공개와 선택, 적 차례 연결·라운드당 제한·세션 전달 구현 관리 | `BattleParticipant.cs`, `CoreLoopBattle.cs`, `CoreLoopSession.cs`, `CombatActionChangeTests.cs`, 관련 문서 | AI 구현·테스트 보조, Unity 최종 컴파일 오류 0·CoreLoop 49/49·전체 EditMode 72/72; 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 전투 행동 BA-04 | 폴드 비용·패배 위험과 체인지 후보·사용 상태 표시, View 버튼·후보 입력과 Controller 전달 구현 관리 | `CoreLoopBattle.cs`, `Assets/01. Scripts/Runtime/UI/CoreLoop`, `CoreLoopPresentationTests.cs`, 관련 문서 | AI 구현·화면·테스트 보조, CoreLoop 55/55·전체 EditMode 78/78·Game View·양쪽 씬·Console 검증 통과; 이천서 최종 코드·화면 승인 대기 |
| 2026-07-19 | 전투 행동 BA-05 | 런 전투의 폴드·체인지를 진행 세션에 통합하고 종료·지속 영혼 동기화와 전체 회귀·실제 패배·재시작 검증 관리 | `StageProgressionSession.cs`, `CoreLoopController.cs`, `StageProgressionBattleTests.cs`, 관련 문서와 `Temp/BA05Validation` 로컬 화면 증거 | AI 구현·검증·기록 보조, 진행 27/27·전체 EditMode 82/82·양쪽 씬 문제 0·Console 오류 0; 전투 행동 확장 마감, 이천서 최종 승인 대기 |
| 2026-07-19 | 카드 사용 CU-00 | 일반 수동 카드 4종의 범위·임시 규칙·카드 정의·효과 선택·UI·진행 연결과 CU-00~CU-06 검증 계획 수립 | `Docs/card-use-design.md`, `card-use-development-spec.md`, `card-use-implementation-plan.md`, `card-use-progress-log.md` | AI 문서 초안·구조 대조 보조, 코드·씬·Unity 테스트 없음; 이천서 기획 기준 최종 검토 필요 |
| 2026-07-19 | 카드 사용 CU-01 | 숫자별 카드 정의·카탈로그, 물리 카드 사용 상태, 기존 생성자 호환과 런 정의 키 보존 구현 관리 | CoreLoop·StageProgression 런타임, `CardDefinitionTests.cs`, 진행 테스트와 관련 문서 | AI 구현·테스트·기록 보조, 신규 19개·전체 EditMode 101/101·Unity Console Error/Warning 0; 씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 카드 사용 CU-02 | 승인 전 사용 검증, 선택 대기·효과 처리·완료 결과, 카드 이동 명령과 효과 버스트 종료 원인 구현 관리 | CoreLoop 런타임, `CardEffectFoundationTests.cs`, 관련 문서 | AI 구현·테스트·기록 보조, 신규 16개·CoreLoop 87/87·전체 EditMode 117/117·Unity Console Error/Warning 0; 실제 카드·UI·씬·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 카드 사용 CU-03 | 리볼버 7·8의 단일 비공개 카드 추측, 성공·실패·직접 버스트와 정보 은닉 구현 관리 | `AutoPistolEffectHandler.cs`, 효과 등록·전투 변경, `AutoPistolEffectTests.cs`, 관련 문서 | AI 구현·테스트·기록 보조, 신규 8개·CoreLoop 95/95·전체 EditMode 125/125·Unity Console Error/Warning 0; 다중 비공개 카드 기능·UI·씬·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 카드 사용 CU-04 | 수정 구슬 후보 순서·소유권, 해머 비용·단일 비공개 교체, 나이프 강제 드로우·유지 정책 구현 관리 | 카드 처리기 3개, `CardEffectResolver.cs`, `CardEffectSelection.cs`, `BattleParticipant.cs`, `RemainingCardEffectTests.cs`, 관련 문서 | AI 구현·테스트·기록 보조, 신규 18개·CoreLoop 113/113·전체 EditMode 143/143·Unity Console Error/Warning 0; 다중 비공개 카드 기능·UI·씬·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-19 | 카드 사용 CU-05 | 카드별 사용 상태·불가 사유·효과 선택·최근 결과 표시, 독립/런 전투 입력 전달과 종료·지속 영혼 동기화 구현 관리 | `Assets/01. Scripts/Runtime/UI/CoreLoop`, `StageProgressionSession.cs`, Presenter·진행 통합 테스트 8개, 관련 문서 | AI 구현·테스트·화면 검증 보조, 관련 28/28·CoreLoop 117/117·StageProgression 34/34·전체 EditMode 151/151·Game View·양쪽 씬·Console 통과; 씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드·화면 승인 대기 |
| 2026-07-20 | 카드 사용 CU-06 | 카드 소유권·정보 은닉·독립 및 런 재시작 반복 회귀, 실제 런 승리·패배·재시작과 최종 기록 관리 | `CardUseSystemValidationTests.cs`, 카드 사용 문서 4종, AI 활용·팀 역할·프로젝트 구조 기록 | AI 테스트·MCP 검증·기록 보조, 신규 5/5·CoreLoop 122/122·StageProgression 34/34·전체 EditMode 156/156·양쪽 씬·Console 통과; 런타임·씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 전투 보상 RW-00 | 모든 전투의 3장 보상, 보스 보상 후 런 승리, 프로토타입 카드 풀, 덱·상태·세션·UI·RW-01~RW-05 구현 기준 수립 | `Docs/battle-reward-design.md`, `battle-reward-development-spec.md`, `battle-reward-implementation-plan.md`, `battle-reward-progress-log.md`, 기준·공통 기록 문서 | AI 문서 초안·현재 구조 대조 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 이천서 기획 기준 최종 검토 필요 |
| 2026-07-20 | 전투 보상 RW-01 | 일반·높은 등급 카탈로그, 결정적 3장 제안, 고유 런 카드 ID, 최초 덱·영혼·ID 재시작 복구 구현 관리 | 전투 보상 런타임 5개, `PlayerRunState.cs`, `AssemblyInfo.cs`, `BattleRewardFoundationTests.cs`, 관련 기록 문서 | AI 구현·테스트·Unity 배치 검증·기록 보조, 신규 8/8·StageProgression 42/42·전체 EditMode 164/164 통과; 진행 상태·UI·씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 전투 보상 RW-02 | 보상 선택 대기, 선택·건너뛰기, 일반·보스 완료 목적지와 실패 원자성 구현 관리 | 보상 상태 런타임 3개, `RunProgress.cs`, 상태·세션·표시 호환, `BattleRewardStateTests.cs`, 관련 기록 문서 | AI 구현·테스트·로컬 Unity MCP 검증·기록 보조, 신규 7/7·StageProgression 49/49·전체 EditMode 171/171·Console 컴파일 오류 0; 실제 세션 연결은 RW-03, 씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 전투 보상 RW-03 | 실제 전투 승리의 영혼 동기화·보상 생성·등급 주입·선택/건너뛰기·다음 전투 덱과 재시작 통합 관리 | `StageProgressionSession.cs`, `RunProgress.cs`, `BattleRewardSessionTests.cs`, 기존 진행·카드 회귀 테스트와 관련 기록 문서 | AI 구현·테스트·로컬 Unity MCP 검증 보조, 신규 6/6·StageProgression 55/55·전체 EditMode 177/177·C# 컴파일 오류 0; UI는 RW-04, 씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 전투 보상 RW-04 | 보상 후보 읽기 모델, 기존 진행 화면의 선택·건너뛰기·완료 목적지·최근 결과와 Controller 세션 전달 구현 관리 | 진행 UI 런타임 3개, `BattleRewardPresentationTests.cs`, 관련 기록 문서와 로컬 화면 증거 | AI 구현·테스트·Game View 검증 보조, 신규 5/5·StageProgression 60/60·전체 EditMode 182/182·양쪽 씬 문제 0·최종 Console Error/Warning 0; 씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드·화면 승인 대기 |
| 2026-07-20 | 전투 보상 RW-05 | 일반·보스 선택/건너뛰기와 패배·재시작 반복 회귀, 실제 흐름과 1차 범위 기록 마감 관리 | `BattleRewardSystemValidationTests.cs`, 전투 보상 문서 4종, AI·역할·프로젝트 구조 기록과 로컬 화면 증거 | AI 테스트·Unity MCP·화면·기록 보조, 각 흐름 10회·신규 5/5·CoreLoop 122/122·StageProgression 65/65·카드 사용 5/5·전체 187/187·씬 문제 0·시각 98/100·최종 Console 0; 런타임·씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-00 | 적 5종 프로필·공개 관측 AI·카드 효과·전투 생성·보상 연결과 상대 선택 분업 경계 수립 | `Docs/enemy-combat-profile-design.md`, 개발 명세·구현 계획·진행 기록, README·AI·역할 기록 | AI 규칙·현재 코드 대조와 문서 초안 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 이천서 기획 기준 최종 검토 필요 |
| 2026-07-20 | 적 전투 프로필 EP-01 | 적 5종 프로필·안전 미리보기·교체 가능 정책·결정적 적 전투 설정 기반 구현 관리 | `Assets/01. Scripts/Runtime/CoreLoop/EnemyAI`, `EnemyProfiles`, `CoreLoopBattle.cs`, `SimpleEnemyPolicy.cs`, `EnemyProfileFoundationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 12/12·CoreLoop 134/134·전체 EditMode 199/199·Console Error 0; 씬·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-02 | 공개 관측·유효 행동 후보·결정 재검증, 적 카드 행위자/대상·선택 옵션과 대칭 폴드 구현 관리 | `EnemyAI`, `CoreLoopBattle.cs`, `CardEffectResolver.cs`, 카드 처리기 4개, `RoundResolver.cs`, `EnemyCommonActionTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 8/8·CoreLoop 142/142·전체 EditMode 207/207·Console Error 0; UI·씬·프리팹·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드·기획 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-03 | 공개 구성 기반 숫자 추론, 총잡이·광신도·사기꾼 정책과 수정 구슬 선택 구현 관리 | 숫자 추론·공통 선택기·일반 적 정책 3개, 정책·프로필 카탈로그, `BlackjackDeck.cs`, `EnemyNormalPolicyTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 10/10·CoreLoop 152/152·전체 EditMode 217/217·Console Error 0; UI·씬·프리팹·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드·밸런스 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-04 | 집행관 방해 정책, 해머 비용·나이프 후속 평가, 엘리트 추론 표시와 높은 등급 보상 연결 구현 관리 | `EnforcerEnemyPolicy.cs`, `EnemyInferenceDisplayModel.cs`, 관측·정책·프로필 카탈로그, `EnemyElitePolicyTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 11/11·CoreLoop 163/163·전체 EditMode 228/228·Console Error/Warning 0; UI·씬·프리팹·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드·밸런스 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-05 | 최종 보스 3구간 정책, 강행동 예고, 추론 방향 표시와 높은 등급 보상 뒤 런 승리 구현 관리 | `FinalBossEnemyPolicy.cs`, `BossCombatDisplayModel.cs`, 정책·프로필 카탈로그, `EnemyBossPolicyTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 16/16·CoreLoop 179/179·전체 EditMode 244/244·Console Error 0; UI·씬·프리팹·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드·밸런스 승인 대기 |
| 2026-07-20 | 적 전투 프로필 EP-06 | 선택 프로필 키를 실제 진행 전투의 전용 덱·영혼·정책·보상으로 변환하고 기존 호환·반복 상태 격리·엘리트/보스 결과 검증 관리 | `StageDefinition.cs`, `StageBattleFactory.cs`, `StageProgressionSession.cs`, `StageProgressionRuntime.cs`, `CoreLoopBattle.cs`, `EnemyProfileStageIntegrationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 16/16·StageProgression 81/81·CoreLoop 179/179·전체 EditMode 260/260·실제 런·씬 문제 0·Console Error 0; 상대 선택 UI·씬·프리팹·패키지·외부 에셋과 다른 담당 파일 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-00 | 후보 2명 비교·확정, 선택 키의 실제 전투 연결, 등급별 추론·보스 예고 UI와 EUI-00~EUI-05 구현·검증 기준 및 담당 변경 관리 | `Docs/enemy-selection-combat-ui-design.md`, 개발 명세·구현 계획·진행 기록, README·AI·역할 기록 | AI 규칙·현재 구조 대조와 문서 초안 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 상대 선택·전투 정보 UI 책임자는 이천서, 최종 기획 검토 필요 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-01 | 결정적 후보 2명·불변 제안·엘리트 제한, 선택 대기 상태와 세션 주입·보스 우회·고정 진행 호환 구현 관리 | `Runtime/StageProgression/OpponentSelection`, `RunProgress.cs`, `StageProgressionSession.cs`, `StageProgressionState.cs`, `OpponentSelectionFoundationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP 검증 보조, 신규 13/13·StageProgression 94/94·CoreLoop 179/179·전체 EditMode 273/273·스크립트 진단·최종 Console 통과; UI·씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-02 | 후보 안전 미리보기 비교·단일 집중·확정 가능 상태와 선택 중 씬 이동 차단·프로토타입 Runtime 활성화 구현 관리 | `StageProgressionPresentation.cs`, `StageProgressionView.cs`, `StageProgressionController.cs`, `StageProgressionRuntime.cs`, `OpponentSelectionPresentationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·화면 검증 보조, 신규 9/9·StageProgression 103/103·CoreLoop 179/179·전체 EditMode 282/282·스크립트 진단·두 해상도 화면 통과; 씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드·화면 승인 대기 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-03 | OfferId+ProfileKey 원자적 확정, 선택 프로필 전투·보상·두 번의 선택·고정 보스·재시작 통합 관리 | `StageProgressionSession.cs`, `StageProgressionController.cs`, `OpponentSelectionIntegrationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·실제 씬 흐름 검증 보조, 신규 14/14·StageProgression 117/117·CoreLoop 179/179·전체 EditMode 296/296·정적 진단·집행관 전투 씬 전환 통과; 씬·패키지·외부 에셋 변경 없음, 이천서 최종 코드 승인 대기 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-04 | 공개 숫자 추론 공유·등급별 안전 표시 스냅샷·일반/엘리트/보스 전투 정보와 보스 예고·720p 반응형 화면 통합 관리 | `EnemyCombatDisplaySnapshot*.cs`, `EnemyObservationFactory.cs`, CoreLoop 표시·View·Controller, `EnemyCombatPresentationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·화면 검증 보조, 신규 14/14·StageProgression 117/117·CoreLoop 193/193·전체 EditMode 310/310·1280×720·1920×1080 화면 통과; 씬·프리팹·패키지·외부 에셋 변경 없음, 이천서 최종 코드·화면 승인 대기 |
| 2026-07-20 | 상대 선택·전투 정보 UI EUI-05 | 후보 조합·두 선택·보상·고정 보스·재시작과 상태 격리 반복 회귀, 실제 씬·화면·기록 마감 관리 | `OpponentSelectionSystemValidationTests.cs`, EUI 문서 4종, README·AI·프로젝트 구조·역할 기록 | AI 테스트 초안·Unity MCP·실제 흐름·화면·기록 보조, 신규 5/5·StageProgression 122/122·CoreLoop 193/193·전체 315/315·두 씬·두 해상도·Console 0; 런타임·씬·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-20 | 정식 런 진행 RF-00 | 최신 골드·상점 규칙 대조, HONG 인계 범위·임시 경제 수치·RF-01~RF-05 구현·검증 기준 수립 | `Docs/formal-run-flow-*.md` 4종, README·AI·역할 기록 | AI 검색·구조 대조·문서 초안 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 이천서 기획·통합 기준 최종 검토 필요 |
| 2026-07-21 | 현행 전투 규칙 이관 | 폴드 제거, 체인지 전투 누적 비용과 `현재 영혼 > 비용` 조건, 기존 비공개 카드 공개 폐기, 적 AI·UI·진행 세션 호환 수정 | CoreLoop·EnemyAI·CoreLoop UI·StageProgression 런타임, EditMode 테스트, 관련 문서 | AI 구현·검증·기록 보조, Unity 전체 EditMode 306/306 통과·컴파일 오류 0; `GameScene`·패키지·외부 에셋·HONG 담당 코드 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 카드 사용 CU-M01 | 위협용 해머의 상대 공개 카드 제거, 스탠드 취소·비공개 교체, 적 AI 대상 정책과 GameScene 표시 이관 관리 | `ThreatHammerEffectHandler.cs`, 카드 효과 선택·관측, 집행관·최종 보스 정책, `GameScenePresentation.cs`, 관련 테스트·문서 | AI 구현·테스트·Unity MCP·기록 보조, 전체 EditMode 308/308·컴파일 오류 0; `GameScene.unity`·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 카드 사용 CU-M02 | 보위 나이프 비버스트 강제 폐기와 공개 합 기반 중간 버스트·전체 합 최종 승부 경계 이관 관리 | `MilitaryKnifeEffectHandler.cs`, `CoreLoopBattle.cs`, `RoundResolver.cs`, 카드 효과 처리기, CoreLoop·StageProgression 테스트·관련 문서 | AI 구현·테스트·Unity MCP·기록 보조, 영향 13/13·전체 EditMode 309/309·Console Error/Warning 0; `GameScene`·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 카드 사용 CU-M03 | 플레이어·상대 비공개 카드의 플레이어 시점 GameScene 최좌측 표시와 규칙 손패 순서·정보 은닉 보존 관리 | `GameScenePresentation.cs`, `CoreLoopPresentationTests.cs`, 관련 기록 문서 | AI 원인 추적·구현·테스트·Unity MCP 화면 검증 보조, 신규 1/1·전체 EditMode 310/310·실제 Game View 양측 최좌측 통과; `GameScene.unity`·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 악마 계약 DC-00 | 계약 비용·후보·수명·시작 덱·우선 악마 4종, 데이터·전투·런·UI·AI 경계와 DC-01~DC-07 구현 기준 수립 | `Docs/demonic-contract-*.md` 4종, README·AI 활용·역할 기록, 카드 사용 문서 상태 정리 | AI 규칙·현재 코드 대조와 문서 초안 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 이천서 기획·개발 기준 최종 검토 필요 |
| 2026-07-22 | 악마 계약 DC-00 공통 규칙 개정 | 개별 대가 영혼 0 즉시 사망, 선택·버스트 범위, 생성 카드 합계, 동일 악마 추가 계약, 적 대칭과 계약 임시 카드의 전투 종료 원상복구 기준 확정 | `Docs/rule.md`, `game-design-document.md`, `demonic-contract-*.md` 4종과 공통 기록 문서 | 이천서 기획 결정, AI 충돌 검색·문서 개정 보조; 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음 |
| 2026-07-22 | 악마 계약 DC-01 | 네 악마 정의·카탈로그, 런 최초/현재 악마 덱, 후보 3장과 버림 보충 전투 덱, 런→전투 변환·독립 시드 구현 관리 | `CoreLoop/DemonContracts`, `RunDemonDefinition.cs`, `PlayerRunState.cs`, `StageBattleFactory.cs`, `CoreLoopBattle.cs`, DC-01 테스트·기록 문서 | AI 구현·테스트·Unity MCP·기록 보조, 대상 19/19·전체 EditMode 329/329·컴파일 오류 0; CU-M03 표시 회귀도 코드 투영만 복구, 씬·프리팹·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 악마 계약 DC-02 | 엄격한 영혼 비용·전투당 1회·후보 필수 선택·상호작용 ID·활성 계약·개별 대가 패배·세션 전달 구현 관리 | `DemonContractSelection.cs`, `DemonContractResolver.cs`, `CoreLoopState.cs`, `CoreLoopBattle.cs`, 두 세션, DC-02 테스트·기록 문서 | AI 구현·테스트·Unity MCP·기록 보조, 대상 13/13·전체 EditMode 342/342·컴파일 오류 0; 개별 악마 효과·UI·씬·프리팹·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 악마 계약 DC-03 | 벨페고르 플레이어 전용 덱 위 미리보기, 동일 물리 ID 히트·덱 아래 이동, 상대 스탠드 뒤 행동 종료 자동 스탠드와 정보 은닉 구현 관리 | `BlackjackDeck.cs`, `CoreLoopBattle.cs`, 계약 선택·Resolver·벨페고르 처리기, `BelphegorDemonContractTests.cs`, `BelphegorStageIntegrationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·기록 보조, 대상 9/9·전체 EditMode 351/351·컴파일 오류 0; UI·씬·프리팹·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-22 | 악마 계약 DC-04 | 마몬 주입식 주사위·차례/최종 선택, 레비아탄 리볼버 후속 버스트·영혼 대가와 정보 은닉 구현 관리 | 마몬·레비아탄 처리기, `CoreLoopBattle.cs`, 계약 선택·Resolver, 합계·종료 원인, `MammonAndLeviathanDemonContractTests.cs`, 관련 문서 | AI 구현·테스트·Unity Editor API·기록 보조, 대상 11/11·전체 EditMode 362/362·컴파일 오류 0; 사탄·UI·씬·프리팹·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-23 | 악마 계약 DC-05 | 계약 비용 확인·후보 3장·활성 상태·소유자 전용 미리보기와 독립/런 UI 통합 관리 | 계약 표시 모델, CoreLoop 표시·View·Controller, `GameManager.cs`, `DemonContractPresentationTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·두 해상도 시각 교정·기록 보조, 대상 7/7·전체 EditMode 369/369·두 씬·Console 0; 사탄 비활성, 씬·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-23 | 악마 계약 DC-06 | 사탄 정상 차례 카운터·스탠드/버스트 제한·영혼 대가·양면 권능·임시 카드 수명·UI 통합 관리 | 사탄 계약/권능 처리기, 덱·전투·표시 변경, `SatanDemonContractTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·두 해상도 시각 교정·기록 보조, 대상 8/8·전체 EditMode 377/377·두 씬·Console Error 0; 씬·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-23 | 악마 계약 DC-07 | 광신도 계약 정책, 적 소유 4종 대칭 처리·Cultist 전용 덱·안전 표시·반복 회귀 통합 관리 | 전투·계약 Resolver/표시, 적 후보·결정·광신도 정책, `StageBattleFactory.cs`, `EnemyDemonContractTests.cs`, 관련 문서 | AI 구현·테스트·Unity MCP·기록 보조, 대상 12/12·CoreLoop 260/260·전체 389/389·GameScene Full HD·Console 0; 씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-23 | 악마 계약 DC-08 | 광신도 계약 선택 편향 측정, 사탄·레비아탄 효용 조건과 벨페고르·마몬 결정적 균형 분산, 자동 전투 회귀 관리 | `CultistEnemyPolicy.cs`, `CultistContractBalanceTests.cs`, 계약·적 프로필·공통 기록 문서 | AI 기준선 재현·코드·테스트·Unity MCP·문서 보조, 400시드·100자동전투·대상 8/8·CoreLoop 268/268·전체 397/397·GameScene Full HD·Console 0; 씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-23 | 자동 발동 카드 AC-00 | 최초 배분 예외·공개 유입·효과·버스트 순서, 5종 카드 임시 결정과 데이터·전투·UI·AI·런 구현 기준 수립 | `Docs/automatic-card-*.md` 4종, README·AI 활용·역할 기록 | AI 규칙·현재 코드 대조와 문서 초안 보조, 코드·테스트·씬·패키지·외부 에셋 변경과 Unity 재검증 없음; 이천서 기획·개발 기준 최종 검토 필요 |
| 2026-07-25 | 자동 발동 카드 AC-01 | 자동 카드 5종 정의, 양측 공개 유입·보류 선택·입력 잠금·수동 효과 연속 처리 기반 구현 관리 | 자동 카드 Resolver·선택 모델, CoreLoop·기존 공개 드로우 처리기, `AutomaticCardFoundationTests.cs`, 관련 문서 | AI 구조 대조·구현·테스트·Unity MCP·기록 보조, 대상 15/15·CoreLoop 283/283·전체 412/412·컴파일 오류 0; UI·세션·보상·적 정책·씬·패키지·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-25 | 자동 발동 카드 AC-02 | 독극물 계약 제한 스탠드·영혼 지불/0 즉시 패배·물리 카드별 승리 회복과 양측 대칭 구현 관리 | `PoisonEffectHandler.cs`, `AutomaticCardBattleState.cs`, 자동 Resolver·전투·영혼 변경, `PoisonAutomaticCardTests.cs`, 관련 문서 | AI 구조 대조·구현·테스트·별도 Unity Headless·기록 보조, 대상 12/12·CoreLoop 295/295·전체 424/424·컴파일 오류 0; UI·세션·보상·적 정책·씬·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-25 | 자동 발동 카드 AC-03 | 거짓말 탐지기 선언·공개/소유자 전용 결과·적 비교 관측·숨은 카드 교체와 라운드/전투 종료 지식 폐기 구현 관리 | `LieDetectorEffectHandler.cs`, `LieDetectorResult.cs`, 자동 전투 상태·전투·적 관측·체인지/해머 변경, `LieDetectorAutomaticCardTests.cs`, 관련 문서 | AI 구조 대조·테스트 우선 구현·Unity MCP·기록 보조, 대상 10/10·CoreLoop 305/305·전체 434/434·컴파일 오류 0·Test Framework 결과 저장 안내 3건만 확인; `GameScene`·UI·세션·보상·적 자동 정책·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-25 | 자동 발동 카드 AC-04 | 화염 방사기 소유자→상대 순차 폐기·회중시계 수동 카드 재활성화와 원본 유지/폐기·양측 소유권 구현 관리 | `FlamethrowerEffectHandler.cs`, `PocketWatchEffectHandler.cs`, 자동 Resolver·카드 상태, `FlamethrowerAndPocketWatchTests.cs`, 관련 문서 | AI 구조 대조·테스트 우선 구현·Unity MCP·기록 보조, 대상 11/11·CoreLoop 316/316·전체 445/445·컴파일 오류 0; `GameScene`·UI·세션·보상·적 자동 정책·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-25 | 자동 발동 카드 AC-05 | 부활초 양측 영혼 조건·승패 없는 라운드 전이·라운드 상태 및 부모 효과 취소 구현 관리 | `ResurrectionHerbEffectHandler.cs`, `RoundTransition.cs`, 자동 Resolver·전투 흐름, `ResurrectionHerbAutomaticCardTests.cs`, 관련 문서 | AI 구조 대조·테스트 우선 구현·Unity MCP·기록 보조, 대상 11/11·CoreLoop 327/327·전체 456/456·컴파일 오류 0; `GameScene`·UI·세션·보상·적 자동 정책·프리팹·Packages·외부 에셋 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-25 | 자동 발동 카드 AC-06 | 독립/런 입력·안전 표시·공개 정보 적 정책·일반 보상 5종·사기꾼 탐지기 통합 관리 | 자동 선택 정책, CoreLoop·StageProgression 세션/표시, View·Controller·`GameManager.cs`, 보상·적 프로필, AC-06 통합 테스트와 관련 문서 | AI 구조 대조·구현·테스트·검증·기록 보조, 대상 15/15·Unity 비의존 461/461·컴파일 성공; Editor 전용 10건과 두 씬·두 해상도·Console 후속 검증, `GameScene.unity`·Packages·외부 에셋 무변경, 이천서 최종 승인 대기 |
| 2026-07-26 | 게임 저장·이어하기 SV-00 | 체크포인트·런 예약·저장 범위·버전·원자 파일·백업 복구·복원·RF 의존성 구현 기준 수립 | `Docs/save-system-*.md` 4종, README·AI 활용·역할·구조 기록 | AI 기준 문서·현재 SaveLoad/런 상태 대조와 문서 초안 보조, 코드·테스트·씬·프리팹·Packages·외부 에셋 변경과 Unity 재검증 없음; 이천서 최종 기획·개발 승인 필요 |
| 2026-07-26 | 게임 저장·이어하기 SV-01 | 스키마 1 순수 스냅샷·타입화된 불변식 검증·현행 안정 상태 캡처 구현 관리 | `StageProgression/Save` 4개 스크립트, `PlayerRunState.cs`, `RunSaveSnapshotTests.cs`, 저장·공통 기록 문서 | AI 구조 대조·구현·테스트·Unity MCP·기록 보조, 대상 7/7·StageProgression 141/141·전체 EditMode 483/483·컴파일 오류 0, Test Framework 안내 3건 분리; 씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-26 | 게임 저장·이어하기 SV-02 | 버전 JSON·안정 문자열·임시 파일 재검증·원자 교체·백업 불러오기 구현 관리 | `SaveLoad` 런 저장 6개 스크립트, `RunSaveRepositoryTests.cs`, 저장·공통 기록 문서 | AI 구조 대조·구현·실패 주입 테스트·Unity MCP·기록 보조, 대상 8/8·StageProgression 149/149·전체 EditMode 491/491·컴파일 오류 0, 기반 시설 안내 10건 분리; 씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-27 | 게임 저장·이어하기 SV-03 | 일반/악마 덱·마지막 발급 ID·안정 스테이지·상대/보상 순번 캡처·새 세션 원자 복원 구현 관리 | `RunRestoreFactory.cs`, 런 상태·세션·생성기 변경, `RunRestoreFactoryTests.cs`, 저장·공통 기록 | AI 구조 대조·테스트·구현·Unity MCP HTTP 연결·회귀 분석·기록 보조, 대상 7/7·StageProgression 156/156·전체 EditMode 500/500·컴파일 오류 0; 기반 시설 출력 6건 분리, `GameScene`·씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음, 이천서 최종 승인 대기 |
| 2026-07-28 | 게임 저장·이어하기 SV-04 | 시작 악마·카드 보상·런 종료 안정 체크포인트와 저장 실패 보류·동일 스냅샷 재시도·다음 진행 차단 구현 관리 | `RunSaveCoordinator.cs`, `RunSaveSerializer.cs`, `RunSaveCoordinatorTests.cs`, 저장 문서 4종·공통 기록 | 이천서 기획·구현·검증 책임, AI 구조 대조·테스트 우선 구현·실패 분석·Unity MCP·기록 보조; 대상 8/8·StageProgression 180/180·CoreLoop 362/362·전체 542/542·게임 코드 오류 0, SV04-I03은 실제 RF 상점·사건 API 대기; Runtime·UI·GameScene·씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음 |
| 2026-07-28 | 게임 저장·이어하기 SV-05 | 원자 런 예약·새 게임 보호·예약 재개·이어하기 세션 교체·시작 악마/손상·버전/저장 실패 UI 구현 관리 | `RunReservation*`, `RunSaveFlow.cs`, `RunSavePresentation.cs`, Runtime·StageProgression UI, `RunSaveFlowTests.cs`, 저장·공통 기록 | 이천서 기획·구현·검증 책임, AI 테스트 우선 구현·기존 주입 테스트 호환 실패 분석·Unity MCP 자동/화면 검증·기록 보조; 전용 9/9·StageProgression 189/189·CoreLoop 362/362·전체 551/551·두 해상도·Console 0, 실제 프로세스 재실행·RF 체크포인트는 SV-06; GameScene·씬·프리팹·Packages·외부 에셋·오픈소스 변경 없음 |

코어 루프 1~4단계, 독립 런·스테이지 SP-00~SP-04, 전투 행동 BA-00~BA-05, 카드 사용 CU-00~CU-06과 전투 보상 RW-00~RW-05의 구현·검증·문서 마감이 완료되었다. 이천서는 EP-00부터 적 전투 프로필·AI·카드 효과·실제 전투 변환을 담당하며 EP-01~EP-06에서 최초 5종 범위를 완료하고, EP-R01~R06에서 겁쟁이 도박사를 포함한 적 6종과 일반·엘리트 특수 정책·보스 고정 계약 단계를 관리한다. EUI-00부터 상대 후보 생성·선택 UI와 전투 중 등급별 적 정보 UI도 이천서 담당으로 변경했으며 EUI-05까지 1차 범위를 마감했다. RF-00에서 HONG 인계용 문서 4종을 작성했고, RFM01 상점 MVP 보정에 이어 RF-01 골드 상태·정산과 RF-02 상점 도메인을 이천서가 선행 구현했다. RF-03~05 정식 순서·UI·마감은 아직 완료되지 않았다. 아트는 Shim0Hwan이 담당한다.

### 3.2 Shim0Hwan(Git 식별자)

| 날짜 | 영역 | 실제 수행 내용 | 관련 파일/산출물 | 검증/비고 |
| --- | --- | --- | --- | --- |
| 2026-07-19~20 | 아트·레벨 환경 | 전투 공간용 FBX·텍스처·재질·셰이더와 `LevelDesign` 씬 분위기 작업 | `Assets/05. Arts`, `Assets/00. Scenes/LevelDesign.unity`, 관련 렌더링 에셋 | Git 커밋으로 파일 기여 확인, 개별 외부 에셋 출처·라이선스는 별도 대장 확인 필요 |
| 2026-07-20 | 도구·카메라 | 시네머신 패키지 참조 추가 | `Packages`, 관련 프로젝트 설정 | Git 커밋 확인, 최종 사용 범위·버전 확인 필요 |

### 3.3 HONG(Git 식별자)

| 날짜 | 영역 | 실제 수행 내용 | 관련 파일/산출물 | 검증/비고 |
| --- | --- | --- | --- | --- |
| 2026-07-20 | 프로젝트 정리 | 폴더 구조 정리와 TMP 기본 리소스 추가 | 프로젝트 폴더 구조, `Assets/TextMesh Pro` | Git 커밋으로 확인, 게임 기능 구현은 아직 확인되지 않음 |
| 예정 | 골드·상점·정식 런 진행 | 승리 골드, 구매·제거·휴식·나가기와 `전투→상점→전투→상점→보스` 연결 | `Docs/formal-run-flow-*.md`, 예정 `RunFlow`·`Shop` 경로 | RF-00 인수인계만 완료, 실제 구현 완료로 기록하지 않음 |

## 4. 실제 구현 기록 규칙

각 작업이 완료될 때 다음 정보를 한 줄 이상 추가한다.

- 날짜와 작업자 이름
- 구현하거나 수정한 기능
- 핵심 파일·씬·프리팹·데이터 경로
- 개인이 직접 결정하고 해결한 기술적 문제
- 공동 작업이면 각자의 기여 경계
- 테스트 방법과 결과
- AI 보조를 사용했으면 AI가 초안한 부분과 사람이 검토·수정한 부분

커밋 수나 코드 줄 수만으로 기여를 판단하지 않는다. 설계, 구현, 통합, 테스트, 리소스 제작을 실제 산출물과 함께 기록한다.

## 5. 코어 루프 업무 대장

아래 표는 최초 계획과 완료 상태를 함께 추적한다. 실제 기여 증거는 3절에 기록한다.

| 작업 ID | 작업 | 예정 책임 | 완료 증거 |
| --- | --- | --- | --- |
| CL-01 | 런·전투·라운드 상태 흐름 | 이천서(AI 보조), 순수 상태 흐름 완료 | 상태 전이 테스트 통과; 플레이 가능한 씬은 CL-05에서 연결 |
| CL-02 | 덱, 드로우, 에이스 합계 | 이천서(AI 보조), 완료 | EditMode 계산·시드·순환 테스트 통과 |
| CL-03 | 히트·스탠드와 단순 적 AI | 이천서(AI 보조), 완료 | 양측 차례 진행 테스트 통과 |
| CL-04 | 버스트·합계 비교·영혼 피해 | 이천서(AI 보조), 완료 | EditMode 판정·피해·중복 방지 테스트 통과 |
| CL-05 | 최소 전투 UI와 입력 연결 | 이천서(AI 보조), 완료 | EditMode 표시 모델 및 Game View 수동 검증 통과 |
| CL-06 | 전투 종료·재시작 | 이천서(AI 보조), 완료 | 승리·패배 표시 및 10회 재시작 테스트 통과 |
| CL-07 | 통합 검증과 기록 | 이천서(AI 보조), 완료 | EditMode 27/27, 승리·패배·재시작, 씬·Console 검증과 문서 갱신 완료 |

실제 담당자가 정해지면 `예정 책임`을 갱신하고, 완료 후 3절의 해당 팀원 기록으로 옮긴다.

## 6. 런·스테이지 진행 업무 대장

아래 항목은 코어 루프와 분리된 신규 작업의 계획과 실제 완료 상태를 추적한다. 완료된 구현 기여는 3절에도 기록한다.

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| SP-00 | 기준 확정과 회귀 기준 기록 | 이천서(AI 검증 보조) | 완료 |
| SP-01 | 런·스테이지 순수 상태 기반 | 이천서(AI 보조) | 완료 |
| SP-02 | 전투 시스템 연결과 지속 영혼 동기화 | 이천서(AI 보조) | 완료 |
| SP-03 | 최소 진행 UI와 전용 씬 | 이천서(AI 보조) | 완료 |
| SP-04 | 전체 흐름 검증과 기록 | 이천서(AI 보조) | 완료 |

## 7. 전투 행동 확장 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| BA-00 | 기획·개발 명세·구현 계획·진행 기록 | 이천서(AI 문서 보조) | 완료 |
| BA-01 | 상태·카드 이동 기반 | 이천서(AI 구현·검증 보조) | 완료 |
| BA-02 | 폴드 규칙·라운드 종료 | 이천서(AI 구현·검증 보조) | 완료 |
| BA-03 | 체인지 후보 선택·라운드당 제한 | 이천서(AI 구현·검증 보조) | 완료 |
| BA-04 | 표시·버튼·입력 연결 | 이천서(AI 구현·화면·검증 보조) | 완료 |
| BA-05 | 런 연동·전체 검증·기록 마감 | 이천서(AI 구현·검증·기록 보조) | 완료 |
| BA-M01 | 현행 폴드 삭제·체인지 전투 누적 비용 이관 | 이천서(AI 구현·검증·기록 보조) | 완료, 전체 EditMode 306/306 |
| BA-R01 | 적 공통 체인지·결정적 후보·사기꾼 비용 1 | 이천서(AI 구현·테스트·검증·기록 보조) | 완료, EP-R05 정책 12/12·CoreLoop 466/466·전체 655/655 |

## 8. 카드 사용 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| CU-00 | 기획·개발 명세·구현 계획·진행 기록 | 이천서(AI 문서 보조) | 완료 |
| CU-01 | 카드 정의·인스턴스 사용 상태 | 이천서(AI 구현·검증 보조) | 완료 |
| CU-02 | 효과 선택·명령·종료 기반 | 이천서(AI 구현·검증 보조) | 완료 |
| CU-03 | 리볼버 세로 기능 | 이천서(AI 구현·검증 보조) | 완료 |
| CU-04 | 수정 구슬·해머·나이프 | 이천서(AI 구현·검증 보조) | 완료 |
| CU-05 | 화면 입력·런 전투 연결 | 이천서(AI 구현·화면·검증 보조) | 완료 |
| CU-06 | 전체 회귀·실제 흐름·기록 마감 | 이천서(AI 구현·검증·기록 보조) | 완료 |
| CU-M01 | 위협용 해머 현행 규칙 이관 | 이천서(AI 구현·검증·기록 보조) | 완료 |
| CU-M02 | 보위 나이프·중간 버스트 현행 규칙 이관 | 이천서(AI 구현·검증·기록 보조) | 완료, 전체 EditMode 309/309 |
| CU-M05 | 공개 적 카드 호버 정보·미공개 정보 차단 | 이천서(AI 구현·검증·기록 보조) | 완료, 신규 3/3·전체 EditMode 622/622 |
| CU-M06 | GameScene 리볼버 준비·예측·성공/실패 연출 연결 | 이천서(AI 구현·검증·기록 보조) | 완료, 대상 2/2·CoreLoop 438/438·전체 EditMode 627/627 |

## 9. 전투 보상 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| RW-00 | 기획·개발 명세·구현 계획·진행 기록 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| RW-01 | 보상 카탈로그·결정적 생성·런 덱 추가·초기화 | 이천서(AI 구현·검증 보조) | 완료 |
| RW-02 | 보상 선택 상태·선택·건너뛰기·완료 목적지 | 이천서(AI 구현·검증 보조) | 완료 |
| RW-03 | 전투 결과·세션·다음 전투 덱 연동 | 이천서(AI 구현·검증 보조) | 완료 |
| RW-04 | 진행 화면 표시·선택·건너뛰기 입력 | 이천서(AI 구현·화면·검증 보조) | 완료 |
| RW-05 | 반복 회귀·실제 흐름·기록 마감 | 이천서(AI 구현·검증·기록 보조) | 완료 |

## 10. 적 전투 프로필 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| EP-00 | 기획·개발 명세·구현 계획·진행 기록과 분업 경계 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| EP-01 | 프로필·카탈로그·정책 인터페이스·기본 정책 호환 | 이천서(AI 구현·검증 보조) | 완료 |
| EP-02 | 공개 관측·공통 행동·적 카드 실행 경계 | 이천서(AI 구현·검증 보조) | 완료 |
| EP-03 | 총잡이·광신도·사기꾼 일반 정책 | 이천서(AI 구현·검증 보조) | 완료 |
| EP-04 | 집행관 엘리트 정책·높은 등급 보상 | 이천서(AI 구현·검증 보조) | 완료 |
| EP-05 | 최종 보스 3구간 정책·런 승리 | 이천서(AI 구현·검증 보조) | 완료 |
| EP-06 | 선택 프로필 전투 변환·반복 회귀·기록 마감 | 이천서(AI 구현·검증·기록 보조) | 완료 |
| EP-R00 | 겁쟁이 도박사 최신 기획 문서 이관 | 이천서(AI 노션·문서 대조 보조) | 완료, 코드·테스트 미변경 |
| EP-R01 | 겁쟁이 도박사 프로필·18장 덱·저위험 정책·전투/선택 연결 | 이천서(AI 구현·검증·기록 보조) | 완료, 전용 9/9·전체 534/534·게임 코드 오류 0 |
| EP-R02 | 공개된 비공개 카드 관측·전 프로필 확정 리볼버 대응 | 이천서(AI 구현·검증·기록 보조) | 완료, 전용 5/5·전체 619/619 |
| EP-R03 | Notion v0.7 적 6종 재명세·결정 게이트 | 이천서(AI Notion·문서 대조 보조) | 완료, 코드·테스트 미변경 |
| EP-R04 | 적 6종 영혼·전용 덱·표시·아스모데우스 제한 이관 | 이천서(AI 구현·검증·기록 보조) | 완료, 전용 14/14·전체 640/640 |
| EP-R05 | 일반·엘리트 특수 정책·전용 계약·체인지·독극물 | 이천서(AI 구현·테스트·검증·기록 보조) | 완료, 정책 12/12·CoreLoop 466/466·StageProgression 189/189·전체 655/655 |
| EP-R06 | 보스 고정 계약 단계·현재 계약 표시·전투 경계 검증 | 이천서(AI 구현·테스트·검증·기록 보조) | 전투 범위 완료, 전용 10/10·전체 665/665; 골드·저장·정식 런 UI는 HONG RF로 분리 |

## 11. 상대 선택·적 전투 정보 UI 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| EUI-00 | 기획·개발 명세·구현 계획·진행 기록과 담당 변경 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| EUI-01 | 후보 생성·선택 대기 상태 기반 | 이천서(AI 구현·검증 보조) | 완료 |
| EUI-02 | 후보 비교·지정·확정 화면 | 이천서(AI 구현·화면·검증 보조) | 완료 |
| EUI-03 | 선택 프로필의 실제 전투·보상 통합 | 이천서(AI 구현·검증 보조) | 완료 |
| EUI-04 | 일반·엘리트·보스 전투 정보 UI | 이천서(AI 구현·화면·검증 보조) | 완료 |
| EUI-05 | 반복 회귀·씬·화면·기록 마감 | 이천서(AI 테스트·Unity MCP·화면·기록 보조) | 완료 |

## 12. 정식 런 진행 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| RF-00 | 기획·개발 명세·구현 계획·진행 기록과 분업 경계 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| RFM01 | GameScene 상점 복수 카드 구매·서비스 가격 단계·나가기 UI 선반영 | 이천서(AI 구현·테스트·MCP·문서 보조) | 완료, 전용 3/3·전체 626/626·실제 GameScene 화면 |
| RF-01A | 런 골드 상태·저장 왕복 | 이천서(AI 구현·검증 보조) | 완료, 대상 7/7·두 어셈블리 컴파일 |
| RF-01B | 적별 승리 정산 | 이천서(AI 구현·검증 보조) | 완료, 대상 7/7·RF-01 전체 14/14·두 어셈블리 컴파일 |
| RF-02 | 상점 구매·제거·휴식·나가기 | 이천서(AI 구현·검증 보조) | 완료, 대상 11/11·두 어셈블리 컴파일 |
| RF-03 | 두 상점과 정식 런 순서 통합 | 이천서(AI 구현·검증 보조) | 완료, RF03 12건·RF-01~03 누적 37/37·두 어셈블리 컴파일 |
| RF-04 | 진행 화면·씬 연결 | 이천서(AI 구현·검증 보조) | 완료, Runtime·화면·씬 연결 및 두 해상도 확인 |
| RF-05 | 반복 회귀·실제 흐름·기록 마감 | 이천서(AI 구현·검증 보조) | 완료, 정식 런 왕복·상점 경제·보스 승리·전체 798/798 |

## 13. 악마 계약 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| DC-00 | 기획·개발 명세·구현 계획·진행 기록과 결정 게이트 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| DC-01 | 계약 정의·런 악마 덱·전투 덱·변환 | 이천서(AI 구현·검증 보조) | 완료, 대상 19/19·전체 EditMode 329/329 |
| DC-02 | 영혼 비용·횟수·후보 3장·선택 상태·세션 전달 | 이천서(AI 구현·검증 보조) | 완료, 대상 13/13·전체 EditMode 342/342 |
| DC-03 | 벨페고르 수직 기능과 정보 은닉 | 이천서(AI 구현·검증 보조) | 완료, 대상 9/9·전체 EditMode 351/351 |
| DC-04 | 마몬 주사위·레비아탄 리볼버 연동 | 이천서(AI 구현·검증 보조) | 완료, 대상 11/11·전체 EditMode 362/362 |
| DC-05 | 플레이어 계약 화면·런 영혼·보상 연결 | 이천서(AI 구현·화면·검증 보조) | 완료, 대상 7/7·전체 EditMode 369/369·두 씬·두 해상도 |
| DC-06 | 사탄 규칙 확정·카운터·권능 카드 구현 | 이천서(AI 구현·화면·검증·기록 보조) | 완료, 대상 8/8·전체 EditMode 377/377·두 씬·두 해상도 |
| DC-07 | 적 AI 계약·반복 회귀·실제 흐름·기록 마감 | 이천서(AI 구현·검증·기록 보조) | 완료, 대상 12/12·CoreLoop 260/260·전체 389/389·Console 0 |
| DC-08 | 광신도 계약 선택 밸런스·실전 검증 | 이천서(AI 구현·검증·기록 보조) | 완료, 400시드·100자동전투·대상 8/8·CoreLoop 268/268·전체 397/397·Console 0 |
| DC-R00 | 최신 노션 계약 규칙 문서 이관 | 이천서(AI 노션 조회·문서 대조 보조) | 완료, 코드·테스트 미변경 |
| DC-R01 | 시작 악마 선택·가변 후보·시작 체크포인트 | 이천서(AI 구현·테스트·MCP 검증 보조) | 완료, 전용 14/14·CoreLoop 349/349·StageProgression 162/162·전체 511/511 |
| DC-R02 | 사탄 카운트 4·활성 계약 양면·별도 권능 제거 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 12/12·CoreLoop 363/363·전체 553/553·게임 코드 오류 0 |
| DC-R03 | 마몬 행동 소비 재굴림·레비아탄 독립 2회 발동 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 14/14·CoreLoop 367/367·전체 556/556·게임 코드 오류 0 |
| DC-R04 1/5 | 바알제붑·메피스토펠레스 수직 구현 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 8/8·400시드·CoreLoop 377/377·전체 566/566 |
| DC-R04 2/5 | 바알제붑 직접 선택 정정·아스모데우스·아자젤 수직 구현 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 바알제붑 8/8·신규 11/11·400시드·100자동전투·CoreLoop 390/390·전체 579/579·게임 코드 오류 0 |
| DC-R04 3/5 | 파이몬·벨리알 수직 구현과 카드 수명·소유권 복구 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 10/10·광신도 9/9·CoreLoop 402/402·전체 591/591·게임 코드 오류 0 |
| DC-R04 4/5 | 바포메트 오망성 생성·소진·덱 초기화·재삽입과 버스트 연속 처리 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 7/7·광신도 9/9·CoreLoop 410/410·전체 599/599·게임 코드 오류 0 |
| DC-R04 5/5 | 루시퍼 최대 5장·건너뛰기·중첩 계약·복합 대가·적 선택 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 전용 10/10·CoreLoop 421/421·전체 610/610·게임 코드 오류 0 |
| DC-R05 | 시작 선택 런타임·활성 계약 입력·후보 레이아웃·통합 검증 | 이천서(AI 구현·테스트·MCP 검증·기록 보조) | 완료, 신규 4/4·CoreLoop 425/425·StageProgression 189/189·합계 614/614·저장 재진입·두 화면 720p/1080p·시각 92점·게임 오류 0 |
| DC-R06 | 적별 계약 이관 | 이천서(AI 구현·테스트·검증·기록 보조) | 완료, 아스모데우스·광신도·집행자·보스 고정 단계; EP-R06 전용 10/10·전체 665/665 |

정식 런의 골드·상점·진행 조정은 HONG의 계획 영역을 유지한다. 계약 작업은 이천서가 소유하는 전투·카드·적 AI 경계 안에서 진행하며, HONG의 `RunFlow`·`Shop` 구현을 선점하지 않는다.

DC-00 공통 규칙 개정으로 동일 악마 추가 계약과 계약 임시 카드의 전투 한정 소유권 기준을 추가했다. DC-R04 3/5에서는 파이몬 선택 뒤 기존 결과의 임시 승자 한 명에게 18 이하 대가를 한 번만 적용하고 새 승자 쪽 파이몬은 같은 라운드에 재연쇄하지 않는 프로토타입 결정을 구현했다. DC-R04 4/5에서는 바포메트를 상대 1~5·소유자 1~3, 대상 드로우 더미 0장 소진·가용 덱 초기화로 구현했으며 수치와 소진 정의는 플레이 테스트 뒤 재검토한다. DC-R04 5/5에서는 루시퍼의 현재 악마 덱 최대 5장·건너뛰기·기본 비용 비중복·별도 인스턴스·재귀 연쇄를 DC-D16 프로토타입으로 구현했다. DC-R05에서는 실제 시작 1장·저장 재진입과 활성 마몬·사탄 입력, 루시퍼 최대 후보 화면을 완료했으며 앞면 월드 카드·호버·면 뒤집기 연출은 별도 아트·씬 후속으로 남는다.

## 14. 자동 발동 카드 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| AC-00 | 기획·개발 명세·구현 계획·진행 기록과 결정 대장 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| AC-01 | 자동 카드 정의·공개 유입·보류 선택·연속 처리 기반 | 이천서(AI 구현·검증 보조) | 완료, 대상 15/15·CoreLoop 283/283·전체 412/412 |
| AC-02 | 독극물 스탠드·영혼 선택과 라운드 승리 회복 | 이천서(AI 구현·검증 보조) | 완료, 대상 12/12·CoreLoop 295/295·전체 424/424 |
| AC-03 | 거짓말 탐지기 선언·소유자 전용 정보·지식 폐기 | 이천서(AI 구현·검증 보조) | 완료, 대상 10/10·CoreLoop 305/305·전체 434/434 |
| AC-04 | 화염 방사기 양측 폐기·회중시계 재활성화 | 이천서(AI 구현·검증 보조) | 완료, 대상 11/11·CoreLoop 316/316·전체 445/445 |
| AC-05 | 부활초 승패 없는 라운드 초기화 | 이천서(AI 구현·검증 보조) | 완료, 대상 11/11·CoreLoop 327/327·전체 456/456 |
| AC-06 | UI·런·보상·적 AI·반복 회귀와 기록 마감 | 이천서(AI 구현·화면·검증·기록 보조) | 구현 완료·Editor 최종 검증 대기, 대상 15/15·Unity 비의존 461/461·컴파일 성공 |
| AC-RV00 | 최신 숫자 1~5 문서 이관 | 이천서(AI 노션 조회·문서 대조 보조) | 완료, 코드·테스트 미변경 |
| AC-RV01 | 정의 키·숫자·보상/런 전투 변환·저장 콘텐츠 리비전 이관 | 이천서(AI 구현·테스트·MCP 검증 보조) | 완료, 전용 11/11·전체 522/522·두 전투 화면·Console 0 |
| AC-RV02 | 적 자동 카드 수량·집행자 라운드별 독극물 주입 | 이천서(AI 구현·테스트·검증·기록 보조) | 완료, EP-R05 정책 12/12·CoreLoop 466/466·전체 655/655 |

정식 런의 골드·상점·진행 조정은 HONG의 계획 영역을 유지한다. 자동 발동 카드는 이천서가 소유하는 전투·카드·적 AI와 현재 전투 UI 경계에서 구현하며, Shim0Hwan의 아트 자산과 HONG의 `RunFlow`·`Shop` 영역을 선점하지 않는다.

## 15. 게임 저장·이어하기 시스템 업무 대장

| 작업 ID | 작업 | 예정 책임 | 현재 상태 |
| --- | --- | --- | --- |
| SV-00 | 기획·개발 명세·구현 계획·진행 기록과 결정 대장 | 이천서(AI 문서·구조 대조 보조) | 완료 |
| SV-01 | 순수 런 스냅샷·검증 | 이천서(AI 구현·검증 보조) | 완료, 대상 7/7·StageProgression 141/141·전체 483/483 |
| SV-02 | 버전 JSON·원자 파일·백업 복구 | 이천서(AI 구현·검증 보조) | 완료, 대상 8/8·StageProgression 149/149·전체 491/491 |
| SV-03 | 현재 런 캡처·새 세션 복원 | 이천서(AI 구현·검증 보조) | 완료, 대상 7/7·StageProgression 156/156·전체 500/500 |
| SV-04 | 안정 체크포인트·결정적 후보 재현 | 이천서(AI 구현·검증 보조) | 현행 범위 완료, 대상 8/8·StageProgression 180/180·CoreLoop 362/362·전체 542/542; SV04-I03은 RF 상점·사건 API 대기 |
| SV-05 | 새 게임 예약·이어하기·저장 실패 UI | 이천서(AI UI·검증 보조) | 완료, 대상 9/9·StageProgression 189/189·CoreLoop 362/362·전체 551/551·두 해상도·Console 0 |
| SV-06 | 골드·상점·사건 통합·재실행 회귀와 기록 마감 | 이천서(AI 통합·검증 보조) | 상점 통합 완료·사건/실제 앱 재실행 잔여, 신규 5/5·전체 798/798 |

이천서는 저장 스냅샷·파일·복원·Runtime·메뉴 통합을 담당한다. HONG의 골드·상점·정식 런 내부 상태는 읽기 전용 저장 계약으로만 연결하며 실제 RF 타입이 생기기 전에 저장 시스템에서 복제하지 않는다. Shim0Hwan의 아트 자산은 별도 소유권을 유지한다.

## 16. 변경 기록

| 날짜 | 작성자 | 변경 |
| --- | --- | --- |
| 2026-07-31 | 이천서 | EPR07에서 광신도 계약 풀을 벨페고르·바알제붑 2종으로 제한하고 공개 카드 2장 기준 선택을 구현해 대상 21/21·전체 EditMode 793/793으로 검증했다. |
| 2026-07-30 | 이천서 | RF-05 반복 검증을 구현해 전체 승리·재시작, 상점 네 행동, 오래된 입력, 세 전투 위치 패배, 네 일반 적 선택·골드와 두 씬 직렬화를 신규 8/8·RF 대상 49/49·전체 EditMode 744/744로 확인했다. AI는 테스트·격리 Unity 실행·문서화를 보조했고 라이브 두 해상도는 MCP handshake 장애로 미검증 기록했다. |
| 2026-07-30 | 이천서 | RF-04 정식 Runtime·진행 상점 UI에 이어 `GameManager`의 진행 세션 행동 위임, 실제 상대 프로필·런 골드 표시, 종료 복귀와 `StageTest → GameScene` 라우팅을 구현했다. 런타임·StageProgression 테스트 어셈블리 컴파일과 RF 비Unity 42/42를 확인했고 실제 Play Mode 왕복은 MCP 연결 복구 후 검증 대상으로 기록했다. |
| 2026-07-30 | 이천서 | RF-02 순수 상점 도메인과 최소 덱 제거 경계를 구현해 대상 11/11·두 어셈블리 컴파일을 확인하고, 이천서 실제 기여와 RF-03~05 후속 범위를 분리 기록했다. |
| 2026-07-30 | 이천서 | RF-01B 적 6종 골드 카탈로그와 승리 전투 참조별 1회 정산을 구현해 대상 7/7·RF-01 전체 14/14·두 어셈블리 컴파일을 확인하고, 이천서 실제 기여와 HONG의 RF-02~05 후속 범위를 분리 기록했다. |
| 2026-07-30 | 이천서 | RF-01A 런 골드 보유·지급·소비·재시작과 저장 캡처·복원을 구현하고 대상 7/7·두 어셈블리 컴파일을 확인; 전체 Unity 회귀는 MCP 부재로 미실행, RF-01B~05는 미완료 유지 |
| 2026-07-30 | 이천서 | 정식 런의 카드 성장 선택을 상점으로 통합하고 자동 발동 카드 상점 후보·일반전 골드 정산·보스 즉시 승리·기존 RW 재사용 경계를 확정한 문서 기여를 추가; HONG의 RF 구현 예정 범위를 새 흐름으로 갱신하고 실제 코드는 미착수로 유지 |
| 2026-07-30 | 이천서 | GSV-02 자동 5종 도상 매핑과 악마 12종 아트 확정 순서를 손패·상점·계약 후보에 연결하고 전용 19/19·전체 687/687로 검증; 기존 팀 에셋 재사용, 원본 GameScene Play Mode 미검증 |
| 2026-07-30 | 이천서 | GSV-01의 세 이미지를 기본·공격 위기·공격받음 상태로 정정하고 공격 효과 처리 전/성공 처리에만 반응하도록 구현 기준과 테스트를 개정; 전용 3/3·전체 668/668·세 상태 원본 일치 100/100 검증 |
| 2026-07-29 | 이천서 | GSV-01 적 6종 상태 스프라이트·상인 이미지·실제 전투 프로필 동기화를 구현하고 전용 3/3·전체 668/668·프리팹 렌더 100/100으로 검증; 원본 GameScene Play Mode 미검증과 외부 에셋·새 패키지 없음 기록 |
| 2026-07-29 | 이천서 | EP-R06·DC-R06 보스 시작·영혼 5·2 고정 계약 전환, 생존·복수 경계·비역행·일반 계약 차단과 표시를 구현하고 전용 10/10·전체 665/665 검증 기여 추가; GameScene·MonoBehaviour UI·씬·프리팹·Packages·외부 자산 무변경, HONG RF 경계 유지 |
| 2026-07-29 | 이천서 | EP-R05 일반·엘리트 특수 정책, BA-R01 공통 체인지, AC-RV02 집행자 독극물, DC-R06 광신도·집행자 계약을 구현하고 정책 12/12·CoreLoop 466/466·StageProgression 189/189·전체 655/655 검증 기여 추가; GameScene·UI·씬·프리팹·Packages·외부 자산 무변경 |
| 2026-07-29 | 이천서 | EP-R04 적 6종 영혼·전용 덱·겁쟁이 합 15·집행자 표시명·보스 영혼 8 표시·아스모데우스 7 이하 제한과 광신도 사탄 선택 정체 방지를 구현해 EPR04 14/14·CoreLoop 451/451·StageProgression 189/189·전체 EditMode 640/640으로 검증; GameScene·씬·프리팹·Packages·외부 자산 무변경 |
| 2026-07-29 | 이천서 | 적 행동·계약·독극물 세부 규칙과 골드 3·4·6·7·9·15를 확정해 EP-R04 및 HONG RF-01A→RF-01B 착수 조건을 해제; 코드·테스트·외부 자산 무변경 |
| 2026-07-29 | 이천서 | 최신 Notion v0.7 기획 전반 재명세 기여와 EP-R04~R06·DC-R06·AC-RV02·BA-R01 후속 책임, HONG RF-01A/01B 착수 경계를 추가; 코드·테스트·외부 자산 무변경 |
| 2026-07-29 | 이천서 | CU-M07 레비아탄 첫 실패 조건부 재예측·두 번 실패 영혼 대가·GameScene 재준비 연출을 대상 4/4·CoreLoop 442/442·전체 631/631로 완료; 씬·프리팹·외부 에셋 무변경 |
| 2026-07-29 | 이천서 | CU-M06 리볼버 준비 등장·숫자 예측·성공/실패 Animator 연결을 대상 2/2·CoreLoop 438/438·전체 627/627·GameScene 실제 상태로 완료; 씬·프리팹·외부 에셋 무변경 |
| 2026-07-29 | 이천서 | RFM01 GameScene 상점의 복수 카드 구매·라이터/위스키 독립 1회·이용 방문당 공통 가격 +1·두 나가기 화면과 전용 3/3·전체 626/626·실제 화면 검증 기여 추가; HONG의 RF-01~RF-05는 미완료 유지 |
| 2026-07-29 | 이천서 | CL-M01 플레이어 전체 합·공개 카드 합 분리 표시를 전용 2/2·전체 EditMode 623/623·GameScene 두 줄 화면·게임 코드 오류 0으로 완료; 적 정보 은닉 유지, 씬·프리팹·외부 에셋 무변경 |
| 2026-07-29 | 이천서 | CU-M05 공개 적 카드 수동·자동 효과 호버 정보·미공개 카드 문자열 차단을 신규 3/3·전체 EditMode 622/622·GameScene HUD 화면·게임 코드 오류 0으로 완료; 씬·프리팹·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | 악마 계약 DC-R05 시작 선택·저장 재진입·활성 마몬/사탄 ID 입력·루시퍼 최대 후보 화면을 신규 4/4·합계 614/614·두 전투 화면 720p/1080p·시각 92점으로 완료; 씬·프리팹·외부 에셋 무변경, 물리 월드 카드 연출은 별도 아트·씬 작업으로 분리 |
| 2026-07-28 | 이천서 | DC-R04 4/5 바포메트 오망성 양측 묶음·드로우 더미 소진·전투 위치 회수·가용 덱 초기화·재삽입·기존 버스트 경계와 파이몬/벨리알 중첩을 구현해 전용 7/7·광신도 9/9·CoreLoop 410/410·전체 599/599 검증 기여 추가; GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | DC-R04 3/5 파이몬 선택 추방·단일 임시 승자 대가와 벨리알 탈취·즉시 재사용·스탠드 취소·라운드 대가·전투 종료 원소유권 복구를 구현해 전용 10/10·광신도 9/9·CoreLoop 402/402·전체 591/591 검증 기여 추가; GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | 바알제붑을 양측 공개 카드 직접 선택으로 정정하고 DC-R04 2/5 아스모데우스 차례 시작 선택·6 이하 카드 제한, 아자젤 재활성화·동일 숫자 선행 버스트를 플레이어·적 대칭으로 구현해 바알제붑 8/8·신규 11/11·400시드·100자동전투·CoreLoop 390/390·전체 579/579 검증 기여 추가; GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | 악마 계약 DC-R04 1차 바알제붑 버스트 대체·최종 승부 재계산·스탠드 제한·영혼 0 원자성, 메피스토펠레스 보위 나이프 비버스트 비공개 공개·적 대칭·광신도 여섯 종 선택과 전용 8/8·400시드·CoreLoop 377/377·전체 566/566 검증 기여 추가; GameScene·씬·프리팹·Packages·HONG RunFlow/Shop·Shim0Hwan 아트·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | 악마 계약 DC-R03 마몬 정상 행동 재굴림·적 공개 주사위 후보, 레비아탄 독립 선언 2회·첫 종료 중단·최종 생존 대가·안전 결과와 전용 14/14·CoreLoop 367/367·전체 556/556 검증 기여 추가; GameScene·씬·프리팹·Packages·외부 에셋 무변경 |
| 2026-07-28 | 이천서 | 악마 계약 DC-R02 사탄 카운트 4·소유자 차례 훅·영혼 대가 1회·활성 계약 양면·자동 카드 재개·적 대칭과 별도 권능 제거, 전용 12/12·CoreLoop 363/363·전체 553/553 검증 기여 추가; 월드 카드 입력은 DC-R05로 분리 |
| 2026-07-28 | 이천서 | 게임 저장·이어하기 SV-05 런 예약·새 게임 보호·이어하기 세션 교체·시작 악마/손상·버전/저장 실패 UI와 전용 9/9·StageProgression 189/189·CoreLoop 362/362·전체 551/551·두 해상도·Console 0 기여 추가; 실제 프로세스 재실행과 RF 체크포인트는 SV-06으로 분리 |
| 2026-07-28 | 이천서 | 게임 저장·이어하기 SV-04 시작 악마·카드 보상·런 종료 체크포인트, 저장 실패 보류·동일 스냅샷 재시도·진행 차단과 대상 8/8·StageProgression 180/180·CoreLoop 362/362·전체 542/542 기여 추가; SV04-I03은 RF API 대기로 분리 |
| 2026-07-28 | 이천서 | EP-R01 겁쟁이 도박사 18장 합계 정정·네 번째 일반 적 편입·영혼 2·합계 14 스탠드·수동 카드 비사용·빈 악마 덱을 구현하고 전용 9/9·전체 534/534·게임 코드 오류 0, 최종 결과 저장 안내 3건 분리 기여 추가 |
| 2026-07-28 | 이천서 | 자동 발동 카드 AC-RV01 새 정의 키·숫자·런/전투 변환·`prototype-v2` 저장 호환 정책과 전용 11/11·전체 522/522·두 전투 화면·Console 0 기여 추가 |
| 2026-07-27 | 이천서 | 악마 계약 DC-R01 시작 선택·가변 후보·저장 복원과 광신도 강제 후보 회귀 교정, 전용 14/14·전체 EditMode 511/511 기여 추가 |
| 2026-07-27 | 이천서 | 게임 저장·이어하기 SV-03 일반/악마 덱·마지막 발급 ID·안정 스테이지·상대/보상 순번 캡처·새 세션 원자 복원과 대상 7/7·StageProgression 156/156·전체 EditMode 500/500·컴파일 오류 0 기여 추가 |
| 2026-07-27 | 이천서 | 최신 노션 기획의 악마 12종·자동 카드 숫자 1~5·겁쟁이 도박사를 문서에 이관하고 DC-R00·AC-RV00·EP-R00 기여 및 후속 구현 경계를 추가; 코드·테스트·씬·외부 에셋은 변경하지 않음 |
| 2026-07-26 | 이천서 | 게임 저장·이어하기 SV-02 v1 JSON·안정 문자열·원자 파일 교체·백업 불러오기와 대상 8/8·StageProgression 149/149·전체 EditMode 491/491·컴파일 오류 0 기여 추가 |
| 2026-07-26 | 이천서 | 게임 저장·이어하기 SV-01 순수 스냅샷·검증·안정 상태 캡처와 대상 7/7·StageProgression 141/141·전체 EditMode 483/483·컴파일 오류 0 기여 추가, Test Framework 안내 3건 분리 |
| 2026-07-26 | 이천서 | 게임 저장·이어하기 SV-00 문서 4종, SV-01~SV-06 업무 대장과 안정 체크포인트·런 예약·버전/복구·RF 협업 경계 기여 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-06 독립/런 세션·코드 기반 UI·공개 정보 적 정책·일반 보상 5종·사기꾼 탐지기 통합과 대상 15/15·Unity 비의존 461/461·컴파일 성공 기여 추가, Editor 전용 10건과 화면 검증은 후속으로 구분 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-05 부활초 양측 영혼 조건·전용 라운드 전이·상태 및 부모 효과 취소 구현과 대상 11/11·CoreLoop 327/327·전체 456/456 검증 기여 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-04 화염 방사기 순차 폐기·회중시계 재활성화와 원본 위치 선택·양측 소유권 구현 및 대상 11/11·CoreLoop 316/316·전체 445/445 검증 기여 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-03 거짓말 탐지기 선언·소유자 전용 비교·적 안전 관측·체인지/해머/라운드/전투 종료 지식 폐기와 대상 10/10·CoreLoop 305/305·전체 434/434 검증 기여 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-02 계약 제한 독극물 선택·영혼 0 즉시 패배·승리 회복 예약·적 대칭 구현과 대상 12/12·CoreLoop 295/295·전체 424/424 검증 기여 추가 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-01 정의·양측 공개 유입·선택 보류·입력 잠금·수동 효과 재개와 대상 15/15·CoreLoop 283/283·전체 412/412 검증 기여 추가 |
| 2026-07-23 | 이천서 | 자동 발동 카드 AC-00 문서 4종, AC-01~AC-06 업무 대장과 전투·적 AI·UI·런 분업 경계 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-06 정상 차례 카운터·스탠드/버스트 제한·영혼 대가·양면 권능·임시 카드 정리·UI 구현과 대상 8/8·전체 377/377·두 씬·두 해상도·Console 0 검증 기여 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-07 광신도 계약 정책·적 소유 4종 대칭 처리·Cultist 전용 덱·안전 표시·반복 회귀와 대상 12/12·CoreLoop 260/260·전체 389/389·Console 0 검증 기여 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-08 광신도 사탄·레비아탄 효용 조건, 벨페고르·마몬 결정적 균형 분산과 400시드·100자동전투·대상 8/8·CoreLoop 268/268·전체 397/397 검증 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-02 비용·횟수·필수 선택·상호작용 ID·활성 계약·대가 패배·세션 전달 구현과 대상 13/13·전체 342/342·컴파일 오류 0 검증 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-03 벨페고르 플레이어 전용 미리보기·동일 ID 처리·자동 스탠드·정보 은닉 구현과 대상 9/9·전체 351/351·컴파일 오류 0 검증 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-04 마몬 주입식 주사위·차례/최종 선택, 레비아탄 리볼버 후속 버스트·영혼 대가·정보 은닉 구현과 대상 11/11·전체 362/362·컴파일 오류 0 검증 기여 추가 |
| 2026-07-23 | 이천서 | 악마 계약 DC-05 비용 확인·후보 설명·활성 상태·소유자 전용 미리보기·독립/런 UI 구현과 대상 7/7·전체 369/369·두 씬·두 해상도·Console 0 검증 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-01 정의·런/전투 덱·후보 이동·보충·전투 변환 구현과 대상 19/19·전체 329/329·Console 0 검증 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약의 개별 대가 사망·선택·버스트·생성 카드 합계·동일 악마 중첩 허용·적 대칭과 전투 한정 카드 수명 기획 기여 추가 |
| 2026-07-22 | 이천서 | 악마 계약 DC-00 문서 4종과 DC-01~DC-07 업무 대장, 사탄 기획 확인 게이트와 분업 경계 추가 |
| 2026-07-19 | 이천서 | 최초 문서 작성, 이천서의 문서 기여와 미정 팀원 영역 분리 |
| 2026-07-19 | 이천서 | 코어 루프 구현 계획 수립 기여 기록 추가 |
| 2026-07-19 | 이천서 | 단계별 코어 루프 진행 기록 수립 기여 추가 |
| 2026-07-19 | 이천서 | 프로젝트·MCP 구조 확인 및 코어 루프 1단계 구현·검증 기록 추가 |
| 2026-07-19 | 이천서 | 코어 루프 2단계 전투 상태·행동·단순 적 정책 구현 및 검증 기록 추가 |
| 2026-07-19 | 이천서 | 코어 루프 3단계 최소 UI·입력·승패·재시작 구현 및 씬 검증 기록 추가 |
| 2026-07-19 | 이천서 | 코어 루프 4단계 전체 회귀·수동 흐름·씬·Console 검증과 최종 기여 기록 추가 |
| 2026-07-19 | 이천서 | 독립 런·스테이지 진행 시스템의 기획·개발 명세·구현 계획·진행 기록 수립 기여 추가 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-00 기준 확정과 Unity 회귀 검증 기여 기록 추가 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-01 순수 상태 기반 구현과 전체 EditMode 40/40 검증 기여 기록 추가 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-02 전투 연결·지속 영혼 동기화와 전체 EditMode 45/45 검증 기여 기록 추가 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-03 진행 UI·씬 전환·런 상태 유지와 전체 EditMode 50/50 검증 기여 기록 추가 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-04 전체 승리·중간 패배·양쪽 재시작·10회 반복과 최종 검증·기록 마감 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-00 폴드·체인지 기획·기술 명세와 단계별 구현·검증 계획 수립 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-01 상태·카드 이동 기반 구현과 CoreLoop 35/35·전체 EditMode 58/58 검증 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-02 폴드 판정·영혼 피해·라운드 종료·세션 전달과 CoreLoop 41/41·전체 EditMode 64/64 검증 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-03 체인지 보류·후보 선택·적 차례·라운드 제한과 CoreLoop 49/49·전체 EditMode 72/72 검증 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-04 비용·위험·후보 표시와 화면 입력·Controller 전달, CoreLoop 55/55·전체 EditMode 78/78·Game View 검증 기여 추가 |
| 2026-07-19 | 이천서 | 전투 행동 BA-05 진행 세션 전달·종료와 영혼 동기화, 진행 27/27·전체 EditMode 82/82·실제 런 패배·재시작 검증과 최종 마감 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-00 일반 수동 카드 4종의 기획·기술 명세와 CU-00~CU-06 구현·검증 계획 수립 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-01 카드 정의·사용 상태·런 정의 키 보존 구현과 신규 19개·전체 EditMode 101/101 검증 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-02 사용 검증·선택 대기·효과 처리·종료 원인 기반 구현과 신규 16개·전체 EditMode 117/117 검증 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-03 리볼버 단일 비공개 카드 추측·성공/실패·정보 은닉 구현과 신규 8개·전체 EditMode 125/125 검증 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-04 수정 구슬 순서 보존·해머 단일 비공개 교체·나이프 강제 드로우 구현과 신규 18개·전체 EditMode 143/143 검증 기여 추가 |
| 2026-07-19 | 이천서 | 카드 사용 CU-05 카드 표시·효과 선택 UI·독립/런 세션 전달·종료 동기화 구현과 신규 8개·전체 EditMode 151/151·Game View·씬·Console 검증 기여 추가 |
| 2026-07-20 | 이천서 | 카드 사용 CU-06 반복 회귀 5개·전체 EditMode 156/156, 실제 런 승리·패배 재시작과 씬·Console 최종 검증·기록 마감 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-00에서 보스 보상 후 런 승리, 프로토타입 카드 풀과 RW-01~RW-05 구현·검증 문서 4종 수립 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-01 카탈로그·결정적 3장 제안·고유 런 카드 ID·최초 덱 복구 구현과 신규 8/8·전체 EditMode 164/164 검증 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-02 선택 대기·선택·건너뛰기·일반/보스 완료 목적지와 실패 원자성 구현, 신규 7/7·전체 EditMode 171/171 검증 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-03 실제 승리·영혼 동기화·보상 생성·등급 주입·다음 전투 덱·재시작 통합, 신규 6/6·전체 EditMode 177/177 검증 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-04 후보 표시·선택·건너뛰기·완료 목적지·최근 결과와 세션 입력 연결, 신규 5/5·전체 EditMode 182/182·실제 일반/보스 화면 검증 기여 추가 |
| 2026-07-20 | 이천서 | 전투 보상 RW-05 다섯 흐름 각 10회 반복, 신규 5/5·CoreLoop 122/122·StageProgression 65/65·전체 187/187와 실제 일반/보스/패배·재시작·씬·Console 검증으로 1차 범위 마감 기여 추가 |
| 2026-07-20 | 이천서 | Git 작성자 3명의 실제 기여를 반영하고 이천서의 적 전투 프로필·AI·카드 효과·전투 변환 담당과 EP-00~EP-06 업무 대장 추가 |
| 2026-07-20 | 이천서 | EP-00 적 5종 전투 프로필 문서 4종, 공개 관측 AI·상대 선택·아트 분업 경계와 구현·검증 계획 수립 기여 추가 |
| 2026-07-20 | 이천서 | EP-01 적 5종 프로필·안전 미리보기·교체 정책·결정적 전투 설정 구현과 신규 12/12·CoreLoop 134/134·전체 EditMode 199/199 검증 기여 추가 |
| 2026-07-20 | 이천서 | EP-02 공개 관측·유효 행동 후보·무변경 재검증·적 카드 행위자/대상·대칭 폴드 구현과 신규 8/8·CoreLoop 142/142·전체 EditMode 207/207 검증 기여 추가 |
| 2026-07-20 | 이천서 | EP-03 공개 구성 기반 숫자 추론·일반 적 정책 3종·수정 구슬 선택 구현과 신규 10/10·CoreLoop 152/152·전체 EditMode 217/217 검증 기여 추가 |
| 2026-07-20 | 이천서 | EP-04 집행관 해머·나이프 후속 평가, 엘리트 추론 표시·높은 등급 보상 연결 구현과 신규 11/11·CoreLoop 163/163·전체 EditMode 228/228 검증 기여 추가 |
| 2026-07-20 | 이천서 | EP-05 최종 보스 3구간 정책·강행동 예고·추론 방향 표시·높은 등급 보상 뒤 런 승리 구현과 신규 16/16·CoreLoop 179/179·전체 EditMode 244/244 검증 기여 추가 |
| 2026-07-20 | 이천서 | EP-06 선택 키의 실제 전용 덱·영혼·정책·보상 변환, 50회 상태 격리와 엘리트/보스 결과·실제 런을 검증하고 신규 16/16·StageProgression 81/81·CoreLoop 179/179·전체 EditMode 260/260으로 1차 범위 마감 기여 추가 |
| 2026-07-20 | 이천서 | EUI-00 상대 선택·적 전투 정보 UI 문서 4종과 업무 대장을 수립하고 해당 UI를 이천서, 랜덤 이벤트·정식 런 진행을 HONG 담당 계획으로 조정 |
| 2026-07-20 | 이천서 | EUI-01 결정적 후보·불변 제안·선택 대기 상태·세션 주입·보스 우회와 신규 13/13·StageProgression 94/94·CoreLoop 179/179·전체 EditMode 273/273 검증 기여 추가 |
| 2026-07-20 | 이천서 | EUI-02 후보 안전 미리보기 비교·로컬 집중·확정 가능 상태·선택 중 씬 이동 차단과 신규 9/9·StageProgression 103/103·CoreLoop 179/179·전체 EditMode 282/282·두 해상도 화면 검증 기여 추가 |
| 2026-07-20 | 이천서 | EUI-03 OfferId+ProfileKey 원자적 확정·선택 프로필 전투·보상·두 선택·고정 보스·재시작 통합과 신규 14/14·StageProgression 117/117·CoreLoop 179/179·전체 EditMode 296/296·실제 전투 씬 전환 검증 기여 추가 |
| 2026-07-20 | 이천서 | EUI-04 공개 추론 공유·등급별 안전 표시 스냅샷·일반/엘리트/보스 정보·보스 예고·720p 반응형 UI와 신규 14/14·StageProgression 117/117·CoreLoop 193/193·전체 EditMode 310/310·두 해상도 화면 검증 기여 추가 |
| 2026-07-20 | 이천서 | EUI-05 후보·전투·보상·고정 보스·재시작·상태 격리 반복 검증, 신규 5/5·StageProgression 122/122·CoreLoop 193/193·전체 315/315·실제 두 씬·두 해상도·Console 0으로 상대 선택·전투 정보 UI 1차 범위 마감 기여 추가 |
| 2026-07-20 | 이천서 | RF-00 최신 골드·상점 기준의 정식 런 문서 4종과 HONG의 RF-01~RF-05 계획 업무 대장 수립 |
| 2026-07-21 | 이천서 | 현행 전투 규칙의 폴드 제거·체인지 누적 비용·엄격한 영혼 조건·공개 폐기 이관과 전체 EditMode 306/306 검증 기여 추가, `GameScene` 및 HONG 담당 경계 무변경 기록 |
| 2026-07-22 | 이천서 | CU-M01 위협용 해머 상대 공개 카드 제거·스탠드 교체·적 AI 최고 숫자 대상·GameScene 표시 이관과 전체 EditMode 308/308 검증 기여 추가 |
| 2026-07-22 | 이천서 | CU-M02 보위 나이프 비버스트 강제 폐기·공개 합 중간 버스트·전체 합 최종 승부 경계 이관과 영향 13/13·전체 EditMode 309/309 검증 기여 추가 |
| 2026-07-22 | 이천서 | CU-M03 플레이어 선두·적 말미 표시 투영으로 양측 비공개 카드의 플레이어 시점 최좌측 배치·규칙 손패 순서·상대 정보 은닉 보존과 신규 1/1·전체 EditMode 310/310·Game View 검증 기여 추가 |
| 2026-07-31 | 이천서 | MainMenuScene 기획·구현 총괄. 기존 저장 흐름을 재사용한 새 런·이어하기·시작 예약 재개·종료 진입, Build Settings 0번 등록, 현재 StageTest 호스트와 후속 GameScene 단일 런 경계 문서화, 전체 EditMode 798/798 및 1280×720 화면 검증 |
| 2026-07-31 | 이천서 | GF-01 GameScene 전체 흐름 계약 정의·검증. 시작 악마 2장 무선택 공개, 상대 선택, 일반전·상점 2회, 고정 보스, 오래된 입력 차단, 재시작 재추첨 금지를 순수 테스트 5개로 고정하고 전체 EditMode 803/803 통과 |
| 2026-07-31 | 이천서 | GF-02 GameScene 통합 제어 구현. 메뉴→GameScene 진입, 도메인 상태 기반 화면 판정, 씬 재로드 없는 전투 바인딩·완료 알림·표시 초기화를 연결하고 GF 7/7·전체 EditMode 805/805·씬 문제·Console 오류 0 검증 |
| 2026-07-31 | 이천서 | GF-03 시작 악마 공개 화면의 상대 영혼 HUD 비노출 결정. 상인 연출은 유지하고 전투 진입 시에만 상대 HUD를 복원하도록 연결, 신규 1/1·HUD 관련 12/12 검증 |
| 2026-07-31 | 이천서 | GF-04 정식 상점 GameScene 이관. 일반 3장·악마 2장과 카드 구매·라이터·위스키·나가기 입력을 정식 세션에 연결하고, 일반 카드 고유 앞면·프로토타입 악마 7종 풀·라이터 왼쪽/위스키 오른쪽·상대 HUD 비노출을 반영했다. 전용 2/2·전체 EditMode 808/808·GameScene 문제 0·컴파일/게임 코드 오류 0·1280×720 화면 검증(Test Framework 결과 저장 안내 1건) |
| 2026-07-31 | 이천서 | DC-R08 플레이어 악마 7종 정합성 검토·수정. 플레이어 계약 공개 행동 기록, 마몬 전투별 결정적 시드, 바알제붑 활성 상태 문구를 보정하고 대상 90/90·CoreLoop 549/549·전체 EditMode 823/823·컴파일 오류 0 검증 기여 추가 |
| 2026-07-31 | 이천서 | GSV-03·CU-V06 상점 카드 표시와 전투 연출 UI 수정. 상점 악마 카드 밝기 통일, 일반 카드 호버 재질 격리, 망치·리볼버 연출 HUD·잔류 호버 차단, 망치 직접 선택의 중앙 검은 옵션 패널 제거와 비활성 덱 즉시 갱신을 구현했다. 상점·연출 대상 56/56, 망치 HUD 16/16, 전체 EditMode 832/832와 Console Error 0 검증 기여 추가 |
| 2026-08-01 | 이천서 | DC-UI01 중앙 계약서 2장 기획·구현. 계약서 클릭으로 기존 계약 후보 UI를 열고 플레이어·상대 계약 확정마다 1장씩 제거하며, 선택 보류 중 유지·비전투 숨김·HUD 계약 버튼 제거를 연결했다. 대상 EditMode 20/20·전체 EditMode 850/850 검증 기여 추가 |
| 2026-08-01 | 이천서 | 아자젤 효과를 재활성화에서 계약 시·정상 히트 뒤 공개 수동 카드 효과의 좌→우 1회 연속 실행으로 개정했다. 카드 ID 스냅샷, 선택 대기 재개, 라운드·전투 종료 취소, 보스 단계 자동 해결과 플레이어·적 대칭 회귀를 구현·문서화했다. AI는 코드 추적·테스트·문서 정리를 보조했으며 최종 승인 책임은 이천서에게 있다. 대상 23개 현행 기대값 통과, 전체 EditMode 856/858; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이다. |
| 2026-08-01 | 이천서 | DC-R09 사탄을 일반 행동/현재 면 능력 선택 구조로 개정했다. 일반 행동의 면 유지, 유효 능력 완료 시 전환, 잘못된 입력 원자성, 광신도·보스의 공개 정보 기반 선택 정책을 구현·검증했다. AI는 규칙 대조·코드·테스트·문서 기록을 보조했으며 최종 승인 책임은 이천서에게 있다. 전용 17/17, 전체 EditMode 861/863; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이다. |
| 2026-08-01 | 이천서 | CU-M11·DC-UI02 공통 카드 효과 UI 이관. 화염 방사기·회중시계·바알제붑의 테이블 카드 직접 선택, 효과 원본·대상 윤곽, 바알제붑 `(1/2)`·`(2/2)` 진행 표시를 구현했다. AI는 최신 문서 대조·코드·테스트·기록을 보조했고 최종 승인 책임은 이천서에게 있다. GameScene HUD 25/25, 전체 EditMode 864/866; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이며 외부 에셋·패키지 추가 없음. |
| 2026-08-01 | 이천서 | GSV-04 전투 카드 흐림 회귀 수정. 양측 카드의 아틀라스 UV 렌더 속성 유실을 매 프레임 검증·자가 복구하도록 구현하고 재현 1/1·카드 표시군 35/35를 통과했다. AI는 화면·코드 경로 분석, 회귀 테스트와 기록을 보조했으며 최종 승인 책임은 이천서에게 있다. 전체 EditMode 865/867; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이고 씬·프리팹·외부 에셋·패키지 변경 없음. |
| 2026-08-01 | 이천서 | CU-M12 수정 구슬 월드 카드 선택 UI 기획·구현. 계약 후보 UI와 동일한 하단 좌표·부채꼴·호버 상승·전진·정렬 및 입력 차단을 사용해 덱 위 후보 2장 직접 클릭과 `추가하지 않기`, 기존 덱 복귀 순서를 연결했다. AI는 문서 대조·표시 모델·GameScene 입력·회귀 테스트·기록을 보조했고 최종 승인 책임은 이천서에게 있다. 신규 2/2·HUD 27/27, 전체 EditMode 867/869; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이며 외부 에셋·패키지 추가 없음. |
| 2026-08-02 | 이천서 | DX-M01 도감 `Q/E` 연속 탐색 기획·구현. 적 마지막 장에서 악마 카드 첫 장으로, 반대 방향은 악마 카드 첫 장에서 적 마지막 장으로 이동하며 책 전체 양 끝은 정지하도록 navigation·버튼 상태·에디터 프리뷰를 통일했다. AI는 구조 대조·회귀 테스트·문서화를 보조했고 최종 승인 책임은 이천서에게 있다. navigation 6/6, 전체 EditMode 857/859; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이다. |
| 2026-08-02 | 이천서 | GSV-05 상점 악마 카드 호버 UI 통일 및 실화면 회귀 정정. 계약 상세 패널 재사용 뒤 비활성 부모 때문에 보이지 않던 문제를 사용자 확인으로 발견하고, 상점 호버 동안 `CombatControls`를 활성화·이탈 시 원복하도록 수정했다. AI는 계층 원인 분석·`activeInHierarchy` 회귀·문서화를 보조했고 최종 승인 책임은 이천서에게 있다. 계층 1/1·HUD 28/28, 전체 EditMode 858/860; 잔여 2건은 기존 도감 좌표·창문 광원 비교로 범위 밖이다. |
| 2026-08-02 | 이천서 | GSV-06 라운드 종료 동시 공개·결과 확인 연출. 양측 비공개 카드 동시 뒤집기, 비공개 숫자를 포함한 최종 합계, 최소 2.5초 입력 잠금·결과 유지 후 다음 라운드 표시를 기획·구현했다. AI는 스텝 타임라인 분석·코드·회귀 테스트·문서화를 보조했고 최종 승인 책임은 이천서에게 있다. 신규 2/2·표시 32/32·카드 35/35, 전체 877/880; 잔여 3건은 기존 도감 좌표·창문 광원·범위 밖 테이블 프리팹 참조다. CoreLoop 도메인 전이·씬·프리팹·외부 에셋·패키지 변경 없음. |
| 2026-08-02 | 이천서 | GSV-07 시작 악마 공개·상점 후보 규칙 보정. 시작 악마를 상점 하단 좌표로 이동하고 계약 상세 호버를 연결했으며, 상점의 보유 악마 제외·악마 상호 중복 금지·일반 카드 중복 허용을 기획·구현했다. AI는 좌표·입력·생성·복원 경계 분석과 코드·테스트·문서화를 보조했다. 신규 3/3·상점 14/14·StageProgression 262/262·전체 879/882, 컴파일·Console Error 0. 잔여 3건은 기존 회귀이며 외부 에셋·오픈소스·패키지 추가 없음. |
| 2026-08-02 | 이천서 | GSV-07 시작 악마 호버 실화면 회귀 정정. 사용자 확인으로 상세 패널이 비활성 `UIHUD` 아래 있어 표시되지 않는 문제를 발견했고, 시작 공개 중 HUD 루트 활성과 직접 참조 바인딩으로 수정했다. AI는 활성 계층 원인 분석·코드·회귀 테스트·문서화를 보조했으며 최종 승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | GSV-07 호버 보정 검증. 화면 전환 11/11, StageProgression 264/264 통과를 확인했다. 전체 EditMode의 기존 실패 2건은 별도 회귀로 유지했다. AI는 테스트 실행과 결과 분류를 보조했고 최종 승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | GameScene 전투 초기화와 나이프→적 자동 카드 동기 선택 예외 수정. AI는 사용자 스택 분석, 상태 전이 보정, 재현 테스트와 회귀 검증, 문서화를 보조했다. 신규 1/1·화면 흐름 11/11 통과, CoreLoop 609/611이며 잔여 2건은 기존 회귀다. 최종 구현·승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | 위 예외 수정의 전체 EditMode 887/889를 확인했다. 잔여 2건은 기존 테이블 명령 프리팹 참조와 창문 광원 색 정밀도 회귀다. |
| 2026-08-02 | 이천서 | CU-M13·DC-UI03 리볼버 순환형 숫자 선택과 사탄 숫자 카드 10장·두 장 낙인 선택 프로토타입을 기획·구현·검증했다. AI는 상태 경계 분석·코드·테스트·문서화·시각 반복을 보조했다. 신규 2/2, CoreLoop 611/613, StageProgression 264/264, 시각 93·92점이며 잔여 2건은 기존 회귀다. 미커밋 낙인 스프라이트는 연결하지 않았고 외부 에셋·패키지·씬·프리팹 변경 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | 기존 `GSH01_U10`·`MOO01_U04` 회귀를 분석·복구했다. AI는 Unity MCP 재현, 프리팹 직렬화 확인, 테스트 경계와 HDR 색상 정밀도 보정을 지원했다. 대상 2/2와 전체 EditMode 891/891을 통과했으며 런타임·씬·프리팹·외부 에셋·패키지 변경은 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | SET-05 설정 시스템 마감. 1920×1080 레이아웃, Windows 창 모드·전체 화면 적용과 저장 재로드, 강제 씬 전환 Missing Script를 검토하고 실제 화면 모드 검증·실패 복구, SettingsSystem 영속화, 플레이어 빌드의 Editor 코드 경계를 수정했다. AI는 자동 검증·코드·회귀 테스트·문서화를 보조했다. 전체 EditMode 891/891, Windows Release 빌드 오류 0, 강제 전환 5회 missing script 0이며 최종 승인 책임은 이천서에게 있다. |
| 2026-08-02 | 이천서 | AC-RV04 자동 카드·악마 최신 규칙 이관. 화염 방사기·부활초의 비공개 확정과 동시 공개·적용, 바알제붑 단측 공개 카드 처리, 자동 카드 우선순위와 GameScene 결과 표시를 구현·검증했다. AI는 구조 분석·코드·회귀 테스트·문서화를 보조했다. 대상 6/6, 전체 EditMode 892/892, 컴파일 오류 0이며 Console에는 Test Framework 결과 저장 안내 1건만 남았다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CU-M14 보위 나이프 연출·표정 타이밍. 나이프 생성·조준·결과 확인·명중/빗나감 흐름과 조준 중 공격 위기, 충격 프레임 피격 전환을 구현하고 리볼버 발사 전 공격 위기 유지를 재검증했다. 사용자 확인으로 발견된 상대 카드 뷰 강제 전환도 제거해 현재 카메라를 유지한다. AI는 상태 단서 투영·Animator 연결·이벤트·테스트·문서화를 보조했다. 전체 EditMode 900/900·Console Error 0이며 기존 프로젝트 에셋을 재사용했고 새 외부 에셋·패키지는 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CUI-02 영혼·골드 아이콘 전체 배치를 검토했다. 1920×1080 전투 HUD의 좌우 대칭 여백, IMGUI 24×24 제한, 도감 아이콘 참조를 확인하고 단독 GameScene에서 상대 영혼이 숨는 회귀를 수정했다. AI는 Unity MCP 실화면 캡처·원인 추적·회귀 테스트·문서화를 보조했다. 집중 6/6, 전체 EditMode 901/901, 시각 판정 96/100이며 새 외부 에셋·패키지는 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | DC-UI04 마몬 주사위 결과 가시화를 담당했다. 전용 프리팹 부재를 확인하고 양측 공개 주사위 눈을 GameScene 상단 임시 UI로 연결했다. AI는 에셋 조사·표시 경계·코드·테스트·1920×1080 시각 검증·문서화를 보조했다. 대상 1/1, 전체 EditMode 904/904, 시각 판정 94/100이며 외부 에셋·패키지는 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | 상점 라이터·위스키의 기존 `OnlyModel`·`Anim` 프리팹 연결과 단독·정식 거래 성공 경로 연출을 담당했다. AI는 에셋·Animator 구조 조사, Unity fake-null 원인 수정, 코드·프리팹 직렬화·실화면 검증과 문서화를 보조했다. Unity MCP에서 두 모델 참조와 두 연출 상태·카메라 전경을 확인했다. 전체 EditMode 904/905이며 잔여 1건은 범위 밖 `SoulIcon-v5` 기대값 회귀다. 외부 에셋·패키지는 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CU-M15 수정 구슬·화염 방사기·벨페고르·아스모데우스·마몬의 선택 취소·일반 행동 진입 UI를 공통 우측 하단에 배치하고 반투명 전체 배경을 제거했다. 사탄은 이후 실제 카드 클릭 입력으로 분리했다. AI는 HUD 표시 경계 추적, 코드·회귀 테스트·Unity MCP 실화면 검증과 기록을 보조했다. 대상 10/10, 시각 판정 96/100이며 HUD 클래스의 기존 `GSH01_U09` 1건은 별도 잔여 회귀다. 씬·프리팹·외부 에셋·패키지 변경은 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CU-M16 카드 효과 종료 뒤 빈 중앙 선택 UI를 제거하고, 플레이어의 실제 활성 사탄 카드 클릭과 성공 시 앞면 유지 180도 상하 회전 연출을 기획·구현·검증했다. AI는 입력·표시·연출 경계 추적, 코드·회귀 테스트·Unity MCP 실화면 검증과 문서화를 보조했다. 집중 10/10·사탄 관련 20/20·시각 96/100이며 씬·프리팹·외부 에셋·패키지 변경은 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CU-M17 보위 나이프의 공개 역할 뒷면 카드 추가, 약 2.5초 대기, 공개 합 반영·투척 동시 실행, 자동 카드 우선 처리와 적 피격 이벤트의 실제 충격 프레임 동기화를 기획·구현·검증했다. AI는 손패 역할 경계·타임라인·회귀 테스트·실화면 검증·문서화를 보조했다. 신규 2/2·관련 72/72·적 방향 17/17·충격 2/2·시각 94/100이며 CoreLoop 전체 647/650의 잔여 3건은 기존 도감·HUD 회귀다. 씬·프리팹·외부 에셋·패키지 변경은 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | CU-M18 상대 히트 카드의 플레이어 화면 기준 최우측 배치를 기획·구현·검증했다. 양측 비공개 역할 카드의 최좌측 고정과 규칙 손패 순서는 유지하고 표시 투영만 통일했다. AI는 순서 경계 추적·반복 드로우 테스트·Unity MCP 화면 좌표·시각 검증·문서화를 보조했다. 집중 1/1·표시 36/36·시각 97/100이며 씬·프리팹·외부 에셋·패키지 변경은 없다. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | DC-UI06 마몬의 정상 행동 유지와 테이블 주사위 직접 재굴림, 계약 직후·재굴림 공통 공중 회전 연출을 기획·구현·검증했다. 기존 주사위 FBX·재질·텍스처로 임시 프리팹을 만들고 버린 카드 더미 왼쪽에 배치했다. AI는 상태·입력 경계 추적, 코드·프리팹·씬·회귀 테스트·시각 검증·문서화를 보조했다. 집중 23/23·시각 92/100이며 외부 에셋·패키지 추가 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | MOO-02 GameScene 진행 단계별 무드 연결을 담당했다. 최초 악마 지급·상대 선택은 `readyStage`, 상점은 `shopStage`, 전투는 겁쟁이 도박사·총잡이·광신도·사기꾼·집행자·보스별 프로필을 적용한다. AI는 새 무드 에셋과 진행 상태 경계를 조사해 코드·씬 참조·테스트·실화면 검증·문서화를 보조했다. 집중 19/19, 프로필 8/8, 씬 문제 0, Console 오류·경고 0, 시각 95/100이며 외부 에셋·패키지 추가 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | AC-F01 적 거짓말 탐지기 자동 선언 뒤 전투 정지 회귀와 플레이어 숫자 선택 UI 통일을 담당했다. AI는 자동 카드 상태 전이·적 정책·GameScene 입력 경계를 추적해 즉시 선택 완료 시 원래 턴 상태를 복구하고, 플레이어 선언을 리볼버 공용 선택기에 연결했다. 거짓말 탐지기 12/12·집중 회귀 5/5·컴파일 오류 0이며 전체 CoreLoop 663/667의 실패 4건은 기존 회귀다. 씬·프리팹·외부 에셋·패키지 변경 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | DC-UI07 벨페고르의 하단 1장 미리보기·히트 여부 두 버튼, 아스모데우스의 사용 여부 두 버튼, 상대 행동 문구의 `Hit`·`Stand`·`Change`·`Contract` 제한과 문구 렌더러의 상대 스프라이트 전면 고정을 기획·구현·검증했다. AI는 상태·표시·정렬 경계 분석, 기존 UI 재사용, 코드·회귀 테스트·문서화를 보조했다. 집중 19/19·캐릭터 표시 15/15, CoreLoop 672/676이며 잔여 4건은 기존 회귀다. 씬·프리팹·외부 에셋·패키지 변경 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-03 | 이천서 | GSV04_U02 동일 카드가 양측 또는 한쪽 손패에 중복 등장할 때 함께 어두워지는 렌더 회귀 수정과 최종 승인을 담당했다. AI는 공유 머티리얼·프로퍼티 블록·호버 아웃라인·블렌드 경계를 분석하고 카드 인스턴스별 앞·뒷면 머티리얼 분리, 코드·테스트·문서화를 보조했다. 신규 1/1·카드 표시 39/39, CoreLoop 674/678이며 잔여 4건은 기존 회귀다. 씬·프리팹·외부 에셋·패키지 변경 없음. |
| 2026-08-04 | 이천서 | DC-UI08 사탄 윗면 숫자 선언의 `DevilShape` 낙인과 테이블 `HIT`·`STAND`·`CHANGE`의 `button.png` 프레임 연결을 기획·구현·최종 승인했다. AI는 표시 상태 분리, 동적 Sprite 정렬, 프리팹 연결과 회귀 검증을 보조했다. 프로젝트 내부 아트만 사용했으며 외부 에셋·패키지 추가 없음. |
| 2026-08-04 | 이천서 | DC-R10 사탄의 반복 종말 대가·정방향/역방향 능력, 마몬의 새 라운드 재굴림·해골 버스트, 바포메트 프로토타입 제외와 최종 보스 고정 계약 순서 개정을 기획·구현·검증했다. AI는 CoreLoop 상태 전이·프로필·SO·테스트·문서 동기화를 보조했다. 관련 대상 66/66, 전체 EditMode 740/747이며 잔여 7건은 기존 자산/UI 회귀다. 외부 에셋·오픈소스·패키지·씬·프리팹 변경 없음. 최종 승인 책임은 이천서에게 있다. |
| 2026-08-04 | 이천서 | 마몬 최종 주사위 포함/미포함 선택을 공통 우측 하단 UI에 통일하고, 도감 표시 악마를 프로토타입 6종으로 제한했다. AI는 HUD·Presenter 추적, 코드·테스트·문서 동기화를 보조했다. 제외 악마 정의·SO·에셋과 도메인 판정은 보존했으며 외부 에셋·패키지·씬·프리팹 변경 없음. |

## 2026-08-04 AC-UI01 공통 선택 UI 기본 버튼 이관

- 이천서: 일반 선택 UI의 적용 범위, 우측 하단 배치, 사용자 문구와 최종 화면 승인.
- AI 보조: Unity MCP 사전 점검, HUD·도메인 선택 경계 추적, `DefaultButton.prefab` 중첩 연결, 회귀 테스트와 문서 기록.
- 다른 팀원 소유의 아트 원본은 수정하지 않고 기존 기본 버튼 프리팹을 재사용했다.

## 2026-08-04 DC-UI09 사탄 종말 카운트 카드 표시

- 이천서: 활성 사탄 카드 앞면에 종말 카운트 `4~0`을 직접 칠하는 카드 귀속 표시 기획·구현·최종 승인.
- AI 보조: Unity MCP 상태 추적, GameScene 모델 투영, 기존 TMP 기반 핏빛 재질·카드 로컬 정렬, 회귀·시각 검증과 문서화.
- 집중 2/2, CoreLoop 768/776, 카드 앞면 직접 표기 시각 판정 95/100. 신규 외부 에셋·패키지·씬·프리팹 없음.
- 2026-08-05 이천서: 실제 플레이 가독성 검토 후 종말 카운트를 확대하되 카드 경계 안에 제한하도록 결정. AI는 글자 크기·로컬 배율·표시 사각형과 안전 영역 회귀 조건을 최소 수정했다.

## 2026-08-05 GSV-10 라이터 소각 카드 표시

- 이천서: 라이터 사용 시 선택한 실제 카드가 온전한 상태로 등장한 뒤 점화되어야 한다는 UX 기준 확정, 실화면 피드백, 최종 승인.
- AI 보조: 선택 카드 전달 경로와 셰이더 초기 상태 분석, 프리팹 저작 면적 보존 로직, 점화 이벤트 연결, 회귀 테스트 및 GameScene 시각 검증.
- 변경 영역: `GameManager`, `LighterDragTriggerController`, `LighterAnimationEventReceiver`, 관련 EditMode 테스트와 진행 기록.

## 2026-08-05 GSV-11 라이터 소각 종료 잔상 제거

- 이천서: 카드 위치와 이동 궤적을 보존하면서 소각 종료 뒤 상단 조각이 남지 않는 UX 기준을 확정하고 최종 검증을 승인한다.
- AI 보조: Git 이력·애니메이션 이벤트·소각 곡선 비교, 최소 코드 수정, 회귀 테스트와 시각 검증을 수행한다.

## 2026-08-05 아자젤 재발동 카드 조건 미확인 보정

- 이천서: 아자젤 계약 상태에서 이미 사용한 보위 나이프가 재발동되며 상대 공개 합이 17 이상일 때 전투가 예외로 멈추는 결함을 제보하고 최종 승인.
- AI 보조: `CoreLoopBattle.ContinueAzazelCardEffectSequence`가 카드 `UseState`만 확인하고 효과별 발동 조건(`CanStart`)은 확인하지 않은 채 재사용을 강제하던 경로를 추적. 카드 재활성화·재사용 전에 `CardEffectResolver.CanStart`를 먼저 확인해 조건 미충족 카드는 원래 상태 그대로 건너뛰도록 순서를 수정.
- 변경 영역: `CoreLoopBattle.cs`.
- 검증: 최초 작성 시점엔 Unity MCP 브릿지 미연결로 미실행이었으나, 같은 세션에서 브릿지 연결 뒤 AI가 컴파일 오류 0·전체 EditMode 1081/1083을 확인했다. 잔여 2건(`GSV13_U01`, `GSB01_U11`)은 무관한 기존 회귀. 씬·프리팹·외부 에셋·패키지 변경 없음.

## 2026-08-05 시작 악마 공개 뒤집기 연출·덱 중앙 배치

- 이천서: 라운드 종료 비공개 카드 뒤집기 연출을 시작 악마 2장 공개에도 적용하고, 그 장면의 악마 덱을 테이블 중앙으로 옮기도록 요청·최종 승인. 덱 중앙 배치는 이천서·AI 담당으로 완료.
- AI 보조(1차 시도, 이후 대체됨): `CardView.PlayRevealFlip`을 `DemonCardView`로 이식하고 `Bind()` 자동 감지 구조를 만들었으나, 실화면에서 회전축이 잘못 도는 문제의 원인(카드 자체 로컬 회전에 눕는 각도가 직접 들어가 있어 회전 오프셋이 다른 축으로 합성됨)을 anchor 분리로 우회했다.
- 실제 반영: Shim0Hwan이 별도 커밋(`38d6cfe`)에서 `_revealBaseLocalRotation * Quaternion.Euler(rotationOffset)` 방식으로 더 근본적으로 고쳐, anchor 없이도 카드 자체 눕는 각도와 무관하게 정상 동작하게 만들었다. 현재 `DemonCardView.cs`·`DemonCard.prefab`은 이 Shim0Hwan 버전이 최종본이며 AI의 anchor 우회 코드는 포함되지 않았다.
- 변경 영역: 뒤집기 애니메이션 본체는 Shim0Hwan 소유(`DemonCardView.cs`, `DemonCard.prefab`, `CardHoverAnchorPreviewEditor.cs`). 덱 위치(`StartingDemonRevealView.cs`, `GameScene.unity`)는 이천서·AI 담당.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 확인. 실제 플레이 화면에서 뒤집기 연출·덱 위치 확인은 이천서 몫으로 남음.

## 2026-08-05 마몬 주사위 투명 벽 회피 고정

- 이천서: 마몬 물리 주사위가 가끔 투명한 굴림판 벽에 닿아 못 나가거나 걸치게 착지하는 문제를 제보하고, 무조건 벽에 안 닿게 고쳐달라고 요청·최종 승인.
- AI 보조: 굴림판이 주사위 스폰 지점 기준 대칭 4면 벽(렌더러 없음)이라 "특정 벽에서 먼 방향"이 없고, 무작위 임펄스·회전이 우연히 벽까지 닿는 게 원인임을 확인. 확률 조정 대신 매 물리 프레임마다 위치를 안전 반경(`wallSafetyMargin` 0.18) 안으로 강제 고정하는 `ClampAwayFromWalls`를 추가해 벽 접촉을 구조적으로 차단.
- 변경 영역: `MammonDieView.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 확인. 전용 테스트는 없고, 실제 여러 회 굴려 벽 접촉 재현 여부 확인은 이천서 몫으로 남음.

## 2026-08-05 마몬 주사위 착지 뒤 자동 자세 보정 제거

- 이천서: 굴린 뒤 화면 갱신마다 주사위 자세가 미세하게 자동 조정되는 현상을 제보하고, 굴린 그대로 남게 해달라고 요청·최종 승인.
- AI 보조: `GameManager.ApplyView`가 매 프레임 같은 값으로 `mammonDie.Render(...)`를 재호출하고, `Render()`가 그때마다 `ApplyResultRotation`으로 물리적으로 멈춘 자세를 "깨끗한" 고정 자세로 되돌리고 있음을 추적. 물리 결과(`PlayPhysicalRoll`)는 `_hasPhysicalRestPose` 플래그로 표시해 이후 재호출에서 자세를 건드리지 않도록 하고, 결과가 외부에서 강제되는 표시용 굴림(`PlayRoll`)만 기존처럼 정확한 면을 강제하도록 분리.
- 변경 영역: `MammonDieView.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 확인. 실제 플레이에서 자동 보정이 사라졌는지 확인은 이천서 몫으로 남음.

## 2026-08-05 마몬 새 라운드 자동 굴림 연출·해골 버스트 지연·적용 보너스 총합 반영

- 이천서: 새 라운드 시작 시 마몬 재굴림에 연출이 없는 점, 해골이 나오면 굴리는 연출을 볼 틈도 없이 즉시 버스트 처리되는 점, 라운드 종료 시 주사위 "포함하기"를 골라도 총합 텍스트에 반영 안 되는 점을 함께 제보·요청·최종 승인.
- AI 보조: 라운드 시작 재굴림과 해골 버스트 판정이 CoreLoop 안에서 완전히 동기적으로(중간 프레임 없이) 처리돼 GameManager가 연출할 틈이 없었음을 추적. 플레이어 쪽 마몬 계약이 있을 때만 재굴림 직후 별도 `Stepped` 프레임을 추가하고, `GameManager`가 그 프레임에서 다이 뷰가 아직 모르는 값(다이 뷰 `CurrentValue` ≠ 새 값)이면 물리 굴림 연출을 재생하며 그 굴림 시간만큼 다음(버스트) 프레임을 기다리도록 연결. 총합 미반영은 `RoundResolver`가 승패 판정에는 마몬 보너스를 반영하면서도 그 값을 저장하지 않아 화면 총합이 보너스 없는 카드 합만 쓰던 것이 원인임을 확인하고, `MammonRuntimeState`에 "포함하기 선택 여부" 플래그를 추가해 화면 총합에 반영.
- 변경 영역: `CoreLoopBattle.cs`, `MammonDemonContractHandler.cs`, `GameScenePresentation.cs`, `MammonDieView.cs`, `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1081/1083(마몬 관련 기존 테스트 전부 통과, 잔여 2건은 무관한 기존 회귀) 확인. 전용 신규 테스트는 아직 없음. 실제 플레이 확인은 이천서 몫으로 남음.

## 2026-08-05 마몬 주사위 강제 결과 굴림의 눈 불일치 제거

- 이천서: 계약 직후 첫 표시·새 라운드 재굴림에서 굴리는 동안 보이던 숫자와 끝난 뒤 실제 판정값이 달라 순간적으로 숫자가 바뀌어 보이는 현상을 제보하고, 상호작용 리롤에서는 재현 안 됨을 확인해줌. 최종 승인.
- AI 보조: 두 경로 모두 결과가 이미 정해진 상태에서 실제 물리로 무작위로 굴린 뒤 끝에서 강제로 정답 자세로 스냅하고 있어, 물리가 우연히 다른 숫자에서 멈추면 그 위에 스냅되는 순간 불일치가 보였음을 특정. 결과가 정해진 굴림 전용으로 물리 없이 항상 정확히 정답 자세로 끝나는 결정적 스크립트 회전(`ScriptedRoll`)을 새로 만들어 대체하고, 결과 자체가 물리로 결정되는 자유 리롤 경로는 그대로 둠.
- 변경 영역: `MammonDieView.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 확인. 새 회전 애니메이션 실화면 확인은 전용 테스트가 없어 이천서 몫으로 남음.

## 2026-08-05 마몬 적용 보너스 총합 미반영 재수정 (Unity MCP 실행으로 직접 검증)

- 이천서: "포함하기"를 눌러도 여전히 총합에 반영 안 된다고 재확인, 직접 플레이 검증 후 고치라고 요청·최종 승인.
- AI 보조: 이전 수정이 실패한 이유(계약 상태의 `ResetRound()`가 화면이 결과를 보기도 전에 플래그를 꺼버림)를 추적하고, 계약 상태 대신 `CoreLoopBattle`에 `LastResolution`과 같은 방식의 안정된 스냅샷(`LastResolutionPlayerBonus`/`EnemyBonus`)을 추가해 근본적으로 다시 고침.
- 변경 영역: `CoreLoopBattle.cs`, `MammonDemonContractHandler.cs`, `GameScenePresentation.cs`.
- 검증: AI가 Unity MCP `execute_code`로 실제 전투를 마몬 계약과 함께 생성해 직접 플레이 검증했다. 손패 20 + 다이 4를 "포함"하면 결과 화면에서 정확히 `총합 : 24`(버스트)로 표시되고, "미포함"이면 `총합 : 14`로 다이가 더해지지 않음을 확인했다. 전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 재확인.

## 2026-08-05 마몬 계약 카드 클릭 비활성화

- 이천서: 마몬 계약 활성화 뒤 테이블 위 계약 카드를 눌러도 아무 일도 안 일어나게 해달라고 요청·최종 승인.
- AI 보조: 재굴림 트리거가 물리 주사위 클릭과 계약 카드 클릭 두 경로에 동시에 걸려 있고, 둘 다 같은 공유 판정 메서드를 쓰고 있어 그 메서드 자체를 막으면 주사위까지 죽는 문제를 확인. 카드의 `CanUse`만 계산하는 지점에서 마몬일 때 무조건 false로 고정해 카드 클릭만 분리해서 무력화했다. HUD 범용 버튼 목록은 이미 마몬을 제외하고 있어 추가 조치 불필요.
- 변경 영역: `GameScenePresentation.cs`.
- 검증: AI가 Unity MCP `execute_code`로 실제 전투에서 확인 — 계약 카드 `CanUse=False`(클릭 무반응), 주사위 `CanPlayerRerollMammon=True`(정상 작동 유지). 전체 EditMode 1081/1083(잔여 2건은 무관한 기존 회귀) 재확인.

## 2026-08-06 적 AI 행동 로직 4종 정교화 (리볼버·보위 나이프·아스모데우스·체인지)

- 이천서: 리볼버(자신 안전+상대 확정 버스트 시 안 쏨/같은 비공개 카드에 재사용 안 함/사탄 계약 중 절대 안 씀), 보위 나이프(독극물 없고 버스트 가능성 없으면 안 씀), 아스모데우스(상대 공개 합이 자신보다 높을 때만 강제 히트), 체인지(자신 영혼 2 이하일 때 상대를 끝장낼 확신 없으면 유료 체인지 안 함) 4가지 규칙을 요청하고, 리볼버 규칙 1번의 모호한 부분은 AI 질문에 "최종 승부로 가면 어차피 이기니 굳이 죽이지 않는다"는 취지로 답해 확정·최종 승인.
- AI 보조: 체인지 후보는 `EnemyPolicyDecisionSelector.TrySelectRequiredChange`가 정책 평가 전에 무조건 가로채, 기존 6개 정책의 체인지 점수 분기가 전부 도달 불가능한 죽은 코드였음을 확인. 선택기와 정책 양쪽에 공용 위험 판단(`EnemyChangeRiskEvaluator`)을 넣어 실제로 작동하게 고침. 리볼버는 `GunslingerEnemyPolicy`를 상태 없는 클래스에서 "이미 쏜 숫자" 기억(라운드·상대 체인지로 리셋)을 갖는 정책으로 바꾸고, 사탄 계약 활성 여부는 새 은닉 필드 없이 이미 공개된 행동 후보 목록만으로 판별해 공정성 규칙(적 AI는 숨은 정보를 못 봄)을 지켰다. `FinalBossEnemyPolicy`에도 같은 세 규칙을 이식했다(단, 이미 복잡한 텔레그래프·집행 단계 로직은 이번 범위에서 제외). 나이프·아스모데우스는 `EnforcerEnemyPolicy`·`CultistEnemyPolicy`·`FinalBossEnemyPolicy`의 기존 평가에 조건을 추가해 고쳤다.
- 변경 영역: `EnemyObservation.cs`, `EnemyObservationFactory.cs`, `EnemyPolicyDecisionSelector.cs`, `EnemyChangeRiskEvaluator.cs`(신규), `GunslingerEnemyPolicy.cs`, `FinalBossEnemyPolicy.cs`, `EnforcerEnemyPolicy.cs`, `CultistEnemyPolicy.cs`, `TricksterEnemyPolicy.cs`, `CowardlyGamblerEnemyPolicy.cs`, `EnemyBossPolicyTests.cs`(기존 테스트 2건을 새 규칙에 맞는 시나리오로 보강).
- 검증: AI가 Unity MCP로 컴파일 오류 0을 확인했다. 새 규칙 때문에 실패한 기존 테스트 3건(아스모데우스·압박 나이프·관측값 유출 가드)을 원인 특정 후 수정했고, 이후 전체 EditMode 1081/1083 통과(잔여 2건은 무관한 기존 렌더링 회귀)를 재확인했다. 실제 전투에서 4가지 행동이 체감상 의도대로 바뀌었는지 확인은 이천서 몫으로 남음.

## 2026-08-06 위스키 즉시 회복·초기 악마 풀 교체·초기 악마 화면 HUD/버튼 정리

- 이천서: 위스키가 상점을 나갈 때만 반영되는 것처럼 보이는 문제, 초기 악마 후보를 벨페고르→아스모데우스로 교체, 초기 악마 배정 씬의 기본 HUD 숨김과 확인 버튼을 프로젝트 표준 버튼 프리팹으로 교체 3건을 요청·최종 승인.
- AI 보조: 위스키는 로직(`ShopVisit.TryRest`)이 아니라 화면 갱신 경로의 문제였음을 추적 — 상점이 열린 동안엔 활성 전투가 없어 기존 HUD 갱신 코드가 아예 실행되지 않았고, 상점 전용 갱신 지점(`BindFormalShop`)은 골드만 쓰고 영혼은 건드리지 않았다. 상점 갱신 시마다 영혼 텍스트도 직접 쓰도록 연결해 고쳤다. 초기 악마 풀은 카탈로그의 상수 배열 한 줄만 교체. 초기 악마 화면은 유일하게 즉석 모드(OnGUI)로 그려지던 버튼을 확인하고, Unity MCP로 씬에 프로젝트 표준 버튼 프리팹 인스턴스를 배치해 일반 UI 버튼 클릭 방식으로 교체했으며, HUD의 라운드·영혼·골드 텍스트를 이 화면에서만 묶어 숨기는 기능을 추가했다.
- 변경 영역: `GameHudView.cs`, `GameManager.cs`, `GameFlowController.cs`, `StartingDemonRevealView.cs`, `DemonContractCatalog.cs`, `GameScene.unity`(신규 버튼 오브젝트만 추가), 관련 테스트 1건 갱신.
- 검증: AI가 컴파일 오류 0을 확인했다. 전체 EditMode 실행에서 나온 실패 중 1건(시작 악마 풀 테스트)은 의도된 변경이라 테스트를 갱신했고, 2건은 이전부터 기록돼 있던 무관한 렌더링 회귀, 1건(`CodexAssetTests.DXM07_U03`)은 이번 세션에서 라벨 텍스트를 새로 렌더링하며 생긴 것으로 보이는 TextMeshPro 메모리 내부 부작용(디스크상 관련 파일은 커밋 내용과 동일함을 확인, 에디터 재시작 시 해소 예상)으로 특정했다. 전체 EditMode 1080/1083 확인. 위스키 즉시 반영과 초기 악마 화면 실제 확인은 이천서 몫으로 남음.

## 2026-08-06 나이프·리볼버·아스모데우스 연출 버그 4건 수정

- 이천서: 나이프 연출 미완주로 카메라가 안 돌아옴, 적 리볼버 연출 시 시점이 강제로 플레이어 쪽으로 이동, 리볼버 실패 확정 시 겁먹은 표정이 원래대로 돌아감, 나이프 강제 뽑기 카드가 뒷면인데 총합에 먼저 반영되는 문제 4건을 요청·최종 승인.
- AI 보조: 나이프 성공/실패 애니메이션 클립을 직접 열어 실제 길이(4.8~5.5초)가 코드의 대기 시간(3.1초)보다 훨씬 길어 매번 잘려나간다는 근거를 확보하고, 정확한 재타이밍 대신 카메라 시야각·화면 효과를 강제로 원상복구하는 안전장치를 추가해 어떤 상황에서도 복귀되게 했다. 리볼버 카메라는 배우(플레이어/적)를 구분하지 않고 항상 전환되던 코드를 찾아 적 차례에는 전환을 건너뛰도록 고쳤다. 표정 문제는 리볼버 결과 분기에서 "실패"만 처리 누락돼 초기값으로 되돌아가던 지점을 찾아 리볼버는 성공·실패 모두 겁먹은 표정을 유지하도록 고쳤다. 총합 문제는 카드 뒤집기 애니메이션(시간이 걸림)과 총합 텍스트 갱신(즉시 반영)이 같은 순간에 함께 실행되고 있던 게 원인임을 특정하고, 카드가 실제로 앞면으로 바뀌는 시점까지 기다린 뒤에 총합을 갱신하도록 순서를 분리했다.
- 변경 영역: `PresentationManager.cs`, `GameManager.cs`, `GameScenePresentation.cs`, `CardView.cs`, 관련 테스트 1건 갱신.
- 검증: AI가 컴파일 오류 0을 확인했다. 새 규칙 때문에 실패한 기존 테스트 1건을 원인 특정 후 갱신했고, 전체 EditMode 1080/1083(잔여 3건은 무관한 기존 회귀·세션 한정 부작용) 재확인. 실제 화면에서 네 가지가 의도대로 보이는지 확인은 이천서 몫으로 남음.

## 2026-08-06 리볼버 시점 고정 추가 수정 (1차 수정 누락분)

- 이천서: 리볼버 시점 고정이 여전히 안 되고 연출 시작 시 강제 전환된다고 재확인·재요청·최종 승인.
- AI 보조: 1차 수정이 적용되는 코드 경로 자체가 실제로는 적의 리볼버에서 전혀 실행되지 않는다는 것을 추적 — 적의 리볼버는 "Ready" 단계 큐가 아예 생성되지 않고 바로 "Resolved" 단계로 처리되는데, 그 처리 안에 배우 구분 없이 항상 카메라를 "Current"로 되돌리는 호출이 남아 있어 실제 원인이었다. 이 호출을 플레이어 본인의 시퀀스일 때만 실행하도록 고쳤다.
- 변경 영역: `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1080/1083(잔여 3건은 무관한 기존 회귀) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 GSB-03 카드 행동별 적 대사 분리

- 이천서: 카드별 대사 분류, 실제 적 보유 카드 범위, 공통 상대 반응, 재생 순서·폴백·문장 수 기준을 기획하고 최종 승인한다.
- AI 보조: 자동 카드 발동 관측값, 단계·순번 기반 cue 중복 방지, 연출 전후 재생, 프로필별 필수 cue 검증, 여섯 적 대사 에셋과 GSB03 회귀 테스트를 구현·검증했다.
- 변경 영역: CoreLoop 내부 관측, GameScene 말풍선 프레젠테이션·디렉터·타임라인, 적 대사 SO 6개, 말풍선 테스트·기술 문서. 씬·프리팹 변경 없음.
- 검증: GSB03 범주·직접 영향·말풍선 클래스 테스트와 Unity 컴파일·Console Error 결과를 `Docs/gamescene-full-flow.md`에 기록한다. 기존 카메라 스택·말풍선 앵커 실패와 실제 Play Mode 대표 흐름은 별도 항목으로 남긴다.

## 2026-08-06 나이프 카드 뒷면-총합 동기화 추가 수정 (1차 수정의 적용 범위 누락)

- 이천서: 나이프 총합 반영이 여전히 뒷면 상태에서 일어난다고 재확인·재요청·최종 승인.
- AI 보조: 1차 수정이 특수한 경우(정확히 매칭되는 리빌→해결 빔 쌍)에만 적용되고, 그 패턴에 맞지 않는 일반 렌더링 경로에는 적용되지 않아 여전히 손패와 총합이 동시에 갱신되고 있었음을 추적했다. 각 카드가 "지금 실제로 뒤집기 애니메이션을 시작하는지" 스스로 보고하게 만들고, 그 신호를 기준으로 총합 갱신을 지연시키는 방식을 손패 렌더링이 일어나는 모든 경로에 통일 적용했다.
- 변경 영역: `CardView.cs`, `CardHand.cs`, `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1080/1083(잔여 3건은 무관한 기존 회귀) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 나이프 카드 뒷면-총합 문제, 진짜 원인 확정 (`총합` vs `공개 카드 합`은 서로 다른 계산)

- 이천서: "공개 카드 숫자는 안 반영되는데 총합에는 반영된다"고 더 정확히 짚어줘서, 앞선 두 차례 수정이 실제로는 다른 문제(연출 타이밍)를 고치고 있었음이 드러남. 최종 승인.
- AI 보조: "총합"과 "공개 카드 합"이 화면에 같이 표시되는 서로 다른 두 계산값이며, "공개 카드 합"은 뒷면 카드를 올바르게 제외하지만 "총합"은 앞·뒷면 구분 없이 손패 전체를 그대로 합산하고 있었음을 확인 — 애니메이션 타이밍과 무관한 순수 로직(계산식) 버그였다. "총합"이 전체를 합산하는 이유(자신의 진짜 비공개 카드는 원래 알아야 함) 자체는 유지하면서, 나이프처럼 연출상 일시적으로 뒷면인 카드만 별도로 제외하는 새 계산을 추가해 교체했다.
- 변경 영역: `BattleParticipant.cs`, `CoreLoopPresentation.cs`, `GameScenePresentation.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1080/1083(잔여 3건은 무관한 기존 회귀) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 테이블 버튼 간격·초기 악마 씬 연출 미표시·마몬 도감 해골 그림·악마 덱 중앙 배치

- 이천서: 테이블 히트·스탠드·체인지 버튼 간격, 초기 악마 씬의 최초 실행 시 카드 딜 연출 누락, 도감 마몬 항목의 "(해골 그림)" 텍스트를 실제 이미지로 교체, 초기 악마 씬 덱 중앙 배치 총 4건을 요청·최종 승인(마지막 1건은 대화 도중 추가).
- AI 보조: 버튼은 씬에 좌표만 있고 배치 로직이 없어 직접 좌표 조정. 초기 연출 누락은 `GameFlowController`의 실행 순서(-50)가 카메라 컨트롤러(기본 0)보다 빨라, 콜드 스타트 시 카메라가 준비되기 전에 딜 애니메이션이 먼저 재생되는 게 원인임을 추적 — 애니메이션 시작을 한 프레임 지연시켜 해결. 도감 해골 그림은 마몬 주사위가 쓰는 공유 텍스처 아틀라스에서 픽셀 분석으로 정확한 해골 영역을 찾아 서브 스프라이트로 분리하고, 기존 골드·영혼 아이콘과 같은 방식(TextMeshPro 스프라이트 에셋)으로 만들어 텍스트에 인라인 삽입했다. 덱 위치는 좌표값 하나만 중앙(0)으로 조정.
- 변경 영역: `GameScene.unity`, `StartingDemonRevealView.cs`, `DemonContractCatalog.cs`, `Dice.png`(스프라이트 슬라이스 추가), 신규 TMP 스프라이트 에셋 1개.
- 검증: AI가 컴파일 오류 0을 확인했다. 전체 EditMode 1078/1083 — 이번 세션에서 새로 나타난 도감 관련 테스트 3건은 관련 파일이 전혀 수정되지 않았음(`git status` 확인)을 근거로 이번 변경과 무관한 세션 한정 부작용으로 판단(기존에 문서화된 것과 같은 계열). 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 마몬 도감 텍스트 진짜 소스 특정·초기 연출 재수정·악마 덱 깊이 조정

- 이천서: 직전 항목의 마몬 도감 수정 후에도 "여전히 (해골 그림)"이라고 재확인, 이어서 최초 실행 시 초기 악마 딜 연출이 여전히 안 보인다는 점과 방금 중앙 정렬한 악마 덱을 카메라 기준 조금 더 뒤로 옮겨달라는 요청 총 2건 추가. 최종 승인.
- AI 보조: 도감 텍스트는 직전에 고친 `DemonContractCatalog.cs`가 실제로는 실행 중 쓰이지 않는 죽은 폴백이었고, 진짜 화면 표시 소스는 별도의 스크립터블 오브젝트(`mammon.asset`)였음을 추적해 그쪽을 수정했다. 초기 연출은 직전의 "1프레임 지연" 수정이 실제로는 재현이 계속돼, Unity MCP로 Play 모드에 직접 진입해 실측을 시도했으나 이 환경에서는 에디터 플레이어 루프가 사실상 진행되지 않아 프레임 단위 관측에 실패했다 — 대신 코드 추적으로 더 유력한 대안 원인(콜드 스타트 첫 프레임의 셰이더·머티리얼 워밍업으로 인한 순간 정지가 델타타임 기반 애니메이션 진행을 한 번에 건너뛰게 만드는 구조적 문제)을 찾아, 프레임 지연과 무관하게 안전한 방어적 수정(델타타임 클램프)을 적용했다. 이 이론은 실측으로 100% 확정하지 못했음을 투명하게 밝혔다. 덱 깊이는 씬 카메라의 실제 위치·방향을 조회해 "뒤"가 Z 감소 방향임을 확인한 뒤 적용했다.
- 변경 영역: `mammon.asset`, `StartingDemonRevealView.cs`, `GameScene.unity`(덱 위치만).
- 검증: AI가 컴파일 오류 0·전체 EditMode 1095/1102 확인. 잔여 7건 중 5건은 이전부터 기록된 무관 회귀·세션 한정 부작용과 동일 계열이며, 이번에 새로 나타난 2건(테이블 북 배치, 적 말풍선 앵커)도 관련 파일이 전혀 수정되지 않았음을 확인해 같은 계열로 판단했다. 초기 연출이 실제로 이제 보이는지, 덱 깊이가 적절한지는 실측 미완료 상태라 이천서의 실제 플레이 확인이 특히 중요함.

## 2026-08-06 테이블 버튼 간격 재조정 (직전 수정 유실 발견)

- 이천서: 히트·스탠드·체인지 버튼 간격을 다시 띄워달라고 재요청 — 이전에 넓혔다고 보고했던 간격이 실제로는 반영 안 돼 있었음. 최종 승인.
- AI 보조: 씬을 다시 열어 확인한 결과 직전 수정값이 아니라 최초 원본 좌표 그대로였고, `git diff`로 대조해 해당 프리팹 오버라이드 자체가 파일에 없었음을 확인 — 세션 중 반복된 Play 모드 진입·씬 재로드 과정에서 저장 없이 유실된 것으로 판단. 이번에는 저장 직후 `git diff`로 실제 파일 반영 여부를 직접 재확인하는 절차를 추가해 재발을 방지했다. 간격은 직전보다 더 넓게 재조정.
- 변경 영역: `GameScene.unity`(버튼 위치만).
- 검증: AI가 씬 저장 후 `git diff`로 오버라이드가 실제로 파일에 기록됐음을 대조 확인. 콘솔 오류 없음. 스크립트 변경 없어 EditMode 재실행 생략. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 히트 버튼이 도감(책) 프롭과 겹쳐 위치 소폭 축소

- 이천서: 직전 조정 후 스크린샷으로 히트 버튼이 도감(책) 프롭과 겹친다고 확인, 조금만 좁혀달라고 요청. 최종 승인.
- AI 보조: 카메라 방향을 근거로 로컬 X 증가가 화면상 왼쪽(도감 방향) 이동임을 확인하고, 겹침이 없었던 이전 값과 겹침이 생긴 직전 값 사이로 되돌려 조정.
- 변경 영역: `GameScene.unity`(히트 위치만).
- 검증: AI가 씬 저장 후 `git diff`로 값 반영 확인. 콘솔 오류 없음. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 테이블 버튼 간격 추가 축소 및 좌우대칭 복원

- 이천서: 히트 간격을 조금 더 좁혀달라고 요청하면서, 직전 수정이 히트만 좁혀 좌우대칭이 깨졌으니 체인지도 함께 좁혀야 한다고 정확히 지적. 최종 승인.
- AI 보조: 직전 수정이 도감 겹침 회피를 위해 히트만 줄이고 체인지는 그대로 둬 대칭이 깨져 있었음을 인정하고, 중앙 기준 좌우 간격을 0.85로 통일해 양쪽을 대칭으로 재조정.
- 변경 영역: `GameScene.unity`(히트·체인지 위치).
- 검증: AI가 씬 저장 후 `git diff`로 두 값 반영 확인. 콘솔 오류 없음. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 사소한 버그 5건 (입력 잠금·바알제붑 순서·독극물·상점/초기 씬 덱·적 덱 애니메이션)

- 이천서: 적 등장 연출 중 입력 잠금, 바알제붑 능력이 무기 연출보다 먼저 발동하는 순서 문제, 독극물 카드의 에이스(1/11) 취급 제거, 초기 악마·상점 씬의 상대 덱 비활성화 및 상점에 내 덱 추가, (대화 중 추가) 상대 덱 쌓임·줄어듦 연출 미발생 총 5건 요청. 최종 승인.
- AI 보조: 입력 잠금은 상점 화면에 이미 있던 메커니즘이 전투 화면에는 아예 연결돼 있지 않았던 것을 발견해 동일 메커니즘을 전투에도 연결. 바알제붑 순서는 서브에이전트 정적 추적으로 프레젠테이션이 아니라 CoreLoop 순수 로직에서 무기 연출 데이터 발행이 바알제붑 선택 대기보다 늦게 일어나는 게 원인임을 특정, 기존 이벤트 발행 시점 하나에 데이터만 앞당겨 얹는 방식으로 순서만 바로잡고 다른 타이밍은 건드리지 않음. 독극물은 헌드값 계산이 랭크 1을 전부 에이스로 취급하던 조건에 예외를 추가. 덱 노출 문제는 씬의 덱 오브젝트들이 화면 종류와 무관하게 항상 떠 있던 것과, 상점 진입 시 리셋된 내 덱이 다시 채워지지 않던 것 두 가지를 함께 수정. 적 덱 애니메이션 미발생은 서브에이전트가 로직이 아니라 씬에서 `GameManager`의 적 덱 필드 두 개가 애초에 연결돼 있지 않았던(=항상 null) 단순 배선 누락임을 특정.
- 변경 영역: `GameManager.cs`, `GameFlowController.cs`, `CoreLoopBattle.cs`, `HandValueCalculator.cs`, `GameScene.unity`(적 덱 필드 배선), `PoisonAutomaticCardTests.cs`(독극물 옛 에이스 버그에 기대 있던 테스트 1건 보강).
- 검증: AI가 컴파일 오류 0을 확인했다. 전체 EditMode 1112/1120 — 잔여 8건 중 7건은 이전부터 기록된 무관 회귀·세션 한정 부작용과 동일 계열, 1건은 이번에 처음 나타났지만 관련 파일이 전혀 수정되지 않아 같은 계열로 판단. 바알제붑 순서 수정은 정적 추적 근거만 확보했고, 다섯 항목 모두 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 적 등장 연출 후 입력이 영구히 잠긴 채로 남는 문제 (방어적 안전장치 추가)

- 이천서: 직전 입력 잠금 수정 이후 "연출 끝났는데도 버튼이 활성화되지 않습니다"라고 재확인, 입력이 풀리지 않는 회귀를 보고. 최종 승인.
- AI 보조: 잠금 해제 콜백 체인이 여러 단계(문 애니메이션, 상점 진입 전 퇴장 연출, 등장 트윈)를 거쳐야 하고, 화면 전환 재계산이 중간에 대기 플래그를 리셋할 수 있는 지점도 있어 특정 경로에서 콜백이 오지 않으면 영구 잠금 가능성이 있음을 정적으로 확인했다. 다만 이 환경에서는 Play 모드 프레임 진행이 멈춰 정확한 재현 경로를 실측하지 못해, 원인 규명 대신 "어떤 경로로 실패하든 6초 안에는 반드시 풀린다"는 시간 상한 안전장치를 추가하는 방어적 접근으로 전환.
- 변경 영역: `GameFlowController.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 근본 원인 미확정 상태이므로, 정상 케이스에서 즉시 풀리는지와 재발 시 6초 안에는 풀리는지 둘 다 이천서의 실제 확인이 필요함.

## 2026-08-06 진짜 원인 확정: 잠금은 풀리지만 전투 HUD가 다시 그려지지 않던 문제

- 이천서: 직전 안전장치를 적용해도 여전히 안 풀리고, 카드 사용은 되지만 히트·스탠드·체인지·계약만 안 된다고 구체적으로 재확인 — 이 관찰이 결정적 단서가 됨.
- AI 보조: 이 네 버튼만 `_inputLocked`를 직접 조건으로 쓴다는 것과, 그 활성 상태가 매 프레임이 아니라 특정 이벤트 때만 다시 만들어지는 캐시된 스냅샷이라는 것을 확인 — `SetPresentationInputLocked`가 잠금 플래그만 바꾸고 이 스냅샷을 다시 그리게 하는 호출을 빠뜨리고 있어서, 잠금 해제 자체는 정상 동작하는데도 화면상으로는 영원히 잠긴 것처럼 보였음을 코드 추적만으로 확정했다.
- 변경 영역: `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 이천서가 짚어준 정확한 증상 덕분에 이번엔 확정적 원인 특정이 가능했으나, 실제 화면 확인은 여전히 필요함.

## 2026-08-06 나이프 버스트 시 바알제붑 발동 순서, 리볼버는 정상·나이프만 잔여 결함

- 이천서: 단순 패배(21 이하)도 버스트로 취급되는 것 같다는 우려와, 총·칼 맞기가 완전히 끝난 뒤에야 바알제붑이 발동해야 한다는 요구 재확인. 최종 승인.
- AI 보조: CoreLoop 전체를 전수 조사해 단순 패배가 바알제붑으로 새는 경로는 없음을 확정(실제 규칙 위반 없음). 반면 직전 세션에서 고친 "무기 연출보다 바알제붑이 먼저 발동" 문제를 리볼버·나이프 각각 재검증한 결과, 리볼버는 이미 정상이었지만 나이프는 결과가 이미 발행됐는데도 여전히 "찌르기 시작" 단계로만 판정되는 필드 우선순위 문제가 남아 있어, 이천서가 느낀 "결과를 보기도 전에 바알제붑이 뜬다"는 증상이 나이프에 한해 실제로 재현됨을 확정했다.
- 변경 영역: `GameScenePresentation.cs`(나이프 연출 큐 판정 순서만, 평상시 흐름은 무변화).
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 패배-비버스트 구분은 코드 전수 조사로, 나이프 결함은 정적 추적으로 확정했다. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 규칙 개정: 최종 승부 비공개 카드 초과 21은 버스트가 아닌 공동 패배 (게임 규칙 변경, 버그 수정 아님)

- 이천서: 직전 항목 이후에도 "라운드 종료 시 내 카드 총합(비공개 포함)이 22 이상이면 바알제붑이 발동한다"고 재차 문제 제기. AI의 확인 질문에 "최종 승부에서 비공개 카드를 더해 22 이상"인 상황임을 특정. AI가 `Docs/rule.md` 7.4를 확인해 이 동작이 실제로는 **당시 문서화된 의도된 규칙**(로직 버그 아님)이었음을 밝히자, 이천서가 **규칙 자체를 바꾸기로 최종 결정** — 최종 승부에서 비공개 포함 21 초과는 더 이상 버스트가 아니라 승자 없는 공동 패배(양쪽 영혼 1)로 처리.
- AI 보조: 코드와 문서가 충돌할 때 임의로 고르지 않고 이천서에게 확인받는 절차를 따랐다(CLAUDE.md 원칙). 결정 확정 후 `RoundOutcome.MutualLoss`를 신설해 최종 승부 비교 로직(`RoundResolver.Resolve`)만 교체하고, 히트 중 즉시 버스트·카드 효과 버스트 등 다른 버스트 경로는 전혀 건드리지 않았다. 파이몬의 "라운드 종료 시 승자 강제 버스트" 로직은 승자가 없는 경우를 건너뛰도록 손봤고, 파이몬의 "상대 버스트 후" 트리거는 별도 원시 판정을 쓰고 있어 이번 범위(바알제붑류 계약) 밖으로 판단해 그대로 유지했다. `Docs/rule.md` 7.4·8.2와 변경 이력을 함께 개정했다.
- 변경 영역: `RoundResolver.cs`, `CoreLoopBattle.cs`, `CoreLoopPresentation.cs`, `GameScenePresentation.cs`, `Docs/rule.md`. 테스트 6개 파일, 7건을 새 규칙에 맞게 갱신(옛 규칙에 기대 짜여 있던 것들).
- 검증: AI가 컴파일 오류 0을 확인했다. 규칙 개정 직후 나온 실패 7건을 새 규칙 기준으로 갱신하고, 영혼 수치 계산 오차 1건을 추가로 잡았다. 최종 전체 EditMode 1112/1120 — 잔여 8건은 기존에 이미 규명된 무관 항목과 완전히 동일해, 이번 개정이 의도 밖 영향을 주지 않았음을 확인했다. 이 규칙 변경 자체의 게임 밸런스·의도 부합 여부에 대한 최종 승인은 이천서 몫이며, 실제 화면 확인도 필요함.

## 2026-08-06 상점 내 덱 클릭·미리보기 막힘 수정

- 이천서: 몇 항목 전 상점에 내 덱 파일을 보이게 했는데도 "아직 상점에서 내 덱 활성화 안 되네요"라고 재확인 — "활성화"가 시각적 표시가 아니라 클릭 열람을 뜻했음이 드러남. 최종 승인.
- AI 보조: 입력 처리 코드에서 덱 클릭 감지 자체가 상점 중엔 무조건 무시되도록 하드코딩돼 있었고, 설령 그걸 없애도 미리보기를 여는 함수가 활성 전투가 있어야만 동작하는 구조라 이중으로 막혀 있었음을 확인. 적 덱은 애초에 클릭 컴포넌트가 없어(장식용) 실수로 열릴 위험은 없음. 상점 중 클릭을 허용하고, 전투가 없을 때는 직전에 만든 라이터 제거용 보유 카드 목록 로직과 같은 방식으로 내 덱 열람 뷰를 새로 구성해 연결.
- 변경 영역: `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면에서 상점 중 덱 클릭이 되는지는 이천서 몫으로 남음.

## 2026-08-06 상점에서 내 덱이 아예 안 보이던 진짜 원인 (씬 배선 문제)

- 이천서: "내 말은 상점에 덱 자체가 없음. 상점 씬에도 내 덱이 있어야 해"라고 정정 — 클릭이 아니라 덱 오브젝트 자체가 안 보인다는 뜻이었음. 최종 승인.
- AI 보조: 상점 컨트롤러가 인스펙터 배열로 등록된 "전투 전용 오브젝트" 목록을 상점 열릴 때 통째로 비활성화하는데, 그 목록에 덱 더미 두 개가 (다른 진짜 전투 전용 요소들과 함께) 섞여 들어가 있었음을 확인 — 배열에서 덱 항목만 제거. 이 과정에서 배열 수정 형식을 잘못 지정해 배열이 일시적으로 전부 비는 사고가 있었으나 즉시 재수정으로 복구. 또한 씬을 저장하며 이번 세션에서 반복돼 온 것과 같은 계열의 의도치 않은 머티리얼 변경 2건을 발견해 원복했고, 직접 파일 편집 도중 Editor가 잠깐 응답하지 않는 문제(이전에도 겪은 것과 동일 패턴)가 있어 이천서에게 확인 요청 후 정상화.
- 변경 영역: `GameScene.unity`(배열 배선만, 스크립트 변경 없음).
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 상점 전용 라이터·도감 위치 조정

- 이천서: 상점에서 라이터를 위로, 도감을 왼쪽으로 살짝 옮겨달라고 요청(상점에서만 적용). 최종 승인.
- AI 보조: 라이터는 상점 전용 오브젝트라 직접 위치만 조정. 도감은 전투·상점 공용 고정 위치라, 상점 진입/퇴장 때 이미 호출되던 함수에 위치 오프셋 적용/복원 로직을 추가해 상점에서만 옮겨지도록 구현.
- 변경 영역: `ShopController.cs`, `GameScene.unity`(위치·배선).
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 도감 관련 기존 세션 한정 실패 항목의 값이 도감의 기본(비상점) 위치와 정확히 일치함을 교차 확인해 기본 위치는 안 건드렸음을 검증. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 기본 버튼 호버·계약서 클릭/연출·사탄 버튼 위치·턴 카운터·독극물 연출 6건

- 이천서: 6건을 한 번에 요청 — (1) 기본 버튼 호버 테두리(흰색), (2) 계약서 2장 중 어느 쪽을 눌러도 계약 시작, (3) 계약 시 맨 위 계약서가 사라지는 연출(불가능하면 알려달라는 단서 포함), (4) 사탄 정방향 능력의 확인 버튼을 화면 정중앙에, (5) Round 밑에 Turn 정보 표시, (6) 집행자 독극물 주입 시 카드가 중앙에 나타났다 아래로 사라지며 덱에 섞이는 연출(불가능하면 알려달라는 단서 포함). 최종 승인.
- AI 보조: (1) 기존 월드스페이스 오브젝트 호버 테두리 시스템은 `Renderer` 기반이라 UGUI 버튼에는 적용 불가함을 확인, 신규 UGUI 전용 호버 컴포넌트로 대체 구현. (2) 계약서 인터랙터블 판정이 스택 맨 위 1장에만 걸려 있던 버그를 수정. (3) 총·칼 애니메이션을 조사해 리깅에 종속돼 그대로 재사용은 불가능함을 확인 후, 신규 DOTween 연출로 대체 구현(어느 계약서를 누르든 항상 맨 위가 사라지도록 스택 판정도 함께 수정). (4) 사탄 확인 버튼 전용 배치 옵션을 HUD 배치 로직에 추가. (5) 턴 카운터를 CoreLoop 배틀 상태에 추가해 액션 종료 시점마다 증가시키고 HUD에 노출. (6) 독극물 주입 신호는 이미 CoreLoop에 있었음을 확인, "덱에 섞이는" 연출은 기존 덱 더미 삽입 애니메이션이 이미 처리하고 있어 추가 구현 없이 동작함을 확인했고, "중앙에 나타났다 사라지는" 부분만 신규 연출로 구현.
- 막힌 점과 해결: 이 프로젝트는 씬을 저장할 때마다(Play 모드 여부 무관) RenderTexture·셰이더 시간값·일부 UI 스크롤 위치·유틸리티 카메라 상태가 무작위로 재직렬화되는 기존 현상이 있음을 발견 — 두 차례 저장을 시도했다 매번 `git diff`로 발견해 전량 되돌리고, 세 번째는 Unity의 저장 파이프라인을 우회해 의도한 변경분만 YAML에 직접 삽입하는 방식으로 전환해 해결.
- 변경 영역: `GameScene.unity`(신규 오브젝트 1개, `GameManager` 필드 배선), `DefaultButton.prefab`, 그 외 CoreLoop/GameScene/UI 스크립트 다수.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건은 이번 변경과 무관한 기존 베이스라인과 정확히 일치) 확인. 계약서·사탄 버튼 배치를 검증하던 기존 테스트 2건은 의도된 동작 변경에 맞춰 함께 수정. Play 모드 프레임 진행이 안 되는 환경 제약으로, 실제 화면에서의 각 연출 확인은 이천서 몫으로 남음.

## 2026-08-06 계약서 연출 삭제·버튼 호버 근본 수정·사탄 버튼 배경 버그 수정

- 이천서: 위 6건 직후 후속 3건 요청 — (1) 계약서 사라지는 연출 삭제, (2) 기본 버튼 호버가 흰 테두리로 안 보인다는 버그 제보, (3) 사탄 버튼이 카드 2장 선택 전엔 배경이 아예 안 보이고 텍스트만 보이는 버그. 최종 승인.
- AI 보조: (1) 계약서는 다시 즉시 표시/숨김으로 되돌리고 연출 코드 제거(맨 위부터 사라지는 순서는 유지). (2) 원인을 다시 조사한 결과, 기본 버튼이 쓰는 커스텀 브러시 셰이더가 범용 UGUI 아웃라인 효과를 아예 반영하지 않는다는 근본 원인을 발견 — 이 셰이더가 카드·덱 호버와 동일한 자체 아웃라인 프로퍼티를 이미 갖고 있어, 거기에 맞춰 다시 구현. (3) 화면 중앙으로 옮기며 배경이 될 패널이 사라졌는데, 버튼 비활성 상태의 기본 투명도(Unity 기본값 50%)가 3D 배경 위에서 거의 안 보이는 수준이었던 게 원인 — 비활성 상태 배경을 불투명하게 수정(모든 기본 버튼에 공통 적용).
- 막힌 점과 해결: 버튼 색상 구조체를 부분적으로만 지정해 호출했다가 나머지 색상이 전부 초기화되는 사고가 있었음(이전 배열 사고와 같은 종류) — 즉시 전체 값을 다시 지정해 복구. 씬 저장 시 무관한 값들이 계속 재직렬화되는 현상이 이번에도 재발해, 씬을 원본으로 되돌리고 Unity 저장 경로를 거치지 않는 방식으로 다시 적용.
- 변경 영역: `ContractPaperView.cs`, `ContractPaperClickable.cs`, `UIHoverOutline.cs`, `DefaultButton.prefab`, `GameScene.unity`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면에서 호버·버튼 배경이 의도대로 보이는지는 이천서 몫으로 남음.

## 2026-08-06 버튼 호버 테두리 롤백·계약서 맨 위 1장만 호버·클릭

- 이천서: 버튼 호버 테두리는 두 번 고쳐도 안 됐으니 기능 자체를 롤백해달라고 요청, 이어서 계약서는 어느 걸 눌러도 되는 게 아니라 맨 위 1장만 호버·클릭되게 바꿔달라고 요청. 최종 승인.
- AI 보조: 버튼 호버 테두리 컴포넌트와 스크립트를 완전히 제거. 계약서는 "몇 장 남았든 물리적으로 맨 위인 카드만" 클릭 가능하도록 판정을 좁혔고(스택이 줄면 새로 맨 위가 된 카드가 자동으로 클릭 가능해짐), 호버 시 뜨는 설명 툴팁도 같은 규칙을 따르도록 함께 막았다.
- 변경 영역: `DefaultButton.prefab`(컴포넌트 제거), `ContractPaperView.cs`, `GameManager.cs`. `UIHoverOutline.cs` 삭제.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인.

## 2026-08-06 계약서 맨 위 1장만 호버되는 수정이 실제로 안 먹히던 문제 해결

- 이천서: 직전 수정 이후에도 여전히 계약서 2장 다 호버된다고 재제보. 최종 승인.
- AI 보조: 원인은 계약서 게이팅 로직 자체가 아니라, 마우스가 가리키는 대상이 없어질 때 이전에 뜬 설명 배지를 지우는 코드가 빠져 있던 기존 버그였음을 확인 — 계약서 A를 가리키다 B로 옮기면 B는 막혔지만 A의 배지가 안 지워지고 남아 있어 "B도 되는 것처럼" 보였다. 대상이 없을 때도 배지를 확실히 숨기도록 수정.
- 변경 영역: `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 위 수정이 카드 호버를 깨뜨린 회귀 수정

- 이천서: 직전 수정 직후 "카드 호버 창이 아예 안 뜬다"와 "계약서는 여전히 둘 다 호버된다"를 함께 제보. 최종 승인.
- AI 보조: 직전 수정("대상 없으면 배지 무조건 숨김")이 카드 호버 배지와 같은 위젯을 공유한다는 걸 놓쳐, 카드를 가리킬 때마다 카드 자신의 배지를 즉시 지워버리고 있었음을 확인 — 이게 카드 호버가 안 뜨던 원인. 카드/악마카드를 가리키는 중이 아닐 때만 숨기도록 조건을 좁혀 해결. 계약서 문제도 별도 원인이 아니라 같은 배지 뒤섞임 때문이었던 것으로 판단.
- 변경 영역: `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 계약서 콜라이더 자체를 꺼서 아래쪽 계약서 완전 비활성화

- 이천서: 배지 로직을 두 번 고쳐도 여전히 둘 다 호버된다고 재제보. 맨 위 1장만 호버·클릭되고 아래쪽은 순수 장식(적이 먼저 계약한 뒤에만 활성화)이라고 의도를 명확히 설명. 최종 승인.
- AI 보조: 호버 표시 로직을 우회하는 방식 대신, 비활성 계약서는 콜라이더 자체를 꺼서 레이캐스트가 물리적으로 맞을 수 없도록 근본적으로 바꿈 — 어떤 호버·클릭 경로든 원천적으로 반응하지 않게 됨. 이전에 추가했던 우회 게이팅 코드는 이제 불필요해져 정리.
- 변경 영역: `ContractPaperClickable.cs`, `GameManager.cs`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 라이터·위스키 가격 상승 규칙 분리 (위스키 고정, 라이터만 이용당 상승)

- 이천서: 상점 가중치 질문에 이어 "위스키: 2골드·영혼 2 회복, 라이터: 2골드·사용할수록 비용 1 오름"으로 특수 행동 규칙을 바꿔달라고 요청. 최종 승인.
- AI 보조: 실제 게임에 쓰이는 건 `ShopController.cs`(standalone 경로)이고, 아직 UI 연결이 안 된 별도 formal-run 백엔드(`ShopOfferGenerator`)는 이번엔 손대지 않았음을 확인. 기존엔 라이터·위스키 어느 쪽을 써도 둘 다 같이 가격이 올랐는데, 라이터 전용 카운터로 분리해 라이터만 이용당 오르고 위스키는 항상 고정 가격이 되도록 변경. 요청한 기본 수치(2골드, 영혼 2, 1골드 상승)는 이미 기존 기본값과 같아서 별도 조정 불필요.
- 막힌 점과 해결: 무관한 프리팹 저장 중 이번 세션에서 반복된 것과 같은 계열의 자산 드리프트가 발생 — 이번엔 카드 SO 21개(에이스 카드의 상점 등장 가중치 등 실제 값 변경 포함)가 오염돼 무관한 테스트가 새로 깨졌음. `git diff`로 발견해 전량 원복하고 재확인.
- 변경 영역: `ShopController.cs`, `Table Controller.prefab`, `ShopControllerTests.cs`, `Docs/rule.md`.
- 검증: AI가 컴파일 오류 0·전체 EditMode 1112/1120(잔여 8건 무관) 확인. 아직 UI에 안 붙은 formal-run 백엔드는 옛 결합 로직을 그대로 갖고 있으니, 나중에 그쪽이 실제로 연결될 때 같이 정리가 필요함.

## 2026-08-06 승리 골드를 적 프로필별 값으로 실제 연결 (사고 정정 포함)

- 이천서: 직전 작업 중 SO를 되돌리며 이천서가 직접 수정한 상점 가중치까지 날린 사고가 있어 사과함. 이후 "적 영혼·골드를 전체적으로 조정했는데 코드도 바꿔야 하나?"라고 질문, AI가 원인을 보고하자 "ㅇㅇ"로 실제 연결 작업을 승인.
- AI 보조: 적 영혼은 이미 SO 수정만으로 실제 게임에 반영되고 있었으나, 승리 골드는 SO에 적별 값을 넣어도 실제 상점(`ShopController`)이 그 값을 전혀 안 쓰고 항상 같은 고정 골드만 주고 있었음을 확인. 상점이 방금 이긴 적의 프로필 키를 받아 그 적 전용 골드 값을 실제로 지급하도록 연결. 이번엔 이천서가 실시간으로 편집 중인 `Enemies`/`Cards` SO 자산은 절대 건드리지 않고, 내 코드 변경으로 새로 깨진 테스트 2건만 정확히 짚어 수정.
- 변경 영역: `ShopController.cs`, `GameManager.cs`, `ShopDebugPanel.cs`, `ShopControllerTests.cs`, `GameSceneSpeechBubbleTests.cs`.
- 검증: AI가 컴파일 오류 0 확인. 내가 만든 신규 실패 2건만 재실행으로 사라짐을 확인했고, 이천서가 직접 편집 중인 자산 관련 실패는 손대지 않고 그대로 둠.

## 2026-08-06 초반 악마 지급 씬에 NPC 대사 추가, 말풍선 키 입력 진행 가능성 조사

- 이천서: 초반 악마 지급 씬의 NPC에게 "이번엔 이 악마들이 너와 함께할 것이다." 대사를 넣어달라고 요청. 이어서 대사 말풍선을 스페이스바 같은 특정 키로 다음 줄로 넘기는 게 적은 수정으로 가능한지 질문.
- AI 보조: 그 화면엔 이미 상점과 같은 캐릭터가 "상인 모드"로 서 있지만 대사가 없었음을 확인 — 상점이 쓰는 것과 똑같은 대사 시스템을 재사용해 화면 진입 시 1회만 대사가 뜨도록 연결. 키 입력 질문은 코드 작성 없이 조사만 진행 — 지금은 그런 기능이 없지만, 이미 있는 스페이스바 폴링 패턴을 그대로 가져다 쓰면 되는 작고 국소적인 변경으로 확인, 이천서가 원하면 이어서 구현 가능.
- 변경 영역: `SpeechCueKeys.cs`, `merchant_speech.asset`, `GameFlowController.cs`, `GameScene.unity`(씬이 에디터에서 안 열려 있는 걸 확인하고 직접 편집해 드리프트 위험 회피).
- 검증: AI가 컴파일 오류 0, 이번 변경으로 인한 신규 실패 없음을 확인. 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 초반 악마 지급 씬 대사가 안 뜨던 문제 조사 (원인 미확정)

- 이천서: 스크린샷과 함께 대사가 안 뜬다고 재현 확인, 진입 판정 자체가 잘못된 게 아닌지 의심 제기.
- AI 보조: 씬을 직접 조회해 여러 가설을 검증 — 진입 판정 로직, 대사 프로필 배선, 캐릭터 활성 상태는 모두 정상임을 확인해 배제. 실제 타이밍 버그 하나는 발견해 수정(캐릭터가 화면에 들어오기도 전에 대사를 걸고 있었음). 다만 Play 모드 테스트가 안 되는 환경이라 이게 진짜 원인인지 100% 확정은 못 함 — 캐릭터가 원래 작고 어둡게 렌더돼서 화면에서 눈에 안 띄었을 가능성도 함께 전달.
- 변경 영역: `GameFlowController.cs`.
- 검증: AI가 컴파일 오류 0, 신규 실패 없음 확인. 재테스트 후 여전히 안 뜨면 추가 확인 필요.

## 2026-08-06 계약서 사라지는 연출을 카드 뽑기 애니메이션과 최대한 유사하게 재구현

- 이천서: 계약서 사라지는 연출을 카드 뽑기 연출처럼 아래로(플레이어 쪽) 내려가게 해달라고 요청, AI의 1차 구현(DOTween 근사)에 "비슷하게가 아니라 완전히 동일하게 가능한지"를 묻는 질문으로 요구 수준을 정정. AI가 원본 애니메이션(`Draw.anim`)을 그대로 재사용할 수 없는 이유(좌표계·전용 셰이더 의존)를 설명하고 대안을 제시하자 "A로 갑니다. 대신 최대한 비슷하게"로 최종 승인.
- AI 보조: `Draw.anim`의 실제 키프레임 타이밍(정지 28%→감속 아크+회전 킥 28~60%→정지 유지 60~100%)을 추출해, 원본 클립을 그대로 쓰는 대신 같은 비율의 DOTween 시퀀스로 재현. 회전이 가산 방식이라 재사용 시 누적되지 않도록 리셋 로직도 함께 보강.
- 변경 영역: `ContractPaperClickable.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1120개 중 1110개 통과, 나머지 10건은 전부 이번 변경과 무관(진행 중인 SO 편집·기존 베이스라인)하며 계약서 관련 테스트는 모두 통과. 실제 화면에서의 타이밍 "느낌" 확인은 이천서 몫으로 남음.

## 2026-08-06 계약서 사라지는 연출이 화면 밖으로 다 나가기 전에 픽 사라지던 문제 수정

- 이천서: "갑자기 뚝 사라지는 느낌이네요. 완전히 화면 밖으로 내려올 때까지 유지되어야 합니다. 만약 뚝 사라지는 게 아니라면, 너무 빠른 걸 수도 있습니다."라고 재현 확인.
- AI 보조: 직전 3단계 구조(정지→감속 아크→정지 홀드)에서 알파 페이드와 위치 이동이 둘 다 재생시간의 60% 시점에 동시에 끝나도록 짜여 있었던 게 원인 — 실제로 프레임을 벗어났는지와 무관하게 그 시점에 알파가 이미 0이 돼 "뚝 사라지는" 것처럼 보였음. 3단 구조를 걷어내고 이동을 전체 재생시간에 걸친 단일 가속 곡선(`Ease.InQuad`)으로 바꾸고, 알파는 이동 후반부(55%~100%)에서만 페이드해 이동이 끝나는 시점에 정확히 함께 끝나도록 재배선 — 구조적으로 알파가 이동보다 먼저 끝날 수 없게 함. "너무 빠를 수도 있다"는 가설에도 대응해 재생시간(0.42→0.55초)과 이동 거리(2.5→4.5유닛)도 함께 늘림.
- 변경 영역: `ContractPaperClickable.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1120개 중 1110개 통과, 직전 라운드와 동일한 무관 실패 10건만 남아 신규 실패 없음을 확인. 정확히 몇 유닛이어야 프레임을 완전히 벗어나는지는 계산하지 않고 넉넉히 늘리는 쪽을 택했으므로, 이번에도 실제 화면 확인은 이천서 몫으로 남음.

## 2026-08-06 기본 버튼(DefaultButton) 호버 효과가 너무 옅어서 안 보이던 문제 수정

- 이천서: "기본 버튼 호버 효과 지금 없는 것 같은데 다시 넣어주세요"라고 요청.
- AI 보조: 씬을 직접 열어 대표 사용처(`ConfirmDemonsButton`)의 `Canvas`/`GraphicRaycaster` 배선을 확인한 결과 레이캐스트 경로 자체는 정상 — 문제는 작동이 끊긴 게 아니라 `DefaultButton.prefab`의 `Button.HighlightedColor`가 생성 시점부터 줄곧 Normal 대비 4%만 어두워지는 값이라 육안으로는 "효과가 없다"로 보일 만큼 옅었던 것. 20% 어두워지도록 값을 올려 호버 대비를 확실히 체감되게 만듦. 이 프리팹을 쓰는 모든 버튼에 공통 적용됨.
- 변경 영역: `DefaultButton.prefab`.
- 검증: AI가 컴파일 오류 0, 라이브 인스턴스에 새 값이 반영됨을 확인. 전체 EditMode 1120개 중 1110개 통과, 무관 실패 10건만 남아 신규 실패 없음. 씬 인스펙션을 위해 `GameScene.unity`를 저장 없이 열었다가 원래 활성 씬으로 되돌려 놓음. 20% 대비가 충분한지는 이천서의 육안 확인 필요.

## 2026-08-06 계약서에도 덱/도감과 동일한 스텐실 호버 아웃라인 추가

- 이천서: "계약서에도 뽑을 카드, 도감과 같은 호버 효과 추가해줘"라고 요청.
- AI 보조: 덱(`DeckStackView`)과 도감(`CodexClickable`)이 둘 다 공용 `PostProcessOutlineRegistry`에 자신의 `Renderer`를 등록해 URP 렌더러 피처가 그리는 스텐실 아웃라인을 공유함을 확인 — 도감은 여기에 펀치·SFX가 더 있지만 덱은 아웃라인만 있어서, "둘과 같은 효과"의 교집합인 아웃라인만 계약서에 동일 패턴으로 추가. 아웃라인을 매 프레임 켜고 끄려면 `GameManager`의 포인터 파이프라인에 연결해야 했는데, 계약서용 포인터 변수가 이미 있었지만 클릭 처리 시점에서만 계산되고 있어 호버용으로 쓸 수 없었다 — 이를 다른 호버 대상들과 같은 이른 시점으로 끌어올리고, 코드베이스 전역의 호버 초기화 지점 17곳 전부에 계약서 초기화도 함께 배선(빠뜨리면 특정 화면 전환 경로에서 아웃라인이 꺼지지 않고 남을 수 있어서).
- 변경 영역: `ContractPaperClickable.cs`, `GameManager.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1120개 중 1110개 통과, 무관 실패 10건만 남아 신규 실패 없음. 실제 화면에서 덱·도감과 같은 느낌인지 확인은 이천서 몫으로 남음.

## 2026-08-06 계약서 "위/아래" 판정이 실제 렌더 순서와 반대였던 근본 원인 수정

- 이천서: "지금 보니까 아래에 있는 계약서가 위로 판정되어 있네요! 여태까지 계약서 관련 버그도 이거 때문이었던 것 같습니다."라고 제보.
- AI 보조: `ContractPaperView`가 그동안 "누가 위인지"를 GameObject 이름 알파벳순(A<B)으로 정했는데, 실제 화면에 무엇이 앞에 그려지는지는 `SpriteRenderer.sortingOrder`가 결정하고 있었다 — 프리팹상 A=10, B=11이라 실제로는 B가 앞(위)이었다. 이름순=A가 위, 실제 렌더=B가 위, 이 어긋남 때문에 화면에 보이는 앞쪽 카드(B)의 콜라이더는 꺼져 있고 안 보이는 뒤쪽(A)만 클릭·호버가 걸리는 상태였음 — 이번 세션의 계약서 호버 혼란들이 다 이 근본 원인이었을 가능성이 높다고 판단. 정렬 기준을 이름에서 실제 `sortingOrder` 내림차순으로 교체. 관련 테스트도 실제 렌더 순서를 반영하도록 함께 정리.
- 변경 영역: `ContractPaperClickable.cs`, `ContractPaperView.cs`, `GameSceneCombatHudPresentationTests.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1120개 중 1110개 통과, 계약서 관련 테스트 포함 신규 실패 없음. 실제로 앞에 보이는 카드가 이제 정확히 반응하는지는 이천서의 재확인 필요.

## 2026-08-06 기본 버튼 호버가 20% 대비 조정 후에도 안 보인다는 재제보 — 앰버 톤으로 강화

- 이천서: "그리고 아직 기본 버튼에 호버 효과 없는 것 같은데요"라고, 직전 20% 대비 조정 후에도 재제보.
- AI 보조: 색 대비를 더 만지기 전에 포인터 이벤트 파이프라인부터 재점검 — `EventSystem` 중복 없음, `InputSystemUIInputModule`의 Point/Click 액션 정상 바인딩, `GraphicRaycaster.eventCamera`가 실제 활성 `MainCamera`를 정확히 가리킴을 확인해 파이프라인 자체는 문제없다고 판단. `DefaultButton.prefab`의 실제 사용처를 전수 조사해, `ConfirmDemonsButton` 외에 HUD의 `optionSlots` 100개(전투 중 카드/보상/상점 선택 목록)도 전부 같은 프리팹이고 인스턴스별 색상 오버라이드가 없음을 확인 — 이천서가 실제로 보고 있던 건 이 옵션 슬롯들일 가능성. 구조적 결함은 못 찾았고, "밝기만 20% 낮추는 변화가 칠해진 텍스처 위에서 잘 안 보였을 가능성"으로 판단해, 도감·덱·계약서 호버에 이미 쓰는 호박색(amber) 언어를 기본 버튼에도 동일 적용(회색 밝기 조정 → 호박색 색조 변화로 교체) — 색조 변화는 바탕 명도와 무관하게 항상 뚜렷하다.
- 변경 영역: `DefaultButton.prefab`. 모든 사용처(ConfirmDemonsButton, 옵션 슬롯 100개, ShopLeaveButton 등)에 공통 적용.
- 검증: AI가 컴파일 오류 0, 전체 EditMode 1120개 중 1110개 통과(무관 실패 10건만, 신규 실패 없음) 확인. 이번에도 안 보이면 이천서가 보는 버튼이 이 프리팹을 아예 안 쓰는 다른 화면(OnGUI 등)일 가능성이 높으므로 어느 화면인지 짚어달라고 요청 필요.

## 2026-08-06 상점 나가기(2번째)가 예외로 진행 불가하던 버그 수정

- 이천서: "상점 나가기 눌렀는데 다음과 같은 버그 발생하면서 아무 일도 안 일어남"이라며 `InvalidOperationException: The selected enemy profile no longer matches the stage soul configuration.` 전체 스택트레이스를 제보.
- AI 보조: `StageBattleFactory.Create`의 정합성 검사(`enemy.EnemyMaximumSoul != stage.EnemyMaximumSoul`이면 예외)까지 역추적 — 상대를 실제로 고르는 정상 경로(`TrySelectOpponent`)는 선택 순간 라이브 카탈로그로 `StageDefinition`을 매번 새로 만들어 항상 일치하지만, 상대 선택이 없는 고정 적 스테이지(최종 보스가 대표적)로 자동 진행하는 `TryAdvanceToNextStage`는 런 시작 시점에 미리 구워둔 옛 `StageDefinition`을 그대로 재사용하고 있었다 — 런 도중 적 프로필 SO(`maximumSoul`)가 조정되면 이 스냅샷만 낡은 채로 남아, 두 번째 상점을 나가 최종 보스로 들어가려는 순간 정합성 검사에 걸려 진행이 완전히 막히는 구조였다(이번 세션 초반의 "상대 영혼·골드 SO 조정" 작업과 정확히 맞물리는 시나리오).
- 상대 선택 없이 고정 적으로 진입하는 두 지점(`TryAdvanceToNextStage`, `PrepareCurrentStage`)에서 항상 라이브 카탈로그로 스테이지 정의를 다시 만들도록(`RefreshStageDefinition` 헬퍼) 통일 — 검사 자체는 정당한 불변식이라 그대로 뒀고, 애초에 항상 최신으로 구성하니 다시는 안 깨짐. 이 변경으로 "인스턴스가 재사용된다"는 걸 그대로 검증하던 기존 테스트 2건이 새로 깨져, 참조 동일성 대신 값 동등성 검증으로 함께 고쳐 통과시킴.
- 변경 영역: `StageProgressionSession.cs`, `OpponentSelectionFoundationTests.cs`, `OpponentSelectionIntegrationTests.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1120개 중 1110개 통과 — 새로 깨졌던 테스트 2건 포함 신규 실패 전부 해소, 무관 실패 10건만 남음. 실제로 두 번째 상점을 나가 최종 보스로 정상 진입하는지는 이천서의 재확인 필요.

## 2026-08-06 최종 보스전 진입 전 "현상수배서 공개" 화면 신규 추가

- 이천서: "최종 보스 때는 현상수배서가 중앙에 하나 (최종보스)만 뜨게 되어 있나요? 안 되어 있으면 그렇게 해주세요"라고 질문.
- AI 보조: 조사 결과 최종 보스는 상대 선택 자체가 없어(고정 적) 상점을 나가면 곧바로 전투가 시작되고, 현상수배서 화면은 전혀 뜨지 않고 있었음을 확인. 기존 "2명 중 1명 선택" 화면은 보스를 후보 풀에서 아예 제외하도록 설계돼 있어 억지로 확장하기보다, 이천서 승인을 받아 "초반 악마 지급" 화면과 같은 패턴의 새 연출 화면(`FinalBossReveal`)을 추가 — 포스터 클릭으로 전투 진입. 순수 로직 계층(`StageProgression`)은 건드리지 않고 UI 계층(`GameFlowController`)에서만 화면을 오버라이드했고, 뷰모델 데이터도 기존 생성자(수많은 테스트가 직접 호출)를 건드리지 않는 별도 정적 메서드로 추가. 씬에는 기존 상대 선택 화면의 포스터 프리팹을 새로 인스턴스화해 화면 정중앙(기존 좌/우 포스터 사이의 자연스러운 지점)에 배치.
- 막힌 점: `duplicate` 액션이 비활성 프리팹 자식 대상을 못 찾아 `create`+`prefab_path`로 우회, RectTransform 앵커링을 수동으로 재조정. 씬 저장 후 diff를 라인 단위로 전수 검토해 의도한 변경 외 실제 손실이 없음(기존 드리프트 패턴만 동반)을 확인.
- 변경 영역: `GameFlowScreen.cs`, `GameSceneMoodResolver.cs`, `OpponentSelectionView.cs`, `GameFlowController.cs`, `StageProgressionPresentation.cs`, `GameScene.unity`, `MoodControllerTests.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1121개(신규 무드 테스트 1건 포함) 중 1111개 통과, 무관 실패 10건만 남아 신규 실패 없음. `GameFlowScreenResolver`의 순수 상태 매핑은 건드리지 않아 관련 기존 테스트도 그대로 통과. 실제로 포스터가 정중앙에 뜨고 클릭 시 전투로 진입하는지는 이천서의 확인 필요.

## 2026-08-06 최종 보스 포스터 클릭 시 예외 발생 — 보스 영혼 9 반영 및 악마 구간(3·6·9) 재조정

- 이천서: 방금 추가한 최종 보스 포스터 클릭 시 `InvalidOperationException: Final boss policy requires maximum soul 8.` 발생을 제보하고, "최종 보스 영혼이 9로 조정되었으므로, 악마 구간도 3 6 9로 조정해주십시오"라고 요청.
- AI 보조: `FinalBossPhaseResolver.ExpectedMaximumSoul = 8`이 SO와 무관하게 코드에 하드코딩돼 있어, 이천서가 이미 SO의 `maximumSoul`을 9로 조정해둔 것과 어긋나 보스 AI가 매 결정마다 예외를 던지고 있었음을 확인. "악마 구간"은 `FinalBossPhase`(3·6 경계, 이미 요청과 일치)가 아니라 영혼 임계값에 따라 계약 악마가 바뀌는 `fixedDemonContractPhases`를 가리키는 것으로 판단 — 기존 (없음, 5, 2)가 옛 최대치 8과 3칸 균등 간격이었으므로 새 최대치 9에 맞춰 (없음, 6, 3)으로 갱신, 정확히 "3 6 9"와 일치.
- 코드 레벨 폴백 카탈로그와 실제 라이브 SO 양쪽 모두 갱신(테스트가 하드코딩 카탈로그를 직접 참조하는 경우가 많아 SO와 별개로 일치시켜야 함). 이 값들이 여러 파일(CoreLoop·StageProgression 양쪽)의 최종 보스 전용 테스트에 깊게 박혀 있어 전역 치환 대신 파일별로 테스트 의도를 먼저 파악한 뒤 조정 — 데미지로 특정 페이즈에 도달시키는 테스트는 데미지량 자체를 재계산, 라운드 시뮬레이션 테스트는 직접 계산하지 않고 실제 테스트 실행 결과를 신뢰해 반영.
- 변경 영역: `BossCombatDisplayModel.cs`, `EnemyCombatProfileCatalog.cs`, `final-boss.asset`, CoreLoop/StageProgression 테스트 7개 파일.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1122개 중 1112개 통과 — 신규 실패였던 항목 전부 해소, 무관 실패 10건만 남음. 실제로 포스터 클릭 시 예외 없이 전투가 시작되는지는 이천서의 재확인 필요.

## 2026-08-06 여섯 건 동시 요청 — 버튼 호버 애니메이션화·계약서 단순화·스테이지 순서·망치/독극물/카드뒤집기 버그

- 이천서: 한 번에 6가지 요청 — (1) 기본 버튼 호버를 외곽선 대신 확대/축소 애니메이션으로, (2) 계약서 사라짐을 순수 페이드아웃만 남기고 나머지 연출 전부 삭제, (3) 스테이지 시작 순서를 "상대 등장 → 오브젝트 세팅 → 버튼 활성화"로, (4) 망치 미표시/오타겟팅 버그, (5) 독극물 연출 카드 잘림 + 1라운드 미표시 버그, (6, 중간 추가) 상대 비공개 카드 뒤집기 위치 어긋남. TaskCreate로 6개 추적하며 순서대로 처리.
- (1) `DefaultButton.prefab`의 Transition을 `ColorTint`→`None`으로 바꾸고 신규 `Border.UI.UIButtonScaleFeedback`(DOTween 스케일, 호버 1.08배·클릭 0.92배, 비활성 버튼은 무시) 컴포넌트 추가 — 102개 인스턴스 전체 공통 적용.
- (2) `ContractPaperClickable.PlayDisappearAnimation`에서 이동/회전과 관련 죽은 코드를 전부 제거하고 알파 페이드 하나만 남김.
- (3) `GameFlowController`가 상대 등장 연출이 끝나기도 전에 오브젝트 세팅(`BindBattle`)을 즉시 호출하던 걸, 연출 완료 콜백에서 세팅 → 버튼 활성화 순으로 분리 호출하도록 재구성(`_pendingCombatBindSession`). 세이프티 타이머에도 동일 로직 반영.
- (4) `HammerAnimationController`: 타겟 위치가 최초 1회만 캡처되고 이후 재사용만 되던 걸 매번 재조회하도록 수정(위치 어긋남), 최초 캡처 실패(해당 프레임에 카드 렌더가 아직 안 끝난 경우)를 더 이상 치명적으로 취급하지 않도록 수정(미표시 버그) — 렌더 순서 자체는 지연 로직과 얽혀 있어 손대지 않음.
- (5) `PoisonInjectionAnnounceView`의 낙하 거리·시간을 늘리고 알파 페이드를 낙하 후반부에만 배치(계약서와 같은 패턴의 "화면 안에서 픽 사라짐" 방지). 1라운드 미표시는 별도 원인이 없었고 (3)번과 동일 원인(연출 대기 중 트리거되어 못 보고 지나침)으로 판단 — (3)번 수정으로 부수적으로 해결됨.
- (6) `CardView.PlayRevealFlip()`이 뒤집기 기준 위치를 진행 중인 이동 트윈의 중간값에서 캡처하던 걸, 시작 직전 `DOTween.Complete(transform)`으로 트윈을 완료시킨 뒤 캡처하도록 수정.
- 변경 영역: `DefaultButton.prefab`, `UIButtonScaleFeedback.cs`(신규), `ContractPaperClickable.cs`, `GameFlowController.cs`, `HammerAnimationController.cs`, `PoisonInjectionAnnounceView.cs`, `GameScene.unity`, `CardView.cs`, `GameSceneDeckPreviewTests.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1122개 중 1112개 통과 — 신규 깨진 테스트(구 `ColorTint` 검증)를 새 디자인에 맞게 수정해 통과, 무관 실패 10건만 남음. 망치·독극물·카드뒤집기 세 버그는 Play 모드 테스트 불가로 실제 재생 확인 못 함 — 코드 흐름 정밀 추적으로 각 증상과 정확히 들어맞는 원인을 찾아 수정했으나 이천서의 실제 확인 필요.

## 2026-08-06(2) 직전 세 건(등장 순서·독극물·망치) 재제보 — 근본 원인 재진단 + 씬 저장 시 카메라 값이 매번 틀어지는 별도 이슈 발견

- 이천서: 직전 라운드 세 수정이 불완전했다고 재제보 — (1) 상대 등장 연출 후 총잡이(기본값)→실제 적으로 뚝 바뀜, (2) 1라운드 독극물 연출 여전히 미표시 + 여전히 잘려 보임, (3) 망치가 찍는 순간 사라짐. "제대로" 고쳐달라고 명시.
- (1) 오브젝트 세팅(`BindBattle`) 안에서만 적 스프라이트를 확정하던 게 원인 — 연출은 프리팹 기본값으로 재생되다 연출 종료 후 `BindBattle`에서야 실제 적으로 바뀌어 보였음. `GameManager.PrepareEnemyAppearance(session)`을 스프라이트 확정 전용으로 추출해 연출 대기 시작 직전에 별도 호출(오브젝트 세팅 자체는 여전히 연출 완료 후 `BindBattle`에서 처리).
- (2) 수치 조정 대신 렌더링 구조를 직접 조사 — 독극물 카드가 월드스페이스 스프라이트인데, 전투 중 거의 항상 켜지는 HUD의 전체화면 반투명 딤머(`OptionPanel`)가 Canvas Screen Space - Overlay라서 `sortingOrder`와 무관하게 항상 3D/스프라이트보다 위에 그려지는 Unity 하드 제약을 확인 — 거리·타이밍을 아무리 조정해도 못 고치는 구조였음. `PoisonInjectionAnnounceView`를 월드스페이스 스프라이트→UI(Image)로 전면 재작성해 같은 HUD 캔버스의 딤머보다 나중 형제로 배치. 1라운드 미표시는 (1)과 동일 원인이라 (1) 수정으로 부수 해결.
- (3) 직전 라운드의 "매번 위치 재조회" 수정이 새 버그를 만듦 — 재조회 성공 여부와 무관하게 무조건 위치를 적용해, 세션 내내 한 번도 해석에 성공 못 하면 C# 기본값(월드 원점)이 그대로 대입돼 타격 순간 원점으로 순간이동하며 사라져 보였음. 성공했을 때만 적용하도록 가드 추가, 실패 시 트랜스폼을 건드리지 않고 비차단 처리.
- **부수 발견**: (2) 씬 수정 후 diff 전수 검토 중, 이번 수정 범위와 무관한 `CM_Current`(Map/Camera의 Cinemachine 카메라) 위치·회전·FOV·클립평면이 씬을 저장할 때마다 서로 다른 값으로 틀어져 있는 것을 발견 — 손으로 원복 후 검증차 한 번 더 저장했더니 또 다른 값으로 재차 틀어지는 걸 실제로 재현해 "저장할 때마다 매번 발생"함을 확정. 진짜 트리거(에디터 모드에서 라이브 블렌드 포즈를 실제 Transform에 기록하는 주체)는 이번 범위 밖이라 못 찾았고, 값만 원복 후 이후로는 `save` 없이 `load`로만 확인 — **이천서 또는 Shim0Hwan(카메라·아트)의 직접 확인 필요**, 방치 시 누구든 이 씬을 저장할 때마다 카메라 프레이밍이 재발할 수 있음.
- 변경 영역: `GameManager.cs`, `GameFlowController.cs`, `HammerAnimationController.cs`, `PoisonInjectionAnnounceView.cs`, `GameScene.unity`(카메라 값은 원복해 순변경 없음).
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1122개 중 1112개 통과, 무관 실패 10건만 남음. `Mat_CardSprite.mat`(줄바꿈만 다르고 내용 동일)·폰트 SDF 에셋(TMP 동적 아틀라스가 테스트 중 마주친 한글 글리프 4개를 순수 추가) 두 건이 부수적으로 더 걸렸으나 둘 다 자동 생성된 무해한 부수효과. 세 증상 모두 Play 모드 테스트 불가로 이천서의 실제 확인 필요.

## 2026-08-07 "플레이어 22 · 적 18인데 상대 패배 대사" 제보 — 스탠드 후 21 초과는 버스트가 아니라 그냥 패배로 정정

- 이천서: 최종 합 22(플레이어) vs 18(적)인데 적 패배 대사가 나온다고 제보. 확인 결과 코드-문서 모두 "한쪽만 21을 넘겨도 공동 패배(양쪽 영혼 1, 승자 없음)"로 일치하게 구현돼 있어 버그가 아니라 그렇게 설계된 규칙임을 먼저 보고 — "한 쪽만 넘으면 그 쪽의 패배, 둘 다 넘어야 공동 패배"가 의도였다고 확정하며 코드·문서 함께 수정 요청.
- **1차 수정(과잉 수정, 되돌림)**: "그 쪽의 패배"를 "그 쪽의 버스트"로 잘못 해석해 `PlayerBust`/`EnemyBust`(8.1의 버스트 영혼 피해 2/1)로 구현했다가, 이 때문에 원래 죽어있던 `HandleShowdownBustReplacement`의 바알제붑 개입 분기가 되살아나 관련 테스트가 깨짐 — 여기까지 고쳐서 일단 보고.
- 이천서: "스탠드 이후에 드러나는 건 버스트 아니라고요!!!! ... 22 이상이면 그건 버스트가 아니라 그냥 진 거라고요"라며 강하게 재정정 — 버스트 취급(8.1 피해·바알제붑 등 반응형 계약 개입) 자체가 없어야 한다는 뜻이었음.
- **2차 수정(최종)**: 한쪽만 21을 넘기면 넘지 않은 쪽이 `PlayerWin`/`EnemyWin`(8.2의 일반 패배 피해 1)으로 그냥 승리하도록 되돌림 — 버스트 총합은 정상 비교에서 항상 지는 쪽으로만 취급. 파이몬의 "상대가 버스트할 때마다" 트리거(`CoreLoopBattle.DidOpponentBust`)에도 Outcome이 아니라 실제 손패의 `IsBust`를 직접 보는 폴백이 있어 새 규칙에서도 몰래 발동할 수 있었던 것을 발견해 함께 제거.
- 변경 영역: `RoundResolver.cs`, `CoreLoopBattle.cs`, `rule.md`, CoreLoop 테스트 5개 파일.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1123개(신규 케이스 1건 포함) 중 1113개 통과, 무관 실패 10건만 남음. 씬 파일은 건드리지 않음. 실제 화면에서 대사·연출이 새 규칙대로 나오는지는 이천서의 확인 필요.

## 2026-08-07(2) 나이프 연출 도중 자동 발동 카드가 나오면 이후 나이프 애니메이션이 고장나는 문제 수정

- 이천서: 나이프 연출 도중 자동 발동 카드가 나오면 이후 나이프 사용 시 애니메이션이 안 나오거나 고장나는 경향이 있다고 제보 — 자동 발동 효과부터 먼저 적용한 뒤 나이프 연출을 이어가도록 요청.
- 순수 로직(`MilitaryKnifeEffectHandler`)은 이미 자동 발동 카드를 먼저 처리한 뒤 나이프를 완료하도록 올바르게 짜여 있어 문제는 연출 계층에 있었다. 적이 강제로 뽑은 자동 카드는 AI 정책이 같은 동기 호출 안에서 즉시 처리하므로, 드로우·공개·자동 카드 발동·나이프 판정이 전부 한 번의 호출에서 벌어져 `GameManager`의 `Stepped` 타임라인에 여러 비트가 쌓이는데, 나이프의 "공개"+"판정" 비트를 하나의 던지기 연출로 합치는 로직이 두 비트가 **정확히 인접**해야만 성립하도록 짜여 있어 자동 카드의 비트가 그 사이에 끼면 깨졌다. 더 결정적으로, 독극물 주입 연출(`TryPlayPoisonInjectionAnimation`)의 재생 여부가 애니메이션 대기 시간 계산에서 아예 누락돼 있어, 그 연출이 다 끝나기도 전에 코루틴이 나이프의 다음 비트로 넘어가 버렸다 — Animator 트리거가 전환 도중 다시 리셋되며 다음 나이프 사용까지 이상한 상태로 남는 게 실제 증상의 메커니즘으로 추정.
- `PoisonInjectionAnnounceView`에 `IsPlaying`/`TotalDurationSeconds`를 노출해 `GameManager`가 해머·마몬 주사위와 같은 패턴으로 대기 시간에 반영하고 실제로 폴링해서 기다리도록 배선했고(자동 발동 효과 먼저), 나이프의 인접성 검사도 타임라인 전체를 앞으로 스캔하는 헬퍼로 교체해 자동 카드가 끼어도 결합 연출이 이어지도록 했다(나이프 연출 이어가기).
- 변경 영역: `PoisonInjectionAnnounceView.cs`, `GameManager.cs`, CoreLoop 테스트 1개 파일(신규 테스트로 버그 전제와 수정 모두 확인).
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개(신규 케이스 1건 포함) 중 1114개 통과, 무관 실패 10건만 남음. 씬 파일은 건드리지 않음. Play 모드 테스트 불가로 실제 재생은 이천서의 확인 필요.

## 2026-08-07(3) 상점 도감 위치를 살짝 왼쪽으로, 배틀 스테이지와 동일한 위치로 통일

- 이천서: 상점 도감 위치를 아주 조금만 왼쪽으로, 배틀 스테이지에서도 같은 위치가 되도록 요청.
- `ShopController`가 도감 프롭(`CodexBook`)의 배틀 기본 위치를 캡처해 두고 상점이 열릴 때만 `codexShopLocalPositionOffset`(기존 `(0.25,0,0)`)만큼 밀었다 닫히면 되돌리는 구조라, 지금까지 상점·배틀 위치가 서로 달랐던 것을 확인. Unity MCP 스크린샷으로 로컬 +X가 실제 화면상 왼쪽임을 확인한 뒤, `CodexBook` 로컬 X를 기존 배틀 위치(1.39)에서 기존 상점 위치(1.64)보다 살짝 더 왼쪽인 1.69로 옮기고, 오프셋을 `(0,0,0)`으로 낮춰 상점·배틀이 항상 같은 위치를 쓰도록 통일.
- 씬 저장 후 diff 전수 검토 — 의도한 변경 외에 이미 확인된 자동 생성 드리프트(RenderTexture 재생성, 폰트 SDF 글리프 추가)와 새로 발견된 두 건(옵션 슬롯 100개 등 레이아웃 그룹 리스트의 앵커/위치가 아직 레이아웃 안 된 템플릿 상태로 리셋된 것 — 라이브 인스턴스는 정상임을 확인, `Card_Fire.mat`의 UV rect 한 줄이 무관 실패 테스트 중 하나인 라이터/화상 카드 연출 실행의 부수효과로 바뀐 것)을 자동 생성된 무해한 것으로 판단해 그대로 둠.
- 변경 영역: `GameScene.unity`(`CodexBook` 위치, `ShopController.codexShopLocalPositionOffset`).
- 검증: AI가 컴파일 오류 0 확인(코드 변경 없음). 전체 EditMode 1124개 중 1114개 통과, 무관 실패 10건만 남음(`DXM11_U01`도 그중 하나이며 프리팹 원본 검사라 이번 씬 오버라이드와 무관하게 원래도 실패 중이었음을 확인). 스크린샷으로 위치가 자연스러움을 확인했으나, 실제 상점 개폐 시 도감이 같은 자리에 머무는지는 이천서의 최종 확인 필요.

## 2026-08-07(4) 사탄 능력을 카드 클릭이 아니라 아스모데우스식 턴 시작 선택으로 변경

- 이천서: 사탄 능력의 사용 방식을 카드 클릭에서 아스모데우스·마몬과 동일한 턴 시작 "능력 사용하기/사용하지 않기" 선택으로 바꿔달라고 요청. 능력 판정 로직 자체는 변경 없음.
- 마몬·아스모데우스가 쓰던 공용 턴 시작 선택 파이프라인(`IDemonContractOwnerTurnStartChoiceHandler`)에 사탄을 세 번째로 추가하고, 카드를 직접 누르던 기존 진입점(`TryBeginPlayerSatanContractAction` 등 3곳)은 삭제했다. 선택지 렌더링은 기존에 종류 무관 구조라 UI 코드 추가 없이 자동으로 마몬·아스모데우스와 동일해졌다. 사탄이 더 이상 상시 행동 후보가 아니게 되어 깨진 최종 보스 AI의 "사탄 활성 감지" 로직(`EnemyObservation.OwnerHasActiveSatanContract` 신규)도 함께 고쳤다 — 다만 실제 적 계약 풀에는 사탄을 쥔 프로필이 없어 게임에는 영향 없음.
- 계약 서명 자체가 완결된 플레이어 행동으로 처리되는 기존 구조상, 즉시 스탠드하는 적과 맞물려 능력 완료 직후 곧바로 다음 턴 시작 선택이 뜨는 기존 연쇄 동작이 새 선택 UI로 인해 테스트 5건을 깨뜨렸다 — 프로덕션 버그가 아니라 테스트 헬퍼가 히트 전 잔여 선택지를 정리하지 않은 것이 원인이라 확인 후 헬퍼 한 줄과 낡은 단언문 하나만 고쳤다.
- 변경 영역(대부분 `dafbf8d`에 기커밋): `CoreLoopBattle.cs` 등 프로덕션 11개 파일, EditMode 테스트 8개 파일.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1113개 통과 — 기존 무관 베이스라인 10건 + 이번 실행에서만 관찰된 무관·비결정적 실패(`MOO01_U09`, 무드/조명 시스템으로 계약과 무관) 1건. 이천서가 실제 게임에서 사탄 사용법을 직접 테스트해 정상 동작 확인.

## 2026-08-07(5) 위스키 연출·적 등장 시퀀스·상인 대사·바알제붑/마몬 연출-판정 순서 5건

- 이천서: (1) 위스키 사용 시 영혼 UI가 초록색+확대 후 원상복귀하는 1회성 연출, (2) 적 등장 연출 후 세팅이 매끄럽지 않은 문제(등장 대사가 곧장 라운드 시작 대사로 바뀜, 오브젝트가 뚝 하고 생김) — 등장 연출 종료 3초 후에야 1라운드 시작+UI 활성화+초기 카드 딜 연출이 나오는 정확한 순서 요구, (3) 상점 "영혼 최대치인데 위스키 구매 시도" 전용 대사, (4) 바알제붑이 총/칼/사탄 연출이 끝나기(혹은 시작하기)도 전에 버스트를 미리 확정하는 문제 수정, (5) "마몬도 마찬가지로 주사위가 완전히 멈춘 후에만 턴 전환·버스트 확정"을 동일 부류로 추가 요청.
- 4개 영역이 서로 겹치지 않아 Explore 에이전트 4개를 병렬로 띄워 먼저 조사한 뒤 직접 구현했다.
- (1) 기존에 `PoisonInjectionAnnounceView`가 쓰던 DOTween 시퀀스 패턴을 그대로 재사용해 `GameHudView.PlaySoulRestoredFlourish()`를 추가하고, 일반/정식 상점 양쪽의 위스키 사용 지점에 연결했다.
- (2) 등장 애니메이션 종료 직후 `BindBattle`+입력 잠금 해제가 같은 프레임에 곧바로 실행되고 있었고, HUD는 그보다 훨씬 전인 화면 전환 시점에 이미 켜져 있었으며, 초기 카드 딜은 `CardHand`의 "첫 렌더는 애니메이션 안 함" 플래그 때문에 무조건 즉시 배치되고 있었다 — 3초 대기 자체가 아예 없었다. 등장 완료 콜백을 코루틴화해 3초 대기 후에야 배틀 바인드+HUD 활성화+입력 해제를 한 번에 수행하도록 바꾸고, 그 첫 렌더 예외 플래그를 제거해 초기 딜도 히트와 같은 드로우 연출을 타게 했다. 대사 문제는 라운드 1의 "라운드 시작" 대사가 "전투 시작(등장)" 대사보다 우선순위는 낮지만 특정 호출 순서에서 뒤이어 재발화해 덮어쓸 수 있는 구조적 허점이었다 — 라운드 1은 전투 시작 대사가 이미 대변하므로 애초에 라운드 시작 대사가 발화하지 않도록 가드를 바꿔 원천 차단했다.
- (3) 구매 가능 여부 enum에 `SoulFull` 케이스를 분리 추가하고 신규 대사 키+실제 대사를 등록했다.
- (4)+(5) 애니메이션 재생 중에도 그 결과(손패 합계·버스트)를 곧바로 그려버리는 구조였다 — 해머·독극물·칼은 이미 지연 렌더/실시간 폴링으로 처리돼 있었는데 **총(리볼버)만 빠져 있어** 버스트가 총성보다 먼저 확정되고 있었고, 마몬은 주사위가 실제로 멈췄는지 확인하는 코드 자체가 없어 고정 시간만 기다리고 있었다 — 둘 다 해머와 같은 방식(실시간 상태 폴링)으로 맞췄다. 사탄은 순수 로직 쪽에, 바알제붑 개입 시 필요한 연출 식별 필드가 버스트 판정 호출보다 늦게 기록되고 있어 그 개입 박자에서 사탄 연출이 아예 안 잡히던 버그를 발견해 기록 순서만 앞당겼다(판정 로직 자체는 불변).
- 변경 영역: `UI/GameHudView.cs`, `GameScene/{GameManager,GameFlowController,EnemySpeechDirector,ShopController,MammonDieView,SpeechCueKeys}.cs`, `GameScene/Card/CardHand.cs`, `CoreLoop/CoreLoopBattle.cs`, 상인 대사 에셋.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1114개 통과 — 무관 베이스라인 10건만 남고 신규 실패 없음. 씬 파일은 건드리지 않음. Play 모드 확인 불가로 각 연출의 실제 타이밍·색감은 이천서의 에디터 내 확인 필요.

## 2026-08-07(6) 적 등장 시퀀스 재수정·상인 대사 잔류·자동 카드 공개 순서·영혼 피해 확인

- 이천서: 직전 (5)의 등장 시퀀스 구현이 과했다고 정정 — 덱·버튼·계약서·도감은 처음부터 보여야 하는데 지금은 도감 빼고 다 비활성화됐다 라운드1에서 갑자기 나타난다고 지적. 첫 적 등장 시 상인의 마지막 대사가 남아 출력되는 버그, 적 자동 카드가 가끔 공개보다 효과가 먼저 나오는 버그, 숫자비교 패배 시 영혼 2를 잃는 것 같다는 버그 3건을 추가 제보.
- 원인은 직전 라운드에서 `BindBattle` 자체를 3초 지연시킨 것 — 덱/버튼/계약서는 전부 `Battle`이 바인딩돼야 그려지는 구조라 그 3초 동안 이전 화면 상태 그대로 방치돼 있었다. `BindBattle`은 등장 연출 종료 즉시 실행하도록 되돌리고, 대신 손패 렌더만 선택적으로 억제하는 새 플래그(`SuppressHandRenderUntilRoundOneStart`/`RevealRoundOneHands`)를 추가해 카드만 3초 후 애니메이션과 함께 등장하도록 재설계했다.
- 상인 대사 잔류는 `CharacterView.ExitMerchant()`가 말풍선은 건드리지 않는 것이 원인 — 등장 연출 직전에 말풍선을 명시적으로 숨기도록 추가했다.
- 자동 카드 공개 순서는 순수 로직 자체는 이미 올바르게 분리돼 있었고, "공개" 박자의 대기 시간이 실제 카드 뒤집기 애니메이션 길이가 아니라 고정 시간이었던 것이 원인 — 뒤집기가 실제로 재생됐는지를 관통시켜 대기 시간에 반영하도록 고쳤다(마몬 주사위 폴링과 같은 패턴).
- 영혼 피해 2는 버그가 아니라 문서화·테스트된 "정확히 21 승리 보너스" 규칙이었음을 확인 — 코드 변경 없이 이천서에게 그대로 보고했다.
- 변경 영역: `GameScene/{GameManager,GameFlowController}.cs`.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1114개 통과, 무관 베이스라인 10건만 남음. 씬 파일 미변경. Play 모드 확인 불가로 각 항목의 실제 동작은 이천서의 최종 확인 필요.

## 2026-08-07(7) 정확히 21 보너스 폐지·버스트 피해 대칭화·등장 시퀀스 재재수정

- 이천서: "21로 이겨도 2 피해 주지 않게 해줘"로 정확히 21 보너스 폐지를 명시적으로 지시. 반영 도중 "반대로 상대를 중도 버스트 시켰는데도 상대가 1 피해 밖에 안 입는 것도 수정"이라는 요청이 추가로 들어와, 8.1의 의도적 비대칭(플레이어 버스트 2/적 버스트 1)을 어떻게 바꿀지 확인 질문 후 "플레이어와 동일하게 2로" 확정. 이어서 (6)의 등장 시퀀스 구현이 여전히 부족하다고 재지적 — 덱·버튼·계약서·도감은 적 등장 연출이 시작되기 이전부터 있어야 한다고 정정. 적 비공개 카드 플립이 다음 행동(체인지 등)보다 먼저 완전히 끝나야 한다는 요청도 재확인.
- `RoundResolver.cs`에서 `PlayerTwentyOneWin` 피해를 2→1(보너스 폐지, "21!" 연출 표시만 유지)로, `EnemyBust` 피해를 1→2(플레이어와 대칭)로 바꾸고 `rule.md`·관련 테스트 4개를 갱신했다.
- 등장 시퀀스는 `charactersRoot`(적 캐릭터 계층)가 등장 연출이 시작되기 전까지 비활성 상태로 남아있다는 점을 이용해, `BindBattle` 자체를 전투 화면 전환 시점(등장 연출 시작 전)으로 완전히 앞당겼다 — 캐릭터는 비활성이라 안 보이고 테이블(덱·버튼·계약서·도감·HUD)만 실제로 그려진다.
- 플립 순서는 (6)의 수정이 특정 카드 효과에 국한되지 않고 모든 뒤집기 박자에 공통 적용되는 구조임을 재확인 — 추가 코드 변경 없이 이천서의 재확인 요청.
- 변경 영역: `CoreLoop/{RoundResolver,CoreLoopPresentation}.cs`, `GameScene/GameFlowController.cs`, `rule.md`, 테스트 4개.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1114개 통과, 무관 베이스라인 10건만 남음. 씬 파일 미변경. Play 모드 확인 불가로 각 항목의 실제 동작은 이천서의 최종 확인 필요.

## 2026-08-07(8) 비공개 카드 사용 시 공개(플립)가 효과 연출보다 먼저 끝나도록 근본 원인 수정

- 이천서: (7)의 재확인 요청에 (1) 수정 구슬로 카드 추가 시 뒤집기 안착 위치/각도가 이상함, (2) 여전히 카드가 애니메이션 없이 생김, (3) 비공개 카드 사용 시 효과 연출(리볼버 등)이 카드 공개보다 먼저 재생된다는 3건으로 답하며, 공개→플립 완료→효과 적용 순서로 고치면 1번도 함께 해결될 것 같다고 직접 원인을 짚어줬다.
- `CoreLoopBattle.TryBeginCardUse`에서 `card.Reveal()`과 효과 시작(`_cardEffectResolver.Begin`) 사이에 `RaiseStepped()`가 없어 두 사건이 같은 스냅샷에 함께 담기고 있었던 것이 원인 — `GameManager.ApplyView`에서 효과 애니메이션 트리거가 손패 재렌더보다 코드상 먼저 실행돼 효과가 먼저 보였다. 카드가 이전에 비공개였을 때만 `Reveal()` 직후 `RaiseStepped()`를 추가해 공개 전용 박자를 분리했다 — (7)에서 넣어둔 대기 시간 연장 로직이 이 새 박자에도 그대로 적용돼, 플립이 끝나야 다음 박자(효과 연출)로 넘어간다. 순수 로직의 판정 결과는 전혀 바뀌지 않았다.
- 수정 구슬 위치/각도 버그와 "애니메이션 없이 생기는" 문제는 사용자 진단대로 같은 근본 원인에서 비롯된 것으로 보고 별도 수정 없이 이 하나의 변경으로 해소되는지 재확인 요청.
- 변경 영역: `CoreLoop/CoreLoopBattle.cs` 1개 파일.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1114개 통과, 무관 베이스라인 10건만 남음(추가된 `RaiseStepped()` 한 번으로 인한 회귀 없음 확인). 씬 파일 미변경. Play 모드 확인 불가로 실제 동작은 이천서의 최종 확인 필요.

## 2026-08-07(9) 라운드1 카드 딜 애니메이션이 즉시 재렌더로 끊기는 실제 버그 수정

- 이천서: "여전히 애니메이션 없이 카드가 생긴다"고 재차 지적. AI가 직전 (8)에서 이걸 다른 수정(비공개 카드 사용 시 공개 순서)이 함께 해결해줄 것으로 잘못 추정하고 조사 없이 완료 처리했던 것이 원인 — 실제로는 라운드 1 최초 딜이라는 완전히 다른 코드 경로였다.
- `GameFlowController.BeginRoundOneAfterEntranceHold`가 `RevealRoundOneHands()`(카드 진입 애니메이션 시작) 바로 다음 줄에서 `SetPresentationInputLocked(false)`를 호출하는데, 이 메서드가 버튼 상태 갱신을 위해 자체적으로 `RefreshView()`를 한 번 더 호출한다(같은 프레임). 이 두 번째 렌더는 새 카드가 없어 `animate=false`가 되고, `CardHand.MoveCardToLayoutPosition`이 무조건 기존 트윈을 죽이고 즉시 최종 위치로 스냅하는 구조라 — 방금 시작한 진입 애니메이션이 바로 다음 줄에서 끊기고 있었다.
- `CardHand.MoveCardToLayoutPosition`/`MoveDemonCardToLayoutPosition`에 방어 로직 추가 — `animate=false`인데 이미 활성 트윈이 있으면 건드리지 않고 그대로 둔다. 호출 순서 자체는 정당한 설계라 건드리지 않고, "중복 렌더가 진행 중인 연출을 파괴 못 하게"만 고쳤다. 라운드 1뿐 아니라 같은 프레임에 `RefreshView`가 중복 호출되는 모든 상황에 동일하게 영향 미쳤을 것으로 추정.
- 변경 영역: `GameScene/Card/CardHand.cs` 1개 파일.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1124개 중 1114개 통과, 무관 베이스라인 10건만 남음. 씬 파일 미변경. Play 모드 확인 불가로 실제 동작은 이천서의 최종 확인 필요.
- 반성: 서로 다른 코드 경로를 같은 문제로 오판해 조사 없이 완료 처리한 것은 실수였음을 기록.

## 2026-08-07(10) 도감·호버 툴팁 카드 설명의 히트/스탠드/체인지/버스트에 리치 텍스트 적용

- 이천서: 도감·호버 툴팁 카드 설명의 히트/스탠드/체인지/버스트 용어를 볼드+색상(버스트 진한빨강, 히트 파랑, 스탠드 주황, 체인지 초록)으로 표시해달라고 요청. SO(ScriptableObject) 원본 데이터는 본인이 직접 관리 중이니 건드리지 말라고 명시.
- `CardDefinition.Description`이 도감·호버·상점 등 카드 설명 표시 경로의 공통 원천임을 확인 — SO의 원본 `description` 필드는 그대로 두고, `CardDefinition` 생성자가 그 값을 읽어들이는 시점에 리치 텍스트를 입히는 방식으로 구현(순수 문자열 조작이라 CoreLoop 순수성 규칙 위반 없음). 신규 `CardDescriptionRichTextFormatter` 정적 클래스로 4개 용어를 `<b><color=#RRGGBB>`로 감싸고, 도감 악마 로어 텍스트 파이프라인에도 동일 적용.
- 하드코딩된 카드 설명을 비교하던 기존 테스트 4건을 새 마크업에 맞춰 기대값 수정(설명 내용 자체는 불변).
- `git status` 확인 중 이미 수정돼 있던 `Normal/{02,03,04}_plain.asset` 3개(빈 description에 "아무 효과가 없습니다." 채움)를 발견 — 사용자가 언급한 본인 SO 변경과 일치해 건드리지 않음.
- 변경 영역: `CoreLoop/{CardDefinition,CardDescriptionRichTextFormatter(신규)}.cs`, `Content/CardContentCatalogSO.cs`, 테스트 1개.
- 검증: AI가 컴파일 오류 0 확인. 전체 EditMode 1126개 중 1116개 통과, 무관 베이스라인 10건만 남음. Play 모드 확인 불가로 실제 색상·볼드 렌더링(TMP Rich Text 옵션 포함)은 이천서의 최종 확인 필요.

## 2026-08-07(11) 튜토리얼 시스템 착수 — 레이어 A: 첫 플레이 감지 + 세션 강제 구성

- 이천서: 첫 플레이 시 상대 선택 없이 '겁쟁이 도박사' 고정, 시작 악마도 '바알제붑'·'아스모데우스' 고정인 스크립트 튜토리얼 전투를 요청. 아스모데우스가 계약서 왼쪽에서 나레이터로 등장해 0~6장 대사(타자기 출력)와 함께 정확한 카드 배분·행동 제한을 재현하는 전체 대본을 직접 작성해 전달. "오래 걸려도 되니 차근차근"이라고 명시.
- 규모가 커서 EnterPlanMode로 전환, Explore 에이전트 3개로 조사 후 레이어 A(첫 플레이 감지+세션 강제)→B(나레이터 UI+타자기)→C(행동 제한 게이트)→D(대본 전체 배선) 계획을 승인받았다.
- 구현 중 확인: 현재 라이브 프로토타입 세션도 이미 스테이지 0을 포함해 모든 일반 전투에서 상대 선택이 실제로 발생하고 있었고(하드코딩된 프로필은 후보 풀 생성용 템플릿일 뿐), 세션 팩토리가 저장 복원 등에서도 여러 번 불릴 수 있어 "튜토리얼 봤음" 플래그는 세션 생성이 아니라 `TryStartRun()`이 실제로 성공한 시점에 기록하도록 했다.
- 신규: `TutorialProgressStore`(런 단위로 지워지는 저장과 무관한 영속 플래그), `StartingDemonGrantGenerator`의 첫 호출만 고정 쌍을 반환하는 옵션, `StageProgressionSession`의 스테이지 0 강제 상대 경로(`TrySelectOpponent`와 동일한 재구성 로직 재사용, 스테이지 1 이후는 원래대로), `TutorialBattleFactory`(튜토리얼 스테이지에서만 스크립트 덱, 나머지는 기존 팩토리 위임).
- 신규 EditMode 테스트 8건 전부 통과 확인. 전체 EditMode 1137개 중 1126개 통과, 무관 베이스라인 10건 + 이번과 무관한 외부 변경 1건만 남음. 씬 파일은 건드리지 않았다(레이어 B에서 발생 예정).
- 변경 영역: `SaveLoad/TutorialProgressStore.cs`(신규), `StageProgression/{StartingDemonGrant,StageProgressionSession,TutorialBattleFactory(신규),StageBattleFactory}.cs`, `Bootstrap/StageProgressionRuntime.cs`, `UI/StageProgression/StageProgressionController.cs`, 테스트 1개(신규).

## 2026-08-07(12) 튜토리얼 시스템 — 레이어 B: 나레이터 UI + 타자기 텍스트 + 클릭 진행

- 이천서: 레이어 A 확인 후 레이어 B(나레이터 UI) 진행 지시. "튜토리얼 스테이지 한정으로 계약서 왼쪽에 아스모데우스 카드가 앞면으로 나와있으면 되는 거 알지?"로 설계 의도(계획 문서와 일치) 재확인.
- AI가 나레이터 카드(아스모데우스, 항상 앞면·사용 불가) + 타자기 텍스트(타이핑 중 클릭 시 즉시 완성, 완성 후 클릭 시 다음 대사 요청) + `GameManager` 클릭 게이트를 코드로 작성했으나, Unity Editor/MCP 연결이 안 돼 씬 배선이 막혔다.
- Unity 쪽에서 MCP for Unity 클라이언트 설정이 "Not Configured" 상태였던 것이 원인 — 이천서가 Unity 창에서 `Configure`로 재등록하고, Claude Code 재시작, Unity 쪽 연결도 한 번 더 끊었다 재연결한 뒤에야 정상적으로 잡혔다.
- 연결 후 AI가 `GameScene.unity`를 열어 실제 계약서 위치를 확인하고 그 왼쪽에 나레이터 카드 앵커를 배치, 기존 계약 후보 카드와 같은 `DemonCard.prefab`을 재사용하고, 이미 동작 중인 적 캐릭터의 말풍선 오브젝트를 복제해 텍스트 박스로 재활용(레이어·오버레이 카메라 배선을 그대로 물려받음)한 뒤 `GameManager` 필드까지 전부 연결하고 씬을 저장했다.
- 변경 영역: `GameScene/{TutorialNarratorView,TutorialTypewriterTextView}.cs`(신규), `GameScene/GameManager.cs`, `00. Scenes/GameScene.unity`.
- 검증: 전체 EditMode 1137개 중 1126개 통과, 실패 11건은 전부 기존 무관 베이스라인과 동일(신규 회귀 없음). Play 모드 확인 불가로 카드/텍스트 박스의 실제 크기·위치는 이천서의 에디터 내 최종 확인 필요(특히 텍스트 박스 스케일은 즉흥값이라 조정 가능성 높음).

## 2026-08-07(13) 튜토리얼 시스템 — 레이어 C: 행동 제한 게이트

- 이천서: 레이어 B 완료 후 실제 플레이에서 나레이터가 안 보인다고 지적 — AI가 "레이어 B는 부품만 만든 것이고, 실제로 띄우는 트리거는 레이어 D 몫"이라고 설명(이 설명이 완료 보고에 빠져있었던 점은 AI 쪽 커뮤니케이션 실수). "레이어 C로 넘어가면 보이냐"는 질문에는 C가 나레이터와 무관한 행동 제한 기능임을 정정하고, 계획대로 C 진행을 지시받았다.
- AI가 히트/스탠드/체인지 중 하나만 활성화하는 제한(HUD 프레젠테이션 레이어의 순수 함수 확장)과, 허용된 버튼에 기존 호버 강조색을 강제로 유지시키는 하이라이트, 리볼버 숫자 다이얼을 특정 값에 고정하고 확인 전까지 조작을 막는 기능을 코드로 구현했다.
- 레이어 B와 마찬가지로 이번에도 두 기능 모두 `GameManager`에 호출 가능한 메서드로만 열어뒀을 뿐, 실제로 언제 켜고 끌지는 아직 아무 것도 정해져 있지 않다(레이어 D 몫) — 지금은 화면상 아무 변화도 없는 게 정상이다.
- 변경 영역: `GameScene/{GameSceneCombatHudPresentation,TableCombatCommandGroup,RevolverNumberSelectorView,GameManager}.cs`, `UI/GameHudView.cs`, 테스트 1개(신규 2건 추가).
- 검증: 전체 EditMode 1139개 중 1128개 통과, 실패 11건은 전부 기존 무관 베이스라인과 동일(신규 회귀 없음). 씬 파일은 이번엔 전혀 안 건드림.

## 2026-08-07(14) 튜토리얼 시스템 — 레이어 D: 0~6장 대본 전체 배선 (완료)

- 이천서: 레이어 D 진행 지시, 대화 요약으로 유실된 0~6장 대본 원문을 다시 전달. "0~1장 대사 중 적 등장을 동시 진행할지 멈춰둘지" 질문에 "대사 끝날 때까지 대기"로 답변.
- AI가 계획 수립 중 레이어 B의 실제 버그(카메라 좌우 반전으로 나레이터 카드가 계약서 오른쪽에 있었음)를 스스로 발견해 수정했고, 계획 승인 후 구현 중에도 실제 게임 로직을 직접 실행해보며 대본과 엔진 규칙이 안 맞는 지점 3곳(카드 딜 순서, 카드 효과 사용 시 히든카드 취급, 적 AI의 자발적 체인지 조건)을 발견해 전부 대본의 의도를 최대한 살리는 방식으로 조정했다 — 특히 "적 체인지" 지시문 하나는 실제 엔진 규칙상 재현이 불가능함을 확인하고, 화면에 안 뜨는 괄호 지시문이라 조용히 대체 행동으로 바꿨다.
- 나레이터 대사 진행·게임플레이 제한 게이트·적 AI·카드 덱·전투 화면 전환 타이밍까지 튜토리얼 전체를 실제로 배선했다. 신규 EditMode 테스트로 라운드 1~3과 최종 계약 피날레 전체를 실제로 플레이해 카드·영혼 피해·결과가 대본과 정확히 일치하는지 검증했다.
- 변경 영역: `CoreLoop/TutorialEnemyPolicy.cs`(신규), `GameScene/{TutorialDirector(신규),GameManager,GameFlowController,GameSceneCombatHudPresentation,DemonContractSelectionView,TableCombatCommandGroup,RevolverNumberSelectorView}.cs`, `UI/GameHudView.cs`, `StageProgression/TutorialBattleFactory.cs`, 테스트 2개.
- 검증: 전체 EditMode 1142개 중 1131개 통과, 실패 11건은 기존 무관 베이스라인과 동일(신규 회귀 없음). 씬 파일은 레이어 B의 나레이터 위치 수정 1건 외 이번엔 추가 변경 없음(레이어 D는 순수 코드). Play 모드 확인 불가로 실제 재생 흐름은 이천서의 에디터 내 최종 확인 필요.
