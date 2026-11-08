"""Slice generated artwork without repainting it; no Unity invocation."""
from pathlib import Path
import hashlib
import json
import re
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUTPUT = ROOT / 'Assets/Art/Effects/DavisNovaSupernova'


def metadata(path):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        value = uuid.uuid5(uuid.NAMESPACE_URL, 'ubh-davis-supernova/' + path.relative_to(ROOT).as_posix()).hex
        if path.is_dir():
            meta.write_text(f'fileFormatVersion: 2\nguid: {value}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')
        else:
            template = (ROOT / 'Assets/Art/Planets/DavisNova/Sun.png.meta').read_text()
            template = re.sub(r'^guid: \w+', 'guid: ' + value, template, flags=re.M)
            template = re.sub(r'spriteID: \w+', 'spriteID: ' + value, template)
            meta.write_text(template)


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    metadata(OUTPUT.parent)
    metadata(OUTPUT)
    source = Image.open(HERE / 'supernova-source-v2.png').convert('RGBA')
    frames = []
    report = []
    for index in range(8):
        col, row = index % 4, index // 4
        box = (round(col * source.width / 4), round(row * source.height / 2),
               round((col + 1) * source.width / 4), round((row + 1) * source.height / 2))
        cell = source.crop(box)
        # Remove only nearly invisible alpha residue from generation. Preserve RGB.
        cell.putalpha(cell.getchannel('A').point(lambda a: 0 if a < 8 else a))
        frame = Image.new('RGBA', (512, 512))
        frame.paste(cell, ((512 - cell.width) // 2, (512 - cell.height) // 2))
        path = OUTPUT / f'Supernova-{index + 1:02}.png'
        frame.save(path)
        metadata(path)
        frames.append(frame)
        bounds = frame.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        report.append(dict(path=path.relative_to(ROOT).as_posix(), source_rect=box,
                           opaque_bounds=bounds, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    preview = Image.new('RGBA', (1024, 512), (29, 35, 48, 255))
    for index, frame in enumerate(frames):
        preview.alpha_composite(frame.resize((256, 256), Image.Resampling.NEAREST), ((index % 4) * 256, (index // 4) * 256))
    preview.convert('RGB').save(HERE / 'frame-review.png')
    animation = []
    for frame in frames:
        background = Image.new('RGBA', frame.size, (29, 35, 48, 255))
        background.alpha_composite(frame)
        animation.append(background.convert('RGB'))
    animation.append(Image.new('RGB', (512, 512), (29, 35, 48)))
    animation[0].save(HERE / 'animation-review.gif', save_all=True, append_images=animation[1:], duration=[100]*8+[600], loop=0)
    (HERE / 'frames.json').write_text(json.dumps(dict(cell_size=512, pixels_per_unit=100, fps=10,
        cleanup='alpha below 8 removed; RGB preserved; equal-grid crops centered without rescaling', frames=report), indent=2) + '\n')
    print('Prepared eight centered supernova frames and transparent Sprite metadata.')


if __name__ == '__main__':
    main()
