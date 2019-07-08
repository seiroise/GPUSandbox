using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
    public sealed class FluidSolver2D : MonoBehaviour
    {
        /// <summary>
        /// シェーダのパス
        /// </summary>
        private enum SolverPass
        {
            Clear = 0,
			ClearBoundaries,
            Copy,
            CalcDivergence,
            CalcAndApplyVorticity,
            CalcPressure,
            ApplyPressure,
            AdvectColor,
            AdvectVelocity,
            Mouse_Circle,
            Mouse_LineSeg,
            Draw_Circle,
			ApplyObstableMap,
            VeloicityColor,
            VorticityColor,
            PressureColor,
        }

        /// <summary>
        /// 表示結果
        /// </summary>
        public enum View
        {
            All,
            Velocity,
            VelocityColor,
            Vorticity,
            Pressure,
            Texture
        }

        /// <summary>
        /// マウスの相互作用の種類
        /// </summary>
        public enum MouseInteraction
        {
            Circle,
            LineSeg,
            Source,
        }

        public bool isSimulated = true;

        public View view = View.All;
        public float p0, p1, p2, p3;

        [Space]

        [Range(1e-3f, 2e-1f)]
        public float deltaTime = 1.6e-2f;
        [Range(1, 100)]
        public int iterations = 4;
        [Range(0, .5f)]
        public float vorticityCoef = .11f;
        [Range(0, 10f)]
        public float viscosityCoef = .25f;
        [Range(0.9f, 1f)]
        public float velocityAdvectionDecay = .998f;

        [Space]

        public MouseInteraction mouse = MouseInteraction.Circle;
        [Range(0.001f, .2f)]
        public float mouseRadius = 0.1f;
        public float mouseForce = 1f;
        public Vector2 mouseForceDir = Vector2.one;
        public bool autoMouseColor = true;
        public Gradient mouseColorPallet;

		[Space]
		public Texture2D obstacleMap = null;

		[Space]

		public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public Material copy;
        public Texture2D sourceTexture;
        [Range(0.99f, 1f)]
        public float colorAdvectionDecay = .998f;

        private bool _mouseDragging;
        private Vector2 _prevMouseSt;

        private PingPongTexture _params;		// シミュレーション用のパラメータ描画テクスチャ
        private PingPongTexture _view;			// 表示用のテクスチャ
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

        private void OnEnable()
        {
            BindResources();
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isSimulated = !isSimulated;
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            // マウスインタラクション
            Vector2 st = Input.mousePosition / new Vector2(Screen.width, Screen.height);
            if (Input.GetMouseButton(0))
            {
                if (!_mouseDragging) _prevMouseSt = st;
                ApplyInteraction(ref st, ref _prevMouseSt);
            }
            _mouseDragging = Input.GetMouseButton(0);

            // マウスインタラクションの描画
            DrawInteraction(st);
            _prevMouseSt = st;

            // シミュレーションを進める
            if (isSimulated)
            {
                Step();
            }

            // 最終的なレンダリング
            Draw(src, dst);
        }

        private void BindResources()
        {
            RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc();
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

            // 表示用テクスチャへの描画
            _view = new PingPongTexture("stablue fluids view", desc, wrapMode);
            _solver.SetTexture(_sourceTexId, sourceTexture);
            Graphics.Blit(null, _view.write, _solver, (int)SolverPass.Copy);
            _view.Swap();
        }

        private void ReleaseResources()
        {
            if (_params != null)
            {
                _params.Dispose();
                _params = null;
            }
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
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

			// 障害物マップを描画
			if (obstacleMap != null)
			{
				_solver.SetTexture(_paramsId, _params.read);
				_solver.SetTexture(_obstacleMapId, obstacleMap);
				Graphics.Blit(null, _params.write, _solver, (int)SolverPass.ApplyObstableMap);
				_params.Swap();
			}

			_solver.SetVector(_simConstantsId, new Vector4(vorticityCoef, viscosityCoef, velocityAdvectionDecay, colorAdvectionDecay));
            _solver.SetFloat(_dtId, deltaTime);
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcAndApplyVorticity);
            _params.Swap();

            for (int i = 0; i < iterations; ++i)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcPressure);
                _params.Swap();
            }

            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.ApplyPressure);
            _params.Swap();

            // 色の移流
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(_view.read, _view.write, _solver, (int)SolverPass.AdvectColor);
            _view.Swap();

            // 速度の移流
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.AdvectVelocity);
            _params.Swap();
        }

        private void ApplyInteraction(ref Vector2 st, ref Vector2 prevSt)
        {
            Vector2 d = st - prevSt;
            int pass = -1;
            if (mouse == MouseInteraction.Circle)
            {
				// 円形に力を加える。
                _solver.SetVector(_mouseId, new Vector4(st.x, st.y, mouseRadius, mouseForce * d.magnitude));
                _solver.SetVector(_forceDirId, d.normalized);
                pass = (int)SolverPass.Mouse_Circle;
            }
            else if (mouse == MouseInteraction.LineSeg)
            {
                _solver.SetVector(_lineSegId, new Vector4(prevSt.x, prevSt.y, st.x, st.y));
                _solver.SetFloat(_lineWidthId, mouseRadius);
                _solver.SetVector(_forceDirId, d.normalized);
                pass = (int)SolverPass.Mouse_LineSeg;
            }
            else
            {
                _solver.SetVector(_mouseId, new Vector4(st.x, st.y, mouseRadius, mouseForce));
                _solver.SetVector(_forceDirId, mouseForceDir);
                pass = (int)SolverPass.Mouse_Circle;
            }

            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, pass);
            _params.Swap();
        }

        private void DrawInteraction(Vector2 st)
        {
            if (Input.GetMouseButton(1) || (autoMouseColor && Input.GetMouseButton(0)))
            {
                _solver.SetTexture(_paramsId, _params.read);
                _solver.SetVector(_mouseId, new Vector4(st.x, st.y, mouseRadius, 0f));
                _solver.SetColor(_mouseColorId, mouseColorPallet.Evaluate(Mathf.Sin(Time.time) * .5f + .5f));
                Graphics.Blit(_view.read, _view.write, _solver, (int)SolverPass.Draw_Circle);
                _view.Swap();
            }
        }

        private void Draw(RenderTexture src, RenderTexture dst)
        {
            Vector4 renderingParams = new Vector4(p0, p1, p2, p3);
            _solver.SetVector(_renderingParamsId, renderingParams);
            if (view == View.VelocityColor)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, dst, _solver, (int)SolverPass.VeloicityColor);
            }
            else if (view == View.Vorticity)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, dst, _solver, (int)SolverPass.VorticityColor);
            }
            else if (view == View.Pressure)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, dst, _solver, (int)SolverPass.PressureColor);
            }
            else if (view == View.Texture)
            {
                Graphics.Blit(_view.read, dst);
            }
            else
            {
                Graphics.Blit(_params.read, dst);
            }
        }
    }
}