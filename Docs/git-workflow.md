# Git 워크플로우 — 팀 약속

HIJACKPOT 2인 팀이 GitHub(`Jovver-lit/HiJackPot`)에서 함께 작업할 때 지키는 약속이다. 사람과 Claude Code 모두 이 절차를 따른다. 인원이 적을 때 브랜치 전략이 복잡하면 오히려 방해가 되므로 `main` + `feature/*` 두 종류만 쓴다.

## 브랜치

| 브랜치 | 규칙 |
|---|---|
| `main` | 항상 실행 가능한 상태만 유지한다. **직접 커밋하지 않는다.** PR의 Squash and merge로만 바뀐다. |
| `feature/기능명` | 작업 하나당 브랜치 하나. 이름은 영어 케밥 케이스. 예: `feature/roulette-core`, `feature/dealer-ai`, `feature/dopamine-gauge` |

## 작업 절차

1. **시작 전**: `main`을 최신으로 받는다.
   ```bash
   git checkout main
   git pull
   ```
2. **브랜치 만들기**: `git checkout -b feature/기능명`
3. **작업 중**: 의미 단위로 자주 커밋한다. 하루가 끝나면 최소 한 번은 push해서 백업한다.
4. **작업이 끝나면**: GitHub에서 Pull Request를 만든다(`feature/기능명` → `main`).
5. **리뷰**: 정식 승인 절차 대신 상대방이 diff를 훑어보고 씬·에셋 충돌이 없는지만 확인한다. 문제없으면 바로 merge한다.
6. **머지 방식**: **Squash and merge**. 기능 하나가 `main`에 커밋 하나로 남는다.
7. **머지 후 정리**:
   ```bash
   git checkout main
   git pull
   git branch -d feature/기능명
   ```

## 충돌 방지

- 같은 씬·프리팹을 두 명이 동시에 건드리지 않는다. 브랜치를 만들기 전에 "나 이 씬 작업한다"고 서로 확인한다.
  - 이 프로젝트의 기준 씬 `Assets/Scenes/SampleScene.unity`는 `BattleSceneBuilder.cs`가 코드로 다시 만든다. 화면 구조를 바꿀 때는 씬이 아니라 빌더 코드를 고치고, 빌더를 고치는 사람도 미리 알린다.
- 씬(`.unity`)·프리팹(`.prefab`) 충돌은 텍스트로 직접 병합하지 않는다. Unity의 Smart Merge(UnityYAMLMerge)를 쓰고, 그래도 안 되면 한 명의 작업을 버리고 다시 적용하는 편이 빠르다. 기준 씬이라면 빌더를 다시 실행해 새로 만든다.
- `main`이 바뀌었으면 내 feature 브랜치에 자주 반영한다(`git merge main` 또는 `git rebase main`). 마지막에 몰아서 충돌이 나지 않게 한다.

## 커밋 메시지

접두어만 지키고 나머지는 자유롭게 쓴다. 어차피 Squash merge라 `main` 히스토리는 PR 단위로 정리된다.

| 접두어 | 쓰는 곳 | 예 |
|---|---|---|
| `feat:` | 기능 추가·변경 | `feat: 룰렛 스핀 로직 추가` |
| `fix:` | 버그 수정 | `fix: 딜러 봉인 패턴 조건 오류 수정` |
| `docs:` | 문서 | `docs: 전투 규칙 문서 업데이트` |
| `chore:` | 정리·설정·에셋 이동 등 | `chore: 에셋 정리` |

접두어 뒤는 한국어 한 줄 요약. 무엇을 왜 바꿨는지 필요하면 본문에 적는다.

## 이 구조로 부족해지면 (지금은 불필요)

팀원이 늘거나 릴리스 주기가 생기면 그때 `dev` 통합 브랜치를 고려한다. 2인 규모에서는 오버엔지니어링이다.

## 덧붙임

- 큰 파일(이미지·사운드·폰트·PDF)은 Git LFS로 관리된다. 처음 한 번 `git lfs install`.
- git에 올리는 모든 파일은 목적이 한눈에 보여야 한다(폴더마다 README, 스크립트마다 머리 설명). 자세한 규칙은 `CLAUDE.md` "git에 올리는 파일 규칙".
- 이 약속은 2026-10-01에 정했다. 그 전 작업은 `feature/pot-battle` 한 브랜치에 한국어 커밋으로 쌓였고(PR #1), 이 PR의 Squash merge부터 새 규칙을 적용한다.
