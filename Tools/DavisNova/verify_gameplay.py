"""Read-only serialized data, socket and artwork checks. Does not run Unity."""
import sys
sys.dont_write_bytecode = True
import hashlib
import json
import math
import re
import subprocess
from PIL import Image
from pathlib import Path
from prepare_gameplay import ART_SCALE, BOSS, CONFIG, ENEMIES, LEVEL, RADIUS, ROOT, SCENE, TOWER_NAMES, TOWERS, WAVES, WORLD_MAGNIFICATION, docs, read


def guid(path):
    return re.search(r'^guid: (\w+)', read(str(path) + '.meta'), re.M)[1]


def field(source, key):
    return re.search(r'^  ' + re.escape(key) + r':[ \t]*(.*)$', source, re.M)[1]


def check():
    metas = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            assert match[1] not in metas, f'Duplicate GUID: {meta}'
            metas[match[1]] = meta.with_suffix('')
    package_root = ROOT / 'Library/PackageCache'
    # A new worktree has no Library: the camera's package script can be resolved
    # from the editor-generated camera metadata in the original shared checkout.
    if not package_root.exists():
        main_root = subprocess.check_output(['git', 'worktree', 'list', '--porcelain'], cwd=ROOT, text=True).splitlines()[0].split(' ', 1)[1]
        package_root = Path(main_root) / 'Library/PackageCache'
    for meta in package_root.glob('com.unity.render-pipelines.universal@*/Runtime/UniversalAdditionalCameraData.cs.meta'):
        value = re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]
        metas[value] = meta.with_suffix('')
    references = 0
    for path in [LEVEL, CONFIG, SCENE]:
        source = read(path)
        objects = docs(source)
        ids = {i for _, i, _ in objects}
        assert len(ids) == len(objects), f'Duplicate IDs: {path}'
        guid(path)
        for match in re.finditer(r'\{fileID: (-?\d+)(?:, guid: (\w+), type: \d+)?\}', source):
            ident, value = int(match[1]), match[2]
            if ident == 0:
                continue
            if value is None:
                assert ident in ids, f'Missing local ID: {path}/{ident}'
            elif not value.startswith('0000000000000000'):
                assert value in metas, f'Missing GUID: {path}/{value}'
                target = metas[value]
                assert target.exists(), f'Orphan metadata: {target}'
                if target.suffix in ['.asset', '.prefab'] and ident != 100100000:
                    assert ident in {i for _, i, _ in docs(target.read_text(encoding='utf-8-sig'))}, f'Missing external ID: {target}/{ident}'
            references += 1
    level, config, scene = read(LEVEL), read(CONFIG), read(SCENE)
    assert 'TowerLifespan' not in scene and 'PlanetPageController' not in scene
    for key, value in [('money', 10), ('fuel', 50), ('planetMaxHealth', 75), ('planetRadius', RADIUS), ('isClockwise', 1)]:
        assert float(field(level, key)) == value
    assert field(config, 'displayName') == 'Davis Nova' and field(config, 'nextScene') == ''
    assert field(config, 'stageLabel') == 'CAMPUS DEFENSE / LEVEL 2'
    assert field(config, 'starterSlots') == '0000000006000000'
    towers = config.split('  towers:\n')[1].split('  waves:\n')[0]
    assert re.findall(r'guid: (\w+)', towers) == [guid(TOWERS + name + '.asset') for name in TOWER_NAMES]
    assert re.findall(r'guid: (\w+)', config.split('  finalBosses:\n')[1].split('  starterSlots:')[0]) == [guid(ENEMIES + BOSS + '.asset')]
    prewarmed = set(re.findall(r'  - enemy: \{fileID: \d+, guid: (\w+)', level))
    expected_roster = {guid(ENEMIES + name + '.asset') for _, roster, _ in WAVES for name, _ in roster}
    assert prewarmed == expected_roster | {guid(ENEMIES + BOSS + '.asset')}
    waves = re.split(r'^  - duration:', config.split('  waves:\n')[1].split('  finalBosses:')[0], flags=re.M)[1:]
    assert len(waves) == 3
    for wave, (budget, roster, miniboss) in zip(waves, WAVES):
        assert float(wave.splitlines()[0]) == 20 and '    restDuration: 10\n' in wave
        assert f'    costBudget: {budget}\n' in wave
        actual = re.findall(r'    - enemy: \{fileID: \d+, guid: (\w+), type: 2\}\n      enemyInstances: \[\]\n      startingBudget: ([\d.]+)\n      endingBudget: ([\d.]+)', wave)
        assert [(g, float(a), float(b)) for g, a, b in actual] == [(guid(ENEMIES + name + '.asset'), share, share) for name, share in roster]
        assert math.isclose(sum(share for _, share in roster), 1)
        assert (guid(ENEMIES + miniboss + '.asset') if miniboss else 'miniboss: {fileID: 0}') in wave
    objects = {i: b for _, i, b in docs(scene)}
    assert float(field(objects[1009], 'worldMagnification')) == WORLD_MAGNIFICATION == 1.5
    camera = next(b for c, _, b in docs(scene) if c == 20)
    assert math.isclose(float(field(camera, 'orthographic size')), 8.7 / WORLD_MAGNIFICATION)
    for i in [1002, 1005, 1006]:
        assert guid(LEVEL) in objects[i]
    assert guid(CONFIG) in objects[1008]
    assert field(objects[1006], 'externallyControlled') == '1' and field(objects[1006], 'startOnAwake') == '0'
    assert field(objects[1010], 'intervalSeconds') == '40' and field(objects[1010], 'warningSeconds') == '5'
    assert field(objects[1010], 'sun') == '{fileID: 3002}'
    frames = [f'Assets/Art/Effects/DavisNovaSupernova/Supernova-{n:02}.png' for n in range(1, 9)]
    assert re.findall(r'guid: (\w+)', objects[1010].split('  explosionFrames:\n')[1]) == [guid(path) for path in frames]
    assert field(objects[1010], 'explosionRadius') == '6'
    for path in frames:
        image = Image.open(ROOT / path).convert('RGBA')
        assert image.size == (512, 512)
        bounds = image.getchannel('A').getbbox()
        assert bounds and min(bounds[:2]) > 0 and max(bounds[2:]) < 512
        meta = read(path + '.meta')
        for setting in ['filterMode: 0', 'enableMipMap: 0', 'spriteMode: 1',
                        'spritePixelsToUnits: 100', 'alphaIsTransparency: 1', 'textureCompression: 0']:
            assert setting in meta, (path, setting)
    assert '{fileID: 1010}' in objects[1000]
    assert field(objects[1004], 'slots') == '12' and float(field(objects[1004], 'radius')) == RADIUS
    layout = json.loads(read('Art/Planets/DavisNova/Pixel/assembly.json'))
    cx, cy = layout['source_center']
    for n, site in enumerate(layout['sites']):
        ident = 4001 + n * 10
        body = objects[ident]
        x, y = map(float, re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+)', body).groups())
        assert math.isclose(x, (site['mount'][0] - cx) * ART_SCALE / 100, abs_tol=1e-7)
        assert math.isclose(y, (cy - site['mount'][1]) * ART_SCALE / 100, abs_tol=1e-7)
        z, w = map(float, re.search(r'm_LocalRotation: \{x: 0, y: 0, z: ([^,]+), w: ([^}]+)', body).groups())
        assert math.isclose(z, math.sin(math.radians(-site['angle']) / 2), abs_tol=1e-7)
        assert math.isclose(w, math.cos(math.radians(-site['angle']) / 2), abs_tol=1e-7)
        assert f'  - {{fileID: {ident}}}\n' in objects[1004] and 'm_Father: {fileID: 2001}' in body
    imported = json.loads(read('Art/Planets/DavisNova/unity-import.json'))
    for n, (name, order) in enumerate([('Sun', -10), ('Ring', 0), ('SocketFronts', 35)]):
        image = imported['images'][name]
        assert hashlib.sha256((ROOT / image['path']).read_bytes()).hexdigest() == image['sha256']
        assert guid(image['path']) == image['guid']
        assert image['guid'] in objects[3002 + n * 10]
        assert int(field(objects[3002 + n * 10], 'm_SortingOrder')) == order
        assert f'm_LocalScale: {{x: {ART_SCALE}, y: {ART_SCALE}, z: 1}}' in objects[3001 + n * 10]
    assert '    path: Assets/Scenes/DavisNova.unity' in read('ProjectSettings/EditorBuildSettings.asset')
    print(json.dumps(dict(serialized_assets_checked=3, references_checked=references, authored_sockets_checked=12,
                          unchanged_art_layers=3, waves_checked=3, tower_types=5, boss=BOSS,
                          synchronized_blast_seconds=40, supernova_frames=8, unity_launched=False), indent=2))


if __name__ == '__main__':
    check()
