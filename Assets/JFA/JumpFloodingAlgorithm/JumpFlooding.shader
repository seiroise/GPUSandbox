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

		sampler2D _MainTex;	// 取得する対象のデータ
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
			for (int y = -1; y <= 1; ++y)
			{
				for (int x = -1; x <= 1; ++x)
				{
					float2 sampleUV = i.uv + float2(x, y) * _JumpStep;
					if (sampleUV.x < 0 || sampleUV.y < 0 || 1 <= sampleUV.x || 1 <= sampleUV.y)
					{
						continue;
					}
					float4 q = tex2D(_MainTex, sampleUV);
					float2 qCoord = q.xy;
					if (q.z < 1)
					{
						continue;
					}
					float dist = length(i.uv - qCoord);
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
			p.w = bestDist;
			return p;
		}

		float4 fragDist(v2f i) : SV_Target
		{
			// シード座標との距離を計算
			float4 p = tex2D(_MainTex, i.uv);
			float len = length(p.xy - i.uv) * _DistScale;
			return float4(len, len, len, 1); 
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJF
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDist
			ENDCG
		}
	}
}
