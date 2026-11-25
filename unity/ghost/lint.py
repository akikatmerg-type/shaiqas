#!/usr/bin/env python3
"""Призрачный Юнити · C# тексеруші (Java компиляциясы ұстамайтын қателер үшін).
 1) жақшалар теңгерімі            2) 'f'-сіз бөлшек сан (double→float қатесі)
 3) System.Math (double қайтарады)  4) CS0136: бір атты айнымалы ішкі/сыртқы аймақта
 5) switch: case-тен case-ке өту     6) Core класының жоқ мүшесіне сілтеме
 7) Unity API: белгісіз мүше (ақ тізім бойынша)"""
import sys, os, re, json
from pygments.lexers import CSharpLexer
from pygments.token import Token

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'SHAIQAS', 'Assets', 'Scripts')
errs = []
def E(f, line, msg): errs.append(f'{os.path.relpath(f, ROOT)}:{line}: {msg}')

def tokens(src):
    out = []; line = 1
    for tt, v in CSharpLexer().get_tokens(src):
        if tt in Token.Comment or (tt in Token.Text and v.strip() == ''):
            line += v.count('\n'); continue
        # pygments кейде көп таңбалы операторларды біріктіреді — бөлек тексеру қажет емес
        out.append((tt, v, line)); line += v.count('\n')
    return out

# ---------- Core символдар кестесі ----------
def core_symbols():
    syms = {}
    for root, _, files in os.walk(os.path.join(ROOT, 'Core')):
        for fn in files:
            if not fn.endswith('.cs'): continue
            T = tokens(open(os.path.join(root, fn), encoding='utf-8').read())
            depth = 0; stack = []  # (class, depth)
            for i, (tt, v, ln) in enumerate(T):
                if v == 'class' or v == 'interface':
                    name = T[i+1][1]; stack.append([name, None]); syms.setdefault(name, set()); pend = name
                if v == '{':
                    depth += 1
                    if stack and stack[-1][1] is None: stack[-1][1] = depth
                if v == '}':
                    if stack and stack[-1][1] == depth: stack.pop()
                    depth -= 1
                if stack and depth == stack[-1][1] and tt in Token.Name:
                    nxt = T[i+1][1] if i+1 < len(T) else ''
                    if nxt in ('(', '=', ';', ',', '<') or (nxt == '{' and False):
                        syms[stack[-1][0]].add(v)
    # Lst<T>, JNode generics etc already included
    return syms

UNITY = {
 'Mathf': 'Rad2Deg Min Max Clamp Clamp01 Abs Sin Cos Sqrt Atan2 Floor Ceil Round RoundToInt FloorToInt CeilToInt Lerp PI Pow Exp Sign Infinity',
 'Screen': 'width height orientation sleepTimeout',
 'Application': 'targetFrameRate dataPath isEditor isMobilePlatform platform',
 'Time': 'unscaledDeltaTime deltaTime unscaledTime time',
 'GUI': 'color DrawTexture DrawTextureWithTexCoords Label Button TextField enabled depth skin matrix BeginClip EndClip',
 'Resources': 'Load UnloadAsset',
 'Shader': 'Find',
 'PlayerPrefs': 'HasKey GetString SetString Save DeleteKey',
 'Input': 'GetKey GetTouch touchCount mousePosition GetMouseButton',
 'Color': 'black white clear Lerp',
 'Vector2': 'zero one',
 'Vector3': 'zero one',
 'TextAnchor': 'UpperLeft UpperCenter MiddleLeft MiddleCenter MiddleRight UpperRight',
 'Sprite': 'Create',
 'Object': 'FindFirstObjectByType FindAnyObjectByType DontDestroyOnLoad Destroy',
 'TextureFormat': 'RGBA32 RGB24',
 'FilterMode': 'Bilinear Point',
 'TextureWrapMode': 'Clamp',
 'SleepTimeout': 'NeverSleep',
 'CameraClearFlags': 'SolidColor',
 'RuntimeInitializeLoadType': 'AfterSceneLoad BeforeSceneLoad',
 'PlayerSettings': 'productName colorSpace companyName defaultInterfaceOrientation allowedAutorotateToPortrait allowedAutorotateToPortraitUpsideDown allowedAutorotateToLandscapeLeft allowedAutorotateToLandscapeRight defaultScreenWidth defaultScreenHeight',
 'UIOrientation': 'AutoRotation LandscapeLeft',
 'AssetDatabase': 'LoadAssetAtPath IsValidFolder CreateFolder CreateAsset SaveAssets ImportAsset',
 'EditorSceneManager': 'GetActiveScene NewScene SaveScene',
 'EditorBuildSettings': 'scenes',
 'NewSceneSetup': 'DefaultGameObjects EmptyScene',
 'NewSceneMode': 'Single Additive',
 'EditorApplication': 'delayCall',
 'SessionState': 'GetBool SetBool',
 'TextureImporterType': 'Default Sprite',
 'TextureImporterNPOTScale': 'None',
 'TextureImporterCompression': 'Uncompressed Compressed CompressedHQ',
 'ImportAssetOptions': 'ImportRecursive ForceUpdate',
 'Debug': 'Log LogWarning LogError',
 'GUIContent': 'none',
 'GUIStyle': 'none',
 'KeyCode': 'A D W S J K L LeftArrow RightArrow UpArrow DownArrow Space Return Escape F12 F11 F10 U',
 'TouchPhase': 'Ended Canceled',
 'Keyboard': 'current', 'Touchscreen': 'current', 'Mouse': 'current',
 'Texture2D': 'whiteTexture',
 'TextClipping': 'Overflow Clip',
 'System': 'Array Random Math IO DateTime Collections',
 'ScreenCapture': 'CaptureScreenshot',
 'Array': 'Copy',
 'TextureFormat': 'RGBA32 RGB24',
}
UNITY = {k: set(v.split()) for k, v in UNITY.items()}

