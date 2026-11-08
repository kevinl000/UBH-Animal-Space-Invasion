"""Offline sprite geometry review. Does not edit source art or launch Unity."""
import hashlib
import json
import math
import re
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]


def verify_native_assets(assembly):
    prefab = (ROOT / assembly['prefab']).read_text()
    assert re.findall(r'^--- !u!(\d+) &', prefab, re.M).count('212') == 2
    assert set(re.findall(r'^--- !u!(\d+) &', prefab, re.M)) == {'1', '4', '212'}
    assert len(re.findall(r'^--- !u!1 &', prefab, re.M)) == 4
    assert all(f'm_Name: {name}\n' in prefab for name in ('TeslaCoilTowerVisual', 'Base', 'CoilPivot', 'Emission'))
    ids = set(re.findall(r'^--- !u!\d+ &(\d+)', prefab, re.M))
    for reference in re.findall(r'\{fileID: (\d+)\}', prefab):
        assert reference == '0' or reference in ids, f'Unresolved internal fileID {reference}'
    assert re.findall(r'm_Father: \{fileID: (\d+)\}', prefab) == ['0', '400000', '400000', '400002']
    transforms = re.findall(r'--- !u!4 &\d+\n(.*?)(?=---|\Z)', prefab, re.S)
    expected_positions = [(0, 0), (0, 0), assembly['cannon_local_position'], assembly['muzzle_local_position']]
    for block, expected in zip(transforms, expected_positions):
        values = re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', block)
        assert abs(float(values[1]) - expected[0]) < 1e-10
        assert abs(float(values[2]) - expected[1]) < 1e-10
        assert float(values[3]) == 0
        assert 'm_LocalRotation: {x: 0, y: 0, z: 0, w: 1}' in block
        assert 'm_LocalScale: {x: 1, y: 1, z: 1}' in block
    own_metas = []
    for part in (assembly['base'], assembly['weapon']):
        meta_path = ROOT / (part['runtime'] + '.meta')
        own_metas.append(meta_path)
        meta = meta_path.read_text()
        assert f"guid: {part['guid']}\n" in meta
        assert f"m_Sprite: {{fileID: 21300000, guid: {part['guid']}, type: 3}}" in prefab
        for setting in ('enableMipMap: 0', 'filterMode: 0', 'wrapU: 1', 'wrapV: 1',
                        'nPOTScale: 0', 'spriteMode: 1', 'spriteMeshType: 0', 'alignment: 9',
                        'alphaIsTransparency: 1', 'textureType: 8', 'textureCompression: 0'):
            assert setting in meta, f'{meta_path}: {setting}'
        assert f"spritePixelsToUnits: {part['pixels_per_unit']}\n" in meta
        anchor = part.get('ground_mount_top_left_pixels', part.get('hinge_top_left_pixels'))
        expected = [anchor[0] / part['size'][0], 1 - anchor[1] / part['size'][1]]
        pivot = re.search(r'spritePivot: \{x: ([^,]+), y: ([^}]+)\}', meta)
        assert all(abs(float(pivot[i + 1]) - expected[i]) < 1e-10 for i in range(2))
    own_metas += [ROOT / 'Assets/Art/Towers/TeslaCoil.meta', ROOT / (assembly['prefab'] + '.meta')]
    assert f"guid: {assembly['prefab_guid']}\n" in own_metas[-1].read_text()
    guids = [re.search(r'^guid: (\w+)', path.read_text(), re.M)[1] for path in own_metas]
    counts = dict.fromkeys(guids, 0)
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)
        if match and match[1] in counts:
            counts[match[1]] += 1
    assert all(count == 1 for count in counts.values()), counts
    preview = (HERE / 'preview.html').read_text(encoding='utf-8')
    for source in re.findall(r'<img[^>]+src="([^"]+)"', preview):
        assert (HERE / source).is_file()
    assert 'socket:[1091.5,478]' in preview
    assert 'ppu:2200, hinge:[626.5,1064], muzzle:[626.5,88]' in preview
    js_check = 'not run; Node unavailable'
    if shutil.which('node'):
        scratch = ROOT / '.git/menu-work/tesla-tower'
        scratch.mkdir(parents=True, exist_ok=True)
        script = scratch / 'preview-script.js'
        script.write_text(re.search(r'<script>(.*?)</script>', preview, re.S)[1], encoding='utf-8')
        subprocess.run(['node', '--check', str(script)], check=True, capture_output=True, text=True)
        js_check = 'passed node --check; browser rendering not exercised'
    return {'native_prefab_and_import_references': 'passed', 'new_asset_guid_uniqueness': 'passed',
            'preview_local_image_references': 'passed', 'preview_javascript_syntax': js_check}


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
        bounds = image.getchannel('A').point(lambda value: 255 if value >= 16 else 0).getbbox()
        assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < image.width and bounds[3] < image.height

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
    verification.update(verify_native_assets(assembly))
    assembly["verification"] = verification
    (HERE / "assembly.json").write_text(json.dumps(assembly, indent=2) + "\n")
    scratch = ROOT / ".git/menu-work/tesla-tower"
    scratch.mkdir(parents=True, exist_ok=True)
    (scratch / "geometry-samples.json").write_text(json.dumps(samples, indent=2) + "\n")
    make_preview(assembly, images)
    print(json.dumps(verification, indent=2))
    assert verification["max_protected_overlap_samples"] == 0, "Weapon intersects protected base"
    assert verification["min_joint_contact_samples"] > 0, "Weapon loses socket contact"
    assert verification["max_muzzle_ray_base_samples"] == 0, "Firing line hits base"


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
