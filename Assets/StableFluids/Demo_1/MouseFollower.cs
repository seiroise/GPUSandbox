using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class MouseFollower : MonoBehaviour
	{
		public Rigidbody2D body;
		public float forceFactor = 5f;
		public float steerFactor = 5f;

		private void FixedUpdate()
		{
			if (body && Input.GetMouseButton(0))
			{
				Vector3 mPos = Input.mousePosition;
				mPos.z = -Camera.main.transform.position.z;
				Vector3 wPos = Camera.main.ScreenToWorldPoint(mPos);
				Vector3 diff = wPos - transform.position;

				body.AddForce(transform.right * forceFactor);
				body.AddTorque(Vector3.Cross(transform.right, diff.normalized).z * steerFactor);
			}
		}
	}
}