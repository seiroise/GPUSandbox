using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
    public sealed class FluidScreen2D : MonoBehaviour
    {
        public FluidSimulator2D simulator = null;
        public Renderer simRenderer = null;
        public View view = View.Texture;
        public Resolution resolution = Resolution.x1024;
		
		[Space]
		public Gradient pallete;
		[Range(0.1f, 10f)]
		public float particleLifeTime = 3f;
		public Material followerMat = null;

        private PingPongTexture _view = null;

        private int _followerRes = 2048;
        private PingPongTexture _followerMap = null;
        public PingPongTexture followerMap { get { return _followerMap; } }

        private int _palleteRes = 128;
        private Texture2D _palleteTex = null;

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
            if (followerMat)
            {
                followerMat.SetTexture("_Follower", _followerMap.read);
                followerMat.SetTexture("_Pallete", _palleteTex);
				followerMat.SetFloat("_ParticleLifeTime", particleLifeTime);
                Graphics.Blit(null, _view.read, followerMat);
            }
            simRenderer.sharedMaterial.SetTexture("_MainTex", _view.read);
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
            if (simulator)
            {
                int size = 1 << (int)resolution;
                RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc(size, size);
                _view = new PingPongTexture("fluid view", desc);
            }

            if (_followerMap == null)
            {
                RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc(_followerRes, _followerRes);
                _followerMap = new PingPongTexture("fluid follower", desc);
                simulator.BindFollowerTexture(_followerMap);
            }

            UpdatePallete();
        }

        private void ReleaseResources()
        {
            if (_view != null)
            {
                if (simulator)
                {
                    simulator.ReleaseViewTexture();
                }
                _view.Dispose();
                _view = null;
            }
            if (_followerMap != null)
            {
                _followerMap.Dispose();
                _followerMap = null;
                if (simulator)
                {
                    simulator.ReleaseFollowerTexture();
                }
            }
            if (_palleteTex != null)
            {
                Destroy(_palleteTex);
                _palleteTex = null;
            }
        }

        private void UpdatePallete()
        {
            if (_palleteTex != null)
            {
                Destroy(_palleteTex);
                _palleteTex = null;
            }
            _palleteTex = UtilFunc.CreatePallete(pallete, _palleteRes);
        }
    }
}