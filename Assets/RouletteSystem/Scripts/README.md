# Scripts — 화면 쪽 MonoBehaviour

규칙은 계산하지 않는다(그건 `../Core/`). 입력을 받고, 룰렛을 돌리고, 결과를 보여준다.

| 파일 | 무엇 |
|---|---|
| `RabbitBattlePrototype.cs` | 토끼 딜러 전투 화면 진행: 코어(`PotBattle`) 호출, 턴 흐름, 텍스트·버튼 갱신, 템포 |
| `HoldToSpinInput.cs` | SPIN 버튼 길게 누르기 → 강도 게이지 → 놓으면 회전 |
| `BattlePresentationUI.cs` | 단계 표시(준비·회전·결과·딜러)와 룰렛·하우스 룰 강조 |
| `HijackTransferPresenter.cs` | HIJACK 때 칸 토큰이 딜러 룰렛에서 내 룰렛으로 날아가는 연출 |
| `NaturalBlinkAnimator.cs` | 플레이어 캐릭터 눈 깜빡임 스프라이트 애니메이션 |
| `RouletteController.cs` | 룰렛 칸 목록과 각도 계산, 포인터 아래 칸 판정 |
| `RouletteSpinController.cs` | 룰렛 회전·감속·착지(강도 구역 + 오차, 역전 보정 무게 반영), 시드 재현 |
| `RoulettePixelWheelRenderer.cs` | 룰렛을 저해상도 텍스처에 픽셀 아트로 그림 |
| `RouletteSegmentGraphic.cs` | 칸 위 아이콘·텍스트 배치 |
| `RouletteSegmentData.cs`, `RouletteSegmentType.cs` | 룰렛 위젯이 쓰는 칸 표시 데이터(전투 규칙의 `Core.Slot`을 화면용으로 변환한 것) |
| `RouletteTest.cs` | 룰렛 위젯 단독 개발용 키보드 테스트(데모 씬 전용, 게임에서는 쓰지 않음) |

룰렛 위젯 자체의 상세 설계는 `../README_RouletteSystem.md`.
