"""Prepare serialized gameplay assets without launching Unity or changing source artwork.

Run from the repo root. Existing root component IDs, metadata and visual-prefab
GUIDs are preserved. The generator owns UnionPrime.unity and the wrappers below;
it does not rebuild the other planet scenes or their artwork.
"""
from pathlib import Path
import hashlib
import json
import math
import re
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


def write(path, text):
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(text, encoding='utf-8', newline='\n')


def guid(path):
    meta = ROOT / (str(path) + '.meta')
    if not meta.exists():
        value = uuid.uuid5(uuid.NAMESPACE_URL, 'ubh-union-prime/' + str(path)).hex
        meta.parent.mkdir(parents=True, exist_ok=True)
        meta.write_text(f'fileFormatVersion: 2\nguid: {value}\n', encoding='utf-8')
    return re.search(r'^guid: (\w+)', meta.read_text(), re.M).group(1)


def docs(text):
    return [(int(c), int(i), b) for c, i, b in re.findall(r'--- !u!(\d+) &(\d+)(?: stripped)?\n(.*?)(?=--- !u!|\Z)', text, re.S)]


def doc(kind, ident, body):
    return f'--- !u!{kind} &{ident}\n{body.rstrip()}\n'


def ref(path, ident=11400000, typ=2):
    return f'{{fileID: {ident}, guid: {guid(path)}, type: {typ}}}'


def setfield(text, name, value):
    replacement = f'  {name}: {value}'
    if re.search(r'^  ' + re.escape(name) + r':.*$', text, re.M):
        return re.sub(r'^  ' + re.escape(name) + r':.*$', lambda _: replacement, text, flags=re.M)
    return text.rstrip() + '\n' + replacement + '\n'


def script(ident, go, name, fields=''):
    return doc(114, ident, f'''MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {ref('Assets/Scripts/' + name + '.cs', 11500000, 3)}
  m_Name:
  m_EditorClassIdentifier: PlanetaryDefense.Runtime::{name.split('/')[-1]}
{fields}''')


def visual_instance(path, parent, scale=1, rotation=0, order=30):
    source = docs(read(path))
    root_go = source[0][1]
    root_transform = next(i for c, i, _ in source if c == 4)
    transform = next(b for c, i, b in source if i == root_transform)
    authored = [float(v) for v in re.search(r'm_LocalScale: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+), z: ([\d.eE+-]+)', transform).groups()]
    z = math.sin(math.radians(rotation) / 2)
    w = math.cos(math.radians(rotation) / 2)
    mods = [(root_transform, 'm_LocalPosition.' + axis, 0) for axis in 'xyz']
    mods += [(root_transform, 'm_LocalScale.' + axis, scale * authored[n]) for n, axis in enumerate('xyz')]
    mods += [(root_transform, 'm_LocalRotation.' + axis, v) for axis, v in zip('xyzw', [0, 0, z, w])]
    orders = [int(re.search(r'm_SortingOrder: (-?\d+)', b).group(1)) for c, _, b in source if c == 212]
    for c, i, body in source:
        if c == 212:
            relative = int(re.search(r'm_SortingOrder: (-?\d+)', body).group(1)) - min(orders)
            mods.append((i, 'm_SortingOrder', order + relative))
    entries = ''
    for ident, key, value in mods:
        entries += f'''    - target: {ref(path, ident, 3)}
      propertyPath: {key}
      value: {value}
      objectReference: {{fileID: 0}}
'''
    return f'''--- !u!1001 &900002
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {parent}}}
    m_Modifications:
{entries}    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {ref(path, 100100000, 3)}
--- !u!4 &900003 stripped
Transform:
  m_CorrespondingSourceObject: {ref(path, root_transform, 3)}
  m_PrefabInstance: {{fileID: 900002}}
  m_PrefabAsset: {{fileID: 0}}
'''


TOWER_FOLDER = 'Assets/Prefabs/Gameplay/Towers/'
ENEMY_FOLDER = 'Assets/Prefabs/Gameplay/Enemies/'
SHOT_FOLDER = 'Assets/Prefabs/Gameplay/Projectiles/'
EFFECT_FOLDER = 'Assets/Prefabs/Gameplay/Effects/'
DATA_FOLDER = 'Assets/Scriptable Objects/'


def component(path):
    return next(i for c, i, b in docs(read(path)) if c == 114 and 'PlanetaryDefense.Runtime::' in b)


