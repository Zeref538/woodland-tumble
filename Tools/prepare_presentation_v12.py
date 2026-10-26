"""Package reviewed generated sheets. Only slice, uniformly scale and pad pixels."""
from pathlib import Path
import argparse
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw
from prepare_marin_v10 import PAIRS
from prepare_roster_rework import write_frame, components

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Documentation/Presentation-v12'
NAMES = ('Fern', 'Ember', 'Luna', 'Bram', 'Zephyr', 'Volt', 'Nyx', 'Marin')
ACTIONS = ('Guard', 'Punch', 'Kick', 'Special')
MARIN_BODY_PIXELS = 210


def box(image, threshold=32):
    return image.getchannel('A').point(lambda a: 255 if a > threshold else 0).getbbox()


def spaced_cells(sheet):
    found,_=components(sheet)
    largest=max(body['area'] for body in found.values())
    bodies=[body for body in found.values() if body['area']>largest*.3]
    if len(bodies)!=20:
        raise ValueError(f'Expected twenty separate figures, found {len(bodies)}')
    # Lying figures sit low in a cell. Order actual visual rows rather than
    # assuming their centers fall in the canvas quarter containing the baseline.
    bodies.sort(key=lambda body:(body['bounds'][1]+body['bounds'][3])/2)
    ordered=[]
    for row in range(4):
        ordered.extend(sorted(bodies[row*5:(row+1)*5],key=lambda body:body['bounds'][0]))
    cells=[]
    for body in ordered:
        a,b,c,d=body['bounds'];left=max(0,a-4);top=max(0,b-4)
        cells.append(dict(image=sheet.crop((left,top,min(sheet.width,c+4),min(sheet.height,d+4))),origin=(left,top),bodyBounds=body['bounds']))
    return cells,len(bodies)


