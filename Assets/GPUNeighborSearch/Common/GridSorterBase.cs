using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{

    // グリッドの次元数はどうしても2Dと3Dで分ける必要はあるので、
    // それらについては派生クラスと、入力するシェーダプロパティでなんとかする。
	// うーんなんというか、ぱっと見で、このクラスはどのプロパティを設定する必要があるのかがわからない...
    public abstract class GridSorterBase
    {
		public class ShaderProps
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

			public ShaderProps(ref ComputeShader cs, ParticleKind particleKind)
			{
				// カーネルの使用しているバッファの型が異なるが、
				// それぞれ対応するインデックス分ずれて宣言されているのでparticleKindだけずらす。
				int offset = (int)particleKind;

				buildGridParticlesKernelId = cs.FindKernel("BuildGridParticlesCS") + offset;
				clearGridIndicesKernelId = cs.FindKernel("ClearGridIndicesCS") + offset;
				buildGridIndicesKernelId = cs.FindKernel("BuildGridIndicesCS") + offset;
				rearrangeParticlesKernelId = cs.FindKernel("RearrangeParticlesCS") + offset;
				copyBufferKernelId = cs.FindKernel("CopyBuffer") + offset;

				particlesBufReadId = Shader.PropertyToID("_ParticlesBufferRead");
				particlesBufWriteId = Shader.PropertyToID("_ParticlesBufferWrite");
				gridParticlePairBufReadId = Shader.PropertyToID("_GridParticlePairBufferRead");
				gridParticlePairBufWriteId = Shader.PropertyToID("_GridParticlePairBufferWrite");
				gridIndicesBufReadId = Shader.PropertyToID("_GridIndicesBufferRead");
				gridIndicesBufWriteId = Shader.PropertyToID("_GridIndicesBufferWrite");

				particleCountConstId = Shader.PropertyToID("_ParticleCount");
				gridDimConstId = Shader.PropertyToID("_GridDim");
				gridCellSizeConstId = Shader.PropertyToID("_GridCellSize");
			}
		};

        protected int _objectCount;
		protected int _objectStride;

        protected int _gridCellCount;
        protected float _gridCellSize;

        protected ComputeShader _gridSortCS;
		protected ShaderProps _shaderProps;

		private ComputeBuffer _gridParticlesBuffer;					// size : uint2 * objectCount
        private ComputeBuffer _gridParticlesPingPongBuffer;			// size : uint2 * objectCount

        protected ComputeBuffer _gridIndicesBuffer;					// size : uint2 * gridCellCount;
        protected ComputeBuffer _sortedObjectsTemporaryBuffer;		// size : T * objectCount

        private int _threadGroupCount;
        private BitonicSort _bitonicSort;

		public ComputeBuffer gridIndicesBuffer { get { return _gridIndicesBuffer; } }

        public GridSorterBase(int objectCount, int objectStride, int gridCellCount, float gridCellSize, ComputeShader gridSortCS, ComputeShader bitonicSortCS, ParticleKind particleKind)
        {
            _objectCount = objectCount;
			_objectStride = objectStride;

			_gridCellCount = gridCellCount;
			_gridCellSize = gridCellSize;

            _gridSortCS = gridSortCS;
            _shaderProps = new ShaderProps(ref _gridSortCS, particleKind);

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

        public void GridSort(ref ComputeBuffer objectBuffer)
        {
            SetShaderParams();

            // build grid particle
            _gridSortCS.SetBuffer(_shaderProps.buildGridParticlesKernelId, _shaderProps.particlesBufReadId, objectBuffer);
            _gridSortCS.SetBuffer(_shaderProps.buildGridParticlesKernelId, _shaderProps.gridParticlePairBufWriteId, _gridParticlesBuffer);
            _gridSortCS.Dispatch(_shaderProps.buildGridParticlesKernelId, _threadGroupCount, 1, 1);

            // sort particles with grid hash by bitonic sort
            _bitonicSort.Sort(ref _gridParticlesBuffer, ref _gridParticlesPingPongBuffer);

			// constract grid indices
			_gridSortCS.SetBuffer(_shaderProps.buildGridIndicesKernelId, _shaderProps.gridParticlePairBufReadId, _gridParticlesBuffer);
			_gridSortCS.SetBuffer(_shaderProps.buildGridIndicesKernelId, _shaderProps.gridIndicesBufWriteId, _gridIndicesBuffer);
			_gridSortCS.Dispatch(_shaderProps.buildGridIndicesKernelId, _threadGroupCount, 1, 1);

			// rearrange particles
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.gridParticlePairBufReadId, _gridParticlesBuffer);
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.particlesBufReadId, objectBuffer);
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.particlesBufWriteId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.Dispatch(_shaderProps.rearrangeParticlesKernelId, _threadGroupCount, 1, 1);

			// copy buffer
			_gridSortCS.SetBuffer(_shaderProps.copyBufferKernelId, _shaderProps.particlesBufReadId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.SetBuffer(_shaderProps.copyBufferKernelId, _shaderProps.particlesBufWriteId, objectBuffer);
			_gridSortCS.Dispatch(_shaderProps.copyBufferKernelId, _threadGroupCount, 1, 1);
        }

		// 前回のソート結果を使用してソートを行う。
		// オブジェクトの数が同一のバッファの場合に適応可能。
		public void RearrangeWithIndices(ref ComputeBuffer objectBuffer)
		{
			if (objectBuffer.count != _objectCount)
			{
				return;
			}

			// rearrange
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.gridParticlePairBufReadId, _gridParticlesBuffer);
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.particlesBufReadId, objectBuffer);
			_gridSortCS.SetBuffer(_shaderProps.rearrangeParticlesKernelId, _shaderProps.particlesBufWriteId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.Dispatch(_shaderProps.rearrangeParticlesKernelId, _threadGroupCount, 1, 1);

			// copy buffer
			_gridSortCS.SetBuffer(_shaderProps.copyBufferKernelId, _shaderProps.particlesBufReadId, _sortedObjectsTemporaryBuffer);
			_gridSortCS.SetBuffer(_shaderProps.copyBufferKernelId, _shaderProps.particlesBufWriteId, objectBuffer);
			_gridSortCS.Dispatch(_shaderProps.copyBufferKernelId, _threadGroupCount, 1, 1);
		}

		public void LogGridIndicesForDebug()
		{
			int count = _gridIndicesBuffer.count;
			uint[] data = new uint[count * 2];
			_gridIndicesBuffer.GetData(data);

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < count; ++i)
			{
				sb.AppendFormat("grid[{0}] : start = {1}, end = {2}", i, data[i * 2], data[i * 2 + 1]);
				sb.AppendLine();
			}

			Debug.Log(sb.ToString());
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
            _gridSortCS.SetInt(_shaderProps.particleCountConstId, _objectCount);
			_gridSortCS.SetFloat(_shaderProps.gridCellSizeConstId, _gridCellSize);

			SetGridDim(_shaderProps.gridDimConstId, _gridSortCS);
        }

		// 派生クラスでは必ずこの値を設定する必要がある、逆に言うとこれ以外は設定しなくてもいい。
		// 正直内部的にどんなベクトル型でもいいっていうのが一番心配な部分。
		protected abstract void SetGridDim(int shaderPropId, ComputeShader cs);
    }
}