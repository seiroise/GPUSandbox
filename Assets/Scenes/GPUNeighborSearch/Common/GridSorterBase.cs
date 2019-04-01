using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
    public abstract class GridSorterBase
    {
		protected static readonly int SIMULATION_BLOCK_SIZE = 32;

		public class ShaderParameters
		{
			public readonly int buildGridKernelId;
			public readonly int clearGridKernelId;
			public readonly int buildGridIndicesKernelId;
			public readonly int rearrangeParticlesKernelId;
			public readonly int copyBufferKernelId;

			public readonly int readParticlesBufId;
			public readonly int writeParticlesBufId;
			public readonly int readGridBufId;
			public readonly int writeGridBufId;
		};

		protected int _objectCount;

		protected int _gridCellCount;
		protected int _gridCellSize;

		protected ComputeShader _gridSortCS;

		protected ComputeBuffer _gridBuffer;
		protected ComputeBuffer _gridPingPongBuffer;
		protected ComputeBuffer _gridIndicesBuffer;
		protected ComputeBuffer _sortedObjectsOutputBuffer;

		private BitonicSort _bitonicSort;

		private int _threadGroupCount;

		public GridSorterBase(int objectCount, ComputeShader bitonicSortCS)
		{
			_objectCount = objectCount;
			_bitonicSort = new BitonicSort(objectCount, bitonicSortCS);
			_threadGroupCount = objectCount / SIMULATION_BLOCK_SIZE;
		}

		public void ReleaseResources()
		{
			DestroyBuffer(_gridBuffer);
			DestroyBuffer(_gridPingPongBuffer);
			DestroyBuffer(_gridIndicesBuffer);
			DestroyBuffer(_sortedObjectsOutputBuffer);
		}

		public void GridSort(ref ComputeBuffer inBuffer)
		{

		}

		private void DestroyBuffer(ComputeBuffer buffer)
		{
			if (buffer != null)
			{
				buffer.Release();
				buffer = null;
			}
		}


	}
}