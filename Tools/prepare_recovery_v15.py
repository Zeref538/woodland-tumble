"""Slice reviewed recovery atlases with one body scale per fighter."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw
from prepare_roster_rework import components

ROOT = Path(__file__).resolve().parents[1]
NAMES = ('Fern', 'Ember', 'Luna', 'Bram', 'Zephyr', 'Volt', 'Nyx', 'Marin')


def run():
    report = []
    prior_path=ROOT/"Documentation/RECOVERY-IMPORT-v15.json"
    prior={item["path"]:item for item in json.loads(prior_path.read_text())} if prior_path.exists() else {}
    contact = Image.new('RGB', (1080, 8 * 270), (42, 44, 50))
    draw = ImageDraw.Draw(contact)
    for row, name in enumerate(NAMES):
        source = ROOT / 'Documentation/Recovery-v15' / (name + '.png')
        sheet = Image.open(source).convert('RGBA')
        found, _ = components(sheet)
        bodies = sorted(found.values(), key=lambda item: item['area'], reverse=True)[:15]
        if len(bodies) != 15:
            raise ValueError(f'{name}: expected fifteen separate body poses')
        bodies.sort(key=lambda item: (item['bounds'][1] + item['bounds'][3]) / 2)
        ordered = []
        for start in range(0, 15, 5):
            ordered.extend(sorted(bodies[start:start+5], key=lambda item: item['bounds'][0]))
        poses = [sheet.crop(item['bounds']) for item in ordered]
        target = 210 if name == 'Marin' else 190
        factor = target / poses[14].height
        for state_index, state in enumerate(('Knockout', 'Downed', 'GetUp')):
            order = (0, 1, 2, 3, 4, 3, 2, 1, 0, 1) if state == 'Downed' else (0, 0, 1, 1, 2, 2, 3, 3, 4, 4)
            for frame, pose_index in enumerate(order, 1):
                body = poses[state_index * 5 + pose_index]
                correction=(.9 if state=="Downed" else 1-.1*pose_index/4 if state=="Knockout" else .9+.1*pose_index/4) if name=="Luna" else 1
                body = body.resize((round(body.width * factor * correction), round(body.height * factor * correction)), Image.Resampling.NEAREST)
                out = Image.new('RGBA', (384, 320))
                out.alpha_composite(body, ((384-body.width)//2, 288-body.height))
                if body.width >= 368 or body.height >= 280:
                    raise ValueError(f'{name}/{state}: body would clip')
                path = ROOT / 'Assets/Art/CharactersRework' / name / state / f'{frame:02}.png'
                old_hash = prior.get(str(path.relative_to(ROOT)),{}).get('oldSha256',hashlib.sha256(path.read_bytes()).hexdigest())
                out.save(path)
                meta = path.with_suffix('.png.meta')
                text = meta.read_text()
                import re
                text = re.sub(r'spritePivot: \{x: [^,]+, y: [^}]+\}', 'spritePivot: {x: 0.5, y: 0.1}', text)
                meta.write_text(text)
                report.append(dict(path=str(path.relative_to(ROOT)), oldSha256=old_hash, sha256=hashlib.sha256(path.read_bytes()).hexdigest(), sharedScale=factor, poseScaleCorrection=correction, visiblePixels=list(out.getbbox()), standingPixels=target))
            sample = Image.open(path if state != 'Downed' else path.parent/'01.png').convert('RGBA')
            sample.thumbnail((260, 235))
            contact.paste(sample, ((state_index+1)*270, row*270+28), sample)
            draw.text(((state_index+1)*270+4,row*270+5), name+' '+state, fill='white')
        idle = Image.open(ROOT/'Assets/Art/CharactersRework'/name/'Idle/01.png').convert('RGBA')
        idle.thumbnail((252,235));contact.paste(idle,(0,row*270+28),idle)
        draw.text((4,row*270+5),name+' Idle',fill='white')
    contact.save(ROOT/'Documentation/Recovery-v15/Runtime-Review.png')
    (ROOT/'Documentation/RECOVERY-IMPORT-v15.json').write_text(json.dumps(report,indent=2))
    print(f'Imported {len(report)} frames with shared per-fighter scale and ground pivot')


if __name__ == '__main__':
    run()
