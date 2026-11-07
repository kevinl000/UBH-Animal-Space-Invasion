"""Serialize the existing free Laser sandbox onto prepared planet sprite layers."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
COMPONENT_ID = 746212930054721001


def guid(path):
    return re.search(r'^guid: ([a-f0-9]+)$', Path(str(path)+'.meta').read_text(), re.M)[1]


def attach_placement(content, root_object, tower_layer, size, sites, offset=(0, 0), label='socket'):
    component_ref = f'  - component: {{fileID: {COMPONENT_ID}}}\n'
    pattern = rf'(^--- !u!1 &{root_object}\n.*?)(?=^--- !u!|\Z)'
    def attach(match):
        block = match[0].replace(component_ref, '')
        return re.sub(r'(  m_Component:\n  - component:.*\n)',
                      lambda m: m[0]+component_ref, block, count=1)
    content, count = re.subn(pattern, attach, content, flags=re.M|re.S)
    assert count == 1
    content = re.sub(rf'^--- !u!114 &{COMPONENT_ID}\n.*?(?=^--- !u!|\Z)', '', content, flags=re.M|re.S)
    script = ROOT/'Assets/Scripts/Menu/Planets/UnionPrimeVisualTest.cs'
    shared = ROOT/'Assets/Art/Planets/UnionPrime'
    component = f'''--- !u!114 &{COMPONENT_ID}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_object}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: PlanetaryDefense.Menu::UnionPrimeVisualTest
  silhouette: {{fileID: 0}}
  rearGround: {{fileID: 0}}
  terrain: {{fileID: 0}}
  foreground: {{fileID: 0}}
'''
    for field, name in [('laserBase', 'LaserBase'), ('laserGun', 'LaserGun'), ('laserPortrait', 'LaserPortrait')]:
        component += f'  {field}: {{fileID: 21300000, guid: {guid(shared/(name+".png"))}, type: 3}}\n'
    laser_guid = guid(ROOT/'Assets/UI/Planets/Tower0.asset')
    component += f'  laser: {{fileID: 11400000, guid: {laser_guid}, type: 2}}\n  facades: []\n  sites:\n'
    for site in sites:
        x, y = site['mount']
        component += (f'  - number: {site["site"]}\n'
                      f'    mount: {{x: {x+offset[0]}, y: {y+offset[1]}}}\n'
                      f'    clockwiseAngle: {site["angle"]}\n')
    component += (f'  preparedTowerLayer: {{fileID: {tower_layer}}}\n'
                  f'  sourceSize: {{x: {size[0]}, y: {size[1]}}}\n'
                  f'  towerScale: 2\n  siteLabel: {label}\n')
    return content+component
