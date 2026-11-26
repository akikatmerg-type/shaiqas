// SHAIQAS · ұрыс камерасының пост-өңдеуі (веб: gfxPost — bloom, soft-light градация, виньетка, түйір).
// HUD (OnGUI) бұдан кейін салынады, сондықтан өңдеуге түспейді.
using UnityEngine;

public class PostFx : MonoBehaviour
{
    public static bool on, dbgOff;
    // соққы толқындары (экран uv): x, y, уақыт, күші
    static readonly Vector4[] shock = new Vector4[3]; static int shockI;
    public static float chroma; static bool heat;
    /// <summary>Ауаны қисайтатын соққы толқыны (ауыр соққы, нокаут, рух соққысы).</summary>
    public static void AddShock(Vector2 uv, float power)
    {
        if (!Gfx.Shock) return;
        shock[shockI] = new Vector4(uv.x, uv.y, 0, power); shockI = (shockI + 1) % shock.Length;
    }
    Material mat; Texture2D grade; string gradeId;
    float bloom = 0.2f, sl = 0.14f;

    // веб: GFX_ST
    static readonly string[] IDS = { "steppe", "sygnaq", "nogai", "siberia", "dzungar", "kokand", "khiva" };
    static readonly string[] GT = { "255,220,160", "255,170,170", "255,200,120", "175,210,255", "255,150,100", "255,215,140", "255,228,150" };
    static readonly string[] GB = { "40,70,40", "50,24,60", "80,44,20", "16,36,80", "70,10,24", "100,56,20", "110,64,20" };
    static readonly float[] SL = { 0.14f, 0.18f, 0.2f, 0.24f, 0.2f, 0.2f, 0.2f };
    static readonly float[] BLOOM = { 0.18f, 0.32f, 0.3f, 0.22f, 0.4f, 0.22f, 0.2f };
    static readonly float[] VIG = { 0.3f, 0.42f, 0.4f, 0.4f, 0.5f, 0.38f, 0.38f };

    /// <summary>Арена жарығының түсі жауынгерлерге (жоғарғы градация түсінен, жұмсартылған; a — күші).</summary>
    public static Color TintOf(string id)
    {
        int c = Col.Parse("rgb(" + GT[Idx(id)] + ")");
        Color t = new Color(Col.R(c) / 255f, Col.G(c) / 255f, Col.B(c) / 255f);
        float mx = Mathf.Max(t.r, Mathf.Max(t.g, t.b)); t /= mx;   // ең ашық арна = 1
        t.a = 0.42f; return t;
    }
    public static int Idx(string id) { for (int i = 0; i < IDS.Length; i++) if (IDS[i] == id) return i; return 0; }

    public void SetStage(string id)
    {
        on = true;
        if (gradeId == id && grade != null) return;
        heat = id == "khiva" && Gfx.Shock;
        gradeId = id; int k = Idx(id);
        sl = SL[k]; bloom = BLOOM[k];
        int top = Col.Parse("rgb(" + GT[k] + ")"), bot = Col.Parse("rgb(" + GB[k] + ")");
        if (grade == null) { grade = new Texture2D(160, 90, TextureFormat.RGBA32, false); grade.wrapMode = TextureWrapMode.Clamp; grade.filterMode = FilterMode.Bilinear; }
        Color32[] px = new Color32[160 * 90];
        for (int y = 0; y < 90; y++)
        {
            float v = y / 89f; // 0 — жоғары
            float r, g, b;
            if (v < 0.5f) { float t = v / 0.5f; r = Mathf.Lerp(Col.R(top), 128, t); g = Mathf.Lerp(Col.G(top), 128, t); b = Mathf.Lerp(Col.B(top), 128, t); }
            else if (v < 0.78f) { r = g = b = 128; }
            else { float t = (v - 0.78f) / 0.22f; r = Mathf.Lerp(128, Col.R(bot), t); g = Mathf.Lerp(128, Col.G(bot), t); b = Mathf.Lerp(128, Col.B(bot), t); }
            for (int x = 0; x < 160; x++)
            {
                float d = Mathf.Sqrt((x - 80) * (x - 80) + (y - 49) * (y - 49)), u = Mathf.Clamp01((d - 26) / 74f);
                float a = u < 0.55f ? Mathf.Lerp(0, VIG[k] * 0.5f, u / 0.55f) : Mathf.Lerp(VIG[k] * 0.5f, VIG[k], (u - 0.55f) / 0.45f);
                px[(89 - y) * 160 + x] = new Color32((byte)(r * (1 - a)), (byte)(g * (1 - a)), (byte)(b * (1 - a)), 255);
            }
        }
        grade.SetPixels32(px); grade.Apply();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!on || dbgOff || grade == null)
        {
            Graphics.Blit(src, dst); return;
        }
        if (mat == null)
        {
            Shader sh = Resources.Load<Shader>("Shaders/ShaiqasPost");
            if (sh == null || !sh.isSupported) { UnityEngine.Debug.LogWarning("SHAIQAS: Post шейдері жоқ/қолдау жоқ"); on = false; Graphics.Blit(src, dst); return; }
            mat = new Material(sh);
        }
        RenderTexture a = RenderTexture.GetTemporary(320, 180, 0, src.format);
        RenderTexture c = RenderTexture.GetTemporary(160, 90, 0, src.format);
        RenderTexture d = RenderTexture.GetTemporary(80, 45, 0, src.format);
        a.filterMode = c.filterMode = d.filterMode = FilterMode.Bilinear;
        Graphics.Blit(src, a, mat, 0);
        Graphics.Blit(a, c, mat, 1);
        Graphics.Blit(c, d, mat, 1);
        mat.SetTexture("_BloomC", c); mat.SetTexture("_BloomD", d); mat.SetTexture("_Grade", grade);
        mat.SetFloat("_Bloom", Mathf.Min(1, bloom));
        mat.SetFloat("_GradeA", Mathf.Min(1, sl * 2.4f));
        mat.SetFloat("_Grain", 0.06f);
        mat.SetFloat("_Lin", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        mat.SetFloat("_Seed", Random.value * 100);
        float dt = Time.deltaTime;
        for (int i = 0; i < shock.Length; i++)
        {
            Vector4 s = shock[i]; if (s.w <= 0) { mat.SetVector("_Shock" + i, Vector4.zero); continue; }
            s.z += dt; float life = 0.42f, k = s.z / life;
            if (k >= 1) { s.w = 0; shock[i] = s; mat.SetVector("_Shock" + i, Vector4.zero); continue; }
            shock[i] = s;
            mat.SetVector("_Shock" + i, new Vector4(s.x, s.y, 0.02f + k * 0.42f, s.w * (1 - k) * (1 - k)));
        }
        chroma = Mathf.Max(0, chroma - dt * 1.6f);
        mat.SetFloat("_Chroma", chroma); mat.SetFloat("_Heat", heat ? 1 : 0); mat.SetFloat("_T", Time.time);
        mat.SetFloat("_Aspect", (float)src.width / Mathf.Max(1, src.height));
        Graphics.Blit(src, dst, mat, 2);
        RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(c); RenderTexture.ReleaseTemporary(d);
    }
}
