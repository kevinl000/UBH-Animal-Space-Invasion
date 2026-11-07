"""Copy the reviewed layers into Unity and wire only UnionPrimeVisual.prefab.

Run from the project root with Pillow. Existing asset GUIDs and prefab IDs are
preserved. The Unity Editor imports the prepared sprites when opened through Hub.
"""
import hashlib
import json
from pathlib import Path
import re
import shutil
import uuid

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
DEST = ROOT / "Assets/Art/Planets/UnionPrime"
SCRIPT = ROOT / "Assets/Scripts/Menu/Planets/UnionPrimeVisualTest.cs"
PREFAB = ROOT / "Assets/UI/Planets/UnionPrimeVisual.prefab"
COMPONENT_ID = 746212930054721001


def guid(path):
    meta = Path(str(path)+".meta")
    if meta.exists():
        return re.search(r"^guid: ([a-f0-9]+)$", meta.read_text(), re.M)[1]
    return uuid.uuid4().hex


def main():
    for folder in (ROOT / "Assets/Art/Planets", DEST):
        folder.mkdir(exist_ok=True)
        meta = Path(str(folder)+".meta")
        if not meta.exists():
            meta.write_text(f"fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    script_guid = guid(SCRIPT)
    script_meta = Path(str(SCRIPT)+".meta")
    if not script_meta.exists():
        script_meta.write_text(f"fileFormatVersion: 2\nguid: {script_guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n", newline="\n")
    template = (ROOT / "Assets/UI/Planets/Body.png.meta").read_text()
    template = re.sub(r"  internalIDToNameTable:.*?  externalObjects:", "  internalIDToNameTable: []\n  externalObjects:", template, flags=re.S)
    template = re.sub(r"  spriteSheet:.*?  mipmapLimitGroupName:", "  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: 5e97eb03825dee720800000000000000\n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    nameFileIdTable: {}\n  mipmapLimitGroupName:", template, flags=re.S)
    template = template.replace("spriteMeshType: 1", "spriteMeshType: 0").replace("spriteGenerateFallbackPhysicsShape: 1", "spriteGenerateFallbackPhysicsShape: 0")
    sources = {}

    def sprite(source, name):
        target = DEST / (name+".png")
        shutil.copyfile(source, target)
        sprite_guid = guid(target)
        meta = Path(str(target)+".meta")
        if not meta.exists():
            meta.write_text(re.sub(r"^guid: .*$", "guid: "+sprite_guid, template, flags=re.M), newline="\n")
        sources[target.relative_to(ROOT).as_posix()] = {
            "source": source.relative_to(ROOT).as_posix(),
            "sha256": hashlib.sha256(source.read_bytes()).hexdigest()}
        return f"{{fileID: 21300000, guid: {sprite_guid}, type: 3}}"

    polish = HERE
    rear_ground = sprite(polish / "rear-ground.png", "RearGround")
    terrain = sprite(polish / "terrain.png", "Terrain")
    foreground = sprite(polish / "foreground.png", "Foreground")
    sweep = ROOT / "Art/Towers/Laser/Redesign/Sweep"
    base = sprite(sweep / "base-sprite.png", "LaserBase")
    gun = sprite(sweep / "gun-sprite.png", "LaserGun")
    portrait_source = HERE / "laser-portrait.png"
    portrait = Image.new("RGBA", (256, 256))
    portrait.alpha_composite(Image.open(sweep / "base-sprite.png"), (64, 104))
    portrait.alpha_composite(Image.open(sweep / "gun-sprite.png"), (116, 100))
    portrait.crop(portrait.getbbox()).save(portrait_source)
    portrait_ref = sprite(portrait_source, "LaserPortrait")
    composition_path = polish / "Facades/facades.json"
    parts = json.loads(composition_path.read_text())["layers"]
    facades = []
    for index, part in enumerate(parts):
        source = (composition_path.parent / part["file"]).resolve()
        ref = sprite(source, "Facade"+str(index))
        x, y = part["origin_xy"]
        facades.append(f"  - sprite: {ref}\n    origin: {{x: {x}, y: {y}}}")
    study = json.loads((polish / "polish.json").read_text())
    sites = []
    for site in study["site_placements"]:
        x, y = site["study_mount"]
        sites.append(f"  - number: {site['clock_site']}\n    mount: {{x: {x}, y: {y}}}\n    clockwiseAngle: {site['study_angle']}")
    content = PREFAB.read_text()
    component_ref = f"  - component: {{fileID: {COMPONENT_ID}}}\n"
    if component_ref not in content:
        content = content.replace("  - component: {fileID: 7136061047531963944}\n", "  - component: {fileID: 7136061047531963944}\n"+component_ref, 1)
    component = f"""--- !u!114 &{COMPONENT_ID}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6809327136469136354}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: PlanetaryDefense.Menu::UnionPrimeVisualTest
  silhouette: {{fileID: 8885571135369800694}}
  rearGround: {rear_ground}
  terrain: {terrain}
  foreground: {foreground}
  laserBase: {base}
  laserGun: {gun}
  laserPortrait: {portrait_ref}
  laser: {{fileID: 11400000, guid: {guid(ROOT / 'Assets/UI/Planets/Tower0.asset')}, type: 2}}
  facades:
"""+"\n".join(facades)+"\n  sites:\n"+"\n".join(sites)+"\n"
    marker = f"--- !u!114 &{COMPONENT_ID}\n"
    if marker in content:
        content = re.sub(re.escape(marker)+r".*?(?=--- !u!|\Z)", component, content, flags=re.S)
    else:
        content += component
    PREFAB.write_text(content, encoding="utf-8", newline="\n")
    (HERE / "unity-import.json").write_text(json.dumps({"editor": "6000.6.7f1", "images": sources,
        "visual_prefab": PREFAB.relative_to(ROOT).as_posix(), "site_count": len(sites),
        "tower_scale": 1.5, "source_canvas_display_width": 640,
        "limits": "Terrain and facade polish candidate; Unity visual review pending; no combat/economy"}, indent=2)+"\n")
    print(f"Prepared {len(sources)} separate sprites, 12 site mounts, and the existing UnionPrimeVisual prefab.")


if __name__ == "__main__":
    main()
