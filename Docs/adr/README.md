# ADR — 설계 결정 기록

되돌리기 어렵고, 맥락 없이 보면 의아하며, 실제 대안 사이에서 고른 결정만 남긴다. 셋 중 하나라도 아니면 ADR로 만들지 않는다.

형식: `NNNN-결정-요약.md`, 번호는 이어서 붙인다. 본문은 "무엇을 정했고 왜"를 1~3문장으로 쓰고, 필요할 때만 기각한 대안(Considered Options)과 파급 효과(Consequences)를 덧붙인다.

| 번호 | 결정 |
|---|---|
| [0001](0001-spin-builds-pot-cash-out-deals-damage.md) | SPIN은 판돈을 쌓고, 피해는 CASH OUT이 낸다 |
| [0002](0002-hijack-is-earned-not-flipped.md) | HIJACK 기회는 운(코인플립)이 아니라 딜러의 하우스 룰 달성으로 얻는다 |
| [0003](0003-no-trophy-hijack-is-permanent.md) | 전리품 칸 없이 HIJACK이 영구 덮어쓰기다 (룰렛 8칸 고정) |
| [0004](0004-dealers-have-house-cut.md) | 일반 딜러의 룰렛에도 하우스 몫이 있다 (토끼 제외) |
| [0005](0005-outer-ring-carries-house-cut.md) | 바깥 링에는 보호막 대신 하우스 몫이 있다 |
| [0006](0006-relics-are-door-prizes.md) | 유물은 문 카드에 걸린 상금으로 얻는다 |
| [0007](0007-table-chips-stay-at-the-table.md) | ~~전투에서 딴 칩은 테이블에 두고 나간다~~ (0008로 대체) |
| [0008](0008-won-chips-split-into-chips-and-cash.md) | 딴 칩은 일부는 칩으로, 나머지는 현금으로 환전된다 |
| [0009](0009-run-continues-after-boss-into-shop.md) | 보스를 이기면 상점층을 지나 2회차로 이어진다 (10층 런) |
| [0010](0010-relics-need-house-rule-and-win.md) | 문 카드 유물은 하우스 룰을 달성하고 이겨야 받는다 (0006 수정) |
| [0011](0011-final-boss-at-30f-steals-house-cut.md) | 정식판은 30층, 최종 보스전에서만 하우스 몫을 HIJACK할 수 있다 |
| [0012](0012-house-cut-chance-rises-each-spin.md) | 한 턴 안에서 SPIN할수록 하우스 몫 확률이 오른다 (N번째 = N × 5%, 최대 35%) |
| [0013](0013-chains-are-a-table-rule-not-base.md) | 연쇄는 기본 규칙이 아니라 테이블 규칙이다 (칸은 하나씩 발동) |
