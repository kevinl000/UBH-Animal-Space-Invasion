"""Rigidly place the accepted crater template at all twelve existing site angles.

This is an offline fit study. Approved terrain, facades and tower art stay intact.
"""
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageChops, ImageDraw

HERE = Path(__file__).resolve().parent
PROTOTYPE = HERE.parent
ROOT = PROTOTYPE.parents[3]
sys.dont_write_bytecode = True
sys.path.insert(0, str(PROTOTYPE))
import study_transforms as review

CENTER = (627, 610)
RADIUS = 330
TEMPLATE_MOUNT = (627, 280)
POSES = (-60, 0, 60)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tile_bounds(mount):
    x, y = mount
    return x-192, y-192, x+192, y+192


def main():
    composition_path = PROTOTYPE.parent / "Composition/composition.json"
    terrain_path = PROTOTYPE.parent / "Circular/terrain-circular.png"
    inputs = [PROTOTYPE / name for name in ("terrain-prototype.png", "site-edit-mask.png", "foreground-soil.png")]
    inputs += [composition_path, terrain_path, review.SWEEP / "site-mounts.json",
               review.SWEEP / "base-sprite.png", review.SWEEP / "gun-sprite.png"]
    hashes = {p.relative_to(ROOT).as_posix(): digest(p) for p in inputs}
    terrain = Image.open(terrain_path).convert("RGBA")
    template = Image.open(inputs[0]).convert("RGBA").crop(tile_bounds(TEMPLATE_MOUNT))
    template_mask = Image.open(inputs[1]).convert("L").crop(tile_bounds(TEMPLATE_MOUNT))
    lip = Image.open(inputs[2]).convert("RGBA").crop(tile_bounds(TEMPLATE_MOUNT))
    # Foreground pixels must belong to the installed patch; its source crop also
    # contains blue terrain outside that patch, which must not cover other sites.
    lip.putalpha(ImageChops.multiply(lip.getchannel("A"), template_mask))
    sites = json.loads((review.SWEEP / "site-mounts.json").read_text())["sites"]
    assert len(sites) == 12
    edited = Image.new("L", terrain.size)
    foreground = Image.new("RGBA", terrain.size)
    mask_overlap = 0
    for site in sites:
        angle = 0 if site["clock_site"] == 12 else site["angle_clockwise_degrees"]
        dx, dy = review.vector(0, -RADIUS, angle)
        site["study_angle"] = angle
        site["study_mount"] = [round(CENTER[0]+dx), round(CENTER[1]+dy)]
        site["original_mount"] = site.pop("mount_xy")
        x, y, _, _ = tile_bounds(site["study_mount"])
        rotated_mask = review.turn(template_mask, angle)
        world_mask = Image.new("L", terrain.size)
        world_mask.paste(rotated_mask, (x, y))
        shared = ImageChops.multiply(edited, world_mask)
        mask_overlap += shared.histogram()[255]
        rotated = review.turn(template, angle)
        # Adjacent upper templates share small grass/blue blending margins.
        # Preserve the accepted top reference there; never crop overlapping soil.
        shared_local = shared.crop((x, y, x+384, y+384))
        previous = terrain.crop((x, y, x+384, y+384))
        shared_bounds = shared_local.getbbox()
        if shared_bounds:
            for py in range(shared_bounds[1], shared_bounds[3]):
                for px in range(shared_bounds[0], shared_bounds[2]):
                    if shared_local.getpixel((px, py)):
                        colors = (previous.getpixel((px, py)), rotated.getpixel((px, py)))
                        assert not any(c[3] >= 128 and c[0] > c[1]*1.12 and c[0] > c[2]*1.45 for c in colors), "Overlapping soil cores"
        available = ImageChops.subtract(world_mask, edited)
        edited = ImageChops.lighter(edited, world_mask)
        terrain.paste(rotated, (x, y), available.crop((x, y, x+384, y+384)))
        foreground.alpha_composite(review.turn(lip, angle), (x, y))
    original = Image.open(terrain_path).convert("RGBA")
    red, green, blue, alpha = original.split()
    soil = ImageChops.multiply(
        ImageChops.subtract(red, green.point(lambda v: min(255, int(v*1.12)))).point(lambda v: 255 if v else 0),
        ImageChops.subtract(red, blue.point(lambda v: min(255, int(v*1.45)))).point(lambda v: 255 if v else 0))
    remaining_soil = ImageChops.multiply(ImageChops.multiply(soil, alpha), ImageChops.invert(edited))
    neighborhood = Image.new("L", (384, 384))
    ImageDraw.Draw(neighborhood).rectangle((107, 147, 277, 257), fill=255)
    delta = ImageChops.difference(terrain, original)
    changed = ImageChops.lighter(ImageChops.lighter(delta.getchannel("R"), delta.getchannel("G")),
                                 ImageChops.lighter(delta.getchannel("B"), delta.getchannel("A")))
    assert ImageChops.multiply(changed, ImageChops.invert(edited)).getbbox() is None
    top = tile_bounds(TEMPLATE_MOUNT)
    # The top site must remain the exact accepted reference in its edit region.
    top_delta = ImageChops.difference(terrain.crop(top), Image.open(inputs[0]).convert("RGBA").crop(top))
    assert all(ImageChops.multiply(c, template_mask).getbbox() is None for c in top_delta.split())
    terrain.save(HERE / "terrain-fit.png")
    foreground.save(HERE / "foreground-fit.png")
    edited.save(HERE / "site-masks.png")
    manifest = json.loads(composition_path.read_text())
    rear = Image.new("RGBA", terrain.size)
    for part in [manifest["guild_support"], *manifest["structures"]]:
        path = composition_path.parent / part["file"]
        hashes[path.resolve().relative_to(ROOT).as_posix()] = digest(path)
        rear.alpha_composite(Image.open(path).convert("RGBA"), tuple(part["origin_xy"]))
    empty = rear.copy()
    empty.alpha_composite(terrain)
    empty.save(HERE / "planet-empty-fit.png")
    base = Image.new("RGBA", (384, 384))
    base.alpha_composite(Image.open(review.SWEEP / "base-sprite.png"), (158, 121))
    base = review.scale_about_mount(base, 1.5)
    gun_source = Image.new("RGBA", (256, 256))
    gun_source.alpha_composite(Image.open(review.SWEEP / "gun-sprite.png"), (116, 100))
    patches = [review.solid(terrain.crop(tile_bounds(s["study_mount"]))) for s in sites]
    lip_masks = [review.solid(foreground.crop(tile_bounds(s["study_mount"]))) for s in sites]
    checks = [{"site": s["clock_site"], "mount_xy": s["study_mount"],
               "max_gun_terrain_pixels": 0, "max_ray_terrain_pixels": 0,
               "max_gun_foreground_pixels": 0, "failing_angles": [],
               "old_soil_pixels_outside_template": review.overlap(
                   remaining_soil.crop(tile_bounds(s["study_mount"])), review.turn(neighborhood, s["study_angle"])),
               "hidden_base_pixels": review.overlap(review.solid(review.turn(base, s["study_angle"])), lip_masks[i])}
              for i, s in enumerate(sites)]
    composites = {}
    for step in range(-120, 121):
        aim = step / 2
        gun = Image.new("RGBA", (384, 384))
        gun.alpha_composite(gun_source.rotate(-aim, review.NEAREST, center=(128, 128)), (94, 17))
        gun = review.scale_about_mount(gun, 1.5)
        for i, site in enumerate(sites):
            weapon = review.solid(review.turn(gun, site["study_angle"]))
            gun_hits = review.overlap(weapon, patches[i])
            ray_hits = review.overlap(review.ray(1.5, aim, site["study_angle"]), patches[i])
            for key, value in (("max_gun_terrain_pixels", gun_hits), ("max_ray_terrain_pixels", ray_hits),
                               ("max_gun_foreground_pixels", review.overlap(weapon, lip_masks[i]))):
                checks[i][key] = max(checks[i][key], value)
            if gun_hits or ray_hits:
                checks[i]["failing_angles"].append(aim)
        if aim in POSES:
            local = base.copy()
            local.alpha_composite(gun)
            composite = empty.copy()
            for site in sites:
                composite.alpha_composite(review.turn(local, site["study_angle"]), tile_bounds(site["study_mount"])[:2])
            composite.alpha_composite(foreground)
            composites[int(aim)] = composite
    composites[0].save(HERE / "planet-occupied-fit.png")
    sheet = Image.new("RGB", (2880, 840), review.BACKGROUND)
    for i, site in enumerate(sites):
        dx, dy = review.vector(0, -40, site["study_angle"])
        cx, cy = round(site["study_mount"][0]+dx), round(site["study_mount"][1]+dy)
        for j, aim in enumerate(POSES):
            x, y = (i % 4)*720+j*240, (i // 4)*280
            patch = composites[aim].crop((cx-120, cy-120, cx+120, cy+120))
            sheet.paste(review.backdrop(patch), (x, y+32))
            ImageDraw.Draw(sheet).text((x+8, y+10), f"Site {site['clock_site']:02d} | {aim:+d} deg | 150%", fill="white")
    sheet.save(HERE / "all-sites-aiming.png")
    detail = Image.new("RGB", (880, 1856), review.BACKGROUND)
    for row, number in enumerate((1, 3, 6, 10)):
        site = next(s for s in sites if s["clock_site"] == number)
        dx, dy = review.vector(0, -40, site["study_angle"])
        cx, cy = round(site["study_mount"][0]+dx), round(site["study_mount"][1]+dy)
        for col, (label, im) in enumerate((("empty", empty), ("occupied", composites[0]))):
            patch = im.crop((cx-110, cy-110, cx+110, cy+110))
            detail.paste(review.backdrop(patch).resize((440, 440), review.NEAREST), (col*440, row*464+24))
            ImageDraw.Draw(detail).text((col*440+10, row*464+6), f"Site {number:02d} | {label} | 2x nearest", fill="white")
    detail.save(HERE / "orientation-closeups.png")
    page = Image.new("RGB", (1280, 672), review.BACKGROUND)
    for i, (label, im) in enumerate((("Shared crater profile | empty", empty), ("Shared crater profile | 150% Laser", composites[0]))):
        page.paste(review.backdrop(im.resize((640, 640), review.NEAREST)), (i*640, 32))
        ImageDraw.Draw(page).text((i*640+12, 10), label+" | offline", fill="white")
    page.save(HERE / "page-review.png")
    for relative, expected in hashes.items():
        assert digest(ROOT / relative) == expected, relative
    result = {"status": "Twelve-site offline fit review; approved sources unchanged",
              "source_sha256": hashes, "planet_center": CENTER, "common_mount_radius": RADIUS,
              "tower_scale": 1.5, "template_scale": 1, "placement_rule": "Existing site angles; common radius from accepted top mount",
              "site_placements": sites, "angle_samples_per_site": 241, "site_angle_combinations": 2892,
              "shared_margin_pixels": mask_overlap,
              "old_soil_detection": "Original orange/brown opaque terrain outside the applied masks; local x +/-85, y -45..65 around each mount",
              "margin_rule": "Top reference first, then clockwise; shared grass/blue margins retain the earlier patch; overlapping soil cores rejected",
              "checks": checks,
              "limitations": ["Offline raster sampling; thin rays do not validate projectile width",
                              "Empty recesses may expose incomplete rear facade surfaces",
                              "Original soil outside the common edit mask can remain; inspect perimeter transitions",
                              "No Unity import, gameplay change or accepted planet replacement"]}
    (HERE / "fit-study.json").write_text(json.dumps(result, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(checks, indent=2))


if __name__ == "__main__":
    main()
