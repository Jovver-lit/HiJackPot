---
name: hijackpot-unity-work
description: Use when changing HIJACKPOT's Unity project (scripts, scenes, prefabs, assets, build settings) through Unity MCP, and when verifying such changes.
---

# HIJACKPOT Unity 작업 절차

프로젝트 지침 원본은 저장소 루트 `CLAUDE.md`다. 먼저 따른다.

## 0. 환경
- Unity 6000.4.0f1, URP, Input System. 에디터 조작은 MCP for Unity(`unityMCP` 서버)의 도구를 쓴다.
- 핵심 코드: `Assets/RouletteSystem/Scripts`, 씬 빌더: `Assets/RouletteSystem/Editor`, 시스템 설명: `Assets/RouletteSystem/README_RouletteSystem.md`
- 기준 씬: `Assets/Scenes/SampleScene.unity` (룰렛 배틀 데모). 모든 화면 변경은 이 씬에서 한다.

## 1. 시작 점검
1. `manage_scene(action=get_active)`로 연결을 확인한다. "No Unity Editor instances found"면 멈추고 사용자에게 요청한다: 에디터에서 HiJackPot 열기, MCP for Unity 창(⇧⌘M) 세션 켜기.
2. `read_console(types=[error,warning])`로 작업 전 기존 에러를 기록한다. 기존 에러와 내가 만든 에러를 구분하기 위해서다.
3. `git status`로 미커밋 변경을 확인한다. 사용자가 작업 중인 파일을 덮어쓰지 않는다.

## 2. 수정 규칙
- 씬은 빌더가 생성한다(`Tools/HIJACKPOT/Build Battle Scene (SampleScene)` → SampleScene 저장). 빌더를 다시 실행하면 씬이 새로 만들어져 덮어써지므로, 구조 변경은 빌더 코드(`BattleSceneBuilder.cs`)에 반영하고 `execute_menu_item`으로 다시 빌드한다. 씬만 직접 고쳤다면 보고에 명시한다.
- 빌더에서 UI 오브젝트를 없앨 때는 `DealerBattleController`/`BattlePresentationUI`가 null 체크 없이 참조하는 필드가 있는지 먼저 확인한다. 숨길 오브젝트는 만들고 `SetActive(false)`로 둔다.
- 전투 계산과 연출을 분리한다. 룰렛 칸·유물·적 패턴·보상 수치는 데이터로 둔다. 동일 시드 재현(`battleSeed`)을 깨지 않는다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 건드리지 않는다.

## 3. 검증 (완료 보고 전 필수)
1. `refresh_unity(mode=force, compile=request)`로 재컴파일을 기다린다.
2. `read_console`에서 새 에러가 0인지 확인한다. 에러가 있으면 고치고 1로 돌아간다.
3. 동작 확인이 필요하면 `manage_editor(play)` → `manage_camera(screenshot, include_image=true, output_folder=Temp/ClaudeCaptures)` → `read_console` → `manage_editor(stop)`. Play 모드를 켜둔 채로 끝내지 않는다.
   - 입력은 `execute_code`로 흉내 낼 수 있다. 예: `FindFirstObjectByType<RouletteLike.Roulette.HoldToSpinInput>()`의 `OnPointerDown`/`OnPointerUp` 호출.
4. 씬을 바꿨다면 `manage_scene(action=save)`.

## 4. git
- `Docs/git-workflow.md`(팀 약속)를 따른다: `main`에서 `feature/기능명`(영어 케밥 케이스) 브랜치를 만들어 작업하고, `main`에는 직접 커밋·푸시하지 않는다.
- 의미 단위로 자주 커밋한다. 메시지는 `feat:`/`fix:`/`docs:`/`chore:` 접두어 + 한국어 한 줄 요약 + 필요 시 본문.
- 작업이 끝나면 `feature/…` → `main` PR을 만든다. merge(Squash and merge)는 사용자·팀원 확인 뒤에만 한다.
- 씬·프리팹을 건드리는 작업은 시작 전에 사용자에게 알린다(동시 수정 금지).

## 5. 보고
- 바꾼 파일, 검증 결과(콘솔 에러 수, Play 확인 여부), 사용자가 직접 손으로 확인할 것을 짧게 적는다.
