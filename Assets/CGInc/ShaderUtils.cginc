#ifndef __INCLUDE_SHADER_UTILS_
#define __INCLUDE_SHADER_UTILS_

inline float max3(float3 v)
{
	return max(max(v.x, v.y), v.z);
}

inline float max4(float4 v)
{
	return max(max(v.x, v.y), max(v.z, v.w));
}

inline float2 addressing(in float2 p)
{
#if UNITY_UV_STARTS_AT_TOP
	return float2(p.x, 1 - p.y);
#else
	return p;
#endif
}

#endif