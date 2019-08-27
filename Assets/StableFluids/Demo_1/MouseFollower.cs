using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class MouseFollower : MonoBehaviour
	{
		public Rigidbody2D body;

		private void FixedUpdate()
		{
			if (body && Input.GetMouseButton(0))
			{
				Vector3 mPos = Input.mousePosition;
				mPos.z = -Camera.main.transform.position.z;
				Vector3 wPos = Camera.main.ScreenToWorldPoint(mPos);
				Vector3 diff = wPos - transform.position;
				body.AddForce(diff);
			}
		}
	}
}