def assetref(path):
    return ref(path, component(path), 3)


def projectile_collider(visual, scale, rotation):
    """A small hit circle centered on visible flight pixels, forward of emission pivots."""
    source = read(visual)
    sprite_id, sprite_guid = re.search(r'm_Sprite: \{fileID: (\d+), guid: (\w+)', source).groups()
    meta = next(p for p in (ROOT / 'Assets/Art/Projectiles').rglob('*.png.meta') if sprite_guid in p.read_text())
    settings = meta.read_text()
    ppu = float(re.search(r'spritePixelsToUnits: ([\d.]+)', settings).group(1))
    sprite = next(part for part in settings.split('    - serializedVersion: 2')[1:] if re.search(r'internalID: ' + sprite_id + r'\b', part))
    x, y, width, height = [int(v) for v in re.search(r'        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)', sprite).groups()]
    pivot_x, pivot_y = [float(v) for v in re.search(r'pivot: \{x: ([\d.]+), y: ([\d.]+)', sprite).groups()]
    image = Image.open(meta.with_suffix('')).convert('RGBA')
    cell = image.crop((x, image.height - y - height, x + width, image.height - y))
    left, top, right, bottom = cell.getchannel('A').point(lambda a: 255 if a >= 16 else 0).getbbox()
    authored_x, authored_y = [float(v) for v in re.search(r'm_LocalScale: \{x: ([\d.]+), y: ([\d.]+)', source).groups()]
    center_x = ((left + right) / 2 - width * pivot_x) / ppu * authored_x * scale
    center_y = (height * (1 - pivot_y) - (top + bottom) / 2) / ppu * authored_y * scale
    radians = math.radians(rotation)
    offset = (math.cos(radians) * center_x - math.sin(radians) * center_y,
              math.sin(radians) * center_x + math.cos(radians) * center_y)
    radius = min((right - left) / ppu * authored_x, (bottom - top) / ppu * authored_y) * scale * .45
    return radius, offset


def wrap(path, visual, scale, rotation, category, effect_fields='', impact=None, secondary=None):
    # Keep the teammate's gameplay root and its serialized component IDs.
    source = docs(read(path))
    root_script = next(b for c, _, b in source if c == 114 and 'PlanetaryDefense.Runtime::' in b and '::CombatVisual' not in b)
    go_id = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', root_script).group(1))
    go = next(b for c, i, b in source if c == 1 and i == go_id)
    ids = [int(i) for i in re.findall(r'component: \{fileID: (\d+)\}', go)]
    transform_id = next(i for c, i, b in source if c == 4 and i in ids)
    ids = [i for i in ids if i != 900001]
    go = re.sub(r'  m_Component:\n(?:  - component:.*\n)+', '  m_Component:\n' + ''.join(f'  - component: {{fileID: {i}}}\n' for i in ids) + ('' if category == 'shot' else '  - component: {fileID: 900001}\n'), go)
    out = HEADER + doc(1, go_id, go)
    for c, i, body in source:
        if i == go_id or i not in ids:
            continue
        if i == transform_id:
            body = re.sub(r'  m_Children:(?: \[\]\n|\n(?:  - \{fileID:.*\n)+)', '  m_Children:\n  - {fileID: 900003}\n', body)
        if c == 58:
            radius = 0.04 if category == 'shot' else 0.42 if category == 'tower' else 0.38 * (1.5 if 'Healthy' in path or 'Mother' in path or 'Buck' in path else 1)
            if category == 'shot':
                radius, offset = projectile_collider(visual, scale, rotation)
                body = setfield(body, 'm_Offset', f'{{x: {offset[0]}, y: {offset[1]}}}')
            body = setfield(body, 'm_Radius', str(radius))
            if category == 'tower':
                body = setfield(body, 'm_Offset', '{x: 0, y: 0.45}')
        if c == 114:
            if category == 'shot':
                if '::TowerProjectile' in body:
                    body = setfield(setfield(body, 'speed', '8'), 'maxLifeTime', '5')
                if impact:
                    body = setfield(body, 'impactEffect', assetref(EFFECT_FOLDER + impact + '.prefab'))
                if secondary:
                    body = setfield(body, 'secondaryEffect', assetref(EFFECT_FOLDER + secondary + '.prefab'))
        out += doc(c, i, body)
    if category != 'shot':
        fields = effect_fields
        fields += '  frozenScale: 1.1\n  frozenVisual: ' + ref('Assets/Prefabs/Effects/BuckFrozenStatusVisual.prefab', 100000, 3) + '\n' if category == 'tower' else ''
        out += script(900001, go_id, 'Gameplay/CombatVisual', fields)
    out += visual_instance(visual, transform_id, scale, rotation, 30 if category == 'tower' else 60 if category == 'enemy' else 75)
    write(path, out)


