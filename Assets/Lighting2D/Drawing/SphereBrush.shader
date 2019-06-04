Shader "Hidden/SphereBrush"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

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
				float2 st : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			float2 _Screen;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv.y = 1 - o.uv.y;
				o.st = o.uv * _Screen;
				return o;
			}

			struct mrtoutput
			{
				fixed4 color		: COLOR0;
				fixed4 substance	: COLOR1;
			};

			sampler2D _BaseColor;
			sampler2D _Substance;

			fixed4 _C;
			fixed4 _S;

			float2 _Position;
			float _Radius2;
			
			float sphere(float2 p, float r2)
			{
				float2 d = _Position - p;
				return sqrt(dot(d, d)) - r2;
			}

			mrtoutput frag (v2f i)
			{
				mrtoutput o;
				float s = sphere(i.st, _Radius2);
				o.color		= lerp(tex2D(_BaseColor, i.uv), fixed4(_C.rgb, 1), step(s, 0));
				o.substance = lerp(tex2D(_Substance, i.uv), _S, step(s, 0));
				return o;
			}
			ENDCG
		}
	}
}
