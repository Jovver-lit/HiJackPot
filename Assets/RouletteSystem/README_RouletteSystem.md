# 동적 룰렛 전투 시스템

## 바로 실행하기

`Assets/RouletteSystem/Scenes/RouletteDemo.unity`를 열고 Play를 누른다. 기존 Scene에서는 `Assets/RouletteSystem/Prefabs/RouletteDemo.prefab`을 Canvas 아래에 배치해 재사용할 수 있다. 구조를 다시 만들려면 Unity 메뉴 `Tools > HIJACKPOT > Create Roulette Demo Scene`을 실행한다.

데모에는 `Art/roulette_ui_sheet.png`의 `roulette_frame`, `pointer`, `center_cap` Sprite가 이미 연결되어 있다. 원본 Aseprite 파일은 `Art/Source`에 보관한다. 프레임은 원본의 `frame`, `yellow jewel`, `red jewel` 레이어를 모두 합성한 버전이다. 프레임과 포인터는 고정되고 `Wheel`과 런타임 세그먼트만 회전하며, CenterCap은 Wheel의 자식이어도 역회전 보정으로 화면상 고정된다.

## 1. 전체 구조 설명

이 시스템은 룰렛을 `데이터`, `동적 부채꼴 UI`, `회전`, `개발 테스트`로 분리한다.

- `RouletteSegmentData`는 한 칸의 타입, 값, weight, 색, 아이콘, 표시 문자열과 상태를 보관한다.
- `RouletteController`는 리스트를 소유하고 weight 합으로 각도를 계산한다. 칸 추가·삽입·삭제·크기 변경·인접 합체와 포인터 판정을 담당한다.
- `RouletteSegmentGraphic`은 Unity UI `Graphic`의 메시를 런타임에 만들어 각 칸을 부채꼴로 표시한다. 외곽/방사형 경계선, 아이콘, 텍스트, 비활성 색, 선택 강조를 포함한다.
- `RouletteSpinController`는 Wheel만 시계 방향으로 회전시키고 빠른 유지 구간 뒤 자연 감속한다. 정지 후 Controller의 각도 판정을 호출하고 C# 이벤트와 UnityEvent를 모두 발생시킨다.
- `RouletteTest`는 기본 8칸과 Space/A/R/W/M 개발 입력을 제공한다.

각도 규칙은 시스템 전체에서 `12시 = 0도`, `시계 방향 증가`, `0 이상 360 미만으로 Normalize`를 사용한다. 정지 결과는 `시작 각도 포함 / 끝 각도 미포함` 규칙으로 경계를 결정론적으로 처리한다.

`weight`는 확률과 시각적 크기를 동시에 결정한다. 예를 들어 `[1, 1, 2, 1]`은 정확히 `[72°, 72°, 144°, 72°]`가 된다. 개수와 각도에 8칸 전용 하드코딩은 없다.

## 2. Hierarchy 구성

Canvas 아래에 다음 구조를 만든다. 이름을 그대로 사용하면 컴포넌트가 참조를 자동 탐색할 수 있지만, Inspector에 명시적으로 드래그하는 편이 안전하다.

```text
Canvas
└── RouletteRoot                       RectTransform (데모: 620 x 720)
    ├── Wheel                         RectTransform (데모: 420 x 420, Pivot 0.5/0.5)
    │   ├── DynamicSegments           빈 RectTransform, Wheel에 Stretch
    │   └── CenterCap                 Image, 외부 픽셀아트 Sprite
    ├── FixedFrame                    Image, 외부 픽셀아트 Sprite
    ├── Pointer                       Image, 12시 방향
    ├── HighlightFX                   선택 연출 확장용 빈 Image/RectTransform
    └── SpinButton                    Button
```

Unity Transform 규칙상 Wheel의 자식인 CenterCap도 원래 함께 회전한다. 제공한 `RouletteSpinController`는 CenterCap의 월드 회전을 매 프레임 보정하므로 위 계층을 유지하면서 화면상 고정한다. CenterCap을 Wheel의 형제 오브젝트로 옮긴다면 `Keep Center Cap Visually Fixed`를 꺼도 된다.

`DynamicSegments`에는 수동으로 칸을 만들지 않는다. 실행 시 `Segment_00_Damage` 같은 오브젝트와 그 하위 `Icon`, `Label`이 자동 생성된다.

