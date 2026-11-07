"""Constrain the generated natural edge to the approved single-circle guide.

The central source pixels stay unchanged. The peripheral texture is mapped with
nearest-neighbor sampling and a color transition inside the repaired band.
Requires Python 3 and Pillow. Outputs stay outside Unity.
"""
from collections import deque
import hashlib
import json
import math
from pathlib import Path
import statistics

from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
SOURCE = HERE.parent / "Separation/terrain-visible.png"
REPAIR = HERE / "natural-edge-generated.png"
CENTER = (627,610)
RADIUS = 365
PRESERVE_RADIUS = 270
WARP_START = 300
TRANSITION_END = 315
FRINGE_LIMIT = 5
SAMPLES = 720


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(size):
    result = Image.new("RGBA",size,(48,54,64,255))
    draw = ImageDraw.Draw(result)
    for y in range(0,size[1],16):
        for x in range(0,size[0],16):
            if (x//16+y//16)%2:
                draw.rectangle((x,y,x+15,y+15),fill=(61,67,77,255))
    return result


def preview(image):
    result = checker(image.size)
    result.alpha_composite(image)
    return result.convert("RGB")


def source_radii(repair):
    radii = []
    pixels = repair.load()
    for index in range(SAMPLES):
        angle = index*math.tau/SAMPLES
        dx,dy = math.sin(angle),-math.cos(angle)
        opaque = [r for r in range(320,395)
            if pixels[round(CENTER[0]+dx*r),round(CENTER[1]+dy*r)][3]>=245]
        assert opaque, "Missing terrain in radial sample"
        radii.append(max(opaque))
    # Ignore individual grass tips when estimating the underlying source edge.
    medians = [statistics.median(radii[(i+j)%SAMPLES] for j in range(-8,9))
        for i in range(SAMPLES)]
    return [sum(medians[(i+j)%SAMPLES] for j in range(-4,5))/9
        for i in range(SAMPLES)]


def count_sites(image):
    pixels = image.load()
    soil = set()
    for y in range(245,975):
        for x in range(262,992):
            if (x+.5-CENTER[0])**2+(y+.5-CENTER[1])**2 < PRESERVE_RADIUS**2:
                continue
            r,g,b,a = pixels[x,y]
            if a and r>60 and r>g*1.2 and g>b*1.35:
                soil.add((x,y))
    regions = []
    while soil:
        start = soil.pop()
        pending = deque([start])
        count = 1
        while pending:
            x,y = pending.popleft()
            for point in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if point in soil:
                    soil.remove(point)
                    pending.append(point)
                    count += 1
        if count >= 200:
            regions.append(count)
    return sorted(regions)


def main():
    source_hash = digest(SOURCE)
    repair_hash = digest(REPAIR)
    assert source_hash == "579ffc6dccc7a24071e9b4d96d332ebcaa276cd5e8307d6a1d09a27b356bd3a0", "Terrain source changed; review correction first"
    assert repair_hash == "1e69634fe51d2d507fa5d096aac26c31c923e4def1d239a0892346fc6c1f4f0c"
    source = Image.open(SOURCE).convert("RGBA")
    repair = Image.open(REPAIR).convert("RGBA")
    assert source.size == repair.size == (1254,1254)
    radii = source_radii(repair)
    original,generated = source.load(),repair.load()
    old_grass = Image.new("L",source.size)
    for y in range(source.height):
        for x in range(source.width):
            r,g,b,a = original[x,y]
            if a and r>150 and g>160 and b<100:
                old_grass.putpixel((x,y),255)
    old_grass = old_grass.filter(ImageFilter.MaxFilter(7)).load()
    body = Image.new("RGBA",source.size)
    fringe = Image.new("RGBA",source.size)
    circle = Image.new("L",source.size)
    changed_mask = Image.new("L",source.size)
    body_pixels,fringe_pixels,circle_pixels = body.load(),fringe.load(),circle.load()
    fallback_count = 0
    for y in range(240,980):
        for x in range(257,997):
            dx,dy = x+.5-CENTER[0],y+.5-CENTER[1]
            distance = math.hypot(dx,dy)
            if distance > RADIUS+FRINGE_LIMIT:
                continue
            if distance <= RADIUS:
                circle_pixels[x,y] = 255
            if distance <= PRESERVE_RADIUS:
                assert original[x,y][3] == 255
                body_pixels[x,y] = original[x,y]
                continue
            angle = math.atan2(dx,-dy)%math.tau
            sample = angle*SAMPLES/math.tau
            index = int(sample)
            fraction = sample-index
            edge = radii[index]*(1-fraction)+radii[(index+1)%SAMPLES]*fraction
            t = max(0,min(1,(distance-WARP_START)/(RADIUS-WARP_START)))
            weight = t*t*(3-2*t)
            source_distance = distance+(edge-RADIUS)*weight
            ux,uy = dx/distance,dy/distance
            sx = round(CENTER[0]+ux*source_distance)
            sy = round(CENTER[1]+uy*source_distance)
            color = generated[sx,sy]
            if distance > RADIUS:
                r,g,b,a = color
                # Exterior detail is vegetation only, with no translucent halo.
                if a>=245 and g>r*1.08 and g>b*1.25:
                    fringe_pixels[x,y] = (r,g,b,255)
                continue
            if color[3] < 245:
                fallback_count += 1
                for step in range(1,33):
                    color = generated[round(CENTER[0]+ux*(source_distance-step)),
                        round(CENTER[1]+uy*(source_distance-step))]
                    if color[3]>=245:
                        break
                assert color[3]>=245, "Could not fill circular boundary from repair pixels"
            if distance < TRANSITION_END and not old_grass[x,y]:
                blend = (distance-PRESERVE_RADIUS)/(TRANSITION_END-PRESERVE_RADIUS)
                blend = blend*blend*(3-2*blend)
                color = tuple(round(original[x,y][channel]*(1-blend)+color[channel]*blend)
                    for channel in range(3))+(255,)
            body_pixels[x,y] = (*color[:3],255)

    assert body.getchannel("A").tobytes() == circle.tobytes()
    terrain = body.copy()
    terrain.alpha_composite(fringe)
    changed = unchanged_center = 0
    target = terrain.load()
    for y in range(source.height):
        for x in range(source.width):
            if target[x,y] != original[x,y]:
                changed_mask.putpixel((x,y),255)
                changed += 1
            if (x+.5-CENTER[0])**2+(y+.5-CENTER[1])**2 <= PRESERVE_RADIUS**2:
                assert target[x,y] == original[x,y], "Central artwork changed"
                unchanged_center += 1
    sites = count_sites(terrain)
    assert len(sites) == 12, "Expected twelve separate dirt sites"
    body.save(HERE / "terrain-body.png")
    fringe.save(HERE / "grass-fringe.png")
    circle.save(HERE / "base-circle-mask.png")
    terrain.save(HERE / "terrain-circular.png")
    changed_mask.save(HERE / "changed-pixels-mask.png")
    preview(terrain).save(HERE / "terrain-preview.png")

    guide = preview(terrain).convert("RGBA")
    draw = ImageDraw.Draw(guide)
    draw.ellipse((262,245,992,975),outline=(255,90,180,255),width=1)
    draw.line((617,610,637,610),fill="white",width=1)
    draw.line((627,600,627,620),fill="white",width=1)
    guide.convert("RGB").save(HERE / "circle-guide.png")

    report = {
        "source":"../Separation/terrain-visible.png","source_sha256":source_hash,
        "repair_source":REPAIR.name,"repair_sha256":repair_hash,
        "canvas_size":list(source.size),"center_xy":list(CENTER),
        "radius_x":RADIUS,"radius_y":RADIUS,"pixel_grid":1,
        "mask_rule":"(x + 0.5 - 627)^2 + (y + 0.5 - 610)^2 <= 365^2",
        "body_bounds_xyxy_exclusive":list(circle.getbbox()),
        "body_alpha_matches_circle":True,"body_holes":0,
        "fringe_max_distance_beyond_circle":FRINGE_LIMIT,
        "alpha_values":[0,255],"preserved_center_radius":PRESERVE_RADIUS,
        "preserved_center_pixels":unchanged_center,"changed_center_pixels":0,
        "changed_pixels_total":changed,"edge_inward_samples":fallback_count,
        "soil_region_count":len(sites),"soil_region_pixels":sites,
        "sampling":"Nearest-neighbor peripheral texture; color transition from radius 270 to 315, excluding old grass",
        "source_edge_radius_min":min(radii),"source_edge_radius_max":max(radii),
        "buildings_modified":False,"unity_integration":False,
        "limits":["Dirt recesses are retained artwork inside the circle; site sizes are not standardized.",
            "Perimeter pixels are generated/remapped; only the central 270-pixel radius is exact source artwork.",
            "Building support alignment against the corrected edge still needs review before Unity integration."]
    }
    assert digest(SOURCE) == source_hash and digest(REPAIR) == repair_hash
    (HERE / "circular-terrain.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__ == "__main__":
    main()
