Shader "Seiro/GPUSandbox/JFA/JFStep"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_JumpStep("Jump Step", float) = 0
	}

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Off

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
		float _JumpStep;	// 今回のJFAのステップ幅

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		float4 fragJF(v2f i) : SV_Target
		{
			// 最も近いシードの座標 : (x, y)
			float4 p = tex2D(_MainTex, i.uv);
			float bestDist = 9999.0;
			float2 bestCoord = p.xy;
			for (int y = -1; y <= 1; ++y)
			{
				for (int x = -1; x <= 1; ++x)
				{
					float2 sampleUV = i.uv + float2(x, y) * _JumpStep;
					float4 q = tex2D(_MainTex, sampleUV);
					float2 qCoord = q.xy;
					if (qCoord.x <= 0 || qCoord.y <= 0)
					{
						continue;
					}
					float dist = length(i.uv - qCoord);
					if (bestDist > dist)
					{
						bestDist = dist;
						bestCoord = qCoord;
					}
				}
			}
			p.xy = bestCoord;
			p.z = bestDist;
			return p;
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJF
			ENDCG
		}
	}
}
