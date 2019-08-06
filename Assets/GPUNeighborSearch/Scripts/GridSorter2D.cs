using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
	public sealed class GridSorter2D : GridSorterBase
	{
		private Vector2Int _gridDim;

		public GridSorter2D(int objectCount, int objectStride, Vector2Int gridDim, float gridCellSize, Particle2DKind particleKind)
			: base(objectCount, objectStride, gridDim.x * gridDim.y, gridCellSize, Resources.Load<ComputeShader>("GridSorter2D"), Resources.Load<ComputeShader>("BitonicSort"), particleKind)
		{
			_gridDim = gridDim;
		}

		protected override void SetGridDim(int shaderPropId, ComputeShader cs)
		{
			cs.SetInts(shaderPropId, _gridDim.x, _gridDim.y);
		}
	}
}