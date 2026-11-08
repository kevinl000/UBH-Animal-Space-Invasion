"""Read-only Capen scene, socket, data and artwork checks. Does not run Unity."""
import sys
sys.dont_write_bytecode = True
from pathlib import Path
import hashlib
import json
import math
import re

from prepare_gameplay import CONFIG, ENEMIES, LEVEL, ROOT, WAVES, docs, read


def guid(path):
    return re.search(r'^guid: (\w+)', read(str(path) + '.meta'), re.M)[1]


def check():
    metas = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            assert match[1] not in metas, f'Duplicate GUID: {meta}'
            metas[match[1]] = meta.with_suffix('')
    # The preserved Capen camera has URP's package-owned camera component.
    for meta in (ROOT / 'Library/PackageCache').glob('com.unity.render-pipelines.universal@*/Runtime/UniversalAdditionalCameraData.cs.meta'):
        value = re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]
        metas[value] = meta.with_suffix('')
    reference_count = 0
    for path in [LEVEL, CONFIG, 'Assets/Scenes/CapenMajor.unity']:
        source = read(path)
        objects = docs(source)
        ids = {i for _, i, _ in objects}
        assert len(objects) == len(ids), f'Duplicate local IDs: {path}'
        assert Path(str(ROOT / path) + '.meta').exists()
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
            reference_count += 1
    level, config, scene = read(LEVEL), read(CONFIG), read('Assets/Scenes/CapenMajor.unity')
    for key, value in [('planetMaxHealth', 75), ('money', 10), ('fuel', 50), ('planetRadius', 4.9)]:
        assert float(re.search(r'^  ' + key + r': (.*)$', level, re.M)[1]) == value
    for name in ['Basic Goose 1', 'Basic Skunk 1']:
        assert guid(ENEMIES + name + '.asset') not in level + config
    prewarmed = set(re.findall(r'  - enemy: \{fileID: \d+, guid: (\w+)', level))
    assert prewarmed == {guid(ENEMIES + name + '.asset') for name in [
        'Basic Goose 2', 'Basic Skunk 2', 'Basic Goose 3', 'Shielded Goose', 'Shielded Skunk', 'Mother Goose']}
    assert '  displayName: Capen Major\n' in config
    assert '  nextPlanetName: Davis Nova\n' in config
    assert '  nextScene: DavisNova\n' in config
    assert '  starterSlots: 0000000008000000\n' in config
    final = config.split('  finalBosses:\n')[1].split('  starterSlots:')[0]
    assert re.findall(r'guid: (\w+)', final) == [guid(ENEMIES + 'Mother Goose.asset')]
    waves = re.split(r'^  - duration:', config.split('  waves:\n')[1].split('  finalBosses:')[0], flags=re.M)[1:]
    assert len(waves) == 3
    for wave, (budget, roster, miniboss) in zip(waves, WAVES):
        assert float(wave.splitlines()[0]) == 20
        assert '    restDuration: 10\n' in wave
        assert f'    costBudget: {budget}\n' in wave
        actual = re.findall(r'    - enemy: \{fileID: \d+, guid: (\w+), type: 2\}\n      enemyInstances: \[\]\n      startingBudget: ([\d.]+)\n      endingBudget: ([\d.]+)', wave)
        expected = [(guid(ENEMIES + name + '.asset'), share, share) for name, share in roster]
        assert [(g, float(a), float(b)) for g, a, b in actual] == expected
        assert math.isclose(sum(float(a) for _, a, _ in actual), 1)
        expected_miniboss = guid(ENEMIES + miniboss + '.asset') if miniboss else 'miniboss: {fileID: 0}'
        assert expected_miniboss in wave
    tutorial = read('Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset')
    for key in ['spawnInterval', 'bossEntranceDuration', 'bossReinforcementBudgetFraction', 'bossReinforcementRate',
                'bossReinforcementMaxAlive', 'bossReinforcementTaperHealth', 'bossReinforcementStopHealth']:
        assert re.search(r'^  ' + key + r': (.*)$', config, re.M)[1] == re.search(r'^  ' + key + r': (.*)$', tutorial, re.M)[1]
    by_id = {i: (c, b) for c, i, b in docs(scene)}
    assert guid(LEVEL) in by_id[1002][1] and guid(LEVEL) in by_id[1005][1] and guid(LEVEL) in by_id[1006][1]
    assert guid(CONFIG) in by_id[1008][1]
    assert '  externallyControlled: 1\n' in by_id[1006][1]
    assert '  startOnAwake: 0\n' in by_id[1006][1]
    assert '  planetBody: {fileID: 2002}\n' in by_id[1002][1]
    assert '  planet: {fileID: 2001}\n' in by_id[1004][1]
    layout = json.loads(read('Art/Planets/CapenMajor/assembly.json'))
    cx, cy = layout['planet_center']
    assert '  slots: 16\n' in by_id[1004][1]
    for n, site in enumerate(layout['sites']):
        ident = 4001 + n * 10
        body = by_id[ident][1]
        x, y = map(float, re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+)', body).groups())
        assert math.isclose(x, (site['mount'][0] - cx) / 100, abs_tol=1e-7)
        assert math.isclose(y, (cy - site['mount'][1]) / 100, abs_tol=1e-7)
        z, w = map(float, re.search(r'm_LocalRotation: \{x: 0, y: 0, z: ([^,]+), w: ([^}]+)', body).groups())
        assert math.isclose(z, math.sin(math.radians(-site['angle']) / 2), abs_tol=1e-7)
        assert math.isclose(w, math.cos(math.radians(-site['angle']) / 2), abs_tol=1e-7)
        assert 'm_Father: {fileID: 2001}' in body
        assert f'  - {{fileID: {ident}}}\n' in by_id[1004][1]
    imported = json.loads(read('Art/Planets/CapenMajor/unity-import.json'))
    for image in imported['images'].values():
        assert hashlib.sha256((ROOT / image['path']).read_bytes()).hexdigest() == image['sha256']
        assert image['guid'] == guid(image['path'])
    mother = read('Assets/Prefabs/Gameplay/Enemies/Mother Goose.prefab')
    assert '  flockSize: 4\n' in mother and '  flockInterval: 8\n' in mother
    assert guid(ENEMIES + 'Basic Goose 3.asset') in mother
    assert '    path: Assets/Scenes/CapenMajor.unity' in read('ProjectSettings/EditorBuildSettings.asset')
    assert '    path: Assets/Scenes/DavisNova.unity' in read('ProjectSettings/EditorBuildSettings.asset')
    print(json.dumps(dict(serialized_assets_checked=3, references_checked=reference_count,
                          waves_checked=3, authored_sockets_checked=16, unchanged_art_layers=8,
                          boss='Mother Goose', unity_launched=False), indent=2))


if __name__ == '__main__':
    check()
