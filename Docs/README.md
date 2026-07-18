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
| [core-loop-design.md](./core-loop-design.md) | 최소 코어 루프 기획 범위 | 3단계 구현 기준안 v0.1 |
| [core-loop-development-spec.md](./core-loop-development-spec.md) | 코어 루프 구조·상태·검증 명세 | 3단계 구현 기준 v0.1 |
| [core-loop-implementation-plan.md](./core-loop-implementation-plan.md) | 코어 루프 작업 순서·담당·일정·완료 증거 | 3단계 완료 v0.1 |
| [core-loop-progress-log.md](./core-loop-progress-log.md) | 단계별 착수·구현·검증·변경 누적 기록 | 3단계 완료 v0.1 |
| [project-structure-and-mcp-reference.md](./project-structure-and-mcp-reference.md) | Unity 구조·어셈블리·MCP 참조 및 연결 확인 | 확인 완료 v0.1 |
| [ai-usage-technical-document.md](./ai-usage-technical-document.md) | 개발 AI와 게임 내 AI 활용 기술 기록 | 지속 갱신 |
| [team-role-technical-document.md](./team-role-technical-document.md) | 팀원별 담당 및 실제 구현 기록 | 지속 갱신 |

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

