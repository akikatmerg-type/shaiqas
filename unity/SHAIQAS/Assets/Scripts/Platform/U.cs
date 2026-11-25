// SHAIQAS · Платформа: Unity нұсқасы. Core коды тек осы көмекшілер арқылы жолдармен жұмыс істейді,
// сол себепті «призрачный Юнити» (Java) дәл осындай U класымен Core-ды тексере алады.
using System;
using System.Globalization;
using System.Text;

public static class U
{
    static System.Random rng = new System.Random(12345);
    public static int Len(string s) { return s == null ? 0 : s.Length; }
    public static char Ch(string s, int i) { return s[i]; }
    public static string Sub(string s, int start, int len) { return s.Substring(start, len); }
    public static string From(string s, int start) { return s.Substring(start); }
    public static bool Eq(string a, string b) { return string.Equals(a, b, StringComparison.Ordinal); }
    public static bool Starts(string s, string p) { return s != null && s.StartsWith(p, StringComparison.Ordinal); }
    public static bool Ends(string s, string p) { return s != null && s.EndsWith(p, StringComparison.Ordinal); }
    public static int IndexOf(string s, string p) { return s == null ? -1 : s.IndexOf(p, StringComparison.Ordinal); }
    public static bool Has(string s, string p) { return IndexOf(s, p) >= 0; }
    public static string Replace(string s, string a, string b) { return string.IsNullOrEmpty(a) || s == null ? s : s.Replace(a, b); }
    public static string[] Split(string s, char c) { return s.Split(c); }
    public static string Trim(string s) { return s.Trim(); }
    public static string Upper(string s) { return s.ToUpperInvariant(); }
    public static string Lower(string s) { return s.ToLowerInvariant(); }
    public static string ChStr(char c) { return c.ToString(); }
    public static string FromCode(int code) { return ((char)code).ToString(); }
    public static double ParseNum(string s) { return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture); }
    public static string Num(double v)
    {
        if (Math.Abs(v - Math.Round(v)) < 1e-9 && Math.Abs(v) < 1e15) return ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture);
        // float → double шуын кесеміз: 1.4f «1.3999999761581421» емес, «1.4» болып шығады
        return Math.Round(v, 5).ToString("R", CultureInfo.InvariantCulture);
    }
    public static string Int(int v) { return v.ToString(CultureInfo.InvariantCulture); }
    public static float Rand() { lock (rng) return (float)rng.NextDouble(); }
    public static void Seed(int s) { lock (rng) rng = new System.Random(s); }
    public static void Log(string s) { UnityEngine.Debug.Log(s); }
    public static void Warn(string s) { UnityEngine.Debug.LogWarning(s); }
}

/// <summary>Жол жинағыш (Core үшін): JSON жазу т.б.</summary>
public class Sb
{
    readonly StringBuilder b = new StringBuilder();
    public void Add(string s) { b.Append(s); }
    public void AddCh(char c) { b.Append(c); }
    public string Str() { return b.ToString(); }
}
