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
    float4 fragFinal(v2f i) : SV_Target
    {
        float3 col = ToG(tex2D(_MainTex, i.uv).rgb);
        float3 bl = saturate(tex2D(_BloomC, i.uv).rgb + 0.7 * tex2D(_BloomD, i.uv).rgb) * _Bloom;
        col = 1 - (1 - col) * (1 - bl);
        float3 g = tex2D(_Grade, i.uv).rgb;
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
