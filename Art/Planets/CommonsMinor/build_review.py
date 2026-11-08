"""Fit the shared Laser to eight shallow Commons earth lips and render sprite layers."""
import base64
import hashlib
import json
import math
from pathlib import Path
import subprocess

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SOURCE = HERE/'commons-source-v6.png'
LASER = ROOT/'Art/Towers/Laser/Redesign/Sweep'
OUT = HERE/'FittedSites'
WORK = ROOT/'.git/menu-work/commons-minor-render'
ANCHORS = [(627, 353), (889, 455), (1004, 697), (879, 992),
           (627, 1098), (372, 992), (250, 697), (363, 455)]
ANGLES = (0, 36, 90, 135, 180, 225, 270, 324)
SCALE = 1.75
DISPLAY_SIZE = 600*1.25/SCALE
PROFILE_HALF_WIDTH = 66
AIMS = (-60, 0, 60)


def world(anchor, angle, u, v):
    a = math.radians(angle)
    return (anchor[0]+u*math.cos(a)-v*math.sin(a),
            anchor[1]+u*math.sin(a)+v*math.cos(a))


def image_def(name, path):
    with Image.open(path) as im:
        width, height = im.size
    data = base64.b64encode(path.read_bytes()).decode()
    return f'<image id="{name}" width="{width}" height="{height}" href="data:image/png;base64,{data}"/>'


def document(width, height, defs, body, view=None):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
            f'viewBox="{view or f"0 0 {width} {height}"}"><style>image{{image-rendering:pixelated}}'
            'text{font-family:Arial,sans-serif;fill:#eef0dd}</style>'
            f'<defs>{defs}</defs>{body}</svg>')


def label(x, y, text, size=20):
    return f'<text x="{x}" y="{y}" font-size="{size}">{text}</text>'