TYPEWORDS = {'int', 'float', 'bool', 'string', 'var', 'double', 'char', 'long', 'object', 'byte'}

def lint_file(path, core, is_core):
    src = open(path, encoding='utf-8').read()
    T = tokens(src)
    # 1) жақшалар
    bal = {'(': 0, '[': 0, '{': 0}; pair = {')': '(', ']': '[', '}': '{'}
    for tt, v, ln in T:
        if tt in Token.String: continue
        for ch in v:
            if ch in bal: bal[ch] += 1
            elif ch in pair:
                bal[pair[ch]] -= 1
                if bal[pair[ch]] < 0: E(path, ln, f'артық "{ch}"'); bal[pair[ch]] = 0
    for k, n in bal.items():
        if n: E(path, T[-1][2], f'жабылмаған "{k}" ×{n}')
    # 2) бөлшек сан (Core-ды javac тексереді)
    for i, (tt, v, ln) in (enumerate(T) if not is_core else []):
        if tt in Token.Number and re.fullmatch(r'\d*\.\d+([eE][-+]?\d+)?', v):
            nxt = T[i+1][1] if i+1 < len(T) else ''
            if nxt not in ('f', 'F', 'd', 'D', 'm'):  # pygments суффиксті бөлек бере алады
                E(path, ln, f'бөлшек сан "{v}" — "f" жоқ (double)')
    # 3) Math.
    if not path.endswith('U.cs'):
        for i, (tt, v, ln) in enumerate(T):
            if v == 'Math' and T[i+1][1] == '.' and (i == 0 or T[i-1][1] != '.'): E(path, ln, 'System.Math қолданылған — Mathf керек')
    # 3б) Mathf → using UnityEngine керек (призракта Mathf — заглушка, сондықтан Java мұны көрмейді)
    if any(v == 'Mathf' for _, v, _ in T) and 'using UnityEngine;' not in src: E(path, 1, 'Mathf бар, бірақ "using UnityEngine;" жоқ')
    # 4) CS0136 және 5) switch
    scope_check(path, T)
    switch_check(path, T)
    # 6–7) мүшелер
    if not is_core: member_check(path, T, core, is_core)

def scope_check(path, T):
    # әр әдіс денесі ішінде: аймақтар ағашы және жарияланымдар
    i = 0; n = len(T)
    depth = 0; class_depth = []
    while i < n:
        tt, v, ln = T[i]
        if v in ('class', 'struct', 'interface'):
            # келесі '{' класс денесі
            j = i
            while T[j][1] != '{': j += 1
            class_depth.append(depth + 1); depth += 1; i = j + 1; continue
        if v == '{':
            # әдіс денесі ме? алдыңғы маңызды таңба ')' болса және тереңдік класс денесінде
            if class_depth and depth == class_depth[-1] and T[i-1][1] in (')',) :
                end = match_brace(T, i)
                analyze_body(path, T, i, end)
                i = end + 1; continue
            depth += 1; i += 1; continue
        if v == '}':
            if class_depth and class_depth[-1] == depth: class_depth.pop()
            depth -= 1
        i += 1

