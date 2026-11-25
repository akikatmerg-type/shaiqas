"""Папкадағы PNG-ді бір атласқа жинау (сөре әдісі). Шығыс: атлас PNG + {w,h,r:{name:[x,y,w,h]}} JSON.
Қолдану: python3 pack_atlas.py <src_dir> <out.png> <out.json> [max_w]"""
import sys, json, os
from PIL import Image
src, out_png, out_json = sys.argv[1], sys.argv[2], sys.argv[3]
MW = int(sys.argv[4]) if len(sys.argv) > 4 else 4096
ims = [(f[:-4], Image.open(os.path.join(src, f)).convert('RGBA')) for f in sorted(os.listdir(src)) if f.endswith('.png')]
ims.sort(key=lambda t: -t[1].height)
x = y = sh = 0; pos = {}; P = 2
for n, im in ims:
    if x + im.width + P > MW: x = 0; y += sh + P; sh = 0
    pos[n] = [x, y, im.width, im.height]; x += im.width + P; sh = max(sh, im.height)
H = y + sh; A = Image.new('RGBA', (MW, H), (0, 0, 0, 0))
for n, im in ims: A.paste(im, (pos[n][0], pos[n][1]))
A.save(out_png, optimize=True)
json.dump({'w': MW, 'h': H, 'r': pos}, open(out_json, 'w', encoding='utf-8'))
print(len(ims), 'images ->', MW, 'x', H)
