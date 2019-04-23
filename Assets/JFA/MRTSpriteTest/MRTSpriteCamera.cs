using UnityEngine;

namespace Seiro.GPUSandbox.JFA
{

	public class MRTSpriteCamera : MonoBehaviour
	{

		public enum DisplayBuf
		{
			Diffuse, Substance
		}

		public DisplayBuf displayBuf = DisplayBuf.Diffuse;
		public RenderTexture diffuse;
		public RenderTexture substance;
		public Material copyMat;

		private Color _jfBackground = new Color(-1f, -1f, 0f, 0f);
		private bool _mrtSupported = false;

		private void Awake()
		{
			_mrtSupported = CheckMRTSupport();
			if (_mrtSupported)
			{
				Camera.main.SetTargetBuffers(new RenderBuffer[2] { diffuse.colorBuffer, substance.colorBuffer }, diffuse.depthBuffer);
			}
		}

		private void OnPreRender()
		{
			// substanceは明示的にクリアしないとクリアされないので、きちんとクリアしてあげる。
			// https://answers.unity.com/questions/770366/how-to-clear-multiple-render-buffers-mrt.html
			if (_mrtSupported)
			{
				RenderTexture lastActive = RenderTexture.active;
				RenderTexture.active = substance;
				GL.Clear(false, true, _jfBackground);
				RenderTexture.active = lastActive;
			}
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (displayBuf == DisplayBuf.Diffuse)
			{
				Graphics.Blit(diffuse, destination, copyMat, 0);
			}
			else
			{
				Graphics.Blit(substance, destination, copyMat, 0);
			}
		}

		private bool CheckMRTSupport()
		{
			if (SystemInfo.supportedRenderTargetCount >= 2)
			{
				return true;
			}
			else
			{
				Debug.LogWarning("This device don't support mrt");
				return false;
			}
		}
	}
}