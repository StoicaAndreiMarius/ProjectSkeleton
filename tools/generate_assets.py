"""
Asset generator for "The Adventure - Coin Run".

Produces every PNG, the Tiled map/tileset JSON, and the sprite-sheet definition
JSON the game consumes, into ../Assets. Re-runnable and deterministic.

These are GENERATED ASSETS (disclosed in AI_USAGE.md). They do not count toward
the C# authorship cap. Requires Pillow:  pip install pillow
"""

import json
import os
import random

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "..", "Assets"))
os.makedirs(ASSETS, exist_ok=True)
random.seed(1234)


def out(name):
    return os.path.join(ASSETS, name)


def save_json(name, obj):
    with open(out(name), "w", encoding="utf-8") as f:
        json.dump(obj, f, indent=2)


def load_font(size):
    for candidate in ("arialbd.ttf", "arial.ttf", "segoeui.ttf"):
        path = os.path.join("C:\\Windows\\Fonts", candidate)
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


# --------------------------------------------------------------------------- #
# Terrain tiles + Tiled map
# --------------------------------------------------------------------------- #
def make_grass():
    bases = [(86, 150, 70), (78, 142, 64), (94, 158, 78), (70, 134, 60)]
    for i, base in enumerate(bases, start=1):
        img = Image.new("RGBA", (16, 16), base + (255,))
        d = ImageDraw.Draw(img)
        for _ in range(22):
            x, y = random.randint(0, 15), random.randint(0, 15)
            shade = random.choice([-14, -8, 10, 16])
            c = tuple(max(0, min(255, base[k] + shade)) for k in range(3))
            d.point((x, y), fill=c + (255,))
        # a few darker grass blades
        for _ in range(5):
            x = random.randint(1, 14)
            y = random.randint(4, 14)
            d.line((x, y, x, y - random.randint(2, 4)), fill=(50, 100, 45, 255))
        img.save(out(f"grass_{i:03d}.png"))


def make_tiled_map():
    tileset = {
        "name": "grass",
        "tilecount": 4,
        "tilewidth": 16,
        "tileheight": 16,
        "tiles": [
            {"id": 0, "image": "grass_001.png", "imageheight": 16, "imagewidth": 16},
            {"id": 1, "image": "grass_002.png", "imageheight": 16, "imagewidth": 16},
            {"id": 2, "image": "grass_003.png", "imageheight": 16, "imagewidth": 16},
            {"id": 3, "image": "grass_004.png", "imageheight": 16, "imagewidth": 16},
        ],
    }
    save_json("grass.tsj", tileset)

    width, height = 60, 40
    data = []
    for _ in range(width * height):
        # mostly tile 1, occasional variants for texture
        roll = random.random()
        if roll < 0.80:
            data.append(1)
        elif roll < 0.90:
            data.append(2)
        elif roll < 0.96:
            data.append(3)
        else:
            data.append(4)

    level = {
        "height": height,
        "width": width,
        "tileheight": 16,
        "tilewidth": 16,
        "orientation": "orthogonal",
        "renderorder": "right-down",
        "type": "map",
        "version": "1.10",
        "layers": [
            {
                "data": data,
                "height": height,
                "width": width,
                "name": "Tile Layer 1",
                "type": "tilelayer",
                "opacity": 1,
                "visible": True,
                "x": 0,
                "y": 0,
                "id": 1,
            }
        ],
        "tilesets": [{"firstgid": 1, "source": "grass.tsj"}],
    }
    save_json("terrain.tmj", level)


# --------------------------------------------------------------------------- #
# Player sprite sheet (10 rows x 6 cols, 48x48)
# --------------------------------------------------------------------------- #
SKIN = (242, 206, 162, 255)
HAIR = (96, 64, 36, 255)
TUNIC = (60, 120, 205, 255)
TUNIC_D = (44, 92, 160, 255)
PANTS = (74, 58, 44, 255)
BOOT = (40, 32, 26, 255)


def draw_player_frame(img, direction, phase):
    d = ImageDraw.Draw(img)
    cx = 24
    swing = [0, 2, 0, -2, 0, 2][phase % 6]
    bob = [0, -1, 0, -1, 0, -1][phase % 6]

    # shadow
    d.ellipse((14, 41, 34, 47), fill=(0, 0, 0, 70))
    # legs
    d.rectangle((18, 33 + max(0, swing), 23, 42), fill=PANTS)
    d.rectangle((25, 33 + max(0, -swing), 30, 42), fill=PANTS)
    d.rectangle((18, 41, 23, 44), fill=BOOT)
    d.rectangle((25, 41, 30, 44), fill=BOOT)
    # body
    d.rounded_rectangle((16, 20 + bob, 32, 35 + bob), radius=4, fill=TUNIC)
    d.rectangle((16, 30 + bob, 32, 35 + bob), fill=TUNIC_D)
    # head
    hy = 8 + bob
    d.ellipse((16, hy, 32, hy + 16), fill=SKIN)
    # hair cap
    d.pieslice((16, hy - 2, 32, hy + 16), start=180, end=360, fill=HAIR)

    eye = (30, 30, 36, 255)
    if direction == "down":
        d.rectangle((20, hy + 9, 22, hy + 11), fill=eye)
        d.rectangle((26, hy + 9, 28, hy + 11), fill=eye)
    elif direction == "up":
        pass  # back of head
    elif direction == "right":
        d.rectangle((27, hy + 9, 29, hy + 11), fill=eye)
        d.rectangle((22, hy + 9, 24, hy + 11), fill=eye)
        d.polygon([(32, hy + 6), (36, hy + 9), (32, hy + 12)], fill=SKIN)  # nose


