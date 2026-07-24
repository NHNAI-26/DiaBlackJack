# 데블랙잭 프로젝트 문서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 최종 갱신: 2026-07-25

이 디렉터리는 게임 규칙, 기획, 개발 명세, AI 활용, 외부 출처 및 팀 기여 기록을 관리한다.

## 문서 목록

| 문서 | 용도 | 상태 |
| --- | --- | --- |
| [rule.md](./rule.md) | 게임 원본 규칙 | 사탄 카운터·권능 확정 v0.4 |
| [game-design-document.md](./game-design-document.md) | 전체 게임 기획과 임시 기획 결정 | 사탄 카운터·권능 구현 기준 반영 v0.3 |
| [core-loop-design.md](./core-loop-design.md) | 최소 코어 루프 기획 범위 | 4단계 완료 기준안 v0.1 |
| [core-loop-development-spec.md](./core-loop-development-spec.md) | 코어 루프 구조·상태·검증 명세 | 4단계 검증 완료 v0.1 |
| [core-loop-implementation-plan.md](./core-loop-implementation-plan.md) | 코어 루프 작업 순서·담당·일정·완료 증거 | 전체 완료 v0.1 |
| [core-loop-progress-log.md](./core-loop-progress-log.md) | 단계별 착수·구현·검증·변경 누적 기록 | 4단계 완료 v0.1 |
| [project-structure-and-mcp-reference.md](./project-structure-and-mcp-reference.md) | Unity 구조·어셈블리·MCP 참조 및 연결 확인 | 확인 완료 v0.1 |
| [ai-usage-technical-document.md](./ai-usage-technical-document.md) | 개발 AI와 게임 내 AI 활용 기술 기록 | 지속 갱신 |
| [team-role-technical-document.md](./team-role-technical-document.md) | 팀원별 담당 및 실제 구현 기록 | 지속 갱신 |
| [stage-progression-design.md](./stage-progression-design.md) | 코어 루프와 분리된 런·스테이지 진행 기획 범위 | SP-04 검증 완료 v0.1 |
| [stage-progression-development-spec.md](./stage-progression-development-spec.md) | 런 상태·스테이지 전이·전투·씬 연결·검증 명세 | SP-04 검증 완료 v0.1 |
| [stage-progression-implementation-plan.md](./stage-progression-implementation-plan.md) | 독립 진행 시스템 작업 순서·일정·완료 증거 | 전체 완료 v0.1 |
| [stage-progression-progress-log.md](./stage-progression-progress-log.md) | 독립 진행 시스템 착수·구현·검증 누적 기록 | SP-04 완료 v0.1 |
| [combat-action-design.md](./combat-action-design.md) | 폴드 삭제, 체인지 누적 비용과 패배 피해의 현행 전투 행동 규칙 | 현행 기획·코드 이관 완료 v0.2 |
| [combat-action-development-spec.md](./combat-action-development-spec.md) | 행동 상태·카드 이동·UI·진행 연결과 테스트 명세 | 현행 규칙 이관 완료 v0.2 |
| [combat-action-implementation-plan.md](./combat-action-implementation-plan.md) | BA-00~BA-05 이력과 현행 규칙 이관 결과 | 현행 규칙 이관 완료 v0.2 |
| [combat-action-progress-log.md](./combat-action-progress-log.md) | 전투 행동 확장 결정·구현·검증 누적 기록 | 현행 규칙 이관 완료 v0.2 |
| [card-use-design.md](./card-use-design.md) | 플레이어 일반 카드 사용 범위·카드별 규칙·전체 카드 재검토 | CU-M03 현행 기준 v0.5 |
| [card-use-development-spec.md](./card-use-development-spec.md) | 카드 정의·사용 상태·효과 선택·UI·진행 연결과 테스트 명세 | CU-M03 검증 완료 v0.3 |
| [card-use-implementation-plan.md](./card-use-implementation-plan.md) | CU-00~CU-06·CU-M01~M03 작업 순서와 단계별 검증 게이트 | CU-M03 완료 v0.3 |
| [card-use-progress-log.md](./card-use-progress-log.md) | 카드 사용 결정·구현·검증 누적 기록 | CU-M03 완료 |
| [automatic-card-design.md](./automatic-card-design.md) | 자동 발동 시점·원본 위치·5종 카드 규칙과 임시 기획 결정 | AC-05 부활초 구현 완료 v0.1 |
| [automatic-card-development-spec.md](./automatic-card-development-spec.md) | 공개 카드 유입·보류 선택·연속 처리·AI·UI·런 연결 명세 | AC-05 구현 완료·다음 AC-06 v0.1 |
| [automatic-card-implementation-plan.md](./automatic-card-implementation-plan.md) | AC-00~AC-06 구현 순서와 단계별 검증 게이트 | AC-05 완료 v0.1 |
| [automatic-card-progress-log.md](./automatic-card-progress-log.md) | 자동 발동 카드 결정·구현·검증 누적 기록 | AC-05 완료·대상 11/11·전체 456/456 v0.1 |
| [demonic-contract-design.md](./demonic-contract-design.md) | 악마 계약 제공·비용·지속·우선 악마 4종과 광신도 선택 밸런스 | DC-08 완료 v1.0 |
| [demonic-contract-development-spec.md](./demonic-contract-development-spec.md) | 계약 데이터·전투 상태·효과 훅·런 덱·UI·적 AI·테스트 명세 | DC-08 검증 완료 v1.0 |
| [demonic-contract-implementation-plan.md](./demonic-contract-implementation-plan.md) | DC-00~DC-08 구현 순서와 단계별 검증 게이트 | 전체 완료 v1.0 |
| [demonic-contract-progress-log.md](./demonic-contract-progress-log.md) | 계약 결정·구현·검증 누적 기록 | DC-08 완료, 대상 8/8·400시드·100자동전투·전체 397/397 |
| [battle-reward-design.md](./battle-reward-design.md) | 일반·엘리트·보스 전투 보상 규칙과 프로토타입 카드 풀 | RW-00~RW-05 완료 v0.1 |
| [battle-reward-development-spec.md](./battle-reward-development-spec.md) | 보상 생성·덱 추가·진행 상태·세션·UI와 테스트 명세 | RW-05 최종 검증 완료 v0.1 |
| [battle-reward-implementation-plan.md](./battle-reward-implementation-plan.md) | RW-00~RW-05 작업 순서와 단계별 검증 게이트 | 전체 완료 v0.1 |
| [battle-reward-progress-log.md](./battle-reward-progress-log.md) | 전투 보상 결정·구현·검증 누적 기록 | RW-05 완료 v0.1 |
| [enemy-combat-profile-design.md](./enemy-combat-profile-design.md) | 일반 적 3종·엘리트·보스의 전투 성향과 책임 경계 | 현행 폴드 삭제·코드 이관 완료 v0.7 |
| [enemy-combat-profile-development-spec.md](./enemy-combat-profile-development-spec.md) | 적 프로필·공개 관측·정책·카드·전투 변환과 테스트 명세 | EP-06 검증 완료 v0.6 |
| [enemy-combat-profile-implementation-plan.md](./enemy-combat-profile-implementation-plan.md) | EP-00~EP-06 작업 순서와 단계별 검증 게이트 | 전체 완료 v0.6 |
| [enemy-combat-profile-progress-log.md](./enemy-combat-profile-progress-log.md) | 적 전투 프로필 결정·구현·검증 누적 기록 | EP-06 완료 v0.6 |
| [enemy-selection-combat-ui-design.md](./enemy-selection-combat-ui-design.md) | 상대 후보 2명 비교·확정과 등급별 전투 정보 표시 규칙 | EUI-05 1차 범위 완료 v0.1 |
| [enemy-selection-combat-ui-development-spec.md](./enemy-selection-combat-ui-development-spec.md) | 후보 생성·선택 상태·전투 변환·안전 표시 스냅샷과 테스트 명세 | EUI-05 최종 검증 완료 v0.1 |
| [enemy-selection-combat-ui-implementation-plan.md](./enemy-selection-combat-ui-implementation-plan.md) | EUI-00~EUI-05 작업 순서와 단계별 검증 게이트 | 전체 완료 v0.1 |
| [enemy-selection-combat-ui-progress-log.md](./enemy-selection-combat-ui-progress-log.md) | 상대 선택·적 전투 정보 UI 결정·구현·검증 누적 기록 | EUI-05 완료 v0.1 |
| [formal-run-flow-design.md](./formal-run-flow-design.md) | 골드·상점과 `전투→이벤트→전투→이벤트→보스` 정식 진행 규칙 | 현행 상점 기획 v0.3·기술 이관 필요 |
| [formal-run-flow-development-spec.md](./formal-run-flow-development-spec.md) | 골드 정산·상점 거래·정식 런 조정 API와 테스트 명세 | RF-01 착수 가능 v0.1 |
| [formal-run-flow-implementation-plan.md](./formal-run-flow-implementation-plan.md) | RF-00~RF-05 HONG 인수인계 작업 순서와 검증 게이트 | RF-00 완료 v0.1 |
| [formal-run-flow-progress-log.md](./formal-run-flow-progress-log.md) | 정식 런 분업·결정·구현·검증 누적 기록 | RF-00 완료 v0.1 |
| [scene-presentation-design.md](./scene-presentation-design.md) | 2.5D 술집 테이블의 씬 흐름·월드 오브젝트·UI·상점 전환 기획 | 현행 기준안 v0.3 |

