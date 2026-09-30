# Tests — 코어 규칙 테스트

`Editor/`에 EditMode 테스트(`HiJackPot.Core.Tests` 어셈블리)가 있다. Unity `Window > General > Test Runner > EditMode`에서 실행한다. 규칙(`../Core/`)을 바꾸면 반드시 함께 고치고 전부 통과시킨다.

| 파일 | 무엇을 검증하나 |
|---|---|
| `Editor/PotBattleTests.cs` | 연쇄 착지, 배율, 하우스 몫, CASH OUT 제로섬, 앤티 한도, 전액 보장 → HIJACK, 하우스 몫 보호, 완전 강탈, 역탈취, 역전 보정, 튜토리얼 대본, 시드 재현 |
| `Editor/RunTests.cs` | 층·문 구성, 칩·룰렛 이어가기와 상금, 패배 시 런 종료, 보스 격파 탈출, JACKPOT 칸 획득 |
| `Editor/DealerRuleTests.cs` | 여우(기본 보험·작은 CASH OUT·허풍), 고양이(배율 CASH OUT), 까마귀(선불 75%·선공 CASH OUT) |
