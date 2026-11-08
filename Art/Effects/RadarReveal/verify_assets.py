"""Offline Radar effect checks and assembled previews. Does not launch Unity."""
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import re

from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
data = json.loads((ART/'sprites.json').read_text())
source = ART/data['source']
runtime = ROOT/data['runtime']
assert source.read_bytes() == runtime.read_bytes()
assert hashlib.sha256(source.read_bytes()).hexdigest() == data['sha256']
sheet = Image.open(source).convert('RGBA')
assert list(sheet.size) == data['size']
assert sheet.getchannel('A').getextrema() == (0,255)
meta = Path(str(runtime)+'.meta').read_text()
for token in ('spriteMode: 2','filterMode: 0','enableMipMap: 0','nPOTScale: 0',
              'spriteMeshType: 0','wrapU: 1','wrapV: 1','textureCompression: 0',
              'alphaIsTransparency: 1',f'spritePixelsToUnits: {data["pixels_per_unit"]}'):
    assert token in meta, token
cells=[]
measurements=[]
for f in data['frames']:
    x,y,w,h=f['rect']
    assert 0 <= x < x+w <= sheet.width and 0 <= y < y+h <= sheet.height
    cell=sheet.crop((x,sheet.height-y-h,x+w,sheet.height-y))
    alpha=cell.getchannel('A')
    bounds=alpha.point(lambda v:255 if v>=16 else 0).getbbox()
    assert list(bounds)==f['visible_bounds_in_slice']
    assert min(bounds[0],bounds[1],w-bounds[2],h-bounds[3])>=8
    ax,ay=f['anchor_top_left_pixels']
    assert abs(f['pivot'][0]*w-ax)<1e-7
    assert abs((1-f['pivot'][1])*h-ay)<1e-7
    assert f'internalID: {f["file_id"]}' in meta
    assert f'{f["name"]}: {f["file_id"]}' in meta
    if f['kind']=='scan':
        angles=[math.degrees(math.atan2(px+.5-ax,ay-py-.5))
                for py in range(bounds[1],bounds[3]) for px in range(bounds[0],bounds[2])
                if alpha.getpixel((px,py))>=16]
        assert min(angles)>=-45 and max(angles)<=45
        measurements.append(dict(frame=f['name'],minimum_degrees=min(angles),maximum_degrees=max(angles),crest_radius_source_pixels=ay-bounds[1]))
        assert alpha.crop((0,418,w,h)).getextrema()[1]<16
    else:
        assert alpha.crop((round(ax-45),round(ay-35),round(ax+45),round(ay+35))).getextrema()[1]<16
    cells.append(cell)
assert all(measurements[i]['crest_radius_source_pixels']<measurements[i+1]['crest_radius_source_pixels'] for i in range(2))
assert sheet.getchannel('A').crop((0,836,1254,1254)).getextrema()[1]<16

for name,effect in data['effects'].items():
    clip=(ROOT/effect['clip']['path']).read_text()
    controller=(ROOT/effect['controller']['path']).read_text()
    prefab=(ROOT/effect['prefab']['path']).read_text()
    for token in ('m_LoopTime: 0','m_RotationCurves: []','m_PositionCurves: []',
                  'm_ScaleCurves: []','m_FloatCurves: []','m_Events: []'):
        assert token in clip,(name,token)
    curves=clip.split('  m_PPtrCurves:')[1].split('    attribute:')[0]
    times=[float(t) for t in re.findall(r'- time: ([\d.]+)',curves)]
    assert all(abs(a-b)<1e-8 for a,b in zip(times,effect['key_times_seconds'])) and len(times)==4
    ids=[int(fid) for fid in re.findall(r'value: \{fileID: (\d+)',curves)]
    assert ids==[data['frames'][i]['file_id'] for i in effect['frame_indices']]+[0]
    assert float(re.search(r'm_StopTime: ([\d.]+)',clip)[1])==effect['duration_seconds']
    assert curves.count(data['guid'])==3
    assert effect['clip']['guid'] in controller
    assert effect['controller']['guid'] in prefab and data['guid'] in prefab
    assert prefab.count('--- !u!95 ')==1 and prefab.count('--- !u!212 ')==1
    assert 'm_ApplyRootMotion: 0' in prefab and 'MonoBehaviour:' not in prefab and 'Collider' not in prefab
    scale=effect['prefab_scale']
    assert f'm_LocalScale: {{x: {scale}, y: {scale}, z: 1}}' in prefab
    declared=set(re.findall(r'^--- !u!\d+ &(\d+)',prefab,re.M))
    assert all(fid=='0' or fid in declared for fid in re.findall(r'\{fileID: (\d+)\}',prefab))

