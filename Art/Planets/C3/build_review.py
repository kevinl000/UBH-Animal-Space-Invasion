"""Render C3's generated sprites and measured Laser contacts with SVG layers.

Pillow reads source/rendered pixels for validation only; Chrome renders the
sprite transforms, alpha cleanup, occlusion masks and review compositions.
"""
import base64
import hashlib
import json
import math
import re
import subprocess
from pathlib import Path

from PIL import Image


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = HERE / 'FittedSites'
WORK = ROOT / '.git/menu-work/c3-render'
LASER = ROOT / 'Art/Towers/Laser/Redesign/Sweep'
THEMES = ('grill', 'salad', 'bakery')
SIZE = 1254
CENTER = (620, 625)
TOWER_SCALE = 3.125
AIMS = (-60, 0, 60)
PROFILE_WIDTH = 108
PREVIEW_ORBIT_RADIUS = 376.2
PREVIEW_PLANET_SCALE = .40


def world(angle, u, v):
    a = math.radians(angle)
    return (CENTER[0] + u*math.cos(a) - (v-CENTER[1])*math.sin(a),
            CENTER[1] + u*math.sin(a) + (v-CENTER[1])*math.cos(a))


def image_def(name, path):
    with Image.open(path) as im:
        w, h = im.size
    data = base64.b64encode(path.read_bytes()).decode()
    return f'<image id="{name}" width="{w}" height="{h}" href="data:image/png;base64,{data}"/>'


def document(w, h, defs, body, view=None):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
            f'viewBox="{view or f"0 0 {w} {h}"}"><style>image{{image-rendering:pixelated}}'
            'text{font-family:Arial,sans-serif;fill:#f5ecd8}</style>'
            f'<defs>{defs}</defs>{body}</svg>')


def label(x, y, value, size=19):
    return f'<text x="{x}" y="{y}" font-size="{size}">{value}</text>'


