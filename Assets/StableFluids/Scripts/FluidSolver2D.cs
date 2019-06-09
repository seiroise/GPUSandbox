﻿using UnityEngine;

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
            Copy,
            CalcDivergence,
            CalcPressure,
            ApplyPressure,
            AdvectColor,
            AdvectVelocity,
            Mouse_Circle,
            Mouse_LineSeg,
            VeloicityColor,
        }

        /// <summary>
        /// 表示結果
        /// </summary>
        public enum View
        {
            All,
            Velocity,
            Divergence,
            Pressure,
            VelocityColor,
            Texture
        }

        /// <summary>
        /// マウスの相互作用の種類
        /// </summary>
        public enum MouseInteraction
        {
            Circle,
            LineSeg
        }

        public View view = View.All;

        [Range(1, 10)]
        public int iterations = 4;
        public MouseInteraction mouse = MouseInteraction.Circle;
        [Range(0.001f, .2f)]
        public float mouseRadius = 0.1f;
        [Range(0.01f, 1000f)]
        public float mouseForce = 1f;

        public Material copy;
        public Texture2D sourceTexture;

        private bool _mouseDragging;
        private Vector2 _prevMouseSt;

        private PingPongTexture _params;		// シミュレーション用のパラメータ描画テクスチャ
        private PingPongTexture _view;			// 表示用のテクスチャ
        private Material _solver;
        private int _sourceTexId;
        private int _paramsId;
        private int _mouseId;
        private int _forceDirId;
        private int _lineSegId;
        private int _lineWidthId;

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
            // マウスインタラクション
            if (Input.GetMouseButton(0))
            {
                Vector2 st = Input.mousePosition / new Vector2(Screen.width, Screen.height);
                if (!_mouseDragging) _prevMouseSt = st;
                ApplyExternalForce(ref st, ref _prevMouseSt);
                _prevMouseSt = st;
            }
            _mouseDragging = Input.GetMouseButton(0);

            Step();

            // _solver.SetTexture(_paramsId, _work.read);
            // Graphics.Blit(src, dst, _solver, (int)SolverPass.AdvectColor);
            // Graphics.Blit(null, dst, _solver, (int)SolverPass.Mouse);

            // 速度を移流させる
            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.AdvectVelocity);
            _params.Swap();

            // 最終的なレンダリング
            Draw(src, dst);
        }

        private void BindResources()
        {
            RenderTextureDescriptor desc = UtilFunc.CreateCommonDesc();
            desc.colorFormat = RenderTextureFormat.ARGBFloat;
            _params = new PingPongTexture("stable fluids work", desc, TextureWrapMode.Repeat);

            // シェーダプロパティインデックスの取得
            _solver = new Material(Shader.Find("Hidden/FluidSolver2D"));
            _sourceTexId = Shader.PropertyToID("_SourceTex");
            _paramsId = Shader.PropertyToID("_Params");
            _mouseId = Shader.PropertyToID("_Mouse");
            _forceDirId = Shader.PropertyToID("_ForceDir");
            _lineSegId = Shader.PropertyToID("_LineSeg");
            _lineWidthId = Shader.PropertyToID("_LineWidth");

            // 表示用テクスチャへの描画
            _view = new PingPongTexture("stablue fluids view", desc, TextureWrapMode.Repeat);
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

            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, (int)SolverPass.CalcDivergence);
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
        }

        private void ApplyExternalForce(ref Vector2 st, ref Vector2 prevSt)
        {
            Vector2 d = st - prevSt;
            int pass = -1;
            if (mouse == MouseInteraction.Circle)
            {
                _solver.SetVector(_mouseId, new Vector4(st.x, st.y, mouseRadius, mouseForce * d.magnitude));
                _solver.SetVector(_forceDirId, d.normalized);
                pass = (int)SolverPass.Mouse_Circle;
            }
            else
            {
                _solver.SetVector(_lineSegId, new Vector4(prevSt.x, prevSt.y, st.x, st.y));
                _solver.SetFloat(_lineWidthId, mouseRadius);
                _solver.SetVector(_forceDirId, d.normalized);
                pass = (int)SolverPass.Mouse_LineSeg;
            }

            _solver.SetTexture(_paramsId, _params.read);
            Graphics.Blit(null, _params.write, _solver, pass);
            _params.Swap();
        }

        private void Draw(RenderTexture src, RenderTexture dst)
        {
            Vector4 viewMask = new Vector4(1f, 1f, 1f, 1f);
            if (view == View.VelocityColor)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(null, dst, _solver, (int)SolverPass.VeloicityColor);
            }
            else if (view == View.Texture)
            {
                _solver.SetTexture(_paramsId, _params.read);
                Graphics.Blit(_view.read, _view.write, _solver, (int)SolverPass.AdvectColor);
                _view.Swap();
                Graphics.Blit(_view.read, dst);
            }
            else
            {
                Graphics.Blit(_params.read, dst);
            }
        }
    }
}