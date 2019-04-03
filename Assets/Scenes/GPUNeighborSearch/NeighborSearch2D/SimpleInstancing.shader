Shader "Seiro/NS/SimpleInstancing"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Cull Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM

			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "./../Common/NeighborSearch2D.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

#if SHADER_TARGET >= 45
			StructuredBuffer<Particle2D> buf;
#endif

			v2f vert(appdata v, uint id : SV_InstanceID)
			{
				Particle2D p = buf[id];

				float2 localPosition = v.vertex.xy + p.position.xy;

				v2f o;
				o.vertex = mul(UNITY_MATRIX_VP, float4(localPosition, 0.0, 1.0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = float4(p.color, 1.0);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb *= i.color.rgb;
				return col;
			}

			ENDCG
		}
	}
}