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
			
		}
		
		private void OnDisable()
		{
			
		}

		private void BindResources()
		{
			// 各種プールの初期化
			_particle = new GPUPingPongObjectPool(particleCount, typeof(CGS_Particle2D));
			_edge = new GPUObjectPool(particleCount, typeof(CGS_Edge2D));
			_dividablePool = new GPUIndexPool(particleCount);

			// インスタンシングの用意
			_particleMesh = UtilFunc.BuildQuad();
			_instancingArgs[0] = _particleMesh.GetIndexCount(0);
			_instancingArgs[1] = (uint)particleCount;
			_instancingArgsBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
			_instancingArgsBuffer.SetData(_instancingArgs);
			_palleteTex = UtilFunc.CreatePallete(pallete, 128);
			particleMat.SetTexture("_Pallete", _palleteTex);


		}

		private void InitParticles()
		{
			var kernel = compute.FindKernel("CS_InitParticles");
			compute.SetBuffer(kernel, "_Particles", _particle.read);
			compute.SetBuffer(kernel, "_ParticlePoolAppend", _particle.countBuffer);
			UtilFunc.Dispatch1D(compute, kernel, particleCount);
		}

		private void InitEdges()
		{
			var kernel = compute.FindKernel("CS_InitEdges");
			compute.SetBuffer(kernel, "_Edges", _edge.objectBuffer);
			compute.SetBuffer(kernel, "_EdgePoolAppend", _edge.poolBuffer);
		}
	}
}