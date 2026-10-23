"""Package the Unity all-form locomotion frames for review."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[3]
sources = sorted((root / 'Temp/PinoAllFormsLocomotionFrames').glob('frame_*.png'))
if not sources:
    raise SystemExit('Run Tools > Pino Character > Capture Walk and Idle Preview in Unity first.')
title = ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf', 29)
font = ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 19)
forms = ('NORMAL', 'MAGNET', 'SPRING', 'GAS', 'LIQUID')
frames = []
for source in sources:
    frame = Image.new('RGB', (1500, 760), '#eff7f9')
    frame.paste(Image.open(source).convert('RGB').crop((0, 150, 1500, 760)), (0, 110))
    draw = ImageDraw.Draw(frame)
    draw.text((36, 15), 'PINO / IDLE & WALK IN EVERY FORM', font=title, fill='#215770')
    for x, form in zip((216, 483, 750, 1017, 1284), forms):
        draw.text((x - draw.textlength(form, font=font) / 2, 67), form, font=font, fill='#215770')
    draw.text((22, 185), 'IDLE', font=font, fill='#215770')
    draw.text((22, 450), 'WALK', font=font, fill='#215770')
    draw.text((491, 730), 'Unity preview  /  gentle motion tailored to each ability', font=small, fill='#3b7185')
    frames.append(frame)
sample = Image.new('RGB', (1500, 760 * 8))
for i in range(8):
    sample.paste(frames[i * (len(frames) - 1) // 7], (0, 760 * i))
palette = sample.quantize(colors=240)
indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
destination = root / 'Art/Pino/Morphing/Pino_AllForms_Idle_Walk.gif'
indexed[0].save(destination, save_all=True, append_images=indexed[1:], duration=42, loop=0, optimize=True)
frames[10].save(destination.with_suffix('.png'))
print(destination)
