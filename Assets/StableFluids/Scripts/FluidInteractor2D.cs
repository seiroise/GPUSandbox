using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class FluidInteractor2D : MonoBehaviour
	{
		public bool canInteract = true;
		public Fluid2D target;
		public Rigidbody2D body;
		public float minVelocityThresh = 0.01f;

		[Space]
		public float interactionForce = 0.01f;
		public float interactionRadius = 0.01f;
		public Color color = Color.white;

		private void Update()
		{
			if (canInteract)
			{
				Interact();
			}
		}

		private void Interact()
		{
			if (!target || !body) return;
			if(body.velocity.sqrMagnitude > minVelocityThresh * minVelocityThresh)
			{
				target.Interact(transform.position, interactionRadius, transform.rotation * Vector3.left * interactionForce, color);
			}
		}
	}
}