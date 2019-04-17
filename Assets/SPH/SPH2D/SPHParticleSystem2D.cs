using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Seiro.GPUSandbox.SPH
{
	public class SPHParticleSystem2D : MonoBehaviour
	{

		public ComputeShader fluidCS = null;                        // 流体シミュレーション用のコンピュートシェーダ
		public ParticleCount particleCount = ParticleCount.N_8K;    // パーティクル数
		public float smoothlen = 0.012f;                            // 粒子半径
		public float particleMass = 0.0002f;                        // パーティクル質量
		public float restDensity = 1000.0f;                         // 対象物体の密度
		[Space]
		public float pressureStiffness = 200.0f;                    // 圧力項係数
		public float viscosity = 0.1f;                              // 粘性係数
		[Space]
		public Vector2 gravity = new Vector2(0.0f, -0.5f);          // 重力加速度
		public Vector2 range = new Vector2(10f, 5f);                // シミュレーション領域
		public float wallStiffness = 100f;
		public float mouseRadius = 1f;
		[Space]
		public bool simulate = true;
		[Range(1, 32)]
		public int maxIterations = 8;
		[Range(0.0001f, 0.02f)]
		public float timestep = 0.001f;
		[Space]
		public Material instancingMat = null;
		public Mesh instancingMesh = null;
		[Range(0.01f, 2f)]
		public float particleScale = 1f;

		public bool debugDraw = false;

		private int _particleCountInt;
		private float _densityCoef;
		private float _pressureGradCoef;
		private float _viscosityLapCoef;

		private ComputeBuffer _particlesBufferRead = null;
		private ComputeBuffer _particlesBufferWrite = null;

		private SPHShaderProps _shaderProps = null;

		private uint[] _instancingArgs = {0, 0, 0, 0, 0};
		private ComputeBuffer _instancingArgsBuffer = null;

        private void Start()
        {
            if (fluidCS == null)
            {
                return;
            }
            _particleCountInt = (int)particleCount;
            _shaderProps = new SPHShaderProps(ref fluidCS);
            InitParticlesBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);
			InitRendering();
			CalcCoef();
        }

        private void Update()
        {
            if (simulate)
            {
                int iterations = Mathf.Min(Mathf.FloorToInt(Time.deltaTime / timestep), maxIterations);
				if (iterations <= 0) iterations = 1;
				SetShaderProperties();
				for (int i = 0; i < iterations; ++i)
                {
                    Simulate();
                }
            }

			RenderParticles();
        }

        private void OnDestroy()
        {
            SPH.Functions.DestroyBuffer(ref _particlesBufferRead);
            SPH.Functions.DestroyBuffer(ref _particlesBufferWrite);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(range * 0.5f, range);
        }

        // 各種物理量の計算に使用する定数係数の事前計算。
        private void CalcCoef()
        {
            // 密度項 定数係数 poly6 kernel : mass * 4 / (pi * h^8)
            _densityCoef = particleMass * 4f / (Mathf.PI * Mathf.Pow(smoothlen, 8f));
            // 圧力項 勾配 定数係数 spiky kernel : mass * -20 / (pi * h^5)
            _pressureGradCoef = particleMass * -30f / (Mathf.PI * Mathf.Pow(smoothlen, 5f));
            // 粘性項 ラプラシアン 定数係数 viscosity kernel : mass * 20 / (3 * pi * h^5)
            _viscosityLapCoef = particleMass * 20f / (3f * Mathf.PI * Mathf.Pow(smoothlen, 5f));
        }

        // パーティクルバッファの初期化
        private void InitParticlesBuffer(ref ComputeBuffer pingBuffer, ref ComputeBuffer pongBuffer)
        {
            SPH_Particle2D[] particles = new SPH_Particle2D[_particleCountInt];
            Vector2 center = range * 0.5f;
            for (int i = 0, n = _particleCountInt; i < n; ++i)
            {
                SPH_Particle2D p = new SPH_Particle2D();
                p.position = Random.insideUnitCircle * range * 0.5f + center;
                p.velocity = Vector2.zero;
                p.acceleration = Vector2.zero;
                p.density = .0f;
                p.pressure = .0f;
                particles[i] = p;
            }

            int count = _particleCountInt;
            int stride = Marshal.SizeOf(typeof(SPH_Particle2D));
            pingBuffer = new ComputeBuffer(count, stride);
            pingBuffer.SetData(particles);
            pongBuffer = new ComputeBuffer(count, stride);
            pongBuffer.SetData(particles);
        }

		// レンダリング関係の初期化
		private void InitRendering()
		{
			if (instancingMat == null || instancingMesh == null)
			{
				return;
			}

			_instancingArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
			uint indexCount = instancingMesh.GetIndexCount(0);
			_instancingArgs[0] = indexCount;
			_instancingArgs[1] = (uint)_particleCountInt;
			_instancingArgsBuffer.SetData(_instancingArgs);
		}

        // シェーダ内の定数の設定
        private void SetShaderProperties()
        {
            if (fluidCS == null)
            {
                return;
            }

            fluidCS.SetInt(_shaderProps.particleCountConstId, _particleCountInt);
            fluidCS.SetFloat(_shaderProps.smoothlenConstId, smoothlen);
            fluidCS.SetFloat(_shaderProps.densityKernelCoefConstId, _densityCoef);
            fluidCS.SetFloat(_shaderProps.pressureKernelCoefConstId, _pressureGradCoef);
            fluidCS.SetFloat(_shaderProps.viscosityKernelCoefConstId, _viscosityLapCoef);
            fluidCS.SetFloat(_shaderProps.pressureStiffnessConstId, pressureStiffness);
            fluidCS.SetFloat(_shaderProps.restDensityConstId, restDensity);
            fluidCS.SetFloat(_shaderProps.viscosityConstId, viscosity);
            fluidCS.SetFloat(_shaderProps.timestepConstId, timestep);

            fluidCS.SetFloats(_shaderProps.gravityConstId, gravity.x, gravity.y);
            fluidCS.SetFloats(_shaderProps.rangeConstId, range.x, range.y);
            fluidCS.SetFloat(_shaderProps.wallStiffnessConstId, wallStiffness);

            Vector2 mousePosition = Vector2.zero;
            if (Camera.current)
            {
                Vector3 screenMousePosition = Input.mousePosition;
                screenMousePosition.z = -Camera.current.transform.position.z;
                mousePosition = Camera.current.ScreenToWorldPoint(screenMousePosition);
            }
            fluidCS.SetBool(_shaderProps.mouseDownConstId, Input.GetMouseButton(0));
            fluidCS.SetFloats(_shaderProps.mousePositionConstId, mousePosition.x, mousePosition.y);
            fluidCS.SetFloat(_shaderProps.mouseRadiusConstId, mouseRadius);
        }

        private void Simulate()
        {
            if (fluidCS == null)
            {
                return;
            }

            int threadGroupSize = _particleCountInt / SPH.Constants.SIMULATION_BLOCK_SIZE;

            // 密度の計算
            fluidCS.SetBuffer(_shaderProps.densityKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.densityKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.densityKernelId, threadGroupSize, 1, 1);
            SPH.Functions.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 圧力の計算
            fluidCS.SetBuffer(_shaderProps.pressureKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.pressureKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.pressureKernelId, threadGroupSize, 1, 1);
			SPH.Functions.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 計算しておいた密度、圧力をもとに圧力項と拡散粘性項を計算する。
            fluidCS.SetBuffer(_shaderProps.forceKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.forceKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.forceKernelId, threadGroupSize, 1, 1);
			SPH.Functions.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 計算しておいた力をもとに、漸進オイラー法によりパーティクルの座標を更新する
            fluidCS.SetBuffer(_shaderProps.integrateKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.integrateKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.integrateKernelId, threadGroupSize, 1, 1);
			SPH.Functions.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);
        }

		private void RenderParticles()
		{
			if (instancingMat == null || instancingMesh == null)
			{
				return;
			}

			instancingMat.SetPass(0);
			instancingMat.SetBuffer("buf", _particlesBufferRead);
			instancingMat.SetFloat("_Smoothlen", smoothlen * particleScale);
			Graphics.DrawMeshInstancedIndirect(instancingMesh, 0, instancingMat, new Bounds(range * 0.5f, range), _instancingArgsBuffer);
		}
    }
}