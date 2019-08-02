using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
	public sealed class GridSorter3D : GridSorterBase
	{
		public GridSorter3D(int objectCount, int objectStride, int gridCellCount, float gridCellSize, ComputeShader gridSortCS, ComputeShader bitonicSortCS, ParticleKind particleKind)
			: base(objectCount, objectStride, gridCellCount, gridCellSize, gridSortCS, bitonicSortCS, particleKind)
		{

		}

		protected override void SetGridDim(int shaderPropId, ComputeShader cs)
		{
			throw new System.NotImplementedException();
		}
	}
}