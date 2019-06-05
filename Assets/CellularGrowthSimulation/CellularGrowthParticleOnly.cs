using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CellularGrowthSimulation
{
	
	[StructLayout(LayoutKind.Sequential)]
	public struct CellularParticle
	{
		public Vector2 position;	// 粒子の座標
		public Vector2 velocity;	// 粒子の保持している速度
		public float rdius;			// 粒子の半径
		public float threshold;		// 粒子の最大半径
		public int links;			// 接続しているエッジの数
		public uint alive;			// 活性化フラグ
	}

	public class CellularGrowthParticleOnly : MonoBehaviour
	{

		public int particleCount = 2000;
		public ComputeShader compute = null;

		private PingPongBuffer _particleBuffer;
		private ComputeBuffer _poolBuffer, _dividablePoolBuffer;

		private int[] _countArgs = { 0, 1, 0, 0 };
		private ComputeBuffer _countArgsBuffer;
		private uint[] _drawArgs = { 0, 0, 0, 0, 0 };
		private ComputeBuffer _drawArgsBuffer;

		private Mesh _drawMesh = null;

		private void Start()
		{
			// _poolBufferと_dividablePoolBufferがそれぞれパーティクルのオブジェクプールとして機能している。
			// それぞれのバッファには非活性なパーティクルのインデックスが格納されているので、
			// パーティクルが必要になった場合は、それぞれのプールバッファから一つポップして、その番号のパーティクルをパーティクルバッファから取り出して、アクティブにする。
			// またアクティブではなくなったパーティクルが存在する場合は、その番号を取り出して、プールバッファにpushする。
			// これでオブジェクトプールとして機能させる事ができる。

			// パーティクル本体のデータを保持するためのダブルバッファ
			_particleBuffer = new PingPongBuffer(particleCount, typeof(CellularParticle));

			// オブジェクトプールの初期化
			_poolBuffer = new ComputeBuffer(
				particleCount,
				Marshal.SizeOf(typeof(int)),
				ComputeBufferType.Append		// append bufferとして作成
			);
			_poolBuffer.SetCounterValue(0);

			_countArgsBuffer = new ComputeBuffer(
				4,
				Marshal.SizeOf(typeof(int)),
				ComputeBufferType.IndirectArguments
			);
			_countArgsBuffer.SetData(_countArgs);

			// 分裂可能なオブジェクトを管理するためのオブジェクトプール
			_dividablePoolBuffer = new ComputeBuffer(
				particleCount,
				Marshal.SizeOf(typeof(int)),
				ComputeBufferType.Append
			);
			_dividablePoolBuffer.SetCounterValue(0);

			// オブジェクトプールの初期化
			InitParticlesKernel();

			// インスタンシングを行うためのメッシュをセットアップ。
			_drawMesh = UtilFunc.BuildQuad();
			_drawArgs[0] = _drawMesh.GetIndexCount(0);
			_drawArgs[1] = (uint)particleCount;
			_drawArgsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)) * 5);
			_drawArgsBuffer.SetData(_drawArgs);


		}

		/// <summary>
		/// ComputeShaderを使用してそれぞれのバッファを初期化する
		/// </summary>
		private void InitParticlesKernel()
		{
			if (compute == null) return;
			var kernel = compute.FindKernel("InitParticles");
			uint x, y, z;
			compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
			compute.SetBuffer(kernel, "_Particles", _particleBuffer.read);
			compute.SetBuffer(kernel, "_ParticlePoolAppend", _poolBuffer);	// append/consume bufferを設定する。
			compute.Dispatch(kernel, Mathf.CeilToInt(particleCount / (int)x), (int)y, (int)z);
		}
	}
}