using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CGS
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct CGS_Particle2D
	{
		public Vector2 position;    // 粒子の座標
		public Vector2 velocity;    // 粒子の保持している速度
		public float rudius;        // 粒子の半径
		public float threshold;     // 粒子の最大半径
		public int links;           // 接続しているエッジの数
		public uint alive;          // 活性化フラグ
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct CGS_Edge2D
	{
		public int a, b;            // 接続している粒子の番号
		public Vector2 force;       // お互いを引きつけるための力
		uint alive;                 // エッジの活性化フラグ
	}

}