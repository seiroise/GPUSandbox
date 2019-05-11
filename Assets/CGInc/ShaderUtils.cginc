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

#endif