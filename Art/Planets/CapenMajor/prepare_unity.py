"""Export approved SVG layers without repainting sources; prepare the Unity sandbox.

Requires Pillow and Chrome. Run from any directory; --chrome overrides its path.
The existing SVG retains all curved foundations, masks and facade transforms.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import uuid
import xml.etree.ElementTree as ET

from PIL import Image, ImageChops

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT/'Art/Planets'))
from visual_test_prefab import attach_placement, guid

DEST = ROOT/'Assets/Art/Planets/CapenMajor'
PREFAB = ROOT/'Assets/UI/Planets/CapenMajorVisual.prefab'
SIZE = (1392, 1420)
DISPLAY_WIDTH = 600
ROOT_OBJECT, ROOT_RECT = 6561523609088438156, 6537155858197131333
BODY_IDS = (229140459621728174, 3202025291746200281, 6851297782449792523, 6801483279293362070)
FIRST_ID = 900000000000030000
TOWER_ID = FIRST_ID+800


def field(block, name, value):
    result, count = re.subn(r'^  '+name+r':.*$', '  '+name+': '+value, block, flags=re.M)
    assert count == 1, name
    return result


def children(block, ids):
    value = '\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ids) if ids else ' []\n'
    return re.sub(r'  m_Children:.*?(?=  m_Father:)', '  m_Children:'+value, block, flags=re.S)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--chrome', default='C:/Program Files/Google/Chrome/Application/chrome.exe')
    args = parser.parse_args()
    layout = json.loads((HERE/'assembly.json').read_text())
    assert len(layout['sites']) == 16
    assert layout['full_preview_view_box'] == [-80, -100, *SIZE]
    for source, expected in layout['source_sha256'].items():
        assert hashlib.sha256((ROOT/source).read_bytes()).hexdigest() == expected, source

    source_svg = (HERE/'planet-empty.svg').read_text(encoding='utf-8')
    tree = ET.fromstring(source_svg)
    ns = {'s': 'http://www.w3.org/2000/svg'}
    facade_group = tree.find(".//s:g[@id='placed-facades']", ns)
    assert len(facade_group) == 5
    layers = [(name, ET.tostring(element, encoding='unicode')) for name, element in zip(
        ['Administration', 'BusStation', 'FlagPlaza', 'TransitShelter', 'CargoDepot'], facade_group)]
    layers += [(name, f'<use href="#{ref}"/>') for name, ref in
               [('RearFloor', 'rear'), ('Terrain', 'terrain'), ('SteelFronts', 'fronts')]]
    # Use the original definitions verbatim to preserve renderer mask/filter behavior.
    prefix = source_svg[:source_svg.index('</defs>')+len('</defs>')]
    DEST.mkdir(exist_ok=True)
    folder_meta = Path(str(DEST)+'.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', newline='\n')
    template = (ROOT/'Assets/Art/Planets/UnionPrime/Terrain.png.meta').read_text()
    template = re.sub(r'[ \t]+$', '', template, flags=re.M)
    images = {}
    with tempfile.TemporaryDirectory(prefix='capen-unity-export-') as temp:
        work = Path(temp)
        for name, body in layers:
            svg = work/(name+'.svg')
            svg.write_text(prefix+body+'</svg>', encoding='utf-8')
            html = work/'render.html'
            html.write_text(f'<html style="background:transparent"><body style="margin:0;overflow:hidden"><img src="{svg.as_uri()}" width="{SIZE[0]}" height="{SIZE[1]}"></body></html>')
            target = DEST/(name+'.png')
            subprocess.run([args.chrome, '--headless', '--disable-gpu', '--no-sandbox',
                            '--disable-extensions', '--no-first-run', '--hide-scrollbars',
                            '--allow-file-access-from-files', '--default-background-color=00000000',
                            '--force-device-scale-factor=1', '--virtual-time-budget=3000',
                            f'--user-data-dir={work/"browser"}', f'--window-size={SIZE[0]},{SIZE[1]}',
                            f'--screenshot={target}', html.as_uri()], check=True,
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                           creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0)
            with Image.open(target) as image:
                assert image.size == SIZE and image.mode == 'RGBA'
                assert image.getchannel('A').getextrema() == (0, 255), name
            meta = Path(str(target)+'.meta')
            if not meta.exists():
                meta.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex, template, flags=re.M), newline='\n')
            images[name] = dict(path=target.relative_to(ROOT).as_posix(), guid=guid(target),
                                sha256=hashlib.sha256(target.read_bytes()).hexdigest())

    composed = Image.new('RGBA', SIZE)
    for name, _ in layers:
        composed.alpha_composite(Image.open(DEST/(name+'.png')).convert('RGBA'))
    reference = Image.open(HERE/'planet-empty.png').convert('RGBA')
    # Compare visible colors over both extremes; unassociated RGB at near-zero
    # alpha can differ greatly despite representing the same visible pixel.
    max_difference = 0
    for color in ('black', 'white'):
        background = Image.new('RGBA', SIZE, color)
        difference = ImageChops.difference(Image.alpha_composite(background, composed),
                                          Image.alpha_composite(background, reference))
        max_difference = max(max_difference, max(high for low, high in difference.getextrema()))
    assert max_difference <= 3, f'Layer export differs from the approved preview: {max_difference}'

    content = PREFAB.read_text()
    blocks = {int(m[1]): m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)', content, re.M|re.S)}
    templates = [blocks[i] for i in BODY_IDS]

    def node(name, start, sprite=None):
        ids = tuple(range(start, start+4)) if start != BODY_IDS[0] else BODY_IDS
        mapping = dict(zip(BODY_IDS, ids))
        parts = [re.sub(r'(?<=&)(\d+)|(?<=fileID: )(\d+)',
                        lambda m: str(mapping.get(int(m[0]), int(m[0]))), part) for part in templates]
        parts[0] = field(parts[0], 'm_Name', name)
        parts[1] = field(parts[1], 'm_SizeDelta', f'{{x: {SIZE[0]}, y: {SIZE[1]}}}')
        scale = DISPLAY_WIDTH/SIZE[0]
        parts[1] = field(parts[1], 'm_LocalScale', f'{{x: {scale}, y: {scale}, z: 1}}')
        if sprite is None:
            parts[0] = re.sub(r'  - component:.*\n', '', parts[0])
            parts[0] = parts[0].replace('  m_Component:\n', f'  m_Component:\n  - component: {{fileID: {ids[1]}}}\n')
            return parts[:2]
        parts[3] = field(parts[3], 'm_Sprite', f'{{fileID: 21300000, guid: {sprite}, type: 3}}')
        parts[3] = field(parts[3], 'm_Color', '{r: 1, g: 1, b: 1, a: 1}')
        return parts

    # Retain root and original Body IDs; replace only exporter-owned nodes.
    owned = set(BODY_IDS) | {i for i in blocks if FIRST_ID <= i < FIRST_ID+1000}
    parts = [blocks[i] for i in blocks if i not in owned]
    child_ids = []
    for index, (name, _) in enumerate(layers):
        if name == 'SteelFronts':
            child_ids.append(TOWER_ID+1)
            parts += node('PlacedLasers', TOWER_ID)
        start = BODY_IDS[0] if name == 'Terrain' else FIRST_ID+index*10
        child_ids.append(BODY_IDS[1] if name == 'Terrain' else start+1)
        parts += node('Body' if name == 'Terrain' else name, start, images[name]['guid'])
    for index, part in enumerate(parts):
        if part.startswith(f'--- !u!224 &{ROOT_RECT}\n'):
            parts[index] = children(part, child_ids)
    content = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(parts)
    content = attach_placement(content, ROOT_OBJECT, TOWER_ID+1, SIZE, layout['sites'], (80, 100), 'recess')
    content, count = re.subn(r'^  towerScale: .*$', f'  towerScale: {layout["tower_scale"]}', content, flags=re.M)
    assert count == 1
    PREFAB.write_text(re.sub(r'[ \t]+$', '', content, flags=re.M), encoding='utf-8', newline='\n')
    manifest = dict(editor='6000.6.7f1', images=images, source='planet-empty.svg',
                    source_sha256=hashlib.sha256(source_svg.encode('utf-8')).hexdigest(),
                    source_view_box=layout['full_preview_view_box'], source_canvas_size=SIZE,
                    display_width=DISPLAY_WIDTH, site_count=len(layout['sites']), tower_scale=layout['tower_scale'],
                    maximum_visible_channel_difference_from_approved_preview=max_difference,
                    free_laser_placement=True, visual_prefab=PREFAB.relative_to(ROOT).as_posix())
    (HERE/'unity-import.json').write_text(json.dumps(manifest, indent=2)+'\n', newline='\n')
    print(f'Prepared eight Capen sprite layers and sixteen Laser sites; preview difference {max_difference}.')


if __name__ == '__main__':
    main()
