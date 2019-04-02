using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{

    // グリッドの次元数はどうしても2Dと3Dで分ける必要はあるので、
    // それらについては派生クラスと、入力するシェーダプロパティでなんとかする。
    public abstract class GridSorterBase
    {
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
			public readonly int gridCellSizeConstId;

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
				gridCellSizeConstId = Shader.PropertyToID("_GridH");
			}
		};

		protected static readonly int SIMULATION_BLOCK_SIZE = 32;

        protected int _objectCount;
		protected int _objectStride;

        protected int _gridCellCount;
        protected float _gridCellSize;

        protected ComputeShader _gridSortCS;
		protected ShaderParameters _shaderParams;

		private ComputeBuffer _targetObjectsBuffer;

		private ComputeBuffer _gridParticlesBuffer;				// uint2 * objectCount
        private ComputeBuffer _gridParticlesPingPongBuffer;		// uint2 * objectCount

        protected ComputeBuffer _gridIndicesBuffer;				// uint2 * gridCellCount;
        protected ComputeBuffer _sortedObjectsTemporaryBuffer;		// T * objectCount

        private int _threadGroupCount;
        private BitonicSort _bitonicSort;

        public GridSorterBase(ComputeBuffer targetObjectsBuffer, int gridCellCount, float gridCellSize, ComputeShader gridSortCS, ComputeShader bitonicSortCS)
        {
			_targetObjectsBuffer = targetObjectsBuffer;

            _objectCount = targetObjectsBuffer.count;
			_objectStride = targetObjectsBuffer.stride;

			_gridCellCount = gridCellCount;
			_gridCellSize = gridCellSize;

            _gridSortCS = gridSortCS;
            _shaderParams = new ShaderParameters(ref _gridSortCS);

            InitializeBuffer();

            _threadGroupCount = _objectCount / SIMULATION_BLOCK_SIZE;
            _bitonicSort = new BitonicSort(_objectCount, bitonicSortCS);
        }
		
        public void ReleaseResources()
        {
            DestroyBuffer(_gridParticlesBuffer);
            DestroyBuffer(_gridParticlesPingPongBuffer);
            DestroyBuffer(_gridIndicesBuffer);
            DestroyBuffer(_sortedObjectsTemporaryBuffer);
        }

        public void GridSort()
        {
            SetShaderParams();

            // build grid particle
            _gridSortCS.SetBuffer(_shaderParams.buildGridParticlesKernelId, _shaderParams.particlesBufReadId, _targetObjectsBuffer);
            _gridSortCS.SetBuffer(_shaderParams.buildGridParticlesKernelId, _shaderParams.gridParticlePairBufWriteId, _gridParticlesBuffer);
            _gridSortCS.Dispatch(_shaderParams.buildGridParticlesKernelId, _threadGroupCount, 1, 1);

            // sort particles with grid hash by bitonic sort
            _bitonicSort.Sort(ref _gridParticlesBuffer, ref _gridParticlesPingPongBuffer);

			// constract grid indices
			_gridSortCS.SetBuffer(_shaderParams.buildGridIndicesKernelId, _shaderParams.gridParticlePairBufReadId, _gridParticlesBuffer);
			_gridSortCS.SetBuffer(_shaderParams.buildGridIndicesKernelId, _shaderParams.gridIndicesBufWriteId, _gridIndicesBuffer);
			_gridSortCS.Dispatch(_shaderParams.buildGridIndicesKernelId, _threadGroupCount, 1, 1);

			// rearrange particles
			_gridSortCS.SetBuffer(_shaderParams.rearrangeParticlesKernelId, _shaderParams.gridParticlePairBufReadId, _gridParticlesBuffer);
			_gridSortCS.SetBuffer(_shaderParams.rearrangeParticlesKernelId, _shaderParams.particlesBufReadId, _targetObjectsBuffer);
			_gridSortCS.SetBuffer(_shaderParams.rearrangeParticlesKernelId, _shaderParams.particlesBufWriteId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.Dispatch(_shaderParams.rearrangeParticlesKernelId, _threadGroupCount, 1, 1);

			// copy buffer
			_gridSortCS.SetBuffer(_shaderParams.copyBufferKernelId, _shaderParams.particlesBufReadId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.SetBuffer(_shaderParams.copyBufferKernelId, _shaderParams.particlesBufWriteId, _targetObjectsBuffer);
			_gridSortCS.Dispatch(_shaderParams.copyBufferKernelId, _threadGroupCount, 1, 1);
        }

        private void DestroyBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

		private void InitializeBuffer()
		{
			_gridParticlesBuffer = new ComputeBuffer(_objectCount, sizeof(uint) * 2);
			_gridParticlesPingPongBuffer = new ComputeBuffer(_objectCount, sizeof(uint) * 2);

			_gridIndicesBuffer = new ComputeBuffer(_gridCellCount, sizeof(uint) * 2);
			_sortedObjectsTemporaryBuffer = new ComputeBuffer(_objectCount, _objectStride);
		}

        protected virtual void SetShaderParams()
        {
            _gridSortCS.SetInt(_shaderParams.particleCountConstId, _objectCount);
			_gridSortCS.SetFloat(_shaderParams.gridCellSizeConstId, _gridCellSize);
        }
    }
}