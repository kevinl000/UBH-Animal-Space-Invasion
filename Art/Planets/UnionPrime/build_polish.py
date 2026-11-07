"""Pack terrain layers using the painted soil rims and seat towers against them."""
import hashlib
import json
from pathlib import Path
from statistics import median_low
import sys

from PIL import Image, ImageChops, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
import study_transforms as review


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bounds(mount):
    x, y = mount
    return x-192, y-192, x+192, y+192


def contact_layers(art, sites, base):
    # Rear support belongs behind the facades. Only the inward terrain and lips
    # are drawn in front; the painted rim determines each recess opening.
    terrain_mask = Image.new("L", art.size)
    # Seven source pixels of the outer 365-radius support remain behind facades.
    ImageDraw.Draw(terrain_mask).ellipse((269, 252, 985, 968), fill=255)
    foreground_mask = Image.new("L", art.size)
    contacts = []
    feet = []
    for u in range(-50, 46):
        ys = [v for v in range(-30, 3) if base.getpixel((192+u, 192+v))[3] >= 128]
        if ys and max(ys) >= -20:
            feet.append((u, max(ys)))
    for site in sites:
        mount = site["study_mount"]
        angle = site["study_angle"]
        local = review.turn(art.crop(bounds(mount)), -angle)
        edges = {}
        for u in range(-72, 73):
            soil = []
            for v in range(-45, 46):
                r, g, b, a = local.getpixel((192+u, 192+v))
                if a >= 128 and r > g*1.12 and r > b*1.45:
                    soil.append(v)
            if soil:
                edges[u] = max(soil)
        # A three-column median rejects isolated texture pixels at the boundary.
        edges = {u: median_low(edges[k] for k in range(u-1, u+2) if k in edges) for u in edges}
        assert all(u in edges for u in range(-50, 46)), site["clock_site"]
        opening = Image.new("L", (384, 384))
        lip = Image.new("L", (384, 384))
        cut, front = ImageDraw.Draw(opening), ImageDraw.Draw(lip)
        for u, edge in edges.items():
            cut.line((192+u, 100, 192+u, 192+edge-11), fill=255)
            front.line((192+u, 192+edge-10, 192+u, 237), fill=255)
        opening_world = Image.new("L", art.size)
        opening_world.paste(review.turn(opening, angle), bounds(mount)[:2])
        lip_world = Image.new("L", art.size)
        lip_world.paste(review.turn(lip, angle), bounds(mount)[:2])
        terrain_mask = ImageChops.subtract(terrain_mask, opening_world)
        terrain_mask = ImageChops.lighter(terrain_mask, lip_world)
        foreground_mask = ImageChops.lighter(foreground_mask, lip_world)

        # Fit the actual lower foundation contour, not just an overlap count.
        inset = max(0, max(edges[u]-10-v+2 for u, v in feet))
        assert inset <= 20, (site["clock_site"], inset)
        dx, dy = review.vector(0, inset, angle)
        site["crater_mount"] = mount[:]
        site["seating_inset"] = inset
        site["study_mount"] = [round(mount[0]+dx), round(mount[1]+dy)]
        contacts.append({"site": site["clock_site"], "inset_source_pixels": inset,
                         "foundation_columns": len(feet),
                         "minimum_lip_overlap": min(v+inset-(edges[u]-10)+1 for u, v in feet)})
    terrain = art.copy()
    terrain.putalpha(ImageChops.multiply(terrain_mask, review.solid(art)))
    foreground = art.copy()
    foreground.putalpha(ImageChops.multiply(foreground_mask, review.solid(art)))
    for site, contact in zip(sites, contacts):
        touching = 0
        missing = []
        for u, v in feet:
            dx, dy = review.vector(u, v, site["study_angle"])
            point = (round(site["study_mount"][0]+dx), round(site["study_mount"][1]+dy))
            touching += foreground.getpixel(point)[3] >= 128
            if foreground.getpixel(point)[3] < 128:
                missing.append((u, v, point))
        contact["world_contact_columns"] = touching
        assert touching == len(feet), (site["clock_site"], missing)
    return terrain, foreground, contacts