def make_player():
    cols, rows, fw, fh = 6, 10, 48, 48
    sheet = Image.new("RGBA", (cols * fw, rows * fh), (0, 0, 0, 0))
    # row -> direction used by Player.json
    row_dir = {0: "down", 3: "down", 4: "right", 5: "up"}
    for r in range(rows):
        direction = row_dir.get(r, "down")
        for c in range(cols):
            frame = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
            draw_player_frame(frame, direction, c)
            sheet.paste(frame, (c * fw, r * fh))
    sheet.save(out("player.png"))

    save_json("Player.json", {
        "rowCount": 10, "columnCount": 6,
        "frameWidth": 48, "frameHeight": 48,
        "frameCenter": {"offsetX": 24, "offsetY": 42},
        "fileName": "player.png",
        "animations": {
            "IdleDown":  {"startFrame": {"row": 0, "col": 0}, "endFrame": {"row": 0, "col": 5}, "durationMs": 1000, "loop": True},
            "MoveLeft":  {"startFrame": {"row": 4, "col": 0}, "endFrame": {"row": 4, "col": 5}, "durationMs": 700, "loop": True, "flip": 1},
            "MoveRight": {"startFrame": {"row": 4, "col": 0}, "endFrame": {"row": 4, "col": 5}, "durationMs": 700, "loop": True, "flip": 0},
            "MoveDown":  {"startFrame": {"row": 3, "col": 0}, "endFrame": {"row": 3, "col": 5}, "durationMs": 700, "loop": True, "flip": 0},
            "MoveUp":    {"startFrame": {"row": 5, "col": 0}, "endFrame": {"row": 5, "col": 5}, "durationMs": 700, "loop": True, "flip": 0},
        },
    })


# --------------------------------------------------------------------------- #
# Coin sprite sheet (1 row x 6 cols, 16x16) - spinning
# --------------------------------------------------------------------------- #
def make_coin():
    cols, fw, fh = 6, 16, 16
    sheet = Image.new("RGBA", (cols * fw, fh), (0, 0, 0, 0))
    widths = [12, 9, 5, 2, 5, 9]
    for c in range(cols):
        f = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
        d = ImageDraw.Draw(f)
        w = widths[c]
        x0 = 8 - w // 2
        d.ellipse((x0, 2, x0 + w, 13), fill=(255, 205, 50, 255), outline=(190, 140, 20, 255))
        if w > 4:
            d.ellipse((x0 + 1, 4, x0 + max(2, w - 2), 8), fill=(255, 235, 140, 255))
        sheet.paste(f, (c * fw, 0))
    sheet.save(out("coin.png"))
    save_json("Coin.json", {
        "rowCount": 1, "columnCount": 6,
        "frameWidth": 16, "frameHeight": 16,
        "frameCenter": {"offsetX": 8, "offsetY": 8},
        "fileName": "coin.png",
        "animations": {
            "Spin": {"startFrame": {"row": 0, "col": 0}, "endFrame": {"row": 0, "col": 5}, "durationMs": 700, "loop": True},
        },
    })


# --------------------------------------------------------------------------- #
# Slime enemy sheet (1 row x 4 cols, 32x32) - squishing
# --------------------------------------------------------------------------- #
def make_slime():
    cols, fw, fh = 4, 32, 32
    sheet = Image.new("RGBA", (cols * fw, fh), (0, 0, 0, 0))
    squish = [0, 3, 0, 2]
    for c in range(cols):
        f = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
        d = ImageDraw.Draw(f)
        s = squish[c]
        d.ellipse((4, 27, 28, 31), fill=(0, 0, 0, 70))
        top = 8 + s
        d.ellipse((4, top, 28, 30), fill=(120, 200, 110, 255), outline=(70, 150, 70, 255))
        d.ellipse((9, top + 3, 16, top + 9), fill=(180, 230, 170, 200))
        d.ellipse((12, top + 6, 16, top + 11), fill=(30, 40, 30, 255))
        d.ellipse((19, top + 6, 23, top + 11), fill=(30, 40, 30, 255))
        sheet.paste(f, (c * fw, 0))
    sheet.save(out("slime.png"))
    save_json("Slime.json", {
        "rowCount": 1, "columnCount": 4,
        "frameWidth": 32, "frameHeight": 32,
        "frameCenter": {"offsetX": 16, "offsetY": 26},
        "fileName": "slime.png",
        "animations": {
            "Move": {"startFrame": {"row": 0, "col": 0}, "endFrame": {"row": 0, "col": 3}, "durationMs": 600, "loop": True},
        },
    })


