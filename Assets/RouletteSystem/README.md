# RouletteSystem — 게임 본체

규칙(무엇이 일어나는가)과 화면(어떻게 보이는가)을 나눠 둔다. 규칙은 `Core/`의 순수 C#이 계산하고, `Scripts/`는 입력·룰렛 회전·표시만 맡는다.

| 폴더 | 무엇 |
|---|---|
| [`Core/`](Core/) | 판돈 전투·런 규칙 코어(`HiJackPot.Core` 어셈블리). Unity 의존 없음, 같은 시드면 같은 결과 |
| [`Scripts/`](Scripts/) | 화면 쪽 MonoBehaviour: 룰렛 렌더링·회전, 전투 화면 진행, HIJACK 연출 |
| [`Editor/`](Editor/) | 씬 빌더와 기준 씬 고정 등 에디터 전용 도구 |
| [`Tests/`](Tests/) | 코어 규칙 EditMode 테스트 |
| [`Art/`](Art/) | 룰렛 UI 스프라이트, 캐릭터, 폰트(원본 Aseprite 포함) |
| [`Music/`](Music/) | 룰렛 회전 효과음 |
| [`Scenes/`](Scenes/), [`Prefabs/`](Prefabs/) | 룰렛 단독 데모와 이전 사본(Legacy) |
| `README_RouletteSystem.md` | 룰렛 위젯(데이터·픽셀 렌더링·회전) 자체의 상세 설계와 Hierarchy 구성 설명 |

## 데이터 흐름 (전투 한 번)

```
HoldToSpinInput ──SPIN──▶ DealerBattleController ──PlaceAnte/Land/CashOut/Hijack──▶ Core.PotBattle
        ▲                        │  ▲                                                     │
        │                        ▼  │ 착지 칸                                              ▼ 칩·판돈·보험·로그
   플레이어 입력        RouletteSpinController (회전·착지)                         텍스트/룰렛 표시 갱신
```

용어는 저장소 루트의 `CONTEXT.md`를 따른다(HP 대신 칩, 방어 대신 보험 등).
