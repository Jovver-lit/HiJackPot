# Core — 판돈 전투·런 규칙 코어

게임 규칙만 담은 순수 C# 어셈블리(`HiJackPot.Core`, `noEngineReferences`). 화면·연출을 모르며, 같은 시드면 같은 결과를 낸다. 규칙을 바꿀 때는 여기와 `../Tests/`만 고치면 된다.

| 파일 | 무엇 |
|---|---|
| `Slot.cs` | 룰렛 한 칸(`Slot`)과 칸 종류(레이즈·배율·보험·배당·하우스 몫·봉인), 발동 키워드 |
| `Seat.cs` | 테이블의 한 자리(플레이어/딜러): 칩·앤티·판돈·보험·룰렛 |
| `DealerProfile.cs` | 딜러 공개 정보: 시작 칩, 테이블 한도, 성향(CASH OUT 기준), 하우스 룰, 예고·역탈취 여부, 튜토리얼 대본 |
| `PotBattle.cs` | 한 전투의 규칙: 선공 코인플립 → 앤티 → 착지 연쇄 → CASH OUT/하우스 몫, HIJACK, 역탈취, 역전 보정, 승패 |
| `Run.cs` | 한 런: 1층 토끼 → 문 선택 → 보스, 칩·룰렛 이어가기, 상금, JACKPOT 칸 획득 |
| `BattlePresets.cs` | 시작 룰렛 8칸과 토끼 딜러 수치(전부 임시값) |
| `HiJackPot.Core.asmdef` | 어셈블리 정의. `Assembly-CSharp`가 자동 참조한다 |

수치 변경 근거와 시뮬레이션 결과는 `Docs/playtest/`에 남긴다.
