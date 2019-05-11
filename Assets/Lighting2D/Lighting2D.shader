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
		
		#define DISTANCE_EPSILON 1e-4
		#define EMISSION_EPSILON 1e-2
		#define EPSILON 1e-5

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
		sampler2D _PrevLighting;
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
			return fixed4(pow(col.rgb, 1. / 2.2), 1.);
		}

		fixed4 frag_emis(v2f i) : SV_Target
		{
			fixed e = tex2D(_MainTex, i.uv).a * _EmisScale;
			return fixed4(e, e, e, 1.);
		}

		// 二次元ベクトルから対応するハッシュの生成
		inline float hash12(float2 p)
		{
			float3 p3  = frac(float3(p.xyx) * HASHSCALE1);
			p3 += dot(p3, p3.yzx + 19.19);
			return frac((p3.x + p3.y) * p3.z);
		}

		// 指定した座標の勾配を取得する。
		inline float2 getGradFromDistanceField(float2 st)
		{
			float2 pix = _ScreenParams.zw - float2(1., 1.);
			float x0 = tex2D(_DistanceField, st + pix * float2(-1, 0)).x;
			float x1 = tex2D(_DistanceField, st + pix * float2(1, 0)).x;
			float y0 = tex2D(_DistanceField, st + pix * float2(0, -1)).x;
			float y1 = tex2D(_DistanceField, st + pix * float2(0, 1)).x;
			return float2(x1 - x0, y1 - y0);
		}

		// 指定した座標周辺の輝度値の最大値を取得する。
		inline float getEmissionFromPrevLighting(float2 st)
		{
			float2 pix = _ScreenParams.zw - float2(1., 1.);
			float e = 0.;
			e = max(tex2D(_PrevLighting, st + pix * float2(-1, 1)).w, 0.);
			e = max(max(tex2D(_PrevLighting, st + pix * float2(1, 1)).w, 0.), e);
			e = max(max(tex2D(_PrevLighting, st + pix * float2(-1, -1)).w, 0.), e);
			e = max(max(tex2D(_PrevLighting, st + pix * float2(1, -1)).w, 0.), e);
			float l = length(pix);	// 距離減衰
			return e; // * (1. - l * l);
		}

		// 距離場を利用してレイマーチングを行う。
		bool trace(float2 origin, float2 ray, out float2 hitPos, out float d)
		{
			d = 0;
			for (int i = 0; i < MAX_MARCHING_STEP; ++i)
			{
				float2 samplePoint = origin + ray * d;
				float4 dist = abs(tex2D(_DistanceField, samplePoint));
				d += dist.x;
				if (dist.x <= DISTANCE_EPSILON)
				{
					hitPos = origin + ray * (d + DISTANCE_EPSILON);
					// hitPos = samplePoint;
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

			sampler2D _Noise;
			float _NoiseOffset;

			fixed4 frag (v2f i) : SV_Target
			{	
				// マテリアルを確認
				float4 material = tex2D(_Substance, i.uv);
				float4 baseColor = tex2D(_BaseColor, i.uv);
				
				// 輝度を外部に放射しているような物体
				if(material.x > 0.)
				{
					// 発光している場合はサンプリングが必要無くなるのでこのあとの処理はスキップ可能
					float3 baseColor = tex2D(_BaseColor, i.uv).xyz;
					return fixed4(baseColor, material.x);
				}

#if false
				if(material.y > 0.)
				{
					/*
					// 現在位置の勾配方向に距離場の絶対値の方向に進んだ位置で輝度をサンプリングし
					// 距離による減衰を考慮する。
					// しかしこの方法の場合、勾配が0になる地点でうまく計算が行えなくなる。これはかなり致命的。
					// なので物体内であっても、トレースして輝度を集めるようにした方がいい。
					// 物体内部と、外部での主な違いは、自分のもともと保持している色に輝度を考慮した加算を行うかどうかということ。
					float d = -tex2D(_DistanceField, i.uv).x;	// 物体内にいるので距離場の値は負になっていることに気をつける。
					float2 grad = getGradFromDistanceField(i.uv);
					grad = grad / length(grad);
					float emission = getEmissionFromPrevLighting(i.uv + grad * d);
					float3 baseColor = tex2D(_BaseColor, i.uv);

					float r = material.y;
					float att = pow(max(1.0 - (d * d) / (r * r), 0.), 2.);

					emission *= att;
					float3 result = baseColor * emission;
					// return fixed4(grad * material.y, 0., 1.);
					return fixed4(result, emission * (material.y + d));
					// return fixed4(result, 0.);
					*/

					/*
					float dist = tex2D(_DistanceField, i.uv).x;
					float emission = getEmissionFromPrevLighting(i.uv);
					float3 baseColor = tex2D(_BaseColor, i.uv);
					float3 result = baseColor * emission;
					return fixed4(result, emission * (material.y + dist));
					*/
				}
#endif

				float2 origin = i.uv;
				float3 color = float3(0., 0., 0.);
				float emis = 0.;
				float dist = 0.;
				float minDist = 1e+3;

				// ノイズマップから乱数を取得する。
				float2 time = frac(float2(_Time.y, _Time.y) * 0.98);
				float rand = tex2D(_Noise, frac(i.uv + time) * _NoiseOffset).r;
				// rand = 0.;

				// 前回のレンダリング結果
				float4 prevResult  = tex2D(_PrevLighting, i.uv);

				[loop]
				for(float i = 0; i < RAYS_PER_PIXEL; ++i)
				{
					float2 hitPos = float2(0, 0);
					float d = 0.;

					float2 ray = float2(cos(i / RAYS_PER_PIXEL * 2. * PI + rand),
										sin(i / RAYS_PER_PIXEL * 2. * PI + rand));

					if(trace(origin, ray, hitPos, d))
					{
						float3 baseColor 	= tex2D(_BaseColor, hitPos).xyz;
						float4 material 	= tex2D(_Substance, hitPos);
						float lastEmimssion = 0.;
						if(material.x < EPSILON && d > EPSILON)
						{
							lastEmimssion = getEmissionFromPrevLighting(hitPos);
							lastEmimssion *= step(EMISSION_EPSILON, lastEmimssion);	// 低すぎる輝度は除外
						}
						float r = 2.;
						float att = pow(max(1.0 - (d * d) / (r * r), 0.), 2.);

						// 前回の輝度値を考慮する。
						float emission = material.x + lastEmimssion;

						color += emission * baseColor * att;
						emis += emission * att;
						dist += d;
						minDist = min(minDist, d * 50.);
					}
				}
				float invRaysPerPixel = 1. / RAYS_PER_PIXEL;
				emis  *= invRaysPerPixel;
				color *= invRaysPerPixel;
				dist  *= invRaysPerPixel;

				if(material.y > 0.)
				{
					// 表面化散乱する物体

					// float3 result = baseColor.rgb * (emis / (dist * dist));
					float t = emis * material.y;// * (1. - minDist) + material.y;
					float3 result = float3(t, t, t);
					result = baseColor.rgb * t;
					return fixed4(result, t);
				}
				else
				{
					// それ以外の物体
					float integ = 1. / 2.;
					float3 result = (1. - integ) * prevResult.rgb + integ * color;
					return fixed4(result * step(EMISSION_EPSILON, emis), emis);	// 輝度が低すぎる場合は除外
				}
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