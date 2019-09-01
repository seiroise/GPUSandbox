Shader "Hidden/Seiro/FluidSolver2D-1"
{
	Properties
	{
		[PreRendererData]_MainTex ("Texture", 2D) = "white" {}
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

		float _DT;					// 時間差分
		float4 _SimConstants;		// x = 

		sampler2D_float _Params;
		float4 _Params_TexelSize;

		sampler2D_float _Follower;	// 速度場情報によって流れる値。

		float calc_pressure(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x - float2(_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x - float2(0, _Params_TexelSize.y));
			// 発散はこの場で計算する
			float d = (p_r.x - p_l.x) + (p_u.y - p_d.y);
			return (p_r.w + p_l.w + p_u.w + p_d.w - d * 0.5) * 0.25;
		}

		float2 calc_pressure_gradient(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x - float2(_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x - float2(0, _Params_TexelSize.y));
			return float2((p_r.w - p_l.w) * 0.5, (p_u.w - p_d.w) * 0.5);
		}

		// >> fragment 

		float4 frag_calc_pressure(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.w = calc_pressure(i.uv);
			return p;
		}

		float4 frag_apply_velocity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.xy -= calc_pressure_gradient(i.uv);
			return p;
		}

		float4 frag_advect_velocity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float4 q = tex2D(_Params, i.uv - (p.xy * _DT));
			p.xy = q.xy * _SimConstants.z;
			return p;
		}

		float frag_advect_follower(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float4 f = tex2D(_Follower, i.uv - p.xy * _DT);
			return f * _SimConstants.w;
		}

		// <<

		ENDCG

		// 速度場の発散を0にするための圧力場の計算
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_calc_pressure
			ENDCG
		}

		// 圧力場の勾配を速度場に適応
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_apply_velocity
			ENDCG
		}

		// 速度場の情報を速度情報を元に伝搬させる
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_advect_velocity
			ENDCG
		}

		// 値を伝搬させる
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_advect_follower
			ENDCG
		}
	}
}