"""Offline image, native-reference and launch-corridor checks; never launches Unity."""
import collections
import hashlib
import json
import math
import re
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
SCRATCH = ROOT / '.git/menu-work/rabbit-tesla'


def main():
    SCRATCH.mkdir(parents=True, exist_ok=True)
    m = json.loads((ART / 'sprites.json').read_text())
    source, runtime = ART / m['source'], ROOT / m['sheet']
    assert hashlib.sha256(source.read_bytes()).hexdigest() == m['source_sha256']
    assert source.read_bytes() == runtime.read_bytes()
    sheet = Image.open(source)
    assert sheet.mode == 'RGBA' and sheet.getchannel('A').getextrema() == (0, 255)
    texture = Path(str(runtime) + '.meta').read_text()
    assert f"guid: {m['sheet_guid']}\n" in texture
    frames = {frame['file_id']: frame for frame in m['frames']}
    assert len(frames) == 8
    assert re.findall(r'^      internalID: (\d+)$', texture, re.M) == [str(value) for value in frames]
    cells, min_padding, radius = [], 418, 0
    for frame in m['frames']:
        x, y, w, h = frame['rect']
        assert x >= 0 and y >= 0 and x+w <= sheet.width and y+h <= sheet.height
        cell = sheet.crop((x, sheet.height-y-h, x+w, sheet.height-y))
        cells.append(cell)
        for threshold, field in [(16, 'visible_bounds_alpha16'), (128, 'strong_bounds_alpha128')]:
            bounds = cell.getchannel('A').point(lambda v: 255 if v >= threshold else 0).getbbox()
            assert list(bounds) == frame[field]
        b = frame['visible_bounds_alpha16']
        min_padding = min(min_padding, b[0], b[1], w-b[2], h-b[3])
        assert min(b[0], b[1], w-b[2], h-b[3]) >= 16
        assert all(0 <= value <= 1 for value in frame['pivot'])
        anchor = frame['anchor_top_left_pixels']
        assert abs(anchor[0]/w-frame['pivot'][0]) < 1e-12
        assert abs(1-anchor[1]/h-frame['pivot'][1]) < 1e-12
        block = texture.split('      name: ' + frame['name'] + '\n')[1].split('    - serializedVersion: 2')[0]
        pivot = re.search(r'pivot: \{x: ([^,]+), y: ([^}]+)\}', block)
        assert all(abs(float(v)-want) < 1e-10 for v, want in zip(pivot.groups(), frame['pivot']))
        assert f"{frame['name']}: {frame['file_id']}" in texture
        for field, value in zip(['x', 'y', 'width', 'height'], frame['rect']):
            assert re.search(rf'^        {field}: {value}$', block, re.M)
        if 'Burst' in frame['name']:
            alpha = cell.getchannel('A')
            current = max(math.hypot(px+0.5-anchor[0], py+0.5-anchor[1]) for py in range(h) for px in range(w) if alpha.getpixel((px, py)) >= 16)
            assert abs(current-frame['max_radius_pixels_alpha16']) < 1e-10
            radius = max(radius, current)
            if not frame['name'].endswith('01'):
                assert all(alpha.getpixel((px, py)) < 16 for py in range(h) for px in range(w)
                           if math.hypot(px+0.5-anchor[0], py+0.5-anchor[1]) < 45)
    assert abs(radius-m['burst_radius_pixels_alpha16']) < 1e-10
    for row, col in m['empty_cells_top_left_row_col']:
        assert sheet.crop((col*418, row*418, (col+1)*418, (row+1)*418)).getchannel('A').getextrema()[1] <= 1
    for setting in ('spriteMode: 2', 'filterMode: 0', 'wrapU: 1', 'wrapV: 1', 'nPOTScale: 0', 'enableMipMap: 0',
                    'spriteMeshType: 0', 'alphaIsTransparency: 1', 'spritePixelsToUnits: 1024', 'textureCompression: 0'):
        assert setting in texture, setting

    new_files = [runtime]
    for animation in m['animations']:
        clip = ROOT / 'Assets/Animations/Projectiles' / (animation['name'] + '.anim')
        controller = clip.with_suffix('.controller')
        text = clip.read_text()
        curve = text.split('  m_PPtrCurves:')[1].split('  m_SampleRate:')[0]
        keys = re.findall(r'- time: ([\d.]+)\n      value: \{fileID: (\d+)', curve)
        assert len(keys) == len(animation['keys'])
        for (time, file_id), key in zip(keys, animation['keys']):
            assert abs(float(time)-key['time']) < 1e-8 and int(file_id) == key['file_id']
        assert f"m_SampleRate: {animation['fps']}\n" in text
        assert abs(float(re.search(r'm_StopTime: ([\d.]+)', text)[1])-animation['duration']) < 1e-8
        assert f"m_LoopTime: {int(animation['loop'])}\n" in text
        assert keys[-1][1] == (keys[0][1] if animation['loop'] else '0')
        assert curve.count('attribute: m_Sprite') == 1 and 'classID: 212' in curve
        for field in ('m_RotationCurves', 'm_CompressedRotationCurves', 'm_EulerCurves', 'm_PositionCurves', 'm_ScaleCurves', 'm_FloatCurves', 'm_Events'):
            assert field + ': []' in text, field
        mapping = text.split('    pptrCurveMapping:')[1].split('  m_AnimationClipSettings:')[0]
        assert [int(v) for v in re.findall(r'fileID: (\d+)', mapping)] == [key['file_id'] for key in animation['keys']]
        assert set(re.findall(r'guid: (\w+)', text)) == {m['sheet_guid']}
        controller_text = controller.read_text()
        assert f"m_Motion: {{fileID: 7400000, guid: {animation['clip_guid']}, type: 2}}" in controller_text
        assert f"m_Name: {animation['state']}\n" in controller_text
        assert 'm_Transitions: []' in controller_text
        new_files.extend([clip, controller])

    for item in m['prefabs']:
        prefab = ROOT / item['path']
        text = prefab.read_text()
        assert collections.Counter(re.findall(r'^--- !u!(\d+)', text, re.M)) == {'1': 1, '4': 1, '212': 1, '95': 1}
        assert f"m_Sprite: {{fileID: {item['initial_sprite_file_id']}, guid: {m['sheet_guid']}, type: 3}}" in text
        assert f"m_LocalScale: {{x: {item['uniform_xy_scale']:.12f}, y: {item['uniform_xy_scale']:.12f}, z: 1}}" in text
        animation = next(a for a in m['animations'] if a['state'] == item['kind'])
        assert f"m_Controller: {{fileID: 9100000, guid: {animation['controller_guid']}, type: 2}}" in text
        assert 'm_ApplyRootMotion: 0' in text
        assert 'm_SortingOrder: 10' in text
        declared = set(re.findall(r'^--- !u!\d+ &(\d+)', text, re.M))
        assert all(v == '0' or v in declared for v in re.findall(r'\{fileID: (\d+)\}', text))
        if item['kind'] == 'Burst':
            assert abs(item['uniform_xy_scale'] * radius/m['pixels_per_unit'] - 1) < 1e-10
        new_files.append(prefab)
    metas = [Path(str(p) + '.meta') for p in new_files] + [ROOT / 'Assets/Art/Projectiles/RabbitTesla.meta']
    own = [re.search(r'^guid: (\w+)', p.read_text(), re.M)[1] for p in metas]
    assert len(set(own)) == len(metas) == 11
    counts = collections.Counter()
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)
        if match and match[1] in own:
            counts[match[1]] += 1
    assert all(counts[value] == 1 for value in own)

    html = (ART / 'preview.html').read_text(encoding='utf-8')
    for target in re.findall(r"loadImage\('([^']+)'\)", html):
        assert (ART / target).is_file(), target
    data = json.loads(re.search(r'<script id="asset-data" type="application/json">(.*?)</script>', html, re.S)[1])
    assert data['frames'] == m['frames']
    assert data['scales'] == {item['kind']: item['uniform_xy_scale'] for item in m['prefabs']}
    tower = json.loads((ROOT / 'Art/Towers/TeslaCoil/assembly.json').read_text())
    assert data['tower'] == tower
    script = SCRATCH / 'preview-script.js'
    script.write_text(re.search(r'<script>\n(.*?)</script>', html, re.S)[1], encoding='utf-8')
    subprocess.run(['node', '--check', str(script)], check=True, capture_output=True, text=True)

    # Conservative full-width launch corridor beyond the electrode, sampled against base alpha.
    base = Image.open(ROOT / 'Art/Towers/TeslaCoil' / tower['base']['source']).getchannel('A')
    socket, bp = tower['base']['socket_top_left_pixels'], tower['base']['pixels_per_unit']
    emission_distance = tower['muzzle_local_position'][1]
    half_width = 0
    for index, frame in enumerate(m['frames'][:4]):
        b, anchor = frame['visible_bounds_alpha16'], frame['anchor_top_left_pixels']
        scale = data['scales']['Flight' if index < 2 else 'Muzzle']
        half_width = max(half_width, max(anchor[1]-b[1], b[3]-anchor[1])/m['pixels_per_unit']*scale)
    hits = 0
    for step in range(241):
        angle = math.radians(-60+step*0.5)
        s, c = math.sin(angle), math.cos(angle)
        for distance in range(math.ceil(2*bp)+1):
            travel = emission_distance+distance/bp
            for lateral in (-half_width, -half_width/2, 0, half_width/2, half_width):
                x = socket[0]+(-s*travel+c*lateral)*bp
                y = socket[1]+(-c*travel-s*lateral)*bp
                if 0 <= x < base.width and 0 <= y < base.height and base.getpixel((int(x), int(y))) >= 128:
                    hits += 1
    assert hits == 0, f'Launch corridor intersects base at {hits} samples'
    review = Image.new('RGBA', (4*350, 2*380), '#263142')
    draw = ImageDraw.Draw(review)
    for index, (frame, cell) in enumerate(zip(m['frames'], cells)):
        b = frame['visible_bounds_alpha16']
        crop = cell.crop(b)
        scale = min(1, 310/crop.width, 310/crop.height)
        crop = crop.resize((round(crop.width*scale), round(crop.height*scale)), Image.Resampling.NEAREST)
        col, row = index%4, index//4
        review.alpha_composite(crop, (round(col*350+(350-crop.width)/2), round(row*380+35+(320-crop.height)/2)))
        draw.text((col*350+12, row*380+12), frame['name'], fill='white')
    review.convert('RGB').save(ART / 'frame-review.png')
    report = {'asset_checks': 'PASS', 'sprite_slices': 8, 'unique_new_guids': 11,
              'source_runtime_bytes': 'identical', 'minimum_slice_padding_alpha16_pixels': min_padding,
              'looping_flight_clips': 1, 'one_shot_clips_with_blank_ending': 2,
              'no_transform_animation': True, 'open_ring_center_radius_source_pixels': 45,
              'burst_radius_source_pixels_alpha16': radius,
              'launch_corridor_angles': 241, 'launch_corridor_length_world_units': 2,
              'launch_corridor_lateral_samples': 5, 'launch_corridor_base_hits': hits,
              'conservative_launch_width_world_units': 2*half_width,
              'javascript_syntax': 'PASS', 'unity_run': False,
              'limitations': ['Sampled outgoing corridor, not a continuous proof or later homing behavior.',
                              'Source alpha preserved; geometric checks use documented alpha thresholds.',
                              'Final gameplay radius, camera readability, imports/materials and pooled replay need manual review.']}
    (SCRATCH / 'offline-validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
