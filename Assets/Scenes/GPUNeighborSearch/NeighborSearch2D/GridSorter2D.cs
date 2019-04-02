using UnityEngine;
using System.Runtime.InteropServices;

namespace Seiro.GPUSandbox.NS
{
	public sealed class GridSorter2D : GridSorterBase
	{
		public struct Particle2D
		{
			Vector2 position;
			Vector3 color;
		}

		private Vector2Int _gridDim;
		private float _cellSize;

		public GridSorter2D(ComputeBuffer objectBuffer, int objectCount, Vector2Int gridDim, float gridCellSize)
			: base(objectBuffer, gridDim.x * gridDim.y, gridCellSize, Resources.Load<ComputeShader>("GridSort2D"), Resources.Load<ComputeShader>("BitonicSort"))
		{
			_gridDim = gridDim;
			_cellSize = gridCellSize;
		}
	}
}