# Editor — 에디터 전용 도구

빌드에 포함되지 않는다.

| 파일 | 메뉴 | 무엇 |
|---|---|---|
| `BattleSceneBuilder.cs` | `Tools > HIJACKPOT > Build Battle Scene (SampleScene)` | 기준 씬 `Assets/Scenes/SampleScene.unity`를 코드로 생성. 배치: 상단 무대(20%) / 내 룰렛 · 가운데 테이블(칸 기호 범례·판돈 칩 더미·CASH OUT) · 딜러 룰렛(각각 포인터 위 착지 이름표, 아래 바깥 링 띠) / 하단 사건 띠, 그리고 문 선택·HIJACK·종료·하우스 룰 상세 패널. 씬 구조 변경은 여기서 한다 |
| `HijackpotMainSceneLock.cs` | `Tools > HIJACKPOT > Open Main Scene (SampleScene)` | Play 시작 씬과 Build Settings 0번을 기준 씬으로 고정 |
| `RouletteDemoSceneBuilder.cs` | `Tools > HIJACKPOT > Create Roulette Demo Scene` / `Validate Roulette Pixel Demo` | 룰렛 위젯 단독 데모 씬·프리팹 생성(Legacy, 위젯 개발용) |
