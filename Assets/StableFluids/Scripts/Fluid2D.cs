using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class Fluid2D : MonoBehaviour
	{
		public FluidSimulator2D simulator;
		public FluidScreen2D screen;

		public void Interact(Vector3 worldPosition, float radius, Vector2 force, Color color)
		{
			if (simulator && screen)
			{
				Vector3 pos = screen.WorldToScreenViewport(worldPosition);
				simulator.Interact(pos, radius, force, color);
			}
		}
	}
}