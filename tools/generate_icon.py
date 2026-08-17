"""生成像素风应用图标 app.ico（PNG-in-ICO，16/32/48/64）。

主题「远眺」：暖纸卡片上，一只眼睛望向青色远山与琥珀暖阳，
呼应 20-20-20 护眼提醒（远眺 20 秒）。
"""

import struct
import zlib
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "src" / "健康助手" / "Resources" / "app.ico"

PALETTE = {
    ".": (0, 0, 0, 0),
    "#": (74, 56, 39, 255),
    "F": (242, 230, 205, 255),
    "P": (239, 226, 198, 255),
    "C": (255, 247, 232, 255),
    "R": (217, 79, 48, 255),
    "A": (232, 163, 61, 255),
    "I": (59, 46, 35, 255),
    "G": (111, 168, 107, 255),
    "g": (22, 101, 52, 255),
}

BASE = [
    "................",
    ".##############.",
    ".#AFFFFFFFFAAF#.",
    ".#FPFFFFFFFAAF#.",
    ".#....####....#.",
    ".#..#CCCCCC#..#.",
    ".#..#CAIIAC#..#.",
    ".#..#CCCCCC#..#.",
    ".#....####....#.",
    ".#FFFFFFFFFFFP#.",
    ".#...GG...GG...#.",
    ".#..GGGG.GGGG..#.",
    ".#.gGGGGgGGGGg.#.",
    ".#gGGGGGGGGGGg#.",
    ".##############.",
    "................",
]


def png_chunk(tag: bytes, data: bytes) -> bytes:
    return (
        struct.pack(">I", len(data))
        + tag
        + data
        + struct.pack(">I", zlib.crc32(tag + data))
    )


def make_png(pixels: list[list[tuple[int, int, int, int]]], size: int) -> bytes:
    raw = b"".join(
        b"\x00" + b"".join(bytes(pixels[y][x]) for x in range(size))
        for y in range(size)
    )
    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    return (
        b"\x89PNG\r\n\x1a\n"
        + png_chunk(b"IHDR", ihdr)
        + png_chunk(b"IDAT", zlib.compress(raw, 9))
        + png_chunk(b"IEND", b"")
    )


def upscale(size: int) -> list[list[tuple[int, int, int, int]]]:
    return [
        [PALETTE[BASE[y * 16 // size][x * 16 // size]] for x in range(size)]
        for y in range(size)
    ]


def main() -> None:
    images = [(size, make_png(upscale(size), size)) for size in (16, 32, 48, 64)]

    header = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b""
    payload = b""
    for size, png in images:
        entries += struct.pack(
            "<BBBBHHII", size, size, 0, 0, 1, 32, len(png), offset
        )
        payload += png
        offset += len(png)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_bytes(header + entries + payload)
    print(f"已生成 {OUT}（{len(images)} 个尺寸）")


if __name__ == "__main__":
    main()