## 기록 원칙

1. 문서 작성 및 주요 의사결정의 책임자는 이름을 명시한다. 현재 책임자는 **이천서**다.
2. 계획한 업무와 실제 완료한 업무를 분리한다. 구현하지 않은 기능을 구현 완료로 기록하지 않는다.
3. AI와의 대화 원문을 그대로 복사하지 않는다. 목적, 주요 지시, 결과, 검증과 수정 내용을 기술적으로 정제해 기록한다.
4. 외부 에셋과 오픈소스는 이름, 버전, 출처, 사용 영역, 라이선스 확인 상태를 기록한다.
5. 팀원이 추가되면 실명 또는 팀에서 합의한 식별자와 실제 기여 파일·기능을 변경 이력에 추가한다.
6. 규칙과 구현이 충돌하면 임의로 숨기지 않고 결정 기록에 사유와 영향을 남긴다.

## 변경 기록

| 날짜 | 작성자 | 변경 내용 |
| --- | --- | --- |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-05 부활초의 양측 영혼 2 이상 재시작, 승패 없는 전용 전이와 부모 효과 취소를 구현하고 대상 11/11·CoreLoop 327/327·전체 EditMode 456/456로 검증 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-04 화염 방사기 소유자→상대 순차 폐기와 회중시계 수동 카드 재활성화·원본 유지/폐기를 구현하고 대상 11/11·CoreLoop 316/316·전체 EditMode 445/445로 검증 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-03 거짓말 탐지기 선언·소유자 전용 비교·지식 폐기를 구현하고 대상 10/10·CoreLoop 305/305·전체 EditMode 434/434로 검증 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-02 독극물의 계약 제한 스탠드·영혼 0 패배·승리 회복 예약을 양측 대칭으로 구현하고 대상 12/12·CoreLoop 295/295·전체 EditMode 424/424로 검증 |
| 2026-07-25 | 이천서 | 자동 발동 카드 AC-01 정의·공개 유입·보류 선택·입력 잠금·연속 처리 기반을 구현하고 대상 15/15·CoreLoop 283/283·전체 EditMode 412/412로 검증 |
| 2026-07-23 | 이천서 | 자동 발동 카드의 최초 배분 예외, 공개 유입·효과·버스트 순서, 5종 카드 프로토타입 결정과 AC-00~AC-06 기획·개발 명세·구현 계획·진행 기록 4종 수립 |
| 2026-07-23 | 이천서 | 악마 계약 DC-08 광신도 사탄·레비아탄 효용 조건과 벨페고르·마몬 결정적 균형 분산을 구현하고 400시드·100자동전투·대상 8/8·CoreLoop 268/268·전체 397/397로 검증 |
| 2026-07-23 | 이천서 | 악마 계약 DC-07 광신도 계약 정책·적 소유 4종 대칭 처리·Cultist 전용 덱·안전 표시와 반복 회귀를 구현하고 대상 12/12·CoreLoop 260/260·전체 389/389·Console 0으로 1차 범위를 완료 |
| 2026-07-23 | 이천서 | 악마 계약 DC-06 정상 차례 카운터·스탠드/버스트 제한·영혼 대가·양면 권능·화면을 구현하고 대상 8/8·전체 377/377·두 씬·두 해상도 검증 뒤 DC-07로 전환 |
| 2026-07-23 | 이천서 | 악마 계약 DC-05 비용 확인·후보 3장·활성 상태·소유자 전용 미리보기·독립/런 UI를 구현하고 대상 7/7·전체 369/369·두 씬·두 해상도 검증 뒤 DC-06 결정 게이트로 전환 |
| 2026-07-22 | 이천서 | 악마 계약 DC-03 벨페고르 플레이어 전용 미리보기·동일 ID 히트/덱 이동·행동 후 자동 스탠드·정보 은닉과 대상 9/9·전체 351/351을 완료하고 다음 단계를 DC-04로 전환 |
| 2026-07-22 | 이천서 | 악마 계약 DC-04 마몬 주입식 주사위·최종 합 선택, 레비아탄 리볼버 후속 버스트·영혼 대가·정보 은닉과 대상 11/11·전체 362/362를 완료하고 다음 단계를 DC-05로 전환 |
| 2026-07-22 | 이천서 | 악마 계약 DC-02 비용·횟수·후보 필수 선택·상호작용 ID·활성 계약·세션 전달과 대상 13/13·전체 342/342를 완료하고 다음 단계를 DC-03으로 전환 |
| 2026-07-22 | 이천서 | 악마 계약 DC-01 정의·런/전투 덱·후보 3장·버림 보충·전투 변환 기반과 테스트·기술 기록을 완료하고 다음 단계를 DC-02로 전환 |
| 2026-07-22 | 이천서 | 악마 카드의 선택·버스트·개별 대가 사망·생성 카드 합계·동일 악마 추가 계약과 사탄·바포메트·파이몬·벨리알의 전투 한정 수명을 원본·기획·개발 문서에 통일 |
| 2026-07-22 | 이천서 | 카드 사용 문서 상태를 CU-M03 기준으로 정리하고 악마 계약 DC-00 기획·개발 명세·구현 계획·진행 기록 4종 수립 |
| 2026-07-22 | 이천서 | 상점을 일반 카드 3장·악마 카드 2장, `SOLD OUT`, 라이터·위스키 1회 서비스로 구체화하고 화면 콘셉트 추가 |
| 2026-07-22 | 이천서 | 독극물을 즉시 스탠드 또는 영혼 베팅으로, 루시퍼를 무작위 악마 카드 5개 선택·영혼 1 대가로 확정 |
| 2026-07-22 | 이천서 | 위협용 해머의 상대 공개 카드 제거 규칙을 코드·적 AI·GameScene 표시·자동 테스트에 이관하고 전체 EditMode 308/308을 확인 |
| 2026-07-21 | 이천서 | 위협용 해머를 상대 공개 카드 제거 효과로 바로잡고, 폴드에 의존했던 독극물·루시퍼를 재설계 전까지 제외한 뒤 전체 카드를 개별 재검토 |
| 2026-07-21 | 이천서 | 계약을 전투 스테이지당 원칙적으로 1회로 제한하고 일반·악마 카드 덱을 분리했으며, 상점을 판매 카드 5장·개별 재고·새로고침 없음·1회 휴식으로 확정하고 가격·회복량은 미정으로 유지 |
| 2026-07-21 | 이천서 | 폴드를 플레이어·적 행동에서 삭제하고 체인지 비용을 전투 내 `0→1→2→3…` 영혼 누적·다음 전투 초기화 방식으로 변경 |
| 2026-07-21 | 이천서 | 폴드 계약 제거, 체인지 누적 비용·엄격한 영혼 조건·기존 비공개 카드 공개 폐기를 코드에 이관하고 전체 EditMode 306/306 검증 완료, `GameScene` 변경 없음 |
| 2026-07-21 | 이천서 | 테이블의 양측 덱·버림패, 비공개·사용 완료 카드 표시, 계약 영역, 행동 버튼과 상시 무기 장식 배치 확정 및 콘셉트 이미지 추가 |
| 2026-07-21 | 이천서 | 고정 카메라 2.5D 서부 술집, 월드 카드·덱, 3D 무기 연출과 동일 테이블 상인 전환 방향 확정 |
| 2026-07-20 | 이천서 | 서부 시대 결투 콘셉트에 맞춰 카드 표시명을 자동 권총에서 리볼버로, 군용 나이프에서 보위 나이프로 변경하고 위협용 해머는 유지 |
| 2026-07-19 | 이천서 | 전체 게임 기획서 작성 및 미확정 규칙의 프로토타입 임시안 정리 |
| 2026-07-19 | 이천서 | AI 활용 기술서, 팀 역할 기술서, 코어 루프 기획·개발 문서 체계 수립 |
| 2026-07-19 | 이천서 | 코어 루프 4일 구현 계획과 작업별 검증 게이트 수립 |
| 2026-07-19 | 이천서 | 코어 루프 단계별 단일 진행 기록과 1단계 착수 양식 작성 |
| 2026-07-19 | 이천서 | 프로젝트·MCP 구조 확인 및 코어 루프 1단계 규칙 기반 구현 완료 |
| 2026-07-19 | 이천서 | 코어 루프 2단계 전투 흐름·행동·단순 적 정책 구현 완료 |
| 2026-07-19 | 이천서 | 코어 루프 3단계 최소 UI·입력·승패·재시작 통합 완료 |
| 2026-07-19 | 이천서 | 코어 루프 4단계 전체 테스트·승패·재시작·씬·Console 검증과 기록 완료 |
| 2026-07-19 | 이천서 | 런·스테이지 진행을 코어 루프와 분리하고 기획·개발 명세·구현 계획·진행 기록 체계 작성 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-00 기준 확정과 Unity EditMode 27/27 회귀 검증 완료 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-01 순수 상태 기반 구현과 Unity EditMode 전체 40/40 검증 완료 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-02 전투 연결·지속 영혼 동기화와 Unity EditMode 전체 45/45 검증 완료 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-03 진행 UI·전용 진행 씬·공용 전투 씬 연결과 Unity EditMode 전체 50/50 검증 완료 |
| 2026-07-19 | 이천서 | 런·스테이지 SP-04 전체 승리·중간 패배·양쪽 재시작·10회 반복과 씬·Console 최종 검증 완료 |
| 2026-07-19 | 이천서 | 전투 행동 확장을 별도 작업으로 분리하고 폴드·체인지의 기획·개발 명세·구현 계획·진행 기록 4종 작성 |
| 2026-07-19 | 이천서 | 전투 행동 BA-01 선택 상태·카드 이동 기반 구현과 전체 EditMode 58/58 검증 완료 |
| 2026-07-19 | 이천서 | 전투 행동 BA-02 폴드 판정·라운드 종료·세션 전달 구현과 전체 EditMode 64/64 검증 완료 |
| 2026-07-19 | 이천서 | 전투 행동 BA-03 체인지 후보 선택·라운드당 제한·적 차례 연결 구현과 전체 EditMode 72/72 검증 완료 |
| 2026-07-19 | 이천서 | 전투 행동 BA-04 폴드·체인지 비용·후보 표시와 화면 입력 연결, CoreLoop 55/55·전체 EditMode 78/78·Game View 검증 완료 |
| 2026-07-19 | 이천서 | 전투 행동 BA-05 런 진행 세션 전달·영혼과 종료 동기화, 진행 테스트 27/27·전체 EditMode 82/82·실제 패배·재시작 검증으로 확장 작업 마감 |
| 2026-07-19 | 이천서 | 카드 사용 CU-00 일반 수동 카드 4종의 기획·개발 명세·구현 계획·진행 기록 수립, CU-01 즉시 착수 기준 확정 |
| 2026-07-19 | 이천서 | 카드 사용 CU-01 카드 정의·사용 상태·런 정의 키 보존 구현과 전체 EditMode 101/101 검증 완료 |
| 2026-07-19 | 이천서 | 카드 사용 CU-02 사용 가능 판정·선택 대기·효과 완료·종료 원인 기반 구현과 전체 EditMode 117/117 검증 완료 |
| 2026-07-19 | 이천서 | 카드 사용 CU-03 리볼버 추측·성공/실패·직접 버스트·정보 은닉 구현과 전체 EditMode 125/125 검증 완료 |
| 2026-07-19 | 이천서 | 카드 사용 CU-04 수정 구슬 순서 보존·해머 단일 비공개 교체·나이프 강제 드로우 구현과 전체 EditMode 143/143 검증 완료 |
| 2026-07-19 | 이천서 | 카드 사용 CU-05 카드별 표시·효과 선택 UI·독립/런 세션 전달과 종료 동기화 구현, 전체 EditMode 151/151·Game View·씬·Console 검증 완료 |
| 2026-07-20 | 이천서 | 카드 사용 CU-06 반복 회귀 5개·전체 EditMode 156/156, 실제 런 승리·패배 재시작과 씬·Console 최종 검증으로 1차 범위 마감 |
| 2026-07-20 | 이천서 | 정식 런을 지도 없는 `전투→이벤트→전투→이벤트→보스` 구조로 변경하고 상대 2명 선택·엘리트 제한·랜덤 이벤트 규칙 확정 |
| 2026-07-20 | 이천서 | 전투 보상을 카드 3장 중 1장 선택 또는 건너뛰기로 변경하고 엘리트 보상 3장 모두 높은 등급으로 확정 |
| 2026-07-20 | 이천서 | 전투 승리 골드·강한 적과 엘리트의 추가 골드, 상점 내 유료 휴식, 첫 개발의 전투 후 상점 고정 구조 확정 |
| 2026-07-20 | 이천서 | 최종 보스도 높은 등급 보상 처리 뒤에만 런 승리로 전환하도록 명시하고 전투 보상 기획·개발 명세·구현 계획·진행 기록 4종 작성 |
| 2026-07-20 | 이천서 | 전투 보상 RW-01 명시적 카탈로그·결정적 3장 제안·고유 런 카드 ID·최초 덱 복구 구현과 신규 8/8·전체 EditMode 164/164 검증 완료 |
| 2026-07-20 | 이천서 | 전투 보상 RW-02 선택 대기·선택·건너뛰기·일반/보스 완료 목적지와 실패 원자성 구현, 신규 7/7·전체 EditMode 171/171 검증 완료 |
| 2026-07-20 | 이천서 | 전투 보상 RW-03 실제 전투 승리·보상 생성·등급 주입·선택/건너뛰기·다음 전투 덱·재시작 통합과 신규 6/6·전체 EditMode 177/177 검증 완료 |
| 2026-07-20 | 이천서 | 전투 보상 RW-04 후보 3장·선택·건너뛰기·완료 목적지·결과 표시를 기존 진행 화면에 연결하고 신규 5/5·전체 EditMode 182/182·Game View 검증 완료 |
| 2026-07-20 | 이천서 | 전투 보상 RW-05 일반·보스 선택/건너뛰기와 패배·재시작을 각 10회 검증하고 전체 EditMode 187/187·씬·Console·Game View 최종 검증으로 1차 범위 마감 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-00에서 일반 적 3종·엘리트·보스의 성향, 공개 정보 AI, 카드 효과·전투 생성·상대 선택 연동과 EP-01~EP-06 구현·검증 문서 4종 수립 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-01에서 적 5종 카탈로그·안전 미리보기·교체 가능 정책·결정적 전투 설정을 구현하고 신규 12/12·전체 EditMode 199/199 검증 완료 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-02에서 공개 관측·유효 행동 후보·결정 재검증·적 카드 행위자/대상과 대칭 폴드를 구현하고 신규 8/8·CoreLoop 142/142·전체 EditMode 207/207 검증 완료 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-03에서 공개 구성 기반 숫자 추론과 총잡이·광신도·사기꾼 전용 정책을 구현하고 신규 10/10·CoreLoop 152/152·전체 EditMode 217/217 검증 완료 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-04에서 집행관 해머·나이프 후속 평가, 엘리트 추론 표시와 높은 등급 보상 연결을 구현하고 신규 11/11·CoreLoop 163/163·전체 EditMode 228/228 검증 완료 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-05에서 최종 보스 3구간 정책·강행동 예고·추론 방향 표시와 높은 등급 보상 뒤 런 승리를 구현하고 신규 16/16·CoreLoop 179/179·전체 EditMode 244/244 검증 완료 |
| 2026-07-20 | 이천서 | 적 전투 프로필 EP-06에서 선택 키를 실제 덱·영혼·정책·보상으로 연결하고 50회 상태 격리·엘리트/보스 결과·실제 런을 검증해 신규 16/16·StageProgression 81/81·CoreLoop 179/179·전체 EditMode 260/260으로 1차 범위 마감 |
| 2026-07-20 | 이천서 | 상대 선택·적 전투 정보 UI를 이천서 담당으로 변경하고 후보 2명 비교·확정, 등급별 추론·보스 예고 표시와 EUI-00~EUI-05 구현·검증 문서 4종 수립 |
| 2026-07-20 | 이천서 | EUI-01 결정적 상대 후보 2명·엘리트 제한·선택 대기 상태·세션 주입 기반을 구현하고 신규 13/13·StageProgression 94/94·CoreLoop 179/179·전체 EditMode 273/273 검증 완료 |
| 2026-07-20 | 이천서 | EUI-02 상대 후보 비교·로컬 집중·확정 가능 상태와 선택 상태 화면 유지를 구현하고 신규 9/9·StageProgression 103/103·CoreLoop 179/179·전체 EditMode 282/282·1280×720·1920×1080 화면 검증 완료 |
| 2026-07-20 | 이천서 | EUI-03 선택 상대의 원자적 확정·프로필 전투·보상·두 선택·고정 보스·재시작을 통합하고 신규 14/14·StageProgression 117/117·CoreLoop 179/179·전체 EditMode 296/296·실제 전투 씬 전환 검증 완료 |
| 2026-07-20 | 이천서 | EUI-04 일반·엘리트·보스 공개 정보와 보스 예고를 실제 전투 화면에 연결하고 신규 14/14·StageProgression 117/117·CoreLoop 193/193·전체 EditMode 310/310·1280×720·1920×1080 화면 검증 완료 |
| 2026-07-20 | 이천서 | EUI-05 후보·두 선택·보상·고정 보스·재시작·상태 격리 10회 반복 검증과 신규 5/5·StageProgression 122/122·CoreLoop 193/193·전체 EditMode 315/315·실제 두 씬·두 해상도·Console 0으로 1차 범위 마감 |
| 2026-07-20 | 이천서 | RF-00에서 최신 골드·상점 규칙을 반영해 HONG 인계용 정식 런 기획·개발 명세·구현 계획·진행 기록 4종 작성 |

