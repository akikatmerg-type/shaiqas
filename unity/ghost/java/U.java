// Призрачный Юнити: Java-нұсқасы (Platform/U.cs-тің егізі)
import java.util.*;
final class U {
    static Random rng = new Random(12345);
    static int Len(String s) { return s == null ? 0 : s.length(); }
    static char Ch(String s, int i) { return s.charAt(i); }
    static String Sub(String s, int start, int len) { return s.substring(start, start + len); }
    static String From(String s, int start) { return s.substring(start); }
    static boolean Eq(String a, String b) { return Objects.equals(a, b); }
    static boolean Starts(String s, String p) { return s != null && s.startsWith(p); }
    static boolean Ends(String s, String p) { return s != null && s.endsWith(p); }
    static int IndexOf(String s, String p) { return s == null ? -1 : s.indexOf(p); }
    static boolean Has(String s, String p) { return IndexOf(s, p) >= 0; }
    static String Replace(String s, String a, String b) { return (a == null || a.isEmpty() || s == null) ? s : s.replace(a, b); }
    static String[] Split(String s, char c) { return s.split(java.util.regex.Pattern.quote(String.valueOf(c)), -1); }
    static String Trim(String s) { return s.trim(); }
    static String Upper(String s) { return s.toUpperCase(Locale.ROOT); }
    static String Lower(String s) { return s.toLowerCase(Locale.ROOT); }
    static String ChStr(char c) { return String.valueOf(c); }
    static String FromCode(int code) { return String.valueOf((char) code); }
    static double ParseNum(String s) { return Double.parseDouble(s); }
    static String Num(double v) {
        if (Math.abs(v - Math.rint(v)) < 1e-9 && Math.abs(v) < 1e15) return Long.toString(Math.round(v));
        return Double.toString(v);
    }
    static String Int(int v) { return Integer.toString(v); }
    static float Rand() { return rng.nextFloat(); }
    static void Seed(int s) { rng = new Random(s); }
    static void Log(String s) { System.out.println(s); }
    static void Warn(String s) { System.out.println("WARN " + s); }
}
final class Sb {
    final StringBuilder b = new StringBuilder();
    void Add(String s) { b.append(s); }
    void AddCh(char c) { b.append(c); }
    String Str() { return b.toString(); }
}
final class Mathf {
    static final float PI = (float) Math.PI;
    static final float Infinity = Float.POSITIVE_INFINITY;
    static float Sin(float a) { return (float) Math.sin(a); }
    static float Cos(float a) { return (float) Math.cos(a); }
    static float Tan(float a) { return (float) Math.tan(a); }
    static float Atan2(float y, float x) { return (float) Math.atan2(y, x); }
    static float Acos(float a) { return (float) Math.acos(a); }
    static float Asin(float a) { return (float) Math.asin(a); }
    static float Sqrt(float a) { return (float) Math.sqrt(a); }
    static float Abs(float a) { return Math.abs(a); }
    static int Abs(int a) { return Math.abs(a); }
    static float Min(float a, float b) { return Math.min(a, b); }
    static float Max(float a, float b) { return Math.max(a, b); }
    static int Min(int a, int b) { return Math.min(a, b); }
    static int Max(int a, int b) { return Math.max(a, b); }
    static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
    static int Clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }
    static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
    static float Floor(float a) { return (float) Math.floor(a); }
    static float Ceil(float a) { return (float) Math.ceil(a); }
    static float Round(float a) { return (float) Math.rint(a); }
    static int FloorToInt(float a) { return (int) Math.floor(a); }
    static int CeilToInt(float a) { return (int) Math.ceil(a); }
    static int RoundToInt(float a) { return (int) Math.rint(a); }
    static float Pow(float a, float b) { return (float) Math.pow(a, b); }
    static float Exp(float a) { return (float) Math.exp(a); }
    static float Lerp(float a, float b, float t) { t = Clamp01(t); return a + (b - a) * t; }
    static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
    static float Sign(float a) { return a >= 0 ? 1f : -1f; }
}
