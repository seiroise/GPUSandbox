#ifndef __INCLUDE_NEIGHBOR_SEARCH_
#define __INCLUDE_NEIGHBOR_SEARCH_

#define NS_SIMULATION_BLOCK_SIZE 32

// 座標に対応するグリッド番号を計算する。
// 小数点以下については、そのグリッド内の正規化された位置。(今回は使わないけど。
inline float2 CalcGridAddress(float2 pos, float gridCellSize, float2 gridDim)
{
	return clamp(pos / gridCellSize, float2(0, 0), float2(gridDim - 1));
}

inline float3 CalcGridAddressFor3D(float3 pos, float gridCellSize, float3 gridDim)
{
	return clamp(pos / gridCellSize, float3(0, 0, 0), float3(gridDim - 1));
}

#endif // __INCLUDE_NEIGHBOR_SEARCH_