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

		#define PI 3.14159265

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
			o.uv.y = 1 - o.uv.y;	// 自前で用意したRenderTextureに対してはy座標は反転させる。
			return o;
		}

		sampler2D_float _Params;	// (x, y) : 速度場, (z) : 渦度, (w) : 圧力
		float4 _Params_TexelSize;

		float _DT;					// delta time
		float4 _SimConstants;		// (x) : 渦度係数, (y) : 動粘性係数, (z) : 速度移流減衰, (w) : 色移流減衰

		// 発散は圧力計算時に計算するようにしたので、これは未使用。
		// 指定した座標の速度場の発散を計算
		float div_velocity(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return ((p_r.x - p_l.x) + (p_u.y - p_d.y)) * 0.5;
		}

		// 指定した座標の速度場の回転とそれによって生じるベクトルを計算
		void calc_vorticity(float2 x, out float3 vort)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			vort = float3(abs(p_u.z) - abs(p_d.z)
			,abs(p_l.z) - abs(p_r.z)
			,p_d.x - p_u.x + p_r.y - p_l.y);
		}

		// 指定した座標の速度場の発散を0に抑えるための圧力を計算
		float calc_pressure(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			// 発散はこの場で計算する
			float d = (p_r.x - p_l.x) + (p_u.y - p_d.y);
			return (p_r.w + p_l.w + p_u.w + p_d.w - d * 0.5) * 0.25;
		}

		// 圧力場の勾配を出力
		float2 grad_pressure(float2 x)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return float2((p_r.w - p_l.w) * 0.5, (p_u.w - p_d.w) * 0.5);
		}

		// 速度場のラプラシアンを計算
		float2 lap_velocity(float2 x, float2 v)
		{
			float4 p_r = tex2D(_Params, x + float2(_Params_TexelSize.x, 0));
			float4 p_l = tex2D(_Params, x + float2(-_Params_TexelSize.x, 0));
			float4 p_u = tex2D(_Params, x + float2(0, _Params_TexelSize.y));
			float4 p_d = tex2D(_Params, x + float2(0, -_Params_TexelSize.y));
			return p_r.xy + p_l.xy + p_u.xy + p_d.xy - 4 * v;
		}

		// テクスチャを0で初期化
		float4 frag_clear(v2f i) : SV_Target
		{
			return float4(0, 0, 0, 0);
		}

		float boxSDF(float2 p, float2 size)
		{
			float2 r = abs(p) - size;
			return min(max(r.x, r.y), 0.) + length(max(r, float2(0, 0)));
		}

		// 境界付近のパラメータだけを初期化する。
		float4 frag_clear_boundaries(v2f i) : SV_Target
		{
			float d = boxSDF(i.uv - .5, .5 - _Params_TexelSize.xy);
			float4 src = tex2D(_Params, i.uv);
			return lerp(float4(0,0,0,0.5), src, step(d, 0));
		}

		sampler2D _SourceTex;

		// _SourceTexの内容をコピーする
		fixed4 frag_copy(v2f i) : SV_Target
		{
			return tex2D(_SourceTex, i.uv);
		}

		// 速度場の発散を計算する
		float4 frag_calc_div(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.z = div_velocity(i.uv);
			return p;
		}

		// 速度場の渦度を計算しそれを速度場に適用する
		float4 frag_apply_vorticity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float3 vort;
			calc_vorticity(i.uv, vort);
			p.xy += vort.xy * (_SimConstants.x / length(vort.xy + 1e-9) * vort.z);
			p.z = vort.z;
			return p;
		}

		// 速度場に拡散粘性を適用する。
		float4 frag_apply_viscosity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.xy += _SimConstants.y * lap_velocity(i.uv, p.xy);
			return p;
		}

		// 速度場の発散を0にするための圧力を計算する
		// これを複数回呼び出すことでpoisson方程式を解く
		float4 frag_calc_pres(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.w = calc_pressure(i.uv);
			return p;
		}

		// 速度場に圧力場の勾配を適用する。
		float4 frag_apply_pres(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			p.xy -= grad_pressure(i.uv);
			return p;
		}

		sampler2D _MainTex;

		// 色を移流させる
		fixed4 frag_advect_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			fixed4 c = tex2D(_MainTex, i.uv - (p.xy * _DT));
			return c * _SimConstants.w;		// 速度場による色の移流と減衰
		}

		// 速度場を移流させる
		float4 frag_advect_velocity(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float4 q = tex2D(_Params, i.uv - (p.xy * _DT));
			p.xy = q.xy * _SimConstants.z;	// 速度の移流による減衰;
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

		fixed4 _MouseColor;

		// マウスの位置に描画
		fixed4 frag_draw_circle(v2f i) : SV_Target
		{
			fixed4 col = tex2D(_MainTex, i.uv);
			float2 d = i.uv - _Mouse.xy;
			float r2 = _Mouse.z * _Mouse.z;
			float c = sdf_circle(d, r2);
			return lerp(col, _MouseColor, step(c, 0));
		}

		sampler2D _InputParamsMask;		// 入力用のパラメータを適用するためのマスク。もったいないけどxの0, 1で判定する。
		sampler2D_float _InputParams;	// 入力用のパラメータ。パラメータの配置は_Paramsと同様。

		float4 frag_apply_input_params(v2f i) : SV_Target
		{
			fixed4 mask = tex2D(_InputParamsMask, i.uv);
			float4 params = tex2D(_Params, i.uv);
			float4 inParams = tex2D(_InputParams, i.uv);
			return lerp(params, inParams, mask);			// このlerpってベクトルの要素毎に働くんだっけ？働いてくれるとすごく便利
		}

		// 障害物マップ
		sampler2D _ObstacleMap;			// x > 0 : 障害物が存在

		float4 frag_apply_obstacle_map(v2f i) : SV_Target
		{
			fixed4 obstacle = tex2D(_ObstacleMap, i.uv);
			float4 params = tex2D(_Params, i.uv);
			// return obstacle;
			return lerp(params, float4(0,0,0,0.5), step(0.9, obstacle.x));
		}

		// 任意のパラメータの移流
		sampler2D_float _FollowerTex;
		float4 _FollowerDissipation;

		float4 frag_advect_follower(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.uv);
			float4 f = tex2D(_FollowerTex, i.uv - p.xy * _DT);
			return f * _FollowerDissipation * _DT;
		}

		float3 _FollowerArea;
		float4 _FollowerParams;

		float4 frag_write_follower(v2f i) : SV_Target
		{
			float4 f = tex2D(_FollowerTex, i.uv);
			float2 d = i.uv - _FollowerArea.xy;
			float r2 = _FollowerArea.z * _FollowerArea.z;
			float c = sdf_circle(d, r2);
			return lerp(f, _FollowerParams, step(c, 0));
		}

		// それぞれのパラメータの描画
		float4 _RenderingParams;
		fixed4 frag_velocity_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.st);
			return fixed4(length(p.xy) * _RenderingParams.rgb, 1);
		}
		fixed4 frag_vorticity_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.st);
			return fixed4(p.z * _RenderingParams.rgb, 1);
		}
		fixed4 frag_pressure_color(v2f i) : SV_Target
		{
			float4 p = tex2D(_Params, i.st);
			return fixed4(p.w * _RenderingParams.rgb, 1);
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
			#pragma fragment frag_clear_boundaries
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
			#pragma fragment frag_apply_vorticity
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_apply_viscosity
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
			#pragma fragment frag_draw_circle
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_apply_obstacle_map
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_advect_follower
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_write_follower
			ENDCG
		}

		// ここからそれぞれのパラメータの描画
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_velocity_color
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_vorticity_color
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_pressure_color
			ENDCG
		}
	}
}
