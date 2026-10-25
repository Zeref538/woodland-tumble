"""Center visible effect artwork within a dedicated circular HUD safe area."""
from pathlib import Path
from PIL import Image
import numpy as np,json
root=Path(__file__).resolve().parents[1]
report=[]
for name in ('Fern','Ember','Luna','Bram','Zephyr','Volt','Nyx','Marin'):
 for action,source in (('Guard','Guard'),('Punch','Hit'),('Kick','Slash'),('Special','Finisher')):
  im=Image.open(root/'Assets/Resources/Art/CombatV9'/name/source/'03.png').convert('RGBA')
  a=np.array(im.getchannel('A'),dtype=float);ys,xs=np.nonzero(a>32);weights=(a[ys,xs]/255)**2
  cx=float(np.average(xs,weights=weights));cy=float(np.average(ys,weights=weights))
  radius=float(np.sqrt((xs-cx)**2+(ys-cy)**2).max());scale=27/radius
  box=im.getchannel('A').point(lambda v:255 if v>8 else 0).getbbox();crop=im.crop(box)
  resized=crop.resize((max(1,round(crop.width*scale)),max(1,round(crop.height*scale))),Image.Resampling.NEAREST)
  out=Image.new('RGBA',(64,64));out.alpha_composite(resized,(round(32-(cx-box[0])*scale),round(32-(cy-box[1])*scale)))
  path=root/'Assets/Art/UI/SkillIcons'/name/f'{action}.png';path.parent.mkdir(parents=True,exist_ok=True);out.save(path)
  aa=np.array(out.getchannel('A'),dtype=float);yy,xx=np.nonzero(aa>32);ww=(aa[yy,xx]/255)**2
  center=[float(np.average(xx,weights=ww)),float(np.average(yy,weights=ww))]
  report.append({'fighter':name,'action':action,'visibleCenter':center,'sourceCenter':[cx,cy],'distanceFromCanvasCenter':float(np.linalg.norm(np.array(center)-32))})
(root/'Documentation/HUD-ICON-CENTERING-v10.json').write_text(json.dumps(report,indent=2))
print('Centered 32 icons; largest visual center offset:',max(p['distanceFromCanvasCenter'] for p in report))
