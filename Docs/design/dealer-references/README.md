# 딜러 4명 — Claude 재작화용 레퍼런스

여우·고양이·까마귀·매니저의 기존 디자인을 Claude가 바로 열어 보고 128px 도트로 다듬을 수 있도록 모은 자료다. 사용자가 2026-11-02 채팅에서 마지막으로 확인한 네 딜러 PNG를 변경 없이 보존했다. 이 폴더는 디자인 참고 자료이며 게임에 연결한 완성 스프라이트가 아니다.

## 먼저 볼 파일

![딜러 4명, 네이티브 PNG의 정확한 2배 확대](dealer-lineup-2x.png)

| 딜러 | 128px 투명 원본 | 정확한 5배 확대 | 고해상도 디자인 원본 |
|---|---|---|---|
| 여우 | [fox-reference-128.png](fox-reference-128.png) | [fox-reference-5x.png](fox-reference-5x.png) | [fox-design-master.png](fox-design-master.png) |
| 고양이 | [cat-reference-128.png](cat-reference-128.png) | [cat-reference-5x.png](cat-reference-5x.png) | [cat-design-master.png](cat-design-master.png) |
| 까마귀 | [crow-reference-128.png](crow-reference-128.png) | [crow-reference-5x.png](crow-reference-5x.png) | [crow-design-master.png](crow-design-master.png) |
| 매니저 | [manager-reference-128.png](manager-reference-128.png) | [manager-reference-5x.png](manager-reference-5x.png) | [manager-design-master.png](manager-design-master.png) |

`*-reference-128.png`는 모두 실제 128×128px이며 투명도는 0/255다. `*-reference-5x.png`는 최종 한 도트가 정확히 단색 5×5px가 되는 640×640px PNG다. 캡처나 JPG가 아니다.
`*-design-master.png`는 얼굴·머리·의상 형태를 확인하는 생성 원본이다. 원본 크기를 그대로 보존했으며, 128px 파일 또는 정확한 도트 격자라고 취급하지 않는다. 특히 여우는 스타킹 보정을 반영한 마스터다.

## Claude가 작업할 순서

1. [ArtStyleBible.md](../ArtStyleBible.md)와 [토끼 Gold Standard](../../../Assets/RouletteSystem/Art/Characters/Rabbit/bunny-dealer-128px.png)를 먼저 연다. [토끼 레퍼런스](../img/2026-11-02-rabbit-reference.png)도 함께 본다.
2. 위 표에서 해당 딜러의 128px 원본과 고해상도 디자인 원본을 열어 얼굴·자세·의상·실루엣을 확인한다. 이미 존재하는 캐릭터를 새 디자인으로 바꾸지 않는다.
3. 128×128px 투명 캔버스에서 토끼와 도트 밀도, 1px 윤곽, 재질별 명암 단계와 광원을 맞춰 정리한다. PNG 저장 방향은 바이블대로 오른쪽을 보는 3/4 시점으로 맞추고, 대치 방향 반전은 씬에서 한다. 여기의 레퍼런스 자체는 덮어쓰지 않는다.
4. 얼굴은 자연스러운 시선, 읽히는 작은 입, 과장되지 않은 코와 이어지는 볼·턱을 함께 확인한다. 눈동자만 가장자리에 몰거나, 코만 한 칸씩 깎아 얼굴 전체를 반복 계단 모양으로 만들지 않는다. 캐릭터 고유 비율은 유지한다.
5. 현재 바이블의 공용 32색에 재질별로 맞춘다. 전역 최근접색 자동 변환으로 눈·피부·금속 색을 뭉개지 않는다. 아래 팔레트 차이를 먼저 읽는다.
6. 작업용 `.aseprite`와 128px PNG는 저장소의 `Assets/RouletteSystem/Art/Characters/<Dealer>/`에 기존 캐릭터 구조에 맞춰 만든다. 숨긴 원본 레이어를 보존하고, 승인 전에는 토끼 Gold Standard를 교체하지 않는다.
7. 네이티브 크기, 정확한 정수 배율 확대, 토끼와의 나란한 비교에서 검수한다. 눈 깜빡임을 만들 때는 승인된 정지 프레임을 기준으로 눈만 움직이고 머리·얼굴·자세가 바뀌지 않게 한다. 씬 적용은 별도 작업으로 검증한다.

