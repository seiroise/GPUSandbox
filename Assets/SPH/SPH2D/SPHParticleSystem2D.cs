using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
    public class SPHParticleSystem2D : MonoBehaviour
    {

        public struct Particle2D
        {
            Vector2 position;
            Vector2 velocity;
            Vector2 acceleration;
            float density;
            float pressure;
        };

        public class ShaderProperties
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
            public readonly int particleCountConstId;
            public readonly int smoothlenConstId;
            public readonly int densityKernelCoefConstId;
            public readonly int pressureKernelCoefConstId;
            public readonly int viscosityKernelCoefConstId;
            public readonly int pressureStiffnessConstId;
            public readonly int restDensityConstId;
            public readonly int viscosityConstId;
            public readonly int gravityConstId;
            public readonly int timestepConstId;

            public ShaderProperties(ref ComputeShader cs)
            {
                densityKernelId = cs.FindKernel("DensityCS");
                pressureKernelId = cs.FindKernel("PressureCS");
                forceKernelId = cs.FindKernel("ForceCS");
                integrateKernelId = cs.FindKernel("IntegrateCS");

                particlesBufferReadId = Shader.PropertyToID("_ParticlesBufferRead");
                particlesBufferWriteId = Shader.PropertyToID("_ParticlesBufferWrite");

                particleCountConstId = Shader.PropertyToID("_ParticleCount");
                smoothlenConstId = Shader.PropertyToID("_Smoothlen");
                densityKernelCoefConstId = Shader.PropertyToID("_DensityKernelCoef");
                pressureKernelCoefConstId = Shader.PropertyToID("_PressureKernelCoef");
                viscosityKernelCoefConstId = Shader.PropertyToID("_ViscosityKernelCoef");
                pressureStiffnessConstId = Shader.PropertyToID("_PressureStiffness");
                restDensityConstId = Shader.PropertyToID("_RestDensity");
                viscosityConstId = Shader.PropertyToID("_Viscosity");
                gravityConstId = Shader.PropertyToID("_Gravity");
                timestepConstId = Shader.PropertyToID("_Timestep");
            }
        }

        public ParticleCount particleCount = ParticleCount.N_8K;	// パーティクル数
        public float smoothlen = 0.012f;                            // 粒子半径
        public float particleMass = 0.0002f;                        // パーティクル質量
        public float restDensity = 1000.0f;                         // 対象物体の密度
        [Space]
        public float pressureStiffness = 200.0f;                    // 圧力項係数
        public float viscosity = 0.1f;                              // 粘性係数
        [Space]
        public Vector2 gtavity = new Vector2(0.0f, -0.5f);          // 重力加速度
        [Space]
        public bool simulate = true;
        public float timestep = 0.05f;

        private int _particleCountInt;
        private float _densityCoef;
        private float _pressureCoef;
        private float _viscosityCoef;

        private ComputeShader _fluidCS;
        private ComputeBuffer _particlesBufferRead;
        private ComputeBuffer _particlesBufferWrite;




    }
}