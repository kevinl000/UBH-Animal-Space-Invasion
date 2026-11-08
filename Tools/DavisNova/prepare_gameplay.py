"""Prepare Davis-only gameplay assets; never launches Unity or rebuilds source art."""
import sys
sys.dont_write_bytecode = True
from pathlib import Path
import json
import re

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'UnionPrime'))
from prepare_assets import HEADER, ROOT, doc, docs, gameobject, guid, read, ref, script, setfield, write

LEVEL = 'Assets/Scriptable Objects/Levels/DavisNova.asset'
CONFIG = 'Assets/Scriptable Objects/Levels/DavisNovaGameplay.asset'
SCENE = 'Assets/Scenes/DavisNova.unity'
ENEMIES = 'Assets/Scriptable Objects/Enemies/README/'
TOWERS = 'Assets/Scriptable Objects/Towers/README/'
TOWER_NAMES = ['Laser', 'Bomb', 'Sniper', 'Pest Trap', 'Laser SMG']
BOSS = 'Mother Skunk'
WAVES = [
    (80, [('Basic Goose 2', .3), ('Basic Skunk 2', .3), ('Basic Goose 3', .2),
          ('Shielded Goose', .2)], 'Shielded Goose'),
    (105, [('Basic Goose 3', .3), ('Basic Skunk 2', .25), ('Basic Skunk 3', .15),
           ('Shielded Goose', .15), ('Shielded Skunk', .15)], 'Shielded Skunk'),
    (130, [('Basic Goose 3', .3), ('Basic Skunk 3', .25), ('Shielded Goose', .2),
           ('Shielded Skunk', .25)], None),
]
# The shared tower wrapper has 1.5x source scale; approved Davis art uses 2x.
# Scaling the ring and its measured mounts preserves the approved foundation fit.
ART_SCALE = .75
RADIUS = 3.96
WORLD_MAGNIFICATION = 1.5


def entry(name, share, indent='  ', boss=False):
    return (f'{indent}- enemy: {ref(ENEMIES + name + ".asset")}\n'
            f'{indent}  enemyInstances: []\n{indent}  startingBudget: {share}\n'
            f'{indent}  endingBudget: {share}\n{indent}  isLevelBoss: {int(boss)}\n')


def prepare_config():
    template = read(LEVEL)
    level = template.split('  enemyDataList:\n')[0] + '  enemyDataList:\n'
    names = list(dict.fromkeys(name for _, roster, _ in WAVES for name, _ in roster))
    for name in names + [BOSS]:
        level += entry(name, 1, boss=name == BOSS)
    level += '  money:' + template.split('  money:', 1)[1]
    for key, value in [('money', 10), ('fuel', 50), ('planetMaxHealth', 75),
                       ('planetRadius', RADIUS), ('planetGravity', 0),
                       ('planetRotationSpeed', 6), ('isClockwise', 1),
                       ('startingDifficulty', 80), ('postBossDifficulty', 105),
                       ('maxDifficulty', 315), ('maxTime', 60)]:
        level = setfield(level, key, str(value))
    write(LEVEL, level)
    fields = ('  displayName: Davis Nova\n  stageLabel: CAMPUS DEFENSE / LEVEL 2\n'
              '  nextPlanetName: Level Select\n  level: ' + ref(LEVEL) + '\n  towers:\n')
    fields += ''.join('  - ' + ref(TOWERS + name + '.asset') + '\n' for name in TOWER_NAMES)
    fields += '  waves:\n'
    for budget, roster, miniboss in WAVES:
        fields += f'  - duration: 20\n    restDuration: 10\n    costBudget: {budget}\n    roster:\n'
        fields += ''.join(entry(name, share, '    ') for name, share in roster)
        fields += '    miniboss: ' + (ref(ENEMIES + miniboss + '.asset') if miniboss else '{fileID: 0}') + '\n'
    fields += '  finalBosses:\n  - ' + ref(ENEMIES + BOSS + '.asset') + '\n'
    fields += ('  starterSlots: 0000000006000000\n  spawnInterval: 1\n  bossEntranceDuration: 3\n'
               '  bossReinforcementBudgetFraction: 0.5\n  bossReinforcementRate: 1\n'
               '  bossReinforcementMaxAlive: 6\n  bossReinforcementTaperHealth: 0.5\n'
               '  bossReinforcementStopHealth: 0.15\n  nextScene:\n')
    write(CONFIG, HEADER + script(11400000, 0, 'Gameplay/LevelGameplayConfig', fields))
    guid(CONFIG)