## 딜러별 유지할 디자인

| 딜러 | 유지할 특징 |
|---|---|
| 여우 | 성인 여성, 사람 얼굴, 주황색 긴 머리·여우 귀·크림색 끝의 큰 꼬리, 호박색 눈. 붉은 조끼와 단추, 흰 칼라·커프스, 검은 짧은 치마, 갈색 스타킹, 검정·빨강 구두. 한 손을 허리에 둔 자세. 스타킹은 찢어진 피부색 틈 없이 연속된 재질로 표현한다. |
| 고양이 | 성인 여성, 검은 단발·고양이 귀·말린 꼬리, 금색 눈. 복원된 검은 의상, 흰 칼라·커프스, 초록 나비넥타이와 허리 장식, 갈색 스타킹, 검정·초록 구두, 다리를 교차한 자세. 다른 제복 디자인으로 바꾸지 않는다. |
| 까마귀 | 성인 남성, 사람 얼굴과 검은 머리. 짙은 깃털 어깨 외투, 흰 셔츠·버건디 넥타이, 어두운 바지·구두, 내린 손의 갈색 장부. 주요 깃털·옷·바지 면이 분리되어 읽히게 한다. 새 부리나 동물 귀를 붙이지 않는다. |
| 매니저 | 중년 남성, 은발과 작은 회색 콧수염, 넓은 어깨. 초록 더블브레스트 재킷·검은 라펠, 흰 셔츠·검은 나비넥타이, 황동 단추·포켓 체인, 검은 바지·구두. 뒷짐을 진 곧은 자세. |

네 명 모두 기존 의상과 노출 범위를 유지한다. 외형 자료이며 전투 규칙을 새로 확정하는 문서는 아니다. 기획은 [expansion-roster.md](../expansion-roster.md)를 함께 확인한다.

## 팔레트와 승인 상태

여우는 사용자가 더 선호한 원본 색감의 네이티브 128px 시안이다. 다른 여우 변환본으로 바꾸지 않았다. 이 여우의 시안 팔레트는 바이블과 다르며, 새 공용 팔레트가 확정됐다는 뜻이 아니다.
고양이·까마귀도 이전 시안 팔레트의 황동색 `#b0702a`가 남아 있다. 매니저는 현재 공용 팔레트 안에 있다. 정확한 색 차이는 [manifest.json](manifest.json)의 `colors_outside_current_style_bible`에 기록했다. 재작화 시 바이블 안의 재질별 색으로 정리하되 이 레퍼런스 원본을 다시 칠하지 않는다.

이 자료를 저장해 달라는 요청은 네 딜러를 새 Gold Standard나 게임 적용 완료본으로 승인한 것이 아니다. 토끼는 계속 기준 그림이며, 네 딜러는 재작화·시각 검토 대상이다.

## 파일과 출처

- `dealer-lineup-2x.png`: 위 네 네이티브 PNG를 정확히 2배로 모은 비교 이미지.
- `manifest.json`: 딜러별 파일, 원본 SHA-256, 실제 크기, 알파, 색 수, 팔레트 차이, 생성·수정 출처.
- `verification.json`: 원본 복사 일치와 128px·이진 알파·5배 확대 검증.
- `generation-prompts-original.md`: 기존 생성 당시의 네 딜러 프롬프트. 과거 팔레트가 포함된 기록이므로 현재 작업 기준은 위 바이블과 이 README를 따른다.

기존 AI 생성 초안을 기반으로 했으며 여우는 스타킹 수정과 원본 색감 보존을 거쳤다. 이번 Git 인계에서는 새 그림 생성·기존 디자인 변경·Unity 임포트·씬 연결을 하지 않았다.
