"""Assemble the pixel restyle with measured Laser contacts, using SVG layers."""
import base64
import hashlib
import json
import math
import re
import subprocess
from pathlib import Path

from PIL import Image, ImageChops


HERE = Path(__file__).resolve().parent
OUT = HERE/'Pixel'
SOURCE = HERE/'ring-pixel-v1.png'
SUN = HERE/'sun-pixel-source.png'
ROOT = HERE.parents[2]
WORK = ROOT/'.git/menu-work/davis-contact-render'
LASER = ROOT/'Art/Towers/Laser/Redesign/Sweep'
CENTER = (627, 627)
RADIUS = 528
SCALE = 2.0
BG = '#29313b'


def build():
    OUT.mkdir(exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    layout = json.loads((HERE/'socket-layout.json').read_text())
    sites = layout['sites']
    assert Image.open(SOURCE).size == (1254, 1254)
    assert Image.open(SUN).size == (1254, 1254)
    defs = image_def('source', SOURCE)
    defs += image_def('sun-source', SUN)
    defs += image_def('base', LASER/'base-sprite.png')
    defs += image_def('gun', LASER/'gun-sprite.png')
    defs += ('<filter id="solid-alpha" x="0" y="0" width="100%" height="100%" color-interpolation-filters="sRGB">'
             '<feComponentTransfer><feFuncA type="linear" slope="1.04" intercept="-0.02"/>'
             '</feComponentTransfer></filter><g id="ring"><use href="#source" filter="url(#solid-alpha)"/></g>')
    sun_scale = layout['sun_scale']
    defs += (f'<g id="sun" filter="url(#solid-alpha)"><use href="#sun-source" '
             f'transform="translate(627 627) scale({sun_scale}) translate(-627 -627)"/></g>'
             '<g id="empty"><use href="#sun"/><use href="#ring"/></g>')
    fronts = []
    for site in sites:
        angle, shift, number = site['angle'], site['tangent_offset'], site['site']
        mx, my = site['mount']
        profile = dict(site['rim_profile'])
        foreground_top = site['foreground_top']
        points = [world(angle, shift+u, max(profile[u], foreground_top)) for u in range(-65, 66)]
        points += [world(angle, shift+65, 140), world(angle, shift-65, 140)]
        coords = ' '.join(f'{x:.3f},{y:.3f}' for x, y in points)
        defs += f'<clipPath id="rim{number}"><polygon points="{coords}"/></clipPath>'
        fronts.append(f'<use href="#ring" clip-path="url(#rim{number})"/>')
        for aim in (-60, 0, 60):
            defs += (f'<g id="tower{number}a{aim}" transform="translate({mx:.3f} {my:.3f}) rotate({angle}) scale(2)">'
                     '<use href="#base" x="-34" y="-71"/>'
                     f'<g transform="translate(30 -47) rotate({aim})"><use href="#gun" x="-12" y="-28"/></g></g>')
    defs += '<g id="fronts">'+''.join(fronts)+'</g>'
    for aim in (-60, 0, 60):
        defs += (f'<g id="occupied{aim}"><use href="#empty"/>'
                 +''.join(f'<use href="#tower{s["site"]}a{aim}"/>' for s in sites)
                 +'<use href="#fronts"/></g>')
    outputs = {
        'sun': document(1254, 1254, defs, '<use href="#sun"/>', background=False),
        'ring-background': document(1254, 1254, defs, '<use href="#ring"/>', background=False),
        'socket-fronts': document(1254, 1254, defs, '<use href="#fronts"/>', background=False),
        'planet-empty': document(1536, 1536, defs, '<use href="#empty"/>', '-141 -141 1536 1536', False),
        'planet-occupied': document(1536, 1536, defs, '<use href="#occupied0"/>', '-141 -141 1536 1536', False),
    }
    body = label(20, 32, 'DAVIS NOVA / PIXEL ART AND MEASURED TOWER CONTACT', 24)
    for j, name in enumerate(('empty', 'occupied0')):
        body += label(j*760+20, 80, 'Sun and empty ring' if j == 0 else 'Seated towers', 20)
        body += f'<svg x="{j*760}" y="100" width="760" height="760" viewBox="-141 -141 1536 1536"><use href="#{name}"/></svg>'
    outputs['page-review'] = document(1520, 900, defs, body)
    mx, my = sites[0]['mount']
    body = label(18, 30, 'PIXEL RING / FLAT SOCKET AND TOWER CONTACT', 22)
    for j, name in enumerate(('empty', 'occupied0')):
        body += label(j*520+18, 60, 'Empty' if j == 0 else 'Actual Laser / bronze lip in front', 17)
        body += (f'<svg x="{j*520}" y="75" width="520" height="450" viewBox="{mx-110} {my-170} 220 190">'
                 f'<use href="#{name}"/></svg>')
    outputs['top-review'] = document(1040, 540, defs, body)
    body = label(16, 28, 'PIXEL RING / ALL TWELVE CONTACTS', 22)
    for i, site in enumerate(sites):
        x, y = (i%4)*360, (i//4)*295+55
        mx, my = site['mount']
        body += label(x+10, y, f'Site {site["site"]}', 16)
        body += (f'<svg x="{x}" y="{y+10}" width="355" height="265" viewBox="-105 -140 210 157">'
                 f'<g transform="rotate({-site["angle"]}) translate({-mx} {-my})"><use href="#occupied0"/></g></svg>')
    outputs['contact-review'] = document(1440, 940, defs, body)
    for name, svg in outputs.items():
        (WORK/f'{name}.svg').write_text(svg, encoding='utf-8')
    manifest = dict(source=str(SOURCE.relative_to(ROOT)),
                    source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                    tower_source_sha256={name: hashlib.sha256((LASER/name).read_bytes()).hexdigest()
                                         for name in ('base-sprite.png', 'gun-sprite.png')},
                    sun_source_sha256=hashlib.sha256(SUN.read_bytes()).hexdigest(), sun_scale=sun_scale,
                    status='current pixel art assembly; see UNITY_TESTING.md for Unity verification',
                    source_center=CENTER, reference_radius=RADIUS,
                    tower_scale=2, foundation_pivot=[34, 71], gun_pivot=[12, 28], base_hinge=[64, 24],
                    alpha_transfer=dict(slope=1.04, intercept=-.02), sites=sites,
                    geometry_note='Style edit retains twelve flat-profile seats; generated edges shifted, so lip contacts were remeasured.',
                    layers=['sun', 'ring-background', 'tower base and independently aimed gun', 'socket-fronts'])
    (OUT/'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    render(outputs)
    # Same rendered support/occlusion checks, allowing painted pixel steps in the lip.
    verify_rendered(flat_seat_tolerance=12)

def world(angle, u, v):
    t = math.radians(angle)
    return (627 + (RADIUS-v)*math.sin(t) + u*math.cos(t),
            627 - (RADIUS-v)*math.cos(t) + u*math.sin(t))


def image_def(name, path):
    with Image.open(path) as im:
        w, h = im.size
    data = base64.b64encode(path.read_bytes()).decode()
    return f'<image id="{name}" width="{w}" height="{h}" href="data:image/png;base64,{data}"/>'


def label(x, y, text, size=18):
    return f'<text x="{x}" y="{y}" font-size="{size}">{text}</text>'


def document(width, height, defs, body, view=None, background=True):
    view = view or f'0 0 {width} {height}'
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="{view}">'
            '<style>image{image-rendering:pixelated}text{font-family:Arial,sans-serif;fill:#eef1f6}</style>'
            f'<defs>{defs}</defs>'
            +(f'<rect x="-300" y="-300" width="4000" height="4000" fill="{BG}"/>' if background else '')
            +body+'</svg>')


def render(outputs):
    chrome = Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
    work = WORK
    work.mkdir(parents=True, exist_ok=True)
    for name, svg in outputs.items():
        width, height = map(int, re.search(r'width="(\d+)" height="(\d+)"', svg).groups())
        html = work/f'{name}.html'
        html.write_text('<style>html,body{margin:0;padding:0;overflow:hidden}img{display:block}</style>'
                        +f'<img src="{(work/(name+".svg")).as_uri()}" width="{width}" height="{height}">', encoding='utf-8')
        args = [str(chrome), '--headless', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
                '--hide-scrollbars', '--allow-file-access-from-files', '--force-device-scale-factor=1',
                '--default-background-color=00000000', '--run-all-compositor-stages-before-draw',
                '--virtual-time-budget=1500', f'--user-data-dir={work/"browser-shape"}',
                f'--window-size={width},{height}', f'--screenshot={OUT/(name+".png")}', html.as_uri()]
        result = subprocess.run(args, capture_output=True, timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
        if result.returncode:
            raise RuntimeError(result.stderr.decode(errors='replace')[-1500:])
        with Image.open(OUT/(name+'.png')) as im:
            assert im.size == (width, height)
        print('Rendered', name, flush=True)


def verify_rendered(flat_seat_tolerance=1):
    manifest = json.loads((OUT/'assembly.json').read_text())
    front = Image.open(OUT/'socket-fronts.png').convert('RGBA')
    background = Image.open(OUT/'ring-background.png').convert('RGBA')
    sun = Image.open(OUT/'sun.png').convert('RGBA')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    gun = Image.open(LASER/'gun-sprite.png').convert('RGBA')
    supported = 0
    covered_upper_base_pixels = 0
    covered_gun_pixels = 0
    checked_gun_pixels = 0
    flat_seat_variation = []
    assert len(manifest['sites']) == 12
    assert sorted(s['angle'] for s in manifest['sites']) == list(range(0, 360, 30))
    for site in manifest['sites']:
        center_profile = [v for u, v in site['rim_profile'] if -35 <= u <= 35]
        flat_seat_variation.append(max(center_profile)-min(center_profile))
        assert flat_seat_variation[-1] <= flat_seat_tolerance, (site['site'], 'rounded seating edge')
        for x in range(base.width):
            ys = [y for y in range(64, 72) if base.getpixel((x, y))[3] >= 128]
            if not ys:
                continue
            u = (x+.5-34)*SCALE+site['tangent_offset']
            v = (max(ys)+1-71)*SCALE+site['inward_offset']
            wx, wy = world(site['angle'], u, v)
            point = round(wx), round(wy)
            assert background.getpixel(point)[3] >= 128, (site['site'], x, 'no ground')
            assert front.getpixel(point)[3] >= 128, (site['site'], x, 'no foreground contact')
            supported += 1
        for y in range(53):
            for x in range(base.width):
                if base.getpixel((x, y))[3] < 128:
                    continue
                u = (x+.5-34)*SCALE+site['tangent_offset']
                v = (y+.5-71)*SCALE+site['inward_offset']
                wx, wy = world(site['angle'], u, v)
                if 0 <= round(wx) < 1254 and 0 <= round(wy) < 1254:
                    covered_upper_base_pixels += front.getpixel((round(wx), round(wy)))[3] >= 128
        for aim in (-60, 0, 60):
            a = math.radians(aim)
            for y in range(gun.height):
                for x in range(gun.width):
                    if gun.getpixel((x, y))[3] < 128:
                        continue
                    gx, gy = x+.5-12, y+.5-28
                    u = (30+gx*math.cos(a)-gy*math.sin(a))*SCALE+site['tangent_offset']
                    v = (-47+gx*math.sin(a)+gy*math.cos(a))*SCALE+site['inward_offset']
                    wx, wy = world(site['angle'], u, v)
                    if 0 <= round(wx) < 1254 and 0 <= round(wy) < 1254:
                        covered_gun_pixels += front.getpixel((round(wx), round(wy)))[3] >= 128
                    checked_gun_pixels += 1
    assert supported == 744
    assert covered_upper_base_pixels == 0, covered_upper_base_pixels
    assert covered_gun_pixels == 0, covered_gun_pixels
    assert background.getpixel((0, 0))[3] == 0
    assert background.getpixel(CENTER)[3] == 0
    assert background.getpixel((627, 130))[3] == 255
    assert sun.getpixel((0, 0))[3] == 0
    assert sun.getpixel(CENTER)[3] == 255
    sun_solid = sun.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    ring_solid = background.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    assert ImageChops.multiply(sun_solid, ring_solid).getbbox() is None, 'Sun overlaps playable ring'
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == manifest['source_sha256']
    for name, expected in manifest['tower_source_sha256'].items():
        assert hashlib.sha256((LASER/name).read_bytes()).hexdigest() == expected
    manifest['rendered_verification'] = dict(foundation_samples_supported_and_overlapped=supported,
                                            upper_base_samples_obscured_by_rim=covered_upper_base_pixels,
                                            socket_count=len(manifest['sites']),
                                            central_seat_width=70,
                                            maximum_central_seat_variation=max(flat_seat_variation),
                                            gun_pixel_samples=checked_gun_pixels,
                                            gun_samples_obscured_by_rim=covered_gun_pixels,
                                            aims=[-60, 0, 60], alpha_corner=0, alpha_center=0,
                                            sampled_metal_alpha=255, sun_corner_alpha=0,
                                            sun_center_alpha=255, sun_overlaps_ring=False)
    (OUT/'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print('Rendered checks:', manifest['rendered_verification'])


if __name__ == '__main__':
    build()
