// SHAIQAS Core · Шағын JSON (оқу/жазу). Unity-ге тәуелді емес: деректер мен сақтау осы арқылы.
using System;
public class JNode
{
    public const int NUL = 0, BOOL = 1, NUM = 2, STR = 3, ARR = 4, OBJ = 5;
    public int kind = NUL;
    public bool b;
    public double n;
    public string s;
    public Lst<JNode> items;
    public Lst<string> keys;

    public static readonly JNode Null = new JNode();

    public static JNode NewObj() { JNode j = new JNode(); j.kind = OBJ; j.items = new Lst<JNode>(); j.keys = new Lst<string>(); return j; }
    public static JNode NewArr() { JNode j = new JNode(); j.kind = ARR; j.items = new Lst<JNode>(); return j; }
    public static JNode Of(string v) { if (v == null) return Null; JNode j = new JNode(); j.kind = STR; j.s = v; return j; }
    public static JNode Of(double v) { JNode j = new JNode(); j.kind = NUM; j.n = v; return j; }
    public static JNode OfBool(bool v) { JNode j = new JNode(); j.kind = BOOL; j.b = v; return j; }

    public bool IsNull() { return kind == NUL; }
    public bool IsArr() { return kind == ARR; }
    public bool IsObj() { return kind == OBJ; }
    public bool IsStr() { return kind == STR; }
    public bool IsNum() { return kind == NUM; }

    public int Count() { return items == null ? 0 : items.Count(); }
    public JNode At(int i) { if (items == null || i < 0 || i >= items.Count()) return Null; return items.Get(i); }
    public string KeyAt(int i) { return keys.Get(i); }

    public int IndexOfKey(string k)
    {
        if (keys == null) return -1;
        for (int i = 0; i < keys.Count(); i++) if (U.Eq(keys.Get(i), k)) return i;
        return -1;
    }
    public bool Has(string k) { return IndexOfKey(k) >= 0; }
    public JNode Get(string k) { int i = IndexOfKey(k); return i < 0 ? Null : items.Get(i); }

    public JNode Set(string k, JNode v)
    {
        int i = IndexOfKey(k);
        if (i >= 0) items.Set(i, v); else { keys.Add(k); items.Add(v); }
        return this;
    }
    public JNode Add(JNode v) { items.Add(v); return this; }

    // --- мәндерді алу (жоқ болса — әдепкі мән) ---
    public string Str() { return kind == STR ? s : kind == NUM ? U.Num(n) : kind == BOOL ? (b ? "true" : "false") : ""; }
    public string Str(string def) { return kind == STR ? s : def; }
    public float F() { return kind == NUM ? (float)n : kind == BOOL ? (b ? 1f : 0f) : 0f; }
    public float F(float def) { return kind == NUM ? (float)n : def; }
    public double D() { return kind == NUM ? n : 0.0; }
    public int I() { return kind == NUM ? (int)n : kind == BOOL ? (b ? 1 : 0) : 0; }
    public int I(int def) { return kind == NUM ? (int)n : def; }
    public bool B() { return kind == BOOL ? b : kind == NUM ? n != 0.0 : kind == STR ? U.Len(s) > 0 : kind == ARR || kind == OBJ; }
    public string S(string k) { return Get(k).Str(); }
    public float Fk(string k) { return Get(k).F(); }
    public int Ik(string k) { return Get(k).I(); }
    public bool Bk(string k) { return Get(k).B(); }

    // ---------------- оқу ----------------
    string src; int p;

    public static JNode Parse(string text)
    {
        JNode r = new JNode();
        r.src = text; r.p = 0;
        JNode v = r.Val();
        r.src = null;
        return v;
    }

    void Ws()
    {
        while (p < U.Len(src))
        {
            char c = U.Ch(src, p);
            if (c == ' ' || c == '\n' || c == '\r' || c == '\t') p++; else break;
        }
    }

