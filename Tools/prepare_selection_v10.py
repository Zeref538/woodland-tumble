"""Package selection artwork without painting or changing generated pixels."""
from pathlib import Path
from PIL import Image
import json
ROOT=Path(__file__).resolve().parents[1]
NAMES=('Fern','Ember','Luna','Bram','Zephyr','Volt','Nyx','Marin')

def package(name):
    src=ROOT/'Documentation/Selection-v10'
    sheet=Image.open(src/f'{name}-Idle.png').convert('RGBA')
    cw,ch=sheet.width//2,sheet.height//2
    cells=[sheet.crop((x*cw,y*ch,(x+1)*cw,(y+1)*ch)) for y in range(2) for x in range(2)]
    boxes=[im.getchannel('A').point(lambda a:255 if a>48 else 0).getbbox() for im in cells]
    assert all(boxes),name
    # One common canvas, original pixel scale, feet anchored consistently.
    target=ROOT/'Assets/Art/SelectionV10'/name
    target.mkdir(parents=True,exist_ok=True)
    size=max(cw,ch,656)
    for i,(im,b) in enumerate(zip(cells,boxes),1):
        out=Image.new('RGBA',(size,size))
        feet=im.getchannel('A').crop((0,max(0,b[3]-round((b[3]-b[1])*.12)),im.width,b[3])).point(lambda a:255 if a>96 else 0).getbbox()
        anchor=(feet[0]+feet[2])/2 if feet else (b[0]+b[2])/2
        out.alpha_composite(im,(round(size/2-anchor),size-20-b[3]))
        out.save(target/f'{i:02}.png')
    portrait=Image.open(src/f'{name}-Portrait.png').convert('RGBA')
    target=ROOT/'Assets/Art/SelectionV10/Portraits';target.mkdir(parents=True,exist_ok=True)
    portrait.save(target/f'{name}.png')
    return {'fighter':name,'sourceSize':sheet.size,'cellSize':[cw,ch],'importCanvas':size,'alphaBounds':boxes,'uniquePoses':len(set(im.tobytes() for im in cells)),'portraitSize':portrait.size}

if __name__=='__main__':
    report=[package(n) for n in NAMES]
    (ROOT/'Documentation/SELECTION-ART-v10.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))
