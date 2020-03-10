Shader "Unlit/VisualizeTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LUT ("LUT",2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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
            sampler2D _LUT;
            float4 _LUT_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 trail = tex2D(_MainTex, i.uv);
                fixed4 col = tex2D( _LUT , float2( clamp( trail.r , 0.001 , 0.995) , 0));

                return col;
            }
            ENDCG
        }
    }
}
