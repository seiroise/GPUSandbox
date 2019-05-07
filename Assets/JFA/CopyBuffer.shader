Shader "Hidden/CopyBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Scale ("Scale", Vector) = (1, 1, 1, 1)
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
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

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _Scale;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
#if UNITY_UV_STARTS_AT_TOP
				if(_MainTex_TexelSize.y > 0)
				{
					o.uv.y = 1 - v.uv.y;
				}
#endif
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 v = tex2D(_MainTex, i.uv);
				return v * _Scale;
			}
			ENDCG
		}
	}
}