guid_counts=Counter()
for path in (ROOT/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',path.read_text(encoding='utf-8-sig'),re.M)
    if match:guid_counts[match[1]]+=1
for gid in [data['guid']]+[info['guid'] for e in data['effects'].values() for info in (e['prefab'],e['clip'],e['controller'])]:
    assert guid_counts[gid]==1,gid
for parent in ('Assets/Art/Effects','Assets/Animations/Effects','Assets/Prefabs/Effects'):
    for path in (ROOT/parent).rglob('*'):
        if path.suffix!='.meta':assert Path(str(path)+'.meta').exists(),path
    assert Path(str(ROOT/parent)+'.meta').exists()

# Review only: source crops, nearest-neighbor scaling and rigid compositing.
tower=Image.open(ROOT/'Art/Towers/Radar/tower-source-v1.png').convert('RGBA')
enemy=Image.open(ROOT/'Art/Enemies/BasicGoose1/basic-goose-1-source-v1.png').convert('RGBA')
def place(canvas,image,x,y,ratio):
    scaled=image.resize((round(image.width*ratio),round(image.height*ratio)),Image.Resampling.NEAREST)
    canvas.alpha_composite(scaled,(round(x),round(y)))

review=Image.new('RGBA',(1254,900),'#182034')
draw=ImageDraw.Draw(review)
for i,cell in enumerate(cells):
    col,row=i%3,i//3
    ax,ay=data['frames'][i]['anchor_top_left_pixels']
    target_y=410 if row==0 else 660
    place(review,cell,col*418+209-ax,target_y-ay,1)
    draw.text((col*418+16,25+row*435),data['frames'][i]['name'],fill='#a7ead5')
review.convert('RGB').save(ART/'effect-review.png')

demo=[]
for tick in range(15):
    canvas=Image.new('RGBA',(960,680),'#182034')
    ratio=360/1400
    place(canvas,tower,500-878*ratio,360-154*ratio,ratio)
    revealed=tick>=1
    ship=enemy.copy()
    if not revealed:ship.putalpha(ship.getchannel('A').point(lambda a:round(a*.12)))
    er=360/2500
    place(canvas,ship,500-626*er,115-652.5*er,er)
    age=(tick-1)*.08
    for kind,origin,multiplier,duration,offset,step in [('scan',(500,360),1,.36,0,.12),('marker',(500,115),1.75,.24,3,.08)]:
        if 0<=age<duration:
            index=min(2,math.floor((age+1e-8)/step))+offset
            ax,ay=data['frames'][index]['anchor_top_left_pixels']
            r=360/data['pixels_per_unit']*multiplier
            place(canvas,cells[index],origin[0]-ax*r,origin[1]-ay*r,r)
    ImageDraw.Draw(canvas).text((45,645),'Demo reset / hidden guide' if not revealed else 'Enemy stays revealed after the marker fades',fill='#a7ead5')
    demo.append(canvas.convert('RGB').resize((720,510),Image.Resampling.NEAREST))
demo[0].save(ART/'reveal-preview.gif',save_all=True,append_images=demo[1:],duration=[320]+[80]*13+[480],loop=0)
report=dict(angles=measurements,source_runtime_equal=True,one_shot_clips=True,blank_final_keys=True,gameplay_interval=None,unity_run=False)
report_dir=ROOT/'.git/menu-work/radar-reveal'
report_dir.mkdir(parents=True,exist_ok=True)
(report_dir/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('PASS: alpha/hashes, six slices/pivots/gutters, scan pixels inside +/-45 degrees,')
print('increasing crest distances, empty marker centers, one-shot clips/final blank keys,')
print('prefab/import/GUID references and metadata pairing. Rebuilt review PNG and demo GIF.')
print('No Unity execution. Scan interval remains undecided.')
