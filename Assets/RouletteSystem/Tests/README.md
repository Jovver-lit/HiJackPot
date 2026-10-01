# Tests — 코어 규칙 테스트

`Editor/`에 EditMode 테스트(`HiJackPot.Core.Tests` 어셈블리)가 있다. Unity `Window > General > Test Runner > EditMode`에서 실행한다. 규칙(`../Core/`)을 바꾸면 반드시 함께 고치고 전부 통과시킨다.

| 파일 | 무엇을 검증하나 |
|---|---|
| `Editor/PotBattleTests.cs` | 연쇄 착지, 배율, 하우스 몫, CASH OUT 제로섬, 앤티 한도, 전액 보장 → HIJACK, 하우스 몫 보호, 완전 강탈, 역탈취, 역전 보정, 튜토리얼 대본, 시드 재현 |
| `Editor/RunTests.cs` | 층·문 구성, 칩 이어가기(입장 칩까지 + 상금, ADR 0007)·룰렛 이어가기, 패배 시 런 종료, 보스 격파 탈출, JACKPOT 칸 획득 |
| `Editor/DealerRuleTests.cs` | 여우(기본 보험·작은 CASH OUT·허풍), 고양이(배율 CASH OUT), 까마귀(선불 75%·선공 CASH OUT) |
| `Editor/EconomyTests.cs` | 테이블 마감 사용료(10·15라운드, 사용료 파산), 층별 딜러 칩·앤티 강화 |
| `Editor/RelicTests.cs` | 유물 12개 효과(키워드 6종 포함), NUDGE 횟수·사용 조건, 문 카드 유물 상금과 VIP 상금 |
| `Editor/OuterRingTests.cs` | 바깥 링(레이즈·배율·보험·보호막), 잭팟 라인과 보스 하우스 룰, 보호막 만료, 보스 격파 시 바깥 링 해금 |
