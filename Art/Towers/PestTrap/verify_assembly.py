"""Offline sprite geometry review. Does not edit source art or launch Unity."""
import hashlib
import json
import math
import re
from pathlib import Path

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]


def main():
    assembly = json.loads((HERE / "assembly.json").read_text())
    base = assembly["base"]
    gun = assembly["weapon"]
    images = [Image.open(HERE / part["source"]).convert("RGBA") for part in (base, gun)]
    for part, image in zip((base, gun), images):
        assert list(image.size) == part["size"]
        assert image.getchannel("A").getextrema() == (0, 255)
        assert hashlib.sha256((HERE / part["source"]).read_bytes()).hexdigest() == part["sha256"]
        assert (HERE / part["source"]).read_bytes() == (ROOT / part["runtime"]).read_bytes()

    check_unity_assets(assembly)

    base_image, gun_image = images
    alpha = base_image.getchannel("A")
    gun_alpha = gun_image.getchannel("A")
    socket = base["socket_top_left_pixels"]
    hinge = gun["hinge_top_left_pixels"]
    muzzle = gun["muzzle_top_left_pixels"]
    bp, gp = base["pixels_per_unit"], gun["pixels_per_unit"]
    exclusion = assembly["joint_exclusion_radius_world_units"]
    screen_bounds = base["operator_screen_bounds_top_left_pixels"]
    stride = 4
    points = []
    for y in range(0, gun_image.height, stride):
        for x in range(0, gun_image.width, stride):
            if gun_alpha.getpixel((x, y)) >= 128:
                dx, dy = (x + 0.5 - hinge[0]) / gp, (y + 0.5 - hinge[1]) / gp
                points.append((dx, dy, math.hypot(dx, dy)))

    def opaque(x, y):
        return 0 <= x < base_image.width and 0 <= y < base_image.height and alpha.getpixel((int(x), int(y))) >= 128

    samples = []
    for step in range(241):
        angle = -60 + step * 0.5
        # Image coordinates point down. Positive Unity Z is negative image rotation.
        radians = math.radians(-angle)
        c, s = math.cos(radians), math.sin(radians)
        overlap, contact, ray = 0, 0, 0
        for x, y, radius in points:
            bx = socket[0] + (c * x - s * y) * bp
            by = socket[1] + (s * x + c * y) * bp
            if opaque(bx, by):
                in_screen = screen_bounds[0] <= bx < screen_bounds[2] and screen_bounds[1] <= by < screen_bounds[3]
                if radius > exclusion or in_screen:
                    overlap += 1
                else:
                    contact += 1
        mx, my = (muzzle[0] - hinge[0]) / gp, (muzzle[1] - hinge[1]) / gp
        mx, my = c * mx - s * my, s * mx + c * my
        # One-source-base-pixel sampling, two world units beyond the muzzle.
        for distance in range(math.ceil(2 * bp) + 1):
            bx = socket[0] + mx * bp + s * distance
            by = socket[1] + my * bp - c * distance
            ray += int(opaque(bx, by))
        samples.append({"local_z_degrees": angle, "protected_overlap_samples": overlap,
                        "joint_contact_samples": contact, "muzzle_ray_base_samples": ray})

    verification = {
        "angles": len(samples), "angle_step_degrees": 0.5,
        "weapon_sample_stride_source_pixels": stride, "alpha_threshold": 128,
        "joint_exclusion_radius_world_units": exclusion,
        "max_protected_overlap_samples": max(x["protected_overlap_samples"] for x in samples),
        "min_joint_contact_samples": min(x["joint_contact_samples"] for x in samples),
        "max_muzzle_ray_base_samples": max(x["muzzle_ray_base_samples"] for x in samples),
        "ray_length_world_units": 2,
        "limitations": ["Sampled raster geometry, not a continuous collision proof.",
                        "Only the joint and upper support are excluded; the operator screen is always protected.",
                        "A clear centerline does not establish arbitrary projectile-width clearance.",
                        "Planet seating, neighbors and final camera readability remain unverified.",
                        "No Unity validation or tests run."]
    }
    assembly["verification"] = verification
    (HERE / "assembly.json").write_text(json.dumps(assembly, indent=2) + "\n")
    scratch = ROOT / ".git/menu-work/pest-trap-tower"
    scratch.mkdir(parents=True, exist_ok=True)
    (scratch / "geometry-samples.json").write_text(json.dumps(samples, indent=2) + "\n")
    make_preview(assembly, images)
    print(json.dumps(verification, indent=2))
    assert verification["max_protected_overlap_samples"] == 0, "Weapon intersects protected base"
    assert verification["min_joint_contact_samples"] > 0, "Weapon loses socket contact"
    assert verification["max_muzzle_ray_base_samples"] == 0, "Firing line hits base"


