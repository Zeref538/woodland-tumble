"""Widen only the ink beside each baked label; keep lettering proportions."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT/'Assets/Art/UI/InkV15'


def run():
    DEST.mkdir(exist_ok=True)
    report = []
    for source in sorted((ROOT/'Assets/Art/UI/InkV14').glob('Button_*.png')):
        image = Image.open(source).convert('RGBA')
        for role, width, height in (('Menu',690,180),('Long',1164,180),('Wide',1980,170),('Short',655,180)):
            art=image.resize((round(image.width*height/image.height),height),Image.Resampling.LANCZOS)
            pixels=np.asarray(art);mask=(pixels[:,:,0]>180)&(pixels[:,:,1]>160)&(pixels[:,:,2]>100)&(pixels[:,:,3]>180)
            yy,xx=np.where(mask)
            left=max(1,int(xx.min())-6);right=min(art.width-1,int(xx.max())+7)
            core=art.crop((left,0,right,height));remaining=width-core.width
            if remaining<20:
                raise ValueError(f'{source.name}: insufficient ink gutter')
            out=Image.new('RGBA',(width,height));middle=(width-core.width)//2
            out.alpha_composite(art.crop((0,0,left,height)).resize((middle,height),Image.Resampling.LANCZOS))
            out.alpha_composite(core,(middle,0))
            out.alpha_composite(art.crop((right,0,art.width,height)).resize((width-middle-core.width,height),Image.Resampling.LANCZOS),(middle+core.width,0))
            actual=np.asarray(out)[:,middle:middle+core.width];expected=np.asarray(core)
            assert np.array_equal(actual[expected[:,:,3]>0],expected[expected[:,:,3]>0]), "Visible lettering changed"
            assert np.array_equal(actual[:,:,3],expected[:,:,3]), "Lettering alpha changed"
            target=DEST/(source.stem+'_'+role+'.png');out.save(target)
            report.append(dict(path=str(target.relative_to(ROOT)),source=str(source.relative_to(ROOT)),size=[width,height],protectedLettering=[left,right],uniformLetteringScale=height/image.height))
    (ROOT/'Documentation/BUTTON-WIDTHS-v15.json').write_text(json.dumps(report,indent=2))
    print(f'Prepared {len(report)} single-surface controls with unstretched lettering')


if __name__=='__main__':
    run()
