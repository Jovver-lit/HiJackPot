# Scenes — 기준 씬

| 파일 | 무엇 |
|---|---|
| `SampleScene.unity` | **기준 씬**: 딜러 룰렛 배틀과 런(1층 토끼 → 문 선택 → 보스). Play 시작 씬이자 Build Settings 0번(`HijackpotMainSceneLock`이 유지) |

이 씬은 손으로 고치지 않고 `Tools > HIJACKPOT > Build Battle Scene (SampleScene)`(`RouletteSystem/Editor/BattleSceneBuilder.cs`)으로 다시 만든다. 빌더를 실행하면 씬 전체가 새로 생성되어 덮어써지므로, 구조 변경은 빌더 코드에 반영한다.