데모의 UI 렌더 순서는 넓어진 Wheel이 금속 테두리 아래까지 채워지도록 `Wheel -> FixedFrame -> Pointer` 순서다. Hierarchy 순서와 관계없이 회전 컴포넌트는 Wheel에만 적용된다.

## 3. 각 Script 코드 전체

완성된 전체 코드는 다음 파일에 있다.

- `Scripts/RouletteSegmentType.cs`: 기본 결과 enum
- `Scripts/RouletteSegmentData.cs`: 직렬화 가능한 런타임 칸 데이터와 복제/로그 이름
- `Scripts/RouletteSegmentGraphic.cs`: VertexHelper 기반 부채꼴, 픽셀 스냅, 경계선, 아이콘/텍스트, 강조
- `Scripts/RouletteController.cs`: weight 각도 계산, 생성/재생성, CRUD, 합체 규칙 delegate, 선택 판정
- `Scripts/RouletteSpinController.cs`: 빠른 회전 유지/감속/정지, 최소 회전 수, 재현 가능한 RNG, 결과 이벤트
- `Scripts/RouletteTest.cs`: 기본 8칸 및 키보드 테스트

주요 런타임 API는 다음과 같다.

```csharp
rouletteController.AddSegment(newSegment);
rouletteController.InsertSegment(index, newSegment);
rouletteController.RemoveSegment(id);
rouletteController.RemoveSegment(index);
rouletteController.ChangeSegmentWeight(id, newWeight);
rouletteController.MergeSegments(indexA, indexB);
rouletteController.MergeSegments(indexA, indexB, customMergeRule, out merged);
rouletteController.TryGetSegmentAngles(index, out start, out end, out center, out size);

RouletteSegmentData selected = rouletteController.GetSelectedSegment();
spinController.Spin();
spinController.RouletteFinished += ResolveCombatResult;
spinController.SetRandomSeed(runSeed);
```

외부 합체 규칙은 아래처럼 주입한다. Controller는 어떤 규칙을 쓰더라도 최종 weight를 두 원본의 합으로 다시 보장한다.

```csharp
private RouletteSegmentData MergeForBattle(RouletteSegmentData a, RouletteSegmentData b)
{
    if (a.type == RouletteSegmentType.Damage &&
        b.type == RouletteSegmentType.Multiplier)
    {
        return new RouletteSegmentData(
            System.Guid.NewGuid().ToString("N"),
            RouletteSegmentType.Custom,
            a.value * b.value,
            a.weight + b.weight,
            Color.magenta,
            null,
            "BURST",
            true,
            false);
    }

    return RouletteController.CreateDefaultMergedSegment(a, b);
}
```

## 4. Unity Editor에서 세팅하는 방법

1. 이 `RouletteSystem` 폴더를 Unity 프로젝트의 `Assets` 아래에 둔다. Unity가 `.meta` 파일을 자동 생성하게 한다.
2. Scene에 Canvas와 위 Hierarchy를 만든다. Canvas의 `Pixel Perfect`를 켜고, `DynamicSegments`의 크기와 Pivot을 Wheel과 동일하게 맞춘다.
3. `RouletteRoot`에 `RouletteController`, `RouletteSpinController`, 개발 중에는 `RouletteTest`를 추가한다.
4. Controller에 `Wheel`과 `DynamicSegments`를 연결한다. `Radius Override=0`이면 DynamicSegments의 짧은 변 절반을 자동 반지름으로 사용한다.
5. Spin Controller에 Controller, Wheel, SpinButton, CenterCap을 연결한다. 포인터가 12시면 `Pointer Angle=0`이다.
6. 제공 데모는 `Art/roulette_ui_sheet.png`를 Multiple Sprite로 자동 슬라이스해 Frame, Pointer, CenterCap에 연결한다. 다른 아트로 교체할 때는 시트 크기와 세 Sprite 영역을 함께 바꾸거나 Builder의 슬라이스 좌표를 갱신한다. 아이콘은 각 `RouletteSegmentData.icon`에 넣는다.
7. Builder는 시트에 `Filter Mode=Point`, `Compression=Uncompressed`, `Mip Maps=Off`를 적용한다. Canvas Scaler의 Reference Resolution과 배율은 정수 배율이 나오게 잡는다. 프로젝트가 픽셀 경계를 원한다면 Quality의 불필요한 Anti Aliasing도 끈다.
8. 픽셀 폰트를 사용할 경우 Controller의 `Label Font`에 직접 할당한다. 비워 두면 Unity 기본 런타임 폰트를 사용한다.

