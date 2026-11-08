"""Assemble generated library sprites; measure all twelve existing Laser contacts."""
import base64
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import sys

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
OUT = HERE / 'FittedSites'
SPRITES = HERE / 'Sprites'
WORK = ROOT / '.git/menu-work/silverman-library/render'
LASER = ROOT / 'Art/Towers/Laser/Redesign/Sweep'
SIZE = 1254
CENTER = 627
TOWER_SCALE = 1.25
SOURCE = HERE / 'main-flat-sockets-source-v13.png'
GLOBE_CENTER = (627, 695)
GLOBE_SCALE = 1
GROUND_INNER_RADIUS = 389
ANCHORS = [(629, 275), (915, 359), (1073, 525), (1105, 722),
           (1047, 911), (905, 1072), (629, 1154), (354, 1072),
           (211, 911), (152, 722), (185, 525), (344, 359)]
SITE_ANGLES = (0, 4, 90, 90, 128, 152, 180, 210, 234, 270, 270, 352)
AIMS = (-60, 0, 60)


def world(anchor, angle, u, v):
    a = math.radians(angle)
    return (anchor[0] + u*math.cos(a) - v*math.sin(a),
            anchor[1] + u*math.sin(a) + v*math.cos(a))


def image_def(name, path):
    with Image.open(path) as im:
        width, height = im.size
    data = base64.b64encode(path.read_bytes()).decode()
    return f'<image id="{name}" width="{width}" height="{height}" href="data:image/png;base64,{data}"/>'


def document(width, height, defs, body, view=None):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
            f'viewBox="{view or f"0 0 {width} {height}"}">'
            '<style>image{image-rendering:pixelated}text{font-family:Arial,sans-serif;fill:#e8edf3}</style>'
            f'<defs>{defs}</defs>{body}</svg>')


def label(x, y, text, size=18):
    return f'<text x="{x}" y="{y}" font-size="{size}">{text}</text>'


