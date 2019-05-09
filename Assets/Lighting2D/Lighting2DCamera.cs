﻿using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.Lighting2D
{

    public class Lighting2DCamera : MonoBehaviour
    {
        public enum Display
        {
            BaseColor, Substance, JfaSeed, JfaFill, DistanceField, Lighting
        }
		public enum DisplayLighting
		{
			Result, Emission
		}

        public enum JFAResolution
        {
            Full = 0,
            Half = 1,
            Div_4 = 2,
            Div_8 = 3,
            Div_16 = 4
        }
        public Display display = Display.BaseColor;
		public DisplayLighting displayLighting = DisplayLighting.Emission;

        public JFAResolution jfaResolution = JFAResolution.Full;
        [Range(1, 12)]
        public int maxStep = 10;

        public Material jfaMat;
        public Material lightingMat;
        public Material copyMat;

		public Vector4 jfaCopyScale = Vector4.one;
		public float dfCopyScale = 1f;
		public float emisScale = 1f;

        private RenderTexture _baseColor;
        private RenderTexture _substance;
        private RenderTexture _jfaSeed;

        private int _writeIdx = 0;
        private RenderTexture[] _jfaWorks;			// jfaの計算は書き込みと読込みに分けて行う必要があるので二つのバッファを確保する必要がある。
        private RenderTexture _distanceField;
		private RenderTexture _prevLighting;		// lithingの計算には前回のフレームでの計算結果を利用するので、バッファが複数必要。
        private RenderTexture _lighting;

        private void OnEnable()
        {
            BindResources();
            Camera.main.SetTargetBuffers(new RenderBuffer[3] { _baseColor.colorBuffer, _substance.colorBuffer, _jfaSeed.colorBuffer }, _baseColor.depthBuffer);
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (maxStep > 0)
            {
                _writeIdx = 0;
				Graphics.Blit(_jfaSeed, _jfaWorks[_writeIdx], jfaMat, 2);
				for (int i = 0; i < maxStep; ++i)
                {
					jfaMat.SetFloat("_JumpStep", Mathf.Pow(2, maxStep - (i + 1)));
					_writeIdx = (i + 1) % 2;
                    Graphics.Blit(_jfaWorks[i % 2], _jfaWorks[_writeIdx], jfaMat, 0);
                }
            }

			copyMat.SetVector("_Scale", Vector4.one);
            if (display == Display.BaseColor)
            {
                Graphics.Blit(_baseColor, destination, copyMat);
            }
            else if (display == Display.Substance)
            {
                Graphics.Blit(_substance, destination, copyMat);
            }
            else if (display == Display.JfaSeed)
            {
				// Graphics.Blit(_jfaSeed, _jfaSeed, jfaMat, 2);
				copyMat.SetVector("_Scale", jfaCopyScale);
                Graphics.Blit(_jfaSeed, destination, jfaMat, 2);
            }
            else if (display == Display.JfaFill)
            {
				copyMat.SetVector("_Scale", jfaCopyScale);
				Graphics.Blit(_jfaWorks[_writeIdx], destination, copyMat);
            }
            else if (display == Display.DistanceField)
            {
				jfaMat.SetFloat("_DistScale", dfCopyScale);
                Graphics.Blit(_jfaWorks[_writeIdx], _distanceField, jfaMat, 1);
				Graphics.Blit(_distanceField, destination, copyMat);
            }
            else if (display == Display.Lighting)
            {
				// jfの結果からsdfを生成 
				jfaMat.SetFloat("_DistScale", 1f);
				Graphics.Blit(_jfaWorks[_writeIdx], _distanceField, jfaMat, 1);
				
				// リニア空間でライティングを行う 
				lightingMat.SetTexture("_BaseColor", _baseColor);
                lightingMat.SetTexture("_Substance", _substance);
                lightingMat.SetTexture("_DistanceField", _distanceField);
				lightingMat.SetTexture("_PrevLighting", _prevLighting);
                Graphics.Blit(null, _lighting, lightingMat, 0);

				if (displayLighting == DisplayLighting.Result)
				{
					// sRGB空間に変換
					Graphics.Blit(_lighting, destination, lightingMat, 1);
				}
				else
				{
					// 輝度値に変換
					lightingMat.SetFloat("_EmisScale", emisScale);
					Graphics.Blit(_lighting, destination, lightingMat, 2);
				}

				// ライティング用の作業テクスチャをスワップ。
				UtilFunc.Swap(ref _lighting, ref _prevLighting);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        private void BindResources()
        {
            ReleaseResources();

            RenderTextureDescriptor descBaseColor;
            ConstructRTD(out descBaseColor);
            descBaseColor.colorFormat = RenderTextureFormat.ARGB32;
            descBaseColor.depthBufferBits = 24;

            RenderTextureDescriptor descJfa;
            ConstructRTD(out descJfa);
            descJfa.colorFormat = RenderTextureFormat.ARGB32;
            descJfa.depthBufferBits = 0;

            RenderTextureDescriptor descJfaWork;
            ConstructRTD(out descJfaWork);
            // descJfaWork.colorFormat = RenderTextureFormat.ARGB32;
            descJfaWork.colorFormat = RenderTextureFormat.ARGBFloat;
            descJfaWork.depthBufferBits = 0;
            descJfaWork.width /= (1 << (int)jfaResolution);
            descJfaWork.height /= (1 << (int)jfaResolution);

            RenderTextureDescriptor descDistField;
            ConstructRTD(out descDistField);
            descDistField.colorFormat = RenderTextureFormat.ARGBFloat;
            // descDistField.colorFormat = RenderTextureFormat.ARGB32;
            descDistField.depthBufferBits = 0;
            descDistField.width = descJfaWork.width;
            descDistField.height = descJfaWork.height;

            _baseColor = RenderTexture.GetTemporary(descBaseColor);
            _baseColor.name = "base color";

            _substance = RenderTexture.GetTemporary(descBaseColor);
            _substance.name = "substance";

            _jfaSeed = RenderTexture.GetTemporary(descJfa);
            _jfaSeed.name = "jfa seed";
            // _jfaSeed.filterMode = FilterMode.Point;

            _jfaWorks = new RenderTexture[2];
            _jfaWorks[0] = RenderTexture.GetTemporary(descJfaWork);
            _jfaWorks[0].name = "jfa work 0";
            // _jfaWorks[0].filterMode = FilterMode.Point;
            // _jfaWorks[0].wrapMode = TextureWrapMode.Clamp;

            _jfaWorks[1] = RenderTexture.GetTemporary(descJfaWork);
            _jfaWorks[1].name = "jfa work 1";
            // _jfaWorks[1].filterMode = FilterMode.Point;
            // _jfaWorks[1].wrapMode = TextureWrapMode.Clamp;

            _distanceField = RenderTexture.GetTemporary(descDistField);
            _distanceField.name = "distance field";

            _lighting = RenderTexture.GetTemporary(descBaseColor);
            _lighting.name = "lighting";

			_prevLighting = RenderTexture.GetTemporary(descBaseColor);
			_prevLighting.name = "prev lighting";
        }

        private void ReleaseResources()
        {
            UtilFunc.ReleaseRT(ref _baseColor);
            UtilFunc.ReleaseRT(ref _substance);
            UtilFunc.ReleaseRT(ref _jfaSeed);
            if (_jfaWorks != null && _jfaWorks.Length == 2)
            {
                UtilFunc.ReleaseRT(ref _jfaWorks[0]);
                UtilFunc.ReleaseRT(ref _jfaWorks[1]);
            }
            UtilFunc.ReleaseRT(ref _distanceField);
            UtilFunc.ReleaseRT(ref _lighting);
			UtilFunc.ReleaseRT(ref _prevLighting);
        }

        private void ConstructRTD(out RenderTextureDescriptor rtd)
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
    }
}