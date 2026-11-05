#!/usr/bin/env python3
"""룰렛·UI 도트 키트(Art/UI/*.png)를 코드로 그린다. AI 이미지를 쓰지 않는다.

실행: python3 Assets/RouletteSystem/Art/Source/generate_ui_kit.py
모든 그림은 화면에서 2배로 표시한다(1 도트 = 2 UI). 색은 ArtStyleBible.md의 공용 팔레트만 쓴다.
"""
import math
import os
from PIL import Image

OUT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "UI"))

# ArtStyleBible 3장 공용 팔레트와 룰렛·UI 추가색(7장)
INK = (10, 14, 22, 255)
BLACK1 = (25, 25, 31, 255)
BLACK2 = (39, 40, 48, 255)
BLACK3 = (77, 78, 86, 255)
CREAM1 = (143, 146, 164, 255)
CREAM2 = (196, 198, 212, 255)
CREAM3 = (228, 230, 240, 255)
WHITE = (251, 251, 252, 255)
BROWN1 = (51, 36, 36, 255)
BROWN2 = (74, 54, 50, 255)
BROWN3 = (94, 69, 64, 255)
BROWN4 = (114, 86, 78, 255)
WINE = (92, 15, 34, 255)
RED = (168, 28, 48, 255)
BRASS_DARK = (138, 106, 96, 255)  # 갈색 8a6a60을 황동의 그림자로 쓴다
BRASS = (240, 180, 60, 255)
BRASS_LIGHT = (255, 224, 138, 255)
NAVY0 = (8, 18, 49, 255)
NAVY1 = (17, 36, 82, 255)
FELT_DARK = (26, 68, 54, 255)
FELT = (42, 102, 80, 255)
FELT_DEEP = (18, 46, 38, 255)  # 추가색: 어두운 펠트
CLEAR = (0, 0, 0, 0)


def new(w, h, color=CLEAR):
    return Image.new("RGBA", (w, h), color)


def rect(img, x0, y0, x1, y1, color):
    px = img.load()
    for y in range(max(0, y0), min(img.height, y1)):
        for x in range(max(0, x0), min(img.width, x1)):
            px[x, y] = color


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name))


def lit(dx, dy, r):
    """왼쪽 위 광원: 1이면 밝은 쪽, -1이면 어두운 쪽."""
    return (-dx - dy) * 0.7071 / max(1.0, r)


def pointer():
    # 아래를 가리키는 황동 지시침: 왼쪽 면은 밝고 오른쪽 면은 어두운 각진 금속, 붉은 보석.
    w, h = 32, 40
    img = new(w, h)
    px = img.load()
    c = w / 2.0
    mask = [[False] * w for _ in range(h)]
    for y in range(h):
        half = 14 - abs(y - 5) * 0.8 if y < 12 else max(0, 8.4 * (h - 1 - y) / (h - 12))
        for x in range(w):
            mask[y][x] = abs(x + 0.5 - c) <= half
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            edge = any(not (0 <= x + ax < w and 0 <= y + ay < h and mask[y + ay][x + ax]) for ax, ay in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                px[x, y] = INK
            else:
                u = (x + 0.5 - c)
                band = 5 if u < -5 else 4 if u < 0 else 3 if u < 5 else 2
                if y < 3:
                    band = min(6, band + 1)
                px[x, y] = GOLD_R[band] + (255,)
    for y in range(1, 11):
        for x in range(w):
            m = abs(x + 0.5 - c) + abs(y - 5.5)
            if m <= 5:
                if m > 4:
                    px[x, y] = INK
                elif (x + 0.5 - c) + (y - 5.5) < -1.5:
                    px[x, y] = (247, 184, 196, 255)
                elif (x + 0.5 - c) + (y - 5.5) < 1:
                    px[x, y] = (232, 50, 60, 255)
                else:
                    px[x, y] = RED
    save(img, "pointer.png")


def disc(name, size, tones, ring=None):
    """돔처럼 보이는 원판. tones는 밝음→어두움 3단계. 흰 계열이면 색을 입혀 쓴다."""
    img = new(size, size)
    px = img.load()
    c = size / 2
    r = c - 0.5
    for y in range(size):
        for x in range(size):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            d = math.hypot(dx, dy)
            if d > r:
                continue
            t = lit(dx, dy, r)
            if d > r - 1:
                color = INK
            elif d > r - 3:
                color = tones[0] if t > 0.15 else tones[2]
            elif d > r - 4:
                color = tones[2] if t > 0 else tones[1]
            else:
                color = tones[0] if t > 0.62 else tones[1] if t > -0.55 else tones[2]
            px[x, y] = color
    save(img, name)


def ring(name, size, width):
    img = new(size, size)
    px = img.load()
    c = size / 2
    r = c - 0.5
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + 0.5 - c, y + 0.5 - c)
            if r - width <= d <= r:
                px[x, y] = WHITE
    save(img, name)


