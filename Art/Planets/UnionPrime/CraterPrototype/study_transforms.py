"""Shared raster transforms for the accepted Laser/crater fit studies; Pillow required."""
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
SWEEP = ROOT / "Art/Towers/Laser/Redesign/Sweep"
NEAREST = Image.Resampling.NEAREST
SIZE = 384
CENTER = SIZE // 2
BACKGROUND = (42, 49, 60)


def solid(image):
    return image.getchannel("A").point(lambda a: 255 if a >= 128 else 0)


def overlap(a, b):
    return ImageChops.multiply(a, b).histogram()[255]


def turn(image, angle):
    return image.rotate(-angle, NEAREST, center=(CENTER, CENTER))


def scale_about_mount(image, scale):
    return image.transform(image.size, Image.Transform.AFFINE,
                           (1 / scale, 0, CENTER * (1 - 1 / scale),
                            0, 1 / scale, CENTER * (1 - 1 / scale)), NEAREST)


def backdrop(image):
    result = Image.new("RGBA", image.size, (*BACKGROUND, 255))
    result.alpha_composite(image)
    return result.convert("RGB")


def vector(x, y, angle):
    theta = math.radians(angle)
    return (x * math.cos(theta) - y * math.sin(theta),
            x * math.sin(theta) + y * math.cos(theta))


def ray(scale, aim, orientation):
    # An actual one-pixel ray, not a scaled strip; extend beyond the local terrain.
    dx, dy = vector(0, -25, aim)
    x, y = vector(scale * (30 + dx), scale * (-47 + dy), orientation)
    ex, ey = vector(0, -SIZE * 2, aim + orientation)
    result = Image.new("L", (SIZE, SIZE))
    ImageDraw.Draw(result).line((round(CENTER + x), round(CENTER + y),
                                 round(CENTER + x + ex), round(CENTER + y + ey)),
                                fill=255, width=1)
    return result
