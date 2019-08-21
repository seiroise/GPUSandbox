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

// CGS用の2Dパーティクル
struct CGS_Particle2D
{
	float2 position;
	float2 velocity;
	float radius;
	float threshold;
	int links;
	uint alive;
};

// CGS用の2Dエッジ
struct CGS_Edge2D
{
	int a, b;
	float2 force;
	uint alive;
};

// シンプルな3Dパーティクル
struct Particle3D
{
	float3 position;
	float3 velocity;
	float3 color;
};

// SPH用の3Dパーティクル
struct SPH_Particle3D
{
	float3 position;
	float3 velocity;
	float3 acceleration;
	float density;
	float pressure;
	float3 color;
};

#endif