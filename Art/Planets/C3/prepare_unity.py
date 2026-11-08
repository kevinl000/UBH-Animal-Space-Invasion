"""Import approved C3 sprite layers and update only its existing visual prefab."""
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import sys
import uuid

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent))
from visual_test_prefab import attach_placement, COMPONENT_ID, guid

DEST = ROOT / 'Assets/Art/Planets/C3'
PREFAB = ROOT / 'Assets/UI/Planets/C3Visual.prefab'
MOTION = ROOT / 'Assets/Scripts/Menu/Planets/C3VisualMotion.cs'
ROOT_OBJECT, ROOT_RECT = 7215522090604316617, 8870620821805468365
BODY_IDS = [
    (9028888231713850477, 6125100075016020511, 8945376063487168231, 888810000000000001),
    (1324806755046577252, 5004117112630606110, 5146689539037818787, 888810000000000002),
    (6808364051548384019, 4370412023205013800, 6494308227290963592, 888810000000000003)]
DISPLAY_WIDTH = 240
INITIAL_REVIEW_ORBIT_RADIUS = 500
REVIEW_SPACING_RATIO = .7524


def meta(path, importer):
    path = Path(str(path)+'.meta')
    if not path.exists():
        path.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+importer, newline='\n')


def main():
    layout = json.loads((HERE / 'FittedSites/assembly.json').read_text(encoding='utf-8'))
    assert layout['planet_count'] == 3 and layout['total_sockets'] == 18
    source_size = layout['source_size'][0]
    scale = DISPLAY_WIDTH/source_size
    orbit_radius = INITIAL_REVIEW_ORBIT_RADIUS*REVIEW_SPACING_RATIO*scale/layout['preview']['planet_scale']
    DEST.mkdir(exist_ok=True)
    meta(DEST, 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    meta(MOTION, 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    template = (ROOT / 'Assets/Art/Planets/UnionPrime/Terrain.png.meta').read_text()
    imports = {}
    for planet in layout['planets']:
        theme = planet['theme']
        for suffix, title in [('terrain-background', 'Terrain'), ('socket-fronts', 'SocketFronts')]:
            source = HERE / f'FittedSites/{theme}-{suffix}.png'
            target = DEST / f'{theme.capitalize()}{title}.png'
            shutil.copyfile(source, target)
            target_meta = Path(str(target)+'.meta')
            if not target_meta.exists():
                target_meta.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex, template, flags=re.M), newline='\n')
            imports[theme+title] = dict(guid=guid(target), source=source.relative_to(ROOT).as_posix(),
                                       target=target.relative_to(ROOT).as_posix(),
                                       sha256=hashlib.sha256(source.read_bytes()).hexdigest())

    original = PREFAB.read_text()
    blocks = {int(m[1]): m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)', original, re.M|re.S)}
    # Preserve the prefab root IDs and the three body object/transform IDs.
    root_object, root_rect = blocks[ROOT_OBJECT], blocks[ROOT_RECT]
    root_object = re.sub(r'  m_Component:.*?(?=  m_Layer:)',
                         f'  m_Component:\n  - component: {{fileID: {ROOT_RECT}}}\n'
                         '  - component: {fileID: 888810000000000000}\n', root_object, flags=re.S)
    root_rect = re.sub(r'  m_Children:.*?(?=  m_Father:)',
                       '  m_Children:\n'+''.join(f'  - {{fileID: {ids[1]}}}\n' for ids in BODY_IDS), root_rect, flags=re.S)
    body_template = blocks[BODY_IDS[0][0]]
    rect_template = blocks[BODY_IDS[0][1]]
    renderer_template = blocks[BODY_IDS[0][2]]
    image_template = next(b for b in blocks.values() if 'UnityEngine.UI::UnityEngine.UI.Image' in b)
    image_id = int(re.search(r'^--- !u!114 &(\d+)', image_template)[1])
    template_ids = (BODY_IDS[0][0], BODY_IDS[0][1], BODY_IDS[0][2], image_id)

    def field(block, key, value):
        block, count = re.subn(r'^  '+key+r':.*$', '  '+key+': '+value, block, flags=re.M)
        assert count == 1, key
        return block

    def node(title, ids, parent, position=(0, 0), local_scale=1, pivot=(.5, .5), sprite=None, children=()):
        mapping = dict(zip(template_ids, ids))
        parts = [re.sub(r'(?<=&)(\d+)|(?<=fileID: )(\d+)',
                        lambda m: str(mapping.get(int(m[0]), int(m[0]))), block)
                 for block in (body_template, rect_template, renderer_template, image_template)]
        parts[0] = field(parts[0], 'm_Name', title)
        refs = ids if sprite is not None else ids[:2]
        parts[0] = re.sub(r'  m_Component:.*?(?=  m_Layer:)', '  m_Component:\n'
                         +''.join(f'  - component: {{fileID: {n}}}\n' for n in refs[1:]), parts[0], flags=re.S)
        for key, value in [('m_Father', f'{{fileID: {parent}}}'),
                           ('m_SizeDelta', f'{{x: {source_size}, y: {source_size}}}'),
                           ('m_AnchoredPosition', f'{{x: {position[0]}, y: {position[1]}}}'),
                           ('m_LocalScale', f'{{x: {local_scale}, y: {local_scale}, z: 1}}'),
                           ('m_Pivot', f'{{x: {pivot[0]}, y: {pivot[1]}}}'),
                           ('m_LocalRotation', '{x: 0, y: 0, z: 0, w: 1}'),
                           ('m_LocalEulerAnglesHint', '{x: 0, y: 0, z: 0}')]:
            parts[1] = field(parts[1], key, value)
        parts[1] = re.sub(r'  m_Children:.*?(?=  m_Father:)',
                         '  m_Children:' + ('\n'+''.join(f'  - {{fileID: {n}}}\n' for n in children) if children else ' []\n'),
                         parts[1], flags=re.S)
        if sprite is None:
            return ''.join(parts[:2])
        parts[3] = field(parts[3], 'm_Sprite', f'{{fileID: 21300000, guid: {sprite}, type: 3}}')
        parts[3] = field(parts[3], 'm_GameObject', f'{{fileID: {ids[0]}}}')
        parts[3] = field(parts[3], 'm_Color', '{r: 1, g: 1, b: 1, a: 1}')
        parts[3] = field(parts[3], 'm_RaycastTarget', '0')
        return ''.join(parts)

    content = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+root_object+root_rect
    for i, planet in enumerate(layout['planets']):
        theme, ids = planet['theme'], BODY_IDS[i]
        towers = tuple(range(888810000000000010+i*20, 888810000000000014+i*20))
        front = tuple(range(888810000000000020+i*20, 888810000000000024+i*20))
        a = math.radians(90-i*120)
        cx, cy = layout['center']
        body = node(theme.capitalize(), ids, ROOT_RECT,
                    position=(orbit_radius*math.cos(a), orbit_radius*math.sin(a)), local_scale=scale,
                    pivot=(cx/source_size, 1-cy/source_size), sprite=imports[theme+'Terrain']['guid'],
                    children=(towers[1], front[1]))
        body += node('PlacedLasers', towers, ids[1])
        body += node('SocketFronts', front, ids[1], sprite=imports[theme+'SocketFronts']['guid'])
        sites = [dict(site=s['site'], mount=s['mount'], angle=s['clockwise_angle']) for s in planet['sites']]
        body = attach_placement(body, ids[0], towers[1], (source_size, source_size), sites)
        body = body.replace(str(COMPONENT_ID), str(COMPONENT_ID+i))
        body = body.replace('  towerScale: 2\n', f'  towerScale: {layout["tower_scale"]}\n')
        content += body
    content += f'''--- !u!114 &888810000000000000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ROOT_OBJECT}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(MOTION)}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: PlanetaryDefense.Menu::C3VisualMotion
  planets:
'''+''.join(f'  - {{fileID: {ids[1]}}}\n' for ids in BODY_IDS)+f'''  orbitRadius: {orbit_radius}
  orbitSpeed: 4
  spinSpeed: 10
'''
    PREFAB.write_text(re.sub(r'[ \t]+$', '', content, flags=re.M), encoding='utf-8', newline='\n')
    manifest = dict(status='Approved C3 artwork integrated into existing visual prefab.',
                    images=imports, prefab=PREFAB.relative_to(ROOT).as_posix(),
                    canvas_width_per_planet=DISPLAY_WIDTH, tower_scale=layout['tower_scale'],
                    tower_base_display_size=[70*scale*layout['tower_scale'], 72*scale*layout['tower_scale']],
                    orbit_radius=orbit_radius, spacing_relative_to_approved_review=REVIEW_SPACING_RATIO,
                    clockwise_orbit_degrees_per_second=4, clockwise_spin_degrees_per_second=10,
                    socket_count=18, note='Visual/free Laser sandbox only; no gameplay balance or shared scene edits.')
    (HERE / 'unity-import.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print(f'Imported six sprites; three independent bodies, 18 sockets, orbit radius {orbit_radius:.3f}.')


if __name__ == '__main__':
    main()
