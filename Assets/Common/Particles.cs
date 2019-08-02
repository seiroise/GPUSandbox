using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox
{
	public enum ParticleKind
	{
		NS = 0,		// Neighbor Search 用
		SPH = 1		// Smoothed Particle Hydrodynamics 用
	}

	public enum ParticleCount
	{
		N_512 = 1 << 9,
		N_1K = 1 << 10,
		N_2K = 1 << 11,
		N_4K = 1 << 12,
		N_8K = 1 << 13,
		N_16K = 1 << 14,
		N_32K = 1 << 15,
		N_65K = 1 << 16,
		N_130K = 1 << 17,
		N_260K = 1 << 18,
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
