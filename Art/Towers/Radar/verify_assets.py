"""Offline asset/reference checks and assembled previews; never starts Unity."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
ART = Path(__file__).resolve().parent
manifest = json.loads((ART / 'assembly.json').read_text())
images = {}
for part in ('tower', 'status'):
    info = manifest[part]
    source, runtime = ART / info['source'], ROOT / info['runtime']
    assert source.read_bytes() == runtime.read_bytes(), part
    assert hashlib.sha256(source.read_bytes()).hexdigest() == info['sha256'], part
    im = Image.open(source).convert('RGBA')
    assert im.getchannel('A').getextrema() == (0, 255), part
    images[part] = im
    meta = Path(str(runtime) + '.meta').read_text()
    for token in ('filterMode: 0', 'enableMipMap: 0', 'nPOTScale: 0',
                  'spriteMeshType: 0', 'wrapU: 1', 'wrapV: 1',
                  'alphaIsTransparency: 1', 'textureCompression: 0',
                  f'spritePixelsToUnits: {info["pixels_per_unit"]}'):
        assert token in meta, (part, token)
    assert re.search(r'^guid: (\w+)', meta, re.M)[1] == info['guid']

tower, sheet = images['tower'], images['status']
tower_info, status = manifest['tower'], manifest['status']
ratio = tower_info['pixels_per_unit'] / status['pixels_per_unit']
anchor_x, anchor_y = status['anchor_on_tower_top_left_pixels']
previews = []
for frame in status['frames']:
    x, y, width, height = frame['rect']
    assert 0 <= x < x+width <= sheet.width
    assert 0 <= y < y+height <= sheet.height
    cell = sheet.crop((x, sheet.height-y-height, x+width, sheet.height-y))
    bounds = cell.getchannel('A').point(lambda a: 255 if a >= 16 else 0).getbbox()
    assert list(bounds) == frame['visible_bounds_in_cell']
    assert bounds[0] > 8 and bounds[1] > 8 and width-bounds[2] > 8 and height-bounds[3] > 8
    pivot_x, pivot_y = frame['pivot']
    left = anchor_x - pivot_x * width * ratio
    top = anchor_y - (1-pivot_y) * height * ratio
    # All visible status pixels must remain inside the empty monitor interior.
    placed = [left+bounds[0]*ratio, top+bounds[1]*ratio,
              left+bounds[2]*ratio, top+bounds[3]*ratio]
    assert 550 <= placed[0] < placed[2] <= 725, placed
    assert 670 <= placed[1] < placed[3] <= 750, placed
    composite = Image.new('RGBA', tower.size, '#182034')
    composite.alpha_composite(tower)
    scaled = cell.resize((round(width*ratio), round(height*ratio)), Image.Resampling.NEAREST)
    composite.alpha_composite(scaled, (round(left), round(top)))
    previews.append(composite)

clip = (ROOT / manifest['clip']['path']).read_text()
prefab = (ROOT / manifest['prefab']['path']).read_text()
controller = (ROOT / manifest['controller']['path']).read_text()
for token in ('m_RotationCurves: []', 'm_EulerCurves: []', 'm_PositionCurves: []',
              'm_ScaleCurves: []', 'm_FloatCurves: []', 'm_Events: []',
              'm_LoopTime: 1', 'attribute: m_Sprite', 'm_StopTime: 1.2'):
    assert token in clip, token
curves = clip.split('  m_PPtrCurves:')[1].split('    attribute:')[0]
keys = re.findall(r'- time: ([\d.]+)\s+value: \{fileID: (\d+), guid: (\w+), type: 3\}', curves)
expected = [(float(time), status['frames'][index]['file_id'], status['guid'])
            for time, index in status['animation_sequence']]
assert [(float(time), int(fid), gid) for time, fid, gid in keys] == expected
assert 'm_ApplyRootMotion: 0' in prefab
assert prefab.count('--- !u!95 ') == 1
assert prefab.count('--- !u!212 ') == 2
assert 'MonoBehaviour:' not in prefab and 'Collider' not in prefab
assert 'm_Name: ConsoleStatus' in prefab and 'm_Name: Tower' in prefab
assert manifest['clip']['guid'] in controller
assert manifest['controller']['guid'] in prefab
assert manifest['tower']['guid'] in prefab and manifest['status']['guid'] in prefab
assert 'm_StateMachineBehaviours: []' in controller
assert 'm_AnimatorParameters: []' in controller

metadata = list((ROOT / 'Assets').rglob('*.meta'))
guid_counts = Counter()
for path in metadata:
    match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)
    if match:
        guid_counts[match[1]] += 1
new_files = list((ROOT / 'Assets/Art/Towers/Radar').glob('*.meta'))
new_files += list((ROOT / 'Assets/Animations/Towers/Radar').glob('*.meta'))
new_files += [ROOT / 'Assets/Art/Towers/Radar.meta', ROOT / 'Assets/Animations/Towers.meta',
              ROOT / 'Assets/Animations/Towers/Radar.meta',
              ROOT / 'Assets/Prefabs/Towers/RadarTowerVisual.prefab.meta']
for path in new_files:
    gid = re.search(r'^guid: (\w+)', path.read_text(), re.M)[1]
    assert guid_counts[gid] == 1, (path, gid)
    assert path.with_suffix('').exists(), path

internal_ids = set(re.findall(r'^--- !u!\d+ &(\d+)', prefab, re.M))
for file_id in re.findall(r'\{fileID: (\d+)\}', prefab):
    assert file_id == '0' or file_id in internal_ids, file_id
sheet_meta = Path(str(ROOT / status['runtime']) + '.meta').read_text()
for frame in status['frames']:
    assert f'internalID: {frame["file_id"]}' in sheet_meta
    assert f'{frame["name"]}: {frame["file_id"]}' in sheet_meta

review = Image.new('RGB', (1200, 430), '#182034')
draw = ImageDraw.Draw(review)
for i, preview in enumerate(previews):
    review.paste(preview.convert('RGB').resize((400, 400), Image.Resampling.NEAREST), (400*i, 25))
    draw.text((400*i+14, 8), f'Frame {i+1} - fixed dish / console activity', fill='#a4f0d7')
review.save(ART / 'console-review.png')
gif_frames = [previews[index].convert('RGB').resize((400,400), Image.Resampling.NEAREST)
              for index in (0,1,2,1,0)]
gif_frames[0].save(ART / 'idle-preview.gif', save_all=True, append_images=gif_frames[1:],
                   duration=[300,200,200,200,300], loop=0)
print('PASS: source/runtime hashes, alpha, 3 aligned screen slices, screen containment,')
print('sprite-only 1.2-second loop, prefab hierarchy, imports, file IDs and unique GUIDs.')
print(f'Tower visible width: {(tower_info["bounds_alpha16"][2]-tower_info["bounds_alpha16"][0])/tower_info["pixels_per_unit"]:.4f} units.')
print('Rebuilt console-review.png and idle-preview.gif. No Unity execution.')
