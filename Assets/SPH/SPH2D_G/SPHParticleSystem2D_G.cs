using System.Runtime.InteropServices;
using Seiro.GPUSandbox.NS;
using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
    public class SPHParticleSystem2D_G : MonoBehaviour
    {

        public class SPH_GShaderProps : SPHShaderProps
        {
            public readonly int indicesBufferReadId;

            public readonly int gridDimConstId;
            public readonly int gridCellSizeConstId;

            public SPH_GShaderProps(ref ComputeShader cs) : base(ref cs)
            {
                indicesBufferReadId = Shader.PropertyToID("_IndicesBufferRead");

                gridDimConstId = Shader.PropertyToID("_GridDim");
                gridCellSizeConstId = Shader.PropertyToID("_GridCellSize");
            }
        }

        public ComputeShader fluidCS = null;

        [Space]

        public bool simulate = true;        // シミュレーションの有効/無効
                                            /*
                                            [Range(1, 32)]
                                            public int maxIterations = 8;		// 1フレーム内のシミュレーション反復回数
                                            [Range(0.0001f, 0.02f)]
                                            public float timestep = 0.001f;     // シミュレーションの時間刻み幅

                                            [Space]
                                            [Range(1, 128)]
                                            public int gridDimX = 32;           // x方向のグリッド分割数
                                            [Range(1, 128)]
                                            public int gridDimY = 32;           // y方向のグリッド分割数
                                            [Range(0.01f, 10f)]
                                            public float gridCellSize = 1f;     // 一つの正方グリッドの縦横の大きさ

                                            [Space]

                                            public ParticleCount particleCount = ParticleCount.N_8K;
                                            public float smoothlen = 0.5f;
                                            public float particleMass = 0.08f;
                                            public float restDensity = 1f;

                                            [Space]

                                            public float pressureStiffness = 5f;
                                            public float viscosity = 1f;

                                            [Space]

                                            public Vector2 gravity = new Vector2(0f, -9.8f);
                                            public float wallStiffness = 100f;
                                            public float mouseRadius = 2f;
                                            */
        [Space]
        public SPH2DSimProfile simProfile;

        [Space]
        public Material instancingMat = null;
        public Mesh instancingMesh = null;
        [Range(0.01f, 2f)]
        public float particleScale = 1f;
        public Camera viewCamera;

        private int _particleCountInt;
        private Vector2 _simulationRange;
        private int _threadGroupSize;
        private float _densityCoef;
        private float _pressureGradCoef;
        private float _viscosityLapCoef;

        private SPH_GShaderProps _shaderProps = null;
        private GridSorter2D _gridSorter = null;
        private ComputeBuffer _particlesBufferRead = null;
        private ComputeBuffer _particlesBufferWrite = null;

        private uint[] _instancingArgs = { 0, 0, 0, 0, 0 };
        private ComputeBuffer _instancingArgsBuffer = null;

        private SPH_Particle2D[] _dumpBuffer;

        private void Start()
        {
            if (fluidCS == null)
            {
                return;
            }
            if (simProfile == null)
            {

            }

            _particleCountInt = simProfile.particleCountInt;
            _simulationRange = simProfile.simulationRange;
            _threadGroupSize = _particleCountInt / SPH.Constants.SIMULATION_BLOCK_SIZE;

            _shaderProps = new SPH_GShaderProps(ref fluidCS);
            InitParticlesBuffers(ref _particlesBufferRead, ref _particlesBufferWrite);
            _gridSorter = new GridSorter2D(_particleCountInt, Marshal.SizeOf(typeof(SPH_Particle2D)), simProfile.gridDims, simProfile.gridCellSize, Particle2DKind.SPH);
            InitRendering();
            CalcCoef();

            if (viewCamera != null)
            {
                Vector3 position = _simulationRange * 0.5f;
                position.z = viewCamera.transform.position.z;
                viewCamera.transform.position = position;
                viewCamera.orthographic = true;
                viewCamera.orthographicSize = _simulationRange.y * 0.5f;
            }

            // Shader.DisableKeyword();
        }

        private void Update()
        {
            if (simulate)
            {
                int iterations = Mathf.Min(Mathf.FloorToInt(Time.deltaTime / simProfile.timestep), simProfile.maxIterations);
                if (iterations <= 0) iterations = 1;
                SetShaderProperties();
                for (int i = 0; i < iterations; ++i)
                {
                    Simulate();
                }
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                _gridSorter.LogGridIndicesForDebug();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DumpParticlesBuffer();
            }

            RenderParticles();
        }

        private void OnDestroy()
        {
            UtilFunc.ReleaseBuffer(ref _particlesBufferRead);
            UtilFunc.ReleaseBuffer(ref _particlesBufferWrite);
            UtilFunc.ReleaseBuffer(ref _instancingArgsBuffer);
            if (_gridSorter != null)
            {
                _gridSorter.ReleaseResources();
                _gridSorter = null;
            }
        }

        private void OnDrawGizmos()
        {
            if (simProfile != null)
            {
                Vector2 range = simProfile.simulationRange;
                Gizmos.DrawWireCube(range * 0.5f, range);
            }
        }

        private void InitParticlesBuffers(ref ComputeBuffer pingBuffer, ref ComputeBuffer pongBuffer)
        {
            SPH_Particle2D[] particles = new SPH_Particle2D[_particleCountInt];
            Vector2 center = _simulationRange * 0.5f;
            for (int i = 0, n = _particleCountInt; i < n; ++i)
            {
                SPH_Particle2D p = new SPH_Particle2D();
                p.position = Random.insideUnitCircle * _simulationRange * 0.5f + center;
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

        void CalcCoef()
        {
            float pMass = simProfile.particleMass;
            float smoothlen = simProfile.smoothlen;

            // 密度 定数係数 poly6 kernel : mass * 4 / (pi * h^8)
            _densityCoef = pMass * 4f / (Mathf.PI * Mathf.Pow(smoothlen, 8));
            // 圧力項 勾配定数係数 spiky kernel : mass * -30 / (pi * h^5)
            _pressureGradCoef = pMass * -30f / (Mathf.PI * Mathf.Pow(smoothlen, 5));
            // 粘性項 ラプラシアン定数係数 viscosity kernel : mass * 20 / ( 3 * pi * h^5)
            _viscosityLapCoef = pMass * 20f / (3f * Mathf.PI * Mathf.Pow(smoothlen, 5));
        }

        // シェーダ内の定数の設定
        private void SetShaderProperties()
        {
            if (fluidCS == null)
            {
                return;
            }

            fluidCS.SetInt(_shaderProps.particleCountConstId, _particleCountInt);
            fluidCS.SetFloat(_shaderProps.smoothlenConstId, simProfile.smoothlen);
            fluidCS.SetFloat(_shaderProps.densityKernelCoefConstId, _densityCoef);
            fluidCS.SetFloat(_shaderProps.pressureKernelCoefConstId, _pressureGradCoef);
            fluidCS.SetFloat(_shaderProps.viscosityKernelCoefConstId, _viscosityLapCoef);
            fluidCS.SetFloat(_shaderProps.pressureStiffnessConstId, simProfile.pressureStiffness);
            fluidCS.SetFloat(_shaderProps.restDensityConstId, simProfile.restDensity);
            fluidCS.SetFloat(_shaderProps.viscosityConstId, simProfile.viscosity);
            fluidCS.SetFloat(_shaderProps.timestepConstId, simProfile.timestep);

            fluidCS.SetFloats(_shaderProps.gravityConstId, simProfile.gravity.x, simProfile.gravity.y);
            fluidCS.SetFloats(_shaderProps.rangeConstId, _simulationRange.x, _simulationRange.y);
            fluidCS.SetFloat(_shaderProps.wallStiffnessConstId, simProfile.wallStiffness);

            Vector2 mousePosition = Vector2.zero;
            if (Camera.main)
            {
                Vector3 screenMousePosition = Input.mousePosition;
                screenMousePosition.z = -Camera.main.transform.position.z;
                mousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);
            }
            fluidCS.SetBool(_shaderProps.mouseDownConstId, Input.GetMouseButton(0));
            fluidCS.SetFloats(_shaderProps.mousePositionConstId, mousePosition.x, mousePosition.y);
            fluidCS.SetFloat(_shaderProps.mouseRadiusConstId, simProfile.mouseRadius);

            // grid sort
            fluidCS.SetInts(_shaderProps.gridDimConstId, simProfile.gridDimX, simProfile.gridDimY);
            fluidCS.SetFloat(_shaderProps.gridCellSizeConstId, simProfile.gridCellSize);
        }

        private void Simulate()
        {
            if (fluidCS == null)
            {
                return;
            }

            // パーティクルのグリッドによるソート
            _gridSorter.GridSort(ref _particlesBufferRead);

            // 密度
            fluidCS.SetBuffer(_shaderProps.densityKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.densityKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.SetBuffer(_shaderProps.densityKernelId, _shaderProps.indicesBufferReadId, _gridSorter.gridIndicesBuffer);
            fluidCS.Dispatch(_shaderProps.densityKernelId, _threadGroupSize, 1, 1);
            UtilFunc.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 圧力
            fluidCS.SetBuffer(_shaderProps.pressureKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.pressureKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.pressureKernelId, _threadGroupSize, 1, 1);
            UtilFunc.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 圧力勾配 + 速度ラプラシアン
            fluidCS.SetBuffer(_shaderProps.forceKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.forceKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.SetBuffer(_shaderProps.forceKernelId, _shaderProps.indicesBufferReadId, _gridSorter.gridIndicesBuffer);
            fluidCS.Dispatch(_shaderProps.forceKernelId, _threadGroupSize, 1, 1);
            UtilFunc.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // 外力 + 各種力の合成と漸進オイラー法による観測点座標の更新
            fluidCS.SetBuffer(_shaderProps.integrateKernelId, _shaderProps.particlesBufferReadId, _particlesBufferRead);
            fluidCS.SetBuffer(_shaderProps.integrateKernelId, _shaderProps.particlesBufferWriteId, _particlesBufferWrite);
            fluidCS.Dispatch(_shaderProps.integrateKernelId, _threadGroupSize, 1, 1);
            UtilFunc.SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);
        }

        private void RenderParticles()
        {
            if (instancingMat == null || instancingMesh == null)
            {
                return;
            }

            instancingMat.SetPass(0);
            instancingMat.SetBuffer("buf", _particlesBufferRead);
            instancingMat.SetFloat("_Smoothlen", simProfile.smoothlen * particleScale);
            Graphics.DrawMeshInstancedIndirect(instancingMesh, 0, instancingMat, new Bounds(_simulationRange * 0.5f, _simulationRange), _instancingArgsBuffer);
        }

        private void DumpParticlesBuffer()
        {
            if (_dumpBuffer == null || _dumpBuffer.Length != _particleCountInt)
            {
                _dumpBuffer = new SPH_Particle2D[_particleCountInt];
            }
            _particlesBufferRead.GetData(_dumpBuffer);
        }
    }
}