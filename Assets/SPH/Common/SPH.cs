using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public class SPHShaderProps
    {
        // kernel
        public readonly int densityKernelId;
        public readonly int pressureKernelId;
        public readonly int forceKernelId;
        public readonly int integrateKernelId;

        // buffer
        public readonly int particlesBufferReadId;
        public readonly int particlesBufferWriteId;

        //constant variable
        public readonly int timestepConstId;
        public readonly int particleCountConstId;
        public readonly int smoothlenConstId;
        public readonly int densityKernelCoefConstId;
        public readonly int pressureKernelCoefConstId;
        public readonly int viscosityKernelCoefConstId;
        public readonly int pressureStiffnessConstId;
        public readonly int restDensityConstId;
        public readonly int viscosityConstId;

        public readonly int gravityConstId;
        public readonly int rangeConstId;
        public readonly int wallStiffnessConstId;

        public readonly int mouseDownConstId;
        public readonly int mousePositionConstId;
        public readonly int mouseRadiusConstId;

        public SPHShaderProps(ref ComputeShader cs)
        {
            densityKernelId = cs.FindKernel("DensityCS");
            pressureKernelId = cs.FindKernel("PressureCS");
            forceKernelId = cs.FindKernel("ForceCS");
            integrateKernelId = cs.FindKernel("IntegrateCS");

            particlesBufferReadId = Shader.PropertyToID("_ParticlesBufferRead");
            particlesBufferWriteId = Shader.PropertyToID("_ParticlesBufferWrite");

            timestepConstId = Shader.PropertyToID("_Timestep");
            particleCountConstId = Shader.PropertyToID("_ParticleCount");
            smoothlenConstId = Shader.PropertyToID("_Smoothlen");
            densityKernelCoefConstId = Shader.PropertyToID("_DensityKernelCoef");
            pressureKernelCoefConstId = Shader.PropertyToID("_PressureKernelCoef");
            viscosityKernelCoefConstId = Shader.PropertyToID("_ViscosityKernelCoef");
            pressureStiffnessConstId = Shader.PropertyToID("_PressureStiffness");
            restDensityConstId = Shader.PropertyToID("_RestDensity");
            viscosityConstId = Shader.PropertyToID("_Viscosity");

            gravityConstId = Shader.PropertyToID("_Gravity");
            rangeConstId = Shader.PropertyToID("_Range");
            wallStiffnessConstId = Shader.PropertyToID("_WallStiffness");

            mouseDownConstId = Shader.PropertyToID("_MouseDown");
            mousePositionConstId = Shader.PropertyToID("_MousePosition");
            mouseRadiusConstId = Shader.PropertyToID("_MouseRadius");
        }
    }
}