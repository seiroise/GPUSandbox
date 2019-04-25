Shader "Seiro/GPUSandbox/JFA/MRTSprite"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_AlphaClip("Alpha Clip", range(0, 1)) = 0.01
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
			// MRT使用時はそれぞれにBlendingを設定する必要があるよね。
			Blend 0 One OneMinusSrcAlpha
			Blend 1 Off

			CGINCLUDE

			#include "UnitySprites.cginc"

			struct mrtbuffer
			{
				fixed4 col0	: COLOR0;
				float4 col1 : COLOR1;
			};

			float _AlphaClip;

			mrtbuffer SpriteFragMRT(v2f IN)
			{
				mrtbuffer o;
				fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
				clip(c.a - _AlphaClip);
				c.rgb *= c.a;
				o.col0 = c;
				float2 screenUV = IN.vertex.xy;
				o.col1 = float4(screenUV / _ScreenParams.xy, 1, 0);
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