def make_effect(name, visual, lifetime, scale=1):
    path = EFFECT_FOLDER + name + '.prefab'
    # Keep the full authored animation, including the final blank key.
    controller_guid = re.search(r'm_Controller: \{fileID: \d+, guid: (\w+)', read(visual))
    if controller_guid:
        controllers = list((ROOT / 'Assets/Animations').rglob('*.controller.meta'))
        controller = next(p.with_suffix('') for p in controllers if controller_guid.group(1) in p.read_text())
        motion = re.search(r'm_Motion: \{fileID: \d+, guid: (\w+)', controller.read_text())
        if motion:
            clips = (ROOT / 'Assets/Animations').rglob('*.anim.meta')
            clip = next(p.with_suffix('') for p in clips if motion.group(1) in p.read_text())
            lifetime = max(lifetime, float(re.search(r'm_StopTime: ([\d.]+)', clip.read_text()).group(1)) + .002)
    # Reuse the teammate's native component serialization, without its placeholder art.
    source = docs(read(EFFECT_FOLDER + 'Tower Explosion.prefab'))
    root_script = next(b for c, _, b in source if c == 114 and '::PooledEffect' in b)
    go = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', root_script).group(1))
    go_body = next(b for c, i, b in source if i == go)
    ids = [int(i) for i in re.findall(r'component: \{fileID: (\d+)\}', go_body)]
    collider_ids = {i for c, i, _ in source if c == 58}
    ids = [i for i in ids if i not in collider_ids]
    tid = next(i for c, i, b in source if c == 4 and i in ids)
    out = HEADER
    for c, i, body in source:
        if i != go and i not in ids:
            continue
        if c == 1:
            body = setfield(body, 'm_Name', name)
            body = re.sub(r'  - component: \{fileID: (\d+)\}\n', lambda m: '' if int(m.group(1)) in collider_ids else m.group(0), body)
        if i == tid:
            body = re.sub(r'  m_Children:(?: \[\]\n|\n(?:  - \{fileID:.*\n)+)', '  m_Children:\n  - {fileID: 900003}\n', body)
        if c == 114:
            body = setfield(body, 'lifetime', str(lifetime))
        out += doc(c, i, body)
    out += visual_instance(visual, tid, scale, 0, 100)
    write(path, re.sub(r'[ \t]+(?=\n)', '', out))
    guid(path)


