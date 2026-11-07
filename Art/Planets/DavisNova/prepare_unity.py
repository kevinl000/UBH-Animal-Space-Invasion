"""Import the current Davis Nova layers and update its existing visual prefab.

No scene, shared HUD, gameplay script or existing asset GUID is changed.
The prepared tower layer uses the same free Laser sandbox as Union Prime.
"""
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import uuid
import sys

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT/'Art/Planets'))
from visual_test_prefab import attach_placement

DEST = ROOT/'Assets/Art/Planets/DavisNova'
PREFAB = ROOT/'Assets/UI/Planets/DavisNovaVisual.prefab'
DISPLAY_SIZE = 600
ROOT_RECT = 3982591796417592770
SUN_IDS = (9096592105093771813, 9040941393803622090, 7374539970062387826, 2579115279243265403)
RING_IDS = (6707851231776259181, 2401011163957701538, 6672533100328355242, 4372849611214715009)
PREVIEW_ID = 900000000000010000
FRONT_ID = 900000000000020000


def guid(path):
    return re.search(r'^guid: ([a-f0-9]+)$', Path(str(path)+'.meta').read_text(), re.M)[1]


def field(block, name, value):
    result, count = re.subn(r'^  '+name+r':.*$', '  '+name+': '+value, block, flags=re.M)
    assert count == 1, name
    return result


def children(block, ids):
    value = '\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ids) if ids else ' []\n'
    return re.sub(r'  m_Children:.*?(?=  m_Father:)', '  m_Children:'+value, block, flags=re.S)


