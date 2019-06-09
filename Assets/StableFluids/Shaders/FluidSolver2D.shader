Shader "Hidden/FluidSolver2D"
{
	Properties
	{
		[PreRendererData] _MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always Blend Off

		CGINCLUDE

		#include "UnityCG.cginc"

		#define DT 0.016

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float2 st : TEXCOORD1;
			float4 vertex : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = o.st = v.uv;
			o.uv.y = 1 - o.uv.y;
			return o;
		}
		
		sampler2D_float _Params;	// (x, y) : 速度場, (z) : 発散, (w) : 圧力
		float4 _Params_TexelSize;

		float calc_divergence(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return (p_r.x - p_l.x) + (p_u.y - p_d.y);
		}

		float calc_pressure(float2 x, float div)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return (p_r.w + p_l.w + p_u.w + p_d.w + div) * 0.25;
		}

		float2 grad_pressure(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return float2((p_r.w - p_l.w) * .5, (p_u.w - p_d.w) * .5);
		}

		float4 frag_clear(v2f i) : SV_Target
		{
			return float4(0, 0, 0, 0);
		}

		sampler2D _SourceTex;

		fixed4 frag_copy(v2f i) : SV_Target
		{
			return tex2D(_SourceTex, i.uv);
		}

		float4 frag_calc_div(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.z = calc_divergence(i.uv);
			return p;
		}

		float4 frag_calc_pres(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.w = calc_pressure(i.uv, -p.z);
			return p;
		}

		float4 frag_apply_pres(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.xy -= grad_pressure(i.uv);
			return p;
		}

		sampler2D _MainTex;

		fixed4 frag_advect_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			return tex2D(_MainTex, i.uv - (p.xy * DT));
		}

		float4 frag_advect_velocity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float4 q = tex2D(_Params, i.uv - (p.xy * DT));
			p.xy = q.xy;
			return p;
		}

		float4 _Mouse;	//(x, y) : point, (z) : radius, (w) : force
		float2 _ForceDir;

		float sdf_circle(float2 p, float r2)
		{
			return dot(p, p) - r2;
		}

		float sdf_line_seg(float2 p, float2 s, float2 e, float w)
		{
			float2 xAxis = normalize(e - s);
			float2 yAxis = float2(xAxis.y, -xAxis.x);
			float2 l = p - s;
			float len = length(l);
			float2 r = float2(dot(xAxis, l), dot(yAxis, l));
			float2 c = float2(clamp(r.x, 0., len), clamp(r.y, -w * .5, w * .5));
			return length(r - c);
		}

		float4 frag_mouse(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float2 d = i.uv - _Mouse.xy;
			float r2 = _Mouse.z * _Mouse.z;
			float c = sdf_circle(d, r2);
			p.xy += lerp(float2(0, 0), _ForceDir * _Mouse.w, step(c, 0));
			return p;
		}

		float4 _LineSeg;	// (x, y) : start, (z, w) : end
		float _LineWidth;

		float4 frag_line_seg(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float d = sdf_line_seg(i.uv, _LineSeg.xy, _LineSeg.zw, _LineWidth);
			p.xy += lerp(float2(0, 0), _ForceDir, step(d, 0));
			return p;
		}

		float4 frag_velocity_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.st);
			return length(p.xy);
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_clear
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_copy
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_calc_div
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_calc_pres
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_apply_pres
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_advect_color
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_advect_velocity
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_mouse
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_line_seg
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_velocity_color
			ENDCG
		}
	}
}
