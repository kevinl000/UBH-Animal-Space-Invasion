"""Render native sprite transforms and measure coverage offline. Never runs Unity."""
import sys
sys.dont_write_bytecode = True
from pathlib import Path
import json
import math
import re
from PIL import Image, ImageDraw
from prepare_gameplay import ART_SCALE, ROOT, TOWER_NAMES, docs, read

OUT = ROOT / 'Art/Effects/DavisNovaSupernova'
SIZE = 1600
CENTER = SIZE / 2
PPU = 100 / ART_SCALE
METAS = {re.search(r'^guid: (\w+)', p.read_text(), re.M)[1]: p.with_suffix('')
         for p in (ROOT / 'Assets/Art').rglob('*.png.meta')}


def mul(x, y):
    return (x[0]*y[0]+x[1]*y[3], x[0]*y[1]+x[1]*y[4], x[0]*y[2]+x[1]*y[5]+x[2],
            x[3]*y[0]+x[4]*y[3], x[3]*y[1]+x[4]*y[4], x[3]*y[2]+x[4]*y[5]+x[5])


def inverse(m):
    a,b,c,d,e,f=m
    q=a*e-b*d
    return (e/q,-b/q,(b*f-e*c)/q,-d/q,a/q,(d*c-a*f)/q)


def vector(body, key):
    return [float(v) for v in re.search(key+r': \{x: ([^,]+), y: ([^,]+), z: ([^}]+)', body).groups()]


def tower(name, site, aim):
    name = name.replace(' ', '')
    objects = docs(read(f'Assets/Prefabs/Towers/{name}TowerVisual.prefab'))
    transforms = {i:b for c,i,b in objects if c == 4}
    game_transforms = {int(re.search(r'm_GameObject: \{fileID: (\d+)', b)[1]): i for i,b in transforms.items()}
    angle = math.radians(-site['angle'])
    c,s = math.cos(angle),math.sin(angle)
    parent = (1.5*c,-1.5*s,(site['mount'][0]-627)*ART_SCALE/100,
              1.5*s,1.5*c,(627-site['mount'][1])*ART_SCALE/100)
    cache = {}

    def world(i):
        if i in cache: return cache[i]
        b = transforms[i]
        pos,scale = vector(b,'m_LocalPosition'),vector(b,'m_LocalScale')
        z,w = map(float,re.search(r'm_LocalRotation: \{x: [^,]+, y: [^,]+, z: ([^,]+), w: ([^}]+)',b).groups())
        angle = math.atan2(2*w*z,1-2*z*z)
        go = int(re.search(r'm_GameObject: \{fileID: (\d+)',b)[1])
        if any(c == 1 and j == go and 'm_Name: CannonPivot' in body for c,j,body in objects):
            angle = math.radians(aim)
        c,s = math.cos(angle),math.sin(angle)
        local = (c*scale[0],-s*scale[1],pos[0],s*scale[0],c*scale[1],pos[1])
        pid = int(re.search(r'm_Father: \{fileID: (\d+)',b)[1])
        cache[i] = mul(world(pid) if pid else parent,local)
        return cache[i]

    result = Image.new('RGBA',(SIZE,SIZE))
    maximum_radius = 0
    for kind,_,b in objects:
        if kind != 212: continue
        texture = METAS[re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)', b)[1]]
        image = Image.open(texture).convert('RGBA')
        meta = Path(str(texture)+'.meta').read_text()
        ppu = float(re.search(r'spritePixelsToUnits: ([\d.]+)',meta)[1])
        px,py = map(float,re.search(r'spritePivot: \{x: ([^,]+), y: ([^}]+)',meta).groups())
        go = int(re.search(r'm_GameObject: \{fileID: (\d+)',b)[1])
        local = (1/ppu,0,-image.width*px/ppu,0,-1/ppu,image.height*(1-py)/ppu)
        model = mul(world(game_transforms[go]),local)
        bounds = image.getchannel('A').getbbox()
        # Rectangle corners conservatively include every visible sprite pixel.
        for x in (bounds[0],bounds[2]):
            for y in (bounds[1],bounds[3]):
                maximum_radius = max(maximum_radius, math.hypot(model[0]*x+model[1]*y+model[2],model[3]*x+model[4]*y+model[5]))
        project = mul((PPU,0,CENTER,0,-PPU,CENTER),model)
        result.alpha_composite(image.transform(result.size,Image.Transform.AFFINE,inverse(project),Image.Resampling.NEAREST))
    return result, maximum_radius