def render(name, svg, size):
    path = WORK/f'{name}.svg'
    path.write_text(svg, encoding='utf-8')
    html = WORK/f'{name}.html'
    html.write_text('<style>html,body{margin:0;overflow:hidden}img{display:block}</style>'
                    f'<img src="{path.as_uri()}">', encoding='utf-8')
    result = subprocess.run(['C:/Program Files/Google/Chrome/Application/chrome.exe',
        '--headless', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        '--hide-scrollbars', '--allow-file-access-from-files', '--force-device-scale-factor=1',
        '--default-background-color=00000000', '--run-all-compositor-stages-before-draw',
        '--virtual-time-budget=1000', f'--user-data-dir={WORK/"browser"}',
        f'--window-size={size[0]},{size[1]}', f'--screenshot={OUT/(name+".png")}', html.as_uri()],
        capture_output=True, timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
    assert result.returncode == 0, result.stderr.decode(errors='replace')[-1000:]
    assert Image.open(OUT/(name+'.png')).size == size
    print('Rendered', name, flush=True)


def profile(source, anchor, angle):
    edge = {}
    for u in range(-PROFILE_HALF_WIDTH, PROFILE_HALF_WIDTH+1):
        warm = []
        for v in range(-35, 61):
            point = tuple(round(n) for n in world(anchor, angle, u, v))
            r, g, b, alpha = source.getpixel(point)
            if alpha >= 230 and r > 185 and g > 145 and b > 95 and r-g > 10 and g-b > 15:
                warm.append(v)
        starts = [v for v in warm if v+1 in warm]
        assert starts, (anchor, u, 'missing pale earth lip')
        edge[u] = starts[0]
    return edge


def build():
    OUT.mkdir(exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    source = Image.open(SOURCE).convert('RGBA')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    feet = []
    for x in range(base.width):
        ys = [y for y in range(64, 72) if base.getpixel((x, y))[3] >= 128]
        if ys:
            feet.append(((x+.5-34)*SCALE, (max(ys)+1-71)*SCALE))
    defs = ''.join(image_def(n, p) for n, p in [('source', SOURCE),
                  ('base', LASER/'base-sprite.png'), ('gun', LASER/'gun-sprite.png')])
    defs += ('<filter id="solid" color-interpolation-filters="sRGB"><feComponentTransfer>'
             '<feFuncA type="discrete" tableValues="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1"/>'
             '</feComponentTransfer></filter><g id="terrain" filter="url(#solid)"><use href="#source"/></g>')
    sites, fronts = [], []
    for i, anchor in enumerate(ANCHORS):
        angle = ANGLES[i]
        edge = profile(source, anchor, angle)
        inset = max(edge[round(u)]-v for u, v in feet)+5
        mount = world(anchor, angle, 0, inset)
        # Only the foundation band belongs in front of towers; rear earth walls
        # stay behind operators and weapon mechanisms.
        front_edge = {u: max(v, inset-17*SCALE) for u, v in edge.items()}
        polygon = [world(anchor, angle, u, v) for u, v in front_edge.items()]
        polygon += [world(anchor, angle, PROFILE_HALF_WIDTH, 65),
                    world(anchor, angle, -PROFILE_HALF_WIDTH, 65)]
        coords = ' '.join(f'{x:.3f},{y:.3f}' for x, y in polygon)
        defs += f'<clipPath id="lip{i}"><polygon points="{coords}"/></clipPath>'
        fronts.append(f'<use href="#terrain" clip-path="url(#lip{i})"/>')
        overlaps = [inset+v-edge[round(u)] for u, v in feet]
        sites.append(dict(site=i+1, angle=angle, anchor=anchor, mount=mount,
                          foundation_inset=inset, rim_profile=list(edge.items()),
                          foreground_profile=list(front_edge.items()),
                          minimum_rim_overlap=min(overlaps), maximum_rim_overlap=max(overlaps)))
        for aim in AIMS:
            defs += (f'<g id="tower{i}a{aim}" transform="translate({mount[0]} {mount[1]}) '
                     f'rotate({angle}) scale({SCALE})"><use href="#base" x="-34" y="-71"/>'
                     f'<g transform="translate(30 -47) rotate({aim})">'
                     '<use href="#gun" x="-12" y="-28"/></g></g>')
    defs += '<g id="fronts">'+''.join(fronts)+'</g>'
    for aim in AIMS:
        defs += (f'<g id="occupied{aim}"><use href="#terrain"/>'
                 + ''.join(f'<use href="#tower{i}a{aim}"/>' for i in range(8))
                 + '<use href="#fronts"/></g>')
    for name, group in [('terrain-background', 'terrain'), ('socket-fronts', 'fronts'),
                        ('planet-empty', 'terrain'), ('planet-occupied', 'occupied0')]:
        render(name, document(1254, 1254, defs, f'<use href="#{group}"/>'), (1254, 1254))
    body = '<rect width="1440" height="820" fill="#172923"/>'
    body += label(24, 35, 'COMMONS MINOR / GRASSY PLANET / EIGHT FITTED DEFENSE SOCKETS', 24)
    for i, group in enumerate(('terrain', 'occupied0')):
        body += label(24+i*720, 74, 'Shallow terrain sockets' if i == 0 else 'Existing Laser / foundations behind thin earth lips')
        body += f'<svg x="{i*720}" y="90" width="720" height="720" viewBox="0 0 1254 1254"><use href="#{group}"/></svg>'
    render('page-review', document(1440, 820, defs, body), (1440, 820))
    body = '<rect width="1260" height="1840" fill="#26362e"/>'
    body += label(20, 32, 'ALL EIGHT SOCKETS / EMPTY AND THREE AIMING POSES / UPRIGHT CONTACT VIEWS', 22)
    for i, site in enumerate(sites):
        y = 65+i*220
        body += label(15, y, f'Site {i+1} / {site["angle"]} degrees clockwise', 18)
        mx, my = site['mount']
        for j, group in enumerate(('terrain', 'occupied-60', 'occupied0', 'occupied60')):
            body += (f'<svg x="{j*315}" y="{y+10}" width="310" height="200" viewBox="-75 -140 190 155">'
                     f'<g transform="rotate({-site["angle"]}) translate({-mx} {-my})"><use href="#{group}"/></g></svg>')
    render('contact-review', document(1260, 1840, defs, body), (1260, 1840))
    verify(sites, feet)
    manifest = dict(status='V6 follows user request: V4 soft shoulders with wider, flatter seating middles; fit review pending.', source=SOURCE.name,
        source_size=list(source.size), tower_scale=SCALE, canvas_display_width=DISPLAY_SIZE,
        socket_shape='Soft curved grass shoulders with widened flat middle and a continuous thin pale earth rim.',
        display_scale_rule='Preserve the previous Commons and approved moon displayed tower size while fitting the broader sockets.',
        base_sprite_display_size=[70*SCALE*DISPLAY_SIZE/1254, 72*SCALE*DISPLAY_SIZE/1254],
        foundation_pivot=[34, 71], gun_pivot=[12, 28], base_hinge=[64, 24],
        render_order=['terrain-background', 'towers', 'socket-fronts'], sites=sites,
        source_sha256={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                       for p in (SOURCE, LASER/'base-sprite.png', LASER/'gun-sprite.png')},
        limits=['Measured generated socket contours; mount positions and outward orientations fitted to the artwork. Upper diagonal floors use 36/324 degrees.',
                'Offline artwork checks are not Unity collision or gameplay tests.'])
    (OUT/'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')


def verify(sites, feet):
    terrain = Image.open(OUT/'terrain-background.png').convert('RGBA')
    front = Image.open(OUT/'socket-fronts.png').convert('RGBA')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    gun = Image.open(LASER/'gun-sprite.png').convert('RGBA')
    supported = upper_hidden = gun_hidden = gun_samples = 0
    missing = []
    def alpha(im, p):
        x, y = (round(n) for n in p)
        return im.getpixel((x, y))[3] if 0 <= x < im.width and 0 <= y < im.height else 0
    for site in sites:
        for u, v in feet:
            p = world(site['mount'], site['angle'], u, v)
            assert alpha(terrain, p) >= 240, (site['site'], p, 'unsupported foundation')
            if alpha(front, p) >= 240:
                supported += 1
            else:
                missing.append((site['site'], u, v, alpha(front, p)))
        for y in range(53):
            for x in range(base.width):
                if base.getpixel((x, y))[3] >= 128:
                    upper_hidden += alpha(front, world(site['mount'], site['angle'],
                        (x+.5-34)*SCALE, (y+.5-71)*SCALE)) >= 128
        for aim in AIMS:
            a = math.radians(aim)
            for y in range(gun.height):
                for x in range(gun.width):
                    if gun.getpixel((x, y))[3] < 128:
                        continue
                    u, v = x+.5-12, y+.5-28
                    p = world(site['mount'], site['angle'],
                        (30+u*math.cos(a)-v*math.sin(a))*SCALE,
                        (-47+u*math.sin(a)+v*math.cos(a))*SCALE)
                    gun_hidden += alpha(front, p) >= 128
                    gun_samples += 1
    result = dict(scope='offline rendered foundation and occlusion checks', sites=8,
        foundation_samples_supported=supported, upper_base_pixels_hidden=upper_hidden,
        gun_pixels_hidden=gun_hidden, gun_pixels_checked=gun_samples, aiming_poses=list(AIMS))
    (OUT/'verification.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result), flush=True)
    assert supported == 496, missing
    assert upper_hidden == 0, upper_hidden
    assert gun_hidden == 0, gun_hidden


if __name__ == '__main__':
    build()
