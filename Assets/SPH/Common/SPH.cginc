#ifndef __INCLUDE_SPH__
#define __INCLUDE_SPH__

#define SIMULATION_BLOCK_SIZE 512

struct Particle2D
{
	float2 position;
	float2 velocity;
	float2 acceleration;
	float density;
	float pressure;
};

#endif