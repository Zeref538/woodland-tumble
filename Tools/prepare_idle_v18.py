"""Slice the eight reviewed breathing sheets using combat head size and planted feet."""
from pathlib import Path
import json
import re
import statistics
import shutil
from PIL import Image, ImageDraw
from prepare_roster_rework import components, sprite_meta

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / 'Documentation/Idle-v18'
NAMES = ('Fern', 'Ember', 'Luna', 'Bram', 'Zephyr', 'Volt', 'Nyx', 'Marin')


def head_width(body):
    return body.crop((0, 0, body.width, round(body.height * .38))).getbbox()[2] - body.crop((0, 0, body.width, round(body.height * .38))).getbbox()[0]


def run():
    review = Image.new('RGB', (8 * 220, 8 * 310), (35, 38, 45))
    draw = ImageDraw.Draw(review)
    report = []
    for row, name in enumerate(NAMES):
        folder = ROOT / 'Assets/Art/CharactersRework' / name
        sheet = Image.open(WORK / (name + '.png')).convert('RGBA')
        # The source includes faint alpha glow. Keep only the opaque sprite silhouette.
        sheet.putalpha(sheet.getchannel('A').point(lambda a: 255 if a >= 192 else 0))
        found, _ = components(sheet)
        bodies = sorted(found.values(), key=lambda item: item['area'], reverse=True)[:6]
        if len(bodies) != 6 or min(item['area'] for item in bodies) < 2000:
            raise ValueError(name + ': expected six complete silhouettes')
        bodies.sort(key=lambda item: item['bounds'][1])
        ordered = []
        for start in (0, 3):
            ordered.extend(sorted(bodies[start:start+3], key=lambda item: item['bounds'][0]))
        poses = [sheet.crop(item['bounds']) for item in ordered]
        reference = Image.open(folder / 'Punch/01.png').convert('RGBA')
        reference = reference.crop(reference.getbbox())
        target = head_width(reference)
        factor = min(target / statistics.median(head_width(pose) for pose in poses),
                     reference.height / statistics.median(pose.height for pose in poses))
        backup = WORK / 'Before' / name
        backup.mkdir(parents=True, exist_ok=True)
        for suffix in ('01.png', '01.png.meta'):
            if not (backup / suffix).exists():
                shutil.copy2(folder / 'Idle' / suffix, backup / suffix)
        old = Image.open(backup / '01.png').convert('RGBA')
        samples = [old, Image.open(folder / 'Punch/01.png').convert('RGBA')]
        for index, pose_index in enumerate((0, 1, 2, 3, 4, 5, 4, 3, 2, 1), 1):
            pose = poses[pose_index]
            pose = pose.resize((round(pose.width * factor), round(pose.height * factor)), Image.Resampling.NEAREST)
            foot_bounds = pose.crop((0, round(pose.height * .85), pose.width, pose.height)).getbbox()
            anchor = (foot_bounds[0] + foot_bounds[2]) / 2
            out = Image.new('RGBA', (384, 320))
            x, y = round(192 - anchor), 288 - pose.height
            if x < 0 or y < 0 or x + pose.width > 384:
                raise ValueError(name + ': body clips canvas')
            out.alpha_composite(pose, (x, y))
            path = folder / 'Idle' / f'{index:02}.png'
            out.save(path)
            if not path.with_suffix('.png.meta').exists():
                sprite_meta(path, pivot=(.5, .1))
            else:
                meta = path.with_suffix('.png.meta')
                meta.write_text(re.sub(r'spritePivot: \{[^}]+\}', 'spritePivot: {x: 0.5, y: 0.1}', meta.read_text()))
            report.append(dict(character=name, frame=index, sourcePose=pose_index, scale=factor,
                               combatHeadPixels=target, idleHeadPixels=head_width(pose), bodyHeight=pose.height))
            if index <= 6:
                samples.append(out)
        for col, sample in enumerate(samples):
            sample = sample.crop(sample.getbbox())
            # Fixed review scale: no fitting each image to its cell.
            sample = sample.resize((round(sample.width * .8), round(sample.height * .8)), Image.Resampling.NEAREST)
            review.paste(sample, (col * 220 + (220-sample.width)//2, row * 310 + 280-sample.height), sample)
            draw.text((col * 220 + 6, row * 310 + 12), name + (' OLD IDLE' if col == 0 else ' COMBAT REF' if col == 1 else ' IDLE ' + str(col-1)), fill='white')
    review.save(WORK / 'Runtime-Review.png')
    (WORK / 'import.json').write_text(json.dumps(report, indent=2))
    print('Imported', len(report), 'idle frames using one combat head scale per fighter')


if __name__ == '__main__':
    run()
