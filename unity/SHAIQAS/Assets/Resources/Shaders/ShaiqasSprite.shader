// SHAIQAS · фон спрайттары: тек сурет × түс (uv2 жоқ — SpriteRenderer оны бермейді).
// Араластыру _SrcBlend/_DstBlend арқылы: қалыпты (5/10) немесе жарқын қосу (5/1).
Shader "Shaiqas/Sprite"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
        _Blur ("Blur (texels)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest Always Lighting Off
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Blur;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                if (_Blur <= 0) return tex2D(_MainTex, i.uv) * i.color;
                // алыс қабаттар — сәл бұлдыр (камераның фокусы жауынгерлерде): 9 нүктелі жұмсақ бұлдыр
                float2 d = _MainTex_TexelSize.xy * _Blur;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.2;
                c += (tex2D(_MainTex, i.uv + float2(d.x, 0)) + tex2D(_MainTex, i.uv - float2(d.x, 0)) + tex2D(_MainTex, i.uv + float2(0, d.y)) + tex2D(_MainTex, i.uv - float2(0, d.y))) * 0.12;
                c += (tex2D(_MainTex, i.uv + d * 0.7) + tex2D(_MainTex, i.uv - d * 0.7) + tex2D(_MainTex, i.uv + float2(d.x, -d.y) * 0.7) + tex2D(_MainTex, i.uv + float2(-d.x, d.y) * 0.7)) * 0.08;
                return c * i.color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
