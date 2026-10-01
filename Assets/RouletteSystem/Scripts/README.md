# Scripts — 화면 쪽 MonoBehaviour

규칙은 계산하지 않는다(그건 `../Core/`). 입력을 받고, 룰렛을 돌리고, 결과를 보여준다.

| 파일 | 무엇 |
|---|---|
| `DealerBattleController.cs` | 전투 화면과 런 진행: 코어(`Run`·`PotBattle`) 호출, 턴 흐름, 문 선택, JACKPOT 배치, 바깥 링 띠, 딜러 정보·텍스트·버튼 갱신, 칸 위 기호 글자·착지 고정 표시·착지 이름표, CASH OUT 칩 연출·큰 순간 배너·룰렛 위 HIJACK 선택, NUDGE 선택, 유물 띠·설명·문 카드 유물, 딜러 반응(움찔), 속도 ×1/×2(PlayerPrefs), 타이틀(계약하기·해금 기록 지우기), 개발용 자동 진행(Debug Autoplay, 기본 꺼짐), 전체 전투 기록 펼치기, 확률판(다음 SPIN 판돈 범위)·양쪽 보험, 템포, 바깥 링 해금 저장(PlayerPrefs) |
| `DealerLines.cs` | 딜러별 반응 대사(시작·하우스 몫·큰 연쇄·CASH OUT·HIJACK·NUDGE·패배)와 층간 안내 방송 문구(데이터) |
| `SlotDescriptions.cs` | 칸 효과를 읽을 문장으로 바꿈(문 카드·HIJACK 선택·룰렛 아래 특수 칸 안내·상세 페이지 공용) |
| `HoldToSpinInput.cs` | SPIN 버튼 길게 누르기 → 강도 게이지 → 놓으면 회전 |
| `BattlePresentationUI.cs` | 단계 표시(준비·회전·결과·딜러)와 룰렛·하우스 룰 강조 |
| `HijackTransferPresenter.cs` | HIJACK 때 칸 토큰이 딜러 룰렛에서 내 룰렛으로 날아가는 연출 |
| `NaturalBlinkAnimator.cs` | 플레이어 캐릭터 눈 깜빡임 스프라이트 애니메이션 |
| `RouletteClickArea.cs` | 룰렛 위 클릭을 칸 순서로 바꿔 알림(HIJACK 때 룰렛에서 직접 칸 고르기) |
| `RouletteController.cs` | 룰렛 칸 목록과 각도 계산, 포인터 아래 칸 판정, 착지 고정 표시(착지·연쇄만 밝게) |
| `RouletteSpinController.cs` | 룰렛 회전·감속·착지(강도 구역 + 오차, 역전 보정 무게 반영), 시드 재현, 칸 경계 틱·멈춤 소리, NUDGE 한 칸 밀기 |
| `RoulettePixelWheelRenderer.cs` | 룰렛을 저해상도 텍스처에 픽셀 아트로 그림(착지 고정 표시 때 나머지 칸 어둡게) |
| `RouletteSegmentGraphic.cs` | 칸 위 아이콘·텍스트 배치 |
| `RouletteSegmentData.cs`, `RouletteSegmentType.cs` | 룰렛 위젯이 쓰는 칸 표시 데이터(전투 규칙의 `Core.Slot`을 화면용으로 변환한 것) |
| `RouletteTest.cs` | 룰렛 위젯 단독 개발용 키보드 테스트(데모 씬 전용, 게임에서는 쓰지 않음) |

룰렛 위젯 자체의 상세 설계는 `../README_RouletteSystem.md`.
