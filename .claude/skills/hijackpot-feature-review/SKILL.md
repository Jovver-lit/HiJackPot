---
name: hijackpot-feature-review
description: Use when a new HIJACKPOT feature, system, relic, enemy or content idea is proposed or before starting to build one, to judge it against the project's design principles.
---

# HIJACKPOT 기능 제안 검토

기준은 `CLAUDE.md`의 최상위 개발 원칙, 2장 디자인 원칙, 5장 MVP 범위다. 먼저 읽는다. AI는 재미·타겟·필수 시스템을 독자적으로 확정하지 않고, 판단 근거와 대안을 제시한다.

## 절차
1. 아이디어를 한 문장으로 다시 정리한다. 의도가 모호하면 한 가지만 묻는다.
2. 비슷한 시스템이 이미 있는지 확인한다(`Assets/RouletteSystem/Scripts`, README). 역할이 겹치면 통합안을 먼저 낸다.
3. 아래 표를 채운다.

| 항목 | 내용 |
|---|---|
| Feature | 무엇을 추가하나 |
| Purpose | 왜 필요한가 |
| Player Experience | 플레이어가 무엇을 느끼고 어떤 판단을 하나 |
| Core Fun Contribution | SPIN 조작감 / 빌드 소유감 / 적 룰렛 상호작용 / 재도전 이유 중 무엇을 강화하나 (없으면 보류) |
| Risk | 템포 저하, 설명 증가, 관리 스트레스, 인과관계 불명확, 2인 팀 제작·테스트 부담 |
| Simpler Alternative | 기존 시스템의 수치·규칙 조정으로 같은 효과를 낼 방법 |

4. 디자인 원칙 10개 중 관련 항목을 통과/위반/불명으로 표시한다. 특히 3(칸 위치·크기·인접의 전략성), 4(STOP 외 판단 1개), 7(발동 순서 이해), 8(타이밍이 빌드를 압도하지 않음).
5. 분류: `Core` / `Supporting` / `Polish` / `Later`. MVP 범위 밖이거나 MVP 제외 항목(맵 분기, 다수 캐릭터, 장편 스토리, 영구 강화, PvP, 협동)은 `Later`.
6. 결론: 진행 / 축소 후 진행 / 보류 중 하나와 이유 2-3줄.
7. 진행이면 그레이박스 검증 방법(도형·텍스트·숫자로 무엇을 확인할지)과 이벤트 순서(`SpinStart → Land → AdjacentTrigger → Damage → DopamineChange` 중 어디에 걸리는지)를 적는다.

## 사용자 확인이 필요한 경우
핵심 플레이, 플레이어 경험, 되돌리기 어려운 구조, 큰 트레이드오프가 바뀔 때만 먼저 묻는다. 나머지는 기존 원칙에서 합리적인 기본값으로 진행한다.

## 기록
결정된 제안은 `Docs/design/feature-decisions.md`에 날짜, 분류, 결론을 한 줄씩 추가한다.
