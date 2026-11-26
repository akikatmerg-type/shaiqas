// SHAIQAS · Жауынгер атласы: түс × сурет; uv2.x = 1 болса — суреттің пішіні бір түспен (көлеңке/жиек өтуі).
// Built-in және URP (2D/Universal) жобаларында жұмыс істейді: жарықсыз, мөлдір, Cull Off.
// Жауынгерге көлем: _Center (x, y — дене ортасы, z — дене өлшемі) мен _Light (x, y — жарық бағыты) бойынша жарық/көлеңкелі жақ,
// _Tint — арена жарығының түсі (rgb, a — күші). Әдепкі мәндер (_Shade = 0, _Tint.a = 0) — ешнәрсе өзгермейді (фон, әсерлер).
Shader "Shaiqas/FighterAtlas"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
        _Center ("Center", Vector) = (0, 0, 130, 0)
        _Light ("Light dir", Vector) = (1, 0, 0, 0)
        _Shade ("Shade", Float) = 0
        _Tint ("Tint", Color) = (1, 1, 1, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Center, _Light, _Tint; float _Shade;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float2 solid : TEXCOORD1; float2 lp : TEXCOORD2; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.solid = v.uv2;
                o.lp = v.vertex.xy;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed3 rgb = lerp(t.rgb * i.color.rgb, i.color.rgb, saturate(i.solid.x));
                if (_Shade > 0 && i.solid.y < 0.5)
                {
                    // жарық жағы ашығырақ, қарсы жағы күңгірт (жұмсақ «cel» өтуі); жоғары — аспан жарығы, төмен — жерге жақын көлеңке
                    float2 d = (i.lp - _Center.xy) / _Center.z;
                    float lit = smoothstep(-0.45, 0.45, dot(d, _Light.xy)) * 2 - 1;
                    float up = saturate(d.y * 0.6 + 0.5);
                    rgb *= (1 + lit * _Shade) * lerp(0.84, 1.06, up);
                    rgb = lerp(rgb, rgb * _Tint.rgb * 1.15, _Tint.a);
                }
                return fixed4(rgb, t.a * i.color.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