def bevel():
    # 9조각 입체 판(회색조, 색을 곱해 쓴다). 2칸 두께의 빛과 그림자, 깎은 모서리로 납작한 네모 느낌을 없앤다.
    s = 14
    mid, shadow = (230, 230, 230, 255), (140, 140, 140, 255)
    img = new(s, s)
    rect(img, 2, 0, s - 2, s, INK)
    rect(img, 0, 2, s, s - 2, INK)
    rect(img, 1, 1, s - 1, s - 1, INK)
    rect(img, 2, 2, s - 2, s - 2, mid)
    rect(img, 2, 1, s - 2, 3, WHITE)
    rect(img, 1, 2, 3, s - 2, WHITE)
    rect(img, 2, s - 3, s - 2, s - 1, shadow)
    rect(img, s - 3, 2, s - 1, s - 2, shadow)
    save(img, "bevel.png")

    # 눌린 듯 들어간 판(값 표시·막대 바닥)
    img = new(s, s)
    rect(img, 0, 0, s, s, INK)
    rect(img, 1, 1, s - 1, s - 1, (204, 204, 204, 255))
    rect(img, 1, 1, s - 1, 2, (120, 120, 120, 255))
    rect(img, 1, 1, 2, s - 1, (120, 120, 120, 255))
    rect(img, 1, s - 2, s - 1, s - 1, WHITE)
    save(img, "inset.png")


def deco_frame():
    # 9조각 빈 테두리(색을 곱해 쓴다): 안쪽으로 꺾인 아르데코 모서리.
    s = 20
    img = new(s, s)
    px = img.load()
    notch = 4
    for y in range(s):
        for x in range(s):
            ix, iy = min(x, s - 1 - x), min(y, s - 1 - y)
            if ix + iy < notch:
                continue
            on_outer = ix == 0 or iy == 0 or ix + iy == notch
            on_inner = (ix == 2 and iy >= 2) or (iy == 2 and ix >= 2)
            if on_outer:
                lt = x < s / 2 and y < s / 2
                px[x, y] = WHITE if lt or x <= y else (170, 170, 170, 255)
            elif on_inner and ix + iy >= notch + 2:
                px[x, y] = (170, 170, 170, 255)
    # 모서리 안쪽 작은 징
    for cx, cy in ((5, 5), (s - 6, 5), (5, s - 6), (s - 6, s - 6)):
        px[cx, cy] = WHITE
    save(img, "deco_frame.png")


def bar_fill():
    h = 11
    img = new(4, h, (204, 204, 204, 255))
    rect(img, 0, 0, 4, 2, WHITE)
    rect(img, 0, h - 3, 4, h, (128, 128, 128, 255))
    save(img, "bar_fill.png")


def chip():
    # 카지노 칩(회색조, 색을 곱해 쓴다).
    s = 16
    img = new(s, s)
    px = img.load()
    c = s / 2
    for y in range(s):
        for x in range(s):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            d = math.hypot(dx, dy)
            if d > 7.6:
                continue
            a = math.degrees(math.atan2(dy, dx)) % 90
            if d > 6.6:
                px[x, y] = INK
            elif d > 4.6:
                px[x, y] = WHITE if a < 30 else (204, 204, 204, 255)
            elif d > 3.6:
                px[x, y] = (128, 128, 128, 255)
            else:
                px[x, y] = (204, 204, 204, 255)
    save(img, "chip.png")


