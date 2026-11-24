#!/usr/bin/env python3
"""Призрачный Юнити: C# (ойынның Core бөлігі) → Java аудармашысы.
Core тек шектеулі C# жиынын қолданады (README-де жазылған ережелер). Мақсат — javac арқылы
нақты компиляция және ойын логикасын бұлтта жүргізу (Unity жоқ жерде)."""
import sys, re, os
from pygments.lexers import CSharpLexer
from pygments.token import Token

TYPE_MAP = {'bool': 'boolean', 'string': 'String', 'object': 'Object'}
STRIP = {'sealed', 'virtual', 'override', 'readonly_skip', 'internal', 'partial'}

def toks(src):
    out = []
    for tt, v in CSharpLexer().get_tokens(src):
        out.append([tt, v])
    return out

def interp(s):
    # $"a{b}c" → ("a" + (b) + "c")
    body = s[2:-1]; parts = []; cur = ''; i = 0
    while i < len(body):
        c = body[i]
        if c == '{' and i + 1 < len(body) and body[i+1] == '{': cur += '{'; i += 2; continue
        if c == '}' and i + 1 < len(body) and body[i+1] == '}': cur += '}'; i += 2; continue
        if c == '{':
            j = i + 1; d = 1
            while d:
                if body[j] == '{': d += 1
                elif body[j] == '}': d -= 1
                j += 1
            expr = body[i+1:j-1]
            if ':' in expr and '?' not in expr: raise SystemExit('format spec in interpolation not supported: ' + expr)
            parts.append('"' + cur + '"'); parts.append('(' + expr + ')'); cur = ''; i = j; continue
        cur += c; i += 1
    parts.append('"' + cur + '"')
    return '(' + ' + '.join(parts) + ')'

def convert(src, fname):
    T = toks(src)
    out = []; depth = 0; class_depths = []  # brace depth at which each class body opened
    pending_class = None
    i = 0
    def nxt(k):
        j = k + 1
        while j < len(T) and T[j][0] in Token.Text: j += 1
        return j
    def prv(k):
        j = k - 1
        while j >= 0 and (T[j][0] in Token.Text or T[j][0] in Token.Comment): j -= 1
        return j
    while i < len(T):
        tt, v = T[i]
        if tt in Token.Comment: out.append(' ' if '\n' not in v else '\n'); i += 1; continue
        if tt in Token.String and v.startswith('$"'): out.append(interp(v)); i += 1; continue
        if tt in Token.String and v.startswith('@"'): raise SystemExit(fname + ': verbatim strings not allowed in Core')
        if v == 'using' and depth == 0:
            while T[i][1] != ';': i += 1
            i += 1; continue
        if v == 'namespace': raise SystemExit(fname + ': namespace not allowed in Core')
        if tt in Token.Keyword or tt in Token.Keyword.Type or tt in Token.Name:
            if v in TYPE_MAP and T[nxt(i)][1] != '(':
                out.append(TYPE_MAP[v]); i += 1; continue
            if v in ('sealed', 'virtual', 'override', 'internal', 'partial'): i = nxt(i); continue
            if v == 'readonly': out.append('final'); i += 1; continue
            if v == 'const': out.append('static final'); i += 1; continue
            if v == 'base': out.append('super'); i += 1; continue
            if v == 'foreach':
                # foreach (T x in e) → for (T x : e)
                out.append('for'); i += 1
                j = i; d = 0
                while True:
                    if T[j][1] == '(': d += 1
                    if T[j][1] == ')':
                        d -= 1
                        if d == 0: break
                    if d == 1 and T[j][1] == 'in' and T[j][0] in Token.Keyword: T[j][1] = ':'
                    j += 1
                continue
            if v in ('class', 'interface'):
                pending_class = v
                # top-level: «static» алып тастау; ішкі класс: static қосу
                j = prv(i); mods = []
                k = j
                while k >= 0 and T[k][1] in ('public', 'static', 'sealed', 'abstract', 'private', 'protected', 'internal', 'partial'):
                    mods.append(k); k = prv(k)
                inner = len(class_depths) > 0
                # rebuild modifiers in out: remove already emitted ones
                emitted = ''.join(out)
                # remove trailing modifier words from out
                while out and (out[-1].strip() in ('public', 'static', 'abstract', 'private', 'protected', '') ):
                    w = out.pop()
                    if w.strip() == 'abstract': mods_abs = True
                ab = any(T[m][1] == 'abstract' for m in mods)
                if inner: out.append(' public static ' + ('abstract ' if ab else ''))
                else: out.append(('abstract ' if ab else '') + ('final ' if any(T[m][1]=='sealed' for m in mods) and False else ''))
                out.append(v); i += 1
                # name + optional base list
                j = nxt(i); out.append(' ' + T[j][1]); i = j + 1
                j = nxt(i)
                if T[j][1] == ':':
                    i = j + 1; bases = []
                    while T[nxt(i)][1] != '{':
                        i = nxt(i)
                        if T[i][1] != ',': bases.append(T[i][1])
                        i += 1
                    ext = [b for b in bases if not re.match(r'I[A-Z]', b)]; imp = [b for b in bases if re.match(r'I[A-Z]', b)]
                    if ext: out.append(' extends ' + ext[0])
                    if imp: out.append((' extends ' if v == 'interface' else ' implements ') + ', '.join(imp))
                continue
        if v == '{':
            depth += 1
            if pending_class: class_depths.append(depth); pending_class = None
            out.append(v); i += 1; continue
        if v == '}':
            if class_depths and class_depths[-1] == depth: class_depths.pop()
            depth -= 1; out.append(v); i += 1; continue
        if v == '.' and i + 1 < len(T):
            j = i + 1; w = T[j][1]
            if w == 'Length': out.append('.length'); i = j + 1; continue
            if w == 'Count' and T[nxt(j)][1] != '(': out.append('.Count()'); i = j + 1; continue
            if w in ('Equals', 'ToString', 'GetHashCode') and T[nxt(j)][1] == '(': out.append('.' + w[0].lower() + w[1:]); i = j + 1; continue
        if v == 'new' and T[nxt(i)][1] == 'Exception': out.append('new RuntimeException'); i = nxt(i) + 1; continue
        if v == 'catch' and T[nxt(i)][1] == '{': out.append('catch (Exception __e)'); i += 1; continue
        out.append(v); i += 1
    return ''.join(out)

if __name__ == '__main__':
    src_dir, dst = sys.argv[1], sys.argv[2]
    os.makedirs(dst, exist_ok=True)
    buf = []
    for root, _, files in os.walk(src_dir):
        for f in sorted(files):
            if f.endswith('.cs'):
                p = os.path.join(root, f)
                buf.append('// ---- ' + f + '\n' + convert(open(p, encoding='utf-8').read(), f))
    open(os.path.join(dst, 'Core.java'), 'w', encoding='utf-8').write('import java.util.*;\n' + '\n'.join(buf))
    print('ok', len(buf), 'files')
