Shader "Seiro/SPH/SPH2D_SimpleInstancing"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Dye("Dye", Vector) = (1.0, 1.0, 1.0, 1.0)
		_Smoothlen("Smoothlen", float) = 0.1
	}
		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparet" }
			Cull Off ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM

				#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "Assets/CGInc/Particles.cginc"
				#include "Assets/CGInc/SPH.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Dye;
				float _Smoothlen;

				#if SHADER_TARGET >= 45
					StructuredBuffer<SPH_Particle2D> buf;
				#endif

				v2f vert(appdata v, uint id : SV_InstanceID)
				{
					SPH_Particle2D p = buf[id];

					float2 localPosition = v.vertex.xy * _Smoothlen + p.position;

					v2f o;
					o.vertex = mul(UNITY_MATRIX_VP, float4(localPosition, 0.0, 1.0));
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					// o.color = float4(p.color, 1.0);
					o.color = float4(1.0, p.density, p.pressure, 1.0) * _Dye;
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