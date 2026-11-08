"""Import Commons sprites and update only its existing visual prefab."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import uuid

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT/'Art/Planets'))
from visual_test_prefab import attach_placement

DEST = ROOT/'Assets/Art/Planets/CommonsMinor'
PREFAB = ROOT/'Assets/UI/Planets/CommonsMinorVisual.prefab'
BODY_IDS = (2945820775897371873, 2275033562023057916, 3566527239471902834, 5563255122975793516)
ROOT_OBJECT = 5353438425906219901
ROOT_RECT = 151243226318504134
TOWER_IDS = tuple(range(900000000000010000, 900000000000010004))
FRONT_IDS = tuple(range(900000000000020000, 900000000000020004))


def field(block, name, value):
    result, count = re.subn(r'^  '+name+r':.*$', '  '+name+': '+value, block, flags=re.M)
    assert count == 1, name
    return result


def main():
    layout = json.loads((HERE/'FittedSites/assembly.json').read_text())
    assert len(layout['sites']) == 8
    DEST.mkdir(exist_ok=True)
    folder_meta = Path(str(DEST)+'.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', newline='\n')
    template = (ROOT/'Assets/Art/Planets/UnionPrime/Terrain.png.meta').read_text()
    images = {}
    for source_name, name in [('terrain-background', 'Terrain'), ('socket-fronts', 'SocketFronts')]:
        source = HERE/'FittedSites'/f'{source_name}.png'
        target = DEST/f'{name}.png'
        shutil.copyfile(source, target)
        meta = Path(str(target)+'.meta')
        if not meta.exists():
            meta.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex, template, flags=re.M), newline='\n')
        guid = re.search(r'^guid: (\w+)$', meta.read_text(), re.M)[1]
        images[name] = dict(guid=guid, path=target.relative_to(ROOT).as_posix(),
                            source=source.relative_to(ROOT).as_posix(),
                            sha256=hashlib.sha256(source.read_bytes()).hexdigest())
    content = PREFAB.read_text()
    blocks = {int(m[1]): m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)', content, re.M|re.S)}
    templates = [blocks[i] for i in BODY_IDS]

    def node(name, ids, sprite=None):
        mapping = dict(zip(BODY_IDS, ids))
        parts = [re.sub(r'(?<=&)(\d+)|(?<=fileID: )(\d+)',
            lambda m: str(mapping.get(int(m[0]), int(m[0]))), p) for p in templates]
        parts[0] = field(parts[0], 'm_Name', name)
        display_size = layout['canvas_display_width']
        parts[1] = field(parts[1], 'm_SizeDelta', f'{{x: {display_size}, y: {display_size}}}' if sprite else '{x: 1254, y: 1254}')
        scale = display_size/1254 if sprite is None else 1
        parts[1] = field(parts[1], 'm_LocalScale', f'{{x: {scale}, y: {scale}, z: 1}}')
        if sprite:
            parts[3] = field(parts[3], 'm_Color', '{r: 1, g: 1, b: 1, a: 1}')
            parts[3] = field(parts[3], 'm_Sprite', f'{{fileID: 21300000, guid: {sprite}, type: 3}}')
        else:
            parts[0] = re.sub(r'  - component:.*\n', '', parts[0])
            parts[0] = parts[0].replace('  m_Component:\n', f'  m_Component:\n  - component: {{fileID: {ids[1]}}}\n')
            parts = parts[:2]
        return parts

    owned = set(BODY_IDS+TOWER_IDS+FRONT_IDS)
    parts = [blocks[i] for i in blocks if i not in owned]
    for i, block in enumerate(parts):
        if block.startswith(f'--- !u!224 &{ROOT_RECT}\n'):
            children = ''.join(f'  - {{fileID: {n}}}\n' for n in (BODY_IDS[1], TOWER_IDS[1], FRONT_IDS[1]))
            parts[i] = re.sub(r'  m_Children:.*?(?=  m_Father:)', '  m_Children:\n'+children, block, flags=re.S)
    parts += node('Terrain', BODY_IDS, images['Terrain']['guid'])
    parts += node('PlacedLasers', TOWER_IDS)
    parts += node('SocketFronts', FRONT_IDS, images['SocketFronts']['guid'])
    content = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(parts)
    content = attach_placement(content, ROOT_OBJECT, TOWER_IDS[1], (1254, 1254), layout['sites'])
    # The shared helper defaults to the larger mechanical ring's tower scale.
    content = content.replace('  towerScale: 2\n', f'  towerScale: {layout["tower_scale"]}\n')
    PREFAB.write_text(re.sub(r'[ \t]+$', '', content, flags=re.M), encoding='utf-8', newline='\n')
    (HERE/'unity-import.json').write_text(json.dumps(dict(images=images,
        visual_prefab=PREFAB.relative_to(ROOT).as_posix(), source_size=[1254, 1254],
        canvas_display_width=layout['canvas_display_width'], tower_scale=layout['tower_scale'], site_count=8,
        free_laser_placement=True, limits='Visual sandbox only; no combat, economy, or gameplay scene changes.'), indent=2)+'\n')
    print('Prepared Commons terrain, foundation lips and eight interactive Laser sites.')


if __name__ == '__main__':
    main()
