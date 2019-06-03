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
				o.st = v.uv * _Screen;
				return o;
			}

			sampler2D _MainTex;

			fixed4 _Color;
			float4 _Material;
			float2 _Position;
			float _Radius2;
			
			float sphere(float2 p, float r2)
			{
				float2 d = _Position - p;
				return sqrt(dot(d, d)) - r2;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return sphere(i.st, _Radius2) <= 0 ? fixed4(_Color.rgb, 1) : tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
