Shader "Hidden/BilateralFilter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#include "UnityCG.cginc"

		#define PI 3.14159265

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

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		int _KernelSize;
		float _BSigma;
		float _Weights[41];

		inline float weight(float3 e, float s)
		{
			return exp(-dot(e, e) / (2. * s)) / (sqrt(2. * PI * s));
		}

		fixed4 frag_b(v2f IN) : SV_Target
		{
			float3 v = float3(0, 0, 0);
			float3 c = tex2D(_MainTex, IN.uv).rgb;
			float3 cc = float3(0, 0, 0);
			float3 final = float3(0, 0, 0);
			float sw = 0;
			for(int y = -_KernelSize; y < _KernelSize; ++y)
			{
				for(int x = -_KernelSize; x < _KernelSize; ++x)
				{
					cc = tex2D(_MainTex, IN.uv + _MainTex_TexelSize.xy * float2(x, y)).rgb;
					float w = weight(c - cc, _BSigma) * 20.0 * _Weights[_KernelSize + x] * _Weights[_KernelSize + y];
					sw += w;
					final += cc * w;
				}
			}
			return fixed4(final / sw, 1.);
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_b
			ENDCG
		}
	}
}
