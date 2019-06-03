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

		PingPongBuffer _particleBuffer;

		private void Start()
		{
			// パーティクル本体のデータを保持するためのダブルバッファ
			_particleBuffer = new PingPongBuffer(particleCount, typeof(CellularParticle));
			
			// オブジェクトプールの初期化

			// 分裂可能なオブジェクトのためのオブジェクトプールの初期化
		}
	}
}