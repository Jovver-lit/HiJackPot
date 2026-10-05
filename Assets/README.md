# Assets — Unity 에셋 폴더 안내

게임을 이루는 파일은 거의 모두 [`RouletteSystem/`](RouletteSystem/)에 있다. 나머지 폴더는 Unity·패키지 설정이다.

| 경로 | 무엇 | 손대도 되나 |
|---|---|---|
| [`RouletteSystem/`](RouletteSystem/) | 게임 본체: 규칙 코어, 화면, 씬 빌더, 테스트, 아트, 사운드 | 주 작업 폴더 |
| [`Scenes/`](Scenes/) | 기준 씬 `SampleScene.unity`(룰렛 배틀). 빌더가 생성한다 | 빌더로만 재생성 |
| `Settings/` | URP 2D 렌더러·파이프라인 설정, 2D 씬 템플릿(Unity 기본 생성) | 렌더링 설정을 바꿀 때만 |
| `TextMesh Pro/` | TextMesh Pro 패키지 리소스. `Examples & Extras/`는 패키지 예제로 게임에서 쓰지 않는다 | 폰트 설정 외에는 건드리지 않음 |
| `Screenshots/` | UI 검토용 캡처(`ui-review-1920.png`, 이전 화면 기준). Legacy | 참고용 |
| `DefaultVolumeProfile.asset`, `UniversalRenderPipelineGlobalSettings.asset` | URP 전역 설정(Unity 기본 생성) | 렌더링 설정을 바꿀 때만 |
| `InputSystem_Actions.inputactions` | Input System 기본 액션 맵(Unity 기본 생성). 현재 UI는 EventSystem 포인터 입력만 사용 | 입력 체계를 바꿀 때 |

`Temp/` 등 생성 폴더와 `Library/`는 git에 올리지 않는다.
