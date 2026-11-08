"""Seat existing Laser sprites in locally size-matched moon sockets using SVG layers."""
import base64
import hashlib
import json
import math
import re
import subprocess
from pathlib import Path

from PIL import Image


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SOURCE = HERE/'moon-small-source-v4.png'
LASER = ROOT/'Art/Towers/Laser/Redesign/Sweep'
OUT = HERE/'FittedSites'
WORK = ROOT/'.git/menu-work/silverman-moon-render'
REFERENCE_CANVAS_WIDTH = 600
REFERENCE_SOURCE_SIZE = 1254
REFERENCE_TOWER_SCALE = 1.25
MOON_TO_REFERENCE_CANVAS_RATIO = .30
ANCHORS = [(620, 249), (999, 625), (620, 1019), (254, 625)]
PROFILE_HALF_WIDTH = 165
AIMS = (-60, 0, 60)


def world(anchor, angle, u, v):
    a = math.radians(angle)
    return (anchor[0]+u*math.cos(a)-v*math.sin(a),
            anchor[1]+u*math.sin(a)+v*math.cos(a))


def image_def(name, path):
    with Image.open(path) as im:
        width, height = im.size
    data = base64.b64encode(path.read_bytes()).decode()
    return (f'<image id="{name}" width="{width}" height="{height}" '
            f'href="data:image/png;base64,{data}"/>')


def document(width, height, defs, body, view=None):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
            f'viewBox="{view or f"0 0 {width} {height}"}">'
            '<style>image{image-rendering:pixelated}text{font-family:Arial,sans-serif;fill:#e8edf3}</style>'
            f'<defs>{defs}</defs>{body}</svg>')


def label(x, y, text, size=18):
    return f'<text x="{x}" y="{y}" font-size="{size}">{text}</text>'


def rim_profile(source, anchor, angle, width_scale=1):
    """Trace the first substantial warm stone band inward from each gray floor."""
    profile = {}
    for u in range(-PROFILE_HALF_WIDTH, PROFILE_HALF_WIDTH+1):
        warm = []
        for v in range(-65, 29):
            p = tuple(round(n) for n in world(anchor, angle, u/width_scale, v))
            r, g, b, alpha = source.getpixel(p)
            if alpha >= 240 and r >= 170 and r-g >= 5 and g-b >= 1:
                warm.append(v)
        starts = [v for v in warm if all(v+j in warm for j in (1, 2))]
        assert starts, (angle, u, 'missing stone lip')
        profile[u] = starts[0]
    return profile


