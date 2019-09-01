Shader "Hidden/Seiro/FluidRenderer2D"
{
	Properties
	{
		[PreRendererDaata]_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always Blend Off

		CGINCLUDE

		#include "UnityCG.cginc"

		#ifndef PI
			#define PI 3.14159265
		#endif

		struct appdata
		{
			float4 vertex	: POSITION;
			float2 uv		: TEXCOORD0;
		};

		struct v2f
		{
			float2 uv		: TEXCOORD0;
			float2 st		: TEXCOORD1;
			float4 vertex	: SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = o.st = v.uv;
			o.uv.y = 1 - o.uv.y;	// 自前で用意したRenderTextureに対してはy座標は反転させる。
			return o;
		}

		sampler2D_float _Follower;
		sampler2D _Pallete;

		fixed4 frag_color(v2f i) : SV_Target
		{
			float4 f = tex2D(_Follower, i.uv);
			fixed4 c = tex2D(_Pallete, float2(0.5, f.x));
			return c;
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_color
			ENDCG
		}
	}
}