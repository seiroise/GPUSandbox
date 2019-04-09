using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
    public static class Constants
    {
        public static readonly int SIMULATION_BLOCK_SIZE = 512;
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