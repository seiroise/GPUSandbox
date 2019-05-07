Shader "Seiro/GPUSandbox/FluidLighting2D"
{
	Properties
	{
		_BaseColor ("Base color", 2D) = "white" {}
		_DistanceField ("Distance field", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off 
		ZWrite Off 
		ZTest Always

		CGINCLUDE

		#define MAX_MARCHING_STEP 32
		#define RAYS_PER_PIXEL 32
		#define EPSILON 1e-5
		#define PI 3.141593

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

		sampler2D _DistanceField;

		// 距離場を利用してレイマーチングを行う。
		bool trace(float2 origin, float2 ray, out float2 hitPos, out float d)
		{
			float t = 0;
			float dist;
			float2 samplePoint;
			for (int i = 0; i < MAX_MARCHING_STEP; ++i)
			{
				samplePoint = origin + ray * t;
				dist = tex2D(_DistanceField, samplePoint);
				t += dist;
				d = t;
				if (dist < EPSILON)
				{
					hitPos = samplePoint;
					return true;
				}
			}
			return false;
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _BaseColor;
			float4 _BaseColor_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
#if UNITY_UV_STARTS_AT_TOP
				if (_BaseColor_TexelSize.y > 0)
				{
					o.uv.y = 1 - v.uv.y;
				}
#endif
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(0, 0, 0, 0);
				float2 origin = i.uv;
				// return tex2D(_DistanceField, i.uv);

				for (float i = 0; i < RAYS_PER_PIXEL; ++i)
				{
					float2 hitPos;
					float d;

					float2 ray = float2(cos(i / RAYS_PER_PIXEL * 2 * PI), sin(i / RAYS_PER_PIXEL * 2 * PI));
					if (trace(origin, ray, hitPos, d))
					{
						fixed4 matColor = tex2D(_BaseColor, hitPos);
						float r = 2;
						fixed att = pow(max(1 - (d * d) / (r * r), 0), 2);
						col += matColor * att;
					}
				}

				col = col / RAYS_PER_PIXEL;
				return col;
			}
			ENDCG
		}
	}
}
