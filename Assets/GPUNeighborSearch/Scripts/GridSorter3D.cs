using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
	public sealed class GridSorter3D : GridSorterBase
	{
		private Vector3Int _gridDim;

		public GridSorter3D(int objectCount, int objectStride, Vector3Int gridDim, float gridCellSize, Particle2DKind particleKind)
			: base(objectCount, objectStride, gridDim.x * gridDim.y * gridDim.z, gridCellSize, Resources.Load<ComputeShader>("GridSorter3D"), Resources.Load<ComputeShader>("BitonicSort"), particleKind)
		{
			_gridDim = gridDim;
		}

		protected override void SetGridDim(int shaderPropId, ComputeShader cs)
		{
			cs.SetInts(shaderPropId, _gridDim.x, _gridDim.y, _gridDim.z);
		}
	}
}