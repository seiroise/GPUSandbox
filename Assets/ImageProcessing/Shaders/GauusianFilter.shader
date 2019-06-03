Shader "Hidden/GausianFilter"
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
		#include "Assets/CGInc/ShaderUtils.cginc"

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
		float4  _MainTex_TexelSize;

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		int _KernelSize;
		float _Weights[21];
		float _InvFactor;

		fixed4 frag_u(v2f IN) : SV_Target
		{
			float3 v = tex2D(_MainTex, IN.uv).rgb * _Weights[0];
			for(int i = 1; i < _KernelSize; ++i)
			{
				v += tex2D(_MainTex, IN.uv + _MainTex_TexelSize.xy * float2( i, 0)).rgb * _Weights[i];
				v += tex2D(_MainTex, IN.uv + _MainTex_TexelSize.xy * float2(-i, 0)).rgb * _Weights[i];
			}
			return fixed4(v * _InvFactor, 1.f);
		}

		fixed4 frag_v(v2f IN) : SV_Target
		{
			float3 v = tex2D(_MainTex, IN.uv).rgb * _Weights[0];
			for(int i = 1; i < _KernelSize; ++i)
			{
				v += tex2D(_MainTex, IN.uv + _MainTex_TexelSize.xy * float2( 0, i)).rgb * _Weights[i];
				v += tex2D(_MainTex, IN.uv + _MainTex_TexelSize.xy * float2( 0,-i)).rgb * _Weights[i];
			}
			return fixed4(v * _InvFactor, 1.f);
		}

		ENDCG

		// 横方向のブラー
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_u
			ENDCG
		}

		// 縦方向のブラー
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_v
			ENDCG
		}
	}
}
