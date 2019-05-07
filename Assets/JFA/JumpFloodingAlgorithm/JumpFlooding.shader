Shader "Seiro/GPUSandbox/JFA/JumpFlooding"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_JumpStep("Jump Step", float) = 0
		_DistScale("Dist Scale", float) = 1
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
					float4 data = tex2D(_MainTex, sampleUV);
					float dist = length(data.xy - i.uv);
					if (data.z > 0 && bestDist > dist)
					{
						bestDist = dist;
						bestCoord = data.xy;
						flag = 1;
					}
				}
			}

			return float4(bestCoord, flag, 1);
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
			// float aspect = _ScreenParams.x / _ScreenParams.y;
			float2 p = i.uv;
			float2 q = tex2D(_MainTex, i.uv).xy;
			float2 d = p - q;
			// float l = smoothstep(0.00001 , 0.99, length(d));
			float l = lerp(0, 1, length(d) * 1.02 - 0.01) * _DistScale;
			// float l = length(d);
			return float4(l, l, l, 1);
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJF_1
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDist_2
			ENDCG
		}
	}
}
