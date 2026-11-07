"""Assemble one generated crater patch with unchanged planet and tower sources.

The source art is generated with imagegen; this script selects its site-local
pixels, assembles separate layers, and samples the accepted aiming mechanism.
"""
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.dont_write_bytecode = True
import study_transforms as review

CROP = (499, 205, 755, 365)
MOUNT = (627, 280)
NEAREST = Image.Resampling.NEAREST


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    terrain_path = HERE.parent / "Circular/terrain-circular.png"
    composition_path = HERE.parent / "Composition/composition.json"
    planet_path = HERE.parent / "Composition/planet-assembled.png"
    sources = [terrain_path, composition_path, planet_path, HERE / "crater-generated.png",
               review.SWEEP / "base-sprite.png", review.SWEEP / "gun-sprite.png"]
    hashes = {p.relative_to(ROOT).as_posix(): digest(p) for p in sources}
    approved = list((HERE.parent / "Circular").glob("*.png"))
    approved += list((HERE.parent / "Composition").glob("*.png"))
    approved += list(review.SWEEP.glob("*.png")) + list(review.SWEEP.glob("*.gif"))
    preserved = {p: digest(p) for p in approved}
    original = Image.open(terrain_path).convert("RGBA")
    generated = Image.open(HERE / "crater-generated.png").convert("RGBA").resize((256, 160), NEAREST)
    # Keep the generated source untouched; remove near-transparent fringe specks
    # only in the packed patch. No background color is introduced.
    alpha = generated.getchannel("A")
    generated.putalpha(ImageChops.multiply(alpha, alpha.point(lambda a: 255 if a >= 128 else 0)))
    old = original.crop(CROP)
    selection = Image.new("L", old.size)
    for y in range(30, 104):
        for x in range(52, 204):
            r, g, b, a = old.getpixel((x, y))
            nr, ng, nb, na = generated.getpixel((x, y))
            old_soil = a >= 128 and r > g * 1.12 and r > b * 1.45
            new_soil = na >= 128 and nr > ng * 1.12 and nr > nb * 1.45
            new_grass = na >= 128 and ng > nr * 1.1 and ng > nb * 1.2 and y < 59
            opening = 67 <= x <= 187 and y <= 79 and a > 0 and na == 0
            if old_soil or new_soil or new_grass or opening:
                selection.putpixel((x, y), 255)
    # Include the old patch's grassy perimeter so it cannot leave a second rim.
    selection = selection.filter(ImageFilter.MaxFilter(11))
    revised = original.copy()
    revised.paste(generated, CROP[:2], selection)
    change_mask = Image.new("L", original.size)
    change_mask.paste(selection, CROP[:2])
    revised.save(HERE / "terrain-prototype.png")
    change_mask.save(HERE / "site-edit-mask.png")
    difference = ImageChops.difference(original, revised)
    changed = ImageChops.lighter(ImageChops.lighter(difference.getchannel("R"), difference.getchannel("G")),
                                 ImageChops.lighter(difference.getchannel("B"), difference.getchannel("A")))
    assert ImageChops.multiply(changed, ImageChops.invert(change_mask)).getbbox() is None

    # Reassemble rear facades so newly transparent parts reveal existing scenery.
    manifest = json.loads(composition_path.read_text())
    rear = Image.new("RGBA", original.size)
    for part in [manifest["guild_support"], *manifest["structures"]]:
        path = composition_path.parent / part["file"]
        hashes[path.resolve().relative_to(ROOT).as_posix()] = digest(path)
        rear.alpha_composite(Image.open(path).convert("RGBA"), tuple(part["origin_xy"]))
    baseline = rear.copy()
    baseline.alpha_composite(original)
    assert baseline.tobytes() == Image.open(planet_path).convert("RGBA").tobytes()
    empty = rear.copy()
    empty.alpha_composite(revised)
    empty.save(HERE / "planet-empty-prototype.png")

    # The foreground is the irregular generated floor surface, not a new stripe.
    # Retain its actual upper alpha contour; the rectangular window is below it.
    foreground = Image.new("RGBA", original.size)
    foreground.paste(revised.crop((570, 266, 685, 300)), (570, 266))
    foreground.save(HERE / "foreground-soil.png")
    base = Image.new("RGBA", (384, 384))
    base.alpha_composite(Image.open(review.SWEEP / "base-sprite.png"), (192-34, 192-71))
    base = review.scale_about_mount(base, 1.5)
    gun_source = Image.new("RGBA", (256, 256))
    gun_source.alpha_composite(Image.open(review.SWEEP / "gun-sprite.png"), (116, 100))
    bounds = (MOUNT[0]-192, MOUNT[1]-192, MOUNT[0]+192, MOUNT[1]+192)
    terrain_mask = review.solid(revised.crop(bounds))
    foreground_mask = review.solid(foreground.crop(bounds))
    hidden_base = review.overlap(review.solid(base), foreground_mask)
    checks = {"samples": 241, "max_gun_terrain_pixels": 0, "max_ray_terrain_pixels": 0,
              "max_gun_foreground_pixels": 0, "hidden_base_pixels": hidden_base,
              "failing_angles": []}
    poses = {}
    for step in range(-120, 121):
        aim = step / 2
        gun = Image.new("RGBA", (384, 384))
        gun.alpha_composite(gun_source.rotate(-aim, NEAREST, center=(128, 128)), (94, 17))
        gun = review.scale_about_mount(gun, 1.5)
        gun_hits = review.overlap(review.solid(gun), terrain_mask)
        ray_hits = review.overlap(review.ray(1.5, aim, 0), terrain_mask)
        checks["max_gun_terrain_pixels"] = max(checks["max_gun_terrain_pixels"], gun_hits)
        checks["max_ray_terrain_pixels"] = max(checks["max_ray_terrain_pixels"], ray_hits)
        checks["max_gun_foreground_pixels"] = max(checks["max_gun_foreground_pixels"],
                                                 review.overlap(review.solid(gun), foreground_mask))
        if gun_hits or ray_hits:
            checks["failing_angles"].append(aim)
        if aim in (-60, 0, 60):
            tower = base.copy()
            tower.alpha_composite(gun)
            occupied = empty.copy()
            occupied.alpha_composite(tower, bounds[:2])
            occupied.alpha_composite(foreground)
            poses[int(aim)] = occupied
            if aim == 0:
                before = baseline.copy()
                before.alpha_composite(tower, bounds[:2])
                before_baseline = baseline.copy()
                before_baseline.alpha_composite(tower, (627-192, 267-192))
    assert not checks["failing_angles"], checks
    assert checks["max_gun_foreground_pixels"] == 0 and hidden_base > 0, checks
    poses[0].save(HERE / "planet-occupied-prototype.png")
    closeup_bounds = (499, 125, 755, 365)
    panels = [("Current 150% / existing mount", before_baseline),
              ("Same deeper mount / old crater", before),
              ("Prototype / empty", empty), ("Prototype / occupied", poses[0])]
    closeups = Image.new("RGB", (1536, 1504), review.BACKGROUND)
    for i, (label, im) in enumerate(panels):
        x, y = (i % 2)*768, (i // 2)*752
        closeups.paste(review.backdrop(im.crop(closeup_bounds)).resize((768, 720), NEAREST), (x, y+32))
        ImageDraw.Draw(closeups).text((x+12, y+10), label+" | 3x nearest", fill="white")
    closeups.save(HERE / "crater-comparison.png")
    sweep = Image.new("RGB", (1536, 512), review.BACKGROUND)
    for i, aim in enumerate((-60, 0, 60)):
        sweep.paste(review.backdrop(poses[aim].crop(closeup_bounds)).resize((512, 480), NEAREST), (i*512, 32))
        ImageDraw.Draw(sweep).text((i*512+12, 10), f"150% | {aim:+d} degrees | top-site prototype", fill="white")
    sweep.save(HERE / "aiming-review.png")
    page = Image.new("RGB", (1280, 672), review.BACKGROUND)
    for i, (label, im) in enumerate((("Original site / selected 150%", before_baseline),
                                    ("Prototype top crater / 150%", poses[0]))):
        page.paste(review.backdrop(im.resize((640, 640), NEAREST)), (i*640, 32))
        ImageDraw.Draw(page).text((i*640+12, 10), label+" | offline", fill="white")
    page.save(HERE / "page-review.png")
    for path, expected in preserved.items():
        assert digest(path) == expected, path
    metadata = {"status": "User accepted top-site prototype; other-site fit checks pending",
                "source_sha256": hashes, "source_crop_xyxy": CROP,
                "site": 12, "tower_scale": 1.5, "tower_mount_xy": MOUNT,
                "inward_pixels_from_previous_mount": 13,
                "site_orientation_degrees": 0,
                "orientation_note": "Top-site prototype uses exact local up; previous study used 359.974 degrees",
                "changed_bounds_xyxy": changed.getbbox(),
                "foreground_crop_xyxy": [570, 266, 685, 300],
                "layer_order": ["original facades", "prototype terrain", "tower base", "tower gun", "foreground soil"],
                "checks": checks, "preserved_reference_images": len(preserved),
                "limitations": ["Only site 12 revised; other 11 sites and approved source files unchanged",
                                "Newly exposed hidden facade surfaces are limited to original extracted art",
                                "Offline sampled raster clearance, not Unity or projectile-width validation",
                                "Generated patch is downsampled; production palette and pixel density still pending"]}
    (HERE / "prototype.json").write_text(json.dumps(metadata, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(metadata, indent=2))


if __name__ == "__main__":
    main()
