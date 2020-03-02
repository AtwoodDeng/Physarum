﻿Shader "Custom/GPU Particle"
{
	Properties
	{
		_MainTex("Particle Sprite", 2D) = "white" {}
		_Size("Size Multiplier", Float) = 1
		_Color("Color",color) = (1,1,1,1)
	}

		SubShader
		{
			Pass
			{
				Cull Back 
				Lighting Off
				Zwrite Off

			Blend SrcAlpha OneMinusSrcAlpha
			// Blend One OneMinusSrcAlpha
			//Blend One One
			//Blend OneMinusDstColor One

			LOD 200

			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
			}

			CGPROGRAM

			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Common.cginc" 

			uniform sampler2D _MainTex;
			float _Size;
			float4 _Color;

			StructuredBuffer<ParticleInfo> particles;
			StructuredBuffer<float3> quad;

			struct v2f
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float4 col : COLOR;
			};

			v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				v2f o;

				float3 q = quad[id];

				o.pos = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, float4(  particles[inst].pos  , 1.0f)) + float4(q, 0.0f) * _Size);

				o.uv = q + 0.5f;

				o.col = _Color ;

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				return tex2D(_MainTex, i.uv) * i.col * i.col.a;
			}

			ENDCG
		}
		}
}