def prepare_wrappers():
    effects = {
        'Laser Muzzle': ('Projectiles/RabbitLaserMuzzleVisual', .084, 1.5),
        'Laser Impact': ('Projectiles/RabbitLaserImpactVisual', .168, 1.5),
        'Bomb Muzzle': ('Projectiles/RabbitBombMuzzleVisual', .084, 1.5),
        'Bomb Explosion': ('Projectiles/RabbitBombExplosionVisual', .279, 1),
        'Sniper Muzzle': ('Projectiles/RabbitSniperMuzzleVisual', .084, 1.5),
        'Sniper Impact': ('Projectiles/RabbitSniperImpactVisual', .168, 1.5),
        'Pest Muzzle': ('Projectiles/RabbitPestTrapMuzzleVisual', .084, 1.5),
        'Pest Impact': ('Projectiles/RabbitPestTrapImpactVisual', .251, 1.5),
        'Tesla Muzzle': ('Projectiles/RabbitTeslaMuzzleVisual', .084, 1.5),
        'Tesla Burst': ('Projectiles/RabbitTeslaBurstVisual', .251, 1),
        'Goose Impact': ('Projectiles/GoosePoopImpactVisual', .251, .4),
        'Skunk Burst': ('Projectiles/SkunkBombBurstVisual', .334, .4),
        'Skunk Puff': ('Projectiles/SkunkBombHitPuffVisual', .251, .4),
        'Horn Impact': ('Projectiles/BuckHornImpactVisual', .251, .4),
        'Radar Scan': ('Effects/RadarScanPulseVisual', .251, 1),
        'Radar Reveal': ('Effects/RadarRevealMarkerVisual', .251, .4),
        'Goose Call': ('Effects/MotherGooseCallVisual', .501, .4),
    }
    for name, (visual, duration, scale) in effects.items():
        make_effect(name, 'Assets/Prefabs/' + visual + '.prefab', duration, scale)
    shots = {
        'Laser Bolt': ('RabbitLaserFlightVisual', 1.5, 0, 'Laser Impact', None),
        'Bomb Shell': ('RabbitBombProjectileVisual', 1.5, 0, 'Bomb Explosion', None),
        'Sniper Round': ('RabbitSniperProjectileVisual', 1.5, 0, 'Sniper Impact', None),
        'Sticky Goo': ('RabbitPestTrapProjectileVisual', 1.5, 0, 'Pest Impact', None),
        'Spark': ('RabbitTeslaFlightVisual', 1.5, 0, 'Tesla Burst', None),
        'Goose Poop': ('GoosePoopProjectileVisual', .4, 90, 'Goose Impact', None),
        'Skunk Bomb': ('SkunkBombProjectileVisual', .4, 0, 'Skunk Burst', 'Skunk Puff'),
        'Healthy Goose Poop': ('HealthyGooseProjectileVisual', .4, 90, 'Goose Impact', None),
        'Healthy Skunk Bomb': ('HealthySkunkProjectileVisual', .4, 0, 'Skunk Burst', 'Skunk Puff'),
        'Buck Horn': ('BuckHornProjectileVisual', .4, 0, 'Horn Impact', None),
    }
    for name, (visual, scale, rotation, impact, secondary) in shots.items():
        path = SHOT_FOLDER + name + '.prefab'
        if not (ROOT / path).exists():
            template = SHOT_FOLDER + ('Goose Poop' if name != 'Sniper Round' else 'Laser Bolt') + '.prefab'
            write(path, setfield(read(template), 'm_Name', name))
        wrap(path, 'Assets/Prefabs/Projectiles/' + visual + '.prefab', scale, rotation, 'shot', impact=impact, secondary=secondary)
        guid(path)
    towers = {
        'Laser': ('Laser', 'Laser Muzzle', 'Laser Bolt'),
        'Bomb': ('Bomb', 'Bomb Muzzle', 'Bomb Shell'),
        'Sniper': ('Sniper', 'Sniper Muzzle', 'Sniper Round'),
        'Pest Trap': ('PestTrap', 'Pest Muzzle', 'Sticky Goo'),
        'Radar': ('Radar', 'Radar Scan', None),
        'Oil Rig': ('OilRig', None, None),
        'Bank': ('Bank', None, None),
        'Laser SMG': ('LaserSMG', 'Laser Muzzle', 'Laser Bolt'),
        'Tesla Coil': ('TeslaCoil', 'Tesla Muzzle', 'Spark'),
    }
    for name, (visual, muzzle, shot) in towers.items():
        fields = '  attackEffect: ' + (assetref(EFFECT_FOLDER + muzzle + '.prefab') if muzzle else '{fileID: 0}') + '\n'
        if name == 'Radar':
            fields += '  attackPointsUp: 1\n  emissionLocalPosition: {x: 0.28607142855, y: 1.0896428571}\n'
        # Effects already include the tower scale, so don't apply it twice.
        wrap(TOWER_FOLDER + name + '.prefab', 'Assets/Prefabs/Towers/' + visual + 'TowerVisual.prefab', 1.5, 0, 'tower', fields)
        data = DATA_FOLDER + 'Towers/README/' + name + '.asset'
        text = setfield(read(data), 'projectilePrefab', assetref(SHOT_FOLDER + shot + '.prefab') if shot else '{fileID: 0}')
        write(data, text)
    enemy_names = ['Basic Goose 1', 'Basic Goose 2', 'Basic Goose 3', 'Basic Skunk 1', 'Basic Skunk 2', 'Basic Skunk 3',
                   'Shielded Goose', 'Shielded Skunk', 'Stealth Goose', 'Healthy Goose', 'Healthy Skunk', 'Mother Goose', 'Mother Skunk', 'Restraining Buck']
    for name in enemy_names:
        visual = name.replace(' ', '')
        fields = '  revealEffect: ' + assetref(EFFECT_FOLDER + 'Radar Reveal.prefab') + '\n'
        # Place unmarked outlets at the visible underside lip, accounting for the
        # visual's authored boss scale and the +Y-inward gameplay convention.
        visual_path = 'Assets/Prefabs/Enemies/' + visual + 'Visual.prefab'
        visual_text = read(visual_path)
        if 'm_Name: Muzzle' not in visual_text and 'm_Name: CallOrigin' not in visual_text:
            sprite_guid = re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)', visual_text).group(1)
            meta = next(p for p in (ROOT / 'Assets/Art/Enemies').rglob('*.png.meta') if sprite_guid in p.read_text())
            ppu = float(re.search(r'spritePixelsToUnits: ([\d.]+)', meta.read_text()).group(1))
            image = Image.open(meta.with_suffix('')).convert('RGBA')
            x = image.width // 2
            bottom = max(y for y in range(image.height) if image.getpixel((x, y))[3] >= 128)
            authored = float(re.search(r'm_LocalScale: \{x: ([\d.]+)', visual_text).group(1))
            offset = (bottom - image.height / 2) / ppu * authored * .4
            fields += f'  emissionLocalPosition: {{x: 0, y: {offset}}}\n'
        if name == 'Mother Goose':
            fields += '  attackScale: 1.5\n  attackEffect: ' + assetref(EFFECT_FOLDER + 'Goose Call.prefab') + '\n'
        if name == 'Stealth Goose':
            fields += '  revealScale: 3.5\n'
        wrap(ENEMY_FOLDER + name + '.prefab', 'Assets/Prefabs/Enemies/' + visual + 'Visual.prefab', .4, 180, 'enemy', fields)
        shot = 'Buck Horn' if name == 'Restraining Buck' else 'Healthy Goose Poop' if name == 'Healthy Goose' else 'Healthy Skunk Bomb' if name in ['Healthy Skunk', 'Mother Skunk'] else 'Goose Poop' if 'Goose' in name else 'Skunk Bomb'
        if name == 'Mother Goose':
            continue
        data = DATA_FOLDER + 'Enemies/README/' + name + '.asset'
        write(data, setfield(read(data), 'projectilePrefab', assetref(SHOT_FOLDER + shot + '.prefab')))