def match_brace(T, i):
    d = 0
    for j in range(i, len(T)):
        if T[j][1] == '{': d += 1
        elif T[j][1] == '}':
            d -= 1
            if d == 0: return j
    return len(T) - 1

def analyze_body(path, T, a, b):
    # аймақтар: '{' ... '}' және for/foreach жақшасы
    scopes = [{'parent': None, 'names': {}}]; cur = 0
    stack = [0]
    pending_for = []
    i = a + 1
    def decl(name, ln):
        # ата-бабаларда бар ма?
        s = stack[-1]; k = s
        while k is not None:
            if name in scopes[k]['names'] and k != s:
                E(path, ln, f'CS0136: "{name}" сыртқы аймақта бұрын жарияланған (жол {scopes[k]["names"][name]})'); break
            k = scopes[k]['parent']
        if name in scopes[s]['names'] and scopes[s]['names'][name] != ln:
            E(path, ln, f'CS0128: "{name}" осы аймақта қайта жарияланды')
        scopes[s]['names'].setdefault(name, ln)
        # ұрпақтарда бар ма? (кейін жарияланған сыртқы — тексеру соңында)
    while i < b:
        tt, v, ln = T[i]
        if v in ('for', 'foreach', 'using') and T[i+1][1] == '(':
            scopes.append({'parent': stack[-1], 'names': {}}); stack.append(len(scopes) - 1)
            # жақша соңында, денесі '{' болса — сол аймақ for аймағының ішінде
            d = 0; j = i + 1
            while True:
                if T[j][1] == '(': d += 1
                if T[j][1] == ')':
                    d -= 1
                    if d == 0: break
                j += 1
            pending_for.append((j, T[j+1][1] == '{'))
            i += 2; continue
        if pending_for and i == pending_for[-1][0]:
            jend, braced = pending_for.pop()
            if not braced:
                # бір жолдық дене: ';' дейін, сосын аймақты жабу
                k = i + 1; d = 0
                while not (T[k][1] == ';' and d == 0):
                    if T[k][1] in '({[': d += 1
                    if T[k][1] in ')}]': d -= 1
                    k += 1
                scopes[stack[-1]]['close_at'] = k
            else:
                scopes[stack[-1]]['brace_for'] = True
            i += 1; continue
        if v == '{':
            if scopes[stack[-1]].get('brace_for') and not scopes[stack[-1]].get('opened'):
                scopes[stack[-1]]['opened'] = True
            else:
                scopes.append({'parent': stack[-1], 'names': {}}); stack.append(len(scopes) - 1)
        elif v == '}':
            s = stack.pop()
            if scopes[s].get('brace_for') and len(stack) > 0:
                pass  # for аймағы да жабылды (бір pop жеткілікті, себебі '{' жаңа аймақ ашпады)
        # жариялау: TYPE NAME (=|;|,|:|in|)) ; немесе out TYPE NAME
        if tt in Token.Name or v in TYPEWORDS or tt in Token.Keyword.Type:
            j = i + 1
            # генерик/массив түрі
            if j < b and T[j][1] == '<':
                d = 0; ok = False; j0 = j
                while j < b and j - j0 < 12:
                    w = T[j][1]
                    if w == '<': d += 1
                    elif w == '>': d -= 1
                    elif w == '>>': d -= 2
                    elif not (T[j][0] in Token.Name or w in (',', '[', ']', '[]') or w in TYPEWORDS or T[j][0] in Token.Keyword.Type): break
                    j += 1
                    if d <= 0: ok = True; break
                if not ok: i += 1; continue
            while j < b and T[j][1] == '[' and T[j+1][1] == ']': j += 2
            if j < b and T[j][1] == '[]': j += 1
            if j + 1 < b and T[j][0] in Token.Name and T[j+1][1] in ('=', ';', ',', 'in', ')') and T[i-1][1] not in ('.', 'new'):
                prev = T[i-1][1]
                if prev in ('(', ';', '{', '}', 'out', ',', ')') or T[i-1][0] in Token.Keyword:
                    if not (v[0].islower() and v not in TYPEWORDS):
                        # көп жариялау: int a = 1, b = 2
                        decl(T[j][1], T[j][2])
                        k = j + 1; d = 0
                        while k < b and not (T[k][1] == ';' and d == 0):
                            if T[k][1] in ('(', '[', '{'): d += 1
                            elif T[k][1] in (')', ']', '}'):
                                d -= 1
                                if d < 0: break
                            elif T[k][1] == ',' and d == 0 and T[k+1][0] in Token.Name and T[k+2][1] in ('=', ';', ','):
                                decl(T[k+1][1], T[k+1][2])
                            k += 1
        # бір жолдық for денесі аяқталды
        for s in list(stack[1:]):
            if scopes[s].get('close_at') == i: stack.remove(s)
        i += 1
    # кейін жарияланған сыртқы айнымалы (C#-та да қате)
    for s, sc in enumerate(scopes):
        p = sc['parent']
        while p is not None:
            for nm, ln in sc['names'].items():
                if nm in scopes[p]['names'] and scopes[p]['names'][nm] > ln:
                    E(path, scopes[p]['names'][nm], f'CS0136: "{nm}" ішкі аймақта (жол {ln}) бұрын қолданылған атау')
            p = scopes[p]['parent']

