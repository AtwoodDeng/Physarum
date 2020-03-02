// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "PostProcess/Tex3DDisplay"
{ 
	Properties
	{
        _MainTex ("Texture", 2D) = "white" {}
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

				float4 MergeColor( float4 dentisy , float4 intensity )
				{
					if ( _UseLighting )
						return dentisy * ( intensity ) * 10 ; 
				
					return dentisy;
				}
			
				float4 frag (Interpolator i) : SV_Target
				{
					float4 mainCol = tex2D( _MainTex , i.uv);

					
					float noiseSample = 0;
					float3 viewDir = i.viewDir; 


					float4 intensity;
					float4 dentisy = GetDentisyAdvance(_CamPos, viewDir, noiseSample,intensity);

					float4 color = clamp(MergeColor( dentisy , intensity ),0,1);

					// if we want to display the alpha channel, 
					// we will turn it into a gray scale image by replacing rgb with a
					//if ( _DisplayChannel == 8  ) // a 
					//{
					//	return float4( color.aaa , 1 );
					//}
					// if we want to show the rgb channel, just return the color.rgb
					// and set the alpha to 1
					return float4( mainCol.rgb + color.rgb  , 1 );
				}
				ENDCG
			}

			//// 1 , for rendering the slice
			//Pass {
			//		Cull Off ZWrite Off ZTest Always
			//		CGPROGRAM
			//		#pragma vertex vert
			//		#pragma fragment frag

			//		#include "./VolumeShader.cginc"
			//		#include "UnityCG.cginc"
			//		#include "Lighting.cginc"


			//		struct appdata
			//		{
			//			float4 vertex : POSITION;
			//			float2 uv : TEXCOORD0;
			//		};

			//		struct Interpolator {
			//			float4 vertex : SV_POSITION;
			//			float2 uv : TEXCOORD1;
			//		};

			//		Interpolator vert (appdata v)
			//		{
			//			Interpolator o;
			//			o.vertex = UnityObjectToClipPos(v.vertex);
			//			o.uv = v.uv;
			//			return o;
			//		}

			//		float4 frag (Interpolator i) : SV_Target
			//		{
			//			float4 density = float4(0,0,0,0);
			//			float3 worldPos = float3(0,0,0);

			//			// check the plane type
			//			if ( _Plane == 0 ) // XY
			//			{
			//				 worldPos = float3( i.uv.x , i.uv.y , _SliceOffset) - float3(0.5,0.5,0.5);
						
			//			} else if (_Plane == 1 ) //XZ
			//			{
			//				 worldPos = float3( i.uv.x ,  _SliceOffset , i.uv.y ) - float3(0.5,0.5,0.5);

			//			}else { // YZ
						
			//				 worldPos = float3( _SliceOffset , i.uv.x , i.uv.y ) - float3(0.5,0.5,0.5);
			//			}

			//			// then render each pixel in the slice
			//			if ( ( _DisplayChannel >> 0 ) & 1 )
			//				density.r = SampleDensityAdvance( worldPos , 0  , 0 );
						
			//			if ( ( _DisplayChannel >> 1 ) & 1 ) 
			//				density.g = SampleDensityAdvance( worldPos , 1 , 0  );
					
			//			if ( ( _DisplayChannel >> 2 ) & 1 )
			//				density.b = SampleDensityAdvance( worldPos , 2 , 0 );
					
			//			if ( ( _DisplayChannel >> 3 ) & 1 )
			//				density.a = SampleDensityAdvance( worldPos , 3 , 0 );

			//			// if we want to display the alpha channel, 
			//			// we will turn it into a gray scale image by replacing rgb with a
			//			if ( _DisplayChannel == 8  ) // a 
			//			{
			//				return float4( density.aaa , 1 );
			//			}
			//			// if we want to show the rgb channel, just return the density.rgb
			//			// and set the alpha to 1
			//			return float4(density.rgb, 1 );
			//		}

			//		ENDCG 	
			//}

	}
}
