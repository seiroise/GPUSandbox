using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class FluidScreen2D : MonoBehaviour
	{
		public FluidSimulator2D simulator = null;
		public Renderer simRenderer = null;
		public Shader shader = null;
		public View view = View.Texture;
		public Resolution resolution = Resolution.x1024;

		private Material _renderMat = null;
		private PingPongTexture _view = null;		

		private void LateUpdate()
		{
			Draw();
		}

		private void OnEnable()
		{
			BindResources();
		}

		private void OnDisable()
		{
			ReleaseResources();
		}

		private void Draw()
		{
			if (!simulator || !_renderMat) return;
			_renderMat.SetTexture("_MainTex", _view.read);
		}

		public Vector2 WorldToScreenViewport(Vector3 worldPosition)
		{
			Vector3 localPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition);
			localPosition.z = 0f;
			Vector2 viewportPosition = localPosition;
			viewportPosition.x = localPosition.x + 0.5f;
			viewportPosition.y = localPosition.y + 0.5f;
			return viewportPosition;
		}

		private void BindResources()
		{
			if (shader != null && simRenderer != null)
			{
				_renderMat = new Material(shader);
				simRenderer.material = _renderMat;
			}

			if (simulator)
			{
				int size = 1 << (int)resolution;
				RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc(size, size);
				_view = new PingPongTexture("fluid view", desc);
				simulator.BindViewTexture(_view);
			}
		}

		private void ReleaseResources()
		{
			if (_renderMat != null && simRenderer != null)
			{
				simRenderer.material = null;
				Destroy(_renderMat);
				_renderMat = null;
			}
			if (_view != null)
			{
				if (simulator)
				{
					simulator.ReleaseViewTexture();
				}
				_view.Dispose();
				_view = null;
			}
		}
	}
}