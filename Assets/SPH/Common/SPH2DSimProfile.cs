using UnityEngine;

namespace Seiro.GPUSandbox.SPH
{
	[CreateAssetMenu(fileName = "New SPH2DSimProfile", menuName = "Seiro/GPUSandbox/SPH/SPH2DSimProfile")]
	public class SPH2DSimProfile : ScriptableObject
	{
		[SerializeField, Range(1, 32)]
		private int _maxIterations = 8;
		[SerializeField, Range(0.0001f, 0.02f)]
		private float _timestep = 0.001f;

		[Space]

		[SerializeField, Range(1, 128)]
		private int _gridDimX = 32;
		[SerializeField, Range(1, 128)]
		private int _gridDimY = 32;
		[SerializeField, Range(0.01f, 1f)]
		private float _gridCellSize = 0.5f;

		[Space]

		[SerializeField]
		private ParticleCount _particleCount = ParticleCount.N_8K;
		[SerializeField, Range(0.01f, 1f)]
		private float _smoothlen = 0.5f;
		[SerializeField]
		private float _particleMass = 0.08f;
		[SerializeField]
		private float _restDensity = 1f;

		[Space]

		[SerializeField]
		private float _pressureStiffness = 5f;
		[SerializeField]
		private float _viscosity = 1f;

		[Space]

		[SerializeField]
		private Vector2 _gravity = new Vector2(0f, -9.8f);
		[SerializeField]
		private float _wallStiffness = 3000f;
		[SerializeField, Range(0.1f, 10f)]
		private float _mouseRadius = 2f;

		public int maxIterations
		{
			get { return _maxIterations; }
		}
		public float timestep
		{
			get { return _timestep; }
		}
		public int gridDimX
		{
			get { return _gridDimX; }
		}
		public int gridDimY
		{
			get { return _gridDimY; }
		}
		public float gridCellSize
		{
			get { return _gridCellSize; }
		}
		public Vector2Int gridDims
		{
			get { return new Vector2Int(gridDimX, gridDimY); }
		}
		public Vector2 simulationRange
		{
			get { return new Vector2(gridDimX * gridCellSize, gridDimY * gridCellSize); }
		}
		public ParticleCount particleCount
		{
			get { return _particleCount; }
		}
		public int particleCountInt
		{
			get { return (int)_particleCount; }
		}
		public float smoothlen
		{
			get { return _smoothlen; }
		}
		public float particleMass
		{
			get { return _particleMass; }
		}
		public float restDensity
		{
			get { return _restDensity; }
		}
		public float pressureStiffness
		{
			get { return _pressureStiffness; }
		}
		public float viscosity
		{
			get { return _viscosity; }
		}
		public Vector2 gravity
		{
			get { return _gravity; }
		}
		public float wallStiffness
		{
			get { return _wallStiffness; }
		}
		public float mouseRadius
		{
			get { return _mouseRadius; }
		}
	}
}