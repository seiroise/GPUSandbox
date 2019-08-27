using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class DpadController : MonoBehaviour
	{
		public float forceFactor;
		public float torqueFactor;
		public Rigidbody2D body;

		private void FixedUpdate()
		{
			float dt = Time.deltaTime;
			if (Input.GetKey(KeyCode.W))
			{
				Accele(dt);
			}
			float h = Input.GetAxisRaw("Horizontal");
			Rotation(dt, h);
		}

		private void Accele(float dt)
		{
			if (!body) return;
			body.AddForce(transform.rotation * Vector2.right * forceFactor);
		}

		private void Rotation(float dt, float torque)
		{
			if (!body) return;
			body.AddTorque(torque * torqueFactor);
		}
	}
}