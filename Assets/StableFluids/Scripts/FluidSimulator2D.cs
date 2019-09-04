using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{

    /// <summary>
    /// FluidSolver2Dのシミュレーション部分だけ。
    /// シミュレーション結果をレンダーテクスチャーに保存するが描画はしない。
    /// </summary>
    public sealed class FluidSimulator2D : MonoBehaviour
    {

        /// <summary>
        /// trueの場合はシミュレーションを行う。
        /// </summary>
        public bool simulate = true;

        /// <summary>
        /// ソルバーの解像度
        /// </summary>
        public Resolution solverResolution = Resolution.x256;

        /// <summary>
        /// テクスチャのラップ設定
        /// </summary>
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;

        [Space]

        /// <summary>
        /// シミュレーションの時刻刻み幅
        /// </summary>
        [Range(1e-3f, 2e-1f)]
        public float deltaTime = 1.6e-2f;

        /// <summary>
        /// 1ステップ内の圧力計算の反復回数
        /// </summary>
        [Range(1, 100)]
        public int pressureIterations = 4;

        /// <summary>
        /// 渦度係数。大体0.2ぐらいがちょうどいい。
        /// </summary>
        [Range(0, .5f)]
        public float vorticityCoef = .11f;

        /// <summary>
        /// 動粘性係数
        /// </summary>
        [Range(0, 10f)]
        public float viscosityCoef = .25f;

        /// <summary>
        /// 速度場伝搬時の減衰率
        /// </summary>
        [Range(0.9f, 1f)]
        public float velocityAdvectionDecay = .998f;

        /// <summary>
        /// 色の伝搬時の減衰率
        /// </summary>
        [Range(0.9f, 1f)]
        public float colorAdvectionDecay = .998f;

        [Range(0.1f, 1f)]
        public float followerDissipation = .998f;

        /// <summary>
        /// シミュレーション結果の格納先
        /// </summary>
        private PingPongTexture _params;

        private Material _solver;
        private int _sourceTexId;
        private int _dtId;
        private int _paramsId;
        private int _mouseId;
        private int _mouseColorId;
        private int _forceDirId;
        private int _lineSegId;
        private int _lineWidthId;
        private int _simConstantsId;
        private int _renderingParamsId;
        private int _obstacleMapId;

        private int _followerTexId;
        private int _followerDissipationId;
        private int _followerAreaId;
        private int _followerParamsId;

        private PingPongTexture _view = null;
        private PingPongTexture _follower = null;

        private void Update()
        {
            if (simulate)
            {
                Step();
            }
        }

        private void OnEnable()
        {
            BindResources();
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void BindResources()
        {
            int resolution = 1 << (int)solverResolution;

            RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc(resolution, resolution);

            desc.colorFormat = RenderTextureFormat.ARGBFloat;
            _params = new PingPongTexture("stable fluids work", desc, wrapMode);

            // シェーダプロパティインデックスの取得
            _solver = new Material(Shader.Find("Hidden/FluidSolver2D"));
            _sourceTexId = Shader.PropertyToID("_SourceTex");
            _dtId = Shader.PropertyToID("_DT");
            _paramsId = Shader.PropertyToID("_Params");
            _mouseId = Shader.PropertyToID("_Mouse");
            _mouseColorId = Shader.PropertyToID("_MouseColor");
            _forceDirId = Shader.PropertyToID("_ForceDir");
            _lineSegId = Shader.PropertyToID("_LineSeg");
            _lineWidthId = Shader.PropertyToID("_LineWidth");
            _simConstantsId = Shader.PropertyToID("_SimConstants");
            _renderingParamsId = Shader.PropertyToID("_RenderingParams");
            _obstacleMapId = Shader.PropertyToID("_ObstacleMap");
            _followerTexId = Shader.PropertyToID("_FollowerTex");
            _followerDissipationId = Shader.PropertyToID("_FollowerDissipation");
            _followerAreaId = Shader.PropertyToID("_FollowerArea");
            _followerParamsId = Shader.PropertyToID("_FollowerParams");
        }

        private void ReleaseResources()
        {
            if (_params != null)
            {
                _params.Dispose();
                _params = null;
            }
            if (_solver != null)
            {
                Destroy(_solver);
                _solver = null;
            }
        }

        private void Step()
        {
            if (_solver == null || _params == null) return;

            // 境界付近の物理量を初期化する
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.ClearBoundaries);
            _params.Swap();

            // シミュレーション用の各種変数を設定
            float centreViscCoef = 1f / viscosityCoef;
            float stencilViscCoef = 1f / (4f + centreViscCoef);
            _solver.SetVector(_simConstantsId, new Vector4(vorticityCoef, viscosityCoef, velocityAdvectionDecay, colorAdvectionDecay));
            _solver.SetFloat(_dtId, deltaTime);

            // 渦度の計算と速度場への適用
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcAndApplyVorticity);
            _params.Swap();

            // 速度場の粘性を計算し適用
            if (viscosityCoef > 0f)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcAndApplyViscosity);
                _params.Swap();
            }

            // 速度場の発散を0にするための圧力を計算
            for (int i = 0; i < pressureIterations; ++i)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcPressure);
                _params.Swap();
            }

            // 計算した圧力を適用する
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.ApplyPressure);
            _params.Swap();

            // 色の移流
            if (_view != null)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(_view.read, _view.write, _solver, (int)SolverPass.AdvectColor);
                _view.Swap();
            }

			// followerの移流
			AdvectFollower(_follower, new Vector4(followerDissipation, 0, 0, 0));

            // 速度の移流
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.AdvectVelocity);
            _params.Swap();
        }

        public void Interact(Vector2 pos, float radius, Vector2 force, Color color)
        {
            // 円形に力を加える。
            _solver.SetVector(_mouseId, new Vector4(pos.x, pos.y, radius, force.magnitude));
            _solver.SetVector(_forceDirId, force.normalized);
            int pass = (int)SolverPass.Mouse_Circle;
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, pass);
            _params.Swap();

            if (_view != null)
            {
                _solver.SetTexture(_paramsId, _params.read);
                _solver.SetVector(_mouseId, new Vector4(pos.x, pos.y, radius, 0f));
                _solver.SetColor(_mouseColorId, color);
                Graphics.Blit(_view.read, _view.write, _solver, (int)SolverPass.Draw_Circle);
                _view.Swap();
            }
        }

        public void AdvectFollower(PingPongTexture tex, Vector4 dissipation)
        {
            if (tex == null)
            {
                return;
            }
            _solver.SetTexture(_paramsId, _params.read);
            _solver.SetTexture(_followerTexId, tex.read);
            _solver.SetVector(_followerDissipationId, dissipation);
            Graphics.Blit(null, tex.write, _solver, (int)SolverPass.AdvectFollower);
            tex.Swap();
        }

        public void WriteFollower(PingPongTexture tex, Vector4 follower, Vector2 pos, float radius)
        {
            if (tex == null)
            {
                return;
            }

            _solver.SetTexture(_paramsId, _params.read);
            _solver.SetTexture(_followerTexId, tex.read);
            _solver.SetVector(_followerAreaId, new Vector4(pos.x, pos.y, radius));
            _solver.SetVector(_followerParamsId, follower);
            Graphics.Blit(null, tex.write, _solver, (int)SolverPass.WriteFollower);
            tex.Swap();
        }

        public RenderTexture GetSimulationTexture()
        {
            return _params.read;
        }

        public void BindViewTexture(PingPongTexture view)
        {
            _view = view;
        }

        public void ReleaseViewTexture()
        {
            _view = null;
        }

        public void BindFollowerTexture(PingPongTexture follower)
        {
            _follower = follower;
        }

        public void ReleaseFollowerTexture()
        {
            _follower = null;
        }
    }
}