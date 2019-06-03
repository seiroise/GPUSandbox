using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.ImageProcessing
{
    public sealed class BilateralFilter : MonoBehaviour
    {
        const int MAX_KERNEL_SIZE = 20;

        [Range(1, MAX_KERNEL_SIZE)]
        public int kernelSize = 9;
        [Range(0.01f, 100f)]
        public float sigma = 1f;
        [Range(0.01f, 1f)]
        public float bSigma = 0.1f;
        [Range(1, 9)]
        public int iterations = 1;

        public Material mat = null;
        public Material copyMat = null;

        private PingPongTexture _work;

        private float[] _weights;
        private float _storedSigma = 0f;

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || copyMat == null) return;
            Setup();
            SetShaderParams();
            Graphics.Blit(src, _work.write);
            _work.Swap();
            for (int i = 0; i < iterations; ++i)
            {
                Graphics.Blit(_work.read, _work.write, mat, 0);
                _work.Swap();
            }
            Graphics.Blit(_work.read, dst);
        }

        private void SetShaderParams()
        {
            if (mat == null) return;
            mat.SetFloatArray("_Weights", _weights);
            mat.SetInt("_KernelSize", kernelSize);
            mat.SetFloat("_BSigma", bSigma);
        }

        private void Setup()
        {
            if (_weights == null)
            {
                _weights = new float[MAX_KERNEL_SIZE * 2 + 1];
            }
            if (_weights.Length == (MAX_KERNEL_SIZE * 2 + 1) || _storedSigma != sigma)
            {
                for (int i = 0; i <= kernelSize; ++i)
                {
                    _weights[kernelSize - i] = Weight(i, sigma);
                    _weights[kernelSize + i] = Weight(i, sigma);
                }
                _storedSigma = sigma;
            }

            if (_work == null)
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor();
                desc.width = Screen.width;
                desc.height = Screen.height;
                desc.autoGenerateMips = false;
                desc.volumeDepth = 1;
                desc.msaaSamples = 1;
                desc.dimension = TextureDimension.Tex2D;
                desc.colorFormat = RenderTextureFormat.ARGB32;
                desc.sRGB = false;
                _work = new PingPongTexture("bilateral filter work", desc);
            }
        }

        private float Weight(float e, float s)
        {
            return Mathf.Exp(-(e * e) / (2f * s)) / (Mathf.Sqrt(2f * Mathf.PI * s));
        }
    }
}