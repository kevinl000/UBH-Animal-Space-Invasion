"""Import the revised library planet and the unchanged approved moon, separately."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import uuid

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT/'Art/Planets'))
from visual_test_prefab import attach_placement, COMPONENT_ID, guid

DEST = ROOT/'Assets/Art/Planets/SilvermanLuna'
PREFAB = ROOT/'Assets/UI/Planets/SilvermanLunaVisual.prefab'
ROOT_OBJECT, ROOT_RECT = 355122523979535855, 1314540646232830326
BODY = (3220419004566026065, 5237771976676920125, 114468644595808892, 8726233819178399825)
MOON = (9180515518009259507, 1635646746826403067, 1749815475775758768, 129407172295127125)
TOWERS = tuple(range(900000000000050000, 900000000000050004))
FRONT = tuple(range(900000000000050010, 900000000000050014))
MOON_TOWERS = tuple(range(900000000000050020, 900000000000050024))
MOON_FRONT = tuple(range(900000000000050030, 900000000000050034))
GLOBE = tuple(range(900000000000050040, 900000000000050044))
MOON_COMPONENT = COMPONENT_ID+1


def field(block, name, value):
    block, count = re.subn(r'^  '+name+r':.*$', '  '+name+': '+value, block, flags=re.M)
    assert count == 1, name
    return block


def children(block, ids):
    value = '\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ids) if ids else ' []\n'
    return re.sub(r'  m_Children:.*?(?=  m_Father:)', '  m_Children:'+value, block, flags=re.S)


def main():
    layout = json.loads((HERE/'FittedSites/assembly.json').read_text())
    moon_dir = HERE.parent/'Moon/FittedSites'
    moon = json.loads((moon_dir/'assembly.json').read_text())
    assert len(layout['sites']) == 12 and len(moon['sites']) == 4
    assert layout['tower_scale'] == 1.25
    DEST.mkdir(exist_ok=True)
    folder_meta = Path(str(DEST)+'.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', newline='\n')
    template = (ROOT/'Assets/Art/Planets/UnionPrime/Terrain.png.meta').read_text()
    images = {}
    for name, source in [('Globe', HERE/'FittedSites/globe.png'),
                         ('Terrain', HERE/'FittedSites/terrain-background.png'),
                         ('SocketFronts', HERE/'FittedSites/socket-fronts.png'),
                         ('MoonTerrain', moon_dir/'terrain-background.png'),
                         ('MoonSocketFronts', moon_dir/'socket-fronts.png')]:
        assert Image.open(source).size == (1254, 1254)
        target = DEST/f'{name}.png'
        shutil.copyfile(source, target)
        meta = Path(str(target)+'.meta')
        if not meta.exists():
            meta.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex, template, flags=re.M), newline='\n')
        images[name] = dict(guid=guid(target), source=source.relative_to(ROOT).as_posix(),
                            sha256=hashlib.sha256(source.read_bytes()).hexdigest())

    content = PREFAB.read_text()
    blocks = {int(m[1]): m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)', content, re.M|re.S)}
    templates = [blocks[i] for i in BODY]

    def node(name, ids, parent, size, position=(0, 0), scale=1, sprite=None, child_ids=()):
        mapping = dict(zip(BODY, ids))
        parts = [re.sub(r'(?<=&)(\d+)|(?<=fileID: )(\d+)',
                        lambda m: str(mapping.get(int(m[0]), int(m[0]))), part) for part in templates]
        parts[0] = field(parts[0], 'm_Name', name)
        parts[0] = field(parts[0], 'm_IsActive', '1')
        # The Body template can contain a placement component after a previous import.
        parts[0] = re.sub(r'^  - component:.*\n', '', parts[0], flags=re.M)
        refs = (ids[1],) if sprite is None else (ids[1], ids[2], ids[3])
        parts[0] = parts[0].replace('  m_Component:\n', '  m_Component:\n'+''.join(f'  - component: {{fileID: {n}}}\n' for n in refs))
        rect = parts[1]
        for key, value in [('m_Father', f'{{fileID: {parent}}}'),
                           ('m_SizeDelta', f'{{x: {size[0]}, y: {size[1]}}}'),
                           ('m_AnchoredPosition', f'{{x: {position[0]}, y: {position[1]}}}'),
                           ('m_LocalScale', f'{{x: {scale}, y: {scale}, z: 1}}'),
                           ('m_LocalRotation', '{x: 0, y: 0, z: 0, w: 1}'),
                           ('m_LocalEulerAnglesHint', '{x: 0, y: 0, z: 0}')]:
            rect = field(rect, key, value)
        parts[1] = children(rect, child_ids)
        if sprite is None:
            return ''.join(parts[:2])
        parts[3] = field(parts[3], 'm_Sprite', f'{{fileID: 21300000, guid: {sprite}, type: 3}}')
        parts[3] = field(parts[3], 'm_Color', '{r: 1, g: 1, b: 1, a: 1}')
        parts[3] = field(parts[3], 'm_RaycastTarget', '0')
        return ''.join(parts)

    owned = set(GLOBE+BODY+MOON+TOWERS+FRONT+MOON_TOWERS+MOON_FRONT+(COMPONENT_ID, MOON_COMPONENT))
    kept = []
    for identifier, block in blocks.items():
        if identifier in owned:
            continue
        if identifier == ROOT_OBJECT:
            block = block.replace(f'  - component: {{fileID: {COMPONENT_ID}}}\n', '')
        if identifier == ROOT_RECT:
            block = children(block, [GLOBE[1], BODY[1], TOWERS[1], FRONT[1], MOON[1]])
        kept.append(block)
    content = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(kept)
    content += node('Globe', GLOBE, ROOT_RECT, (600, 600), sprite=images['Globe']['guid'])
    content += node('Body', BODY, ROOT_RECT, (600, 600), sprite=images['Terrain']['guid'])
    content += node('PlacedLasers', TOWERS, ROOT_RECT, (1254, 1254), scale=600/1254)
    content += node('SocketFronts', FRONT, ROOT_RECT, (600, 600), sprite=images['SocketFronts']['guid'])
    content = attach_placement(content, ROOT_OBJECT, TOWERS[1], (1254, 1254), layout['sites'])
    content = content.replace('  towerScale: 2\n', '  towerScale: 1.25\n')
    moon_content = node('Moon', MOON, ROOT_RECT, (180, 180), position=(385, 190),
                        sprite=images['MoonTerrain']['guid'], child_ids=[MOON_TOWERS[1], MOON_FRONT[1]])
    moon_content += node('PlacedLasers', MOON_TOWERS, MOON[1], (1254, 1254), scale=180/1254)
    moon_content += node('SocketFronts', MOON_FRONT, MOON[1], (180, 180), sprite=images['MoonSocketFronts']['guid'])
    moon_content = attach_placement(moon_content, MOON[0], MOON_TOWERS[1], (1254, 1254), moon['sites'])
    moon_content = moon_content.replace(str(COMPONENT_ID), str(MOON_COMPONENT))
    moon_content = moon_content.replace('  towerScale: 2\n', f'  towerScale: {moon["tower_scale"]}\n')
    content += moon_content
    PREFAB.write_text(re.sub(r'[ \t]+$', '', content, flags=re.M), encoding='utf-8', newline='\n')
    manifest = dict(images=images, prefab=PREFAB.relative_to(ROOT).as_posix(),
                    main_sites=12, main_canvas_width=600, main_tower_scale=1.25,
                    moon_sites=4, moon_canvas_width=180, moon_position=[385, 190],
                    moon_tower_scale=moon['tower_scale'], orbit_and_rotation_configured=False,
                    note='Uses existing free Laser placement. No gameplay scripts or scene changes.')
    (HERE/'unity-import.json').write_text(json.dumps(manifest, indent=2)+'\n')
    print('Prepared main planet and independent approved moon, with 12 + 4 free Laser sites.')


if __name__ == '__main__':
    main()