def bubble_tail():
    # 오른쪽을 가리키는 말풍선 꼬리
    img = new(8, 14)
    px = img.load()
    for y in range(14):
        half = 7 - abs(y - 6.5)
        for x in range(8):
            if x <= half:
                edge = x >= half - 1
                px[x, y] = INK if edge else CREAM3
    save(img, "bubble_tail.png")


# ── 재질 색 계단(어두움 → 밝음). 조명 값을 계단으로 끊어 칠해 도트 그림의 면을 만든다.
WALL_R = [(16, 9, 22), (24, 12, 30), (34, 16, 38), (48, 22, 46), (66, 30, 56), (92, 42, 64), (124, 62, 70)]
GOLD_R = [(51, 36, 36), (92, 66, 48), (138, 106, 96), (196, 140, 40), (240, 180, 60), (255, 224, 138), (251, 251, 252)]
CURT_R = [(30, 6, 16), (48, 9, 22), (72, 12, 28), (92, 15, 34), (124, 20, 40), (168, 28, 48)]
CARPET_R = [(26, 7, 16), (42, 10, 22), (62, 13, 28), (92, 15, 34), (118, 21, 40), (150, 32, 50)]
WOOD_R = [(25, 18, 22), (38, 26, 28), (51, 36, 36), (74, 54, 50), (94, 69, 64), (114, 86, 78)]
STEEL_R = [(10, 14, 22), (25, 25, 31), (39, 40, 48), (77, 78, 86), (143, 146, 164), (196, 198, 212)]
FELT_R = [(12, 32, 28), (18, 46, 38), (26, 68, 54), (34, 84, 66), (42, 102, 80), (58, 124, 96)]
ENAMEL_GOLD = [(138, 92, 30), (196, 140, 40), (240, 180, 60), (250, 204, 96), (255, 224, 138), (251, 251, 252)]
ENAMEL_WINE = [(48, 9, 22), (92, 15, 34), (130, 20, 42), (168, 28, 48), (216, 44, 56), (247, 184, 196)]

BAYER = ((0.0, 0.5), (0.75, 0.25))


def ramp(colors, t, x=None, y=None):
    """0~1 값을 색 계단으로 바꾼다. 좌표를 주면 계단 경계에만 2×2 점묘를 얇게 넣는다."""
    t = max(0.0, min(0.999, t))
    v = t * (len(colors) - 1)
    i = int(v)
    f = v - i
    if x is not None and 0.42 < f < 0.58 and BAYER[y % 2][x % 2] < 0.5:
        i = min(len(colors) - 1, i + 1)
    elif f >= 0.58:
        i = min(len(colors) - 1, i + 1)
    c = colors[i]
    return (c[0], c[1], c[2], 255)


def metal(theta_deg, bias=0.0):
    """링 금속의 띠 모양 반사: 왼쪽 위와 오른쪽 아래에 밝은 호가 생긴다."""
    a = math.radians(theta_deg - 315)
    return 0.5 + 0.34 * math.cos(2 * a) + 0.12 * math.cos(a) + bias


SCONCES = [240 * k for k in range(5)]
ALCOVES = [120 + 240 * k for k in range(4)]
FLOOR_Y = 131


def room_light(x, y):
    t = 0.14
    for sx in SCONCES:
        d = ((x - sx) / 92.0) ** 2 + ((y - 44) / 62.0) ** 2
        t += 0.62 * math.exp(-d)
    for ax in ALCOVES:
        d = ((x - ax) / 80.0) ** 2 + ((y - 26) / 44.0) ** 2
        t += 0.28 * math.exp(-d)
    return t