def check_unity_assets(assembly):
    """Read serialized files only: references, pivots and visual-only scope."""
    base, weapon = assembly["base"], assembly["weapon"]
    for part, anchor_key in [(base, "ground_mount_top_left_pixels"),
                             (weapon, "hinge_top_left_pixels")]:
        meta = Path(str(ROOT / part["runtime"]) + ".meta").read_text()
        assert f'guid: {part["guid"]}\n' in meta
        for setting in ["enableMipMap: 0", "filterMode: 0", "wrapU: 1", "wrapV: 1",
                        "nPOTScale: 0", "spriteMode: 1", "spriteMeshType: 0",
                        "alignment: 9", "alphaIsTransparency: 1", "textureType: 8",
                        "textureCompression: 0"]:
            assert setting in meta, setting
        assert f'spritePixelsToUnits: {part["pixels_per_unit"]}\n' in meta
        pivot = re.search(r'spritePivot: \{x: ([^,]+), y: ([^}]+)\}', meta)
        anchor = part[anchor_key]
        assert abs(float(pivot[1]) - anchor[0] / part["size"][0]) < 1e-12
        assert abs(float(pivot[2]) - (1 - anchor[1] / part["size"][1])) < 1e-12

    mount, socket = base["ground_mount_top_left_pixels"], base["socket_top_left_pixels"]
    hinge, muzzle = weapon["hinge_top_left_pixels"], weapon["muzzle_top_left_pixels"]
    bp, wp = base["pixels_per_unit"], weapon["pixels_per_unit"]
    assert assembly["weapon_local_position"] == [(socket[0] - mount[0]) / bp, (mount[1] - socket[1]) / bp]
    assert assembly["muzzle_local_position"] == [(muzzle[0] - hinge[0]) / wp, (hinge[1] - muzzle[1]) / wp]
    assert muzzle[0] == hinge[0], "Nozzle must align with local +Y"
    prefab = (ROOT / assembly["prefab"]).read_text()
    assert re.findall(r'^--- !u!(\d+)', prefab, re.M).count('1') == 4
    assert re.findall(r'^--- !u!(\d+)', prefab, re.M).count('4') == 4
    assert re.findall(r'^--- !u!(\d+)', prefab, re.M).count('212') == 2
    assert set(re.findall(r'^--- !u!(\d+)', prefab, re.M)) == {'1', '4', '212'}
    assert [f'{file_id}' for file_id in re.findall(r'^--- !u!\d+ &(\d+)', prefab, re.M)]
    declared = set(re.findall(r'^--- !u!\d+ &(\d+)', prefab, re.M))
    for reference in re.findall(r'\{fileID: (\d+)\}', prefab):
        assert reference == '0' or reference in declared, reference
    for part in (base, weapon):
        assert f'm_Sprite: {{fileID: 21300000, guid: {part["guid"]}, type: 3}}' in prefab
    for name in ['PestTrapTowerVisual', 'Base', 'CannonPivot', 'Muzzle']:
        assert f'm_Name: {name}\n' in prefab
    for position in [assembly['weapon_local_position'], assembly['muzzle_local_position']]:
        assert f'm_LocalPosition: {{x: {position[0]}, y: {position[1]}, z: 0}}' in prefab
    assert '  m_Father: {fileID: 400002}\n' in prefab, 'Muzzle must follow CannonPivot'
    assert f'guid: {assembly["prefab_guid"]}\n' in Path(str(ROOT / assembly["prefab"]) + '.meta').read_text()

    # Check only our new GUIDs against current imported-asset metadata.
    our_meta = [ROOT / 'Assets/Art/Towers/PestTrap.meta',
                Path(str(ROOT / assembly['prefab']) + '.meta')]
    our_meta += [Path(str(ROOT / part['runtime']) + '.meta') for part in (base, weapon)]
    guids = [re.search(r'^guid: ([0-9a-f]{32})$', p.read_text(), re.M)[1] for p in our_meta]
    assert len(set(guids)) == len(guids)
    occurrences = {value: [] for value in guids}
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8-sig'), re.M)
        if match and match[1] in occurrences:
            occurrences[match[1]].append(str(path))
    assert all(len(paths) == 1 for paths in occurrences.values()), occurrences
    print('Offline metadata checks passed: source equality, import settings, pivots, GUIDs, prefab hierarchy/references.')


def make_preview(assembly, images):
    # Rigid transforms and nearest-neighbor scaling only; source pixels are not repainted.
    density, width, height = 340, 460, 440
    angles = [-60, -30, 0, 30, 60]
    review = Image.new("RGBA", (width * len(angles), height), "#263142")
    draw = ImageDraw.Draw(review)
    base, gun = assembly["base"], assembly["weapon"]
    mount = base["ground_mount_top_left_pixels"]
    socket = base["socket_top_left_pixels"]
    hinge = gun["hinge_top_left_pixels"]
    bp, gp = base["pixels_per_unit"], gun["pixels_per_unit"]
    for index, angle in enumerate(angles):
        origin = (index * width + 170, 405)
        base_image = images[0].resize(tuple(round(v / bp * density) for v in images[0].size), Image.Resampling.NEAREST)
        review.alpha_composite(base_image, (round(origin[0] - mount[0] / bp * density), round(origin[1] - mount[1] / bp * density)))
        # Large temporary layer, pivot at its center, rotated around the measured hinge.
        layer = Image.new("RGBA", (1000, 1000))
        gun_image = images[1].resize(tuple(round(v / gp * density) for v in images[1].size), Image.Resampling.NEAREST)
        layer.alpha_composite(gun_image, (round(500 - hinge[0] / gp * density), round(500 - hinge[1] / gp * density)))
        layer = layer.rotate(angle, resample=Image.Resampling.NEAREST, center=(500, 500))
        target = (round(origin[0] + (socket[0] - mount[0]) / bp * density - 500),
                  round(origin[1] + (socket[1] - mount[1]) / bp * density - 500))
        review.alpha_composite(layer, target)
        draw.text((index * width + 12, 12), f"Local Z {angle:+} degrees", fill="white")
    review.convert("RGB").save(HERE / "sweep-review.png")


if __name__ == "__main__":
    main()
