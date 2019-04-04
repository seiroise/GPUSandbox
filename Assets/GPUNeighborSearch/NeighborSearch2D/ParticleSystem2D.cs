using UnityEngine;
using System.Runtime.InteropServices;

namespace Seiro.GPUSandbox.NS
{
    public sealed class ParticleSystem2D : MonoBehaviour
    {
        public sealed class ShaderProperties
        {
            public readonly int updateParticleColorKernelId;

            public readonly int displayGridConstId;
            public readonly int gridCellSizeConstId;

            public readonly int particlesBufReadId;
            public readonly int particlesBufWriteId;

            public ShaderProperties(ref ComputeShader cs)
            {
                updateParticleColorKernelId = cs.FindKernel("UpdateParticleColorCS");

                displayGridConstId = Shader.PropertyToID("_DisplayGrid");
                gridCellSizeConstId = Shader.PropertyToID("_GridCellSize");

                particlesBufReadId = Shader.PropertyToID("_ParticlesBufferRead");
                particlesBufWriteId = Shader.PropertyToID("_ParticlesBufferWrite");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Particle2D
        {
            Vector2 position;
            Vector3 color;

            public Particle2D(Vector2 position, Vector3 color)
            {
                this.position = position;
                this.color = color;
            }
        }

        public ParticleCount particleCount = ParticleCount.N_8K;
        public float gridCellSize = 1;
        public Vector2Int gridDim = new Vector2Int(16, 16);
        public ComputeShader particleCS;

        [Space]

        public int displayGridIndex = 0;
        public Material renderingMaterial;
        public Mesh instancingMesh;

        private int _particleCountInt;
        private int _threadGroupX;
        private ComputeBuffer _particlesBufferRead;
        private ComputeBuffer _particlesBufferWrite;
        private GridSorter2D _gridSorter;

        private ShaderProperties _shaderProps;

        private ComputeBuffer _instancingArgsBuffer;
        private uint[] _instancingArgs = { 0, 0, 0, 0, 0 };

        public ComputeBuffer particlesBuffer { get { return _particlesBufferRead; } }

        private void Start()
        {
            _particleCountInt = (int)particleCount;
            _threadGroupX = _particleCountInt / Constants.GRID_SORT_SIMULATION_BLOCK_SIZE;

            int structSize = Marshal.SizeOf(typeof(Particle2D));
            _particlesBufferRead = new ComputeBuffer(_particleCountInt, structSize);
            _particlesBufferWrite = new ComputeBuffer(_particleCountInt, structSize);
            InitializeParticlesBuffer();

            _gridSorter = new GridSorter2D(_particlesBufferRead, gridDim, gridCellSize);
            _shaderProps = new ShaderProperties(ref particleCS);

            _instancingArgsBuffer = new ComputeBuffer(1, sizeof(uint) * _instancingArgs.Length, ComputeBufferType.IndirectArguments);
            uint indexCount = (instancingMesh != null) ? (uint)instancingMesh.GetIndexCount(0) : 0;
            _instancingArgs[0] = indexCount;
            _instancingArgs[1] = (uint)_particleCountInt;
            _instancingArgsBuffer.SetData(_instancingArgs);
        }

        private void Update()
        {
            if (particleCS == null) return;

            Vector4 displayGrid = new Vector4(displayGridIndex % gridDim.x, displayGridIndex / gridDim.x);
            // grid sort
            _gridSorter.GridSort();

            // update particle's color
            // particleCS.SetVector(_shaderProps.displayGridConstId, displayGrid);
            particleCS.SetInt("_DispGridX", displayGridIndex % gridDim.x);
            particleCS.SetInt("_DispGridY", displayGridIndex / gridDim.x);
            particleCS.SetFloat(_shaderProps.gridCellSizeConstId, gridCellSize);
            particleCS.SetBuffer(_shaderProps.updateParticleColorKernelId, _shaderProps.particlesBufReadId, _particlesBufferRead);
            particleCS.SetBuffer(_shaderProps.updateParticleColorKernelId, _shaderProps.particlesBufWriteId, _particlesBufferWrite);
            particleCS.Dispatch(_shaderProps.updateParticleColorKernelId, _threadGroupX, 1, 1);

            // swap buffer
            SwapBuffer(ref _particlesBufferRead, ref _particlesBufferWrite);

            // rendering
            if (instancingMesh == null || renderingMaterial == null) return;

            renderingMaterial.SetPass(0);
            renderingMaterial.SetBuffer("buf", particlesBuffer);
            Graphics.DrawMeshInstancedIndirect(instancingMesh, 0, renderingMaterial, new Bounds(Vector3.zero, new Vector3(100f, 100f, 100f)), _instancingArgsBuffer);
        }

        private void OnDestroy()
        {
            ReleaseResources();
        }

        private void InitializeParticlesBuffer()
        {
            Particle2D[] particles = new Particle2D[_particleCountInt];
            Vector2 gridRange = gridCellSize * (Vector2)gridDim;
            for (int i = 0, n = _particleCountInt; i < n; ++i)
            {
                particles[i] = new Particle2D(new Vector2(Random.Range(0f, gridRange.x), Random.Range(0f, gridRange.y)), Vector3.one);
            }
            _particlesBufferRead.SetData(particles);
        }

        private void ReleaseResources()
        {
            Functions.DestroyBuffer(ref _particlesBufferRead);
            Functions.DestroyBuffer(ref _particlesBufferWrite);
            Functions.DestroyBuffer(ref _instancingArgsBuffer);
            _gridSorter.ReleaseResources();
        }

        private void SwapBuffer(ref ComputeBuffer lhs, ref ComputeBuffer rhs)
        {
            ComputeBuffer temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}