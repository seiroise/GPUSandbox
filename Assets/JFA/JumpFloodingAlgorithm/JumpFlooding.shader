Shader "Seiro/GPUSandbox/JFA/JumpFlooding"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Substance("Substance", 2D) = "white" {}
		_JumpStep("Jump Step", float) = 0
		_DistScale("Dist Scale", float) = 1
		_EdgeThreshold("Edge Threshold", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		CGINCLUDE

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

		sampler2D_float _MainTex;	// 取得する対象のデータ
		float4 _MainTex_TexelSize;
		float _JumpStep;	// 今回のJFAのステップ幅
		float _DistScale;	// 距離描画時のスケール

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y > 0)
			{
				o.uv.y = 1 - v.uv.y;
			}
#endif
			return o;
		}

		float4 fragJF(v2f i) : SV_Target
		{
			// 最も近いシードの座標 : (x, y)
			float4 p = tex2D(_MainTex, i.uv);
			float2 bestCoord = float2(-1, -1);
			float flag = 0;
			float bestDist = 9999.0;
			float2 spos = i.uv * _ScreenParams.xy;
			for (float y = -1; y <= 1; ++y)
			{
				for (float x = -1; x <= 1; ++x)
				{
					float2 sampleUV = spos + float2(x, y) * _JumpStep;
					float4 q = tex2D(_MainTex, sampleUV / _ScreenParams.xy);
					float2 qCoord = q.xy;
					if (q.z < 1)
					{
						continue;
					}
					float2 pCoord = i.uv;
					float dist = length(pCoord - qCoord);
					if (bestDist > dist)
					{
						bestDist = dist;
						bestCoord = qCoord;
						flag = q.z;
					}
				}
			}

			p.xy = bestCoord;
			p.z = flag;
			return p;
		}

		float4 fragJF_1(v2f i) :SV_Target
		{
			float aspect = _ScreenParams.x / _ScreenParams.y;

			float4 data = tex2D(_MainTex, i.uv);
			float2 pCoord = i.uv * _ScreenParams.xy;

			float bestDist = 9999.0;
			float2 bestCoord = float2(0, 0);
			float flag = 0;

			for (float y = -1; y <= 1; ++y)
			{
				for (float x = -1; x <= 1; ++x)
				{
					float2 sampleCoord = pCoord + float2(x, y) * _JumpStep;
					float2 sampleUV = sampleCoord / _ScreenParams.xy;
					float4 d = tex2D(_MainTex, sampleUV);
					float dist = length(d.xy - i.uv);
					if (d.z > 0 && bestDist > dist)
					{
						bestDist = dist;
						bestCoord = d.xy;
						flag = 1;
					}
				}
			}

			return float4(bestCoord, flag, data.w);
		}

		float4 fragDist(v2f i) : SV_Target
		{
			// シード座標との距離を計算
			// 距離計算は、アスペクト比を考慮する必要があるので注意する。
			float aspect = _ScreenParams.x / _ScreenParams.y;
			float2 p = i.uv * aspect;
			float2 q = tex2D(_MainTex, i.uv).xy * aspect;
			float2 d = p - q;
			// return float4(d * _DistScale, 0, 1);
			float len = sqrt(dot(d, d)) * _DistScale;
			// return pow(float4(p-q, 0, 1), 100);
			// return float4(i.uv, 0, 1);
			// return tex2D(_MainTex, i.uv);
			
			return float4(len, len, len, 1);
		}

		float4 fragDist_1(v2f i) : SV_Target
		{
			float aspect = _ScreenParams.x / _ScreenParams.y;
			float2 p = i.uv * aspect * _ScreenParams.xy;
			float2 q = tex2D(_MainTex, i.uv).xy * aspect * _ScreenParams.xy;
			// float2 p = i.uv * aspect;
			// float2 q = tex2D(_MainTex, i.uv).xy * aspect;
			float2 d = p - q;
			// float l = round(length(d) * _DistScale;
			float l = smoothstep( 0.001 , 0.999 , length(d) * _DistScale);
			return float4(l, l, l, 1);
		}

		float4 fragDist_2(v2f i) : SV_Target
		{
			float2 p = i.uv;
			float4 data = tex2D(_MainTex, i.uv);
			float2 q = data.xy;
			float2 d = p - q;
			// そのまま出力すると細かい誤差が残るので、下駄をはかせて誤差を取り除く
			// float l = lerp(0, 1, length(d) * 1.02 - 0.01) * _DistScale;
			float l = length(d) * _DistScale;
			// l = floor(l * 256) / 256;
			// data.wが1の場合は物体の内部ということになるので、負の値をとるようにする。
			l = l + (data.w * l * -2.) + 0.001;
			return float4(l, l, l, 1);
		}

		// jump flooding用にエッジ検出を行う。
		// 使用するのはw
		float _EdgeThreshold;
		float4 frag_edge(v2f i) : SV_Target
		{
			float4 t = tex2D(_MainTex, i.uv);

			// 2パスに分けた方がよい。(分けられる？
			float dx = (_ScreenParams.z - 1.);
			float dy = (_ScreenParams.w - 1.);

			float c00 = tex2D(_MainTex, i.uv + float2(-dx, -dy)).w;
			float c01 = tex2D(_MainTex, i.uv + float2( .0, -dy)).w;
			float c02 = tex2D(_MainTex, i.uv + float2( dx, -dy)).w;
			float c10 = tex2D(_MainTex, i.uv + float2(-dx,  .0)).w;
			float c12 = tex2D(_MainTex, i.uv + float2( dx,  .0)).w;
			float c20 = tex2D(_MainTex, i.uv + float2(-dx,  dy)).w;
			float c21 = tex2D(_MainTex, i.uv + float2( .0,  dy)).w;
			float c22 = tex2D(_MainTex, i.uv + float2( dx,  dy)).w;
			
			float sx = (-1.0 * c00) + (-2.0 * c10) + (-1.0 * c20) + (1.0 * c02) + (2.0 * c12) + (1.0 * c22);
			float sy = (-1.0 * c00) + (-2.0 * c01) + (-1.0 * c02) + (1.0 * c20) + (2.0 * c21) + (1.0 * c22);

			float g = sqrt(sx * sx + sy * sy);
			// t.z = g >= _EdgeThreshold ? 1. : 0.;
			float edge = step(_EdgeThreshold, g);
			t.xy = i.uv * edge;
			t.z = edge;
			return t;
		}

		// jump flooding用のエッジ検出をマテリアルの境を考慮して行う。
		sampler2D _Substance;
		float4 frag_edge_with_substance(v2f i) : SV_Target
		{
			float4 t = tex2D(_MainTex, i.uv);

			// 2パスに分けた方がよい。(分けられる？
			float dx = (_ScreenParams.z - 1.);
			float dy = (_ScreenParams.w - 1.);

			float4 c00 = tex2D(_Substance, i.uv + float2(-dx, -dy));
			float4 c01 = tex2D(_Substance, i.uv + float2(.0, -dy));
			float4 c02 = tex2D(_Substance, i.uv + float2(dx, -dy));
			float4 c10 = tex2D(_Substance, i.uv + float2(-dx,  .0));
			float4 c12 = tex2D(_Substance, i.uv + float2(dx,  .0));
			float4 c20 = tex2D(_Substance, i.uv + float2(-dx,  dy));
			float4 c21 = tex2D(_Substance, i.uv + float2(.0,  dy));
			float4 c22 = tex2D(_Substance, i.uv + float2(dx,  dy));

			float4 sx = (-1.0 * c00) + (-2.0 * c10) + (-1.0 * c20) + (1.0 * c02) + (2.0 * c12) + (1.0 * c22);
			float4 sy = (-1.0 * c00) + (-2.0 * c01) + (-1.0 * c02) + (1.0 * c20) + (2.0 * c21) + (1.0 * c22);

			float4 g = sqrt(sx * sx + sy * sy);
			float edge = step(_EdgeThreshold, dot(g, g));
			t.xy = i.uv * edge;
			t.z = edge;
			return t;
		}

		ENDCG

		// jump flooding用のパス
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJF_1
			ENDCG
		}

		// jump flooding後の距離計算用のパス
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDist_2
			ENDCG
		}

		// 前処理用のエッジ検出用のパス
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_edge
			ENDCG
		}

		// 前処理用のエッジ検出用のパス
		// 上のパスとは異なり、それぞれの要素を考慮する。
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_edge_with_substance
			ENDCG
		}
	}
}
