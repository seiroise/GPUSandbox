#ifndef __INCLUDE_PARTICLES_
#define __INCLUDE_PARTICLES_

// シンプルな2Dパーティクル
struct Particle2D
{
	float2 position;
	float2 velocity;
	float3 color;
};

// SPH用の2Dパーティクル
struct SPH_Particle2D
{
	float2 position;
	float2 velocity;
	float2 acceleration;
	float density;
	float pressure;
	float3 color;
};

#endif