def main():
    inputs = [HERE / "terrain-generated.png", HERE / "Sources/site-layout.json",
              review.SWEEP / "base-sprite.png", review.SWEEP / "gun-sprite.png"]
    original = Image.open(inputs[0]).convert("RGBA")
    repair_path = HERE / "ContactRepair/terrain-generated.png"
    inputs.append(repair_path)
    art = Image.open(repair_path).convert("RGBA")
    assert art.size == original.size == (1254, 1254), "Keep generation at source coordinates"
    # Keep central ocean/continent pixels exactly; adopt only the perimeter band
    # containing the soil and its transitions into the planet surface.
    interior = Image.new("L", art.size)
    ImageDraw.Draw(interior).ellipse((327, 310, 927, 910), fill=255)
    art.paste(original, (0, 0), interior)
    art.putalpha(review.solid(art))
    sites = json.loads(inputs[1].read_text())["site_placements"]
    base = Image.new("RGBA", (384, 384))
    base.alpha_composite(Image.open(review.SWEEP / "base-sprite.png"), (158, 121))
    base = review.scale_about_mount(base, 1.5)
    terrain, foreground, seating = contact_layers(art, sites, base)
    # Rear ground includes a hidden overlap under the foreground. This avoids
    # filtered cracks without adding any extra playable/collision surface.
    art.save(HERE / "rear-ground.png")
    terrain.save(HERE / "terrain.png")
    foreground.save(HERE / "foreground.png")
    composition_path = HERE / "Facades/facades.json"
    facade_parts = json.loads(composition_path.read_text())["layers"]
    inputs.append(composition_path)
    rear = art.copy()
    contact_checks = []
    guild_layers = Image.new("RGBA", art.size)
    for part in facade_parts:
        path = (composition_path.parent / part["file"]).resolve()
        inputs.append(path)
        layer = Image.open(path).convert("RGBA")
        world = Image.new("RGBA", art.size)
        world.alpha_composite(layer, tuple(part["origin_xy"]))
        contact = review.overlap(review.solid(world), review.solid(art))
        contact_checks.append({"file": part["file"], "rear_ground_contact_pixels": contact})
        if path.stem.startswith("guild"):
            guild_layers.alpha_composite(world)
        else:
            assert contact > 0, f"Floating facade: {path.name}"
        rear.alpha_composite(world)
    assert review.overlap(review.solid(guild_layers), review.solid(art)) > 0
    empty = rear.copy()
    empty.alpha_composite(terrain)
    empty.save(HERE / "planet-empty.png")
    assert len(sites) == 12
    gun_source = Image.new("RGBA", (256, 256))
    gun_source.alpha_composite(Image.open(review.SWEEP / "gun-sprite.png"), (116, 100))
    terrain_masks = [review.solid(terrain.crop(bounds(s["study_mount"]))) for s in sites]
    lip_masks = [review.solid(foreground.crop(bounds(s["study_mount"]))) for s in sites]
    checks = [{"site": s["clock_site"], "max_gun_terrain_pixels": 0,
               "max_ray_terrain_pixels": 0, "max_gun_foreground_pixels": 0,
               "hidden_base_pixels": review.overlap(review.solid(review.turn(base, s["study_angle"])), lip_masks[i])}
              for i, s in enumerate(sites)]
    poses = {}
    for step in range(-120, 121):
        aim = step / 2
        gun = Image.new("RGBA", (384, 384))
        gun.alpha_composite(gun_source.rotate(-aim, review.NEAREST, center=(128, 128)), (94, 17))
        gun = review.scale_about_mount(gun, 1.5)
        for i, site in enumerate(sites):
            weapon = review.solid(review.turn(gun, site["study_angle"]))
            for key, count in (("max_gun_terrain_pixels", review.overlap(weapon, terrain_masks[i])),
                               ("max_ray_terrain_pixels", review.overlap(review.ray(1.5, aim, site["study_angle"]), terrain_masks[i])),
                               ("max_gun_foreground_pixels", review.overlap(weapon, lip_masks[i]))):
                checks[i][key] = max(checks[i][key], count)
                assert count == 0, (site["clock_site"], aim, key, count)
        if aim in (-60, 0, 60):
            local = base.copy()
            local.alpha_composite(gun)
            composite = empty.copy()
            for site in sites:
                composite.alpha_composite(review.turn(local, site["study_angle"]), bounds(site["study_mount"])[:2])
            composite.alpha_composite(foreground)
            poses[int(aim)] = composite
    assert all(c["hidden_base_pixels"] > 0 for c in checks)
    poses[0].save(HERE / "planet-occupied.png")
    page = Image.new("RGB", (1280, 672), review.BACKGROUND)
    for i, (label, im) in enumerate((("Rear ground + foreground recesses | empty", empty),
                                    ("Same terrain | 150% Laser", poses[0]))):
        page.paste(review.backdrop(im.resize((640, 640), review.NEAREST)), (i*640, 32))
        ImageDraw.Draw(page).text((i*640+12, 10), label+" | offline", fill="white")
    page.save(HERE / "page-review.png")
    lower_arc = Image.new("RGB", (840, 600), review.BACKGROUND)
    for row, (label, im) in enumerate((("Lower arc | empty", empty),
                                     ("Lower arc | occupied", poses[0]))):
        lower_arc.paste(review.backdrop(im.crop((207, 780, 1047, 1060))), (0, row*300+20))
        ImageDraw.Draw(lower_arc).text((10, row*300+4), label+" | native pixels | offline", fill="white")
    lower_arc.save(HERE / "Facades/Orientation/lower-arc-review.png")
    detail = Image.new("RGB", (880, 1856), review.BACKGROUND)
    for row, number in enumerate((12, 3, 6, 8)):
        site = next(s for s in sites if s["clock_site"] == number)
        dx, dy = review.vector(0, -40, site["study_angle"])
        cx, cy = round(site["study_mount"][0]+dx), round(site["study_mount"][1]+dy)
        for col, (label, im) in enumerate((("empty", empty), ("occupied", poses[0]))):
            patch = im.crop((cx-110, cy-110, cx+110, cy+110))
            detail.paste(review.backdrop(patch).resize((440, 440), review.NEAREST), (col*440, row*464+24))
            ImageDraw.Draw(detail).text((col*440+10, row*464+6), f"Site {number:02d} | {label} | 2x nearest", fill="white")
    detail.save(HERE / "orientation-closeups.png")
    sheet = Image.new("RGB", (2880, 840), review.BACKGROUND)
    for i, site in enumerate(sites):
        dx, dy = review.vector(0, -40, site["study_angle"])
        cx, cy = round(site["study_mount"][0]+dx), round(site["study_mount"][1]+dy)
        for j, aim in enumerate((-60, 0, 60)):
            x, y = (i % 4)*720+j*240, (i // 4)*280
            sheet.paste(review.backdrop(poses[aim].crop((cx-120, cy-120, cx+120, cy+120))), (x, y+32))
            ImageDraw.Draw(sheet).text((x+8, y+10), f"Site {site['clock_site']:02d} | {aim:+d} deg | 150%", fill="white")
    sheet.save(HERE / "all-sites-aiming.png")
    manifest = {"source_sha256": {p.relative_to(ROOT).as_posix(): digest(p) for p in inputs},
                "site_placements": sites, "tower_scale": 1.5, "crater_reference_radius": 330,
                "seating_contacts": seating,
                "layer_order": ["rear ground", "facades", "terrain", "Laser base", "Laser gun", "foreground"],
                "site_angle_combinations": 2892, "checks": checks, "facade_contacts": contact_checks,
                "limits": ["Offline raster clearance, not Unity verification or projectile width",
                           "Rear ground and facades are decorative and excluded from clearance",
                           "Accepted Union Prime artwork; offline checks do not replace Unity validation"]}
    (HERE / "polish.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(checks, indent=2))


if __name__ == "__main__":
    main()
