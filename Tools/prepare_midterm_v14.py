"""Extract reviewed ink UI and elemental shield assets without repainting them."""
from pathlib import Path
import json
from PIL import Image
from prepare_roster_rework import components

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Documentation/Presentation-v14'
UI=ROOT/'Assets/Art/UI/InkV14'
NAMES=('Fern','Ember','Luna','Bram','Zephyr','Volt','Nyx','Marin')
LABELS=(('SINGLE PLAYER','LOCAL VERSUS','SETTINGS','EXIT','BACK','LOCK IN'),('FIGHT','MOVE LIST','RESUME','REMATCH','CHOOSE MAP','MAIN MENU'),('VOLUME -','VOLUME +','FULLSCREEN','CAMERA SHAKE','EFFECTS QUALITY'))


def extract(name,count,columns=2):
    image=Image.open(SOURCE/(name+'.png')).convert('RGBA')
    found,_=components(image)
    bodies=sorted(found.values(),key=lambda body:body['area'],reverse=True)[:count]
    if len(bodies)!=count:raise ValueError(f'{name}: missing artwork')
    bodies.sort(key=lambda body:(body['bounds'][1]+body['bounds'][3])/2)
    ordered=[]
    for start in range(0,count,columns):ordered.extend(sorted(bodies[start:start+columns],key=lambda body:body['bounds'][0]))
    return [image.crop(body['bounds']) for body in ordered]


def padded(image,max_size):
    scale=min((max_size[0]-16)/image.width,(max_size[1]-16)/image.height)
    image=image.resize((max(1,round(image.width*scale)),max(1,round(image.height*scale))),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(image.width+16,image.height+16));out.alpha_composite(image,(8,8));return out


def run():
    UI.mkdir(parents=True,exist_ok=True);report=[]
    for group,labels in enumerate(LABELS):
        for label,image in zip(labels,extract('Buttons-'+str(group),len(labels))):
            path=UI/('Button_'+label.replace(' ','_')+'.png');padded(image,(900,180)).save(path);report.append(str(path.relative_to(ROOT)))
    for label,image in zip(('LongPlate','ShortPlate','GuideFrame','ReadyRail'),extract('UISurfaces',4)):
        path=UI/(label+'.png');padded(image,(900,700) if label=='GuideFrame' else (900,180)).save(path);report.append(str(path.relative_to(ROOT)))
    for label,image in zip(('ArenaHeading',)+tuple('Arena_'+str(i) for i in range(6)),extract('ArenaNames',7)):
        path=UI/(label+'.png');padded(image,(800,240)).save(path);report.append(str(path.relative_to(ROOT)))
    for name,image in zip(NAMES,extract('GuideNames',8)):
        path=UI/('Guide_'+name+'.png');padded(image,(700,180)).save(path);report.append(str(path.relative_to(ROOT)))
    image=Image.open(SOURCE/'Slogan.png').convert('RGBA');image=image.crop(image.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox());padded(image,(1100,180)).save(UI/'Slogan.png')
    Image.open(SOURCE/'MenuBackdrop.png').save(ROOT/'Assets/Art/Arcade/BattleBackdrop.png')
    for name in NAMES:
        frames=extract('Shield-'+name,6,3);scale=214/max(frame.height for frame in frames)
        folder=ROOT/'Assets/Resources/Art/ElementShieldsV14'/name;folder.mkdir(parents=True,exist_ok=True)
        for index,frame in enumerate(frames,1):
            frame=frame.resize((round(frame.width*scale),round(frame.height*scale)),Image.Resampling.NEAREST)
            out=Image.new('RGBA',(256,256));out.alpha_composite(frame,((256-frame.width)//2,(256-frame.height)//2));out.save(folder/f'{index:02}.png')
            report.append(str((folder/f'{index:02}.png').relative_to(ROOT)))
    report.extend(['Assets/Art/UI/InkV14/Slogan.png','Assets/Art/Arcade/BattleBackdrop.png'])
    (ROOT/'Documentation/ART-IMPORT-v14.json').write_text(json.dumps({'sourceImages':16,'uiAssets':37,'shieldFrames':48,'menuBackdrops':1,'paths':report},indent=2))
    print('Imported ink UI and 48 real elemental shield frames')

if __name__=='__main__':run()
