using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.JFA
{
	public class JFACamera : MonoBehaviour
	{

		public enum DisplayBuf
		{
			Main, JFAseed, JFAFill, DistanceField, Lighting
		}

		public DisplayBuf display = DisplayBuf.Main;
		[Header("JFA")]
		public Material jfaMat;
		[Range(1, 12)]
		public int maxStep = 10;
		[Header("Lighting")]
		public Material lightingMat;
		[Header("Copy")]
		public Material copyMat;

		private RenderTexture _mainBuf;
		private RenderTexture _jfaSeedBuf;
		private RenderTexture _lightingBuf;
		private RenderTexture _distanceFieldBuf;

		// jfa用のテンポラリバッファ
		private int _writeIdx = 0;
		private RenderTexture[] _jfaFillBufs = { null, null};

		private void OnEnable()
		{
			// RenderTargetの用意
			InitRT();
			// 生成したRenderTargetをTargetBufferに設定する。
			Camera.main.SetTargetBuffers(new RenderBuffer[2] { _mainBuf.colorBuffer, _jfaSeedBuf.colorBuffer }, _mainBuf.depthBuffer);
		}

		private void OnDisable()
		{
			ReleaseResources();
		}

		private void OnPreRender()
		{
			/*
			// メイン以外のバッファは明示的にクリアする必要がある。
			// https://answers.unity.com/questions/770366/how-to-clear-multiple-render-buffers-mrt.html
			RenderTexture lastActive = RenderTexture.active;
			RenderTexture.active = _jfaBuf;
			GL.Clear(false, true, jfaClearColor);
			RenderTexture.active = lastActive;
			*/
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (maxStep > 0)
			{
				_writeIdx = 0;
				Graphics.Blit(_jfaSeedBuf, _jfaFillBufs[_writeIdx], copyMat);
				for (int i = 0; i < maxStep; ++i)
				{
					jfaMat.SetFloat("_JumpStep", 1f / (2 << i));
					_writeIdx = (i + 1) % 2;
					Graphics.Blit(_jfaFillBufs[i % 2], _jfaFillBufs[_writeIdx], jfaMat, 0);
				}
			}

			if (display == DisplayBuf.Main)
			{
				Graphics.Blit(_mainBuf, destination, copyMat);
			}
			else if (display == DisplayBuf.JFAseed)
			{
				Graphics.Blit(_jfaSeedBuf, destination, copyMat);
			}
			else if (display == DisplayBuf.JFAFill)
			{
				Graphics.Blit(_jfaFillBufs[_writeIdx], destination, copyMat);
			}
			else if (display == DisplayBuf.DistanceField)
			{
				Graphics.Blit(_jfaFillBufs[_writeIdx], destination, jfaMat, 1);
			}
			else if (display == DisplayBuf.Lighting)
			{
				Graphics.Blit(_jfaFillBufs[_writeIdx], _distanceFieldBuf, jfaMat, 1);
				lightingMat.SetTexture("_BaseColor", _mainBuf);
				lightingMat.SetTexture("_DistanceField", _distanceFieldBuf);
				Graphics.Blit(_lightingBuf, destination, lightingMat);
			}
			else
			{
				Graphics.Blit(source, destination);
			}
		}

		private void InitRT()
		{
			ReleaseResources();

			// ColorBufferはほとんどデフォルトの設定で使用する。
			RenderTextureDescriptor descMain;
			ConstructCommonRTDescriptor(out descMain);
			descMain.colorFormat = RenderTextureFormat.ARGB32;
			descMain.depthBufferBits = 24;

			// JFA用のバッファは精度が必要になるのでfloatで確保する。
			RenderTextureDescriptor descJFA;
			ConstructCommonRTDescriptor(out descJFA);
			descJFA.colorFormat = RenderTextureFormat.ARGBFloat;
			descJFA.depthBufferBits = 0;

			_mainBuf = RenderTexture.GetTemporary(descMain);
			_mainBuf.name = "main";

			_jfaSeedBuf = RenderTexture.GetTemporary(descJFA);
			_jfaSeedBuf.name = "jfa";
			_jfaSeedBuf.filterMode = FilterMode.Point;

			_jfaFillBufs[0] = RenderTexture.GetTemporary(descJFA);
			_jfaFillBufs[0].name = "jfa temp 0";
			_jfaFillBufs[0].filterMode = FilterMode.Point;

			_jfaFillBufs[1] = RenderTexture.GetTemporary(descJFA);
			_jfaFillBufs[1].name = "jfa temp 1";
			_jfaFillBufs[1].filterMode = FilterMode.Point;

			_lightingBuf = RenderTexture.GetTemporary(descMain);
			_lightingBuf.name = "lighting";

			_distanceFieldBuf = RenderTexture.GetTemporary(descJFA);
			_distanceFieldBuf.name = "distance field";
			_distanceFieldBuf.filterMode = FilterMode.Bilinear;
		}

		private void SetRT(Camera cam)
		{
			if (cam == null) return;

			// 生成したRenderTargetをTargetBufferに設定する。
			cam.SetTargetBuffers(new RenderBuffer[2] { _mainBuf.colorBuffer, _jfaSeedBuf.colorBuffer }, _mainBuf.depthBuffer);
		}

		private void ReleaseResources()
		{
			ReleaseRT(ref _mainBuf);
			ReleaseRT(ref _jfaSeedBuf);
			ReleaseRT(ref _jfaFillBufs[0]);
			ReleaseRT(ref _jfaFillBufs[1]);
			ReleaseRT(ref _lightingBuf);
			ReleaseRT(ref _distanceFieldBuf);
		}

		private void ReleaseRT(ref RenderTexture rt)
		{
			if (rt != null)
			{
				rt.Release();
				rt = null;
			}
		}

		private void ConstructCommonRTDescriptor(out RenderTextureDescriptor rtd)
		{
			rtd = new RenderTextureDescriptor();
			rtd.width = Screen.width;
			rtd.height = Screen.height;
			rtd.autoGenerateMips = false;
			rtd.volumeDepth = 1;
			rtd.msaaSamples = 1;
			rtd.dimension = TextureDimension.Tex2D;
			rtd.sRGB = false;
		}

		/*
		private void InitCmdBuf(Camera cam)
		{
			if (_tarCam != null)
			{
				CleanCmdBuf(_tarCam);
			}

			_cmdBuf = new CommandBuffer() { name = "Jump Flooding"};
			cam.AddCommandBuffer(_camEve, _cmdBuf);
			_tarCam = cam;
		}

		private void CleanCmdBuf(Camera cam)
		{
			if (_tarCam != cam) return;
			cam.RemoveCommandBuffer(_camEve, _cmdBuf);
			_cmdBuf.Clear();
			_tarCam = null;
		}
		*/
	}
}