def prepare_scene():
    original = docs(read(SCENE))
    template = docs(read('Assets/Scenes/UnionPrime.unity'))
    out = HEADER + ''.join(doc(c, i, b) for c, i, b in original if c in [29, 104, 157, 196])
    camera_id, camera = next((i, b) for c, i, b in original if c == 20)
    camera_go = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', camera)[1])
    camera_object = next(b for c, i, b in original if i == camera_go)
    camera_ids = {int(i) for i in re.findall(r'component: \{fileID: (\d+)\}', camera_object)} | {camera_go}
    camera_transform = next(i for c, i, _ in original if c == 4 and i in camera_ids)
    for c, i, body in original:
        if i not in camera_ids:
            continue
        if c == 20:
            body = setfield(setfield(body, 'm_ClearFlags', '2'), 'orthographic size', str(8.7 / WORLD_MAGNIFICATION))
        out += doc(c, i, body)
    for c, i, body in template:
        if not 1000 <= i <= 1009:
            continue
        body = body.replace(guid('Assets/Scriptable Objects/Levels/UnionPrime.asset'), guid(LEVEL))
        body = body.replace(guid('Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset'), guid(CONFIG))
        body = body.replace('{fileID: 1695506653}', f'{{fileID: {camera_id}}}')
        if i == 1000:
            body = body.replace('  m_Layer:', '  - component: {fileID: 1010}\n  m_Layer:', 1)
        if i == 1004:
            body = setfield(body, 'radius', str(RADIUS))
        if i == 1007:
            body += '  - ' + ref(TOWERS + 'Laser SMG.asset') + '\n'
        if i == 1009:
            body = body.replace('  loop: {fileID: 1008}\n', f'  loop: {{fileID: 1008}}\n  worldMagnification: {WORLD_MAGNIFICATION}\n')
            body += '  - ' + ref('Assets/Art/Towers/LaserSMG/LaserSMGBase.png', 21300000, 3) + '\n'
        out += doc(c, i, body)
    out += script(1010, 1000, 'Gameplay/DavisNovaSunBlast',
                  '  loop: {fileID: 1008}\n  sun: {fileID: 3002}\n  explosionFrames:\n'
                  + ''.join('  - ' + ref(f'Assets/Art/Effects/DavisNovaSupernova/Supernova-{n:02}.png', 21300000, 3) + '\n' for n in range(1, 9))
                  + '  explosionRadius: 6\n  intervalSeconds: 40\n  warningSeconds: 5\n')
    layout = json.loads(read('Art/Planets/DavisNova/Pixel/assembly.json'))
    imported = json.loads(read('Art/Planets/DavisNova/unity-import.json'))
    cx, cy = layout['source_center']
    layers = [('Sun', -10), ('Ring', 0), ('SocketFronts', 35)]
    children = [3001 + n * 10 for n in range(3)] + [4001 + n * 10 for n in range(12)]
    out += gameobject(2000, 2001, 'Davis Nova Ring', [2002], children=children)
    out += doc(50, 2002, next(b for c, i, b in template if i == 2002))
    sprite = next(b for c, _, b in template if c == 212)
    for n, (name, order) in enumerate(layers):
        go, tr, sr = 3000 + n * 10, 3001 + n * 10, 3002 + n * 10
        transform = gameobject(go, tr, name, [sr], parent=2001)
        transform = setfield(transform, 'm_LocalScale', f'{{x: {ART_SCALE}, y: {ART_SCALE}, z: 1}}')
        out += transform
        body = setfield(sprite, 'm_GameObject', f'{{fileID: {go}}}')
        body = setfield(setfield(body, 'm_Sprite', ref(imported['images'][name]['path'], 21300000, 3)), 'm_SortingOrder', str(order))
        out += doc(212, sr, body)
    for n, site in enumerate(layout['sites']):
        x, y = site['mount']
        out += gameobject(4000 + n * 10, 4001 + n * 10, 'Socket ' + str(site['site']), parent=2001,
                          position=((x - cx) * ART_SCALE / 100, (cy - y) * ART_SCALE / 100, 0), rotation=-site['angle'])
    out += doc(1660057539, 9223372036854775807, 'SceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n'
               + f'  - {{fileID: {camera_transform}}}\n  - {{fileID: 1001}}\n  - {{fileID: 2001}}\n')
    write(SCENE, out)


if __name__ == '__main__':
    prepare_config()
    prepare_scene()
    print('Prepared Davis Nova: twelve measured sockets, level 2 waves, Mother Skunk and synchronized sun blasts.')
