using UnityEngine;

namespace Seiro.GPUSandbox.NS
{
	public sealed class GridSorter2D : GridSorterBase
	{
		private Vector2Int _gridDim;

		public GridSorter2D(ComputeBuffer objectBuffer, Vector2Int gridDim, float gridCellSize)
			: base(objectBuffer, gridDim.x * gridDim.y, gridCellSize, Resources.Load<ComputeShader>("GridSort2D"), Resources.Load<ComputeShader>("BitonicSort"))
		{
			_gridDim = gridDim;
		}

		protected override void SetGridDim(int shaderPropId, ComputeShader cs)
		{
			cs.SetInts(shaderPropId, _gridDim.x, _gridDim.y);
		}
	}
}