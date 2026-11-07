"""Extract the generated facade treatment using the accepted source masks.

Origins and source pixel dimensions remain exact. Existing masks bound each
layer; transparent generated edge pixels are excluded rather than made opaque.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    composition_path = HERE.parent / "Sources/facade-layout.json"
    composition = json.loads(composition_path.read_text())
    source_path = HERE / "facades-generated.png"
    generated = Image.open(source_path).convert("RGBA")
    assert generated.size == tuple(composition["canvas_size"])
    orientation_path = HERE / "Orientation/orientation-generated.png"
    oriented = Image.open(orientation_path).convert("RGBA")
    assert oriented.size == generated.size
    # Only import corrected merchandise/crates. The generated full composition
    # also changed shop framing, which must retain its accepted geometry.
    orientation_regions = {
        "shop-southwest": (350, 890, 447, 983),
        "shop-southeast": (808, 890, 905, 983),
        "kiosk-southwest": (414, 928, 562, 1043),
        "kiosk-southeast": (693, 928, 841, 1043),
    }
    crate_polygons = {
        "shop-southwest": [(350, 932), (386, 890), (447, 912), (447, 983), (350, 983)],
        "shop-southeast": [(808, 912), (868, 890), (905, 932), (905, 983), (808, 983)],
    }
    ground_path = HERE.parent / "ContactRepair/terrain-generated.png"
    ground = Image.open(ground_path).getchannel("A").point(lambda a: 255 if a >= 128 else 0)
    # Guild source masks contain old soil and ocean beneath the masonry. Retire
    # those pixels where the repaired ground now owns the surface; keep a small
    # overlap at the actual building/ground contact so rotation cannot open cracks.
    ground = ground.filter(ImageFilter.MinFilter(5))
    layers = []
    for index, part in enumerate(composition["layers"]):
        source = (composition_path.parent / part["file"]).resolve()
        mask_source = Image.open(source).convert("L")
        x, y = part["origin_xy"]
        width, height = mask_source.size
        layer = generated.crop((x, y, x+width, y+height))
        replacement = None
        region = orientation_regions.get(source.stem)
        if region:
            replacement = orientation_path
            polygon = crate_polygons.get(source.stem)
            patch_mask = None
            if polygon:
                patch_mask = Image.new("L", (region[2]-region[0], region[3]-region[1]))
                ImageDraw.Draw(patch_mask).polygon(
                    [(px-region[0], py-region[1]) for px, py in polygon], fill=255)
            layer.paste(oriented.crop(region), (region[0]-x, region[1]-y), patch_mask)
        if source.stem == "stand-east-lower":
            replacement = HERE / "Bakery/bakery-generated.png"
            layer = Image.open(replacement).convert("RGBA").resize(
                (width, height), Image.Resampling.NEAREST)
        alpha = ImageChops.multiply(mask_source,
                                   layer.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
        if source.stem.startswith("guild"):
            alpha = ImageChops.subtract(alpha, ground.crop((x, y, x+width, y+height)))
        layer.putalpha(alpha)
        name = source.stem+".png"
        target = HERE / name
        if not target.exists() or Image.open(target).convert("RGBA").tobytes() != layer.tobytes():
            layer.save(target)
        assert ImageChops.subtract(alpha, mask_source).getbbox() is None
        layers.append({"file": name, "origin_xy": [x, y],
                       "mask_source": source.relative_to(ROOT).as_posix(),
                       "mask_source_sha256": digest(source),
                       "transparent_edge_pixels_excluded": ImageChops.subtract(mask_source, alpha).histogram()[255]})
        if replacement:
            layers[-1]["replacement_source"] = replacement.relative_to(HERE).as_posix()
            layers[-1]["replacement_sha256"] = digest(replacement)
            layers[-1]["replacement_sampling"] = (
                "native crop; original dimensions and mask" if region else
                "nearest; original dimensions and mask")
            if region:
                layers[-1]["replacement_region_xyxy"] = list(region)
                if polygon:
                    layers[-1]["replacement_polygon_xy"] = polygon
    manifest = {"source": source_path.name, "source_sha256": digest(source_path),
                "ground_mask_source": ground_path.relative_to(ROOT).as_posix(),
                "ground_mask_sha256": digest(ground_path),
                "layers": layers, "source_masks_and_origins_preserved": True,
                "alpha_rule": "Original mask intersected with generated alpha; guild excludes repaired ground interior with two-pixel contact overlap; no silhouette expansion"}
    (HERE / "facades.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print(f"Extracted {len(layers)} facade layers at their accepted origins.")


if __name__ == "__main__":
    main()
