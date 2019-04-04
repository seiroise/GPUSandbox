using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
	public static class Constants
	{
		public static readonly int GRID_SORT_SIMULATION_BLOCK_SIZE = 32;
	}

	public enum ParticleCount
	{
		N_8K	= 1 << 13,
		N_16K	= 1 << 14,
		N_32K	= 1 << 15,
		N_65K	= 1 << 16,
		N_130K	= 1 << 17,
		N_260K	= 1 << 18,
	}

	public static class Functions
	{
		public static void DestroyBuffer(ref ComputeBuffer buffer)
		{
			if (buffer != null)
			{
				buffer.Release();
				buffer = null;
			}
		}
	}
}