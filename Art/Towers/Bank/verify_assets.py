"""Check Bank artwork and native asset references offline; never launches Unity."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
m = json.loads((ART / 'assembly.json').read_text())
images = {}
for part in ('tower', 'status'):
    info = m[part]
    source, runtime = ART / info['source'], ROOT / info['runtime']
    assert source.read_bytes() == runtime.read_bytes(), part
    assert hashlib.sha256(source.read_bytes()).hexdigest() == info['sha256'], part
    image = Image.open(source).convert('RGBA')
    assert image.getchannel('A').getextrema() == (0, 255), part
    images[part] = image
    assert list(image.size) == info['size'], part
    meta = Path(str(runtime) + '.meta').read_text()
    for token in ('filterMode: 0', 'enableMipMap: 0', 'nPOTScale: 0',
                  'spriteMeshType: 0', 'wrapU: 1', 'wrapV: 1',
                  'alphaIsTransparency: 1', 'textureCompression: 0',
                  f'spritePixelsToUnits: {info["pixels_per_unit"]}'):
        assert token in meta, (part, token)
    assert re.search(r'^guid: (\w+)', meta, re.M)[1] == info['guid']

tower, sheet = images['tower'], images['status']
tower_info, status = m['tower'], m['status']
tower_meta = Path(str(ROOT / tower_info['runtime']) + '.meta').read_text()
match = re.search(r'spritePivot: \{x: ([\d.]+), y: ([\d.]+)\}', tower_meta)
gx, gy = tower_info['ground_mount_top_left_pixels']
assert abs(float(match[1])-gx/tower.width) < 1e-10
assert abs(float(match[2])-(1-gy/tower.height)) < 1e-10
bounds = tower.getchannel('A').point(lambda a: 255 if a >= 16 else 0).getbbox()
assert list(bounds) == tower_info['bounds_alpha16']
assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < tower.width and bounds[3] < tower.height
ratio = tower_info['pixels_per_unit'] / status['pixels_per_unit']
anchor_x, anchor_y = status['anchor_on_tower_top_left_pixels']
interior = status['screen_interior_top_left_pixels']
previews = []
for frame in status['frames']:
    x, y, width, height = frame['rect']
    assert 0 <= x < x+width <= sheet.width and 0 <= y < y+height <= sheet.height
    cell = sheet.crop((x, sheet.height-y-height, x+width, sheet.height-y))
    bounds = cell.getchannel('A').point(lambda a: 255 if a >= 16 else 0).getbbox()
    assert list(bounds) == frame['visible_bounds_in_cell']
    assert min(bounds[0], bounds[1], width-bounds[2], height-bounds[3]) > 8
    px, py = frame['pivot']
    left, top = anchor_x-px*width*ratio, anchor_y-(1-py)*height*ratio
    placed = [left+bounds[0]*ratio, top+bounds[1]*ratio,
              left+bounds[2]*ratio, top+bounds[3]*ratio]
    assert interior[0] <= placed[0] < placed[2] <= interior[2], placed
    assert interior[1] <= placed[1] < placed[3] <= interior[3], placed
    composite = Image.new('RGBA', tower.size, '#182034')
    composite.alpha_composite(tower)
    composite.alpha_composite(cell.resize((round(width*ratio), round(height*ratio)),
                                        Image.Resampling.NEAREST), (round(left), round(top)))
    previews.append(composite)

clip = (ROOT / m['clip']['path']).read_text()
prefab = (ROOT / m['prefab']['path']).read_text()
controller = (ROOT / m['controller']['path']).read_text()
for token in ('m_RotationCurves: []', 'm_EulerCurves: []', 'm_PositionCurves: []',
              'm_ScaleCurves: []', 'm_FloatCurves: []', 'm_Events: []',
              'm_LoopTime: 1', 'attribute: m_Sprite', 'm_StopTime: 1'):
    assert token in clip, token
curves = clip.split('  m_PPtrCurves:')[1].split('    attribute:')[0]
keys = re.findall(r'- time: ([\d.]+)\s+value: \{fileID: (\d+), guid: (\w+), type: 3\}', curves)
expected = [(float(t), status['frames'][i]['file_id'], status['guid'])
            for t, i in status['animation_sequence']]
assert [(float(t), int(fid), gid) for t, fid, gid in keys] == expected
assert expected[-1][0] == 1 and expected[-1][1] == expected[0][1]
mapping = clip.split('    pptrCurveMapping:')[1].split('  m_AnimationClipSettings:')[0]
assert [(int(fid), gid) for fid, gid in re.findall(r'fileID: (\d+), guid: (\w+)', mapping)] == [(fid, gid) for _, fid, gid in expected]
assert 'm_ApplyRootMotion: 0' in prefab
assert prefab.count('--- !u!95 ') == 1 and prefab.count('--- !u!212 ') == 2
assert set(re.findall(r'^--- !u!(\d+)', prefab, re.M)) == {'1', '4', '212', '95'}
assert 'm_Name: CoinStatus' in prefab and 'm_Name: Tower' in prefab
positions = re.findall(r'm_LocalPosition: \{x: ([\d.e-]+), y: ([\d.e-]+), z: ([\d.e-]+)\}', prefab)
assert len(positions) == 3 and positions[:2] == [('0', '0', '0')]*2
sx, sy = status['local_position']
assert abs(float(positions[2][0])-sx) < 1e-10 and abs(float(positions[2][1])-sy) < 1e-10
assert float(positions[2][2]) == 0
assert abs(sx-(anchor_x-gx)/tower_info['pixels_per_unit']) < 1e-10
assert abs(sy-(gy-anchor_y)/tower_info['pixels_per_unit']) < 1e-10
assert m['clip']['guid'] in controller and m['controller']['guid'] in prefab
assert m['tower']['guid'] in prefab and m['status']['guid'] in prefab
assert 'm_StateMachineBehaviours: []' in controller and 'm_AnimatorParameters: []' in controller

guid_counts = Counter()
for path in (ROOT / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)
    if match:
        guid_counts[match[1]] += 1
for relative in m['new_metadata']:
    path = ROOT / relative
    gid = re.search(r'^guid: (\w+)', path.read_text(), re.M)[1]
    assert guid_counts[gid] == 1 and path.with_suffix('').exists(), path
internal_ids = set(re.findall(r'^--- !u!\d+ &(\d+)', prefab, re.M))
for fid in re.findall(r'\{fileID: (\d+)\}', prefab):
    assert fid == '0' or fid in internal_ids, fid
sheet_meta = Path(str(ROOT / status['runtime']) + '.meta').read_text()
chunks = sheet_meta.split('    - serializedVersion: 2\n')
assert len(chunks) == 4
for i, frame in enumerate(status['frames']):
    assert f'internalID: {frame["file_id"]}' in sheet_meta
    assert f'{frame["name"]}: {frame["file_id"]}' in sheet_meta
    match = re.search(r'      pivot: \{x: ([\d.]+), y: ([\d.]+)\}', chunks[i+1])
    assert all(abs(float(match[j+1])-frame['pivot'][j]) < 1e-10 for j in (0, 1))
    rect = re.search(r'        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)', chunks[i+1])
    assert list(map(int, rect.groups())) == frame['rect']
for part in ('prefab', 'clip', 'controller'):
    assert m[part]['guid'] in Path(str(ROOT / m[part]['path']) + '.meta').read_text()

review = Image.new('RGB', (1200, 430), '#182034')
draw = ImageDraw.Draw(review)
for i, preview in enumerate(previews):
    review.paste(preview.convert('RGB').resize((400, 400), Image.Resampling.NEAREST), (400*i, 25))
    draw.text((400*i+14, 8), f'Coin display: {status["frames"][i]["label"]}', fill='#f2d28a')
review.save(ART / 'coin-review.png')
sequence = status['animation_sequence']
gif = [previews[i].convert('RGB').resize((400,400), Image.Resampling.NEAREST) for _, i in sequence[:-1]]
durations = [round((sequence[i+1][0]-sequence[i][0])*1000) for i in range(len(sequence)-1)]
gif[0].save(ART / 'idle-preview.gif', save_all=True, append_images=gif[1:], duration=durations, loop=0)
print('PASS: alpha, unchanged runtime copies/hashes, aligned slices and screen containment,')
print('one-second sprite-only loop, prefab hierarchy, imports, IDs and unique GUIDs.')
print('Rebuilt coin-review.png and idle-preview.gif. No Unity execution.')
