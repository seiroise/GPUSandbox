using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox
{
	public enum ParticleKind
	{
		NS = 0,	// neighbor search 用
		SPH = 1
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Particle2D
	{
		Vector2 position;
		Vector2 velocity;
		Vector3 color;

		public Particle2D(Vector2 position, Vector2 velocity, Vector3 color)
		{
			this.position = position;
			this.velocity = velocity;
			this.color = color;
		}
	}

	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct SPH_Particle2D
	{
		public Vector2 position;
		public Vector2 velocity;
		public Vector2 acceleration;
		public float density;
		public float pressure;
		public Vector3 Color;
	}
}