def render(outputs):
    chrome = Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
    for name, svg in outputs.items():
        path = WORK / f'{name}.svg'
        path.write_text(svg, encoding='utf-8')
        w, h = map(int, re.search(r'width="(\d+)" height="(\d+)"', svg).groups())
        html = WORK / f'{name}.html'
        html.write_text('<style>html,body{margin:0;padding:0;overflow:hidden}img{display:block}</style>'
                        f'<img src="{path.as_uri()}" width="{w}" height="{h}">', encoding='utf-8')
        result = subprocess.run([str(chrome), '--headless', '--disable-gpu', '--no-first-run',
            '--no-default-browser-check', '--hide-scrollbars', '--allow-file-access-from-files',
            '--force-device-scale-factor=1', '--default-background-color=00000000',
            '--run-all-compositor-stages-before-draw', '--virtual-time-budget=1000',
            f'--user-data-dir={WORK / "browser"}', f'--window-size={w},{h}',
            f'--screenshot={OUT / (name+".png")}', html.as_uri()],
            capture_output=True, timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
        if result.returncode:
            raise RuntimeError(result.stderr.decode(errors='replace')[-1200:])
        assert Image.open(OUT / (name+'.png')).size == (w, h)
        print('Rendered', name, flush=True)


def measure_profile(source, angle):
    profile = {}
    for u in range(-160, 161):
        warm = []
        for y in range(140, 310):
            point = tuple(round(n) for n in world(angle, u, y))
            r, g, b, alpha = source.getpixel(point)
            if alpha >= 240 and r > 170 and r-g > 3 and g-b > 8:
                warm.append(y)
        starts = [y for y in warm if all(y+j in warm for j in (1, 2, 3))]
        if starts:
            profile[u] = starts[0]
    return profile


def build():
    OUT.mkdir(exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    base = Image.open(LASER / 'base-sprite.png').convert('RGBA')
    feet = []
    for x in range(base.width):
        ys = [y for y in range(64, 72) if base.getpixel((x, y))[3] >= 128]
        if ys:
            feet.append(((x+.5-34)*TOWER_SCALE, (max(ys)+1-71)*TOWER_SCALE))
    defs = image_def('base', LASER / 'base-sprite.png') + image_def('gun', LASER / 'gun-sprite.png')
    # Generated pixels stay intact; only source alpha and layer masks change.
    defs += ('<filter id="solid-alpha" color-interpolation-filters="sRGB">'
             '<feComponentTransfer><feFuncA type="discrete" '
             'tableValues="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1"/></feComponentTransfer></filter>')
    outputs, planets = {}, []
    for theme in THEMES:
        source_path = HERE / f'{theme}-source-v1.png'
        source = Image.open(source_path).convert('RGBA')
        assert source.size == (SIZE, SIZE)
        defs += image_def(theme+'-source', source_path)
        defs += (f'<g id="{theme}-terrain" filter="url(#solid-alpha)">'
                 f'<use href="#{theme}-source"/></g>')
        sites, fronts = [], []
        for i in range(6):
            angle = i*60
            raw_profile = measure_profile(source, angle)
            fits = []
            for shift in range(-40, 41):
                if not all(u+shift in raw_profile for u in range(-PROFILE_WIDTH, PROFILE_WIDTH+1)):
                    continue
                insets = [raw_profile[round(u)+shift]-v for u, v in feet]
                fits.append((max(insets)-min(insets), abs(shift), shift, max(insets)+3))
            assert fits, (theme, i+1, 'no complete lip profile')
            _, _, shift, mount_y = min(fits)
            profile = {u: raw_profile[u+shift] for u in range(-PROFILE_WIDTH, PROFILE_WIDTH+1)}
            mount = world(angle, shift, mount_y)
            # Tall side walls stay behind the upper chassis; only the lower
            # painted lip crosses in front of the tower foundation.
            foreground_top = mount_y-54
            edge = [world(angle, shift+u, max(v, foreground_top)) for u, v in profile.items()]
            edge += [world(angle, shift+PROFILE_WIDTH, 315), world(angle, shift-PROFILE_WIDTH, 315)]
            coords = ' '.join(f'{x:.4f},{y:.4f}' for x, y in edge)
            defs += f'<clipPath id="{theme}-lip{i}"><polygon points="{coords}"/></clipPath>'
            fronts.append(f'<use href="#{theme}-terrain" clip-path="url(#{theme}-lip{i})"/>')
            sites.append(dict(site=i+1, clockwise_angle=angle, mount=list(mount),
                              tangent_offset=shift, lip_profile=list(profile.items()),
                              foreground_top=foreground_top,
                              minimum_lip_overlap=min(mount_y+v-profile[round(u)] for u, v in feet),
                              maximum_lip_overlap=max(mount_y+v-profile[round(u)] for u, v in feet)))
            for aim in AIMS:
                defs += (f'<g id="{theme}-tower{i}a{aim}" transform="translate({mount[0]} {mount[1]}) '
                         f'rotate({angle}) scale({TOWER_SCALE})"><use href="#base" x="-34" y="-71"/>'
                         f'<g transform="translate(30 -47) rotate({aim})">'
                         '<use href="#gun" x="-12" y="-28"/></g></g>')
        defs += f'<g id="{theme}-fronts">'+''.join(fronts)+'</g>'
        for aim in AIMS:
            defs += (f'<g id="{theme}-occupied{aim}"><use href="#{theme}-terrain"/>'
                     +''.join(f'<use href="#{theme}-tower{i}a{aim}"/>' for i in range(6))
                     +f'<use href="#{theme}-fronts"/></g>')
        planets.append(dict(theme=theme, source=source_path.name,
                            source_sha256=hashlib.sha256(source_path.read_bytes()).hexdigest(),
                            socket_note='Each original generated recess is measured independently; no perimeter patches.',
                            sites=sites))
    for theme in THEMES:
        for filename, group in [('terrain-background', 'terrain'), ('socket-fronts', 'fronts')]:
            outputs[f'{theme}-{filename}'] = document(SIZE, SIZE, defs, f'<use href="#{theme}-{group}"/>')
        for kind, group in [('empty', 'terrain'), ('occupied', 'occupied0')]:
            outputs[f'{theme}-{kind}'] = document(1536, 1536, defs, f'<use href="#{theme}-{group}"/>', '-141 -141 1536 1536')
    body = '<rect width="1800" height="1240" fill="#171d2d"/>'
    body += label(24, 36, 'C3 / THREE BUFFET WORLDS / SIX SOCKETS EACH / UNCHANGED LASER TOWERS', 25)
    for col, theme in enumerate(THEMES):
        body += label(col*600+24, 78, theme.upper(), 24)
        for row, group in enumerate(('terrain', 'occupied0')):
            body += (f'<svg x="{col*600}" y="{90+row*565}" width="600" height="550" '
                     f'viewBox="-141 -141 1536 1536"><use href="#{theme}-{group}"/></svg>')
    outputs['page-review'] = document(1800, 1240, defs, body)
    body = '<rect width="1440" height="1590" fill="#252d38"/>'
    body += label(20, 34, 'C3 / ALL 18 SOCKETS / EMPTY AND -60, 0, +60 DEGREE AIMING POSES', 22)
    for p, planet in enumerate(planets):
        for i, site in enumerate(planet['sites']):
            x, y = (i%3)*480, 60+(p*2+i//3)*250
            body += label(x+10, y, f'{planet["theme"].upper()} / site {i+1} / {site["clockwise_angle"]} degrees', 17)
            mx, my = site['mount']
            for j, group in enumerate(('terrain', 'occupied-60', 'occupied0', 'occupied60')):
                body += (f'<svg x="{x+j*120}" y="{y+12}" width="120" height="205" '
                         f'viewBox="-125 -300 290 440"><g transform="rotate({-site["clockwise_angle"]}) '
                         f'translate({-mx} {-my})"><use href="#{planet["theme"]}-{group}"/></g></svg>')
    outputs['contact-review'] = document(1440, 1590, defs, body)
    # Three independent objects, placed 120 degrees apart with a clear shared center.
    for kind, group in [('empty', 'terrain'), ('occupied', 'occupied0')]:
        body = ''
        for i, theme in enumerate(THEMES):
            a = math.radians(-90+i*120)
            x, y = 900+PREVIEW_ORBIT_RADIUS*math.cos(a), 900+PREVIEW_ORBIT_RADIUS*math.sin(a)
            body += (f'<g transform="translate({x} {y}) scale({PREVIEW_PLANET_SCALE}) '
                     f'translate({-CENTER[0]} {-CENTER[1]})"><use href="#{theme}-{group}"/></g>')
        outputs[f'c3-{kind}'] = document(1800, 1800, defs, body)
    render(outputs)
    verification = verify(planets, feet)
    manifest = dict(status='User-approved C3 artwork, 2026-11-08; see unity-import.json for integration.',
                    source_size=[SIZE, SIZE], center=CENTER, planet_count=3, sockets_per_planet=6,
                    total_sockets=18, tower_scale=TOWER_SCALE, foundation_pivot=[34, 71],
                    gun_pivot=[12, 28], base_hinge=[64, 24],
                    render_order=['terrain-background', 'tower bases and independently aimed guns', 'socket-fronts'],
                    tower_source_sha256={name: hashlib.sha256((LASER/name).read_bytes()).hexdigest()
                                         for name in ('base-sprite.png', 'gun-sprite.png')},
                    preview=dict(orbit_radius=PREVIEW_ORBIT_RADIUS, planet_scale=PREVIEW_PLANET_SCALE,
                                 phase_degrees=[-90, 30, 150],
                                 note='Second spacing adjustment: 180-unit Unity orbit radius, with unchanged 240-unit body canvases.'),
                    planets=planets, verification=verification,
                    limits=['Unity import is recorded separately in ../unity-import.json.',
                            'Three weapon poses sampled; not a continuous aiming collision sweep.',
                            'Sources retained intact; SVG masks separate the original painted foreground lips.'])
    (OUT / 'assembly.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    write_motion_preview()


def verify(planets, feet):
    base = Image.open(LASER / 'base-sprite.png').convert('RGBA')
    gun = Image.open(LASER / 'gun-sprite.png').convert('RGBA')
    results = []

    def alpha(im, point):
        x, y = (round(n) for n in point)
        return im.getpixel((x, y))[3] if 0 <= x < im.width and 0 <= y < im.height else 0

    for planet in planets:
        theme = planet['theme']
        terrain = Image.open(OUT / f'{theme}-terrain-background.png').convert('RGBA')
        front = Image.open(OUT / f'{theme}-socket-fronts.png').convert('RGBA')
        supported = upper_hidden = gun_hidden = gun_samples = 0
        for site in planet['sites']:
            mx, my = site['mount']
            def placed(u, v):
                a = math.radians(site['clockwise_angle'])
                return mx+u*math.cos(a)-v*math.sin(a), my+u*math.sin(a)+v*math.cos(a)
            for u, v in feet:
                point = placed(u, v)
                assert alpha(terrain, point) == 255, (theme, site['site'], point, 'no support')
                assert alpha(front, point) == 255, (theme, site['site'], point, 'no foreground contact')
                supported += 1
            for y in range(53):
                for x in range(base.width):
                    if base.getpixel((x, y))[3] >= 128:
                        upper_hidden += alpha(front, placed((x+.5-34)*TOWER_SCALE, (y+.5-71)*TOWER_SCALE)) >= 128
            for aim in AIMS:
                a = math.radians(aim)
                for y in range(gun.height):
                    for x in range(gun.width):
                        if gun.getpixel((x, y))[3] < 128:
                            continue
                        u, v = x+.5-12, y+.5-28
                        point = placed((30+u*math.cos(a)-v*math.sin(a))*TOWER_SCALE,
                                       (-47+u*math.sin(a)+v*math.cos(a))*TOWER_SCALE)
                        gun_hidden += alpha(front, point) >= 128
                        gun_samples += 1
        assert supported == len(feet)*6
        assert upper_hidden == 0, (theme, upper_hidden)
        assert gun_hidden == 0, (theme, gun_hidden)
        assert terrain.getpixel(CENTER)[3] == 255
        assert terrain.getpixel((0, 0))[3] == 0
        assert terrain.getchannel('A').histogram()[1:255] == [0]*254
        results.append(dict(theme=theme, sites=6, foundation_contacts_supported=supported,
                            upper_base_pixels_hidden=upper_hidden, gun_pixels_hidden=gun_hidden,
                            gun_pixels_checked=gun_samples, aiming_poses=list(AIMS)))
    # Conservative disks include every base/gun pixel at all three sampled poses.
    occupied_radius = 0
    for planet in planets:
        for site in planet['sites']:
            radial = math.dist(site['mount'], CENTER)
            occupied_radius = max(occupied_radius, radial+TOWER_SCALE*110)
    clearance = PREVIEW_ORBIT_RADIUS*math.sqrt(3)-2*occupied_radius*PREVIEW_PLANET_SCALE
    assert clearance > 0, ('preview planet envelopes overlap', clearance)
    report = dict(scope='Offline rendered contacts and lip occlusion; not Unity validation.',
                  planets=results, total_sites=18,
                  foundation_contacts_supported=sum(r['foundation_contacts_supported'] for r in results),
                  conservative_preview_envelope_clearance_pixels=clearance)
    (OUT / 'verification.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(report), flush=True)
    return report


def write_motion_preview():
    # Presentation controls explicitly expose demo-only motion assumptions.
    html = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>C3 buffet worlds — art motion preview</title><style>
*{box-sizing:border-box}body{margin:0;background:#111725;color:#f5ecd8;font:16px system-ui}
main{max-width:1100px;margin:auto;padding:20px}h1{font-size:22px;margin:0 0 8px}p{color:#b9c6db}
canvas{display:block;width:100%;height:auto;background:#171f32;border-radius:12px;image-rendering:pixelated}
.controls{display:flex;gap:18px;align-items:center;flex-wrap:wrap;margin:16px 0}button,input{accent-color:#dfb370}
button{padding:8px 16px;background:#34455e;color:white;border:0;border-radius:6px}label{display:flex;align-items:center;gap:8px}
</style><main><h1>C3 / three buffet worlds / 18 fitted sockets</h1>
<p>Grill, salad bar, bakery. Centers are 16.4% closer than the first Unity version, with space around occupied sockets. Motion settings are for presentation.</p>
<div class="controls"><button id="pause">Pause</button><label><input id="occupied" type="checkbox" checked>Show existing Laser towers</label>
<label>Orbit <input id="orbit" type="range" min="-12" max="12" value="4"><output id="orbitValue">4°/s</output></label>
<label>Spin <input id="spin" type="range" min="-30" max="30" value="10"><output id="spinValue">10°/s</output></label>
<button id="reset">Reset</button></div><canvas id="view" width="1800" height="1800" aria-label="Three independently spinning six-socket planets orbiting a shared empty center"></canvas>
<p>Art review only. Separate terrain, towers, and socket-front layers are exported beside this file. Source PNGs and prompts are preserved one directory above.</p></main>
<script>
const themes=['grill','salad','bakery'], frames={}, canvas=document.getElementById('view'), ctx=canvas.getContext('2d');
const pause=document.getElementById('pause'), occupied=document.getElementById('occupied'), orbit=document.getElementById('orbit'), spin=document.getElementById('spin');
const orbitRadius=500;
let running=true, orbitAngle=0, spinAngle=0, previous;
async function load(){await Promise.all(themes.flatMap(theme=>['empty','occupied'].map(kind=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{frames[theme+'-'+kind]=im;resolve()};im.onerror=reject;im.src=theme+'-'+kind+'.png'}))));canvas.dataset.ready='true';requestAnimationFrame(draw)}
function draw(now){const dt=previous===undefined?0:Math.min((now-previous)/1000,.1);previous=now;if(running){orbitAngle+=dt*Number(orbit.value)*Math.PI/180;spinAngle+=dt*Number(spin.value)*Math.PI/180}
ctx.clearRect(0,0,1800,1800);ctx.imageSmoothingEnabled=false;ctx.strokeStyle='#33425b';ctx.lineWidth=2;ctx.setLineDash([8,15]);ctx.beginPath();ctx.arc(900,900,orbitRadius,0,Math.PI*2);ctx.stroke();ctx.setLineDash([]);
themes.forEach((theme,i)=>{const a=-Math.PI/2+i*Math.PI*2/3+orbitAngle;ctx.save();ctx.translate(900+orbitRadius*Math.cos(a),900+orbitRadius*Math.sin(a));ctx.rotate(spinAngle);ctx.scale(.4,.4);ctx.drawImage(frames[theme+'-'+(occupied.checked?'occupied':'empty')],-761,-766);ctx.restore()});requestAnimationFrame(draw)}
pause.onclick=()=>{running=!running;pause.textContent=running?'Pause':'Resume'};
document.getElementById('reset').onclick=()=>{orbitAngle=spinAngle=0};
orbit.oninput=()=>document.getElementById('orbitValue').textContent=orbit.value+'°/s';spin.oninput=()=>document.getElementById('spinValue').textContent=spin.value+'°/s';
load().catch(()=>{ctx.fillStyle='white';ctx.font='28px sans-serif';ctx.fillText('Unable to load adjacent preview PNGs.',100,100)});
</script></html>'''
    html = html.replace('const orbitRadius=500;', f'const orbitRadius={PREVIEW_ORBIT_RADIUS};')
    (OUT / 'motion-preview.html').write_text(html, encoding='utf-8')


if __name__ == '__main__':
    build()