def build():
    OUT.mkdir(exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    source = Image.open(SOURCE).convert('RGBA')
    # Freeze the approved moon/tower sizes independently of the scrapped main planet.
    reference_scale = REFERENCE_CANVAS_WIDTH/REFERENCE_SOURCE_SIZE
    shared_tower_scale = reference_scale*REFERENCE_TOWER_SCALE
    moon_scale = reference_scale*MOON_TO_REFERENCE_CANVAS_RATIO
    tower_scale = shared_tower_scale/moon_scale
    shared_scale = dict(reference_canvas_display_width=REFERENCE_CANVAS_WIDTH,
                        moon_canvas_display_width=moon_scale*1254,
                        moon_to_reference_canvas_ratio=moon_scale/reference_scale,
                        tower_display_scale=shared_tower_scale,
                        base_sprite_display_size=[70*shared_tower_scale, 72*shared_tower_scale],
                        rule='Fixed approved moon and tower sizes; no main-planet assets required.')
    assert source.size == (1254, 1254)
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    feet = []
    for x in range(base.width):
        ys = [y for y in range(64, 72) if base.getpixel((x, y))[3] >= 128]
        if ys:
            feet.append(((x+.5-34)*tower_scale, (max(ys)+1-71)*tower_scale))
    defs = ''.join(image_def(name, path) for name, path in [
        ('source', SOURCE),
        ('base', LASER/'base-sprite.png'), ('gun', LASER/'gun-sprite.png')])
    # Reuse the top socket's actual rock at all four sites to keep floor sizes equal.
    cores = []
    patches = []
    for i, anchor in enumerate(ANCHORS):
        coords = ' '.join(f'{x},{y}' for x, y in
                          [world(anchor, i*90, u, v) for u, v in
                           [(-210, -200), (210, -200), (210, 70), (-210, 70)]])
        cores.append(f'<polygon points="{coords}"/>')
        patches.append(f'<g transform="translate({anchor[0]} {anchor[1]}) rotate({i*90})">'
                       '<g mask="url(#socket-sides)">'
                       '<g mask="url(#socket-inner)"><use href="#source" transform="translate(-620 -249)"/></g></g></g>')
    defs += ('<linearGradient id="socket-x" gradientUnits="userSpaceOnUse" x1="-240" x2="240">'
             '<stop stop-color="black"/><stop offset=".0625" stop-color="white"/>'
             '<stop offset=".9375" stop-color="white"/><stop offset="1" stop-color="black"/></linearGradient>'
             '<linearGradient id="socket-y" gradientUnits="userSpaceOnUse" x1="0" y1="70" x2="0" y2="100">'
             '<stop stop-color="white"/><stop offset="1" stop-color="black"/></linearGradient>'
             '<mask id="socket-sides" maskUnits="userSpaceOnUse" x="-240" y="-200" width="480" height="300">'
             '<rect x="-240" y="-200" width="480" height="300" fill="url(#socket-x)"/></mask>'
             '<mask id="socket-inner" maskUnits="userSpaceOnUse" x="-240" y="-200" width="480" height="300">'
             '<rect x="-240" y="-200" width="480" height="300" fill="url(#socket-y)"/></mask>'
             '<mask id="socket-core" maskUnits="userSpaceOnUse" width="1254" height="1254">'
             '<rect width="1254" height="1254" fill="white"/><g fill="black">'
             + ''.join(cores)+'</g></mask><g id="matched-rock">'
             '<use href="#source" mask="url(#socket-core)"/>'+''.join(patches)+'</g>')
    defs += ('<filter id="solid" color-interpolation-filters="sRGB">'
             '<feComponentTransfer><feFuncA type="discrete" '
             'tableValues="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1"/>'
             '</feComponentTransfer></filter>'
             '<filter id="trim" x="-5%" y="-5%" width="110%" height="110%">'
             '<feMorphology in="SourceAlpha" operator="erode" radius="1" result="inset"/>'
             '<feMorphology in="inset" operator="dilate" radius="1" result="clean"/>'
             '<feComposite in="SourceGraphic" in2="clean" operator="in"/></filter>'
             '<g id="terrain" filter="url(#trim)"><use href="#matched-rock" filter="url(#solid)"/></g>')
    sites, fronts = [], []
    for i, anchor in enumerate(ANCHORS):
        angle = i*90
        profile = rim_profile(source, ANCHORS[0], 0)
        inset = max(profile[round(u)]-v for u, v in feet)+2
        mount = world(anchor, angle, 0, inset)
        edge = [world(anchor, angle, u, v) for u, v in profile.items()]
        edge += [world(anchor, angle, PROFILE_HALF_WIDTH, 65), world(anchor, angle, -PROFILE_HALF_WIDTH, 65)]
        coords = ' '.join(f'{x:.3f},{y:.3f}' for x, y in edge)
        defs += f'<clipPath id="lip{i}"><polygon points="{coords}"/></clipPath>'
        fronts.append(f'<use href="#terrain" clip-path="url(#lip{i})"/>')
        overlaps = [inset+v-profile[round(u)] for u, v in feet]
        sites.append(dict(site=i+1, angle=angle, anchor=anchor, mount=mount,
                          foundation_inset=inset, rim_profile=list(profile.items()),
                          minimum_rim_overlap=min(overlaps), maximum_rim_overlap=max(overlaps)))
        for aim in AIMS:
            defs += (f'<g id="tower{i}a{aim}" transform="translate({mount[0]} {mount[1]}) '
                     f'rotate({angle}) scale({tower_scale})"><use href="#base" x="-34" y="-71"/>'
                     f'<g transform="translate(30 -47) rotate({aim})">'
                     '<use href="#gun" x="-12" y="-28"/></g></g>')
    defs += '<g id="fronts">'+''.join(fronts)+'</g>'
    for aim in AIMS:
        defs += (f'<g id="occupied{aim}"><use href="#terrain"/>'
                 + ''.join(f'<use href="#tower{i}a{aim}"/>' for i in range(4))
                 + '<use href="#fronts"/></g>')
    outputs = {}
    for name, group in [('terrain-background', 'terrain'), ('socket-fronts', 'fronts')]:
        outputs[name] = document(1254, 1254, defs, f'<use href="#{group}"/>')
    for name, group in [('moon-empty', 'terrain'), ('moon-occupied', 'occupied0')]:
        outputs[name] = document(1536, 1536, defs, f'<use href="#{group}"/>', '-141 -141 1536 1536')
    body = '<rect width="1440" height="810" fill="#151a30"/>'
    body += label(20, 32, 'SILVERMAN MOON / MATCHED SOCKET SIZES / FOUR FITTED LASER SITES', 22)
    for i, group in enumerate(('terrain', 'occupied0')):
        body += label(20+i*720, 72, 'Empty natural rock sockets' if i == 0 else 'Existing Laser sprites / rock lips', 20)
        body += (f'<svg x="{i*720}" y="90" width="720" height="700" '
                 f'viewBox="-141 -141 1536 1536"><use href="#{group}"/></svg>')
    outputs['page-review'] = document(1440, 810, defs, body)
    body = '<rect width="1260" height="1190" fill="#252d38"/>'
    body += label(20, 34, 'FOUR SITES / EMPTY AND THREE GUN ANGLES / UPRIGHT CONTACT VIEWS', 22)
    for i, site in enumerate(sites):
        mx, my = site['mount']
        y = 75+i*275
        clock = (12, 3, 6, 9)[i]
        body += label(12, y, f'{clock} o\u2019clock / site {i+1} / {site["angle"]} degrees clockwise', 18)
        for j, group in enumerate(('terrain', 'occupied-60', 'occupied0', 'occupied60')):
            body += (f'<svg x="{j*315}" y="{y+12}" width="310" height="245" '
                     'viewBox="-220 -380 490 435">'
                     f'<g transform="rotate({-site["angle"]}) translate({-mx} {-my})">'
                     f'<use href="#{group}"/></g></svg>')
    outputs['contact-review'] = document(1260, 1190, defs, body)
    render(outputs)
    verify(sites, feet, tower_scale)
    assert math.isclose(moon_scale*tower_scale, shared_tower_scale, abs_tol=1e-12)
    manifest = dict(status='User-approved moon artwork, 2026-11-08; Unity integration pending.',
                    approved_moon_to_reference_canvas_ratio=MOON_TO_REFERENCE_CANVAS_RATIO,
                    source=SOURCE.name, socket_donor_anchor=ANCHORS[0],
                    socket_donor_note='The same top rock recess rotated to all four sites with blended joins.',
                    source_size=list(source.size),
                    source_sha256={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                                   for p in (SOURCE, LASER/'base-sprite.png', LASER/'gun-sprite.png')},
                    tower_scale=tower_scale, foundation_pivot=[34, 71], gun_pivot=[12, 28],
                    shared_display_scale=shared_scale,
                    base_hinge=[64, 24], render_order=['terrain-background', 'towers', 'socket-fronts'],
                    alpha_cleanup=dict(minimum_retained_source_alpha=240, spur_trim_radius=1),
                    sites=sites, limits=['Offline art assembly; no Unity scene/prefab integration.',
                    'Approved moon display width and tower scale are fixed independently; orbit settings remain unconfirmed.',
                    'Three aiming poses sampled; not a continuous collision sweep.'])
    (OUT/'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')


def render(outputs):
    chrome = Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
    for name, svg in outputs.items():
        path = WORK/f'{name}.svg'
        path.write_text(svg, encoding='utf-8')
        width, height = map(int, re.search(r'width="(\d+)" height="(\d+)"', svg).groups())
        html = WORK/f'{name}.html'
        html.write_text('<style>html,body{margin:0;padding:0;overflow:hidden}img{display:block}</style>'
                        + f'<img src="{path.as_uri()}" width="{width}" height="{height}">', encoding='utf-8')
        result = subprocess.run([str(chrome), '--headless', '--disable-gpu', '--no-first-run',
            '--no-default-browser-check', '--hide-scrollbars', '--allow-file-access-from-files',
            '--force-device-scale-factor=1', '--default-background-color=00000000',
            '--run-all-compositor-stages-before-draw', '--virtual-time-budget=1000',
            f'--user-data-dir={WORK/"browser"}', f'--window-size={width},{height}',
            f'--screenshot={OUT/(name+".png")}', html.as_uri()],
            capture_output=True, timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
        if result.returncode:
            raise RuntimeError(result.stderr.decode(errors='replace')[-1500:])
        assert Image.open(OUT/(name+'.png')).size == (width, height)
        print('Rendered', name, flush=True)


def verify(sites, feet, tower_scale):
    terrain = Image.open(OUT/'terrain-background.png').convert('RGBA')
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
            assert alpha(terrain, point) == 255, (site['site'], point, 'no terrain support')
            assert alpha(front, point) == 255, (site['site'], point, 'no lip contact')
            supported += 1
        for y in range(53):
            for x in range(base.width):
                if base.getpixel((x, y))[3] >= 128:
                    point = world(site['mount'], site['angle'], (x+.5-34)*tower_scale, (y+.5-71)*tower_scale)
                    upper_hidden += alpha(front, point) >= 128
        for aim in AIMS:
            a = math.radians(aim)
            for y in range(gun.height):
                for x in range(gun.width):
                    if gun.getpixel((x, y))[3] < 128:
                        continue
                    u, v = x+.5-12, y+.5-28
                    point = world(site['mount'], site['angle'],
                                  (30+u*math.cos(a)-v*math.sin(a))*tower_scale,
                                  (-47+u*math.sin(a)+v*math.cos(a))*tower_scale)
                    gun_hidden += alpha(front, point) >= 128
                    gun_samples += 1
    assert supported == 248, supported
    assert upper_hidden == 0, upper_hidden
    assert gun_hidden == 0, gun_hidden
    assert terrain.getpixel((620, 625))[3] == 255
    assert terrain.getchannel('A').histogram()[1:255] == [0]*254
    assert terrain.getpixel((0, 0))[3] == 0
    widths = []
    for site in sites:
        def floor(u):
            point = tuple(round(n) for n in world(site['anchor'], site['angle'], u, -20))
            r, g, b, a = terrain.getpixel(point)
            return a == 255 and r-g < 10 and b > g and 95 < r < 190
        assert floor(0), (site['site'], 'floor center missing')
        left = right = 0
        while left > -250 and floor(left-1):
            left -= 1
        while right < 250 and floor(right+1):
            right += 1
        widths.append(right-left+1)
    assert max(widths)-min(widths) <= 16, ('socket widths differ', widths)
    result = dict(scope='offline rendered support and occlusion', sites=4,
                  foundation_samples_supported=supported, upper_base_pixels_hidden=upper_hidden,
                  gun_pixels_hidden=gun_hidden, gun_pixels_checked=gun_samples, aiming_poses=list(AIMS),
                  seating_floor_widths_at_20_pixels_outward=widths,
                  maximum_width_difference_pixels=max(widths)-min(widths))
    (OUT/'verification.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    build()
