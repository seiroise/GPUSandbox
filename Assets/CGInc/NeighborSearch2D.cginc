﻿#ifndef __INCLUDE_NEIGHBOR_SEARCH_2D_
#define __INCLUDE_NEIGHBOR_SEARCH_2D_

#define NS_SIMULATION_BLOCK_SIZE 32

// 座標に対応するグリッド番号を計算する。
// 小数点以下については、そのグリッド内の正規化された位置。(今回は使わないけど。
inline float2 CalcGridAddress(float2 pos, float gridCellSize, float2 gridDim)
{
	return clamp(pos / gridCellSize, float2(0, 0), float2(gridDim - 1));
}

#endif // __INCLUDE_NEIGHBOR_SEARCH_2D_