def prepare_config():
    level_path = DATA_FOLDER + 'Levels/UnionPrime.asset'
    text = read(level_path)
    for key, value in {'money': 15, 'fuel': 180, 'planetMaxHealth': 100, 'planetRadius': 3.3,
                       'planetRotationSpeed': 6, 'isClockwise': 1, 'maxTime': 60}.items():
        text = setfield(text, key, str(value))
    # Prewarm/validation roster; actual wave permissions are exclusively in the configuration.
    roster = ''
    for name in ['Basic Goose 1', 'Basic Skunk 1', 'Shielded Goose', 'Shielded Skunk', 'Healthy Goose', 'Healthy Skunk']:
        roster += f'''  - enemy: {ref(DATA_FOLDER + 'Enemies/README/' + name + '.asset')}
    enemyInstances: []
    startingBudget: 1
    endingBudget: 1
    isLevelBoss: {1 if name.startswith('Healthy') else 0}
'''
    text = re.sub(r'  enemyDataList:.*?(?=  money:)', '  enemyDataList:\n' + roster, text, flags=re.S)
    write(level_path, text)
    fields = '  level: ' + ref(level_path) + '\n  towers:\n'
    for name in ['Laser', 'Bomb', 'Sniper', 'Pest Trap']:
        fields += '  - ' + ref(DATA_FOLDER + 'Towers/README/' + name + '.asset') + '\n'
    fields += '  waves:\n'
    waves = [(30, [.5, .5], 'Shielded Goose'), (50, [.4, .4, .2], 'Shielded Skunk'), (70, [.3, .3, .2, .2], None)]
    for budget, shares, miniboss in waves:
        fields += f'  - duration: 20\n    restDuration: 15\n    costBudget: {budget}\n    roster:\n'
        for name, share in zip(['Basic Goose 1', 'Basic Skunk 1', 'Shielded Goose', 'Shielded Skunk'], shares):
            fields += f'''    - enemy: {ref(DATA_FOLDER + 'Enemies/README/' + name + '.asset')}
      enemyInstances: []
      startingBudget: {share}
      endingBudget: {share}
      isLevelBoss: 0
'''
        fields += '    miniboss: ' + (ref(DATA_FOLDER + 'Enemies/README/' + miniboss + '.asset') if miniboss else '{fileID: 0}') + '\n'
    fields += '  finalBosses:\n'
    for name in ['Healthy Goose', 'Healthy Skunk']:
        fields += '  - ' + ref(DATA_FOLDER + 'Enemies/README/' + name + '.asset') + '\n'
    fields += '  starterSlots: 0000000006000000\n  spawnInterval: 1\n  bossEntranceDuration: 3\n  nextScene: CapenMajor\n'
    config = script(11400000, 0, 'Gameplay/LevelGameplayConfig', fields)
    write(DATA_FOLDER + 'Levels/UnionPrimeGameplay.asset', HEADER + config)
    guid(DATA_FOLDER + 'Levels/UnionPrimeGameplay.asset')