def slot_machine(img, cx, base_y):
    x0, x1 = cx - 22, cx + 22
    top = base_y - 72
    rect(img, x0, top, x1, base_y, INK)
    px = img.load()
    for y in range(top + 1, base_y):
        for x in range(x0 + 1, x1 - 1):
            t = 0.5 + 0.42 * math.cos((x - cx + 8) / 22.0 * 1.5)
            px[x, y] = ramp(STEEL_R, t * 0.62, x, y)
    # 위 간판과 전구 자리
    rect(img, x0 - 2, top - 9, x1 + 2, top + 3, INK)
    rect(img, x0 - 1, top - 8, x1 + 1, top + 2, WINE)
    rect(img, x0 - 1, top - 8, x1 + 1, top - 7, (168, 28, 48, 255))
    rect(img, x0 + 8, top - 5, x1 - 8, top - 1, BRASS)
    rect(img, x0 + 8, top - 5, x1 - 8, top - 4, BRASS_LIGHT)
    # 릴 창
    rect(img, x0 + 4, top + 10, x1 - 4, top + 32, INK)
    rect(img, x0 + 5, top + 11, x1 - 5, top + 12, BRASS)
    rect(img, x0 + 5, top + 31, x1 - 5, top + 32, BRASS_DARK)
    for k in range(3):
        rx = x0 + 6 + k * 11
        rect(img, rx, top + 13, rx + 10, top + 30, CREAM3)
        rect(img, rx, top + 13, rx + 10, top + 15, CREAM1)
        rect(img, rx, top + 28, rx + 10, top + 30, CREAM1)
        sym = RED if k != 1 else BRASS
        rect(img, rx + 3, top + 18, rx + 7, top + 25, sym)
        rect(img, rx + 2, top + 20, rx + 8, top + 23, sym)
    # 받침판과 버튼, 코인 받이
    rect(img, x0 + 3, top + 37, x1 - 3, top + 44, BROWN2)
    rect(img, x0 + 3, top + 37, x1 - 3, top + 38, BROWN4)
    for k, c in enumerate((RED, BRASS, FELT)):
        rect(img, x0 + 8 + k * 11, top + 39, x0 + 14 + k * 11, top + 42, c)
    rect(img, x0 + 8, top + 52, x1 - 8, top + 60, INK)
    rect(img, x0 + 9, top + 58, x1 - 9, top + 60, BRASS_DARK)
    # 손잡이
    rect(img, x1, top + 24, x1 + 4, top + 28, BRASS_DARK)
    rect(img, x1 + 2, top + 10, x1 + 4, top + 26, STEEL_R[4] + (255,))
    rect(img, x1 + 1, top + 5, x1 + 5, top + 11, RED)
    rect(img, x1 + 1, top + 5, x1 + 3, top + 7, (232, 90, 90, 255))


def column(img, cx):
    x0, x1 = cx - 13, cx + 13
    px = img.load()
    for y in range(14, FLOOR_Y):
        for x in range(max(0, x0), min(img.width, x1)):
            u = (x + 0.5 - cx) / 13.0
            flute = 0.11 * math.cos(u * 15.7)
            t = 0.5 + 0.30 * (-u) + flute + 0.25 * (room_light(x, y) - 0.5)
            px[x, y] = ramp(WOOD_R, t, x, y)
        for x in (x0, x1 - 1):
            if 0 <= x < img.width:
                px[x, y] = INK
    # 기둥머리와 받침: 황동
    for y0, y1 in ((14, 25), (FLOOR_Y - 14, FLOOR_Y - 1)):
        for y in range(y0, y1):
            ext = 3
            for x in range(max(0, x0 - ext), min(img.width, x1 + ext)):
                u = (x + 0.5 - cx) / (13.0 + ext)
                t = 0.62 - 0.34 * u + 0.15 * math.sin((y - y0) * 1.7)
                px[x, y] = ramp(GOLD_R, t, x, y)
        rect(img, x0 - 3, y0, x1 + 3, y0 + 1, INK)
        rect(img, x0 - 3, y1, x1 + 3, y1 + 1, INK)