def main():
    OUT.mkdir(exist_ok=True)
    SPRITES.mkdir(exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    source = Image.open(SOURCE).convert('RGBA')
    assert source.size == (SIZE, SIZE)
    source.putalpha(source.getchannel('A').point(lambda a: 255 if a >= 240 else 0))
    clean_source = WORK/'ground-campus-clean.png'
    source.save(clean_source)
    spec = importlib.util.spec_from_file_location('moon_renderer', HERE.parent/'Moon/build_review.py')
    renderer = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(renderer)
    renderer.OUT, renderer.WORK = SPRITES, WORK
    # Split the finer full composition into aligned globe and ground sprite layers.
    shell_defs = image_def('raw', clean_source)
    shell_defs += ('<mask id="outside" maskUnits="userSpaceOnUse" x="0" y="0" width="1254" height="1254">'
                   '<rect width="1254" height="1254" fill="white"/>'
                   f'<circle cx="627" cy="695" r="{GROUND_INNER_RADIUS}" fill="black"/></mask>')
    shell = '<g mask="url(#outside)"><use href="#raw"/></g>'
    renderer.render({'ground-campus': document(SIZE, SIZE, shell_defs, shell)})
    planet = Image.open(SPRITES/'ground-campus.png').convert('RGBA')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    feet = []
    for x in range(base.width):
        ys = [y for y in range(64, 72) if base.getpixel((x, y))[3] >= 128]
        if ys:
            feet.append(((x+.5-34)*TOWER_SCALE, (max(ys)+1-71)*TOWER_SCALE))
    defs = ''.join(image_def(name, path) for name, path in [
        ('source', SPRITES/'ground-campus.png'), ('globe-source', clean_source),
        ('base', LASER/'base-sprite.png'), ('gun', LASER/'gun-sprite.png')])
    defs += ('<filter id="globe-alpha" color-interpolation-filters="sRGB"><feComponentTransfer>'
             '<feFuncA type="linear" slope="1.2" intercept="0"/></feComponentTransfer></filter>')
    defs += ('<clipPath id="globe-circle"><circle cx="627" cy="695" r="405"/></clipPath>'
             '<g id="globe" clip-path="url(#globe-circle)" filter="url(#globe-alpha)"><use href="#globe-source"/></g>')
    fronts, sites = [], []
    for i, anchor in enumerate(ANCHORS):
        angle = SITE_ANGLES[i]
        profile = {}
        for u in range(-75, 76):
            warm = []
            floor = []
            for v in range(-55, 45):
                p = tuple(round(n) for n in world(anchor, angle, u, v))
                r, g, b, a = planet.getpixel(p)
                if a == 255 and r >= 170 and r-g >= 5 and g-b >= 5:
                    warm.append(v)
                if -35 <= v <= 8 and a == 255 and 35 <= r <= 130 and g >= r and b-g >= 5:
                    floor.append(v)
            starts = [v for v in warm if all(v+j in warm for j in (1, 2))]
            after_floor = [v for v in starts if floor and v > max(floor)]
            runs = [v for v in starts if v-1 not in warm]
            profile[u] = after_floor[0] if after_floor else min(runs, key=abs) if runs else 44
        # Center the unchanged foundation over the widest flat part of each painted floor.
        def variation(shift):
            values = [profile[round(u)+shift]-v for u, v in feet]
            return max(values)-min(values)+abs(shift)*.015
        shift = min(range(-16, 17), key=variation)
        inset = max(max(profile[k] for k in range(round(u)+shift-1, round(u)+shift+2))-v
                    for u, v in feet)+3
        mount = world(anchor, angle, shift, inset)
        # The raised side cheeks stay behind the tower; only the low inward lip is in front.
        edge_points = [world(anchor, angle, u, max(profile[u], inset-12)) for u in range(-65, 66)]
        edge_points += [world(anchor, angle, 65, 65), world(anchor, angle, -65, 65)]
        edge = ' '.join(f'{x},{y}' for x, y in edge_points)
        defs += f'<clipPath id="lip{i}"><polygon points="{edge}"/></clipPath>'
        fronts.append(f'<use href="#source" clip-path="url(#lip{i})"/>')
        sites.append(dict(site=i+1, angle=angle, anchor=list(anchor), mount=list(mount),
                          tangent_shift=shift, rim_profile=list(profile.items()),
                          foreground_top=inset-12,
                          minimum_rim_overlap=min(inset+v-profile[round(u)+shift] for u, v in feet),
                          maximum_rim_overlap=max(inset+v-profile[round(u)+shift] for u, v in feet)))
        for aim in AIMS:
            defs += (f'<g id="tower{i}a{aim}" transform="translate({mount[0]} {mount[1]}) rotate({angle}) scale({TOWER_SCALE})">'
                     '<use href="#base" x="-34" y="-71"/>'
                     f'<g transform="translate(30 -47) rotate({aim})"><use href="#gun" x="-12" y="-28"/></g></g>')
    defs += '<g id="terrain"><use href="#source"/></g>'
    defs += '<g id="fronts">'+''.join(fronts)+'</g>'
    defs += '<g id="empty"><use href="#globe"/><use href="#terrain"/></g>'
    for aim in AIMS:
        defs += (f'<g id="occupied{aim}"><use href="#empty"/>'
                 + ''.join(f'<use href="#tower{i}a{aim}"/>' for i in range(12))
                 + '<use href="#fronts"/></g>')
    outputs = {name: document(SIZE, SIZE, defs, f'<use href="#{group}"/>') for name, group in
               [('globe', 'globe'), ('terrain-background', 'terrain'),
                ('socket-fronts', 'fronts'), ('planet-empty', 'empty'), ('planet-occupied', 'occupied0')]}
    body = '<rect width="1600" height="900" fill="#151a30"/>'
    body += label(24, 34, 'SILVERMAN LUNA / LIBRARY PLANET / TWELVE FITTED SITES', 24)
    for i, group in enumerate(('empty', 'occupied0')):
        body += label(24+i*800, 72, 'Empty sockets' if i == 0 else 'Existing Laser sprites / seated foundations', 20)
        body += f'<svg x="{i*800}" y="90" width="800" height="800" viewBox="0 0 1254 1254"><use href="#{group}"/></svg>'
    outputs['page-review'] = document(1600, 900, defs, body)
    body = '<rect width="1440" height="1280" fill="#252d38"/>'
    body += label(16, 32, 'ALL TWELVE SITES / UPRIGHT CONTACTS / GUN AIM -60, 0, +60', 24)
    for i, site in enumerate(sites):
        x, y = (i % 3)*480, 64+(i//3)*300
        body += label(x+12, y, f'Site {i+1} / {site["angle"]} degrees', 18)
        for j, aim in enumerate(AIMS):
            body += label(x+j*160+12, y+25, f'{aim:+} degrees', 14)
            body += (f'<svg x="{x+j*160}" y="{y+35}" width="160" height="240" viewBox="-65 -155 135 203">'
                     f'<g transform="rotate({-site["angle"]}) translate({-site["mount"][0]} {-site["mount"][1]})">'
                     f'<use href="#occupied{aim}"/></g></svg>')
    outputs['contact-review'] = document(1440, 1280, defs, body)
    body = '<rect width="1600" height="1000" fill="#252d38"/>'
    body += label(18, 32, 'TWELVE SHALLOW SOCKETS / WIDE FLAT CENTERS / SHORT ROUNDED SHOULDERS', 22)
    for i, site in enumerate(sites):
        x, y = (i % 4)*400, 65+(i//4)*310
        body += label(x+14, y, f'Site {i+1} / upright profile', 18)
        body += (f'<svg x="{x}" y="{y+16}" width="398" height="275" viewBox="-110 -90 220 152">'
                 f'<g transform="rotate({-site["angle"]}) translate({-site["mount"][0]} {-site["mount"][1]})">'
                 '<use href="#empty"/></g></svg>')
    outputs['socket-shape-review'] = document(1600, 1000, defs, body)
    moon = ROOT/'Art/Planets/SilvermanLuna/Moon/FittedSites'
    defs += image_def('moon-empty', moon/'moon-empty.png')+image_def('moon-occupied', moon/'moon-occupied.png')
    body = '<rect width="1600" height="950" fill="#151a30"/>'
    body += label(28, 36, 'SILVERMAN LUNA / PRESERVED FOUR-SOCKET MOON / SHARED TOWER SIZE', 23)
    body += label(28, 68, 'Main canvas 600 units / moon canvas 180 units / separate sprites; motion not configured', 18)
    # Moon renders include 141px padding on each edge; compensate to preserve 30% source-canvas ratio.
    body += '<g transform="translate(100 115) scale(.63)"><use href="#occupied0"/>'
    body += '<use href="#moon-occupied" transform="translate(1431.5 229.9) scale(.3) translate(-768 -768)"/></g>'
    outputs['with-moon-review'] = document(1600, 950, defs, body)
    renderer.OUT, renderer.WORK = OUT, WORK
    renderer.render(outputs)
    manifest = dict(status='Revised connected library campus; see README for review and integration status.', source_size=[SIZE, SIZE],
                    source=SOURCE.name,
                    source_sha256={SOURCE.name: hashlib.sha256(SOURCE.read_bytes()).hexdigest()},
                    style_reference='Original fine pixel-art library campus; Commons Minor v6 supplies socket geometry only.',
                    source_note='Shallow perimeter notches with wide flat centers, short rounded shoulders and thin flush rims; v11 superseded.',
                    globe_center=list(GLOBE_CENTER), globe_scale=GLOBE_SCALE,
                    ground_inner_radius=GROUND_INNER_RADIUS,
                    ground_transform='identity; aligned globe/ground extraction from finer full source',
                    canvas_display_width=600, tower_scale=TOWER_SCALE,
                    tower_display_size=[70*TOWER_SCALE*600/SIZE, 72*TOWER_SCALE*600/SIZE],
                    foundation_pivot=[34, 71], gun_pivot=[12, 28], base_hinge=[64, 24],
                    render_order=['globe', 'terrain-background', 'towers', 'socket-fronts'],
                    sites=sites, moon_canvas_display_width=180,
                    limits=['Offline support and occlusion samples; not collision/combat verification.',
                            'Moon sources are unchanged. Orbit and rotation settings are not configured.'])
    (OUT/'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n')
    verify(sites, feet)


def verify(sites, feet):
    terrain = Image.open(OUT/'terrain-background.png').convert('RGBA')
    composed = Image.open(OUT/'planet-empty.png').convert('RGBA')
    front = Image.open(OUT/'socket-fronts.png').convert('RGBA')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    gun = Image.open(LASER/'gun-sprite.png').convert('RGBA')
    supported = upper_hidden = gun_hidden = gun_samples = 0
    def alpha(im, point):
        x, y = (round(n) for n in point)
        return im.getpixel((x, y))[3] if 0 <= x < im.width and 0 <= y < im.height else 0
    for site in sites:
        for u, v in feet:
            point = world(site['mount'], site['angle'], u, v)
            assert alpha(terrain, point) >= 240, (site['site'], point, 'no terrain support')
            assert alpha(front, point) >= 128, (site['site'], point, 'no front lip contact')
            supported += 1
        for y in range(53):
            for x in range(base.width):
                if base.getpixel((x, y))[3] >= 128:
                    upper_hidden += alpha(front, world(site['mount'], site['angle'],
                        (x+.5-34)*TOWER_SCALE, (y+.5-71)*TOWER_SCALE)) >= 128
        for aim in AIMS:
            a = math.radians(aim)
            for y in range(gun.height):
                for x in range(gun.width):
                    if gun.getpixel((x, y))[3] < 128:
                        continue
                    u, v = x+.5-12, y+.5-28
                    point = world(site['mount'], site['angle'],
                        (30+u*math.cos(a)-v*math.sin(a))*TOWER_SCALE,
                        (-47+u*math.sin(a)+v*math.cos(a))*TOWER_SCALE)
                    gun_hidden += alpha(front, point) >= 128
                    gun_samples += 1
    assert supported == 744, supported
    assert upper_hidden == 0, upper_hidden
    assert gun_hidden == 0, gun_hidden
    assert composed.getpixel(GLOBE_CENTER)[3] >= 240
    assert terrain.getpixel(GLOBE_CENTER)[3] == 0
    assert terrain.getpixel((0, 0))[3] == 0
    connected = composed.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    ImageDraw.floodfill(connected, GLOBE_CENTER, 128)
    for site in sites:
        assert connected.getpixel(tuple(round(n) for n in site['mount'])) == 128, (site['site'], 'detached socket')
    inner_radii = []
    seam_samples = 0
    for angle in range(360):
        def radial(radius):
            return world(GLOBE_CENTER, angle, 0, -radius)
        boundary = next(r for r in range(370, 430) if alpha(terrain, radial(r)) >= 240)
        inner_radii.append(boundary)
        for radius in range(390, 431):
            assert alpha(composed, radial(radius)) >= 240, (angle, radius, 'globe/ground gap')
            seam_samples += 1
    assert max(inner_radii)-min(inner_radii) <= 16, inner_radii
    result = dict(scope='offline rendered support and rim occlusion', sites=12,
                  foundation_samples_supported=supported, upper_base_pixels_hidden=upper_hidden,
                  gun_pixels_hidden=gun_hidden, gun_pixels_checked=gun_samples, aiming_poses=list(AIMS),
                  globe_ground_seam_samples=seam_samples,
                  ground_inner_radius_range=[min(inner_radii), max(inner_radii)],
                  refined_main_source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest())
    (OUT/'verification.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    main()
