"""Package reviewed effect cells with measured gutters. Requires Tools/requirements.txt."""
from pathlib import Path
from PIL import Image
import numpy as np,json
ROOT=Path(__file__).resolve().parents[1]
import os
os.chdir(ROOT)
report=[]
for p in sorted(Path('Documentation/Design-Samples').glob('FX-*-v9.png')):
 name=p.stem[3:-3];im=Image.open(p).convert('RGBA');a=np.array(im.getchannel('A'));cost=(a>96).sum(axis=1);ys=[0]+[min(range(y-32,y+33),key=lambda q:(cost[q],abs(y-q))) for y in [256,512,768]]+[1024]
 for row,action in enumerate(['Slash','Hit','Finisher','Guard']):
  cost=(a[ys[row]:ys[row+1]]>96).sum(axis=0);xs=[0]+[min(range(x-48,x+49),key=lambda q:(cost[q],abs(x-q))) for x in [256,512,768,1024,1280]]+[1536]
  for col in range(6):
   box=(xs[col],ys[row],xs[col+1],ys[row+1]);crop=im.crop(box)
   # Import packaging only: preserve source pixels and add a common padded canvas.
   assert crop.width<=352 and crop.height<=352
   cell=Image.new('RGBA',(352,352));cell.paste(crop,((352-crop.width)//2,(352-crop.height)//2));cell=cell.resize((256,256),Image.Resampling.LANCZOS)
   target=Path('Assets/Resources/Art/CombatV9')/name/action/f'{col+1:02}.png';cell.save(target)
   bounds=cell.getchannel('A').point(lambda x:255 if x>12 else 0).getbbox();margin=min(bounds[0],bounds[1],256-bounds[2],256-bounds[3]);report.append({'fighter':name,'action':action,'frame':col+1,'sourceCrop':box,'importMargin':margin})
Path('Documentation/FX-SLICING-v9.json').write_text(json.dumps({'frames':len(report),'cellSize':256,'sourceSheets':8,'method':'Adaptive transparent gutters, common 352-pixel padded canvas, uniform 256-pixel import','framesTouchingOutputEdge':sum(r['importMargin']==0 for r in report),'minimumOutputMargin':min(r['importMargin'] for r in report),'cells':report},indent=2))
print('192 frames; output edge contacts:',sum(r['importMargin']==0 for r in report),'minimum margin:',min(r['importMargin'] for r in report))
