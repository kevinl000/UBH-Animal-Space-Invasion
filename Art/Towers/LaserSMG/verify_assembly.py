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

    verify_asset_references(assembly)

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
    scratch = ROOT / ".git/menu-work/laser-smg"
    scratch.mkdir(parents=True, exist_ok=True)
    (scratch / "geometry-samples.json").write_text(json.dumps(samples, indent=2) + "\n")
    make_preview(assembly, images)
    print(json.dumps(verification, indent=2))
    assert verification["max_protected_overlap_samples"] == 0, "Weapon intersects protected base"
    assert verification["min_joint_contact_samples"] > 0, "Weapon loses socket contact"
    assert verification["max_muzzle_ray_base_samples"] == 0, "Firing line hits base"


def verify_asset_references(assembly):
    new_guids = []
    for name, anchor_key in (("base", "ground_mount_top_left_pixels"), ("weapon", "hinge_top_left_pixels")):
        part = assembly[name]
        meta = (ROOT / (part["runtime"] + ".meta")).read_text()
        guid = re.search(r"^guid: (\w+)$", meta, re.M).group(1)
        assert guid == part["guid"]
        new_guids.append(guid)
        for field, value in (("enableMipMap", 0), ("filterMode", 0), ("wrapU", 1), ("wrapV", 1),
                             ("wrapW", 1), ("nPOTScale", 0), ("spriteMode", 1), ("spriteMeshType", 0),
                             ("alignment", 9), ("alphaIsTransparency", 1), ("textureType", 8),
                             ("spritePixelsToUnits", part["pixels_per_unit"])):
            assert re.search(rf"^\s*{field}: {value}$", meta, re.M), field
        assert all(value == "0" for value in re.findall(r"textureCompression: (\d+)", meta))
        pivot = re.search(r"spritePivot: \{x: ([\d.]+), y: ([\d.]+)\}", meta)
        anchor = part[anchor_key]
        expected = (anchor[0] / part["size"][0], 1 - anchor[1] / part["size"][1])
        assert all(abs(float(actual) - target) < 1e-10 for actual, target in zip(pivot.groups(), expected))

    prefab_path = ROOT / assembly["prefab"]
    prefab = prefab_path.read_text()
    assert re.search(r"^guid: (\w+)$", Path(str(prefab_path) + ".meta").read_text(), re.M).group(1) == assembly["prefab_guid"]
    new_guids.append(assembly["prefab_guid"])
    folder_meta = ROOT / "Assets/Art/Towers/LaserSMG.meta"
    new_guids.append(re.search(r"^guid: (\w+)$", folder_meta.read_text(), re.M).group(1))
    assert len(set(new_guids)) == 4
    guid_counts = dict.fromkeys(new_guids, 0)
    for meta_path in (ROOT / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: (\w+)$", meta_path.read_text(encoding="utf-8-sig"), re.M)
        if match and match.group(1) in guid_counts:
            guid_counts[match.group(1)] += 1
    assert all(count == 1 for count in guid_counts.values()), guid_counts

    headers = re.findall(r"^--- !u!(\d+) &(\d+)$", prefab, re.M)
    assert sorted(int(kind) for kind, _ in headers) == [1] * 4 + [4] * 4 + [212] * 2
    ids = {int(identifier) for _, identifier in headers}
    assert len(ids) == 10
    internal = {int(value) for value in re.findall(r"\{fileID: (\d+)\}", prefab)} - {0}
    assert internal <= ids
    for name in ("base", "weapon"):
        assert f"m_Sprite: {{fileID: 21300000, guid: {assembly[name]['guid']}, type: 3}}" in prefab
    assert "m_Name: CannonPivot" in prefab and "m_Name: Muzzle" in prefab
    assert "m_LocalPosition: {x: 0.305, y: 0.497, z: 0}" in prefab
    assert "m_LocalPosition: {x: 0, y: 0.27, z: 0}" in prefab
    b, g = assembly["base"], assembly["weapon"]
    mount, socket = b["ground_mount_top_left_pixels"], b["socket_top_left_pixels"]
    hinge, muzzle = g["hinge_top_left_pixels"], g["muzzle_top_left_pixels"]
    assert assembly["cannon_local_position"] == [(socket[0]-mount[0])/b["pixels_per_unit"], (mount[1]-socket[1])/b["pixels_per_unit"]]
    assert assembly["muzzle_local_position"] == [(muzzle[0]-hinge[0])/g["pixels_per_unit"], (hinge[1]-muzzle[1])/g["pixels_per_unit"]]
    preview = (HERE / "preview.html").read_text(encoding="utf-8")
    for source in re.findall(r'<img[^>]+src="([^"]+)"', preview):
        assert (HERE / source).is_file()
    assert "width:1278, height:1230, ppu:1000, mount:[598,1101], socket:[903,604]" in preview
    assert "width:1254, height:1254, ppu:3000, hinge:[627,1011], muzzle:[627,201]" in preview
    print("Asset checks passed: source/runtime equality, alpha, imports, pivots, four unique GUIDs, prefab hierarchy/references and preview coordinates.")


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
