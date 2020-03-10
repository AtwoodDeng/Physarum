// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "PostProcess/Tex3DDisplay"
{ 
	Properties
	{
        _MainTex ("Texture", 2D) = "white" {}
		_LUT("LUT", 2D ) = "white" {}
	}
		SubShader
		{
			Cull Off ZWrite Off ZTest Always
			Lighting On
			LOD 100
			// 0 , for rendering the cloud
			Pass
			{  

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				// make fog work
				// #pragma multi_compile_fog
				// #pragma multi_complile USE_BAKE_LIGHTING
				#include "./VolumeShader.cginc"
				#include "UnityCG.cginc"
				#include "Lighting.cginc"


				float3 _CamPos; // camera.transform.position
				float3 _ViewStartPos; // transfer the (0,0) in screen position to worldPos
				float3 _ViewOffsetU; // The position offset in the U direction
				float3 _ViewOffsetV; // The position offset in the V direction 

				float _ViewDistance; // how far to view, used to detect the sample step
				sampler2D _MainTex;
				sampler2D _LUT;
				float _Intensity;

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct Interpolator {
					float4 vertex : SV_POSITION;
					float3 viewDir : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};

				Interpolator vert (appdata v)
				{
					Interpolator o;
					o.vertex = UnityObjectToClipPos(v.vertex);
				
					v.vertex.z = 0.5;
					o.viewDir = normalize( _ViewStartPos + v.uv.x * _ViewOffsetU + v.uv.y * _ViewOffsetV - _CamPos);
					o.uv = v.uv; 
				
					return o;
				}

				float4 frag (Interpolator i) : SV_Target
				{
					float4 mainCol = tex2D( _MainTex , i.uv);

					float noiseSample = 0;
					float3 viewDir = i.viewDir; 


					float4 intensity;
					float4 dentisy = GetDentisyAdvance(_CamPos, viewDir, noiseSample,intensity);

					// intensity is not used 
					float4 color = tex2D( _LUT , float2( clamp(dentisy.x ,0.001,0.995) , 0 ) );

					return float4( mainCol.rgb + color.rgb * _Intensity , 1 ); 
				}
				ENDCG
			}


	}
}
