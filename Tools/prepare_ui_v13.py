"""Slice reviewed labeled UI sheets and uniformly fit their original pixels."""
from pathlib import Path
import json
from PIL import Image, ImageDraw
from prepare_roster_rework import components

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'Documentation/Presentation-v13'
TARGET = ROOT/'Assets/Art/UI/MenuV13'
BUTTONS = ('SINGLE PLAYER','LOCAL VERSUS','SETTINGS','EXIT','BACK','LOCK IN','FIGHT','MOVE LIST')


def package(sheet_name, names, size):
    sheet = Image.open(SOURCE/(sheet_name+'.png')).convert('RGBA')
    found,_=components(sheet)
    plates=sorted(found.values(),key=lambda body:body['area'],reverse=True)[:len(names)]
    plates.sort(key=lambda body:(body['bounds'][1]+body['bounds'][3])/2)
    ordered=[]
    for row in range(4):
        ordered.extend(sorted(plates[row*2:row*2+2],key=lambda body:body['bounds'][0]))
    results = []
    for index,name in enumerate(names):
        bounds=ordered[index]['bounds']
        art=sheet.crop(bounds)
        scale=min((size[0]-16)/art.width,(size[1]-16)/art.height)
        art=art.resize((round(art.width*scale),round(art.height*scale)),Image.Resampling.LANCZOS)
        canvas=(art.width+16,art.height+16)
        out=Image.new('RGBA',canvas)
        out.alpha_composite(art,(8,8))
        path=TARGET/(name+'.png');out.save(path)
        results.append(dict(asset=str(path.relative_to(ROOT)),source=sheet_name,sourceBounds=bounds,canvas=canvas))
    return results


if __name__=='__main__':
    TARGET.mkdir(parents=True,exist_ok=True)
    report=package('Button-Sheet',['Menu_'+name.replace(' ','_') for name in BUTTONS],(640,160))
    report+=package('Arena-Name-Sheet',['ArenaHeading']+['Arena_'+str(i) for i in range(6)],(640,240))
    Image.open(SOURCE/'Eight-Rivals-Backdrop.png').save(ROOT/'Assets/Art/Arcade/BattleBackdrop.png')
    Image.open(SOURCE/'Cinder-Quay.png').save(ROOT/'Assets/Art/Arcade/Maps/Forest.png')
    review=Image.new('RGB',(1024,15*130),(14,12,28));draw=ImageDraw.Draw(review)
    for index,item in enumerate(report):
        image=Image.open(ROOT/item['asset']).convert('RGBA');image.thumbnail((740,120))
        review.paste(image,(270+(740-image.width)//2,index*130+(120-image.height)//2),image)
        draw.text((8,index*130+48),Path(item['asset']).stem,fill=(245,243,230))
    review.save(SOURCE/'Labeled-Art-Review.png')
    (ROOT/'Documentation/LABELED-UI-v13.json').write_text(json.dumps(report,indent=2))
    print('Packaged eight labeled buttons, seven arena plaques and two scenery layers.')
