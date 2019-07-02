// CGS用のエッジをインスタンシングを利用して描画する。
Shader "Custom/CGS2D_InstancingEdge"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_Thickness ("Thickness", Range(0, 1)) = .2 
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		Cull Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGINCLUDE

		#include "UnityCG.cginc"
		#include "Assets/CGINC/Particles.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			uint vid : SV_VertexID;	// 頂点番号。初めて使うけどこんなのもあるのね。
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		fixed4 _Color;
		sampler2D _MainTex;
		float4 _MainTex_ST;
		float _Thickness;

		StructuredBuffer<CGS_Edge2D> _Edges;
		StructuredBuffer<CGS_Particle2D> _Particles;

		v2f vert(appdata v, uint id : SV_InstanceID)
		{
			v2f o;
			CGS_Edge2D e = _Edges[id];
			CGS_Particle2D pa = _Particles[e.a];
			CGS_Particle2D pb = _Particles[e.b];

			float2 d = pb.position - pa.position;
			float len = length(d);

			// scale
			float2 pos = v.vertex;
			pos.x *= len;
			pos.y *= _Thickness;

			// rotation
			float2 n = normalize(pb.position - pa.position);
			float rad = atan2(n.y, n.x);
			float c = cos(rad);
			float s = sin(rad);
			pos = float2(pos.x * c - pos.y * s,
						 pos.x * s + pos.y * c);
			
			// transform
			pos = pos + (pb.position + pa.position) * .5;
			
			float4 vertex = float4(pos, 0, 1);
			o.vertex = UnityObjectToClipPos(vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);

			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			fixed4 col = tex2D(_MainTex, i.uv);
			col.rgb *= _Color.rgb;
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