def switch_check(path, T):
    for i, (tt, v, ln) in enumerate(T):
        if v != 'switch': continue
        j = i
        while T[j][1] != '{': j += 1
        end = match_brace(T, j)
        d = 0; labels = []
        k = j + 1
        while k < end:
            w = T[k][1]
            if w in ('{', '(', '['): d += 1
            elif w in ('}', ')', ']'): d -= 1
            elif d == 0 and (w == 'case' or (w == 'default' and T[k+1][1] == ':')): labels.append(k)
            k += 1
        for li, k in enumerate(labels + [end]):
            if li == 0: continue
            # алдыңғы бөлімнің соңғы мәлімдемесі
            p = k - 1
            if T[p][1] == ':' : continue  # қатар белгілер
            # жабылатын жақшаларды өткізіп
            while T[p][1] == '}': p -= 1
            if T[p][1] != ';': E(path, T[k][2], 'switch: бөлім ";"-мен аяқталмаған'); continue
            q = p - 1
            while q > j and T[q][1] not in (';', '{', '}', ':'): q -= 1
            first = T[q+1][1]
            if first not in ('break', 'return', 'continue', 'throw', 'goto'):
                E(path, T[k][2], f'C#: case бөлімі break/return-сыз келесіге өтеді (соңғы мәлімдеме "{first}…")')

def member_check(path, T, core, is_core):
    # айнымалы → түр (Core кластары үшін)
    vt = {}
    for i, (tt, v, ln) in enumerate(T):
        if v in core and i + 1 < len(T) and T[i+1][0] in Token.Name and T[i-1][1] not in ('.', 'new', 'class'):
            vt[T[i+1][1]] = v
            # көп жариялау
            k = i + 2
            while k < len(T) and T[k][1] not in (';', ')', '{'):
                if T[k][1] == ',' and T[k+1][0] in Token.Name and T[k+2][1] in ('=', ';', ','): vt[T[k+1][1]] = v
                k += 1
    for i, (tt, v, ln) in enumerate(T[:-2]):
        if T[i+1][1] != '.' or T[i+2][0] not in Token.Name: continue
        if i > 0 and T[i-1][1] == '.': continue
        mem = T[i+2][1]
        cls = v if v in core else vt.get(v)
        if cls and v not in UNITY:
            if mem not in core[cls] and not (cls == 'Lst' and mem in ('Get', 'Add', 'Count')):
                E(path, T[i+2][2], f'Core: "{cls}" класында "{mem}" жоқ')
        elif not is_core and v in UNITY and v not in core:
            if mem not in UNITY[v]: E(path, T[i+2][2], f'Unity API: "{v}.{mem}" ақ тізімде жоқ (тексер)')

if __name__ == '__main__':
    core = core_symbols()
    core.pop('Game', None) if False else None
    for root, _, files in os.walk(ROOT):
        for fn in sorted(files):
            if fn.endswith('.cs'):
                p = os.path.join(root, fn)
                lint_file(p, core, '/Core/' in p)
    for e in errs: print(e)
    print(f'lint: {len(errs)} ескерту' if errs else 'lint: таза ✔')
