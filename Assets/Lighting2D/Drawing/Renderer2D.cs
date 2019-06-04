using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.Lighting2D
{
	public sealed class Renderer2D : MonoBehaviour
	{
		public Material copyMat;

		private PingPongTexture _baseColor = null;
		public PingPongTexture baseColor { get { return _baseColor; } }

		private PingPongTexture _substance = null;
		public PingPongTexture substance { get { return _substance; } }

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
			Graphics.Blit(src, dst);
		}

		private void BindResources()
		{
			ReleaseResources();

			var desc = CreateCommonDesc();
			desc.colorFormat = RenderTextureFormat.ARGB32;
			_baseColor = new PingPongTexture("base color", desc);
			_substance = new PingPongTexture("substance", desc);
		}

		private void ReleaseResources()
		{
			if (_baseColor != null)
			{
				_baseColor.Dispose();
				_baseColor = null;
			}
			if (_substance != null)
			{
				_substance.Dispose();
				_substance = null;
			}
		}

		private RenderTextureDescriptor CreateCommonDesc()
		{
			var desc = new RenderTextureDescriptor();
			desc.width = Screen.width;
			desc.height = Screen.height;
			desc.autoGenerateMips = false;
			desc.depthBufferBits = 0;
			desc.volumeDepth = 1;
			desc.msaaSamples = 1;
			desc.dimension = TextureDimension.Tex2D;
			desc.sRGB = false;
			return desc;
		}
	}
}