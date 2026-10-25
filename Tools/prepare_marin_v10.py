"""Import reviewed Marin sheets with one scale per animation batch."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from prepare_roster_rework import components,write_frame
ROOT=Path(__file__).resolve().parents[1]
PAIRS=[('Walk','Jump',0),('Hurt','Push',10),('Punch','Kick',0),('Knockout','Block',10),('DoubleJump','Special',10),('Dash','Backstep',19),('Victory','Ultimate',9),('Downed','GetUp',19)]

def reviewed_cells(im):
 found,_=components(im)
 largest=max(c['area'] for c in found.values())
 bodies=[c for c in found.values() if c['area']>largest*.3]
 rows=[[] for _ in range(4)]
 for body in bodies:
  a,b,c,d=body['bounds'];row=min(3,int(((b+d)/2)/im.height*4));rows[row].append(body)
 cells=[]
 for row in rows:
  row.sort(key=lambda c:c['bounds'][0])
  if not 5<=len(row)<=6:raise ValueError(f'Needs visual review: row contains {len(row)} bodies')
  # Some generated rows contain six poses. Retain five evenly spaced poses.
  for i in [round(j*(len(row)-1)/4) for j in range(5)]:
   body=row[i];a,b,c,d=body['bounds'];left=max(0,a-4);top=max(0,b-4)
   cells.append({'image':im.crop((left,top,min(im.width,c+4),min(im.height,d+4))), 'origin':(left,top),'bodyBounds':body['bounds']})
 return cells,len(bodies)

def run():
 report=[]
 for first,second,reference in PAIRS:
  source=ROOT/'Documentation/Selection-v10'/f'Marin-{first}-{second}.png'
  im=Image.open(source).convert('RGBA');cells,source_count=reviewed_cells(im)
  b=cells[reference]['bodyBounds'];factor=190/(b[3]-b[1])
  if first=='Downed':cells=[cells[i] for i in (0,1,2,3,4,3,2,1,0,1)]+cells[10:]
  for i,cell in enumerate(cells):
   a,b,c,d=cell['bodyBounds'];left,top=cell['origin'];art=cell['image']
   scaled=art.resize((round(art.width*factor),round(art.height*factor)),Image.Resampling.NEAREST)
   canvas=Image.new('RGBA',(384,320));anchor_x=((a+c)/2-left)*factor;anchor_y=(d-top)*factor
   canvas.alpha_composite(scaled,(round(192-anchor_x),round(288-anchor_y)))
   item=write_frame('Marin',first if i<10 else second,i%10+1,canvas,(.5,.1))
   item.update(source=source.name,uniformBatchScale=factor,sourcePoseCount=source_count,sourceBounds=cell['bodyBounds'])
   report.append(item)
 # An actual neutral selection pose supplies the static in-game idle and profile.
 im=Image.open(ROOT/'Assets/Art/SelectionV10/Marin/01.png').convert('RGBA');bounds=im.getchannel('A').getbbox();body=im.crop(bounds)
 body=body.resize((round(body.width*190/body.height),190),Image.Resampling.NEAREST)
 out=Image.new('RGBA',(256,256));out.alpha_composite(body,((256-body.width)//2,42))
 write_frame('Marin','Idle',1,out)
 for folder in ('ProfilesRework','PortraitsRework'):
  path=ROOT/'Assets/Art'/folder/'Marin.png';out.save(path)
 # Same uniform grid conventions as the existing six-frame combat atlas.
 im=Image.open(ROOT/'Documentation/Selection-v10/Marin-FX.png').convert('RGBA');alpha=np.array(im.getchannel('A'))
 cost=(alpha>96).sum(axis=1)
 ys=[0]+[min(range(y-32,y+33),key=lambda q:(cost[q],abs(y-q))) for y in (256,512,768)]+[1024]
 for row,state in enumerate(('Slash','Hit','Finisher','Guard')):
  target=ROOT/'Assets/Resources/Art/CombatV9/Marin'/state;target.mkdir(parents=True,exist_ok=True)
  cost=(alpha[ys[row]:ys[row+1]]>96).sum(axis=0)
  xs=[0]+[min(range(x-48,x+49),key=lambda q:(cost[q],abs(x-q))) for x in (256,512,768,1024,1280)]+[1536]
  for col in range(6):
   cell=im.crop((xs[col],ys[row],xs[col+1],ys[row+1]));assert cell.width<=352 and cell.height<=352
   padded=Image.new('RGBA',(352,352));padded.paste(cell,((352-cell.width)//2,(352-cell.height)//2))
   padded.resize((256,256),Image.Resampling.NEAREST).save(target/f'{col+1:02}.png')
 (ROOT/'Documentation/MARIN-IMPORT-v10.json').write_text(json.dumps({'gameplayFrames':len(report)+1,'fxFrames':24,'frames':report},indent=2))
 print('Marin: 161 gameplay frames, 24 effects')
if __name__=='__main__':run()
