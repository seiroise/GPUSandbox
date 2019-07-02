using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CGS
{
    public sealed class CGS_ParticleAndEdge : MonoBehaviour
    {
        public int particleCount = 2048;
        public ComputeShader compute = null;

        [Space]

        public float deltaTime = 0.016f;        // シミュレーションの1ステップあたりに経過する時間
        public float velocityDrag = .9f;        // パーティクルの速度の減衰割合
        public float velocityLimit = 1f;        // パーティクルの持つ速度の最大値
        public float repulsion = 1f;            // パーティクル同士の反発力の係数
        public float spring = 1f;               // エッジの持つ距離を保とうとする力の係数
        public float grow = 1f;                 // パーティクルの成長速度
        public int maxLinks = 2;                // 一つのパーティクルが持つエッジの最大数
        public float dividingInterval = 0.1f;   // パーティクルの分割判定を行う間隔
		public int emitParticleCount = 2;       // マウス操作で生成されるパーティクル量
		public int maxDivideParticleCount = 2;  // 分裂処理時にパーティクルを分割できる最大数
		public int maxDivideEdgeCount = 2;		// 分裂処理時にエッジを分割できる最大数

        [Space]

        public Material particleMat = null;                             // インスタンシング描画用のパーティクルのマテリアル
        public Material edgeMat = null;                                 // インスタンシング描画用のエッジのマテリアル
        public Vector3 instancingExtents = new Vector3(10f, 10f, 10f);  // instancing描画用の範囲
        public Gradient pallete;                                        // パーティクル描画用のパレット

        private GPUPingPongObjectPool _particles = null;                // パーティクルのオブジェクトプール(ダブルバッファ
        private GPUObjectPool _edges = null;                            // エッジのオブジェクトプール(シングルバッファ
        private GPUIndexPool _dividablePool = null;                     // 分裂するオブジェクト(パーティクル、エッジ)のインデックスプール

        private Mesh _particleMesh = null;                              // パーティクルのインスタンシング描画に使用するメッシュ
        private Mesh _edgeMesh = null;                                  // エッジのインスタンシング描画に使用するメッシュ
        private uint[] _instancingArgs = { 0, 0, 0, 0, 0 };             // インスタンシング用の引数
        private ComputeBuffer _instancingArgsBuffer = null;             // インスタンシング用の引数を設定するバッファ
        private Texture2D _palleteTex = null;                           // パレットをテクスチャに変換したもの

        private CGS_Particle2D[] _dumpedParticles;      // デバッグ用にダンプされたComputeBufferの値
        private CGS_Edge2D[] _dumpedEdges;              // デバッグ用にダンプされたComputeBufferの値

        private void OnEnable()
        {
            BindResources();
            StartCoroutine(ProcessDividing());
        }

        private void OnDisable()
        {
            ReleaseResources();
            StopAllCoroutines();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                EmitParticles(GetMousePoint(), emitParticleCount);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DumpGPUBuffer();
            }

            UpdateParticles();

			RenderEdges();
			RenderParticles();
        }

        private IEnumerator ProcessDividing()
        {
            while (true)
            {
                // DivideParticles();
                DivideEdges();
                yield return new WaitForSeconds(dividingInterval);
            }
        }

        /// <summary>
        /// 各種リソースの確保
        /// </summary>
        private void BindResources()
        {
            // 各種プールの初期化
            _particles = new GPUPingPongObjectPool(particleCount, typeof(CGS_Particle2D));
            _edges = new GPUObjectPool(particleCount, typeof(CGS_Edge2D));
            _dividablePool = new GPUIndexPool(particleCount);

            // 各種オブジェクトの初期化
            InitParticles();
            InitEdges();

            // インスタンシングの用意
            _particleMesh = UtilFunc.BuildQuad();
			_edgeMesh = UtilFunc.BuildQuad();
            _instancingArgs[0] = _particleMesh.GetIndexCount(0);
            _instancingArgs[1] = (uint)particleCount;
            _instancingArgsBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
            _instancingArgsBuffer.SetData(_instancingArgs);
            _palleteTex = UtilFunc.CreatePallete(pallete, 128);
            particleMat.SetTexture("_Pallete", _palleteTex);
        }

        /// <summary>
        /// 各種リソースの開放
        /// </summary>
        private void ReleaseResources()
        {
            if (_particles != null) { _particles.Dispose(); _particles = null; }
            if (_edges != null) { _edges.Dispose(); _edges = null; }
            if (_dividablePool != null) { _dividablePool.Dispose(); _dividablePool = null; }
            UtilFunc.ReleaseBuffer(ref _instancingArgsBuffer);
        }

        /// <summary>
        /// パーティクルの初期化
        /// </summary>
        private void InitParticles()
        {
            var kernel = compute.FindKernel("CS_InitParticles");
            compute.SetBuffer(kernel, "_Particles", _particles.read);
            compute.SetBuffer(kernel, "_ParticlePoolAppend", _particles.poolBuffer);
            compute.SetInt("_ParticleCount", particleCount);
            UtilFunc.Dispatch1D(compute, kernel, particleCount);
        }

        /// <summary>
        /// エッジの初期化
        /// </summary>
        private void InitEdges()
        {
            var kernel = compute.FindKernel("CS_InitEdges");
            compute.SetBuffer(kernel, "_Edges", _edges.objectBuffer);
            compute.SetBuffer(kernel, "_EdgePoolAppend", _edges.poolBuffer);
            compute.SetInt("_EdgeCount", particleCount);
            UtilFunc.Dispatch1D(compute, kernel, particleCount);
        }

        /// <summary>
        /// マウスのワールド座標を取得
        /// </summary>
        /// <returns></returns>
        private Vector2 GetMousePoint()
        {
            Vector2 p = Input.mousePosition;
            Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, -Camera.main.transform.position.z));
            // Vector3 l = transform.InverseTransformPoint(w);
            return new Vector2(w.x, w.y);
        }

        /// <summary>
        /// パーティクルの生成
        /// </summary>
        /// <param name="point"></param>
        /// <param name="emitCount"></param>
        private void EmitParticles(Vector2 point, int emitCount = 8)
        {
            emitCount = Mathf.Min(emitCount, _particles.GetRemainingObjectsCount());
            if (emitCount <= 0) return;

            var kernel = compute.FindKernel("CS_EmitParticles");
            compute.SetBuffer(kernel, "_Particles", _particles.read);
            compute.SetBuffer(kernel, "_ParticlePoolConsume", _particles.poolBuffer);
            compute.SetVector("_EmitPoint", point);
            compute.SetInt("_EmitCount", emitCount);

            UtilFunc.Dispatch1D(compute, kernel, emitCount);
        }

        /// <summary>
        /// パーティクルを更新する
        /// </summary>
        private void UpdateParticles()
        {
            if (compute == null) return;
            int kernel = compute.FindKernel("CS_UpdateParticles");

            compute.SetBuffer(kernel, "_ParticlesRead", _particles.read);
            compute.SetBuffer(kernel, "_Particles", _particles.write);

            compute.SetFloat("_DT", deltaTime);
            compute.SetFloat("_Drag", velocityDrag);
            compute.SetFloat("_Limit", velocityLimit);
            compute.SetFloat("_Repulsion", repulsion);
            compute.SetFloat("_Grow", grow);

            UtilFunc.Dispatch1D(compute, kernel, particleCount);

            _particles.Swap();
        }

        /// <summary>
        /// パーティクルの描画
        /// </summary>
        private void RenderParticles()
        {
            if (_particleMesh == null || particleMat == null)
            {
                return;
            }

            particleMat.SetPass(0);
            particleMat.SetBuffer("buf", _particles.read);
            particleMat.SetTexture("_Pallete", _palleteTex);
            Graphics.DrawMeshInstancedIndirect(_particleMesh, 0, particleMat, new Bounds(transform.position, instancingExtents), _instancingArgsBuffer);
        }

		/// <summary>
		/// エッジの描画
		/// </summary>
		private void RenderEdges()
		{
			if (_edgeMesh == null || edgeMat == null)
			{
				return;
			}

			edgeMat.SetPass(0);
			edgeMat.SetBuffer("_Edges", _edges.objectBuffer);
			edgeMat.SetBuffer("_Particles", _particles.read);
			Graphics.DrawMeshInstancedIndirect(_edgeMesh, 0, edgeMat, new Bounds(transform.position, instancingExtents), _instancingArgsBuffer);
		}

        // Dividing Particles

        /// <summary>
        /// パーティクルの分割処理
        /// </summary>
        private void DivideParticles()
        {
            StoreDividableParticles();
            ProcessDividingParticles();
        }

        /// <summary>
        /// 分割可能なパーティクルの保持
        /// </summary>
        private void StoreDividableParticles()
        {
            _dividablePool.poolBuffer.SetCounterValue(0);

            var kernel = compute.FindKernel("CS_StoreDividableParticles");
            compute.SetBuffer(kernel, "_ParticlesRead", _particles.read);
            compute.SetBuffer(kernel, "_DividablePoolAppend", _dividablePool.poolBuffer);

            UtilFunc.Dispatch1D(compute, kernel, particleCount);
        }

        /// <summary>
        /// 分割可能なパーティクルの分割処理
        /// </summary>
        private void ProcessDividingParticles(int divideCount = 4)
        {
            divideCount = Mathf.Min(divideCount, _dividablePool.GetRemainingObjectsCount());
            if (divideCount <= 0) return;

            var kernel = compute.FindKernel("CS_DivideParticles");
            compute.SetBuffer(kernel, "_Particles", _particles.write);
            compute.SetBuffer(kernel, "_ParticlesRead", _particles.read);
            compute.SetBuffer(kernel, "_ParticlePoolConsume", _particles.poolBuffer);
            compute.SetBuffer(kernel, "_DividablePoolConsume", _dividablePool.poolBuffer);
            compute.SetInt("_DivideCount", divideCount);

            UtilFunc.Dispatch1D(compute, kernel, divideCount);

            _particles.Swap();
        }

        // Dividing Edges

        /// <summary>
        /// エッジの分割処理
        /// </summary>
        private void DivideEdges()
        {
            // 分裂可能なエッジが存在するか確認する。
            StoreDividableEdges();
            int dividableEdgeCount = _dividablePool.GetRemainingObjectsCount();
            if (dividableEdgeCount == 0)
            {
                DivideUnconnectedParticles();
            }
            else
            {
                DivideEdgesClosed(dividableEdgeCount, maxDivideEdgeCount);
            }
        }

        /// <summary>
        /// 分裂可能なエッジの保持
        /// </summary>
        private void StoreDividableEdges()
        {
            _dividablePool.poolBuffer.SetCounterValue(0);
            var kernel = compute.FindKernel("CS_StoreDividableEdges");
            compute.SetBuffer(kernel, "_Particles", _particles.read);
            compute.SetBuffer(kernel, "_Edges", _edges.objectBuffer);
            compute.SetBuffer(kernel, "_DividablePoolAppend", _dividablePool.poolBuffer);
            compute.SetInt("_MaxLinks", maxLinks);
            UtilFunc.Dispatch1D(compute, kernel, particleCount);
        }

        /// <summary>
        /// 未接続のパーティクルをエッジで接続します。
        /// </summary>
        /// <param name="divideCount"></param>
        private void DivideUnconnectedParticles()
        {
            var kernel = compute.FindKernel("CS_DivideUnconnectedParticles");
            compute.SetBuffer(kernel, "_Particles", _particles.write);
            compute.SetBuffer(kernel, "_ParticlesRead", _particles.read);
            compute.SetBuffer(kernel, "_ParticlePoolConsume", _particles.poolBuffer);
            compute.SetBuffer(kernel, "_Edges", _edges.objectBuffer);
            compute.SetBuffer(kernel, "_EdgePoolConsume", _edges.poolBuffer);
            compute.SetInt("_ParticleCount", particleCount);

            UtilFunc.Dispatch1D(compute, kernel, particleCount);
        }

        /// <summary>
        /// エッジの分割処理に共通の処理
        /// </summary>
        /// <param name="kernel"></param>
        /// <param name="dividableEdgeCount"></param>
        /// <param name="maxDivideCount"></param>
        private void DivideEdgesInternal(int kernel, int dividableEdgeCount, int maxDivideCount)
        {
            maxDivideCount = Mathf.Min(maxDivideCount, dividableEdgeCount);
            maxDivideCount = Mathf.Min(maxDivideCount, _particles.GetRemainingObjectsCount());
            maxDivideCount = Mathf.Min(maxDivideCount, _edges.GetRemainingObjectsCount());
            if (maxDivideCount <= 0) return;

            compute.SetBuffer(kernel, "_ParticlesRead", _particles.read);
            compute.SetBuffer(kernel, "_Particles", _particles.read);
            compute.SetBuffer(kernel, "_ParticlePoolConsume", _particles.poolBuffer);

            compute.SetBuffer(kernel, "_Edges", _edges.objectBuffer);
            compute.SetBuffer(kernel, "_EdgePoolConsume", _edges.poolBuffer);

            compute.SetBuffer(kernel, "_DividablePoolConsume", _dividablePool.poolBuffer);
            compute.SetInt("_DivideCount", maxDivideCount);

            UtilFunc.Dispatch1D(compute, kernel, maxDivideCount);
        }

		/// <summary>
		/// 閉じたネットワークを生成するようにパーティクルの分割を行う。
		/// </summary>
		/// <param name="dividableEdgeCount"></param>
		/// <param name="maxDivideCount"></param>
        private void DivideEdgesClosed(int dividableEdgeCount, int maxDivideCount = 16)
        {
            var kernel = compute.FindKernel("CS_DivideEdgesClosed");
            DivideEdgesInternal(kernel, dividableEdgeCount, maxDivideCount);
        }

        // Debug

        /// <summary>
        /// ComputeBufferのデータをcpu内の配列に確保する。
        /// </summary>
        private void DumpGPUBuffer()
        {
            if (_dumpedParticles == null || _dumpedParticles.Length != _particles.read.count)
            {
                _dumpedParticles = new CGS_Particle2D[_particles.read.count];
            }
            if (_dumpedEdges == null || _dumpedEdges.Length != _edges.objectBuffer.count)
            {
                _dumpedEdges = new CGS_Edge2D[_edges.objectBuffer.count];
            }

            _particles.read.GetData(_dumpedParticles);
            _edges.objectBuffer.GetData(_dumpedEdges);
        }
    }
}