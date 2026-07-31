# 팀원 역할 기술서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 버전: v0.1  
> 최종 갱신: 2026-07-31

## 1. 기록 목적

팀원별 계획 업무와 실제 구현 영역을 구분해 기록한다. 제출 시에는 아이디어 제안이 아니라 실제로 제작·수정·검증한 결과를 파일, 씬, 기능 또는 산출물 단위로 제시한다.

현재 저장소의 Git 작성자 기록에서는 **이천서(Cheonseo Lee)**, **Shim0Hwan**, **HONG**이 확인되었다. 역할 계획과 실제 커밋 기여는 구분하며, 커밋으로 확인되지 않은 기능을 완료로 추정하지 않는다.

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

