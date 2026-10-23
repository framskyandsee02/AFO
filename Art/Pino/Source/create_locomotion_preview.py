"""Package the Unity 'Capture Walk and Idle Preview' frames as an animated review artifact."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[3]
sources = sorted((root / 'Temp/PinoLocomotionFrames').glob('frame_*.png'))
if not sources:
    raise SystemExit('Run Tools > Pino Character > Capture Walk and Idle Preview in Unity first.')
font = ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf', 24)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
frames = []
for source in sources:
    frame = Image.new('RGB', (960, 610), '#eef7f9')
    frame.paste(Image.open(source).convert('RGB'), (0, 54))
    draw = ImageDraw.Draw(frame)
    draw.text((166, 13), 'IDLE / JELLY WIGGLE', font=font, fill='#215770')
    draw.text((617, 13), 'WALK / TINY HOPS', font=font, fill='#215770')
    draw.text((323, 584), 'PINO  ·  Unity animation preview', font=small, fill='#3b7185')
    frames.append(frame)

# Reuse a shared palette to keep the background and highlights stable between frames.
sample = Image.new('RGB', (960, 610 * 8))
for i in range(8):
    sample.paste(frames[i * (len(frames) - 1) // 7], (0, 610 * i))
palette = sample.quantize(colors=240)
indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
destination = root / 'Art/Pino/Morphing/Pino_Idle_Walk.gif'
indexed[0].save(destination, save_all=True, append_images=indexed[1:], duration=42, loop=0, optimize=True)
frames[10].save(destination.with_suffix('.png'))
print(destination)