def icons():
    report = []
    contact = Image.new('RGB', (640, 8 * 154), (15, 12, 32))
    draw = ImageDraw.Draw(contact)
    for row, name in enumerate(NAMES):
        sheet = Image.open(SOURCE / (name + '-Skills.png')).convert('RGBA')
        for index, action in enumerate(ACTIONS):
            x, y = index % 2, index // 2
            cell = sheet.crop((round(x * sheet.width / 2), round(y * sheet.height / 2),
                               round((x + 1) * sheet.width / 2), round((y + 1) * sheet.height / 2)))
            bounds = box(cell)
            if not bounds:
                raise ValueError(f'{name}/{action}: empty quadrant')
            if min(bounds[0], bounds[1], cell.width-bounds[2], cell.height-bounds[3]) < 2:
                raise ValueError(f'{name}/{action}: circle touches source gutter')
            alpha = np.array(cell.getchannel('A'), dtype=float)
            yy, xx = np.nonzero(alpha > 32)
            weights = alpha[yy, xx] ** 2
            cx, cy = np.average(xx, weights=weights), np.average(yy, weights=weights)
            radius = np.sqrt((xx-cx)**2+(yy-cy)**2).max()
            factor = 54 / radius
            crop = cell.crop(bounds)
            scaled = crop.resize((round(crop.width * factor), round(crop.height * factor)), Image.Resampling.NEAREST)
            out = Image.new('RGBA', (128, 128))
            out.alpha_composite(scaled, (round(64-(cx-bounds[0])*factor), round(64-(cy-bounds[1])*factor)))
            path = ROOT / 'Assets/Art/UI/SkillIcons' / name / (action + '.png')
            previous = hashlib.sha256(path.read_bytes()).hexdigest()
            out.save(path)
            alpha = np.array(out.getchannel('A'), dtype=float)
            yy, xx = np.nonzero(alpha > 32)
            center = [float(np.average(xx, weights=alpha[yy,xx]**2)), float(np.average(yy, weights=alpha[yy,xx]**2))]
            report.append(dict(fighter=name, action=action, source=str((SOURCE/(name+'-Skills.png')).relative_to(ROOT)),
                               sourceBounds=bounds, canvas=[128,128], visibleCenter=center,
                               centerError=float(np.linalg.norm(np.array(center)-64)),
                               safeRadius=float(np.sqrt((xx-64)**2+(yy-64)**2).max()),
                               oldSha256=previous, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
            contact.paste(out, (index*160+16,row*154+4), out)
            draw.text((index*160+12,row*154+133), name+' / '+action, fill=(245,244,233))
    contact.save(SOURCE/'Ability-Review.png')
    (ROOT/'Documentation/ABILITY-ART-v12.json').write_text(json.dumps(report, indent=2))
    assert len(set(item['sha256'] for item in report)) == 32
    assert all(item['sha256'] != item['oldSha256'] and item['centerError'] < 2 and item['safeRadius'] < 56 for item in report)
    print('32 original illustrated icons, all changed, distinct and centered within 2 pixels')


def marin():
    sheet = Image.open(SOURCE/'Marin-Idle.png').convert('RGBA')
    frames = [sheet.crop((round(x*sheet.width/2),round(y*sheet.height/2),round((x+1)*sheet.width/2),round((y+1)*sheet.height/2))) for y in range(2) for x in range(2)]
    bounds = [box(frame,48) for frame in frames]
    # One shared scale: genuine breathing changes never alter the whole-body scale.
    factor = 606/max(b[3]-b[1] for b in bounds)
    selection = ROOT/'Assets/Art/SelectionV10/Marin'
    for index,(frame,b) in enumerate(zip(frames,bounds),1):
        body = frame.crop(b)
        scaled = body.resize((round(body.width*factor),round(body.height*factor)),Image.Resampling.NEAREST)
        out = Image.new('RGBA',(656,656))
        out.alpha_composite(scaled,((656-scaled.width)//2,636-scaled.height))
        out.save(selection/f'{index:02}.png')
    body=frames[0].crop(bounds[0]);scale=MARIN_BODY_PIXELS/body.height
    body=body.resize((round(body.width*scale),MARIN_BODY_PIXELS),Image.Resampling.NEAREST)
    out=Image.new('RGBA',(256,256));out.alpha_composite(body,((256-body.width)//2,232-MARIN_BODY_PIXELS))
    report=[write_frame('Marin','Idle',1,out)]
    for folder in ('ProfilesRework','PortraitsRework'):
        out.save(ROOT/'Assets/Art'/folder/'Marin.png')
    for first,second,reference in PAIRS:
        path=SOURCE/f'Marin-{first}-{second}-Spaced.png'
        if not path.exists():
            raise ValueError(f'Missing reviewed spacing correction: {path.name}')
        sheet=Image.open(path).convert('RGBA');cells,count=spaced_cells(sheet)
        if count!=20:
            raise ValueError(f'{path.name}: expected twenty isolated bodies, found {count}')
        for cell in cells:
            a,b,c,d=cell['bodyBounds']
            if min(a,b,sheet.width-c,sheet.height-d)<4:
                raise ValueError(f'{path.name}: source body touches the canvas edge')
        a,b,c,d=cells[reference]['bodyBounds'];factor=MARIN_BODY_PIXELS/(d-b)
        if first=='Downed':
            cells=[cells[i] for i in (0,1,2,3,4,5,6,5,3,1)]+cells[10:]
        for index,cell in enumerate(cells):
            a,b,c,d=cell['bodyBounds'];left,top=cell['origin'];art=cell['image']
            scaled=art.resize((round(art.width*factor),round(art.height*factor)),Image.Resampling.NEAREST)
            canvas=Image.new('RGBA',(384,320))
            anchor_x=((a+c)/2-left)*factor;anchor_y=(d-top)*factor
            canvas.alpha_composite(scaled,(round(192-anchor_x),round(288-anchor_y)))
            visible=box(canvas)
            if not visible or min(visible[0],visible[1],canvas.width-visible[2],canvas.height-visible[3])<4:
                raise ValueError(f'{first}/{second}/{index}: clipped runtime frame')
            item=write_frame('Marin',first if index<10 else second,index%10+1,canvas,(.5,.1))
            item.update(source=str(path.relative_to(ROOT)),uniformBatchScale=factor,sourcePoseCount=count,sourceBounds=cell['bodyBounds'])
            report.append(item)
    (ROOT/'Documentation/MARIN-PROPORTIONS-v12.json').write_text(json.dumps(dict(gameplayFrames=len(report),selectionFrames=4,standingBodyPixels=MARIN_BODY_PIXELS,otherRosterBodyPixels=190,feetAnchorPreserved=True,frames=report),indent=2))
    print('Marin: 161 gameplay frames and four selection breathing poses imported')


def compare(label):
    contact=Image.new('RGB',(256*6,290*2),(16,13,32));draw=ImageDraw.Draw(contact)
    for row,name in enumerate(('Fern','Marin')):
        for col,(state,index) in enumerate((('Idle',1),('Punch',3),('Kick',4),('Block',1),('Special',1),('GetUp',10))):
            path=ROOT/'Assets/Art/CharactersRework'/name/state/f'{index:02}.png'
            im=Image.open(path).convert('RGBA');b=box(im);art=im.crop(b)
            contact.paste(art,(col*256+(256-art.width)//2,row*290+255-art.height),art)
            draw.text((col*256+10,row*290+266),name+' / '+state,fill=(245,244,235))
    SOURCE.mkdir(parents=True,exist_ok=True);contact.save(SOURCE/f'Marin-{label}-Comparison.png')
    if label=='after':
        roster=Image.new('RGB',(1024,600),(16,13,32));draw=ImageDraw.Draw(roster)
        for index,name in enumerate(NAMES):
            image=Image.open(ROOT/'Assets/Art/CharactersRework'/name/'Idle/01.png').convert('RGBA')
            art=image.crop(box(image));x=index%4*256;y=index//4*300
            roster.paste(art,(x+(256-art.width)//2,y+260-art.height),art)
            draw.text((x+18,y+278),name+' / native pixel scale',fill=(245,244,235))
        roster.save(SOURCE/'Roster-Proportions.png')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('task',choices=['icons','marin','before','after']);args=parser.parse_args()
    if args.task=='icons': icons()
    elif args.task=='marin': marin()
    else: compare(args.task)