def main():
    layout = json.loads((HERE/'socket-layout.json').read_text())
    assert len(layout['sites']) == 12
    DEST.mkdir(exist_ok=True)
    folder_meta = Path(str(DEST)+'.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', newline='\n')
    template = (ROOT/'Assets/Art/Planets/UnionPrime/Terrain.png.meta').read_text()
    template = re.sub(r'[ \t]+$', '', template, flags=re.M)
    images = {}
    for source_name, name in (('sun', 'Sun'), ('ring-background', 'Ring'), ('socket-fronts', 'SocketFronts')):
        source = HERE/'Pixel'/f'{source_name}.png'
        assert Image.open(source).size == (1254, 1254)
        target = DEST/f'{name}.png'
        shutil.copyfile(source, target)
        meta = Path(str(target)+'.meta')
        if not meta.exists():
            meta.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex, template, flags=re.M), newline='\n')
        images[name] = dict(path=target.relative_to(ROOT).as_posix(), guid=guid(target),
                            source=source.relative_to(ROOT).as_posix(),
                            sha256=hashlib.sha256(source.read_bytes()).hexdigest())

    content = PREFAB.read_text()
    blocks = {int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)', content, re.M|re.S)}
    # Reuse the prefab's Image serialization and retain its existing Sun/Ring IDs.
    templates = [blocks[i] for i in RING_IDS]

    def node(name, ids, parent, size=(0, 0), position=(0, 0), pivot=(.5, .5),
             angle=0, scale=1, sprite=None, child_ids=(), active=True):
        mapping = dict(zip(RING_IDS, ids))
        parts = [re.sub(r'(?<=&)(\d+)|(?<=fileID: )(\d+)',
                        lambda m: str(mapping.get(int(m[0]), int(m[0]))), part) for part in templates]
        parts[0] = field(parts[0], 'm_Name', name)
        parts[0] = field(parts[0], 'm_IsActive', str(int(active)))
        if sprite is None:
            parts[0] = re.sub(r'  - component:.*\n', '', parts[0])
            parts[0] = parts[0].replace('  m_Component:\n', f'  m_Component:\n  - component: {{fileID: {ids[1]}}}\n')
        rect = parts[1]
        rect = field(rect, 'm_Father', f'{{fileID: {parent}}}')
        rect = field(rect, 'm_SizeDelta', f'{{x: {size[0]}, y: {size[1]}}}')
        rect = field(rect, 'm_AnchoredPosition', f'{{x: {position[0]}, y: {position[1]}}}')
        rect = field(rect, 'm_Pivot', f'{{x: {pivot[0]}, y: {pivot[1]}}}')
        rect = field(rect, 'm_LocalScale', f'{{x: {scale}, y: {scale}, z: 1}}')
        rect = field(rect, 'm_LocalRotation', f'{{x: 0, y: 0, z: {math.sin(math.radians(angle)/2)}, w: {math.cos(math.radians(angle)/2)}}}')
        rect = field(rect, 'm_LocalEulerAnglesHint', f'{{x: 0, y: 0, z: {angle}}}')
        parts[1] = children(rect, child_ids)
        if sprite is not None:
            parts[3] = field(parts[3], 'm_Sprite', f'{{fileID: 21300000, guid: {sprite}, type: 3}}')
            parts[3] = field(parts[3], 'm_Color', '{r: 1, g: 1, b: 1, a: 1}')
            parts[3] = field(parts[3], 'm_RaycastTarget', '0')
        return parts if sprite is not None else parts[:2]

    # Only replace objects owned by this importer; preserve any unrelated objects.
    owned = set(SUN_IDS+RING_IDS)
    owned.update(i for i in blocks if PREVIEW_ID <= i <= FRONT_ID+3)
    keep = [field(blocks[i], 'm_Name', 'DavisNovaVisual') if i == 4790782634746517014 else blocks[i]
            for i in blocks if i not in owned]
    for i, part in enumerate(keep):
        if part.startswith(f'--- !u!224 &{ROOT_RECT}\n'):
            keep[i] = children(part, [SUN_IDS[1], RING_IDS[1], PREVIEW_ID+1, FRONT_ID+1])
    parts = keep
    parts += node('Sun', SUN_IDS, ROOT_RECT, (DISPLAY_SIZE, DISPLAY_SIZE), sprite=images['Sun']['guid'])
    parts += node('Ring', RING_IDS, ROOT_RECT, (DISPLAY_SIZE, DISPLAY_SIZE), sprite=images['Ring']['guid'])

    def ids(start):
        return tuple(range(start, start+4))

    parts += node('PlacedLasers', ids(PREVIEW_ID), ROOT_RECT, (1254, 1254),
                  scale=DISPLAY_SIZE/1254)
    laser_folder = ROOT/'Assets/Art/Planets/UnionPrime'
    base_path, gun_path = laser_folder/'LaserBase.png', laser_folder/'LaserGun.png'
    parts += node('SocketFronts', ids(FRONT_ID), ROOT_RECT, (DISPLAY_SIZE, DISPLAY_SIZE),
                  sprite=images['SocketFronts']['guid'])
    prefab_text = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(parts)
    prefab_text = attach_placement(prefab_text, 4790782634746517014, PREVIEW_ID+1,
                                   (1254, 1254), layout['sites'])
    PREFAB.write_text(re.sub(r'[ \t]+$', '', prefab_text, flags=re.M), encoding='utf-8', newline='\n')
    manifest = dict(editor='6000.6.7f1', images=images, visual_prefab=PREFAB.relative_to(ROOT).as_posix(),
                    site_count=12, source_canvas_display_width=DISPLAY_SIZE, tower_scale=2,
                    shared_tower_assets=[base_path.relative_to(ROOT).as_posix(), gun_path.relative_to(ROOT).as_posix()],
                    free_laser_placement=True, source_layout='socket-layout.json',
                    limits='Free Laser placement and aiming; no combat or resource spending. See UNITY_TESTING.md for checks.')
    (HERE/'unity-import.json').write_text(json.dumps(manifest, indent=2)+'\n')
    print('Prepared Sun, Ring, SocketFronts and twelve interactive Laser sites in DavisNovaVisual.prefab.')


if __name__ == '__main__':
    main()
