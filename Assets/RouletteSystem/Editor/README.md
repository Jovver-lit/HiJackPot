# Editor — 에디터 전용 도구

빌드에 포함되지 않는다.

| 파일 | 메뉴 | 무엇 |
|---|---|---|
| `BattleSceneBuilder.cs` | `Tools > HIJACKPOT > Build Battle Scene (SampleScene)` | 기준 씬 `Assets/Scenes/SampleScene.unity`를 코드로 생성. 배치: 상단 무대(20%) / 내 룰렛 · 가운데 테이블(칸 기호 범례·판돈 칩 더미·CASH OUT) · 딜러 룰렛(각각 포인터 위 착지 이름표, 아래 바깥 링 띠) / 하단 사건 띠(오른쪽 끝 속도 버튼), 타이틀 화면·유물 띠(칩 바 아래)·유물/전투 기록 펼침 패널·NUDGE 선택 띠·룰렛 위 클릭 영역·HIJACK 안내 띠·연출 층(FxLayer)·큰 순간 배너, 상점층 화면(서비스·슬롯머신·내 칸 고르기), 그리고 문 선택(환전 창구 포함)·JACKPOT 배치(HIJACK 패널)·종료·하우스 룰 상세 패널. 씬 구조 변경은 여기서 한다 |
| `RunBalanceSimulator.cs` | `Tools > HIJACKPOT > Simulate Runs (Balance Report)` | 코어 규칙만으로 수천 런을 돌려 탈출률·층별 탈락·전투 길이·HIJACK·NUDGE·유물별 탈출률을 콘솔에 보고(휴리스틱 정책) |
| `HijackpotMainSceneLock.cs` | `Tools > HIJACKPOT > Open Main Scene (SampleScene)` | Play 시작 씬과 Build Settings 0번을 기준 씬으로 고정 |
| `RouletteDemoSceneBuilder.cs` | `Tools > HIJACKPOT > Create Roulette Demo Scene` / `Validate Roulette Pixel Demo` | 룰렛 위젯 단독 데모 씬·프리팹 생성(Legacy, 위젯 개발용) |
