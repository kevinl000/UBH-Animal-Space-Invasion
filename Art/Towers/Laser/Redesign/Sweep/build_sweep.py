"""Pack generated parts and render an offline assembly study. Requires Pillow.

Only cropping, uniform scaling and rigid transforms are used; no source art is
repainted. Coordinates are pixel-edge coordinates from each image's top left.
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
NEAREST = Image.Resampling.NEAREST
BASE_HINGE = (64, 24)
BASE_MOUNT = (34, 71)
BARREL_HINGE = (12, 28)
MUZZLE = (14.5, 0)
ORIGIN = (128, 128)
ANGLES = (-60, -30, 0, 30, 60)
OUTWARD_MOUNT_OFFSET = 16


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pack(part):
    source = Image.open(HERE / part["source"]).convert("RGBA")
    export = part["export"]
    scale = export["uniform_scale"]
    sx, sy = export["source_anchor_top_left_pixels"]
    dx, dy = export["destination_anchor_top_left_pixels"]
    # Uniform nearest-neighbor scaling and translation only; never repaint art.
    sprite = source.transform(tuple(part["size"]), Image.Transform.AFFINE,
                              (1/scale, 0, sx-dx/scale, 0, 1/scale, sy-dy/scale), NEAREST)
    return sprite, export["source_bounds_alpha128"]


def canvas():
    return Image.new("RGBA", (256, 256))


def backdrop(im):
    result = Image.new("RGBA", im.size, (42, 49, 60, 255))
    result.alpha_composite(im)
    return result.convert("RGB")


def offset(point, origin):
    return tuple(a - b for a, b in zip(point, origin))


def turn(im, clockwise):
    return im.rotate(-clockwise, NEAREST, center=ORIGIN)


def main():
    setup = json.loads((HERE / "assembly.json").read_text())
    planet_path = ROOT / "Art/Planets/UnionPrime/planet-empty.png"
    terrain_path = ROOT / "Art/Planets/UnionPrime/terrain.png"
    inputs = [HERE / setup["base"]["source"], HERE / setup["weapon"]["source"],
              HERE / "site-mounts.json", planet_path, terrain_path]
    hashes = {path.relative_to(ROOT).as_posix(): digest(path) for path in inputs}
    base, base_crop = pack(setup["base"])
    barrel, barrel_crop = pack(setup["weapon"])
    assert base.size == (70, 72) and barrel.size == (24, 33)
    base.save(HERE / "base-sprite.png")
    barrel.save(HERE / "gun-sprite.png")
    base_layer, weapon_layer = canvas(), canvas()
    base_layer.alpha_composite(base, offset(ORIGIN, BASE_HINGE))
    weapon_layer.alpha_composite(barrel, offset(ORIGIN, BARREL_HINGE))

    def assembly(angle):
        result = base_layer.copy()
        result.alpha_composite(turn(weapon_layer, angle))
        return result

    # The rounded breech covers the socket and upper end of the angled bracket.
    # Exclude that measured joint region, not the rabbit or the chassis.
    protected = base_layer.getchannel("A").point(lambda a: 255 if a >= 128 else 0)
    ImageDraw.Draw(protected).ellipse((116, 116, 140, 140), fill=0)
    overlaps = {}
    shot_overlaps = {}
    wide_shot_overlaps = {}
    joint_contacts = {}
    muzzle_ray = Image.new("L", base_layer.size)
    muzzle_on_canvas = tuple(o + m - h for o, m, h in zip(ORIGIN, MUZZLE, BARREL_HINGE))
    ImageDraw.Draw(muzzle_ray).line((muzzle_on_canvas[0], 0, *muzzle_on_canvas), fill=255, width=1)
    # Seven logical pixels conservatively cover the current ~5.2-pixel bolt width.
    wide_muzzle_ray = Image.new("L", base_layer.size)
    ImageDraw.Draw(wide_muzzle_ray).line((muzzle_on_canvas[0], 0, *muzzle_on_canvas), fill=255, width=7)
    solid_base = base_layer.getchannel("A").point(lambda a: 255 if a >= 128 else 0)
    # The operator screen remains protected even near the allowed joint region.
    x0, y0, x1, y1 = setup["base"]["operator_screen_bounds_top_left_pixels"]
    screen_box = (ORIGIN[0]-BASE_HINGE[0]+x0, ORIGIN[1]-BASE_HINGE[1]+y0,
                  ORIGIN[0]-BASE_HINGE[0]+x1, ORIGIN[1]-BASE_HINGE[1]+y1)
    protected.paste(solid_base.crop(screen_box), screen_box)
    for step in range(-120, 121):
        angle = step / 2
        moving = turn(weapon_layer, angle).getchannel("A").point(lambda a: 255 if a >= 128 else 0)
        overlaps[angle] = ImageChops.multiply(protected, moving).histogram()[255]
        shot_overlaps[angle] = ImageChops.multiply(solid_base, turn(muzzle_ray, angle)).histogram()[255]
        wide_shot_overlaps[angle] = ImageChops.multiply(solid_base, turn(wide_muzzle_ray, angle)).histogram()[255]
        joint_contacts[angle] = ImageChops.multiply(solid_base, moving).histogram()[255]
    assert max(overlaps.values()) == 0, ("body overlap", max(overlaps.values()))
    assert max(shot_overlaps.values()) == 0, ("muzzle ray overlap", max(shot_overlaps.values()))
    assert max(wide_shot_overlaps.values()) == 0, ("full bolt corridor overlap", max(wide_shot_overlaps.values()))
    assert min(joint_contacts.values()) > 0, "A detached weapon pose was found"

    bounds = (57, 91, 168, 184)
    review = Image.new("RGB", (5 * 333, 321), (28, 34, 43))
    draw = ImageDraw.Draw(review)
    for i, angle in enumerate(ANGLES):
        patch = backdrop(assembly(angle).crop(bounds)).resize((333, 279), NEAREST)
        review.paste(patch, (i * 333, 30))
        draw.text((i * 333 + 12, 10), f"{angle:+d} deg | rabbit fixed", fill="white")
    review.save(HERE / "sweep-review.png")
    motion = list(range(-60, 61, 4)) + list(range(56, -60, -4))
    frames = [backdrop(assembly(a).crop(bounds)).resize((444, 372), NEAREST) for a in motion]
    frames[0].save(HERE / "sweep.gif", save_all=True, append_images=frames[1:],
                   duration=65, loop=0, disposal=2)

    planet = Image.open(planet_path).convert("RGBA")
    sites = json.loads((HERE / "site-mounts.json").read_text())["sites"]
    assert len(sites) == 12
    for site in sites:
        theta = math.radians(site["angle_clockwise_degrees"])
        site["original_mount_xy"] = site["mount_xy"][:]
        site["mount_xy"][0] += round(math.sin(theta) * OUTWARD_MOUNT_OFFSET)
        site["mount_xy"][1] -= round(math.cos(theta) * OUTWARD_MOUNT_OFFSET)
        site["mount_radius"] = round(site["mount_radius"] + OUTWARD_MOUNT_OFFSET, 3)
    mount_in_assembly = tuple(o + m - h for o, m, h in zip(ORIGIN, BASE_MOUNT, BASE_HINGE))

    # Check the moving gun and a thin firing ray against foreground terrain at
    # every mount. Rear building facades are scenery and are intentionally absent.
    terrain_mask = Image.open(terrain_path).getchannel("A").point(lambda a: 255 if a >= 128 else 0)
    terrain_checks = []
    local_poses = []
    for step in range(-120, 121):
        pair = []
        for source in (weapon_layer.getchannel("A").point(lambda a: 255 if a >= 128 else 0), muzzle_ray):
            tile = Image.new("L", (256, 256))
            tile.paste(turn(source, step / 2), offset(ORIGIN, mount_in_assembly))
            pair.append(tile)
        local_poses.append(pair)
    for site in sites:
        x, y = site["mount_xy"]
        terrain_patch = terrain_mask.crop((x-128, y-128, x+128, y+128))
        maxima = [0, 0]
        for pair in local_poses:
            for i, pose in enumerate(pair):
                world = turn(pose, site["angle_clockwise_degrees"])
                maxima[i] = max(maxima[i], ImageChops.multiply(world, terrain_patch).histogram()[255])
        terrain_checks.append({"site": site["clock_site"], "mount_xy": site["mount_xy"],
                               "original_mount_xy": site["original_mount_xy"],
                               "max_gun_overlap_pixels": maxima[0], "max_muzzle_ray_overlap_pixels": maxima[1]})
    assert all(c["max_gun_overlap_pixels"] == 0 and c["max_muzzle_ray_overlap_pixels"] == 0
               for c in terrain_checks), terrain_checks

    def on_planet(angle):
        # Recenter on the foot before rotating the entire tower around the planet.
        tile = canvas()
        tile.alpha_composite(assembly(angle), offset(ORIGIN, mount_in_assembly))
        result = planet.copy()
        for site in sites:
            rotated = turn(tile, site["angle_clockwise_degrees"])
            result.alpha_composite(rotated, offset(site["mount_xy"], ORIGIN))
        return result

    composites = {a: on_planet(a) for a in (-60, 0, 60)}
    sites_sheet = Image.new("RGB", (12 * 240, 3 * 280), (28, 34, 43))
    draw = ImageDraw.Draw(sites_sheet)
    for row, angle in enumerate((-60, 0, 60)):
        for column, site in enumerate(sites):
            theta = math.radians(site["angle_clockwise_degrees"])
            x, y = site["mount_xy"]
            cx, cy = round(x + math.sin(theta)*48), round(y - math.cos(theta)*48)
            patch = composites[angle].crop((cx-80, cy-80, cx+80, cy+80))
            patch = backdrop(patch).resize((240, 240), NEAREST)
            left, top = column * 240, row * 280 + 30
            sites_sheet.paste(patch, (left, top))
            draw.text((left+8, top-20), f"Site {site['clock_site']:02d} | {angle:+d} deg", fill="white")
    sites_sheet.save(HERE / "twelve-site-sweep.png")
    page = Image.new("RGB", (1920, 668), (28, 34, 43))
    draw = ImageDraw.Draw(page)
    for i, (angle, composite) in enumerate(composites.items()):
        page.paste(backdrop(composite.resize((640, 640), NEAREST)), (i*640, 28))
        draw.text((i*640+12, 10), f"All sites {angle:+d} degrees | offline 640 px canvas", fill="white")
    page.save(HERE / "page-scale-sweep.png")
    manifest = {
        "status": "screen-equipped base and revised laser cannon; offline sweep; Unity validation not run",
        "source_sha256": hashes,
        "coordinates": "pixel edges from top-left; positive study angles clockwise from local outward",
        "base": {"size": list(base.size), "source_crop": base_crop,
                 "mount": BASE_MOUNT, "hinge": BASE_HINGE},
        "barrel": {"size": list(barrel.size), "source_crop": barrel_crop,
                   "hinge": BARREL_HINGE, "muzzle": MUZZLE},
        "aim_limits_degrees": [-60, 60], "operator": "fixed to base",
        "sorting": "base behind weapon; both above terrain in this study",
        "site_mounts": {"source": "site-mounts.json", "uniform_outward_offset_pixels": OUTWARD_MOUNT_OFFSET,
                        "terrain_checks": terrain_checks},
        "checks": {"sampled_angles": 241, "angle_step_degrees": 0.5, "alpha_threshold": 128,
                   "allowed_joint_overlap_radius_pixels": 12,
                   "max_opaque_overlap_outside_joint_pixels": max(overlaps.values()),
                   "max_muzzle_ray_overlap_with_base_pixels": max(shot_overlaps.values()),
                   "projectile_corridor_width_pixels": 7,
                   "max_projectile_corridor_overlap_with_base_pixels": max(wide_shot_overlaps.values()),
                   "minimum_joint_contact_pixels": min(joint_contacts.values()),
                   "neutral_assembled_bounds": list(assembly(0).getbbox())},
        "limitations": ["Offline raster sampling is not Unity collision or gameplay validation.",
                        "Generated shading and partial alpha remain; final palette/grid cleanup pending.",
                        "Upper facades remain crowded; rear scenery is not a gameplay obstacle.",
                        "Crater mounts adjusted uniformly outward; foreground soil lip still pending.",
                        "Thin firing ray checks do not establish projectile width or gameplay collision behavior."]}
    (HERE / "rotation-study.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    for relative, expected in hashes.items():
        assert digest(ROOT / relative) == expected, relative
    print(json.dumps(manifest["checks"], indent=2))
    print("Twelve sites: 241 angles each clear terrain; limit/neutral poses rendered. Input hashes unchanged.")


if __name__ == "__main__":
    main()
