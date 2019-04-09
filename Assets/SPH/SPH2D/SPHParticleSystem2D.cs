using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
	public class SPHParticleSystem2D : MonoBehaviour
	{
		public class ShaderProperties
		{
			// kernel
			public readonly int densityKernelId;
			public readonly int pressureKernelId;
			public readonly int forceKernelId;
			public readonly int integrateKernelId;

			// buffer
			public readonly int particlesBufferReadId;
			public readonly int particlesBufferWriteId;

			//constant variable
			public readonly int particleCountConstId;
			public readonly int smoothlenConstId;
			public readonly int densityKernelCoefConstId;
			public readonly int pressureKernelCoefConstId;
			public readonly int viscosityKernelCoefConstId;
			public readonly int pressureStiffnessConstId;
			public readonly int restDensityConstId;
			public readonly int viscosityConstId;
			public readonly int gravityConstId;
			public readonly int timestepConstId;

			public ShaderProperties(ref ComputeShader cs)
			{
				densityKernelId		= cs.FindKernel("DensityCS");
				pressureKernelId	= cs.FindKernel("PressureCS");
				forceKernelId		= cs.FindKernel("ForceCS");
				integrateKernelId	= cs.FindKernel("IntegrateCS");

				particlesBufferReadId	= Shader.PropertyToID("_ParticlesBufferRead");
				particlesBufferWriteId	= Shader.PropertyToID("_ParticlesBufferWrite");

				particleCountConstId		= Shader.PropertyToID("_ParticleCount");
				smoothlenConstId			= Shader.PropertyToID("_Smoothlen");
				densityKernelCoefConstId	= Shader.PropertyToID("_DensityKernelCoef");
				pressureKernelCoefConstId	= Shader.PropertyToID("_PressureKernelCoef");
				viscosityKernelCoefConstId	= Shader.PropertyToID("_ViscosityKernelCoef");
				pressureStiffnessConstId	= Shader.PropertyToID("_PressureStiffness");
				restDensityConstId			= Shader.PropertyToID("_RestDensity");
				viscosityConstId			= Shader.PropertyToID("_Viscosity");
				gravityConstId				= Shader.PropertyToID("_Gravity");
				timestepConstId				= Shader.PropertyToID("_Timestep");
			}
		}

		
	}
}