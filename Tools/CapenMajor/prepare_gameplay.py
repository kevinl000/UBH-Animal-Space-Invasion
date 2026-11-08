"""Wire the approved Capen art into the shared gameplay loop without running Unity.

Owns CapenMajor.unity and its two gameplay data assets. Reads Union Prime's
runtime wiring as a template; never regenerates its scene, art or unit wrappers.
"""
import sys
sys.dont_write_bytecode = True
from pathlib import Path
import json

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'UnionPrime'))
from prepare_assets import HEADER, ROOT, doc, docs, gameobject, guid, read, ref, script, setfield, write

LEVEL = 'Assets/Scriptable Objects/Levels/CapenMajor.asset'
CONFIG = 'Assets/Scriptable Objects/Levels/CapenMajorGameplay.asset'
ENEMIES = 'Assets/Scriptable Objects/Enemies/README/'
WAVES = [
    (65, [('Basic Goose 2', .45), ('Basic Skunk 2', .35), ('Shielded Goose', .2)], 'Shielded Goose'),
    (85, [('Basic Goose 2', .3), ('Basic Skunk 2', .3), ('Basic Goose 3', .2),
          ('Shielded Goose', .2)], 'Shielded Skunk'),
    (105, [('Basic Goose 2', .25), ('Basic Skunk 2', .25), ('Basic Goose 3', .2),
           ('Shielded Goose', .15), ('Shielded Skunk', .15)], None),
]


def entry(name, share, indent='  ', boss=False):
    return (f'{indent}- enemy: {ref(ENEMIES + name + ".asset")}\n'
            f'{indent}  enemyInstances: []\n{indent}  startingBudget: {share}\n'
            f'{indent}  endingBudget: {share}\n{indent}  isLevelBoss: {int(boss)}\n')


def prepare_config():
    template = read('Assets/Scriptable Objects/Levels/UnionPrime.asset')
    level = template.split('  enemyDataList:\n')[0] + '  enemyDataList:\n'
    for name in ['Basic Goose 2', 'Basic Skunk 2', 'Basic Goose 3', 'Shielded Goose', 'Shielded Skunk', 'Mother Goose']:
        level += entry(name, 1, boss=name == 'Mother Goose')
    level += '  money:' + template.split('  money:', 1)[1]
    for key, value in [('m_Name', 'CapenMajor'), ('money', '10'), ('fuel', '50'),
                       ('planetMaxHealth', '75'), ('planetRadius', '4.9')]:
        level = setfield(level, key, value)
    write(LEVEL, level)
    guid(LEVEL)
    tutorial = read('Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset')
    fields = ('  displayName: Capen Major\n  stageLabel: CAMPUS DEFENSE / LEVEL 1\n'
              '  nextPlanetName: Davis Nova\n  level: ' + ref(LEVEL) + '\n  towers:\n')
    fields += tutorial.split('  towers:\n')[1].split('  waves:\n')[0] + '  waves:\n'
    for budget, roster, miniboss in WAVES:
        fields += f'  - duration: 20\n    restDuration: 10\n    costBudget: {budget}\n    roster:\n'
        fields += ''.join(entry(name, share, '    ') for name, share in roster)
        fields += '    miniboss: ' + (ref(ENEMIES + miniboss + '.asset') if miniboss else '{fileID: 0}') + '\n'
    fields += '  finalBosses:\n  - ' + ref(ENEMIES + 'Mother Goose.asset') + '\n'
    fields += ('  starterSlots: 0000000008000000\n  spawnInterval: 1\n  bossEntranceDuration: 3\n'
               '  bossReinforcementBudgetFraction: 0.5\n  bossReinforcementRate: 1\n'
               '  bossReinforcementMaxAlive: 6\n  bossReinforcementTaperHealth: 0.5\n'
               '  bossReinforcementStopHealth: 0.15\n  nextScene: DavisNova\n')
    write(CONFIG, HEADER + script(11400000, 0, 'Gameplay/LevelGameplayConfig', fields))
    guid(CONFIG)


def prepare_scene():
    path = 'Assets/Scenes/CapenMajor.unity'
    original = docs(read(path))
    tutorial = docs(read('Assets/Scenes/UnionPrime.unity'))
    out = HEADER + ''.join(doc(c, i, b) for c, i, b in original if c in [29, 104, 157, 196])
    for c, i, body in original:
        if i not in {1695506651, 1695506652, 1695506653, 1695506654, 1695506655}:
            continue
        if c == 20:
            body = setfield(setfield(body, 'm_ClearFlags', '2'), 'orthographic size', '8.7')
        out += doc(c, i, body)
    for c, i, body in tutorial:
        if not 1000 <= i <= 1009:
            continue
        body = body.replace(guid('Assets/Scriptable Objects/Levels/UnionPrime.asset'), guid(LEVEL))
        body = body.replace(guid('Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset'), guid(CONFIG))
        if i == 1004:
            body = setfield(setfield(body, 'slots', '16'), 'radius', '4.9')
            body = body.split('  socketMounts:\n')[0] + '  socketMounts:\n'
            body += ''.join(f'  - {{fileID: {4001 + n * 10}}}\n' for n in range(16))
        out += doc(c, i, body)
    layout = json.loads(read('Art/Planets/CapenMajor/assembly.json'))
    imported = json.loads(read('Art/Planets/CapenMajor/unity-import.json'))
    cx, cy = layout['planet_center']
    left, top, width, height = imported['source_view_box']
    layers = [('Administration', 0), ('BusStation', 1), ('FlagPlaza', 2),
              ('TransitShelter', 3), ('CargoDepot', 4), ('RearFloor', 5),
              ('Terrain', 20), ('SteelFronts', 35)]
    children = [3001 + n * 10 for n in range(len(layers))] + [4001 + n * 10 for n in range(16)]
    out += gameobject(2000, 2001, 'Capen Major Planet', [2002], children=children)
    body = next(b for c, i, b in tutorial if i == 2002)
    out += doc(50, 2002, body)
    sprite = next(b for c, _, b in tutorial if c == 212)
    position = ((left + width / 2 - cx) / 100, (cy - top - height / 2) / 100, 0)
    for n, (name, order) in enumerate(layers):
        go, tr, sr = 3000 + n * 10, 3001 + n * 10, 3002 + n * 10
        out += gameobject(go, tr, name, [sr], parent=2001, position=position)
        body = setfield(sprite, 'm_GameObject', f'{{fileID: {go}}}')
        body = setfield(setfield(body, 'm_Sprite', ref(imported['images'][name]['path'], 21300000, 3)), 'm_SortingOrder', str(order))
        out += doc(212, sr, body)
    for n, site in enumerate(layout['sites']):
        x, y = site['mount']
        out += gameobject(4000 + n * 10, 4001 + n * 10, 'Socket ' + str(site['site']), parent=2001,
                          position=((x - cx) / 100, (cy - y) / 100, 0), rotation=-site['angle'])
    out += doc(1660057539, 9223372036854775807, 'SceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 1695506654}\n  - {fileID: 1001}\n  - {fileID: 2001}\n')
    write(path, out)


if __name__ == '__main__':
    prepare_config()
    prepare_scene()
    print('Prepared Capen Major: 16 sockets, 75 HP, three waves and Mother Goose.')
