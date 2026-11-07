"""Extract visible facade assemblies from the repaired source without repainting.

Requires Python 3 and Pillow. Saved binary masks are the editable source.
Use --make-masks only to reset them to the traced boundaries below.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
SOURCE = HERE.parent / "Working/planet-isolated.png"
EXTRACTION = HERE.parent / "Extraction"
SOURCE_SHA256 = "9c82fa5030f4a3d38193b1fcba195e7b067d640584ce56ece881e3a0df9a111b"

# Source-coordinate selections follow the facade side of the foreground grass.
# Shrubs, signs and visible supports stay with their adjacent building assembly.
OUTLINES = {
    "market-northwest": [(235,180),(457,180),(457,266),(446,277),(428,288),
        (408,300),(389,312),(369,327),(354,345),(337,362),(319,374),
        (308,393),(287,383),(282,375),(280,355),(235,355)],
    "market-northeast": [(797,180),(1020,180),(1020,355),(974,355),
        (972,375),(967,383),(946,393),(935,374),(917,362),(900,345),(885,327),
        (865,312),(846,300),(826,288),(808,277),(797,266)],
    "stand-west-upper": [(200,355),(280,355),(282,375),(287,383),(308,393),
        (311,398),(303,410),(296,427),(290,445),(283,463),(277,485),(200,485)],
    "stand-east-upper": [(1055,355),(974,355),(972,375),(967,383),(946,393),
        (943,398),(951,410),(958,427),(964,445),(971,463),(977,485),(1055,485)],
    "stand-west-lower": [(200,680),(268,680),(274,702),(282,725),
        (289,745),(297,765),(307,782),(283,802),(200,810)],
    "stand-east-lower": [(1055,680),(986,680),(980,702),(972,725),
        (965,745),(957,765),(947,782),(971,802),(1055,810)],
    "shop-southwest": [(225,810),(283,802),(307,782),(319,800),
        (334,820),(350,838),(367,855),(385,872),(405,889),(425,904),
        (445,918),(446,927),(420,982),(225,982)],
    "shop-southeast": [(1030,810),(971,802),(947,782),(935,800),
        (920,820),(904,838),(887,855),(869,872),(849,889),(829,904),
        (809,918),(808,927),(834,982),(1030,982)],
    "kiosk-southwest": [(446,927),(474,938),(498,947),(522,954),
        (544,960),(561,964),(561,1050),(405,1050),(405,982),(420,982)],
    "kiosk-southeast": [(808,927),(780,938),(756,947),(732,954),
        (710,960),(693,964),(693,1050),(849,1050),(849,982),(834,982)],
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(size):
    result = Image.new("RGBA", size, (48,54,64,255))
    draw = ImageDraw.Draw(result)
    for y in range(0,size[1],16):
        for x in range(0,size[0],16):
            if (x//16+y//16)%2:
                draw.rectangle((x,y,x+15,y+15),fill=(61,67,77,255))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--make-masks",action="store_true")
    args = parser.parse_args()
    assert digest(SOURCE) == SOURCE_SHA256, "Working source changed; review masks first"
    source = Image.open(SOURCE).convert("RGBA")
    assert source.size == (1254,1254)
    available = source.getchannel("A").point(lambda a: 255 if a else 0)
    terrain = source.copy()
    layers = []
    for name in ("guild-facade", "guild-foundation-visible"):
        layer = Image.open(EXTRACTION / (name+".png")).convert("RGBA")
        mask = layer.getchannel("A")
        assert set(mask.getdata()) <= {0,255}
        full_mask = Image.new("L",source.size)
        full_mask.paste(mask,(442,65))
        assert not ImageChops.subtract(full_mask,available).getbbox()
        available = ImageChops.subtract(available,full_mask)
        terrain.paste((0,0,0,0),(0,0,*source.size),full_mask)
        layers.append(("../Extraction/"+name+".png",layer,(442,65)))

    records = []
    for name, outline in OUTLINES.items():
        mask_path = HERE / (name+"-mask.png")
        if args.make_masks:
            mask = Image.new("L",source.size)
            ImageDraw.Draw(mask).polygon(outline,fill=255)
            # Shared boundary pixels belong to the first selected assembly.
            mask = ImageChops.multiply(mask,available)
            mask.save(mask_path)
        mask = Image.open(mask_path).convert("L")
        assert mask.size == source.size and set(mask.getdata()) <= {0,255}
        assert not ImageChops.subtract(mask,available).getbbox(), name+": overlap or sky selection"
        bounds = mask.getbbox()
        assert bounds, name+": empty mask"
        available = ImageChops.subtract(available,mask)
        layer = Image.new("RGBA",source.size)
        layer.paste(source,(0,0),mask)
        layer = layer.crop(bounds)
        layer.save(HERE / (name+".png"))
        terrain.paste((0,0,0,0),(0,0,*source.size),mask)
        layers.append((name+".png",layer,bounds[:2]))
        records.append({"file":name+".png","mask":mask_path.name,
            "bounds_xyxy_exclusive":list(bounds),"origin_xy":list(bounds[:2]),
            "visible_pixels":sum(bool(a) for a in mask.getdata())})

    terrain.save(HERE / "terrain-visible.png")
    assembly = terrain.copy()
    for name,layer,origin in layers:
        assembly.alpha_composite(layer,origin)
    assert assembly.tobytes() == source.tobytes(), "Reassembly changed source pixels"
    font_path = HERE.parents[3] / "Assets/UI/Fonts/PixelOperator-Bold.ttf"
    font = ImageFont.truetype(str(font_path),20)
    sheet = checker((1120,1120))
    draw = ImageDraw.Draw(sheet)
    for index,(name,layer,origin) in enumerate(layers):
        col,row = index%3,index//3
        x,y = col*373,row*280
        draw.text((x+10,y+7),Path(name).stem,font=font,fill=(255,239,195,255))
        sheet.alpha_composite(layer,(x+(373-layer.width)//2,y+30))
    sheet.convert("RGB").save(HERE / "structures-preview.png")
    manifest = {
        "source":"../Working/planet-isolated.png","source_sha256":SOURCE_SHA256,
        "source_size":list(source.size),"method":"Binary source-pixel extraction; no resampling or repainting",
        "new_structure_count":len(records),"structures":records,
        "terrain":"terrain-visible.png","terrain_origin_xy":[0,0],
        "existing_layers":[{"file":name,"origin_xy":list(origin)} for name,_,origin in layers[:2]],
        "assembly_order":"terrain-visible, existing guild layers, structures in listed order",
        "full_reassembly_different_pixels":0,"overlapping_selected_pixels":0,
        "reconstructed_pixels":0,"geometry_corrections_applied":False,"unity_integration":False,
        "limits":["Visible fragments only; hidden terrain/support edges are unfinished.",
            "Source radial orientations are retained; cropped origins are not Unity mounting pivots.",
            "Foreground grass stays on terrain where traced; semantic mask boundaries require visual review.",
            "Shrubs and signs stay with adjacent building assemblies.",
            "Twelve sites remain unchanged; circularity and equal site dimensions are not certified."]
    }
    assert digest(SOURCE) == SOURCE_SHA256
    (HERE / "separation.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"new_structures":len(records),"reassembly_different_pixels":0,
        "source_unchanged":True,"visible_structure_pixels":sum(r["visible_pixels"] for r in records)},indent=2))


if __name__ == "__main__":
    main()
