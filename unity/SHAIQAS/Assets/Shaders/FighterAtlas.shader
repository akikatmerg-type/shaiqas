// SHAIQAS · Жауынгер атласы: түс × сурет; uv2.x = 1 болса — суреттің пішіні бір түспен (көлеңке/жиек өтуі).
// Built-in және URP (2D/Universal) жобаларында жұмыс істейді: жарықсыз, мөлдір, Cull Off.
Shader "Shaiqas/FighterAtlas"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float solid : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.solid = v.uv2.x;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed3 rgb = lerp(t.rgb * i.color.rgb, i.color.rgb, saturate(i.solid));
                return fixed4(rgb, t.a * i.color.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
