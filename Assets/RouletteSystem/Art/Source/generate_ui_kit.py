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
    # 아래를 가리키는 황동 지시침. 위쪽 고정부 + 뾰족한 끝.
    w, h = 28, 34
    img = new(w, h)
    px = img.load()
    c = w / 2
    mask = [[False] * w for _ in range(h)]
    for y in range(h):
        half = 12 if y < 10 else max(0, 12 * (h - 1 - y) / (h - 11))
        for x in range(w):
            if abs(x + 0.5 - c) <= half:
                mask[y][x] = True
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            edge = any(not (0 <= x + ax < w and 0 <= y + ay < h and mask[y + ay][x + ax]) for ax, ay in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                px[x, y] = INK
            elif x < c - 5 or y < 3:
                px[x, y] = BRASS_LIGHT
            elif x > c + 4:
                px[x, y] = BRASS_DARK
            else:
                px[x, y] = BRASS
    # 붉은 보석
    for y in range(4, 10):
        for x in range(w):
            if abs(x + 0.5 - c) + abs(y - 6.5) <= 3:
                px[x, y] = RED if abs(x + 0.5 - c) + abs(y - 6.5) > 1 else (232, 50, 60, 255)
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


def stage():
    # 전투 띠 배경 960×170: 벽 + 아르데코 판 + 벽등 + 슬롯머신 + 카펫. 인물이 카펫 위에 선다.
    w, h = 960, 170
    floor_y = 131
    img = new(w, h, NAVY0)
    px = img.load()

    # 벽 위아래 몰딩
    rect(img, 0, 0, w, 8, BLACK1)
    rect(img, 0, 8, w, 10, BRASS_DARK)
    rect(img, 0, 10, w, 11, BLACK1)
    rect(img, 0, 11, w, floor_y, WINE[:3] + (255,))
    # 벽을 어둡게 눌러 인물이 앞으로 나오게 한다
    rect(img, 0, 11, w, floor_y, (38, 16, 30, 255))

    panel_w = 112
    for i in range(9):
        x0 = 4 + i * 120 - 12
        x1 = x0 + panel_w
        rect(img, x0, 18, x1, floor_y - 12, BROWN1)
        rect(img, x0 + 1, 19, x1 - 1, floor_y - 13, (46, 22, 36, 255))
        rect(img, x0 + 1, 19, x1 - 1, 20, BROWN3)
        rect(img, x0 + 1, 19, x0 + 2, floor_y - 13, BROWN3)
        # 부채꼴(선버스트) 상단 장식
        cx = (x0 + x1) // 2
        cy = 62
        for y in range(22, cy):
            for x in range(max(0, x0 + 6), min(w, x1 - 6)):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                d = math.hypot(dx, dy)
                if 8 < d < 39:
                    a = math.degrees(math.atan2(dx, -dy))
                    if abs(a) < 80 and int((a + 90) / 16) % 2 == 0:
                        px[x, y] = (62, 30, 44, 255)
                elif 39 <= d < 41:
                    px[x, y] = BROWN4
        # 아래 세로 홈
        for k in range(-3, 4):
            gx = cx + k * 12
            rect(img, gx, 68, gx + 1, floor_y - 18, (58, 28, 42, 255))

    # 기둥과 벽등
    for i in range(10):
        x = i * 120 - 12
        rect(img, x - 4, 11, x + 4, floor_y, BLACK1)
        rect(img, x - 3, 11, x - 2, floor_y, BRASS_DARK)
        rect(img, x + 2, 11, x + 3, floor_y, BROWN2)
        # 벽등: 황동 받침과 빛나는 갓
        rect(img, x - 3, 40, x + 4, 44, BRASS)
        rect(img, x - 2, 32, x + 3, 40, BRASS_LIGHT)
        rect(img, x - 1, 28, x + 2, 32, WHITE)
        for dy in range(-16, 22):
            for dx in range(-14, 15):
                d = abs(dx) + abs(dy)
                if 8 < d < 15 and (dx + dy) % 2 == 0:
                    yy, xx = 36 + dy, x + dx
                    if 0 <= xx < w and 11 <= yy < floor_y and px[xx, yy] != BLACK1:
                        px[xx, yy] = (92, 50, 44, 255)

    # 슬롯머신: 뒤쪽 어두운 실루엣과 작게 켜진 창
    for sx in (60, 300, 660, 900):
        rect(img, sx - 22, 72, sx + 22, floor_y, BLACK1)
        rect(img, sx - 21, 73, sx + 21, floor_y, BLACK2)
        rect(img, sx - 21, 73, sx + 21, 75, BRASS_DARK)
        rect(img, sx - 17, 80, sx + 17, 96, INK)
        for k in range(3):
            rect(img, sx - 15 + k * 11, 82, sx - 6 + k * 11, 94, CREAM2)
            rect(img, sx - 12 + k * 11, 85, sx - 9 + k * 11, 91, RED if k != 1 else BRASS)
        rect(img, sx - 17, 102, sx + 17, 106, BROWN3)
        rect(img, sx + 22, 86, sx + 25, 90, BRASS)
        rect(img, sx + 23, 78, sx + 25, 86, BLACK3)
        rect(img, sx + 22, 74, sx + 26, 78, RED)

    # 걸레받이
    rect(img, 0, floor_y - 6, w, floor_y - 1, BLACK1)
    rect(img, 0, floor_y - 6, w, floor_y - 5, BROWN3)
    rect(img, 0, floor_y - 1, w, floor_y, INK)

    # 카펫: 버건디에 아르데코 마름모 무늬, 뒤로 갈수록 어둡게
    for y in range(floor_y, h):
        band = (y - floor_y) / (h - floor_y)
        base = (58, 14, 30, 255) if band < 0.33 else WINE if band < 0.72 else (112, 20, 40, 255)
        for x in range(w):
            px[x, y] = base
    for row, y0 in enumerate((floor_y + 6, floor_y + 22)):
        size = 4 if row == 0 else 6
        step = 40
        for gx in range(-step, w + step, step):
            cx = gx + (step // 2 if row else 0)
            for y in range(y0 - size, y0 + size + 1):
                for x in range(cx - size, cx + size + 1):
                    if 0 <= x < w and floor_y <= y < h:
                        m = abs(x - cx) + abs(y - y0)
                        if m == size:
                            px[x, y] = BRASS_DARK
                        elif m == 0:
                            px[x, y] = BRASS
    rect(img, 0, h - 3, w, h - 2, BRASS_DARK)
    rect(img, 0, h - 2, w, h - 1, BRASS)
    rect(img, 0, h - 1, w, h, INK)
    save(img, "stage_bg.png")


def board():
    # 아래 보드 960×370: 펠트, 위쪽 그림자, 양쪽 기둥의 어두운 패널, 황동 상감선.
    w, h = 960, 370
    img = new(w, h, FELT_DARK)
    px = img.load()
    for y in range(h):
        for x in range(w):
            # 가운데에서 멀수록 단계적으로 어둡게(비네트, 칸 단위 계단)
            d = math.hypot((x - w / 2) / (w / 2), (y - h * 0.52) / (h / 2))
            base = FELT if d < 0.5 else FELT_DARK if d < 1.0 else FELT_DEEP
            # 펠트 결: 드문 점
            if (x * 7 + y * 13) % 29 == 0 and base != FELT_DEEP:
                base = FELT_DARK if base == FELT else FELT_DEEP
            px[x, y] = base

    # 위쪽 그림자 띠
    rect(img, 0, 0, w, 3, INK)
    for x in range(w):
        for y in range(3, 7):
            if y < 5 or (x + y) % 2 == 0:
                px[x, y] = FELT_DEEP

    # 기둥 패널: 판돈·기록이 올라가는 어두운 판
    for x0, x1 in ((8, 222), (738, 952)):
        y0, y1 = 14, h - 10
        rect(img, x0, y0, x1, y1, INK)
        rect(img, x0 + 1, y0 + 1, x1 - 1, y1 - 1, BLACK1)
        rect(img, x0 + 1, y0 + 1, x1 - 1, y0 + 2, BLACK3)
        rect(img, x0 + 1, y0 + 1, x0 + 2, y1 - 1, BLACK3)
        rect(img, x0 + 4, y0 + 4, x1 - 4, y0 + 5, BRASS_DARK)
        rect(img, x0 + 4, y1 - 5, x1 - 4, y1 - 4, BRASS_DARK)
        rect(img, x0 + 4, y0 + 4, x0 + 5, y1 - 4, BRASS_DARK)
        rect(img, x1 - 5, y0 + 4, x1 - 4, y1 - 4, BRASS_DARK)
        # 모서리 꺾임
        for cx, cy, sx, sy in ((x0 + 4, y0 + 4, 1, 1), (x1 - 5, y0 + 4, -1, 1), (x0 + 4, y1 - 5, 1, -1), (x1 - 5, y1 - 5, -1, -1)):
            for k in range(6):
                px[cx + sx * k, cy + sy * 6] = BRASS_DARK
                px[cx + sx * 6, cy + sy * k] = BRASS_DARK
    # 가운데 황동 상감 원: 룰렛 자리
    cx, cy = w / 2, 6 + 183
    for y in range(h):
        for x in range(224, 736):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if 181 <= d < 182:
                px[x, y] = BRASS_DARK
            elif 178 <= d < 181:
                px[x, y] = FELT_DEEP
    save(img, "board_bg.png")


if __name__ == "__main__":
    pointer()
    # 색을 곱해 쓰는 회색조 돔(SPIN 버튼·딜러 표시판·동전·허브)
    disc("dome.png", 84, (WHITE, (230, 230, 230, 255), (140, 140, 140, 255)))
    disc("hub.png", 100, (BLACK3, BLACK2, BLACK1))
    ring("ring.png", 100, 7)
    ring("glow_ring.png", 360, 6)
    bevel()
    deco_frame()
    bar_fill()
    chip()
    bubble_tail()
    stage()
    board()
    print("generated", sorted(os.listdir(OUT)))
