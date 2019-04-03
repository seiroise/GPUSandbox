using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{

    // グリッドの次元数はどうしても2Dと3Dで分ける必要はあるので、
    // それらについては派生クラスと、入力するシェーダプロパティでなんとかする。
	// うーんなんというか、ぱっと見で、このクラスはどのプロパティを設定する必要があるのかがわからない...
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

            _threadGroupCount = _objectCount / Constants.GRID_SORT_SIMULATION_BLOCK_SIZE;
            _bitonicSort = new BitonicSort(_objectCount, bitonicSortCS);
        }
		
        public void ReleaseResources()
        {
            Functions.DestroyBuffer(ref _gridParticlesBuffer);
			Functions.DestroyBuffer(ref _gridParticlesPingPongBuffer);
			Functions.DestroyBuffer(ref _gridIndicesBuffer);
			Functions.DestroyBuffer(ref _sortedObjectsTemporaryBuffer);
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

		private void InitializeBuffer()
		{
			_gridParticlesBuffer = new ComputeBuffer(_objectCount, sizeof(uint) * 2);
			_gridParticlesPingPongBuffer = new ComputeBuffer(_objectCount, sizeof(uint) * 2);

			_gridIndicesBuffer = new ComputeBuffer(_gridCellCount, sizeof(uint) * 2);
			_sortedObjectsTemporaryBuffer = new ComputeBuffer(_objectCount, _objectStride);
		}

        private void SetShaderParams()
        {
            _gridSortCS.SetInt(_shaderParams.particleCountConstId, _objectCount);
			_gridSortCS.SetFloat(_shaderParams.gridCellSizeConstId, _gridCellSize);

			SetGridDim(_shaderParams.gridDimConstId, _gridSortCS);
        }

		// 派生クラスでは必ずこの値を設定する必要がある、逆に言うとこれ以外は設定しなくてもいい。
		// 正直内部的にどんなベクトル型でもいいっていうのが一番心配な部分。
		protected abstract void SetGridDim(int shaderPropId, ComputeShader cs);
    }
}