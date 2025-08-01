Shader "Custom/LockedBuildingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Darkness ("Darkness", Range(0, 1)) = 0.7
        _Desaturation ("Desaturation", Range(0, 1)) = 0.8
        _PatternScale ("Pattern Scale", Float) = 10.0
        _PatternOpacity ("Pattern Opacity", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Darkness;
            float _Desaturation;
            float _PatternScale;
            float _PatternOpacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample main texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Desaturate
                float luminance = dot(col.rgb, float3(0.3, 0.59, 0.11));
                col.rgb = lerp(col.rgb, luminance.xxx, _Desaturation);
                
                // Darken
                col.rgb *= (1.0 - _Darkness);
                
                // Add a subtle pattern (checkerboard)
                float2 patternUV = i.uv * _PatternScale;
                float checker = frac(patternUV.x + patternUV.y) > 0.5 ? 0.8 : 0.5;
                col.rgb = lerp(col.rgb, checker, _PatternOpacity);
                
                return col;
            }
            ENDCG
        }
    }
}