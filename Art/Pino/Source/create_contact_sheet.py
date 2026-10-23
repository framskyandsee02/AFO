from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[3]
art=root/'Art/Pino'
out=art/'Morphing/Pino_MorphOverview.png'
out.parent.mkdir(exist_ok=True)
w,h=1680,1060
sheet=Image.new('RGB',(w,h),'#f3f8f8')
draw=ImageDraw.Draw(sheet)
font_path='C:/Windows/Fonts/segoeui.ttf'
bold_path='C:/Windows/Fonts/seguisb.ttf'
title=ImageFont.truetype(bold_path,40)
label=ImageFont.truetype(bold_path,23)
small=ImageFont.truetype(font_path,19)
draw.text((40,25),'PINO / ADAPTIVE SLIME',font=title,fill='#164e61')
draw.text((42,81),'Existing character mesh · Surface-attached eyes · Ten editable morph targets',font=small,fill='#4c7480')
items=[('Normal','NORMAL'),('Magnet','MAGNET'),('Spring','SPRING'),('Gas','GAS'),('SpringCompressed','SPRING / COMPRESS'),('SpringStretched','SPRING / STRETCH'),('Liquid','LIQUID'),('LiquidFlow','LIQUID / FLOW')]
for i,(stem,caption) in enumerate(items):
    x=28+(i%4)*414; y=128+(i//4)*451
    draw.rounded_rectangle((x,y,x+398,y+434),radius=16,fill='white',outline='#dce9ec',width=2)
    image=Image.open(art/'MorphPreviews'/('Pino_'+stem+'.png')).convert('RGB').resize((374,374),Image.Resampling.LANCZOS)
    sheet.paste(image,(x+12,y+12))
    draw.text((x+20,y+395),caption,font=label,fill='#24586b')
sheet.save(out)
print(out)
