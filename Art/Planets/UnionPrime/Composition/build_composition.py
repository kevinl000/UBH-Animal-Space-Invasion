"""Assemble unchanged facade sprites around the approved circular terrain.

Integer translations fix surface contact. Only missing guild support pixels are
taken from imagegen; existing artwork is never repainted. Requires Pillow.
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
SEPARATION = HERE.parent / "Separation"
TERRAIN = HERE.parent / "Circular/terrain-circular.png"
OFFSETS = {
    "market-northwest.png": (10,16),
    "market-northeast.png": (-10,16),
    "stand-west-upper.png": (12,5),
    "stand-east-upper.png": (-12,5),
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def preview(sprite):
    image = Image.new("RGBA",sprite.size,(48,54,64,255))
    draw = ImageDraw.Draw(image)
    for y in range(0,sprite.height,16):
        for x in range(0,sprite.width,16):
            if (x//16+y//16)%2:
                draw.rectangle((x,y,x+15,y+15),fill=(61,67,77,255))
    image.alpha_composite(sprite)
    return image.convert("RGB")


def main():
    data = json.loads((SEPARATION/"separation.json").read_text())
    terrain = Image.open(TERRAIN).convert("RGBA")
    terrain_hash = digest(TERRAIN)
    canvas = Image.new("RGBA",terrain.size)
    guild = Image.new("RGBA",terrain.size)
    records = []
    for item in data["existing_layers"]+data["structures"]:
        path = SEPARATION/item["file"]
        sprite = Image.open(path).convert("RGBA")
        shift = OFFSETS.get(item["file"],(0,0))
        origin = tuple(a+b for a,b in zip(item["origin_xy"],shift))
        canvas.alpha_composite(sprite,origin)
        if item in data["existing_layers"]:
            guild.alpha_composite(sprite,origin)
        records.append({"file":"../Separation/"+item["file"],
            "sha256":digest(path),"source_origin_xy":item["origin_xy"],
            "translation_xy":list(shift),"origin_xy":list(origin),
            "rotation_degrees":0,"scale":1})
    aligned = canvas.copy()
    aligned.alpha_composite(terrain)
    repair = Image.open(HERE/"guild-support-repair-source.png").convert("RGBA")
    assert repair.size == (354,60)
    support = Image.new("RGBA",terrain.size)
    mask = Image.new("L",terrain.size)
    missing = []
    selected = 0
    for x in range(465,790):
        bottom = max((y for y in range(230,270) if guild.getpixel((x,y))[3]),default=-1)
        assert bottom>=0
        surface = math.ceil(610-math.sqrt(365**2-(x+.5-627)**2)-.5)
        for y in range(bottom+1,surface):
            if aligned.getpixel((x,y))[3]:
                continue
            color = repair.getpixel((x-450,y-240))
            if color[3]<245:
                missing.append((x,y))
                continue
            support.putpixel((x,y),(*color[:3],255))
            mask.putpixel((x,y),255)
            selected += 1
    assert not missing, "Generated foundation leaves gaps: "+str(len(missing))
    bounds = support.getbbox()
    assert bounds and selected
    support.crop(bounds).save(HERE/"guild-support.png")
    mask.crop(bounds).save(HERE/"guild-support-mask.png")
    assembled = support.copy()
    assembled.alpha_composite(canvas)
    assembled.alpha_composite(terrain)
    for old,new,selected_pixel in zip(aligned.getdata(),assembled.getdata(),mask.getdata()):
        if not selected_pixel:
            assert old==new, "Change outside guild repair"
    # Foreground terrain always wins, including every crater and grass pixel.
    for ground,final in zip(terrain.getdata(),assembled.getdata()):
        if ground[3]:
            assert ground==final, "Facade covers approved terrain"
    assembled.save(HERE/"planet-assembled.png")
    preview(assembled).save(HERE/"planet-preview.png")
    thumbnail = assembled.resize((192,192),Image.Resampling.NEAREST)
    thumbnail.save(HERE/"thumbnail.png")
    study = Image.new("RGB",(1024,704),(24,30,40))
    page = assembled.resize((640,640),Image.Resampling.NEAREST)
    study.paste(preview(page),(16,48))
    study.paste(preview(thumbnail),(704,48))
    font = ImageFont.truetype(str(HERE.parents[3]/"Assets/UI/Fonts/PixelOperator-Bold.ttf"),22)
    draw = ImageDraw.Draw(study)
    draw.text((16,12),"640 PX CANVAS / COMPOSITION STUDY",font=font,fill=(255,239,195))
    draw.text((704,12),"192 PX THUMBNAIL",font=font,fill=(255,239,195))
    study.save(HERE/"scale-review.png")
    assert digest(TERRAIN)==terrain_hash
    for record in records:
        assert digest(HERE/record["file"])==record["sha256"]
    report = {"canvas_size":list(terrain.size),"terrain":{"file":"../Circular/terrain-circular.png","origin_xy":[0,0],"sha256":terrain_hash},
        "structures":records,"guild_support":{"file":"guild-support.png","origin_xy":list(bounds[:2]),
            "bounds_xyxy_exclusive":list(bounds),"added_pixels":selected,"repair_source":"guild-support-repair-source.png",
            "repair_source_origin_xy":[450,240],"repair_source_sha256":digest(HERE/"guild-support-repair-source.png")},
        "assembly_back_to_front":["guild_support","structures in listed order","terrain"],
        "changes_outside_guild_repair_after_alignment":0,"terrain_pixels_changed":0,
        "source_sprites_modified":False,"resampled_source_sprites":False,"transparent_guild_support_gaps":len(missing),
        "sites":12,"tower_placeholders":0,"unity_integration":False,
        "limits":["Composition review artifact; no Unity mounting pivots or display checks yet.",
            "Crater widths/depths are not standardized.","Supports complete the current pose, not every hidden surface for arbitrary independent movement.",
            "Scale-review images are nearest-neighbor offline studies, not Unity captures."]}
    (HERE/"composition.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"structures":len(records),"translated_structures":len(OFFSETS),"added_support_pixels":selected,"terrain_changed_pixels":0},indent=2))


if __name__ == "__main__":
    main()
