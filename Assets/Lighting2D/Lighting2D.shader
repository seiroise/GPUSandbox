Shader "Seiro/GPUSandbox/Lighting2D"
{
	Properties
	{
		_MainTex ("Main", 2D) = "white" {}
		_BaseColor ("Base color", 2D) = "white" {}
		_Substance ("Substance", 2D) = "white" {}
		_DistanceField ("Distance field", 2D) = "white" {}
		_PrevLighting ("Prev Lighting", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_NoiseOffset("Noise Offset", Range(0, 10)) = 1
		_EmisScale("Emis Scale", float) = 1
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
		#define TRACE_EPSILON 0.00001
		#define EPSILON 0.00001
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

		sampler2D _MainTex;
		sampler2D _BaseColor;
		float4 _BaseColor_TexelSize;
		sampler2D _Substance;
		sampler2D_float _DistanceField;
		float _EmisScale;

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

		fixed4 frag_srgb(v2f i) : SV_Target
		{
			fixed4 col = tex2D(_MainTex, i.uv);
			return fixed4(pow(col.xyz, 1. / 2.2), 1.);
		}

		fixed4 frag_emis(v2f i) : SV_Target
		{
			fixed e = tex2D(_MainTex, i.uv).a * _EmisScale;
			return fixed4(e, e, e, 1.);
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
				d += dist.x;
				if (dist.x <= TRACE_EPSILON)
				{
					hitPos = samplePoint;
					return true;
				}
			}
			return false;
		}

		ENDCG

		// レンダリングのためのパス
		// rgb	: color
		// a	: emission
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _PrevLighting;
			sampler2D _Noise;
			float _NoiseOffset;

			// 指定した座標の周辺の輝度値を取得する。
			float getEmissionFromPrevLighting(float2 st)
			{
				float2 pix = _ScreenParams.zw - float2(1., 1.);
				pix = 1. / _ScreenParams.xy;
				float e = max(tex2D(_PrevLighting, (st) + pix * float2(-1,1)).w,0.);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(0,1)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(1,1)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(-1,0)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(0,0)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(1,0)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(-1,-1)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(0,-1)).w,0.),e);
				e = max(max(tex2D(_PrevLighting, (st) + pix * float2(1,-1)).w,0.),e);
				return e;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 origin = i.uv;
				float3 color = float3(0., 0., 0.);
				float emis = 0.;

				// ノイズマップから乱数を取得する。
				float2 time = frac(float2(_Time.y, _Time.y) * 0.98);
				float rand = tex2D(_Noise, frac(i.uv + time) * _NoiseOffset).r;
				// rand = 0.;

				// 前回のレンダリング結果
				float4 prevResult = tex2D(_PrevLighting, i.uv);

				for(float i = 0; i < RAYS_PER_PIXEL; ++i)
				{
					float2 hitPos = float2(0, 0);
					float d = 0.;

					float2 ray = float2(cos(i / RAYS_PER_PIXEL * 2 * PI + rand),
										sin(i / RAYS_PER_PIXEL * 2 * PI + rand));

					if(trace(origin, ray, hitPos, d))
					{
						float3 baseColor 	= tex2D(_BaseColor, hitPos).xyz;
						float4 material 	= tex2D(_Substance, hitPos);
						float lastEmimssion = 0.;
						if(material.x < EPSILON && d > EPSILON)
						{
							lastEmimssion = getEmissionFromPrevLighting(hitPos);
							lastEmimssion *= step(0.005, lastEmimssion);
						}
						float r = 2.;
						float att = pow(max(1.0 - (d * d) / (r * r), 0.), 2.);

						// 前回の輝度値を考慮する。
						float emission = material.x + lastEmimssion;

						emis += emission * att;
						color += emission * baseColor * att;
					}
				}
				emis *= (1. / RAYS_PER_PIXEL);
				color *= (1. / RAYS_PER_PIXEL);

				// 前回の結果に対して何割かの確立で今回の結果をブレンドする。
				float integ = 1. / 3.;
				float3 result = (1. - integ) * prevResult.rgb + integ * color;
				return fixed4(result, emis);
			}

			ENDCG
		}

		// レンダリング結果をLinear色空間からsRGB色空間に変換するためのパス
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_srgb
			ENDCG
		}

		// デバッグ用に輝度値を表示するためのパス
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_emis
			ENDCG
		}
	}
}