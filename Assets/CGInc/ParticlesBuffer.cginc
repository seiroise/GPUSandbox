#ifndef __INCLUDE_PARTICLES_BUFFER_
#define __INCLUDE_PARTICLES_BUFFER_

#ifdef GRIDSORT_SPH_PARTICLE_2D
StructuredBuffer<SPH_Particle2D> _ParticlesBufferRead;
RWStructuredBuffer<SPH_Particle2D> _ParticlesBufferWrite;

#elif GRIDSORT_PARTICLE_2D
StructuredBuffer<Particle2D> _ParticlesBufferRead;
RWStructuredBuffer<Particle2D> _ParticlesBufferWrite;

#else

#endif

#endif