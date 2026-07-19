# 데블랙잭 프로젝트 문서

> 프로젝트: DiaBlackJack  
> 문서 책임자: 이천서  
> 최종 갱신: 2026-07-19

이 디렉터리는 게임 규칙, 기획, 개발 명세, AI 활용, 외부 출처 및 팀 기여 기록을 관리한다.

## 문서 목록

| 문서 | 용도 | 상태 |
| --- | --- | --- |
| [rule.md](./rule.md) | 게임 원본 규칙 | 기준 문서 |
| [game-design-document.md](./game-design-document.md) | 전체 게임 기획과 임시 기획 결정 | 초안 v0.1 |
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
| [combat-action-design.md](./combat-action-design.md) | 폴드·체인지 전투 행동의 기획 범위와 임시 결정 | BA-00 기준안 v0.1 |
| [combat-action-development-spec.md](./combat-action-development-spec.md) | 행동 상태·카드 이동·UI·진행 연결과 테스트 명세 | BA-01 기반 구현 완료 v0.1 |
| [combat-action-implementation-plan.md](./combat-action-implementation-plan.md) | BA-00~BA-05 작업 순서와 단계별 검증 게이트 | BA-02 착수 대기 v0.1 |
| [combat-action-progress-log.md](./combat-action-progress-log.md) | 전투 행동 확장 결정·구현·검증 누적 기록 | BA-01 완료 v0.1 |

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