def sconce(img, cx, lit=True):
    rect(img, cx - 5, 56, cx + 6, 60, GOLD_R[2] + (255,))
    rect(img, cx - 5, 56, cx + 6, 57, BRASS)
    rect(img, cx - 2, 60, cx + 3, 64, GOLD_R[1] + (255,))
    rect(img, cx - 4, 38, cx + 5, 56, INK)
    rect(img, cx - 3, 39, cx + 4, 55, BRASS_LIGHT)
    rect(img, cx - 2, 41, cx + 1, 53, WHITE)
    rect(img, cx + 2, 39, cx + 4, 55, BRASS)


def mute(img, y0, y1, amount):
    """한 구역의 색을 어두운 자주로 끌어당겨 대비를 줄인다."""
    px = img.load()
    base = (22, 13, 26)
    for y in range(y0, y1):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            px[x, y] = (int(r + (base[0] - r) * amount), int(g + (base[1] - g) * amount), int(b + (base[2] - b) * amount), a)


def stage():
    # 전투 띠 배경 960×170: 조명이 비치는 벽, 커튼 친 아치와 슬롯머신, 기둥과 벽등, 샹들리에, 원근이 있는 카펫.
    w, h = 960, 170
    img = new(w, h, INK)
    px = img.load()

    # 벽지: 마름모 무늬가 조명을 받아 드러난다
    for y in range(0, FLOOR_Y):
        for x in range(w):
            t = room_light(x, y)
            u, v = (x + (y // 16) % 2 * 8) % 16 - 8, y % 16 - 8
            if abs(u) + abs(v) == 7:
                t += 0.05
            px[x, y] = ramp(WALL_R, t * 0.8, x, y)

    # 아래 벽판
    for y in range(96, FLOOR_Y):
        for x in range(w):
            t = room_light(x, y) * 0.62 + 0.1
            px[x, y] = ramp(WOOD_R, t, x, y)
    rect(img, 0, 95, w, 96, INK)
    for x in range(w):
        px[x, 96] = ramp(GOLD_R, metal(x * 0.75, -0.2) * 0.8)
    for gx in range(0, w, 40):
        rect(img, gx + 3, 101, gx + 37, FLOOR_Y - 7, (25, 18, 22, 255))
        rect(img, gx + 4, 102, gx + 36, FLOOR_Y - 8, (51, 36, 36, 255))
        rect(img, gx + 4, 102, gx + 36, 103, (94, 69, 64, 255))
        rect(img, gx + 4, 102, gx + 5, FLOOR_Y - 8, (74, 54, 50, 255))

    # 아치: 깊은 안쪽 + 황동 테 + 커튼 + 슬롯머신
    for cx in ALCOVES:
        r, top = 72, 92
        for y in range(16, FLOOR_Y):
            for x in range(cx - r - 5, cx + r + 6):
                dy = max(0, top - y)
                d = math.hypot(x + 0.5 - cx, dy)
                if d <= r:
                    depth = 1 - d / r
                    t = 0.12 + 0.34 * (y - 20) / 110.0 * (0.4 + depth)
                    px[x, y] = ramp(WALL_R, t, x, y)
                elif d <= r + 4:
                    th = math.degrees(math.atan2(x + 0.5 - cx, -(dy if dy else 0.01)))
                    px[x, y] = INK if d > r + 3 else ramp(GOLD_R, metal(th, 0.02) * 0.92)
        # 안쪽 바닥 빛
        for y in range(FLOOR_Y - 14, FLOOR_Y):
            for x in range(cx - r + 1, cx + r):
                t = 0.42 - abs(x - cx) / r * 0.26
                px[x, y] = ramp(CARPET_R, t, x, y)
        # 커튼: 주름은 코사인, 묶인 끈까지 넓고 아래로 좁아진다
        for side in (-1, 1):
            for y in range(24, FLOOR_Y):
                tie = 92
                width = 24 - 12 * math.exp(-((y - tie) / 11.0) ** 2) + (y - 96) * 0.08 * (y > 96)
                for k in range(int(width)):
                    x = cx + side * (r - k)
                    dy = max(0, top - y)
                    if math.hypot(x + 0.5 - cx, dy) > r:
                        continue
                    t = 0.5 + 0.34 * math.cos(k / 2.4) - 0.2 * (k / max(1, width))
                    px[x, y] = ramp(CURT_R, t * (0.6 + room_light(cx + side * r, y) * 0.5), x, y)
            rect(img, cx + side * (r - 16) - 7, 91, cx + side * (r - 16) + 8, 95, BRASS)
            rect(img, cx + side * (r - 16) - 7, 94, cx + side * (r - 16) + 8, 95, GOLD_R[2] + (255,))
        # 양 끝 아치는 작은 룰렛에 가리므로 비워 둔다. 슬롯머신은 가운데 둘만.
        if 240 < cx < 720:
            slot_machine(img, cx, FLOOR_Y - 4)

    # 천장 몰딩: 치아 장식
    rect(img, 0, 0, w, 14, (25, 18, 22, 255))
    rect(img, 0, 0, w, 3, (16, 9, 22, 255))
    for x in range(w):
        px[x, 11] = ramp(GOLD_R, metal(x * 0.75) * 0.9)
        px[x, 12] = ramp(GOLD_R, metal(x * 0.75, -0.2) * 0.7)
    rect(img, 0, 13, w, 14, INK)

    for cx in SCONCES:
        column(img, cx)

    # 벽 쪽은 통째로 어둡고 차분하게 눌러 인물과 룰렛이 먼저 보이게 한다.
    mute(img, 0, FLOOR_Y, 0.58)
    for cx in SCONCES:
        sconce(img, cx)

    # 걸레받이
    rect(img, 0, FLOOR_Y - 3, w, FLOOR_Y - 1, (25, 18, 22, 255))
    rect(img, 0, FLOOR_Y - 1, w, FLOOR_Y, INK)

    # 카펫: 원근(앞으로 올수록 밝고 무늬가 크다), 벽등의 빛 웅덩이, 줄지어 놓인 황동 마름모
    for y in range(FLOOR_Y, h):
        depth = (y - FLOOR_Y) / float(h - FLOOR_Y)
        for x in range(w):
            t = 0.16 + depth * 0.34
            for sx in SCONCES:
                t += 0.2 * math.exp(-((x - sx) / 80.0) ** 2) * (1 - depth * 0.6)
            px[x, y] = ramp(CARPET_R, t)
    mute(img, FLOOR_Y, h, 0.25)
    # 인물 그림자
    for cx in (200, 760):
        for y in range(155, 166):
            for x in range(cx - 44, cx + 45):
                if ((x - cx) / 44.0) ** 2 + ((y - 160.5) / 5.5) ** 2 <= 1 and (x + y) % 2 == 0:
                    px[x, y] = CARPET_R[0] + (255,)
    # 앞 난간
    rect(img, 0, h - 4, w, h - 3, INK)
    for x in range(w):
        px[x, h - 3] = ramp(GOLD_R, metal(x * 0.75, 0.05))
        px[x, h - 2] = ramp(GOLD_R, metal(x * 0.75, -0.25))
    rect(img, 0, h - 1, w, h, INK)
    save(img, "stage_bg.png")


def stage_lights():
    # 번갈아 켜는 불빛 두 장(나머지는 투명): 벽등 둘레의 잔잔한 깜빡임과 슬롯머신 간판 하나.
    w, h = 960, 170
    for phase in (0, 1):
        img = new(w, h)
        px = img.load()
        for cx in SCONCES:
            if phase == 0:
                for y in range(34, 62):
                    for x in range(max(0, cx - 10), min(w, cx + 11)):
                        d = abs(x - cx) + abs(y - 47)
                        if 9 < d < 12 and (x + y) % 2 == 0:
                            px[x, y] = (138, 92, 48, 255)
        for cx in (360, 600):
            top = FLOOR_Y - 4 - 72
            rect(img, cx - 14, top - 5, cx + 14, top - 1, (196, 140, 40, 255) if phase == 0 else (138, 92, 30, 255))
        save(img, "stage_lights_%d.png" % phase)


def board():
    # 아래 보드 960×370: 차분한 펠트 한 판과 어두운 양쪽 판. 장식 없이 룰렛이 가장 밝게 보이게 한다.
    w, h = 960, 370
    img = new(w, h, FELT_DARK)
    px = img.load()
    cx, cy = w / 2.0, 188.0
    for y in range(h):
        for x in range(w):
            d = math.hypot(x + 0.5 - cx, (y + 0.5 - cy) * 1.05)
            t = 0.5 if d < 230 else 0.34 if d < 340 else 0.2
            px[x, y] = ramp(FELT_R, t)
    # 위쪽 그림자와 얇은 경계
    rect(img, 0, 0, w, 2, INK)
    rect(img, 0, 2, w, 6, FELT_R[0] + (255,))
    for x0, x1 in ((8, 222), (738, 952)):
        y0, y1 = 14, h - 8
        rect(img, x0 - 1, y0 - 1, x1 + 1, y1 + 1, INK)
        rect(img, x0, y0, x1, y1, (22, 22, 28, 255))
        rect(img, x0, y0, x1, y0 + 1, BLACK3)
        rect(img, x0, y0, x0 + 1, y1, BLACK2)
    save(img, "board_bg.png")


def medallion(name, size, enamel):
    # SPIN 버튼: 황동 톱니 테두리 + 에나멜 돔 + 초승달 반사.
    img = new(size, size)
    px = img.load()
    c = size / 2.0
    r = c - 0.5
    for y in range(size):
        for x in range(size):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            d = math.hypot(dx, dy)
            if d > r:
                continue
            th = (math.degrees(math.atan2(dx, -dy)) + 360) % 360
            if d > r - 1:
                col = INK
            elif d > r - 6:
                notch = int(th / 10) % 2 == 0 and d > r - 3
                col = ramp(GOLD_R, metal(th, -0.2 if notch else 0.06), x, y)
            elif d > r - 7:
                col = INK
            elif d > r - 9:
                col = ramp(GOLD_R, metal(th + 180, -0.12))
            elif d > r - 10:
                col = BROWN1
            else:
                inner = r - 10
                nx, ny = dx / inner, dy / inner
                nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
                t = 0.26 + 0.5 * (-0.55 * nx - 0.6 * ny + 0.58 * nz)
                # 위쪽 초승달 반사
                hx, hy = nx + 0.14, ny + 0.12
                if 0.68 < math.hypot(hx, hy) < 0.78 and ny < -0.5 and nx < 0.25:
                    t = 1.0
                col = ramp(enamel, t, x, y)
            px[x, y] = col
    save(img, name)


def hub(name, size):
    img = new(size, size)
    px = img.load()
    c = size / 2.0
    r = c - 0.5
    for y in range(size):
        for x in range(size):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            d = math.hypot(dx, dy)
            if d > r:
                continue
            th = (math.degrees(math.atan2(dx, -dy)) + 360) % 360
            if d > r - 1:
                col = INK
            elif d > r - 3:
                col = ramp(GOLD_R, metal(th, -0.05))
            elif d > r - 4:
                col = INK
            else:
                t = 0.42 + 0.3 * math.cos(math.radians(th - 315) * 2) * 0.6 + (0.1 if int(d) % 3 == 0 else 0)
                col = ramp(STEEL_R, t * 0.7, x, y)
            px[x, y] = col
    save(img, name)


if __name__ == "__main__":
    pointer()
    # 색을 곱해 쓰는 회색조 돔(동전 등)
    disc("dome.png", 84, (WHITE, (230, 230, 230, 255), (140, 140, 140, 255)))
    medallion("spin_button.png", 84, ENAMEL_GOLD)
    medallion("dealer_button.png", 84, ENAMEL_WINE)
    hub("hub.png", 100)
    ring("ring.png", 100, 7)
    ring("glow_ring.png", 360, 3)
    bevel()
    deco_frame()
    bar_fill()
    chip()
    bubble_tail()
    stage()
    stage_lights()
    board()
    print("generated", sorted(os.listdir(OUT)))
