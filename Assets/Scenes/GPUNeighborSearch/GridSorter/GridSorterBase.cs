using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{

    // グリッドの次元数はどうしても2Dと3Dで分ける必要はあるので、
    // それらについては派生クラスと、入力するシェーダプロパティでなんとかする。
    public abstract class GridSorterBase
    {
        protected static readonly int SIMULATION_BLOCK_SIZE = 32;

        public class ShaderParameters
        {
            // kernel
            public readonly int buildGridParticlesKernelId;
            public readonly int clearGridIndicesKernelId;
            public readonly int buildGridIndicesKernelId;
            public readonly int rearrangeParticlesKernelId;
            public readonly int copyBufferKernelId;

            // buffer
            public readonly int particlesBufReadId;
            public readonly int particlesBufWriteId;
            public readonly int gridParticlePairBufReadId;
            public readonly int gridParticlePairBufWriteId;
            public readonly int gridIndicesBufReadId;
            public readonly int gridIndicesBufWriteId;

            // constant variable
            public readonly int particleCountConstId;
            public readonly int gridDimConstId;
            public readonly int gridHConstId;

            public ShaderParameters(ref ComputeShader cs)
            {
                buildGridParticlesKernelId = cs.FindKernel("BuildGridParticlesCS");
                clearGridIndicesKernelId = cs.FindKernel("ClearGridIndicesCS");
                buildGridIndicesKernelId = cs.FindKernel("BuildGridIndicesCS");
                rearrangeParticlesKernelId = cs.FindKernel("RearrangeParticlesCS");
                copyBufferKernelId = cs.FindKernel("CopyBuffer");

                particlesBufReadId = Shader.PropertyToID("_ParticlesBufferRead");
                particlesBufWriteId = Shader.PropertyToID("_ParticlesBufferWrite");
                gridParticlePairBufReadId = Shader.PropertyToID("_GridParticlePairBufferRead");
                gridParticlePairBufWriteId = Shader.PropertyToID("_GridParticlePairBufferWrite");
                gridIndicesBufReadId = Shader.PropertyToID("_GridIndicesBufferRead");
                gridIndicesBufWriteId = Shader.PropertyToID("_GridIndicesBufferWrite");

                particleCountConstId = Shader.PropertyToID("_ParticleCount");
                gridDimConstId = Shader.PropertyToID("_GridDim");
                gridHConstId = Shader.PropertyToID("_GridH");
            }
        };

        protected int _objectCount;

        protected int _gridCellCount;
        protected int _gridCellSize;

        protected ShaderParameters _shaderParams;
        protected ComputeShader _gridSortCS;

        protected ComputeBuffer _gridParticlesBuffer;
        protected ComputeBuffer _gridParticlesPingPongBuffer;

        protected ComputeBuffer _gridIndicesBuffer;
        protected ComputeBuffer _sortedObjectsOutputBuffer;

        private int _threadGroupCount;
        private BitonicSort _bitonicSort;

        public GridSorterBase(int objectCount, ComputeShader gridSortCS, ComputeShader bitonicSortCS)
        {
            _objectCount = objectCount;

            _gridSortCS = gridSortCS;
            _shaderParams = new ShaderParameters(ref _gridSortCS);

            InitializeBuffer();

            _threadGroupCount = objectCount / SIMULATION_BLOCK_SIZE;
            _bitonicSort = new BitonicSort(objectCount, bitonicSortCS);
        }

        public void ReleaseResources()
        {
            DestroyBuffer(_gridParticlesBuffer);
            DestroyBuffer(_gridParticlesPingPongBuffer);
            DestroyBuffer(_gridIndicesBuffer);
            DestroyBuffer(_sortedObjectsOutputBuffer);
        }

        public void GridSort(ref ComputeBuffer particlesBuffer)
        {
            SetShaderParams();

            // build grid particle
            _gridSortCS.SetBuffer(_shaderParams.buildGridParticlesKernelId, _shaderParams.particlesBufReadId, particlesBuffer);
            _gridSortCS.SetBuffer(_shaderParams.buildGridParticlesKernelId, _shaderParams.gridParticlePairBufWriteId, _gridParticlesBuffer);
            _gridSortCS.Dispatch(_shaderParams.buildGridParticlesKernelId, _threadGroupCount, 1, 1);

            // sort particles with grid hash by bitonic sort
            _bitonicSort.Sort(ref _gridParticlesBuffer, ref _gridParticlesPingPongBuffer);

            // constract grid indices

            // rearrange particles

            // copy buffer

        }

        private void DestroyBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        protected abstract void InitializeBuffer();

        protected virtual void SetShaderParams()
        {
            _gridSortCS.SetInt(_shaderParams.particleCountConstId, _objectCount);
        }
    }
}