Inspector 핵심 회전 값:

- `Min/Max Spin Duration`: 전체 회전 시간 범위
- `Start Speed`: 빠른 유지 구간의 기준 각속도
- `Deceleration`: 감속 구간 길이를 결정하는 기준 감속도
- `Random Extra Rotation`: 기본 이동 거리에 더하는 무작위 각도
- `Minimum Full Rotations`: 설정값이 작아도 보장할 최소 바퀴 수
- `Use Deterministic Random`, `Random Seed`: 동일 시드/동일 호출 순서의 정지 결과 재현

전투 결과 실행 코드는 UI 컴포넌트와 분리한다.

```csharp
private void OnEnable()
{
    spinController.RouletteFinished += ApplyResult;
}

private void OnDisable()
{
    spinController.RouletteFinished -= ApplyResult;
}

private void ApplyResult(RouletteSegmentData result)
{
    if (result.isDisabled)
        return;

    switch (result.type)
    {
        case RouletteSegmentType.Damage:
            enemy.TakeDamage(result.value);
            break;
        case RouletteSegmentType.Heal:
            player.Heal(result.value);
            break;
    }
}
```

위 예시의 `enemy`와 `player`는 프로젝트 전투 클래스에 맞춰 연결한다. 제공 코드에는 특정 전투 프레임워크 의존성이 없다.

## 5. 테스트 방법

1. RouletteRoot에 `RouletteTest`를 붙이고 `Initialize Default Wheel When Empty`를 켠다.
2. Play Mode에 들어가면 비어 있는 Controller에 다음 8칸이 생성된다.
   - Damage 5, Damage 10, Damage 15, Heal 8, Critical, Poison 5, Multiplier x2, Mystery
3. Game View에 포커스를 두고 키를 누른다.
   - `Space`: Spin
   - `A`: 임의 타입/값/weight 칸 추가
   - `R`: 임의 칸 삭제. 최소 2칸은 보존
   - `W`: 임의 칸 weight를 1.5배 증가
   - `M`: 임의의 인접 두 칸 합체. 첫 칸/마지막 칸도 인접 처리
4. Console에서 `Spin Result: Damage 10`, `Added Segment: Heal 8`, `Merged: Damage 5 + Damage 10 -> Damage 15` 형식의 로그를 확인한다.
5. `Use Deterministic Random`을 켠 채 같은 `Random Seed`로 Play를 다시 시작하면 같은 Spin 호출 순서에서 같은 회전 거리와 결과를 재현할 수 있다.

`RouletteTest`는 컴파일 심볼에 따라 새 Input System과 레거시 Input Manager를 자동 선택한다. 본 시스템의 런타임 API는 특정 입력 패키지에 의존하지 않는다.

## 6. 나중에 확장할 수 있는 부분

- 추가/삭제/weight 변경/합체 전후 이벤트와 `TryGetSegmentGraphic`을 이용해 Coroutine 애니메이터를 별도 컴포넌트로 붙일 수 있다.
- `SegmentMergeRule`을 ScriptableObject 조합표로 교체하면 Damage+Multiplier 같은 데이터 기반 조합을 만들 수 있다.
- `RouletteFinished`를 전투 이벤트 큐의 `Land`에 연결하고, `AdjacentTrigger`, `Damage`, `DopamineChange` 순서로 해결하면 계산과 연출을 분리할 수 있다.
- `HighlightFX`에는 Jackpot 플래시, Hijack 오염 셰이더, 선택 글로우를 연결할 수 있다. 기본 구현은 선택 세그먼트 메시만 깜빡인다.
- 적 룰렛도 같은 Controller/Graphic/Spin 조합을 사용하고 데이터 프리셋, 포인터 각도, 결과 처리기만 별도로 둔다.
- 정밀 타이밍 STOP을 추가할 때는 Spin Controller에 입력 요청 시점과 실제 판정 시점을 기록하고, 동일한 감속/입력 지연 규칙을 모든 빌드에 적용한다.
- 현재 `isDisabled` 칸도 물리적 칸으로 남고 결과 데이터가 반환된다. 무효, 재회전, 봉인 피해 중 어떤 규칙인지 전투 결과 처리기가 결정하도록 의도적으로 분리했다.
