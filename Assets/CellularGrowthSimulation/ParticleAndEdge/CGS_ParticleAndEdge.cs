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
		public float limit = 1f;				// パーティクルの持つ速度の最大値
		public float grow = 1f;					// パーティクルの成長速度
		public float intervalToDivide = 0.1f;   // パーティクルの分割判定を行う間隔

		[Space]

		public Material instancingMat = null;							// instancing描画用の
		public Vector3 instancingExtents = new Vector3(10f, 10f, 10f);	// instancing描画用の範囲
		public Gradient pallete;                                        // パーティクル描画用のパレット

		private GPUPingPongObjectPool _particlePool = null;
		private GPUObjectPool _edgePool = null;
		private GPUObjectPoolCountOnly _dividablePool = null;

		private Mesh _instancingMesh = null;
		private uint[] _instancingArgs = { 0, 0, 0, 0, 0 };
		private ComputeBuffer _instancingArgsBuffer = null;

		private void OnEnable()
		{
			
		}
		
		private void OnDisable()
		{
			
		}

		private void BindResources()
		{
			_particlePool = new GPUPingPongObjectPool(particleCount, typeof(CGS_Particle2D));
			_edgePool = new GPUObjectPool(particleCount, typeof(CGS_Edge2D));
			_dividablePool = new GPUObjectPoolCountOnly(particleCount);

			_instancingMesh = UtilFunc.BuildQuad();
			_instancingArgs[0] = _instancingMesh.GetIndexCount(0);
			_instancingArgs[1] = (uint)particleCount;
			_instancingArgsBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
			_instancingArgsBuffer.SetData(_instancingArgs);


		}
	}
}