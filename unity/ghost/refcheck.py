#!/usr/bin/env python3
# Призрак: Unity қабатындағы сілтемелерді тексеру — Ui./Snd./Inp. мүшелері және белгісіз әдіс шақырулары.
# (lint.py тек Core мен Unity API-ді тексереді; өз класстарымыздағы жоқ әдістерді осы ұстайды.)
import re, glob, os, sys
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'SHAIQAS', 'Assets', 'Scripts')
files = glob.glob(R + '/Game/*.cs') + glob.glob(R + '/Core/*.cs') + glob.glob(R + '/Platform/*.cs')
src = {f: open(f, encoding='utf-8').read() for f in files}
def strip(s): return re.sub(r'//.*', '', re.sub(r'"(?:\\.|[^"\\])*"', '""', s))
def members(fn):
    s = src[os.path.join(R, 'Game', fn)]; out = set(re.findall(r'public (?:static |readonly |const )*[\w<>\[\],\.]+ (\w+)\s*[\(=;,]', s))
    for decl in re.findall(r'public (?:static |readonly |const )+[\w<>\[\]\.]+ ([^;{]+);', s):
        depth, cur, parts = 0, '', []
        for ch in decl:
            if ch in '([{': depth += 1
            elif ch in ')]}': depth -= 1
            if ch == ',' and depth == 0: parts.append(cur); cur = ''
            else: cur += ch
        parts.append(cur)
        for part in parts: out.add(part.split('=')[0].strip())
    return out
M = {c: members(c + '.cs') for c in ('Ui', 'Snd', 'Inp')}
allsrc = '\n'.join(src.values())
defined = set(re.findall(r'\b(?:void|bool|int|float|string|[A-Z]\w*(?:<\w+>)?(?:\[\])?|System\.Collections\.IEnumerator)\s+(\w+)\s*\(', allsrc)) | set(re.findall(r'\bclass (\w+)', allsrc))
ok = set('Mathf Debug Color Color32 Vector2 Vector3 Rect GUI GUIContent GUIStyle Quaternion Time Resources Sprite Texture2D Material Shader Mesh GameObject Object Camera Screen Application RenderTexture Graphics QualitySettings Input KeyCode Event Destroy StartCoroutine WaitForEndOfFrame RuntimeInitializeOnLoadMethod'.split())
bad = []
for f in glob.glob(R + '/Game/*.cs'):
    s = strip(src[f]); b = os.path.basename(f)
    for c in M:
        for n in re.findall(r'\b' + c + r'\.(\w+)', s):
            if n not in M[c]: bad.append(f'{b}: {c}.{n} жоқ')
    for n in re.findall(r'(?<![\.\w])([A-Z]\w*)\s*\(', s):
        if n not in defined and n not in ok: bad.append(f'{b}: {n}(...) табылмады')
for x in sorted(set(bad)): print(x)
print('refcheck: ' + ('таза ✔' if not bad else f'{len(set(bad))} ескерту'))
sys.exit(1 if bad else 0)
