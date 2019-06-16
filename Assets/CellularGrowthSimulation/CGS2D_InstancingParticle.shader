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

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;

		StructuredBuffer<CGS_Particle2D> buf;

		v2f vert(appdata v, uint id : SV_InstanceID)
		{
			v2f o;

			CGS_Particle2D p = buf[id];
			float2 localPosition = v.vertex.xy * p.alive * p.radius + p.position;
			o.vertex = mul(UNITY_MATRIX_VP, float4(localPosition, 0, 1));
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			fixed4 col = tex2D(_MainTex, i.uv);
			return col;
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