def layer(path):
    image = Image.open(ROOT/path).convert('RGBA')
    out = Image.new('RGBA',(SIZE,SIZE))
    out.alpha_composite(image,((SIZE-image.width)//2,(SIZE-image.height)//2))
    return out


def main():
    layout = json.loads(read('Art/Planets/DavisNova/Pixel/assembly.json'))
    assert math.isclose(1.5 / ART_SCALE, layout['tower_scale'])
    background = Image.new('RGBA',(SIZE,SIZE),(29,35,48,255))
    for path in ['Sun','Ring']:
        background.alpha_composite(layer(f'Assets/Art/Planets/DavisNova/{path}.png'))
    fronts = layer('Assets/Art/Planets/DavisNova/SocketFronts.png')
    contacts = Image.new('RGB',(12*200,5*220),(29,35,48))
    draw = ImageDraw.Draw(contacts)
    maximum = 0
    compositions = 0
    for row,name in enumerate(TOWER_NAMES):
        for col,site in enumerate(layout['sites']):
            for aim in (-60,0,60):
                model,radius = tower(name,site,aim)
                maximum = max(maximum,radius)
                compositions += 1
                if aim == 0:
                    comp = background.copy()
                    comp.alpha_composite(model)
                    comp.alpha_composite(fronts)
                    x,y = site['mount']
                    x,y = x+(SIZE-1254)/2,y+(SIZE-1254)/2
                    crop = comp.crop((x-100,y-140,x+100,y+60)).convert('RGB')
                    contacts.paste(crop,(col*200,row*220+20))
                    draw.text((col*200+3,row*220+3),f'{name} / {site["site"]}',fill='white')
    contacts.save(OUT/'socket-review.png')
    comp = background.copy()
    for index,site in enumerate(layout['sites']):
        comp.alpha_composite(tower(TOWER_NAMES[index%5],site,0)[0])
    comp.alpha_composite(fronts)
    comp.resize((800,800),Image.Resampling.NEAREST).convert('RGB').save(OUT/'gameplay-sizing-review.png')
    peak = Image.open(ROOT/'Assets/Art/Effects/DavisNovaSupernova/Supernova-04.png').convert('RGBA')
    alpha = peak.getchannel('A')
    clear_radius = min(math.hypot(x-256,y-256) for y in range(512) for x in range(512) if alpha.getpixel((x,y)) < 128) / 100
    core_radius = clear_radius * (6 / 1.1)
    assert maximum < core_radius, (maximum,core_radius)
    extent = 512 / 100 * (6 / 1.1) * PPU
    peak = peak.resize((round(extent),round(extent)),Image.Resampling.NEAREST)
    overlay = Image.new('RGBA',(SIZE,SIZE))
    overlay.alpha_composite(peak,(round((SIZE-extent)/2),round((SIZE-extent)/2)))
    comp.alpha_composite(overlay)
    comp.resize((800,800),Image.Resampling.NEAREST).convert('RGB').save(OUT/'blast-coverage-review.png')
    report = dict(socket_aim_compositions=compositions,tower_types=5,source_tower_scale=layout['tower_scale'],
        runtime_tower_scale=1.5,ring_scale=ART_SCALE,conservative_tower_outer_radius=maximum,
        opaque_supernova_core_radius=core_radius,all_towers_inside_peak=True,unity_launched=False)
    (OUT/'sizing-and-coverage.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
