"""Slice reviewed Filipino artwork without stretching lettering or individual body poses."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from prepare_roster_rework import components, sprite_meta

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT/'Documentation/Filipino-v20'
SOURCE = WORK/'Sources'
DEST = ROOT/'Assets/Art/UI/FilipinoV20'
NAMES = ('Fern','Ember','Luna','Bram','Zephyr','Volt','Nyx','Marin')
BUTTONS = (('SINGLE_PLAYER','LOCAL_VERSUS','SETTINGS','EXIT','BACK','LOCK_IN'),
           ('FIGHT','RESUME','REMATCH','CHOOSE_MAP','MAIN_MENU','MOVE_LIST'),
           ('VOLUME_-','VOLUME_+','FULLSCREEN','CAMERA_SHAKE','EFFECTS_QUALITY','ARENA_HEADING'))


def clean(path):
    im=Image.open(path).convert('RGBA')
    im.putalpha(im.getchannel('A').point(lambda a:255 if a>=192 else 0))
    return im


def grid(name,cols,rows):
    im=clean(SOURCE/(name+'.png'));out=[]
    for row in range(rows):
        for col in range(cols):
            cell=im.crop((round(col*im.width/cols),round(row*im.height/rows),round((col+1)*im.width/cols),round((row+1)*im.height/rows)))
            if not cell.getbbox():raise ValueError(name+': empty cell')
            out.append(cell.crop(cell.getbbox()))
    return out


def save(im,path,ppu=100,pivot=(.5,.5)):
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path);sprite_meta(path,ppu,pivot)


def run():
    DEST.mkdir(parents=True,exist_ok=True);report=[]
    review=Image.new('RGB',(7*240,8*330),(35,38,45));draw=ImageDraw.Draw(review)
    for row,name in enumerate(NAMES):
        source=clean(SOURCE/('Uppercut-'+name+'.png'));found,_=components(source)
        bodies=sorted(found.values(),key=lambda v:v['area'],reverse=True)[:6]
        if len(bodies)!=6:raise ValueError(name+': six silhouettes required')
        bodies.sort(key=lambda v:v['bounds'][1]);ordered=[]
        for start in (0,3):ordered.extend(sorted(bodies[start:start+3],key=lambda v:v['bounds'][0]))
        poses=[source.crop(v['bounds']) for v in ordered]
        ref=Image.open(ROOT/'Assets/Art/CharactersRework'/name/'Punch/01.png').convert('RGBA');ref=ref.crop(ref.getbbox())
        scale=ref.height/poses[0].height
        samples=[ref]
        for frame,index in enumerate((0,1,2,3,3,4,4,5,5,0),1):
            pose=poses[index];pose=pose.resize((round(pose.width*scale),round(pose.height*scale)),Image.Resampling.NEAREST)
            feet=pose.crop((0,round(pose.height*.85),pose.width,pose.height)).getbbox();anchor=(feet[0]+feet[2])/2
            out=Image.new('RGBA',(384,384));x=round(192-anchor);y=352-pose.height
            if min(x,y)<0 or x+pose.width>384:raise ValueError(name+': cropped pose')
            out.alpha_composite(pose,(x,y));save(out,ROOT/'Assets/Art/FilipinoUppercuts'/name/f'{frame:02}.png',80,(.5,1/12))
            report.append(dict(character=name,frame=frame,sourcePose=index,sharedScale=scale,bodyPixels=pose.height))
            if frame in (1,2,3,4,6,8):samples.append(pose)
        for col,im in enumerate(samples):
            im=im.resize((round(im.width*.8),round(im.height*.8)),Image.Resampling.NEAREST)
            review.paste(im,(col*240+(240-im.width)//2,row*330+300-im.height),im)
            draw.text((col*240+5,row*330+8),name+(' combat ref' if col==0 else ' uppercut '+str(col)),fill='white')
    review.save(WORK/'Uppercut-Review.png')
    for batch,labels in enumerate(BUTTONS):
        for label,im in zip(labels,grid('Buttons-'+str(batch),2,3)):
            for role,width,height in (('Menu',690,180),('Long',1164,180),('Wide',1980,170),('Short',655,180)):
                art=im.resize((round(im.width*height/im.height),height),Image.Resampling.LANCZOS);p=np.asarray(art)
                yy,xx=np.where((p[:,:,0]>180)&(p[:,:,1]>160)&(p[:,:,2]>100)&(p[:,:,3]>180))
                left=max(1,int(xx.min())-6);right=min(art.width-1,int(xx.max())+7);core=art.crop((left,0,right,height))
                # Uniformly reduce an unusually long label before extending only side ink.
                if core.width>width-30:
                    ratio=(width-30)/core.width;art=art.resize((round(art.width*ratio),round(height*ratio)),Image.Resampling.LANCZOS)
                    left=round(left*ratio);right=round(right*ratio);core=art.crop((left,0,right,art.height))
                out=Image.new('RGBA',(width,height));middle=(width-core.width)//2;top=(height-core.height)//2
                out.alpha_composite(art.crop((0,0,left,art.height)).resize((middle,art.height),Image.Resampling.LANCZOS),(0,top))
                out.alpha_composite(core,(middle,top))
                out.alpha_composite(art.crop((right,0,art.width,art.height)).resize((width-middle-core.width,art.height),Image.Resampling.LANCZOS),(middle+core.width,top))
                actual=np.asarray(out)[top:top+core.height,middle:middle+core.width];expected=np.asarray(core)
                assert np.array_equal(actual[:,:,3],expected[:,:,3])
                assert np.array_equal(actual[expected[:,:,3]>0],expected[expected[:,:,3]>0])
                save(out,DEST/f'Button_{label}_{role}.png')
    for name,im in zip(NAMES,grid('Names',2,4)):save(im,DEST/('Name_'+name+'.png'))
    for index,im in enumerate(grid('ArenaNames',2,3)):save(im,DEST/f'Arena_{index}.png')
    for name,im in zip(NAMES,grid('UppercutIcons',2,4)):
        im.thumbnail((128,128),Image.Resampling.LANCZOS);out=Image.new('RGBA',(128,128));out.alpha_composite(im,((128-im.width)//2,(128-im.height)//2));save(out,DEST/('Uppercut_'+name+'.png'))
    for name,im in zip(NAMES,grid('Fists',2,4)):
        im.thumbnail((256,128),Image.Resampling.NEAREST);out=Image.new('RGBA',(256,128));out.alpha_composite(im,((256-im.width)//2,(128-im.height)//2));save(out,ROOT/'Assets/Resources/Art/FilipinoPunches'/(name+'.png'),160)
    for name in ('Title','Slogan','Laban'):
        im=clean(SOURCE/(name+'.png'));save(im.crop(im.getbbox()),DEST/(name+'.png'))
    (WORK/'import.json').write_text(json.dumps(report,indent=2))
    print('Imported 80 uppercut frames, 72 label variants, eight names, six arena labels, eight icons and eight punch projectiles')


if __name__=='__main__':run()
