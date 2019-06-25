using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CGS
{
	public sealed class CGS_ParticleAndEdge : MonoBehaviour
	{
		public int particleCount = 2048;
		public ComputeShader compute = null;

		[Space]

		public float deltaTime = 0.016f;		// シミュレーションの1ステップあたりに経過する時間
		public float drag = 1f;					// パーティクルの速度の減衰割合
		public float limit = 1f;                // パーティクルの持つ速度の最大値
		public float repulsion = 1f;            // パーティクル同士の反発力の係数
		public float spring = 1f;				// エッジの持つ距離を保とうとする力の係数
		public float grow = 1f;                 // パーティクルの成長速度
		public int maxLink = 2;					// 一つのパーティクルが持つエッジの最大数
		public float intervalToDivide = 0.1f;   // パーティクルの分割判定を行う間隔

		[Space]

		public Material particleMat = null;                             // インスタンシング描画用のパーティクルのマテリアル
		public Material edgeMat = null;									// インスタンシング描画用のエッジのマテリアル
		public Vector3 instancingExtents = new Vector3(10f, 10f, 10f);	// instancing描画用の範囲
		public Gradient pallete;                                        // パーティクル描画用のパレット

		private GPUPingPongObjectPool _particle = null;					// パーティクルのオブジェクトプール(ダブルバッファ
		private GPUObjectPool _edge = null;								// エッジのオブジェクトプール(シングルバッファ
		private GPUIndexPool _dividablePool = null;						// 分裂するオブジェクト(パーティクル、エッジ)のインデックスプール

		private Mesh _particleMesh = null;                              // パーティクルのインスタンシング描画に使用するメッシュ
		private Mesh _edgeMesh = null;									// エッジのインスタンシング描画に使用するメッシュ
		private uint[] _instancingArgs = { 0, 0, 0, 0, 0 };				// インスタンシング用の引数
		private ComputeBuffer _instancingArgsBuffer = null;             // インスタンシング用の引数を設定するバッファ
		private Texture2D _palleteTex = null;							// パレットをテクスチャに変換したもの

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
			
		}

		/// <summary>
		/// 各種リソースの確保
		/// </summary>
		private void BindResources()
		{
			// 各種プールの初期化
			_particle = new GPUPingPongObjectPool(particleCount, typeof(CGS_Particle2D));
			_edge = new GPUObjectPool(particleCount, typeof(CGS_Edge2D));
			_dividablePool = new GPUIndexPool(particleCount);

			// 各種オブジェクトの初期化
			InitParticles();
			InitEdges();

			// インスタンシングの用意
			_particleMesh = UtilFunc.BuildQuad();
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
			if (_particle != null) { _particle.Dispose(); _particle = null; }
			if (_edge != null) { _edge.Dispose(); _edge = null; }
			if (_dividablePool != null) { _dividablePool.Dispose(); _dividablePool = null; }
			UtilFunc.ReleaseBuffer(ref _instancingArgsBuffer);
		}

		/// <summary>
		/// パーティクルの初期化
		/// </summary>
		private void InitParticles()
		{
			var kernel = compute.FindKernel("CS_InitParticles");
			compute.SetBuffer(kernel, "_Particles", _particle.read);
			compute.SetBuffer(kernel, "_ParticlePoolAppend", _particle.countBuffer);
			UtilFunc.Dispatch1D(compute, kernel, particleCount);
		}

		/// <summary>
		/// エッジの初期化
		/// </summary>
		private void InitEdges()
		{
			var kernel = compute.FindKernel("CS_InitEdges");
			compute.SetBuffer(kernel, "_Edges", _edge.objectBuffer);
			compute.SetBuffer(kernel, "_EdgePoolAppend", _edge.poolBuffer);
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
			emitCount = Mathf.Min(emitCount, _particle.GetRemainingObjectsCount());
			if (emitCount <= 0) return;

			var kernel = compute.FindKernel("CS_EmitParticles");
			compute.SetBuffer(kernel, "_Particles", _particle.write);
			compute.SetBuffer(kernel, "_ParticlePoolConsume", _particle.poolBuffer);
			compute.SetVector("_EmitPoint", point);
			compute.SetInt("_EmitCount", emitCount);
			UtilFunc.Dispatch1D(compute, kernel, emitCount);
		}
	}
}