// CGS用のパーティクルをインスタンシングを利用して描画する。
Shader "Custom/CGS2D_InstancingParticle"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Cull Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGINCLUDE

		#include "UnityCG.cginc"
		#include "Assets/CGINC/Particles.cginc"
		#include "Assets/CGINC/Random.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			fixed4 col : COLOR;
		};

		sampler2D _MainTex, _Pallete;
		float4 _MainTex_ST;

		StructuredBuffer<CGS_Particle2D> buf;

		v2f vert(appdata v, uint id : SV_InstanceID)
		{
			v2f o;

			CGS_Particle2D p = buf[id];
			float2 localPosition = v.vertex.xy * p.alive * p.radius + p.position;
			o.vertex = mul(UNITY_MATRIX_VP, float4(localPosition, 0, 1));
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);

			float u = hash12_sin(float2(id, 0));
			o.col = tex2Dlod(_Pallete, float4(u, 0, 0, 0));	// vertex shader時点ではdepthのソートが行われる前段階なのでlodが計算されていない。tex2Dlodのwはlodの指定。忘れないように。
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			fixed4 c = tex2D(_MainTex, i.uv);
			c.rgb *= i.col.rgb;
			return c;
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}