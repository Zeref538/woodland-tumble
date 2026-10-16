"""Mechanically extract generated artwork into standalone aligned PNGs; no repainting."""
from PIL import Image, ImageDraw, ImageFilter, ImageChops
from pathlib import Path
import json, shutil, hashlib
ROOT = Path(__file__).resolve().parent.parent
sources = json.loads((ROOT/'Tools/art-sources.json').read_text())
report = {'frames': [], 'sources': {}, 'normalization': 'Uniform scaling per character/action, 256-square canvas, feet at y=232; nearest-neighbor only.'}

def bands(values, minimum=3, gap=12):
    runs=[]; start=None; last=None
    for i,v in enumerate(values):
        if v >= minimum:
            if start is None: start=i
            last=i
        elif start is not None and i-last>gap:
            runs.append((start,last+1)); start=last=None
    if start is not None:runs.append((start,last+1))
    return runs

def components(im, threshold=40):
    w,h=im.size;mask=bytearray(1 if a>threshold else 0 for a in im.getchannel('A').getdata());found=[]
    for index in range(w*h):
        if not mask[index]:continue
        stack=[index];mask[index]=0;count=0;left=w;top=h;right=bottom=0
        while stack:
            j=stack.pop();x=j%w;y=j//w;count+=1;left=min(left,x);right=max(right,x);top=min(top,y);bottom=max(bottom,y)
            for k in (j-1 if x else -1,j+1 if x<w-1 else -1,j-w if y else -1,j+w if y<h-1 else -1):
                if k>=0 and mask[k]:mask[k]=0;stack.append(k)
        if count>400:found.append((count,(left,top,right+1,bottom+1)))
    return found

def extract_grid(im):
    w,h=im.size
    objects=sorted(components(im),reverse=True)[:20]
    if len(objects)!=20:raise ValueError(f'Expected 20 connected character silhouettes, got {len(objects)}')
    objects.sort(key=lambda o:(o[1][1]+o[1][3])/2)
    ordered=[];rows=[]
    for row in range(4):
        group=sorted(objects[row*5:row*5+5],key=lambda o:o[1][0])
        rows.append([o[1] for o in group])
        for _,(left,top,right,bottom) in group:
            assert left>0 and top>0 and right<w and bottom<h,'Source silhouette touches edge'
            cell=im.crop((max(0,left-3),max(0,top-3),min(w,right+3),min(h,bottom+3)))
            box=cell.getchannel('A').point(lambda v:255 if v>16 else 0).getbbox()
            ordered.append(cell.crop(box))
    return ordered,rows