    JNode Val()
    {
        Ws();
        if (p >= U.Len(src)) throw new Exception("JSON: соңы күтпеген жерде");
        char c = U.Ch(src, p);
        if (c == '{')
        {
            p++; JNode o = NewObj(); Ws();
            if (U.Ch(src, p) == '}') { p++; return o; }
            while (true)
            {
                Ws(); string k = Text(); Ws();
                if (U.Ch(src, p) != ':') throw new Exception("JSON: ':' күтілді, орын " + p);
                p++;
                o.keys.Add(k); o.items.Add(Val()); Ws();
                char d = U.Ch(src, p); p++;
                if (d == '}') return o;
                if (d != ',') throw new Exception("JSON: ',' күтілді, орын " + p);
            }
        }
        if (c == '[')
        {
            p++; JNode a = NewArr(); Ws();
            if (U.Ch(src, p) == ']') { p++; return a; }
            while (true)
            {
                a.items.Add(Val()); Ws();
                char d = U.Ch(src, p); p++;
                if (d == ']') return a;
                if (d != ',') throw new Exception("JSON: ',' күтілді, орын " + p);
            }
        }
        if (c == '"') return Of(Text());
        if (c == 't') { p += 4; return OfBool(true); }
        if (c == 'f') { p += 5; return OfBool(false); }
        if (c == 'n') { p += 4; return Null; }
        int st = p;
        while (p < U.Len(src))
        {
            char d = U.Ch(src, p);
            if ((d >= '0' && d <= '9') || d == '-' || d == '+' || d == '.' || d == 'e' || d == 'E') p++; else break;
        }
        if (p == st) throw new Exception("JSON: белгісіз таңба, орын " + p);
        return Of(U.ParseNum(U.Sub(src, st, p - st)));
    }

    string Text()
    {
        if (U.Ch(src, p) != '"') throw new Exception("JSON: '\"' күтілді, орын " + p);
        p++;
        Sb sb = new Sb();
        while (true)
        {
            char c = U.Ch(src, p); p++;
            if (c == '"') break;
            if (c == '\\')
            {
                char e = U.Ch(src, p); p++;
                if (e == 'n') sb.AddCh('\n');
                else if (e == 't') sb.AddCh('\t');
                else if (e == 'r') sb.AddCh('\r');
                else if (e == 'b') sb.AddCh('\b');
                else if (e == 'f') sb.AddCh('\f');
                else if (e == 'u') { sb.Add(U.FromCode(Hex(U.Sub(src, p, 4)))); p += 4; }
                else sb.AddCh(e);
            }
            else sb.AddCh(c);
        }
        return sb.Str();
    }

    static int Hex(string h)
    {
        int v = 0;
        for (int i = 0; i < 4; i++)
        {
            char c = U.Ch(h, i); int d;
            if (c >= '0' && c <= '9') d = c - '0';
            else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
            else d = c - 'A' + 10;
            v = v * 16 + d;
        }
        return v;
    }

    // ---------------- жазу ----------------
    public string Dump() { Sb sb = new Sb(); Write(sb); return sb.Str(); }

    void Write(Sb sb)
    {
        if (kind == NUL) sb.Add("null");
        else if (kind == BOOL) sb.Add(b ? "true" : "false");
        else if (kind == NUM) sb.Add(U.Num(n));
        else if (kind == STR) Quote(sb, s);
        else if (kind == ARR)
        {
            sb.AddCh('[');
            for (int i = 0; i < items.Count(); i++) { if (i > 0) sb.AddCh(','); items.Get(i).Write(sb); }
            sb.AddCh(']');
        }
        else
        {
            sb.AddCh('{');
            for (int i = 0; i < items.Count(); i++)
            {
                if (i > 0) sb.AddCh(',');
                Quote(sb, keys.Get(i)); sb.AddCh(':'); items.Get(i).Write(sb);
            }
            sb.AddCh('}');
        }
    }

    static void Quote(Sb sb, string v)
    {
        sb.AddCh('"');
        for (int i = 0; i < U.Len(v); i++)
        {
            char c = U.Ch(v, i);
            if (c == '"') sb.Add("\\\"");
            else if (c == '\\') sb.Add("\\\\");
            else if (c == '\n') sb.Add("\\n");
            else if (c == '\r') sb.Add("\\r");
            else if (c == '\t') sb.Add("\\t");
            else if (c < ' ') { sb.Add("\\u00"); sb.AddCh(HexCh(c / 16)); sb.AddCh(HexCh(c % 16)); }
            else sb.AddCh(c);
        }
        sb.AddCh('"');
    }
    static char HexCh(int d) { return U.Ch("0123456789abcdef", d); }
}
