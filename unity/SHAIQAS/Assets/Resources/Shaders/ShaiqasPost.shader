// SHAIQAS · экран деңгейіндегі пост-өңдеу (веб: gfxPost):
// 0 — жарық бөліктерді бөліп алу (s^13, веб: multiply ×5), 1 — бұлдырлату, 2 — соңғы жинақ:
// bloom (screen) + түс градациясы (soft-light) + виньетка + түйір (grain).
Shader "Shaiqas/Post"
{
    Properties { _MainTex ("Src", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    sampler2D _BloomC, _BloomD, _Grade;
    float _Bloom, _GradeA, _Grain, _Lin, _Seed;
    float4 _Shock0, _Shock1, _Shock2;   // соққы толқыны: x, y (экран uv), радиус, күші
    float _Chroma, _Heat, _Aspect, _T;
    float3 ToG(float3 c) { return _Lin > 0.5 ? LinearToGammaSpace(c) : c; }
    float3 ToL(float3 c) { return _Lin > 0.5 ? GammaToLinearSpace(c) : c; }
    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
    v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
    float4 fragBright(v2f i) : SV_Target
    {
        float3 s = ToG(tex2D(_MainTex, i.uv).rgb);
        float3 s2 = s * s, s4 = s2 * s2, s8 = s4 * s4;
        return float4(s8 * s4 * s, 1);
    }
    float4 fragBlur(v2f i) : SV_Target
    {
        float2 d = _MainTex_TexelSize.xy;
        float3 c = tex2D(_MainTex, i.uv).rgb * 0.25;
        c += (tex2D(_MainTex, i.uv + float2(d.x, 0)).rgb + tex2D(_MainTex, i.uv - float2(d.x, 0)).rgb + tex2D(_MainTex, i.uv + float2(0, d.y)).rgb + tex2D(_MainTex, i.uv - float2(0, d.y)).rgb) * 0.125;
        c += (tex2D(_MainTex, i.uv + d).rgb + tex2D(_MainTex, i.uv - d).rgb + tex2D(_MainTex, i.uv + float2(d.x, -d.y)).rgb + tex2D(_MainTex, i.uv + float2(-d.x, d.y)).rgb) * 0.0625;
        return float4(c, 1);
    }
    float SoftL(float b, float s)
    {
        if (s <= 0.5) return b - (1 - 2 * s) * b * (1 - b);
        float D = b <= 0.25 ? ((16 * b - 12) * b + 4) * b : sqrt(b);
        return b + (2 * s - 1) * (D - b);
    }
    float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233)) + _Seed) * 43758.5453); }
    float2 Shock(float2 uv, float4 s)
    {
        if (s.w <= 0) return 0;
        float2 d = uv - s.xy; d.x *= _Aspect;
        float r = length(d), k = (r - s.z) / 0.035;
        return (d / max(r, 1e-4)) * float2(1 / _Aspect, 1) * s.w * exp(-k * k) * 0.022;
    }
    float4 fragFinal(v2f i) : SV_Target
    {
        float2 uv = i.uv, suv = i.uv; float fy = 1;
        // MSAA кезінде (DirectX) көз көрінісі тігінен аударылуы мүмкін — экран координаты үшін түзетеміз
        if (_MainTex_TexelSize.y < 0) { suv.y = 1 - suv.y; fy = -1; }
        // ауа қисаюы: соққы толқындары, Хиуада — ыстықтан дірілдеген ауа (экранның төменгі жартысы)
        float2 sh = Shock(suv, _Shock0) + Shock(suv, _Shock1) + Shock(suv, _Shock2);
        uv -= float2(sh.x, sh.y * fy);
        if (_Heat > 0)
        {
            float hk = saturate((0.62 - suv.y) / 0.4) * _Heat;
            uv.x += sin(suv.y * 90 + _T * 3.1) * 0.0012 * hk + sin(suv.y * 37 - _T * 1.7) * 0.0008 * hk;
            uv.y += sin(suv.x * 60 + _T * 2.3) * 0.0007 * hk * fy;
        }
        float3 col;
        if (_Chroma > 0)
        {   // нокаутта: түстердің шетке қарай сәл ығысуы (линза әсері)
            float2 c = (i.uv - 0.5) * _Chroma * 0.012;
            col = ToG(float3(tex2D(_MainTex, uv + c).r, tex2D(_MainTex, uv).g, tex2D(_MainTex, uv - c).b));
        }
        else col = ToG(tex2D(_MainTex, uv).rgb);
        float3 bl = saturate(tex2D(_BloomC, i.uv).rgb + 0.7 * tex2D(_BloomD, i.uv).rgb) * _Bloom;
        col = 1 - (1 - col) * (1 - bl);
        float3 g = tex2D(_Grade, suv).rgb;
        float3 sl = float3(SoftL(col.r, g.r), SoftL(col.g, g.g), SoftL(col.b, g.b));
        col = lerp(col, sl, _GradeA);
        float n = Hash(floor(i.uv * _ScreenParams.xy)), w = n > 0.5 ? 1 : 0, a = abs(n - 0.5) * 2 * (w > 0 ? 150.0 / 255 : 1) * _Grain;
        col = lerp(col, float3(w, w, w), a);
        return float4(ToL(saturate(col)), 1);
    }
    ENDCG
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Pass { CGPROGRAM
        #pragma vertex vert
        #pragma fragment fragBright
        ENDCG }
        Pass { CGPROGRAM
        #pragma vertex vert
        #pragma fragment fragBlur
        ENDCG }
        Pass { CGPROGRAM
        #pragma vertex vert
        #pragma fragment fragFinal
        ENDCG }
    }
}