for action,path in sources.items():
    source=Path(path);source=source if source.is_absolute() else ROOT/source
    im=Image.open(source).convert('RGBA')
    dest=ROOT/'Assets/Art/Sources'/f'{action}-source.png'
    if source.resolve()!=dest.resolve():shutil.copyfile(source,dest)
    if action=='Arena':continue
    print("Extracting",action,flush=True)
    sprites,rows=extract_grid(im)
    report['sources'][action]={'size':im.size,'detected_row_bounds':rows}
    for p,name in enumerate(('Fern','Ember')):
        cells=sprites[p*10:p*10+10]
        scale=min(200/cells[0].height,224/max(c.width for c in cells),208/max(c.height for c in cells))
        folder=ROOT/'Assets/Art/Characters'/name/action;folder.mkdir(parents=True,exist_ok=True)
        for n,cell in enumerate(cells,1):
            size=(max(1,round(cell.width*scale)),max(1,round(cell.height*scale)))
            cell=cell.resize(size,Image.Resampling.NEAREST)
            canvas=Image.new('RGBA',(256,256));canvas.alpha_composite(cell,((256-size[0])//2,232-size[1]))
            out=folder/f'{n:02}.png';canvas.save(out)
            bounds=canvas.getchannel('A').getbbox()
            assert bounds and bounds[0]>=12 and bounds[1]>=12 and bounds[2]<=244 and bounds[3]<=233,(out,bounds)
            report['frames'].append({'path':str(out.relative_to(ROOT)), 'size':canvas.size, 'bounds':bounds, 'sha256':hashlib.sha256(out.read_bytes()).hexdigest()})

# Find the two large connected silhouettes in the arena kit, including their decorations.
arena_source=Path(sources['Arena']);arena_source=arena_source if arena_source.is_absolute() else ROOT/arena_source
im=Image.open(arena_source).convert('RGBA');w,h=im.size
mask=bytearray(1 if a>32 else 0 for a in im.getchannel('A').getdata()); objects=[]
for index in range(w*h):
    if not mask[index]:continue
    stack=[index];mask[index]=0;count=0;left=w;top=h;right=bottom=0
    while stack:
        j=stack.pop();x=j%w;y=j//w;count+=1;left=min(left,x);right=max(right,x);top=min(top,y);bottom=max(bottom,y)
        for k in (j-1 if x else -1,j+1 if x<w-1 else -1,j-w if y else -1,j+w if y<h-1 else -1):
            if k>=0 and mask[k]:mask[k]=0;stack.append(k)
    if count>1500:objects.append((count,(left,top,right+1,bottom+1)))
objects=sorted(sorted(objects,reverse=True)[:2],key=lambda v:v[1][1])
assert len(objects)==2,objects
for name,(_,box) in zip(('MossboundPlank','RunewoodSphere'),objects):
    binary=im.getchannel('A').point(lambda a:255 if a>32 else 0)
    seed=next((x,box[1]) for x in range(box[0],box[2]) if binary.getpixel((x,box[1])))
    ImageDraw.floodfill(binary,seed,128)
    selected=binary.point(lambda a:255 if a==128 else 0).filter(ImageFilter.MaxFilter(7))
    isolated=im.copy();isolated.putalpha(ImageChops.multiply(im.getchannel('A'),selected))
    crop=isolated.crop((max(0,box[0]-4),max(0,box[1]-4),min(w,box[2]+4),min(h,box[3]+4)))
    # Preserve generated alpha and downsample only with nearest-neighbor to readable pixel clusters.
    target=800 if name=='MossboundPlank' else 256
    ratio=target/crop.width;crop=crop.resize((target,round(crop.height*ratio)),Image.Resampling.NEAREST)
    canvas=Image.new('RGBA',(crop.width+24,crop.height+24));canvas.alpha_composite(crop,(12,12));canvas.save(ROOT/'Assets/Art/Arena'/f'{name}.png')

# Code-native, nine-slice-compatible pixel UI frame.
panel=Image.new('RGBA',(128,64),(12,31,29,255));d=ImageDraw.Draw(panel)
d.rectangle((1,1,126,62),outline=(40,27,20,255),width=3)
d.rectangle((4,4,123,59),outline=(171,126,58,255),width=2)
d.rectangle((7,7,120,56),outline=(70,88,52,255),width=1)
for x,y in ((4,4),(116,4),(4,52),(116,52)):
    d.rectangle((x,y,x+7,y+7),fill=(52,72,44,255));d.rectangle((x+2,y+2,x+5,y+5),fill=(218,174,82,255))
d.line((13,9,114,9),fill=(28,52,40,255),width=1)
panel.save(ROOT/'Assets/Art/UI/WoodlandPanel.png')
# Human-readable contact sheet of the final independent frame assets.
thumb=Image.new('RGBA',(10*128,12*144),(15,30,28,255));d=ImageDraw.Draw(thumb)
for row,(name,action) in enumerate((n,a) for n in ('Fern','Ember') for a in sources if a!='Arena'):
    for i in range(10):
        frame=Image.open(ROOT/f'Assets/Art/Characters/{name}/{action}/{i+1:02}.png').resize((128,128),Image.Resampling.NEAREST)
        thumb.alpha_composite(frame,(i*128,row*144+16))
    d.text((4,row*144),f'{name} / {action}',fill=(245,219,166,255))
thumb.save(ROOT/'Documentation/CharacterFrames-v2.png')
report['total']=len(report['frames']);assert report['total']==120
(ROOT/'Documentation/SPRITE-VALIDATION.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'frames':report['total'],'source_sizes':{k:v['size'] for k,v in report['sources'].items()},'arena':objects},indent=2))
