using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
	public class SPHParticleSystem2D_G : MonoBehaviour
	{

		public class ShaderProperties
		{
			
		}

		public ComputeShader fluidCS;

		[Space]

		public bool simulate = true;		// シミュレーションの有効/無効
		[Range(1, 32)]
		public int maxIterations = 8;		// 1フレーム内のシミュレーション反復回数
		[Range(0.0001f, 0.005f)]
		public float timestep = 0.001f;		// シミュレーションの時間刻み幅

		[Space]

		public Vector2Int gridDim;          // グリッド数
		public float gridCellSize;          // グリッドセルの大きさ

		[Space]

		public ParticleCount particleCount = ParticleCount.N_8K;
		public float smoothlen = 0.5f;
		public float particleMass = 0.08f;
		public float restDensity = 1f;

		[Space]

		public float pressureStiffness = 5f;
		public float viscosity = 1f;

		[Space]

		public Vector2 gravity = new Vector2(0f, -9.8f);
		public float wallStiffness = 100f;
		public float mouseRadius = 2f;

		private bool _hasInitialized = false;

		private int _particleCountInt;
		private float _densityCoef;
		private float _pressureGradCoef;
		private float _viscosityLapCoef;

		private ComputeBuffer _particlesBufferRead;
		private ComputeBuffer _particlesBufferWrite;

		private void Start()
		{
			if (fluidCS == null)
			{
				return;
			}

			

		}

		private void Update()
		{
			if (!_hasInitialized)
			{
				return;
			}


		}
	}
}