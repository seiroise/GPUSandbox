using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.Lighting2D
{
	public sealed class Renderer2D : MonoBehaviour
	{
		public Material copyMat;

		private PingPongTexture _canvas = null;
		public PingPongTexture canvas { get { return _canvas; } }

		private void OnEnable()
		{
			BindResources();
		}

		private void OnDisable()
		{
			ReleaseResources();
		}

		private void OnRenderImage(RenderTexture src, RenderTexture dst)
		{
			if (copyMat == null) return;
			Graphics.Blit(canvas.read, dst, copyMat, 0);
		}

		private void BindResources()
		{
			ReleaseResources();

			var desc = CreateCommonDesc();
			desc.colorFormat = RenderTextureFormat.ARGB32;
			_canvas = new PingPongTexture("canvas", desc);
		}

		private void ReleaseResources()
		{
			if (_canvas != null)
			{
				_canvas.Dispose();
				_canvas = null;
			}
		}

		private RenderTextureDescriptor CreateCommonDesc()
		{
			var desc = new RenderTextureDescriptor();
			desc.width = Screen.width;
			desc.height = Screen.height;
			desc.autoGenerateMips = false;
			desc.volumeDepth = 1;
			desc.msaaSamples = 1;
			desc.dimension = TextureDimension.Tex2D;
			desc.sRGB = false;
			return desc;
		}
	}
}