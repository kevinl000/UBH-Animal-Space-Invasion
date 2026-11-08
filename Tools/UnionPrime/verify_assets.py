"""Read-only checks of the serialized Union Prime integration. Does not run Unity."""
import sys
sys.dont_write_bytecode = True
from pathlib import Path
import hashlib
import json
import math
import re
import subprocess
from prepare_assets import ROOT, docs, read


def check():
    metas = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            assert match.group(1) not in metas, f'Duplicate GUID: {meta}'
            metas[match.group(1)] = meta.with_suffix('')
    changed = subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], cwd=ROOT, text=True).splitlines()
    added = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard'], cwd=ROOT, text=True).splitlines()
    assets = [ROOT / p for p in changed + added if p.endswith(('.prefab', '.asset', '.unity'))]
    refs_checked = 0
    for asset in assets:
        source = asset.read_text(encoding='utf-8-sig')
        objects = docs(source)
        ids = {i for _, i, _ in objects}
        assert len(ids) == len(objects), f'Duplicate local IDs: {asset}'
        assert Path(str(asset) + '.meta').exists(), f'Missing metadata: {asset}'
        # Check local and external references, including nested prefab modifications.
        for match in re.finditer(r'\{fileID: (-?\d+)(?:, guid: (\w+), type: \d+)?\}', source):
            file_id, value = int(match.group(1)), match.group(2)
            if file_id == 0:
                continue
            if value is None:
                assert file_id in ids, f'Missing local fileID {file_id}: {asset}'
                continue
            if value.startswith('0000000000000000'):
                continue
            assert value in metas, f'Missing GUID {value}: {asset}'
            target = metas[value]
            assert target.exists(), f'Orphan metadata: {target}'
            if target.suffix in ['.prefab', '.asset', '.unity'] and file_id != 100100000:
                assert file_id in {i for _, i, _ in docs(target.read_text(encoding='utf-8-sig'))}, f'Missing external fileID {file_id} in {target}'
            if target.suffix == '.png' and file_id != 21300000:
                texture_meta = Path(str(target) + '.meta').read_text()
                assert str(file_id) in texture_meta, f'Missing sprite slice: {target} / {file_id}'
            refs_checked += 1
        # Every root component and child reference must survive the visual replacement.
        for kind, ident, body in objects:
            if kind == 114:
                assert 'm_Script:' in body, f'Unbound script: {asset}/{ident}'
    # README numerical stats remain on canonical gameplay data assets.
    rows = [line.split('|')[1:-1] for line in read('README.md').splitlines() if line.startswith('|')]
    stat_rows = 0
    for columns in rows:
        values = [c.strip() for c in columns]
        if values[0] in ['Enemy Name', '--', 'Unlock Level', 'Level']:
            continue
        if len(values) == 7 and values[1].isdigit():
            data = read('Assets/Scriptable Objects/Enemies/README/' + values[0] + '.asset')
            stats = {'maxHealth': values[1], 'moneyDrop': values[2], 'fuelDrop': values[3], 'damage': values[4]}
        elif len(values) == 9 and values[2].isdigit() and (ROOT / ('Assets/Scriptable Objects/Towers/README/' + values[1] + '.asset')).exists():
            data = read('Assets/Scriptable Objects/Towers/README/' + values[1] + '.asset')
            stats = {'maxHealth': values[2], 'cost': values[3], 'fuelIntake': values[4], 'fov': values[5]}
            if values[6].isdigit():
                stats['damage'] = values[6]
            elif values[1] == 'Pest Trap':
                stats['damage'] = '1'
        elif len(values) == 9 and values[2].isdigit():
            data = read('Assets/Scriptable Objects/Enemies/README/' + values[1] + '.asset')
            stats = {'maxHealth': values[2], 'orbitSpeed': values[3], 'moneyDrop': values[4], 'fuelDrop': values[5]}
            if values[6].isdigit():
                stats['damage'] = values[6]
        else:
            continue
        for key, expected in stats.items():
            actual = re.search(r'^  ' + key + r': ([\d.+-]+)', data, re.M)
            assert actual and float(actual.group(1)) == float(expected), f'README stat mismatch: {values}/{key}'
        stat_rows += 1
    config = read('Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset')
    reverse = {re.search(r'^guid: (\w+)', Path(str(p) + '.meta').read_text(), re.M).group(1): p.stem
               for p in (ROOT / 'Assets/Scriptable Objects/Enemies/README').glob('*.asset')}
    wave_text = config.split('  waves:\n')[1].split('  finalBosses:')[0]
    waves = re.split(r'^  - duration:', wave_text, flags=re.M)[1:]
    expected_rosters = [{'Basic Goose 1', 'Basic Skunk 1'}, {'Basic Goose 1', 'Basic Skunk 1', 'Shielded Goose'},
                        {'Basic Goose 1', 'Basic Skunk 1', 'Shielded Goose', 'Shielded Skunk'}]
    for n, wave in enumerate(waves):
        assert float(wave.splitlines()[0]) == 20
        assert f'costBudget: {[30, 50, 70][n]}' in wave
        assert 'restDuration: 10' in wave
        roster = {reverse[g] for g in re.findall(r'    - enemy: \{fileID: \d+, guid: (\w+)', wave)}
        assert roster == expected_rosters[n], (n, roster)
    assert len(waves) == 3
    assert 'starterSlots: 0000000006000000' in config
    assert 'nextScene: CapenMajor' in config
    for field, value in {'bossReinforcementBudgetFraction': '0.5', 'bossReinforcementRate': '1',
                         'bossReinforcementMaxAlive': '6', 'bossReinforcementTaperHealth': '0.5',
                         'bossReinforcementStopHealth': '0.15'}.items():
        assert f'  {field}: {value}\n' in config, f'Unexpected boss reinforcement tuning: {field}'
    scene = {i: b for _, i, b in docs(read('Assets/Scenes/UnionPrime.unity'))}
    placements = json.loads(read('Art/Planets/UnionPrime/polish.json'))['site_placements']
    for n, site in enumerate(placements):
        transform = scene[4001 + n * 10]
        x, y = [float(v) for v in re.search(r'm_LocalPosition: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)', transform).groups()]
        assert abs(x * 100 + 627 - site['study_mount'][0]) < 1e-6
        assert abs(627 - y * 100 - site['study_mount'][1]) < 1e-6
    effects_checked = 0
    for prefab in (ROOT / 'Assets/Prefabs/Gameplay/Effects').glob('*.prefab'):
        if prefab.name == 'Tower Explosion.prefab':
            continue
        source = prefab.read_text()
        visual_guid = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)', source).group(1)
        visual = metas[visual_guid].read_text()
        controller_guid = re.search(r'm_Controller: \{fileID: \d+, guid: (\w+)', visual).group(1)
        controller = metas[controller_guid].read_text()
        clip_guid = re.search(r'm_Motion: \{fileID: \d+, guid: (\w+)', controller).group(1)
        clip = metas[clip_guid].read_text()
        assert 'm_LoopTime: 0' in clip and 'm_Events: []' in clip
        assert 'attribute: m_Sprite' in clip
        for curve in ['m_RotationCurves', 'm_PositionCurves', 'm_ScaleCurves', 'm_FloatCurves']:
            assert curve + ': []' in clip, f'Unexpected transform animation: {prefab}'
        keys = re.findall(r'- time: ([\d.]+)\n      value: \{fileID: (-?\d+)', clip)
        assert keys and int(keys[-1][1]) == 0, f'One-shot must end blank: {prefab}'
        lifetime = float(re.search(r'lifetime: ([\d.]+)', source).group(1))
        assert lifetime >= float(keys[-1][0]), f'Effect despawns before its last key: {prefab}'
        assert not any(c == 58 for c, _, _ in docs(source)), f'Visual effect has a hit collider: {prefab}'
        effects_checked += 1
    # Approved source artwork and import settings have no changes from main.
    assert not subprocess.check_output(['git', 'diff', 'main', '--name-only', '--', 'Art', 'Assets/Art/Towers',
        'Assets/Art/Enemies', 'Assets/Art/Projectiles', 'Assets/Art/Effects', 'Assets/Art/Planets'], cwd=ROOT, text=True).strip()
    # Every preexisting asset keeps its GUID and every existing typed prefab root keeps its component ID.
    existing = set(subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', 'HEAD'], cwd=ROOT, text=True).splitlines())
    for path in changed:
        if path not in existing:
            continue
        if path.endswith('.meta'):
            old = subprocess.check_output(['git', 'show', 'HEAD:' + path], cwd=ROOT, text=True)
            assert re.search(r'^guid: (\w+)', old, re.M).group(1) == re.search(r'^guid: (\w+)', read(path), re.M).group(1)
        if path.startswith('Assets/Prefabs/Gameplay/') and path.endswith('.prefab'):
            old = subprocess.check_output(['git', 'show', 'HEAD:' + path], cwd=ROOT, text=True)
            typed = [(i, re.search(r'm_Script:.*guid: (\w+)', b).group(1)) for c, i, b in docs(old) if c == 114]
            current = {i: b for c, i, b in docs(read(path)) if c == 114}
            for i, value in typed:
                assert i in current and value in current[i], f'Changed gameplay root: {path}'
    print(json.dumps({'serialized_assets_checked': len(assets), 'external_references_checked': refs_checked,
                      'README_stat_rows_checked': stat_rows, 'wave_rosters_checked': len(waves),
                      'approved_socket_positions_checked': len(placements), 'approved_artwork_changes': 0,
                      'one_shot_bindings_and_lifetimes_checked': effects_checked,
                      'unity_launched': False}, indent=2))


if __name__ == '__main__':
    check()
