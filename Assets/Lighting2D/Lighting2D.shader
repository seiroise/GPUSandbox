Shader "Seiro/GPUSandbox/Lighting2D"
{
	Properties
	{
		_BaseColor ("Base color", 2D) = "white" {}
		_Substance ("Substance", 2D) = "white" {}
		_DistanceField ("Distance field", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off 
		ZWrite Off 
		ZTest Always
		Blend Off

		CGINCLUDE

		#define MAX_MARCHING_STEP 32
		#define RAYS_PER_PIXEL 32
		#define EPSILON 0.0001
		#define PI 3.141593
		#define HASHSCALE1 .1031

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

		sampler2D _BaseColor;
		float4 _BaseColor_TexelSize;
		sampler2D _Substance;
		sampler2D_float _DistanceField;

		v2f vert(appdata v)
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

		// 二次元ベクトルから対応するハッシュの生成
		float hash12(float2 p)
		{
			float3 p3  = frac(float3(p.xyx) * HASHSCALE1);
			p3 += dot(p3, p3.yzx + 19.19);
			return frac((p3.x + p3.y) * p3.z);
		}
		
		// 距離場を利用してレイマーチングを行う。
		bool trace(float2 origin, float2 ray, out float2 hitPos, out float d)
		{
			d = 0;
			for (int i = 0; i < MAX_MARCHING_STEP; ++i)
			{
				float2 samplePoint = origin + ray * d;
				float4 dist = tex2D(_DistanceField, samplePoint);
				// float4 dist = tex2D(_DistanceField, samplePoint / _ScreenParams.xy);
				d += dist.x;
				if (dist.x <= EPSILON)
				{
					// hitPos = samplePoint;
					hitPos = origin + ray * d;
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
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(0, 0, 0, 0);
				float2 origin = i.uv;
				// return tex2D(_BaseColor, i.uv);

				float rand = hash12(i.uv);
				rand = 0;

				for (float i = 0; i < RAYS_PER_PIXEL; ++i)
				{
					float2 hitPos;
					float d;

					float2 ray = float2(
						cos(i / RAYS_PER_PIXEL * 2 * PI + rand),
						sin(i / RAYS_PER_PIXEL * 2 * PI + rand));
					if (trace(origin, ray, hitPos, d))
					{
						fixed4 matColor = tex2D(_BaseColor, hitPos);
						fixed r = 2;
						fixed att = pow(max(1 - (d * d) / (r * r), 0), 2);
						col += matColor * att;
						// col += matColor;
					}
				}
				col = col / RAYS_PER_PIXEL;
				return col;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 frag (v2f i) : SV_Target
			{
				float2 origin = i.uv;
				// float2 origin = i.uv * _ScreenParams.xy;
				float3 color = float3(0, 0, 0);
				float emis = 0;
				float count = 0;

				float rand = hash12(i.uv);
				rand = 0.;
				float aspect = _ScreenParams.x / _ScreenParams.y;
				float invAspect = 1 / aspect;

				for(float i = 0; i < RAYS_PER_PIXEL; ++i)
				{
					float2 hitPos = float2(0, 0);
					float d = 0;

					float2 ray = float2(cos(i / RAYS_PER_PIXEL * 2 * PI + rand),
										sin(i / RAYS_PER_PIXEL * 2 * PI + rand));

					if(trace(origin, ray, hitPos, d))
					{
						float3 baseColor 	= tex2D(_BaseColor, hitPos).xyz;
						float4 material 	= tex2D(_Substance, hitPos);

						float r = 2;
						float att = pow(max(1.0 - (d * d) / (r * r), 0), 2);

						emis += material.x * att;
						color += material.x * baseColor * att;
						// color += baseColor;
						count++;
					}
				}
				float t = count / RAYS_PER_PIXEL;
				// color = float3(t, t, t);
				color *= (1.0 / RAYS_PER_PIXEL);
				// color *= 0.5;
				return fixed4(pow(color, 1 / 2.2), 1);
			}

			ENDCG
		}
	}
}