# --------------------------------------------------------------------------- #
# Bomb explosion sheet (1 row x 13 cols, 32x64)
# --------------------------------------------------------------------------- #
def make_bomb():
    cols, fw, fh = 13, 32, 64
    sheet = Image.new("RGBA", (cols * fw, fh), (0, 0, 0, 0))
    cx, cy = 16, 44
    for c in range(cols):
        f = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
        d = ImageDraw.Draw(f)
        t = c / (cols - 1)
        if c < 3:
            # short fuse: a dark bomb with a spark
            d.ellipse((9, 40, 23, 54), fill=(40, 40, 45, 255))
            d.line((16, 40, 19, 33), fill=(120, 90, 40, 255), width=2)
            spark = (255, 230, 120, 255) if c % 2 == 0 else (255, 160, 40, 255)
            d.ellipse((17, 30, 21, 34), fill=spark)
        else:
            radius = int(6 + t * 15)
            alpha = max(0, int(255 * (1.0 - (t - 0.2))))
            d.ellipse((cx - radius, cy - radius, cx + radius, cy + radius),
                      fill=(255, 140, 30, alpha))
            d.ellipse((cx - radius + 4, cy - radius + 4, cx + radius - 4, cy + radius - 4),
                      fill=(255, 220, 90, min(255, alpha)))
            for _ in range(6):
                ang = random.random() * 6.28
                rr = radius + random.randint(0, 5)
                px = int(cx + rr * 0.9 * (1 if random.random() > 0.5 else -1) * random.random())
                py = int(cy + rr * 0.9 * (1 if random.random() > 0.5 else -1) * random.random())
                d.ellipse((px - 1, py - 1, px + 1, py + 1), fill=(255, 90, 20, alpha))
        sheet.paste(f, (c * fw, 0))
    sheet.save(out("BombExploding.png"))
    save_json("BombExploding.json", {
        "rowCount": 1, "columnCount": 13,
        "frameWidth": 32, "frameHeight": 64,
        "frameCenter": {"offsetX": 16, "offsetY": 48},
        "fileName": "BombExploding.png",
        "animations": {
            "Explode": {"startFrame": {"row": 0, "col": 0}, "endFrame": {"row": 0, "col": 12}, "durationMs": 1000, "loop": False},
        },
    })


# --------------------------------------------------------------------------- #
# HUD: digits, heart, banners
# --------------------------------------------------------------------------- #
def make_digits():
    cell_w, cell_h = 30, 42
    font = load_font(34)
    sheet = Image.new("RGBA", (cell_w * 10, cell_h), (0, 0, 0, 0))
    d = ImageDraw.Draw(sheet)
    for n in range(10):
        s = str(n)
        bbox = d.textbbox((0, 0), s, font=font)
        tw = bbox[2] - bbox[0]
        th = bbox[3] - bbox[1]
        x = n * cell_w + (cell_w - tw) // 2 - bbox[0]
        y = (cell_h - th) // 2 - bbox[1]
        d.text((x, y), s, font=font, fill=(20, 20, 20, 255),
               stroke_width=3, stroke_fill=(20, 20, 20, 255))
        d.text((x, y), s, font=font, fill=(255, 230, 90, 255))
    sheet.save(out("digits.png"))


def make_heart():
    img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    red = (220, 50, 60, 255)
    d.ellipse((3, 4, 13, 14), fill=red)
    d.ellipse((11, 4, 21, 14), fill=red)
    d.polygon([(4, 11), (20, 11), (12, 21)], fill=red)
    d.ellipse((6, 6, 10, 10), fill=(255, 150, 160, 200))
    img.save(out("heart.png"))


def make_banner(name, text, color):
    w, h = 520, 140
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=18, fill=(20, 20, 30, 210),
                        outline=color + (255,), width=4)
    font = load_font(64)
    bbox = d.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    d.text(((w - tw) // 2 - bbox[0], (h - th) // 2 - bbox[1] - 6), text, font=font,
           fill=color + (255,), stroke_width=2, stroke_fill=(0, 0, 0, 255))
    sub = load_font(22)
    s = "Press R to play again"
    sb = d.textbbox((0, 0), s, font=sub)
    d.text(((w - (sb[2] - sb[0])) // 2 - sb[0], h - 34), s, font=sub, fill=(220, 220, 220, 255))
    img.save(out(name))


def main():
    make_grass()
    make_tiled_map()
    make_player()
    make_coin()
    make_slime()
    make_bomb()
    make_digits()
    make_heart()
    make_banner("you_win.png", "YOU WIN!", (90, 220, 110))
    make_banner("game_over.png", "GAME OVER", (230, 80, 90))
    print("Assets written to", ASSETS)


if __name__ == "__main__":
    main()