def gameobject(go, transform, name, components=(), parent=0, position=(0, 0, 0), rotation=0, children=()):
    z, w = math.sin(math.radians(rotation) / 2), math.cos(math.radians(rotation) / 2)
    refs = ''.join(f'  - component: {{fileID: {i}}}\n' for i in [transform, *components])
    childrefs = '\n' + ''.join(f'  - {{fileID: {i}}}\n' for i in children) if children else ' []\n'
    return doc(1, go, f'''GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{refs}  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_IsActive: 1
''') + doc(4, transform, f'''Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: {z}, w: {w}}}
  m_LocalPosition: {{x: {position[0]}, y: {position[1]}, z: {position[2]}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:{childrefs}  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {rotation}}}
''')


def prepare_scene():
    scene_path = 'Assets/Scenes/UnionPrime.unity'
    original = docs(read(scene_path))
    camera_ids = {1695506651, 1695506652, 1695506653, 1695506654}
    out = HEADER + ''.join(doc(c, i, b) for c, i, b in original if c in [29, 104, 157, 196])
    for c, i, body in original:
        if i not in camera_ids:
            continue
        if c == 20:
            body = setfield(setfield(body, 'm_ClearFlags', '2'), 'm_BackGroundColor', '{r: 0.025, g: 0.045, b: 0.085, a: 1}')
            body = setfield(body, 'orthographic size', '8.7')
        out += doc(c, i, body)
    # Stable scene IDs make this builder repeatable and keep every reference reviewable.
    out += gameobject(1000, 1001, 'Gameplay', [1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009])
    out += script(1002, 1000, 'Managers/LevelManager', f'  levelData: {ref(DATA_FOLDER + "Levels/UnionPrime.asset")}\n  planetBody: {{fileID: 2002}}\n')
    out += script(1003, 1000, 'Pooling/PoolManager', '  defaultCapacity: 20\n  maxSizePerPrefab: 200\n')
    sites = json.loads(read('Art/Planets/UnionPrime/polish.json'))['site_placements']
    sockets = ''.join(f'  - {{fileID: {4001 + index * 10}}}\n' for index in range(len(sites)))
    out += script(1004, 1000, 'Gameplay/LevelPlanetDefense', f'''  levelManager: {{fileID: 1002}}
  planet: {{fileID: 2001}}
  spawner: {{fileID: 1006}}
  slots: 12
  radius: 3.3
  socketMounts:
{sockets}''')
    out += script(1005, 1000, 'EnemyAI/WaveDirectorBridge', f'  level: {ref(DATA_FOLDER + "Levels/UnionPrime.asset")}\n  seed: -1\n  excludeLevelBossFromRoster: 1\n  autoCountSpawns: 1\n  useLearning: 1\n  useRules: 1\n  keepLearningAcrossLevels: 1\n')
    out += script(1006, 1000, 'Unique Enemies/EnemySpawner', f'''  level: {ref(DATA_FOLDER + 'Levels/UnionPrime.asset')}
  bridge: {{fileID: 1005}}
  planetDefense: {{fileID: 1004}}
  pool: {{fileID: 1003}}
  spawnDistance: 2.4
  spawnInterval: 1
  startOnAwake: 0
  externallyControlled: 1
  prewarmPerEnemy: 5
''')
    towerrefs = ''.join('  - ' + ref(DATA_FOLDER + 'Towers/README/' + name + '.asset') + '\n' for name in ['Laser', 'Bomb', 'Sniper', 'Pest Trap'])
    out += script(1007, 1000, 'Gameplay/TowerPlacementInput', f'''  defense: {{fileID: 1004}}
  cam: {{fileID: 1695506653}}
  minClickRadius: 0.7
  maxClickRadius: 1.3
  towers:
{towerrefs}''')
    out += script(1008, 1000, 'Gameplay/GameplayLoopController', f'''  config: {ref(DATA_FOLDER + 'Levels/UnionPrimeGameplay.asset')}
  levelManager: {{fileID: 1002}}
  defense: {{fileID: 1004}}
  spawner: {{fileID: 1006}}
  placement: {{fileID: 1007}}
  battleCamera: {{fileID: 1695506653}}
''')
    portraits = ['Planets/UnionPrime/LaserPortrait.png', 'Towers/Bomb/BombBase.png', 'Towers/Sniper/SniperBase.png', 'Towers/PestTrap/PestTrapBase.png']
    out += script(1009, 1000, 'Gameplay/GameplayHUD', '  loop: {fileID: 1008}\n  font: ' + ref('Assets/UI/Fonts/PixelOperator-Bold.ttf', 12800000, 3)
                  + '\n  confirmSound: ' + ref('Assets/Audio/Menu/confirmation_001.ogg', 8300000, 3)
                  + '\n  towerPortraits:\n' + ''.join('  - ' + ref('Assets/Art/' + p, 21300000, 3) + '\n' for p in portraits))
    # Approved image positions are in a 1254px canvas, using 100 pixels/world unit.
    visual = read('Assets/UI/Planets/UnionPrimeVisual.prefab')
    facade_data = re.findall(r'  - sprite: \{fileID: (\d+), guid: (\w+), type: 3\}\n    origin: \{x: ([\d.]+), y: ([\d.]+)\}', visual)
    layers = [('RearGround', 0, None), *[(f'Facade{i}', i + 1, (float(x), float(y))) for i, (_, _, x, y) in enumerate(facade_data)], ('Terrain', 20, None), ('Foreground', 35, None)]
    children = [3001 + n * 10 for n in range(len(layers))] + [4001 + n * 10 for n in range(len(sites))]
    out += gameobject(2000, 2001, 'Union Prime Planet', [2002], children=children)
    rb = next(b for c, _, b in docs(read(TOWER_FOLDER + 'Laser.prefab')) if c == 50)
    rb = setfield(rb, 'm_GameObject', '{fileID: 2000}')
    out += doc(50, 2002, rb)
    sprite_template = next(b for c, _, b in docs(read('Assets/Prefabs/Enemies/BasicGoose1Visual.prefab')) if c == 212)
    for n, (name, order, origin) in enumerate(layers):
        go, tr, sr = 3000 + n * 10, 3001 + n * 10, 3002 + n * 10
        image = 'Assets/Art/Planets/UnionPrime/' + name + '.png'
        width, height = Image.open(ROOT / image).size
        x, y = origin if origin is not None else (0, 0)
        position = ((x + width / 2 - 627) / 100, (627 - y - height / 2) / 100, 0)
        out += gameobject(go, tr, name, [sr], parent=2001, position=position)
        body = setfield(sprite_template, 'm_GameObject', f'{{fileID: {go}}}')
        body = setfield(setfield(body, 'm_Sprite', ref(image, 21300000, 3)), 'm_SortingOrder', str(order))
        out += doc(212, sr, body)
    for n, site in enumerate(sites):
        x, y = site['study_mount']
        out += gameobject(4000 + n * 10, 4001 + n * 10, 'Socket ' + str(site['clock_site']), parent=2001,
                          position=((x - 627) / 100, (627 - y) / 100, 0), rotation=-site['study_angle'])
    out += doc(1660057539, 9223372036854775807, 'SceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 1695506654}\n  - {fileID: 1001}\n  - {fileID: 2001}\n')
    write(scene_path, out)


if __name__ == '__main__':
    for name in ['LevelGameplayConfig', 'GameplayLoopController', 'GameplayHUD', 'CombatVisual']:
        guid('Assets/Scripts/Gameplay/' + name + '.cs')
    prepare_wrappers()
    prepare_config()
    prepare_scene()
    print('Prepared 9 towers, 14 enemies, 10 projectiles, 17 effects and the Union Prime scene/configuration.')
