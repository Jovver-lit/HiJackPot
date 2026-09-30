# HIJACKPOT: Jack the Rules

> 행운을 빼앗는 마법 카지노에서, 룰렛의 규칙을 훔쳐 탈출하라.

딜러와 룰렛으로 결투하며 **판돈을 걸고 → SPIN으로 키우고 → 적당할 때 CASH OUT**하고, 딜러마다 다른 하우스 룰을 달성해 **딜러의 룰렛 칸을 빼앗아(HIJACK) 내 룰렛을 강하게 만드는** 싱글플레이 2D 턴제 로그라이트다. 2인 팀, Unity 6.

## 지금 상태

- 플레이 가능: **5층 런** — 1층 토끼 튜토리얼 → 2~4층 문 두 개 중 딜러 선택(여우·고양이·까마귀) → 5층 보스 「매니저」(바깥 링 테이블). 판돈 모델, 딜러별 하우스 룰·HIJACK, 역탈취, 역전 보정, 보스 격파 시 다음 런부터 바깥 링 해금
- 다음: 직접 플레이테스트로 전체 런 검증(`Docs/playtest/`에 기록)

## 실행

1. Unity **6000.4.0f1**로 이 폴더를 연다.
2. `Assets/Scenes/SampleScene.unity`(기준 씬)를 열고 Play.
3. 룰렛 가운데 **SPIN**을 길게 눌렀다 놓고, 판돈이 충분하면 **CASH OUT**.

씬은 코드로 생성된다. 구조를 바꿨다면 메뉴 `Tools > HIJACKPOT > Build Battle Scene (SampleScene)`으로 다시 만든다.

## 폴더 지도

| 경로 | 무엇 |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | 프로젝트 지침 원본: 설계 원칙, MVP 범위, 작업·git 규칙 |
| [`CONTEXT.md`](CONTEXT.md) | 게임 용어집(칩, 판돈, 보험, HIJACK…). 코드·UI·문서 용어의 기준 |
| [`Docs/`](Docs/) | 기획 결정(ADR), 기능 분류, 플레이테스트·시뮬레이션 기록 |
| [`Assets/RouletteSystem/`](Assets/RouletteSystem/) | 게임 본체: 규칙 코어, 화면 스크립트, 씬 빌더, 테스트, 아트·사운드 |
| [`Assets/Scenes/SampleScene.unity`](Assets/Scenes/) | 기준 씬(룰렛 배틀). 모든 화면 변경은 이 씬에서 확인 |
| [`Assets/README.md`](Assets/README.md) | `Assets/` 안의 나머지 폴더(설정, TextMesh Pro 등) 안내 |
| [`.claude/skills/`](.claude/skills/) | Claude Code 프로젝트 스킬(Unity 작업 절차, 기능 검토, 밸런스 점검) |
| `AGENTS.md` | 다른 AI 도구용 안내. `CLAUDE.md`를 따르라는 한 줄 |

## 작업 규칙 요약

- `main`은 2인 공용 브랜치다. 작업은 `feature/…` 브랜치에서 하고 PR로 합친다.
- 스크립트를 고친 뒤에는 Unity 새로고침 → 콘솔 에러 0 → EditMode 테스트 통과를 확인한다.
- git에 올리는 모든 파일은 목적이 한눈에 보여야 한다: 폴더마다 `README.md`, 스크립트마다 머리 설명. 자세한 규칙은 `CLAUDE.md` "git에 올리는 파일 규칙".
- 큰 파일(이미지·사운드·폰트 등)은 Git LFS로 관리된다. 처음 한 번 `git lfs install`이 필요하다.
