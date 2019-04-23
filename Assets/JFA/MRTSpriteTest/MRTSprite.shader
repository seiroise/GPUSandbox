Shader "Seiro/GPUSandbox/JFA/MRTSprite"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}

			Cull Off
			Lighting Off
			ZWrite Off
			Blend One OneMinusSrcAlpha

			CGINCLUDE

			#include "UnitySprites.cginc"

			struct mrtbuffer
			{
				fixed4 color	: SV_Target;
				float4 substance: SV_Target1;
			};

			mrtbuffer SpriteFragMRT(v2f IN)
			{
				mrtbuffer o;
				fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
				c.rgb *= c.a;
				o.color = c;
				float2 uv = IN.vertex.xy / _ScreenParams.xy;
				o.substance = float4(uv, 0, 1);	// seedになるピクセルはseedと最短座標は同一。
				return o;
			}

			ENDCG

			Pass
			{
			CGPROGRAM
				#pragma vertex SpriteVert
				#pragma fragment SpriteFragMRT
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile _ PIXELSNAP_ON
				#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			ENDCG
			}
		}
}
