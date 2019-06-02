using UnityEngine;
using UnityEngine.Rendering;

namespace Seiro.GPUSandbox.ImageProcessing
{
    public sealed class GauusianFilter : MonoBehaviour
    {
        const int MAX_KERNEL_SIZE = 20;

        [Range(1, MAX_KERNEL_SIZE)]
        public int kernelSize = 9;
        [Range(0.01f, 100f)]
        public float sigma = 1f;
        [Range(1, 9)]
        public int iterations = 1;

        public Material mat = null;
        public Material copyMat = null;

        private float[] _weights;
        private float _invFactor = 0f;
        private float _storedSigma = 0f;

        private PingPongTexture _work;

        private void Start()
        {
            Setup();
        }

        private void Update()
        {
            Setup();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || copyMat == null) return;
            SetShaderParams();
            Graphics.Blit(src, _work.write, copyMat, 0);
            _work.Swap();
            for (int i = 0; i < iterations; ++i)
            {
                Graphics.Blit(_work.read, _work.write, mat, 0);
                _work.Swap();
                Graphics.Blit(_work.read, _work.write, mat, 1);
                _work.Swap();
            }
            Graphics.Blit(_work.read, dst);
        }

        private void Setup()
        {
            if (_weights == null)
            {
                _weights = new float[MAX_KERNEL_SIZE + 1];
            }
            if (_weights.Length != kernelSize + 1 || _storedSigma != sigma)
            {
                _invFactor = _weights[0] = Weight(0f, sigma);

                for (int i = 1; i < kernelSize; ++i)
                {
                    _invFactor += (_weights[i] = Weight(i, sigma)) * 2f;
                }
                _invFactor = 1f / _invFactor;
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
                _work = new PingPongTexture("gauusian filter work", desc);
            }
        }

        private void SetShaderParams()
        {
            if (mat == null) return;
            mat.SetFloatArray("_Weights", _weights);
            mat.SetFloat("_InvFactor", _invFactor);
            mat.SetInt("_KernelSize", kernelSize);
        }

        private float Weight(float e, float s)
        {
            return Mathf.Exp(-(e * e) / (2f * s)) / (Mathf.Sqrt(2f * Mathf.PI * s));
        }
    }
}